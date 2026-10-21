namespace RequisitaPlus.Web.Services;

public sealed class RelogioLocal(TimeProvider clock)
{
    public static TimeZoneInfo Fuso { get; } = ObterFuso();
    public DateTime AgoraUtc => clock.GetUtcNow().UtcDateTime;
    public DateTimeOffset ParaApresentacao(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Informe um instante UTC.", nameof(utc));
        return TimeZoneInfo.ConvertTime(new DateTimeOffset(utc), Fuso);
    }
    private static TimeZoneInfo ObterFuso()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch (TimeZoneNotFoundException) when (OperatingSystem.IsWindows())
        { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}
