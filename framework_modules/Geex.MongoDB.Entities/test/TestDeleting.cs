using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Bson;
using MongoDB.Entities.Tests.Models;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Deleting
    {
        [TestMethod]
        public async Task delete_by_id_removes_entity_from_collectionAsync()
        {
            var author1 = new Author { Name = "auth1" };
            var author2 = new Author { Name = "auth2" };
            var author3 = new Author { Name = "auth3" };
            var dbContext = new DbContext();
            await new[] { author1, author2, author3 }.SaveAsync(dbContext);

            await author2.DeleteAsync();

            var a1 = DB.Queryable<Author>()
                .SingleOrDefault(a => a.Id == author1.Id);

            var a2 = DB.Queryable<Author>()
                .SingleOrDefault(a => a.Id == author2.Id);

            Assert.AreEqual(null, a2);
            Assert.AreEqual(author1.Name, a1.Name);
        }

        [TestMethod]
        public async Task cascade_delete_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteTypedAsync<BatchLoadEntity>();
            dbContext.Attach(new BatchLoadEntity(thisId: "0"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.1", parentId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "1.2", parentId: "1"));
            dbContext.Attach(new BatchLoadEntity(thisId: "2", parentId: "0"));
            var saved = await dbContext.SaveChanges();
            saved.InsertedCount.ShouldBe(5);

            dbContext = new DbContext();
            var count = dbContext.Query<BatchLoadEntity>().Count();
            count.ShouldBe(5);
            var item1 = dbContext.Query<BatchLoadEntity>().FirstOrDefault(x => x.ThisId == "1");
            var deleteResult = await item1.DeleteAsync();
            deleteResult.ShouldBe(3);
            saved = await dbContext.SaveChanges();
            saved.ModifiedCount.ShouldBe(0);

            dbContext = new DbContext();
            count = dbContext.Query<BatchLoadEntity>().Count();
            count.ShouldBe(2);
        }

        [TestMethod]
        public async Task deleting_entity_removes_all_refs_to_itselfAsync()
        {
            await DB.DeleteTypedAsync<Author>();
            await DB.DeleteTypedAsync<Book>();
            var dbContext = new DbContext();

            var author = new Author { Name = "author" };
            var book1 = new Book { Title = "derarti1" };
            var book2 = new Book { Title = "derarti2" };
            dbContext.Attach(author);
            dbContext.Attach(book1);
            dbContext.Attach(book2);
            await book1.SaveAsync();
            await book2.SaveAsync();
            await author.SaveAsync();

            author.BookIds.Add(book1.Id);
            author.BookIds.Add(book2.Id);

            book1.MainAuthorId = author.Id;
            book2.MainAuthorId = author.Id;

            await author.DeleteAsync();
            Assert.AreEqual(null, DB.Queryable<Author>().SingleOrDefault(a => a.Id == author.Id));
            Assert.AreEqual(null, DB.Queryable<Book>().SingleOrDefault(b => b.Id == book1.Id));
            Assert.AreEqual(null, DB.Queryable<Book>().SingleOrDefault(b => b.Id == book2.Id));
        }

        [TestMethod]
        public async Task deleteall_removes_entity_and_refs_to_itselfAsync()
        {
            var dbContext = new DbContext();
            var book = new Book { Title = "Test" };
            dbContext.Attach(book);
            await book.SaveAsync();
            var author1 = new Author { Name = "ewtrcd1" };
            dbContext.Attach(author1);
            await author1.SaveAsync();
            var author2 = new Author { Name = "ewtrcd2" };
            dbContext.Attach(author2);
            await author2.SaveAsync();
            book.GoodAuthorIds.Add(author1.Id);
            book.OtherAuthors = (new Author[] { author1, author2 });
            await book.SaveAsync();
            await book.OtherAuthors.DeleteAsync();
            Assert.AreEqual(0, book.GoodAuthors.Count());
            Assert.AreEqual(null, DB.Queryable<Author>().SingleOrDefault(a => a.Id == author1.Id));
        }

        [TestMethod]
        public async Task delete_by_expression_deletes_all_matchesAsync()
        {
            var dbContext = new DbContext();
            var author1 = new Author { Name = "xxx" };
            dbContext.Attach(author1);
            await author1.SaveAsync();
            var author2 = new Author { Name = "xxx" };
            dbContext.Attach(author2);
            await author2.SaveAsync();
            var toBeDeleted = dbContext.Query<Author>().Where(x => x.Name == "xxx");
            await dbContext.DeleteAsync<Author>(toBeDeleted);

            var count = DB.Queryable<Author>()
                          .Count(a => a.Name == "xxx");

            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task high_volume_deletes_with_idsAsync()
        {
            var Ids = new List<string>(100100);

            for (int i = 0; i < 100100; i++)
            {
                Ids.Add(ObjectId.GenerateNewId().ToString());
            }

            await DB.DeleteAsync<Blank>(Ids);
        }

        [TestCategory("SkipWhenLiveUnitTesting")]
        [TestMethod]
        public async Task high_volume_deletes_with_expressionAsync()
        {
            //start with clean collection
            await DB.DropCollectionAsync<Blank>();

            var list = new List<Blank>(100100);
            for (int i = 0; i < 100100; i++)
            {
                list.Add(new Blank());
            }
            await list.SaveAsync();

            Assert.AreEqual(100100, DB.Queryable<Blank>().Count());

            await DB.DeleteAsync<Blank>(_ => true);

            Assert.AreEqual(0, await DB.CountAsync<Blank>());

            //reclaim disk space
            await DB.DropCollectionAsync<Blank>();
            await DB.SaveAsync(new Blank());
        }
    }
}
