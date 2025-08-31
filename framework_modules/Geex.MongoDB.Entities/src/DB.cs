using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Core;
using MongoDB.Entities.Utilities;

namespace MongoDB.Entities
{
    /// <summary>
    /// Contains cache information for an entity type
    /// </summary>
    public class CacheInfo
    {
        public Type EntityType { get; set; }
        public IMongoDatabase Database { get; set; }
        public string DBName { get; set; }
        public string CollectionName { get; set; }
        public BsonClassMap ClassMap { get; set; }
        public BsonMemberMap[] MemberMaps { get; set; }
        public BsonArray Discriminators { get; set; }
        public Type RootEntityType { get; set; }
        public BsonClassMap RootClassMap { get; set; }
        public BsonMemberMap[] MemberMapsWithoutId { get; set; }
        public BsonMemberMap[] MemberMapsWithoutModifiedOn { get; set; }
        public BsonMemberMap[] MemberMapsWithoutSpecial { get; set; }
    }

    /// <summary>
    /// Contains cache information for a specific entity type
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class CacheInfo<T> : CacheInfo where T : IEntityBase
    {
        public IMongoCollection<T> Collection { get; set; }
        public ConcurrentDictionary<string, Watcher<T>> Watchers { get; set; }
    }

    /// <summary>
    /// The main entrypoint for all data access methods of the library
    /// </summary>
    public static partial class DB
    {
        public static readonly ConcurrentDictionary<Type, BsonClassMap> InheritanceCache = new ConcurrentDictionary<Type, BsonClassMap>();
        public static readonly ConcurrentDictionary<Type, BsonClassMap> InterfaceCache = new ConcurrentDictionary<Type, BsonClassMap>();
        public static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Type>> InheritanceTreeCache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Type>>();
        static DB()
        {

            BsonSerializer.RegisterSerializer(typeof(string), new ObjectIdCompatibleStringSerializer());
            BsonSerializer.RegisterSerializer(typeof(ObjectId), new StringCompatibleObjectIdSerializer());
            //BsonSerializer.RegisterSerializer(typeof(object), new AnonymousObjectBsonSerializer());
            BsonSerializer.RegisterSerializer(new JsonNodeSerializer());
            BsonSerializer.RegisterSerializer(new JsonValueSerializer());
            BsonSerializer.RegisterSerializer(new DateSerializer());
            BsonSerializer.RegisterSerializer(new StringValuesSerializer());
            BsonSerializer.RegisterSerializer(new FuzzyStringSerializer());
            BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
            BsonSerializer.RegisterSerializer(new DateTimeOffsetSupportingBsonDateTimeSerializer());
            BsonSerializer.RegisterSerializer(new LocalDateTimeSerializer());
            ConventionRegistry.Register(
                "DefaultConventions",
                new ConventionPack
                {
                    new IgnoreGetterConvention(),
                    new IgnoreExtraElementsConvention(true),
                    new EntityInheritanceConvention(),
                },
                _ => true);
            //ConventionRegistry.Register("Json", new ConventionPack
            //{
            //    new JsonNodeConvention()
            //}, type => type.IsAssignableTo<JsonNode>());
        }

        internal static event Action DefaultDbChanged;

        private static readonly ConcurrentDictionary<string, IMongoDatabase> dbs = new ConcurrentDictionary<string, IMongoDatabase>();
        public static IMongoDatabase DefaultDb { get; set; }

        /// <summary>
        /// Initializes a MongoDB connection with the given connection parameters.
        /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get duplicated.
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="host">Address of the MongoDB server</param>
        /// <param name="port">Port number of the server</param>
        public static Task InitAsync(string database, string host = "127.0.0.1", int port = 27017)
        {
            return Initialize(
                new MongoClientSettings
                {
                    Server = new MongoServerAddress(host, port),
                    LinqProvider = LinqProvider.V2
                }, database);
        }

