using Microsoft.Extensions.Caching.Memory;

namespace ReverseProxyApplication
{
    public class LocalMemoryCache
    {
        public MemoryCache Cache { get; private set; }
        public LocalMemoryCache()
        {
            Cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1024
            });
        }
    }
}
