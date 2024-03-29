﻿using System;
using System.Collections.Concurrent;
using MongoDB.Bson.Serialization;

namespace MongoDB.Entities.Utilities
{
    public class DbContextCache
    {
        public ConcurrentDictionary<Type, ConcurrentDictionary<string, IEntityBase>> TypedCacheDictionary { get; set; } = new();
        public ConcurrentDictionary<string, IEntityBase> this[Type index]
        {
            get
            {
                if (!TypedCacheDictionary.ContainsKey(index.GetRootBsonClassMap().ClassType))
                {
                    TypedCacheDictionary[index.GetRootBsonClassMap().ClassType] = new ConcurrentDictionary<string, IEntityBase>();
                }
                return TypedCacheDictionary[index.GetRootBsonClassMap().ClassType];
            }
        }
    }
}
