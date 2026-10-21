using Microsoft.EntityFrameworkCore;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Tests.Support;

// Simula snapshots e falha atômica; não comprova transações ou constraints SQL.
public sealed class AnaliseSimulada
{
    public List<Requisito> Requisitos { get; } = [];
    public List<OperacaoAnalise> Operacoes { get; } = [];
    public List<Analise> Analises { get; } = [];
    public List<Sugestao> Sugestoes { get; } = [];
    public bool FalharResultado { get; set; }
    public bool FalharIntencao { get; set; }
    public Action? AntesResultado { get; set; }
    public Func<Task>? AntesResultadoAsync { get; set; }
    public Func<Task>? AntesPrazoAsync { get; set; }
    private long sequencia = 100;

    public AnaliseSimulada()
    {
        var seed = new SimulatedRequisitoStore(); seed.Seed();
        Requisitos.Add(seed.Rows.Single());
    }
    public ApplicationDbContext Contexto() => new ContextoSimulado(this);
    private static T Copiar<T>(T source) where T : class, new()
    {
        var target = new T();
        foreach (var prop in typeof(T).GetProperties().Where(x => x.CanWrite &&
            (x.PropertyType.IsValueType || x.PropertyType == typeof(string) || x.PropertyType == typeof(byte[]))))
            prop.SetValue(target, prop.GetValue(source));
        return target;
    }

    private sealed class ContextoSimulado : ApplicationDbContext
    {
        private readonly AnaliseSimulada store;
        private readonly Dictionary<Type, object> sets = [];
        public ContextoSimulado(AnaliseSimulada store) : base(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer().Options)
        {
            this.store = store;
            var requisitos = store.Requisitos.Select(Copiar).ToArray();
            var operacoes = store.Operacoes.Select(Copiar).ToArray();
            var analises = store.Analises.Select(Copiar).ToArray();
            var sugestoes = store.Sugestoes.Select(Copiar).ToArray();
            foreach (var op in operacoes) op.Requisito = requisitos.Single(x => x.IdRequisito == op.IdRequisito);
            foreach (var analise in analises) analise.Requisito = requisitos.Single(x => x.IdRequisito == analise.IdRequisito);
            foreach (var sugestao in sugestoes) sugestao.Analise = analises.Single(x => x.IdAnalise == sugestao.IdAnalise);
            Registrar(requisitos); Registrar(operacoes); Registrar(analises); Registrar(sugestoes);
        }
        private void Registrar<T>(T[] rows) where T : class
        {
            AttachRange(rows); sets[typeof(T)] = new SimulatedSet<T>(this, rows);
        }
        public override DbSet<T> Set<T>() => sets.TryGetValue(typeof(T), out var set) ? (DbSet<T>)set : base.Set<T>();
        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            ChangeTracker.DetectChanges();
            var entries = ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified).ToArray();
            if (store.AntesPrazoAsync is not null && entries.Any(x => x.Entity is OperacaoAnalise
                { Estado: "Enviada", PrazoExcedidoEmUtc: not null } && x.State == EntityState.Modified))
                await store.AntesPrazoAsync();
            if (entries.Any(x => x.Entity is Analise))
            {
                if (store.AntesResultadoAsync is not null) await store.AntesResultadoAsync();
                store.AntesResultado?.Invoke();
                if (store.FalharResultado) throw new DbUpdateException("SIMULADO conteúdo sensível");
            }
            if (store.FalharIntencao && entries.Any(x => x.Entity is OperacaoAnalise && x.State == EntityState.Added))
                throw new DbUpdateException("SIMULADO intenção");
            foreach (var entry in entries.Where(x => x.Entity is OperacaoAnalise && x.State == EntityState.Modified))
            {
                var op = (OperacaoAnalise)entry.Entity;
                if (!store.Operacoes.Single(x => x.IdOperacao == op.IdOperacao).RowVersion
                    .SequenceEqual((byte[])entry.Property("RowVersion").OriginalValue!)) throw new DbUpdateConcurrencyException();
            }
            foreach (var entry in entries.Where(x => x.Entity is Requisito && x.State == EntityState.Modified))
            {
                var requisito = (Requisito)entry.Entity;
                if (!store.Requisitos.Single(x => x.IdRequisito == requisito.IdRequisito).RowVersion
                    .SequenceEqual((byte[])entry.Property("RowVersion").OriginalValue!)) throw new DbUpdateConcurrencyException();
            }
            foreach (var entry in entries.Where(x => x.Entity is Analise && x.State == EntityState.Added))
                ((Analise)entry.Entity).IdAnalise = store.Analises.Count + 1;
            foreach (var entry in entries)
            {
                switch (entry.Entity)
                {
                    case Requisito requisito:
                        requisito.RowVersion = BitConverter.GetBytes(++store.sequencia);
                        store.Requisitos.RemoveAll(x => x.IdRequisito == requisito.IdRequisito);
                        store.Requisitos.Add(Copiar(requisito)); break;
                    case OperacaoAnalise op:
                        op.RowVersion = BitConverter.GetBytes(++store.sequencia);
                        store.Operacoes.RemoveAll(x => x.IdOperacao == op.IdOperacao);
                        store.Operacoes.Add(Copiar(op)); break;
                    case Analise analise: store.Analises.Add(Copiar(analise)); break;
                    case Sugestao sugestao:
                        sugestao.IdAnalise = sugestao.Analise.IdAnalise;
                        sugestao.IdSugestao = store.Sugestoes.Count + 1;
                        store.Sugestoes.Add(Copiar(sugestao)); break;
                }
            }
            ChangeTracker.AcceptAllChanges();
            return entries.Length;
        }
    }
}
