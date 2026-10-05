using System.Text.Json;

namespace HackerNewsRetriever;

public sealed class DataRefreshWorker(
    IHttpClientFactory httpFactory,
    IDataPublisher publisher,
    DataOptions options,
    ILogger<DataRefreshWorker> log) : BackgroundService
{
    private sealed record HnItem(
        string? Title,
        string? Url,
        string? By,
        int Score,
        long Time,
        int Descendants)
    {
        public News ToNews() => new(Title, Url, By, Score, Time, Descendants);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(options.RefreshInterval);

        // Refresh immediately at startup, then on every tick.
        do
        {
            try
            {
                await RefreshAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Previous snapshot stays in the cache.
                log.LogWarning(ex, "Refresh failed; keeping previous data");
            }
        }
        while (await WaitForNextTickAsync(timer, ct));
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        var http = httpFactory.CreateClient(nameof(DataRefreshWorker));

        var allIds = await http.GetFromJsonAsync<int[]>(options.BestStoriesUrl, ct)
                     ?? throw new InvalidOperationException("Empty id list");

        if (allIds.Length > options.MaxItems)
            log.LogWarning("Source returned {Count} ids; truncating to {Max}",
                allIds.Length, options.MaxItems);

        var ids = allIds.Take(options.MaxItems).ToArray();

        var results = new News?[ids.Length];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, ids.Length),
            new ParallelOptions { MaxDegreeOfParallelism = options.MaxParallelRequests, CancellationToken = ct },
            async (i, token) =>
            {
                try
                {
                    results[i] = (await http.GetFromJsonAsync<HnItem>(
                        string.Format(options.ItemUrlFormat, ids[i]), token))?.ToNews();
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException)
                {
                    log.LogWarning(ex, "Could not load item {Id}", ids[i]);
                }
            });

        var items = results
            .OfType<News>()
            .OrderByDescending(n => n.Score)
            .ToArray();

        if (items.Length == 0)
            throw new InvalidOperationException("No item could be loaded");

        if (items.Length < ids.Length)
            log.LogWarning("Loaded {Loaded} of {Total} items", items.Length, ids.Length);

        publisher.Publish(new DataSnapshot(items));

        log.LogInformation("Cache refreshed ({Count} items)", items.Length);
    }
}
