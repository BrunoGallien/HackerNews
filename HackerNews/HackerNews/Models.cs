namespace HackerNewsRetriever;

public sealed record News(
    string? Title,
    string? Uri,
    string? PostedBy,
    int Score,
    long Time,          
    int CommentCount);

public sealed record DataSnapshot(IReadOnlyList<News> Items);
