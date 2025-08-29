using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Entities.Utilities;

// ReSharper disable once CheckNamespace
namespace Geex
{
    public interface IEnumeration : IStringPresentation
    {
        public static Dictionary<string, IEnumeration> ValueCacheDictionary = new Dictionary<string, IEnumeration>();
        public string Name { get; }
        public string Value { get; }
    }
}