        /// <summary>
        /// Initializes a MongoDB connection with the given connection parameters.
        /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get duplicated.
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="settings">A MongoClientSettings object</param>
        public static Task InitAsync(string database, MongoClientSettings settings)
        {
            return Initialize(settings, database);
        }

        private static async Task Initialize(MongoClientSettings settings, string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
                throw new ArgumentNullException(nameof(dbName), "Database name cannot be empty!");

            if (dbs.ContainsKey(dbName))
                return;

            try
            {
                var db = new MongoClient(settings).GetDatabase(dbName);

                if (dbs.Count == 0)
                    DefaultDb = db;

                if (dbs.TryAdd(dbName, db))
                    await db.RunCommandAsync((Command<BsonDocument>)"{ping:1}").ConfigureAwait(false);
            }
            catch (Exception)
            {
                dbs.TryRemove(dbName, out _);
                throw;
            }
        }

        /// <summary>
        /// Specifies the database that a given entity type should be stored in.
        /// Only needed for entity types you want stored in a db other than the default db.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="database">The name of the database</param>
        public static void DatabaseFor<T>(string database) where T : IEntityBase
            => TypeMap.AddDatabaseMapping(typeof(T), Database(database));

        /// <summary>
        /// Gets the IMongoDatabase for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public static IMongoDatabase Database<T>() where T : IEntityBase
        {
            return Cache<T>.Database;
        }

        /// <summary>
        /// Gets the IMongoDatabase for a given database name if it has been previously initialized.
        /// You can also get the default database by passing 'default' or 'null' for the name parameter.
        /// </summary>
        /// <param name="name">The name of the database to retrieve</param>
        public static IMongoDatabase Database(string name)
        {
            IMongoDatabase db = null;

            if (dbs.Count > 0)
            {
                if (string.IsNullOrEmpty(name))
                    db = DefaultDb;
                else
                    dbs.TryGetValue(name, out db);
            }

            if (db == null)
                throw new InvalidOperationException($"Database connection is not initialized for [{(string.IsNullOrEmpty(name) ? "Default" : name)}]");

            return db;
        }

        /// <summary>
        /// Gets the name of the database a given entity type is attached to. Returns name of default database if not specifically attached.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static string DatabaseName<T>() where T : IEntityBase
        {
            return Cache<T>.DBName;
        }

        /// <summary>
        /// Switches the default database at runtime
        /// <para>WARNING: Use at your own risk!!! Might result in entities getting saved in the wrong databases under high concurrency situations.</para>
        /// <para>TIP: Make sure to cancel any watchers (change-streams) before switching the default database.</para>
        /// </summary>
        /// <param name="name">The name of the database to mark as the new default database</param>
        public static void ChangeDefaultDatabase(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Database name cannot be null or empty");

            DefaultDb = Database(name);

            TypeMap.Clear();
            CacheInfoStorage.Clear();

            DefaultDbChanged?.Invoke();
        }

        /// <summary>
        /// Exposes the mongodb Filter Definition Builder for a given type.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static FilterDefinitionBuilder<T> Filter<T>() where T : IEntityBase
        {
            return Builders<T>.Filter;
        }

        /// <summary>
        /// Exposes the mongodb Sort Definition Builder for a given type.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static SortDefinitionBuilder<T> Sort<T>() where T : IEntityBase
        {
            return Builders<T>.Sort;
        }

        /// <summary>
        /// Exposes the mongodb Projection Definition Builder for a given type.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static ProjectionDefinitionBuilder<T> Projection<T>() where T : IEntityBase
        {
            return Builders<T>.Projection;
        }

        /// <summary>
        /// Returns a new instance of the supplied IEntity type
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static T Entity<T>() where T : IEntityBase, new()
        {
            return new T();
        }

        /// <summary>
        /// Returns a new instance of the supplied IEntity type with the Id set to the supplied value
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="id">The Id to set on the returned instance</param>
        public static T Entity<T>(string id) where T : IEntityBase, new()
        {
            return new T { Id = id };
        }

