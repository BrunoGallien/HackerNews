# README

Runs with: http://localhost:5240/api/data?count={num} or http://localhost:5240/api/data

I went for the simplest version of the code.

## 1. If I had more time I would:

- Make it SOLID.
- Make it CLEAN.
- Add a health endpoint.
- Better handle returning more results than expected in `DataRefreshWorker.cs` line 65.

## 2. Architecture

According to the API description, there are at most 500 best stories. That drove the architecture of the project.

A worker refreshes a cache (`DataCache`) every `RefreshInterval` by loading all the best stories. The API accesses the data only through the cache, so the HackerNews endpoint only receives load when the cache is being refreshed. Thanks to `IMemoryCache`, the cache can be hit by multiple readers and updated without any user receiving partial data.

## 3. If best stories were unbounded

If there were no upper boundary on best stories, the architecture would be totally different. I would still use an `IMemoryCache`, but:

- There would be no worker.
- The cache would be updated whenever a request asks for more items than the cache already holds. If the requested number is smaller than or equal to what is in the cache, the data is served from the cache.
- Every `RefreshInterval`, the maximum amount of data already loaded in the cache would be reloaded from HackerNews.

Managing the cache this way is trickier, as multiple requests asking for larger and larger numbers of items could arrive at the same time. A job queue could be the solution.
