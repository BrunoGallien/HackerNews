using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsRetriever;

public interface IDataProvider
{
    // Snapshot, or null if the first load hasn't completed (or it expired).
    DataSnapshot? Current { get; }
}

public interface IDataPublisher
{
    void Publish(DataSnapshot snapshot);
}

public sealed class DataCache(IMemoryCache cache, DataOptions options) : IDataProvider, IDataPublisher
{
    private const string Key = "remote-data";

    public DataSnapshot? Current =>
        cache.TryGetValue(Key, out DataSnapshot? snapshot) ? snapshot : null;

    public void Publish(DataSnapshot snapshot) =>
        cache.Set(Key, snapshot, options.MaxAge);
}
