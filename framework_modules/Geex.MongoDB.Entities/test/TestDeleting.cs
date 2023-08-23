using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Bson;
using MongoDB.Driver.Linq;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            await new[] { author1, author2, author3 }.ToList().SaveAsync();

            await author2.DeleteAsync();

            var a1 = DB.Queryable<Author>()
                .SingleOrDefault(a => a.Id == author1.Id);

            var a2 = DB.Queryable<Author>()
                .SingleOrDefault(a => a.Id == author2.Id);

            Assert.AreEqual(null, a2);
            Assert.AreEqual(author1.Name, a1.Name);
        }

        [TestMethod]
        public async Task deleting_entity_removes_all_refs_to_itselfAsync()
        {
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

            book1.GoodAuthorIds.Add(author.Id);
            book2.GoodAuthorIds.Add(author.Id);

            await author.DeleteAsync();
            Assert.AreEqual(0, book2.GoodAuthors.Count());

            await book1.DeleteAsync();
            Assert.AreEqual(0, author.Books.Count());
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
        public async Task deleting_a_one2many_ref_entity_makes_parent_nullAsync()
        {
            var dbContext = new DbContext();
            var book = new Book { Title = "Test" };
            dbContext.Attach(book);
            await book.SaveAsync();
            var author = new Author { Name = "ewtrcd1" };
            dbContext.Attach(author);
            await author.SaveAsync();
            book.MainAuthorId = author.Id;
            await book.SaveAsync();
            await author.DeleteAsync();
            Assert.AreEqual(null, book.MainAuthor.Value);
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

            await dbContext.DeleteAsync<Author>(x => x.Name == "xxx");

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
