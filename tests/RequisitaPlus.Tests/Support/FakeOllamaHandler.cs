using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace RequisitaPlus.Tests.Support;

/// <summary>Somente respostas simuladas; nunca delega para rede. Cada passo é de uso único.</summary>
public sealed class FakeOllamaHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> steps = new();
    private int calls;
    public int Calls => Volatile.Read(ref calls);
    public void Enqueue(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> step) => steps.Enqueue(step);
    public void EnqueueJson(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        Enqueue((_, _) => Task.FromResult(new HttpResponseMessage(status)
        { Content = new StringContent(json, Encoding.UTF8, "application/json") }));

    public Barrier EnqueueBlocked(string json)
    {
        var barrier = new Barrier();
        Enqueue(async (_, ct) =>
        {
            barrier.Entered.TrySetResult();
            await barrier.Release.Task.WaitAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });
        return barrier;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref calls);
        cancellationToken.ThrowIfCancellationRequested();
        if (!steps.TryDequeue(out var step))
            throw new InvalidOperationException("Chamada simulada não programada.");
        return step(request, cancellationToken);
    }

    public sealed class Barrier
    {
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task WaitUntilEnteredAsync(CancellationToken ct = default) => Entered.Task.WaitAsync(ct);
        public void Complete() => Release.TrySetResult();
    }
}
