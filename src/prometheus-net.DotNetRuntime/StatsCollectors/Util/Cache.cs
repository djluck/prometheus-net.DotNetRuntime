using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prometheus.DotNetRuntime.StatsCollectors.Util
{
    /// <summary>
    /// A strongly-typed cache that periodically evicts items.
    /// </summary>
    public sealed class Cache<TKey, TValue> : IDisposable
    {
        private readonly ConcurrentDictionary<TKey, CacheValue<TValue>> _cache;
        private readonly TimeSpan _expireItemsAfter;
        private readonly Task _cleanupTask;
        private readonly CancellationTokenSource _cancellationSource;
        
        internal Cache(TimeSpan expireItemsAfter, int initialCapacity = 32)
        {
            _expireItemsAfter = expireItemsAfter;
            if (expireItemsAfter == TimeSpan.Zero)
                throw new ArgumentNullException(nameof(expireItemsAfter));
            
            _cache = new ConcurrentDictionary<TKey, CacheValue<TValue>>(Environment.ProcessorCount, initialCapacity);
            _cancellationSource = new CancellationTokenSource();
            
            _cleanupTask = Task.Run(async () =>
            {
                while (!_cancellationSource.IsCancellationRequested)
                {
                    await Task.Delay(expireItemsAfter);
                    CleanupExpiredValues();
                }
            });
        }
        
        internal void Set(TKey key, TValue value, DateTime? timeStamp = null)
        {
            var cacheValue = new CacheValue<TValue>(value, timeStamp);
            if (_cache.TryAdd(key, cacheValue))
                return;

            // This is a very unthorough attempt to add a value to the cache if it already eixsts.
            // However, this cache is very unlikely to have to store keys that regularly clash (as most keys are event or process ids) 
            _cache.TryRemove(key, out var unused);
            _cache.TryAdd(key, cacheValue);
        }
        
        internal bool TryGetValue(TKey key, out TValue value, out DateTime timeStamp)
        {
            CacheValue<TValue> cacheValue;
            if (_cache.TryGetValue(key, out cacheValue))
            {
                value = cacheValue.Value;
                timeStamp = cacheValue.TimeStamp;
                return true;
            }
            
            value = default(TValue);
            timeStamp = default(DateTime);
            return false;
        }
        
        internal bool TryRemove(TKey key, out TValue value, out DateTime timeStamp)
        {
            CacheValue<TValue> cacheValue;
            if (_cache.TryRemove(key, out cacheValue))
            {
                value = cacheValue.Value;
                timeStamp = cacheValue.TimeStamp;
                return true;
            }
            
            value = default(TValue);
            timeStamp = default(DateTime);
            return false;
        }
        
        internal struct CacheValue<T>
        {
            public CacheValue(T value, DateTime? timeStamp)
            {
                Value = value;
                TimeStamp = timeStamp ?? DateTime.UtcNow;
            }
            
            public DateTime TimeStamp { get; }
            public T Value { get; }
        }

        public void Dispose()
        {
            _cancellationSource.Cancel();
        }
        
        private void CleanupExpiredValues()
        {
            var earliestAddedTime = DateTime.UtcNow.Subtract(_expireItemsAfter);
            
            foreach (var key in _cache.Keys.ToArray())
            {
                CacheValue<TValue> value;
                if (!_cache.TryGetValue(key, out value))
                    continue;
                
                if (value.TimeStamp < earliestAddedTime)
                    _cache.TryRemove(key, out var _);
            }
        }
    }
}