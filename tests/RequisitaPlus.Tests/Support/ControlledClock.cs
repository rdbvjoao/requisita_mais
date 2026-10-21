namespace RequisitaPlus.Tests.Support;

/// <summary>Relógio simulado, sem espera real ou avanço automático.</summary>
public sealed class ControlledClock(DateTimeOffset initial) : TimeProvider
{
    private readonly object gate = new();
    private DateTimeOffset current = initial.ToUniversalTime();
    public override DateTimeOffset GetUtcNow() { lock (gate) return current; }
    public void Advance(TimeSpan interval)
    {
        if (interval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
        lock (gate) current = current.Add(interval);
    }
}
