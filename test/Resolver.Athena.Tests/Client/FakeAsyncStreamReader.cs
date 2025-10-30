using Grpc.Core;

namespace Resolver.Athena.Tests.Client;

internal class FakeAsyncStreamReader<T>(IEnumerable<T> items) : IAsyncStreamReader<T>
{
    private readonly IEnumerator<T> _enumerator = items.GetEnumerator();

    public T Current => _enumerator.Current;

    public Task<bool> MoveNext(CancellationToken cancellationToken)
        => Task.FromResult(_enumerator.MoveNext());
}
