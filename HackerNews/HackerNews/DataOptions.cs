namespace HackerNewsRetriever;

public sealed class DataOptions
{
    public const string Section = "Data";

    public string BestStoriesUrl { get; init; } = "";
    public string ItemUrlFormat { get; init; } = "";      // {0} = story id
    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan MaxAge { get; init; } = TimeSpan.FromMinutes(15);   // Readers get null after this.
    public TimeSpan HttpTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxParallelRequests { get; init; } = 10;
    public int MaxItems { get; init; } = 500;             // Max cached and max allowed per request.
    public int DefaultCount { get; init; } = 50;          // Used when ?count= is omitted.
    public int RetryAfterSeconds { get; init; } = 30;     // Sent with the 503.

    // Fails at startup if the configuration is unusable.
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BestStoriesUrl))
            throw new InvalidOperationException("Data:BestStoriesUrl is required");
        if (!ItemUrlFormat.Contains("{0}"))
            throw new InvalidOperationException("Data:ItemUrlFormat must contain {0}");
        if (RefreshInterval <= TimeSpan.Zero)
            throw new InvalidOperationException("Data:RefreshInterval must be positive");
        if (MaxAge <= RefreshInterval)
            throw new InvalidOperationException("Data:MaxAge must be greater than RefreshInterval");
        if (MaxParallelRequests < 1)
            throw new InvalidOperationException("Data:MaxParallelRequests must be at least 1");
        if (MaxItems < 1)
            throw new InvalidOperationException("Data:MaxItems must be at least 1");
        if (DefaultCount < 1 || DefaultCount > MaxItems)
            throw new InvalidOperationException("Data:DefaultCount must be between 1 and MaxItems");
    }
}
