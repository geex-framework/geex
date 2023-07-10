using Geex.Common.Abstraction;
using Geex.Common.Abstraction.MultiTenant;

using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities;

using StackExchange.Redis;

namespace Geex.Common.Authorization.Casbin
{
    public class CasbinRule : EntityBase<CasbinRule>
    {
        public string PType { get; set; }
        public string V0 { get; set; }
        public string V1 { get; set; }
        public string V2 { get; set; }
        public string V3 { get; set; }
        public string V4 { get; set; }
        public string V5 { get; set; }
    }
}