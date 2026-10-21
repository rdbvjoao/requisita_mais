using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using RequisitaPlus.Web.Data;
using RequisitaPlus.Web.Models;

namespace RequisitaPlus.Tests.Support;

// Test double only. Uses SQL Server metadata but never opens a connection and is
// not an EF database provider. It does not prove SQL constraints/transactions.
public sealed class SimulatedRequisitoStore
{
    public List<Requisito> Rows { get; } = [];
    public Exception? SaveFailure { get; set; }
    public Action? BeforeSave { get; set; }
    public int Writes { get; private set; }
    private long sequence = 100;

    public SimulatedRequisitoContext Context() => new(this);

    public void Seed(int id = 1, int owner = 1, string status = "Rascunho") => Rows.Add(new()
    {
        IdRequisito = id, IdUsuario = owner, Titulo = "Título original", Descricao = "Descrição original",
        Prioridade = "Alta", Status = status, VersaoTextual = 1, DataCadastroUtc = TestData.Instant,
        RowVersion = BitConverter.GetBytes(++sequence)
    });

    internal int Save(SimulatedRequisitoContext db)
    {
        BeforeSave?.Invoke();
        if (SaveFailure is not null) throw SaveFailure;
        var entries = db.ChangeTracker.Entries<Requisito>().Where(x => x.State is EntityState.Added or EntityState.Modified).ToArray();
        foreach (var entry in entries.Where(x => x.State == EntityState.Modified))
        {
            var stored = Rows.SingleOrDefault(x => x.IdRequisito == entry.Entity.IdRequisito);
            if (stored is null || !stored.RowVersion.SequenceEqual(entry.Property(x => x.RowVersion).OriginalValue))
                throw new DbUpdateConcurrencyException("SIMULADO conflito durante commit.");
        }
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added) entry.Entity.IdRequisito = Rows.Select(x => x.IdRequisito).DefaultIfEmpty().Max() + 1;
            entry.Entity.RowVersion = BitConverter.GetBytes(++sequence);
            Rows.RemoveAll(x => x.IdRequisito == entry.Entity.IdRequisito);
            Rows.Add(Clone(entry.Entity));
        }
        Writes += entries.Length;
        db.ChangeTracker.AcceptAllChanges();
        return entries.Length;
    }

    internal static Requisito Clone(Requisito x) => new()
    {
        IdRequisito = x.IdRequisito, IdUsuario = x.IdUsuario, Titulo = x.Titulo, Descricao = x.Descricao,
        Prioridade = x.Prioridade, Status = x.Status, VersaoTextual = x.VersaoTextual,
        UltimaOrdemAnalise = x.UltimaOrdemAnalise, DataCadastroUtc = x.DataCadastroUtc,
        DataAtualizacaoUtc = x.DataAtualizacaoUtc, RowVersion = x.RowVersion.ToArray()
    };
}

public sealed class SimulatedRequisitoContext : ApplicationDbContext
{
    private readonly SimulatedRequisitoStore store;
    private readonly DbSet<Requisito> requisitos;

    public SimulatedRequisitoContext(SimulatedRequisitoStore store)
        : base(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer().Options)
    {
        this.store = store;
        var rows = store.Rows.Select(SimulatedRequisitoStore.Clone).ToArray();
        AttachRange(rows);
        requisitos = new SimulatedSet<Requisito>(this, rows);
    }

    public override DbSet<TEntity> Set<TEntity>() => typeof(TEntity) == typeof(Requisito)
        ? (DbSet<TEntity>)(object)requisitos
        : typeof(TEntity) == typeof(Analise) || typeof(TEntity) == typeof(Sugestao)
            ? new SimulatedSet<TEntity>(this, []) : base.Set<TEntity>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(store.Save(this));
    }
}

internal sealed class SimulatedSet<T>(DbContext db, IEnumerable<T> rows) : DbSet<T>, IQueryable<T>, IAsyncEnumerable<T> where T : class
{
    private readonly SimulatedAsyncQuery<T> query = new(rows.AsQueryable());
    public override IEntityType EntityType => db.Model.FindEntityType(typeof(T))!;
    public override EntityEntry<T> Add(T entity) => db.Add(entity);
    Type IQueryable.ElementType => typeof(T);
    Expression IQueryable.Expression => query.Expression;
    IQueryProvider IQueryable.Provider => query.Provider;
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => query.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => query.GetEnumerator();
    public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default) => query.GetAsyncEnumerator(ct);
}

internal sealed class SimulatedAsyncQuery<T>(IQueryable<T> query) : IOrderedQueryable<T>, IAsyncEnumerable<T>
{
    public Type ElementType => typeof(T);
    public Expression Expression => query.Expression;
    public IQueryProvider Provider => new SimulatedAsyncProvider(query.Provider);
    public IEnumerator<T> GetEnumerator() => query.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default) => new SimulatedEnumerator<T>(query.GetEnumerator(), ct);
}

internal sealed class SimulatedAsyncProvider(IQueryProvider provider) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new SimulatedAsyncQuery<TElement>(provider.CreateQuery<TElement>(expression));
    public object? Execute(Expression expression) => provider.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => provider.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = provider.Execute(expression);
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(typeof(TResult).GenericTypeArguments[0]).Invoke(null, [result])!;
    }
}

internal sealed class SimulatedEnumerator<T>(IEnumerator<T> enumerator, CancellationToken ct) : IAsyncEnumerator<T>
{
    public T Current => enumerator.Current;
    public ValueTask DisposeAsync() { enumerator.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() { ct.ThrowIfCancellationRequested(); return ValueTask.FromResult(enumerator.MoveNext()); }
}
