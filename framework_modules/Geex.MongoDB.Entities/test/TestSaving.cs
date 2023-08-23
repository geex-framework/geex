using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Saving
    {
        [TestMethod]
        public async Task saving_new_document_returns_an_id()
        {
            var book = new Book { Title = "Test" };
            await book.SaveAsync();
            var idEmpty = book.Id == default;
            Assert.IsFalse(idEmpty);
        }

        [TestMethod]
        public async Task saved_book_has_correct_title()
        {
            var book = new Book { Title = "Test" };
            await book.SaveAsync();
            var title = DB.Queryable<Book>().Where(b => b.Id == book.Id).Select(b => b.Title).SingleOrDefault();
            Assert.AreEqual("Test", title);
        }

        [TestMethod]
        public async Task created_on_property_works()
        {
            var author = new Author { Name = "test" };
            await author.SaveAsync();
            var res = (DB.Queryable<Author>()
                        .Where(a => a.Id == author.Id)
                        .Select(a => a.CreatedOn)
                        .Single());

            Assert.AreEqual(res.DateTime.ToLongTimeString(), author.CreatedOn.DateTime.ToLongTimeString());
            Assert.IsTrue(DateTime.UtcNow.Subtract(res.DateTime).TotalSeconds <= 5);
        }

        [TestMethod]
        public async Task save_partially_single_include()
        {
            var book = new Book { Title = "test book", Price = 100 };

            await book.SaveOnlyAsync(b => new { b.Title });

            var res = await DB.Find<Book>().MatchId(book.Id).ExecuteSingleAsync();

            Assert.AreEqual(0, res.Price);
            Assert.AreEqual("test book", res.Title);

            res.Price = 200;

            await res.SaveOnlyAsync(b => new { b.Price });

            res = await DB.Find<Book>().MatchId(res.Id).ExecuteSingleAsync();

            Assert.AreEqual(200, res.Price);
        }

        [TestMethod]
        public async Task save_partially_batch_include()
        {
            var books = new[] {
                new Book{ Title = "one", Price = 100},
                new Book{ Title = "two", Price = 200}
            };

            await books.SaveOnlyAsync(b => new { b.Title });
            var ids = books.Select(b => b.Id).ToArray();

            var res = await DB.Find<Book>()
                .Match(b => ids.Contains(b.Id))
                .Sort(b => b.Id, FindSortType.Ascending)
                .ExecuteAsync();

            Assert.AreEqual(0, res[0].Price);
            Assert.AreEqual(0, res[1].Price);
            Assert.AreEqual("one", res[0].Title);
            Assert.AreEqual("two", res[1].Title);
        }

        [TestMethod]
        public async Task save_partially_single_exclude()
        {
            var book = new Book { Title = "test book", Price = 100 };

            await book.SaveExceptAsync(b => new { b.Title });

            var res = await DB.Find<Book>().MatchId(book.Id).ExecuteSingleAsync();

            Assert.AreEqual(100, res.Price);
            Assert.AreEqual(null, res.Title);

            res.Title = "updated";

            await res.SaveExceptAsync(b => new { b.Price });

            res = await DB.Find<Book>().MatchId(res.Id).ExecuteSingleAsync();

            Assert.AreEqual("updated", res.Title);
        }

        [TestMethod]
        public async Task save_partially_batch_exclude()
        {
            var books = new[] {
                new Book{ Title = "one", Price = 100},
                new Book{ Title = "two", Price = 200}
            };

            await books.SaveExceptAsync(b => new { b.Title });
            var ids = books.Select(b => b.Id).ToArray();

            var res = await DB.Find<Book>()
                .Match(b => ids.Contains(b.Id))
                .Sort(b => b.Id, FindSortType.Ascending)
                .ExecuteAsync();

            Assert.AreEqual(100, res[0].Price);
            Assert.AreEqual(200, res[1].Price);
            Assert.AreEqual(null, res[0].Title);
            Assert.AreEqual(null, res[1].Title);
        }

        [TestMethod]
        public async Task save_preserving_upsert()
        {
            var book = new Book { Title = "Original Title", Price = 123.45m, DontSaveThis = 111 };

            book.Title = "updated title";
            book.Price = 543.21m;

            await book.SavePreservingAsync();

            book = await DB.Find<Book>().OneAsync(book.Id);

            Assert.AreEqual("updated title", book.Title);
            Assert.AreEqual(543.21m, book.Price);
            Assert.AreEqual(default, book.DontSaveThis);
        }

        [TestMethod]
        public async Task save_preserving()
        {
            var book = new Book { Title = "Original Title", Price = 123.45m, DontSaveThis = 111 };
            await book.SaveAsync();

            book.Title = "updated title";
            book.Price = 543.21m;

            await book.SavePreservingAsync();

            book = await DB.Find<Book>().OneAsync(book.Id);

            Assert.AreEqual("updated title", book.Title);
            Assert.AreEqual(543.21m, book.Price);
            Assert.AreEqual(default, book.DontSaveThis);
        }

        [TestMethod]
        public async Task save_preserving_inverse_attribute()
        {
            var book = new Book
            {
                Title = "original", //dontpreserve
                Price = 100, //dontpreserve
                PriceDbl = 666,
                //MainAuthor = ObjectId.GenerateNewId().ToString()
            };
            await book.SaveAsync();

            book.Title = "updated";
            book.Price = 111;
            book.PriceDbl = 999;
            //book.MainAuthor = null;

            await book.SavePreservingAsync();

            var res = await DB.Find<Book>().OneAsync(book.Id);

            Assert.AreEqual(res.Title, book.Title);
            Assert.AreEqual(res.Price, book.Price);
            Assert.AreEqual(res.PriceDbl, 666);
            //Assert.IsFalse(res.MainAuthor.Id == null);
        }

        [TestMethod]
        public async Task save_preserving_attribute()
        {
            var author = new Author
            {
                Age = 123,
                Name = "initial name",
                FullName = "initial fullname",
                Birthday = DateTime.UtcNow
            };
            await author.SaveAsync();

            author.Name = "updated author name";
            author.Age = 666; //preserve
            author.Age2 = 400; //preserve
            author.Birthday = DateTime.MinValue; //preserve
            author.FullName = null;
            //author.BestSeller = ObjectId.GenerateNewId().ToString();

            await author.SavePreservingAsync();

            var res = await DB.Find<Author>().OneAsync(author.Id);

            Assert.AreEqual("updated author name", res.Name);
            Assert.AreEqual(123, res.Age);
            Assert.AreEqual(default, res.Age2);
            Assert.AreNotEqual(DateTime.MinValue, res.Birthday);
            Assert.AreEqual("initial fullname", res.FullName);
            //Assert.AreEqual(author.BestSeller.Id, res.BestSeller.Id);
        }

        [TestMethod]
        public async Task embedding_non_entity_returns_correct_document()
        {
            var book = new Book { Title = "Test" };
            book.Review = new Review { Stars = 5, Reviewer = "enercd" };
            await book.SaveAsync();
            var res = DB.Queryable<Book>()
                          .Where(b => b.Id == book.Id)
                          .Select(b => b.Review.Reviewer)
                          .SingleOrDefault();
            Assert.AreEqual(book.Review.Reviewer, res);
        }

        [TestMethod]
        public async Task embedding_with_ToDocument_returns_correct_doc()
        {
            var book = new Book { Title = "Test" };
            var author = new Author { Name = "ewtdrcd" };
            book.RelatedAuthor = author.ToDocument();
            await book.SaveAsync();
            var res = DB.Queryable<Book>()
                          .Where(b => b.Id == book.Id)
                          .Select(b => b.RelatedAuthor.Name)
                          .SingleOrDefault();
            Assert.AreEqual(book.RelatedAuthor.Name, res);
        }

        [TestMethod]
        public async Task embedding_with_ToDocument_returns_blank_id()
        {
            var book = new Book { Title = "Test" };
            var author = new Author { Name = "Test Author" };
            book.RelatedAuthor = author.ToDocument();
            await book.SaveAsync();
            var res = DB.Queryable<Book>()
                          .Where(b => b.Id == book.Id)
                          .Select(b => b.RelatedAuthor.Id)
                          .SingleOrDefault();
            Assert.AreEqual(book.RelatedAuthor.Id, res);
        }

        [TestMethod]
        public async Task embedding_with_ToDocuments_Arr_returns_correct_docs()
        {
            var book = new Book { Title = "Test" }; await book.SaveAsync();
            var author1 = new Author { Name = "ewtrcd1" }; await author1.SaveAsync();
            var author2 = new Author { Name = "ewtrcd2" }; await author2.SaveAsync();
            book.OtherAuthors = (new Author[] { author1, author2 }).ToDocuments();
            await book.SaveAsync();
            var authors = DB.Queryable<Book>()
                              .Where(b => b.Id == book.Id)
                              .Select(b => b.OtherAuthors).Single();
            Assert.AreEqual(authors.Length, 2);
            Assert.AreEqual(author2.Name, authors[1].Name);
            Assert.AreEqual(book.OtherAuthors[0].Id, authors[0].Id);
        }

        [TestMethod]
        public async Task embedding_with_ToDocuments_IEnumerable_returns_correct_docs()
        {
            var book = new Book { Title = "Test" }; await book.SaveAsync();
            var author1 = new Author { Name = "ewtrcd1" }; await author1.SaveAsync();
            var author2 = new Author { Name = "ewtrcd2" }; await author2.SaveAsync();
            var list = new List<Author>() { author1, author2 };
            book.OtherAuthors = list.ToDocuments().ToArray();
            await book.SaveAsync();
            var authors = DB.Queryable<Book>()
                              .Where(b => b.Id == book.Id)
                              .Select(b => b.OtherAuthors).Single();
            Assert.AreEqual(authors.Length, 2);
            Assert.AreEqual(author2.Name, authors[1].Name);
            Assert.AreEqual(book.OtherAuthors[0].Id, authors[0].Id);
        }

        [TestMethod]
        public async Task find_by_lambda_returns_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = guid }; await author1.SaveAsync();
            var author2 = new Author { Name = guid }; await author2.SaveAsync();

            var res = await DB.Find<Author>().ManyAsync(a => a.Name == guid);

            Assert.AreEqual(2, res.Count);
        }

        [TestMethod]
        public async Task find_by_id_returns_correct_document()
        {
            var book1 = new Book { Title = "fbircdb1" }; await book1.SaveAsync();
            var book2 = new Book { Title = "fbircdb2" }; await book2.SaveAsync();

            var res1 = await DB.Find<Book>().OneAsync(new ObjectId().ToString());
            var res2 = await DB.Find<Book>().OneAsync(book2.Id);

            Assert.AreEqual(null, res1);
            Assert.AreEqual(book2.Id, res2.Id);
        }

        [TestMethod]
        public async Task find_by_filter_basic_returns_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = guid }; await author1.SaveAsync();
            var author2 = new Author { Name = guid }; await author2.SaveAsync();

            var res = await DB.Find<Author>().ManyAsync(f => f.Eq(a => a.Name, guid));

            Assert.AreEqual(2, res.Count);
        }

        [TestMethod]
        public async Task find_by_multiple_match_methods()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; await one.SaveAsync();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; await two.SaveAsync();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; await three.SaveAsync();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; await four.SaveAsync();

            var res = await DB.Find<Author>()
                        .Match(a => a.Age > 10)
                        .Match(a => a.Surname == guid)
                        .ExecuteAsync();

            Assert.AreEqual(3, res.Count);
            Assert.IsFalse(res.Any(a => a.Age == 10));
        }

        [TestMethod]
        public async Task find_by_filter_returns_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; await one.SaveAsync();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; await two.SaveAsync();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; await three.SaveAsync();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; await four.SaveAsync();

            var res = await DB.Find<Author>()
                        .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                        .Sort(a => a.Age, FindSortType.Descending)
                        .Sort(a => a.Name, FindSortType.Descending)
                        .Skip(1)
                        .Limit(1)
                        .Project(p => p.Include("Name").Include("Surname"))
                        .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                        .ExecuteAsync();

            Assert.AreEqual(three.Name, res[0].Name);
        }

        private class Test { public string Tester { get; set; } }
        [TestMethod]
        public async Task find_with_projection_to_custom_type_works()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; await one.SaveAsync();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; await two.SaveAsync();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; await three.SaveAsync();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; await four.SaveAsync();

            var res = (await DB.Find<Author, Test>()
                        .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                        .Sort(a => a.Age, FindSortType.Descending)
                        .Sort(a => a.Name, FindSortType.Descending)
                        .Skip(1)
                        .Limit(1)
                        .Project(a => new Test { Tester = a.Name })
                        .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                        .ExecuteAsync())
                        .FirstOrDefault();

            Assert.AreEqual(three.Name, res.Tester);
        }

        [TestMethod]
        public async Task find_with_exclusion_projection_works()
        {
            var author = new Author
            {
                Name = "name",
                Surname = "sername",
                Age = 22,
                FullName = "fullname"
            };
            await author.SaveAsync();

            var res = (await DB.Find<Author>()
                        .Match(a => a.Id == author.Id)
                        .ProjectExcluding(a => new { a.Age, a.Name })
                        .ExecuteAsync())
                        .Single();

            Assert.AreEqual(author.FullName, res.FullName);
            Assert.AreEqual(author.Surname, res.Surname);
            Assert.IsTrue(res.Age == default);
            Assert.IsTrue(res.Name == default);
        }

        [TestMethod]
        public async Task find_with_aggregation_pipeline_returns_correct_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; await one.SaveAsync();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; await two.SaveAsync();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; await three.SaveAsync();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; await four.SaveAsync();

            var res = await DB.Fluent<Author>()
                        .Match(a => a.Surname == guid && a.Age > 10)
                        .SortByDescending(a => a.Age)
                        .ThenByDescending(a => a.Name)
                        .Skip(1)
                        .Limit(1)
                        .Project(a => new { Test = a.Name })
                        .FirstOrDefaultAsync();

            Assert.AreEqual(three.Name, res.Test);
        }

        [TestMethod]
        public async Task find_with_aggregation_expression_works()
        {
            var guid = Guid.NewGuid().ToString();
            var author = new Author { Name = "a", Age = 10, Age2 = 11, Surname = guid }; await author.SaveAsync();

            var res = (await DB.Find<Author>()
                        .MatchExpression("{$and:[{$gt:['$Age2','$Age']},{$eq:['$Surname','" + guid + "']}]}")
                        .ExecuteAsync())
                        .Single();

            Assert.AreEqual(res.Surname, guid);
        }

        [TestMethod]
        public async Task find_fluent_with_aggregation_expression_works()
        {
            var guid = Guid.NewGuid().ToString();
            var author = new Author { Name = "a", Age = 10, Age2 = 11, Surname = guid }; await author.SaveAsync();

            var res = await DB.Fluent<Author>()
                        .Match(a => a.Surname == guid)
                        .MatchExpression("{$gt:['$Age2','$Age']}")
                        .SingleAsync();

            Assert.AreEqual(res.Surname, guid);
        }

        [TestMethod]
        public async Task decimal_properties_work_correctly()
        {
            var guid = Guid.NewGuid().ToString();
            var book1 = new Book { Title = guid, Price = 100.123m }; await book1.SaveAsync();
            var book2 = new Book { Title = guid, Price = 100.123m }; await book2.SaveAsync();

            var res = DB.Queryable<Book>()
                        .Where(b => b.Title == guid)
                        .GroupBy(b => b.Title)
                        .Select(g => new
                        {
                            Title = g.Key,
                            Sum = g.Sum(b => b.Price)
                        }).Single();

            Assert.AreEqual(book1.Price + book2.Price, res.Sum);
        }

        [TestMethod]
        public async Task ignore_if_defaults_convention_works()
        {
            var author = new Author
            {
                Name = "test"
            };
            await author.SaveAsync();

            var res = await DB.Find<Author>().OneAsync(author.Id);

            Assert.IsTrue(res.Age == 0);
            Assert.IsTrue(res.Birthday == null);
        }

        [TestMethod]
        public async Task json_property_should_be_saved()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TableData>();
            dbContext.Dispose();
            dbContext = new DbContext();
            var tempName = Guid.NewGuid().ToString();
            var data = new TableData()
            {
                DataType = "object",
                Data = new JsonObject { { "customerType", "VIP" }, { "customerName", tempName }, { "test", new JsonObject { { "key", "value" } } } }
            };
            dbContext.Attach(data);
            await dbContext.CommitAsync();
            dbContext.Dispose();
            dbContext = new DbContext();
            var item = dbContext.Queryable<TableData>().ToList();
            item.Count.ShouldBe(1);
            var objectData = item.First();
            objectData.Data["customerType"] = "VVIP";
            await objectData.SaveAsync();
            await dbContext.CommitAsync();
            dbContext.Dispose();
            dbContext = new DbContext();
            item = dbContext.Queryable<TableData>().ToList();
            item.Count.ShouldBe(1);
            item.First().Data["customerType"].GetValue<string>().ShouldBe("VVIP");
        }
    }
}
