namespace HackerNewsRetriever;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        
        // Loading "Data" section from appsettings.json.
        var options = builder.Configuration.GetSection(DataOptions.Section).Get<DataOptions>()
                      ?? new DataOptions();
        options.Validate();
        builder.Services.AddSingleton(options);

        builder.Services.AddMemoryCache();

        // Adding one unique DataCache instance and its required services.
        builder.Services.AddSingleton<DataCache>();
        builder.Services.AddSingleton<IDataProvider>(sp => sp.GetRequiredService<DataCache>());
        builder.Services.AddSingleton<IDataPublisher>(sp => sp.GetRequiredService<DataCache>());

        builder.Services.AddHttpClient(nameof(DataRefreshWorker), c => c.Timeout = options.HttpTimeout);

        // Worker that refreshes the data.
        builder.Services.AddHostedService<DataRefreshWorker>();
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        // GET /api/data?count=20
        app.MapGet("/api/data", (IDataProvider data, DataOptions opt, HttpResponse response, int? count) =>
            {
                var requested = count ?? opt.DefaultCount;

                if (requested < 1 || requested > opt.MaxItems)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["count"] = [$"count must be between 1 and {opt.MaxItems}"]
                    });
                }

                var snapshot = data.Current;   
                if (snapshot is not null) return Results.Ok(snapshot.Items.Take(requested).ToArray());
            
                // First load not finished yet, or data expired after repeated failures.
                response.Headers.RetryAfter = opt.RetryAfterSeconds.ToString();
                return Results.Problem(
                    title: "Data not available yet",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .WithName("GetHackerNews");

        app.Run();
    }
}