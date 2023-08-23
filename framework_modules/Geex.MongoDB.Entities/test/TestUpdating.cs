using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Update
    {
        [TestMethod]
        public async Task updating_modifies_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; await author1.SaveAsync();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; await author2.SaveAsync();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; await author3.SaveAsync();

            await DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(a => a.Name, guid)
              .Modify(a => a.Surname, author1.Name)
              .Option(o => o.BypassDocumentValidation = true)
              .ExecuteAsync();

            var count = DB.Queryable<Author>().Where(a => a.Name == guid && a.Surname == author1.Name).Count();
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void update_without_filter_throws()
        {
            Assert.ThrowsException<ArgumentException>(() => DB.Update<Author>().Modify(a => a.Age2, 22).ExecuteAsync().GetAwaiter().GetResult());
        }

        [TestMethod]
        public async Task updating_returns_correct_result()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; await author1.SaveAsync();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; await author2.SaveAsync();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; await author3.SaveAsync();

            var res = await DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(a => a.Name, guid)
              .Modify(a => a.Surname, author1.Name)
              .Option(o => o.BypassDocumentValidation = true)
              .ExecuteAsync();

            Assert.AreEqual(2, res.MatchedCount);
            Assert.AreEqual(2, res.ModifiedCount);
        }

        [TestMethod]
        public async Task update_by_def_builder_mods_correct_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; await author1.SaveAsync();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; await author2.SaveAsync();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; await author3.SaveAsync();

            await DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(b => b.Inc(a => a.Age, 10))
              .Modify(b => b.Set(a => a.Name, guid))
              .Modify(b => b.CurrentDate(a => a.ModifiedOn))
              .ExecuteAsync();

            var res = await DB.Find<Author>().ManyAsync(a => a.Surname == guid && a.Age == 10);

            Assert.AreEqual(2, res.Count);
            Assert.AreEqual(guid, res[0].Name);
        }

        [TestMethod]
        public async Task nested_properties_update_correctly()
        {
            var guid = Guid.NewGuid().ToString();

            var book = new Book
            {
                Title = "mnpuc title " + guid,
                Review = new Review { Rating = 10.10 }
            };
            await book.SaveAsync();

            await DB.Update<Book>()
                .Match(b => b.Review.Rating == 10.10)
                .Modify(b => b.Review.Rating, 22.22)
                .ExecuteAsync();

            var res = await DB.Find<Book>().OneAsync(book.Id);

            Assert.AreEqual(22.22, res.Review.Rating);
        }

        [TestMethod]
        public async Task bulk_update_modifies_correct_documents()
        {
            var title = "bumcd " + Guid.NewGuid().ToString();
            var books = new List<Book>();

            for (int i = 1; i <= 5; i++)
            {
                books.Add(new Book { Title = title, Price = i });
            }
            await books.SaveAsync();

            var bulk = DB.Update<Book>();

            foreach (var book in books)
            {
                bulk.Match(b => b.Id == book.Id)
                    .Modify(b => b.Price, 100)
                    .AddToQueue();
            }

            await bulk.ExecuteAsync();

            var res = await DB.Find<Book>()
                        .ManyAsync(b => b.Title == title);

            Assert.AreEqual(5, res.Count);
            Assert.AreEqual(5, res.Count(b => b.Price == 100));
        }

        [TestMethod]
        public async Task skip_setting_mod_date_if_user_is_doing_something_with_it()
        {
            var book = new Book { Title = "test" };
            await book.SaveAsync();

            book = await DB.Find<Book>().OneAsync(book.Id);
            Assert.IsTrue(DateTimeOffset.UtcNow.Subtract(book.ModifiedOn.DateTime).TotalSeconds < 5);

            var targetDate = DateTimeOffset.Now.AddDays(100);

            await DB
                .Update<Book>()
                .MatchId(book.Id)
                .Modify(b => b.ModifiedOn, targetDate)
                .ExecuteAsync();

            book = await DB.Find<Book>().OneAsync(book.Id);
            Assert.AreEqual(targetDate.DateTime.ToShortDateString(), book.ModifiedOn.DateTime.ToShortDateString());
        }
    }
}
