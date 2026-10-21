using System.Text.RegularExpressions;

namespace RequisitaPlus.Tests.Integration.SqlServer;

/// <summary>Conservative parser for the approved CHECK grammar. Unknown syntax fails closed.</summary>
public static class CheckExpression
{
    public static string Canonical(string text)
    {
        var parser = new Parser(text);
        var result = parser.Or();
        if (!parser.End) throw new InvalidOperationException("SQL_CHECK_GRAMMAR_NOT_RECOGNIZED");
        return result.Print();
    }

    private sealed record Node(string Kind, params Node[] Children)
    {
        public string Print() => Children.Length == 0 ? Kind : Kind + "(" + string.Join(',', Children.Select(x => x.Print())) + ")";
        public static Node Logic(string kind, IEnumerable<Node> nodes)
        {
            // AND/OR are associative and commutative in SQL three-valued logic.
            var flat = nodes.SelectMany(n => n.Kind == kind ? n.Children : [n]).OrderBy(n => n.Print(), StringComparer.Ordinal).ToArray();
            return flat.Length == 1 ? flat[0] : new Node(kind, flat);
        }
    }

    private sealed class Parser
    {
        private static readonly Regex Token = new(@"\G\s*(N?'(?:''|[^'])*'|\[(?:\]\]|[^\]])+\]|[A-Za-z_][A-Za-z_0-9]*|[0-9]+|<>|>=|<=|[(),=<>])", RegexOptions.IgnoreCase);
        private readonly List<string> tokens = [];
        private int position;
        public bool End => position == tokens.Count;
        public Parser(string text)
        {
            var offset = 0;
            while (offset < text.Length && !string.IsNullOrWhiteSpace(text[offset..]))
            {
                var match = Token.Match(text, offset);
                if (!match.Success) throw new InvalidOperationException("SQL_CHECK_GRAMMAR_NOT_RECOGNIZED");
                var value = match.Groups[1].Value;
                tokens.Add(value.StartsWith("'", StringComparison.Ordinal) ? value :
                    value.StartsWith("N'", StringComparison.OrdinalIgnoreCase) ? "N" + value[1..] :
                    value.Trim('[', ']').ToLowerInvariant());
                offset = match.Index + match.Length;
            }
        }
        private string Peek => End ? "" : tokens[position];
        private bool Take(string token) { if (Peek != token) return false; position++; return true; }
        private void Need(string token) { if (!Take(token)) throw new InvalidOperationException("SQL_CHECK_GRAMMAR_NOT_RECOGNIZED"); }
        public Node Or()
        {
            var parts = new List<Node> { And() };
            while (Take("or")) parts.Add(And());
            return Node.Logic("or", parts);
        }
        private Node And()
        {
            var parts = new List<Node> { Predicate() };
            while (Take("and")) parts.Add(Predicate());
            return Node.Logic("and", parts);
        }
        private Node Predicate()
        {
            if (Take("not"))
            {
                // SQL Server prints x NOT LIKE p as NOT (x) LIKE p.
                // Accept only that equivalent form, not arbitrary NOT expressions.
                var predicate = Predicate();
                if (predicate.Kind != "like" || predicate.Children.Length != 2) throw new InvalidOperationException("SQL_CHECK_GRAMMAR_NOT_RECOGNIZED");
                return new Node("notlike", predicate.Children);
            }
            var left = Value();
            if (Peek is "=" or "<>" or ">=" or "<=" or ">" or "<")
            {
                var op = tokens[position++];
                return new Node(op, left, Value());
            }
            if (Take("is"))
            {
                var negate = Take("not"); Need("null");
                return new Node(negate ? "isnotnull" : "isnull", left);
            }
            if (Take("between"))
            {
                var low = Value(); Need("and");
                return Node.Logic("and", [new Node(">=", left, low), new Node("<=", left, Value())]);
            }
            if (Take("in"))
            {
                Need("("); var values = new List<Node> { Value() };
                while (Take(",")) values.Add(Value());
                Need(")"); return Node.Logic("or", values.Select(v => new Node("=", left, v)));
            }
            if (Take("not")) { Need("like"); return new Node("notlike", left, Value()); }
            if (Take("like")) return new Node("like", left, Value());
            return left;
        }
        private Node Value()
        {
            Node value;
            if (Take("(")) { value = Or(); Need(")"); }
            else
            {
                if (End || Peek is ")" or ",") throw new InvalidOperationException("SQL_CHECK_GRAMMAR_NOT_RECOGNIZED");
                var name = tokens[position++];
                value = new Node(name);
                if (Take("("))
                {
                    if (name is not ("datalength" or "replace" or "nchar")) throw new InvalidOperationException("SQL_CHECK_FUNCTION_NOT_RECOGNIZED");
                    var arguments = new List<Node>();
                    if (!Take(")"))
                    {
                        arguments.Add(Or());
                        while (Take(",")) arguments.Add(Or());
                        Need(")");
                    }
                    value = new Node(name, arguments.ToArray());
                }
            }
            if (Take("collate"))
            {
                if (End) throw new InvalidOperationException("SQL_CHECK_GRAMMAR_NOT_RECOGNIZED");
                value = new Node("collate", value, new Node(tokens[position++]));
            }
            return value;
        }
    }
}
