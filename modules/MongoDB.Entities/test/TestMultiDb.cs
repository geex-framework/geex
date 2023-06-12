using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Entities.Tests.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class MultiDb
    {
        private const string dbName = "mongodb-entities-test-multi";

        [TestMethod]
        public async Task save_entity_works()
        {

            await DB.InitAsync(dbName);

            DB.DatabaseFor<BookCover>(dbName);
            DB.DatabaseFor<BookMark>(dbName);

            var cover = new BookCover
            {
                BookId = ObjectId.GenerateNewId(),
                BookName = "test book " + Guid.NewGuid().ToString()
            };

            await cover.SaveAsync();
            Assert.IsNotNull(cover.Id);

            var res = await DB.Find<BookCover>().OneAsync(cover.Id);

            Assert.AreEqual(cover.Id, res.Id);
            Assert.AreEqual(cover.BookName, res.BookName);
        }

        [TestMethod]
        public async Task get_instance_by_db_name()
        {
            await DB.InitAsync("test1");
            await DB.InitAsync("test2");

            var res = DB.Database("test2");

            Assert.AreEqual("test2", res.DatabaseNamespace.DatabaseName);
        }

        [TestMethod]
        public void uninitialized_get_instance_throws()
        {
            Assert.ThrowsException<InvalidOperationException>(() => DB.Database("some-database"));
        }

        [TestMethod]
        public async Task multiple_initializations_should_not_throw()
        {
            await DB.InitAsync("multi-init");
            await DB.InitAsync("multi-init");

            var db = DB.Database("multi-init");

            Assert.AreEqual("multi-init", db.DatabaseNamespace.DatabaseName);
        }
    }
}
