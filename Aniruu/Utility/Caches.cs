using Microsoft.Extensions.Caching.Memory;

namespace Aniruu.Utility;

public sealed class Caches
{
    public readonly IMemoryCache NewUserCache = new MemoryCache(new MemoryCacheOptions());
    public readonly IMemoryCache PostPageCountCache = new MemoryCache(new MemoryCacheOptions());
    public readonly IMemoryCache TagCountCache = new MemoryCache(new MemoryCacheOptions());
}
