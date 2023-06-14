using System;
using System.Collections.Concurrent;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities
{
    public class FilterDisabledContext : IDisposable
    {
        private readonly DbContext _dbContext;
        private readonly ConcurrentDictionary<Type, IDataFilter> _disabledFilters;

        public FilterDisabledContext(DbContext dbContext, ConcurrentDictionary<Type, IDataFilter> disabledFilters)
        {
            _dbContext = dbContext;
            _disabledFilters = disabledFilters;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var dataFilter in _disabledFilters)
            {
                _dbContext.DataFilters.TryAdd(dataFilter.Key, dataFilter.Value);
                _disabledFilters.TryRemove(dataFilter.Key, out _);
            }
        }
    }
}