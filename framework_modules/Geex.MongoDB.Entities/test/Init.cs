using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public static class InitTest
    {
        [AssemblyInitialize]
        public static async Task Init(TestContext _)
        {
            //DB.InitAsync("mongodb-entities-test", MongoClientSettings.FromConnectionString("mongodb://localhost:27017/?replicaSet=rs0"));
            await DB.InitAsync("mongodb-entities-test");
            //await DB.Flush();
        }
    }
}