        public static Task Flush()
        {
            return DB.DefaultDb.ListCollectionNames().ForEachAsync(x =>
             {
                 if (x is not "system.profile" or "local" or "admin" or "config")
                 {
                     DB.DefaultDb.DropCollection(x);
                 }
             });
        }

        public static void StartProfiler()
        {
            var profileCommand = new BsonDocument("profile", 2);
            DB.DefaultDb.RunCommand<BsonDocument>(profileCommand);
        }
        public static void StopProfiler()
        {
            var profileCommand = new BsonDocument("profile", 0);
            DB.DefaultDb.RunCommand<BsonDocument>(profileCommand);
        }

        public static async Task RestartProfiler()
        {
            var profileCommand = new BsonDocument("profile", 0);
            DB.DefaultDb.RunCommand<BsonDocument>(profileCommand);
            var logCollection = DB.DefaultDb.GetCollection<BsonDocument>("system.profile");
            if (logCollection != default && await logCollection.EstimatedDocumentCountAsync() > 0)
            {
                await DB.DefaultDb.DropCollectionAsync(logCollection.CollectionNamespace.CollectionName);
            }
            profileCommand = new BsonDocument("profile", 2);
            DB.DefaultDb.RunCommand<BsonDocument>(profileCommand);
        }

        public static IMongoCollection<ProfilerLog> GetProfilerLogs()
        {
            return DB.DefaultDb.GetCollection<ProfilerLog>("system.profile");
        }

        private static ConcurrentDictionary<Type, CacheInfo> CacheInfoStorage = new ConcurrentDictionary<Type, CacheInfo>();

        /// <summary>
        /// Gets cache information for a given entity type, initializing it if necessary
        /// </summary>
        /// <param name="entityType">The entity type</param>
        /// <returns>Cache information including database, collection name, etc.</returns>
        public static CacheInfo GetCacheInfo(Type entityType)
        {
            // 尝试从缓存获取
            if (CacheInfoStorage.TryGetValue(entityType, out CacheInfo cachedInfo))
                return cachedInfo;

            if (!typeof(IEntityBase).IsAssignableFrom(entityType))
                throw new ArgumentException($"Type {entityType.Name} must implement IEntityBase");

            // 初始化缓存
            var cacheType = typeof(Cache<>).MakeGenericType(entityType);
            RuntimeHelpers.RunClassConstructor(cacheType.TypeHandle);

            // 使用反射获取Cache<T>的静态属性
            var databaseProperty = cacheType.GetProperty(nameof(Cache<>.Database), BindingFlags.NonPublic | BindingFlags.Static);
            var dbNameProperty = cacheType.GetProperty(nameof(Cache<>.DBName), BindingFlags.NonPublic | BindingFlags.Static);
            var collectionNameProperty = cacheType.GetProperty(nameof(Cache<>.CollectionName), BindingFlags.NonPublic | BindingFlags.Static);
            var classMapProperty = cacheType.GetProperty(nameof(Cache<>.ClassMap), BindingFlags.Public | BindingFlags.Static);
            var memberMapsProperty = cacheType.GetProperty(nameof(Cache<>.MemberMaps), BindingFlags.Public | BindingFlags.Static);
            var bsonDiscriminatorsProperty = cacheType.GetProperty(nameof(Cache<>.Discriminators), BindingFlags.Public | BindingFlags.Static);
            var rootEntityTypeProperty = cacheType.GetProperty(nameof(Cache<>.RootEntityType), BindingFlags.Public | BindingFlags.Static);
            var rootClassMapProperty = cacheType.GetProperty(nameof(Cache<>.RootClassMap), BindingFlags.Public | BindingFlags.Static);
            var memberMapsWithoutIdProperty = cacheType.GetProperty(nameof(Cache<>.MemberMapsWithoutId), BindingFlags.Public | BindingFlags.Static);
            var memberMapsWithoutModifiedOnProperty = cacheType.GetProperty(nameof(Cache<>.MemberMapsWithoutModifiedOn), BindingFlags.Public | BindingFlags.Static);
            var memberMapsWithoutSpecialProperty = cacheType.GetProperty(nameof(Cache<>.MemberMapsWithoutSpecial), BindingFlags.Public | BindingFlags.Static);

            var cacheInfo = new CacheInfo
            {
                EntityType = entityType,
                Database = (IMongoDatabase)databaseProperty?.GetValue(null),
                DBName = (string)dbNameProperty?.GetValue(null),
                CollectionName = (string)collectionNameProperty?.GetValue(null),
                ClassMap = (BsonClassMap)classMapProperty?.GetValue(null),
                MemberMaps = (BsonMemberMap[])memberMapsProperty?.GetValue(null),
                Discriminators = (BsonArray)bsonDiscriminatorsProperty?.GetValue(null),
                RootEntityType = (Type)rootEntityTypeProperty?.GetValue(null),
                RootClassMap = (BsonClassMap)rootClassMapProperty?.GetValue(null),
                MemberMapsWithoutId = (BsonMemberMap[])memberMapsWithoutIdProperty?.GetValue(null),
                MemberMapsWithoutModifiedOn = (BsonMemberMap[])memberMapsWithoutModifiedOnProperty?.GetValue(null),
                MemberMapsWithoutSpecial = (BsonMemberMap[])memberMapsWithoutSpecialProperty?.GetValue(null)
            };

            // 缓存结果
            CacheInfoStorage.TryAdd(entityType, cacheInfo);
            return cacheInfo;
        }
    }

    internal static class TypeMap
    {
        private static readonly ConcurrentDictionary<Type, IMongoDatabase> TypeToDBMap = new ConcurrentDictionary<Type, IMongoDatabase>();
        private static readonly ConcurrentDictionary<Type, string> TypeToCollMap = new ConcurrentDictionary<Type, string>();

        internal static void AddCollectionMapping(Type entityType, string collectionName)
            => TypeToCollMap[entityType] = collectionName;

        internal static string GetCollectionName(Type entityType)
        {
            TypeToCollMap.TryGetValue(entityType, out string name);
            return name;
        }

        internal static void AddDatabaseMapping(Type entityType, IMongoDatabase database)
            => TypeToDBMap[entityType] = database;

        internal static void Clear()
        {
            TypeToDBMap.Clear();
            TypeToCollMap.Clear();
        }

        internal static IMongoDatabase GetDatabase(Type entityType)
        {
            TypeToDBMap.TryGetValue(entityType, out IMongoDatabase db);
            return db ?? DB.Database(default);
        }
    }

    internal static class Cache<T> where T : IEntityBase
    {
        internal static IMongoDatabase Database { get; private set; }
        internal static IMongoCollection<T> Collection { get; private set; }
        internal static string DBName { get; private set; }
        internal static string CollectionName { get; private set; }
        internal static ConcurrentDictionary<string, Watcher<T>> Watchers { get; private set; }

        public static BsonClassMap ClassMap { get; private set; }
        public static BsonMemberMap[] MemberMaps { get; private set; }
        public static BsonArray Discriminators { get; private set; }
        public static Type RootEntityType { get; private set; }
        public static BsonClassMap RootClassMap { get; private set; }
        public static BsonMemberMap[] MemberMapsWithoutId { get; private set; }
        public static BsonMemberMap[] MemberMapsWithoutModifiedOn { get; private set; }
        public static BsonMemberMap[] MemberMapsWithoutSpecial { get; private set; }

        static Cache()
        {
            Initialize();
            DB.DefaultDbChanged += Initialize;
        }

        private static void Initialize()
        {
            var type = typeof(T);
            ClassMap = BsonClassMap.LookupClassMap(type);
            ClassMap.MapInheritance();
            RootClassMap = type.GetRootBsonClassMap();
            RootEntityType = RootClassMap.ClassType;
            Database = TypeMap.GetDatabase(type);
            DBName = Database.DatabaseNamespace.DatabaseName;

            var collAttrb = type.GetCustomAttribute<NameAttribute>(false);
            CollectionName = collAttrb != null ? collAttrb.Name : RootEntityType.Name;

            if (string.IsNullOrWhiteSpace(CollectionName) || CollectionName.Contains("~"))
                throw new ArgumentException($"{CollectionName} is an illegal name for a collection!");

            Collection = Database.GetCollection<T>(CollectionName);
            TypeMap.AddCollectionMapping(type, CollectionName);

            Watchers = new ConcurrentDictionary<string, Watcher<T>>();

            // Initialize BsonMemberMaps
            MemberMaps = GetBsonMemberMaps();
            MemberMapsWithoutId = MemberMaps.Where(x => x.MemberName != nameof(IEntityBase.Id)).ToArray();
            MemberMapsWithoutSpecial = MemberMaps.Where(x => x.MemberName != nameof(IEntityBase.Id) && x.MemberName != nameof(IEntityBase.ModifiedOn)).ToArray();
            MemberMapsWithoutModifiedOn = MemberMaps.Where(x => x.MemberName != nameof(IEntityBase.Id)).ToArray();
            Discriminators = type.GetBsonDiscriminators();
        }

        /// <summary>
        /// Gets BsonMemberMaps for the current entity type
        /// </summary>
        public static BsonMemberMap[] GetBsonMemberMaps()
        {
            try
            {
                var classMap = BsonClassMap.LookupClassMap(typeof(T));
                return classMap.AllMemberMaps.Where(x => x.MemberName != nameof(IEntityBase.ModifiedOn)).ToArray();
            }
            catch
            {
                var classMap = new BsonClassMap(typeof(T));
                classMap.SetDiscriminatorIsRequired(true);
                classMap.AutoMap();
                return classMap.AllMemberMaps.Where(x => x.MemberName != nameof(IEntityBase.ModifiedOn)).ToArray();
            }
        }

        /// <summary>
        /// Gets member value from entity using BsonMemberMap
        /// </summary>
        private static object GetMemberValue(object obj, BsonMemberMap memberMap)
        {
            if (memberMap.MemberInfo is PropertyInfo property)
            {
                return property.GetValue(obj);
            }
            else if (memberMap.MemberInfo is FieldInfo field)
            {
                return field.GetValue(obj);
            }
            return null;
        }
    }

    internal class EntityInheritanceConvention : ConventionBase, IClassMapConvention
    {
        /// <inheritdoc />
        public void Apply(BsonClassMap classMap)
        {
            classMap.SetDiscriminatorIsRequired(true);
            classMap.MapInheritance();
        }
    }

    internal class IgnoreGetterConvention : ConventionBase, IMemberMapConvention
    {
        /// <inheritdoc />
        public void Apply(BsonMemberMap memberMap)
        {
            if (memberMap.MemberInfo is PropertyInfo { CanWrite: true } propertyInfo && !propertyInfo.GetMethod.IsSpecialName)
            {
                memberMap.SetShouldSerializeMethod((o => false));
            }
        }
    }

    //internal class JsonNodeConvention : ConventionBase, IMemberMapConvention, IClassMapConvention, IDiscriminatorConvention
    //{
    //    public void Apply(BsonMemberMap mMap)
    //    {
    //        if (mMap.MemberName is nameof(JsonNode.Parent) or nameof(JsonNode.Root) or nameof(JsonNode.Options))
    //        {
    //            _ = mMap.SetShouldSerializeMethod(_ => false);
    //        }
    //    }

    //    /// <inheritdoc />
    //    public void Apply(BsonClassMap classMap)
    //    {
    //        classMap.SetIsRootClass(true);
    //    }

    //    /// <inheritdoc />
    //    public Type GetActualType(IBsonReader bsonReader, Type nominalType)
    //    {
    //        return nominalType;
    //    }

    //    /// <inheritdoc />
    //    public BsonValue GetDiscriminator(Type nominalType, Type actualType)
    //    {
    //        return null;
    //    }

    //    /// <inheritdoc />
    //    public string ElementName { get; } = null;
    //}
}
