using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    //NOTE: transactions are only supported on replica-sets. you need at least a single-node replica-set.
    //      use mongod.cfg at root level of repo to run mongodb in replica-set mode
    //      then run rs.initiate() in a mongo console

    [TestClass]
    public class Transactions
    {
        [TestMethod]
        public async Task not_commiting_and_aborting_update_transaction_doesnt_modify_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "uwtrcd1", Surname = guid }; await author1.SaveAsync();
            var author2 = new Author { Name = "uwtrcd2", Surname = guid }; await author2.SaveAsync();
            var author3 = new Author { Name = "uwtrcd3", Surname = guid }; await author3.SaveAsync();

            using (var TN = new DbContext())
            {
                await TN.Update<Author>()
                  .Match(a => a.Surname == guid)
                  .Modify(a => a.Name, guid)
                  .Modify(a => a.Surname, author1.Name)
                  .ExecuteAsync();

                await TN.AbortAsync();
                //TN.SaveChanges();
            }

            var res = await DB.Find<Author>().OneAsync(author1.Id);

            Assert.AreEqual(author1.Name, res.Name);
        }

        [TestMethod]
        public async Task commiting_update_transaction_modifies_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "uwtrcd1", Surname = guid }; await author1.SaveAsync();
            var author2 = new Author { Name = "uwtrcd2", Surname = guid }; await author2.SaveAsync();
            var author3 = new Author { Name = "uwtrcd3", Surname = guid }; await author3.SaveAsync();

            using (var TN = new DbContext())
            {
                await TN.Update<Author>()
                  .Match(a => a.Surname == guid)
                  .Modify(a => a.Name, guid)
                  .Modify(a => a.Surname, author1.Name)
                  .ExecuteAsync();

                await TN.SaveChanges();
            }

            var res = await DB.Find<Author>().OneAsync(author1.Id);

            Assert.AreEqual(guid, res.Name);
        }

        [TestMethod]
        public async Task create_and_find_transaction_returns_correct_docs()
        {
            var book1 = new Book { Title = "caftrcd1" };
            var book2 = new Book { Title = "caftrcd1" };

            Book res;
            Book fnt;

            using (var TN = new DbContext())
            {
                TN.Attach(book1);
                TN.Attach(book2);
                await book1.SaveAsync();
                await book2.SaveAsync();

                res = await TN.Find<Book>().OneAsync(book1.Id);
                res = book1.Fluent().Match(f => f.Eq(b => b.Id, book1.Id)).SingleOrDefault();
                fnt = TN.Fluent<Book>().FirstOrDefault();
                fnt = TN.Fluent<Book>().Match(b => b.Id == book2.Id).SingleOrDefault();
                fnt = TN.Fluent<Book>().Match(f => f.Eq(b => b.Id, book2.Id)).SingleOrDefault();

                await TN.SaveChanges();
            }

            Assert.IsNotNull(res);
            Assert.AreEqual(book1.Id, res.Id);
            Assert.AreEqual(book2.Id, fnt.Id);
        }

        [TestMethod]
        public async Task delete_in_transaction_works()
        {
            var book1 = new Book { Title = "caftrcd1" };
            await book1.SaveAsync();

            using (var TN = new DbContext())
            {
                await TN.DeleteAsync<Book>(book1.Id);
                await TN.SaveChanges();
            }

            Assert.AreEqual(null, await DB.Find<Book>().OneAsync(book1.Id));
        }

        [TestMethod]
        public async Task full_text_search_transaction_returns_correct_results()
        {
            await DB.Index<Author>()
              .Option(o => o.Background = false)
              .Key(a => a.Name, KeyType.Text)
              .Key(a => a.Surname, KeyType.Text)
              .CreateAsync();

            var author1 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            var author2 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            await DB.SaveAsync(author1);
            await DB.SaveAsync(author2);

            using var TN = new DbContext();
            var tres = TN.FluentTextSearch<Author>(FindSearchType.Full, author1.Surname).ToList();
            Assert.AreEqual(author1.Surname, tres[0].Surname);

            var tflu = TN.FluentTextSearch<Author>(FindSearchType.Full, author2.Surname).SortByDescending(x => x.ModifiedOn).ToList();
            Assert.AreEqual(author2.Surname, tflu[0].Surname);
        }

        [TestMethod]
        public async Task bulk_save_entities_transaction_returns_correct_results()
        {
            var guid = Guid.NewGuid().ToString();

            var entities = new[] {
                new Book{Title="one "+guid},
                new Book{Title="two "+guid},
                new Book{Title="thr "+guid}
            }.ToList();

            using (var TN = new DbContext())
            {
                TN.Attach(entities);
                await entities.SaveAsync();
                await TN.SaveChanges();
            }

            var res = await DB.Find<Book>().ManyAsync(b => b.Title.Contains(guid));
            Assert.AreEqual(entities.Count, res.Count);

            foreach (var ent in res)
            {
                ent.Title = "updated " + guid;
            }
            await res.SaveAsync();

            res = await DB.Find<Book>().ManyAsync(b => b.Title.Contains(guid));
            Assert.AreEqual(3, res.Count);
            Assert.AreEqual("updated " + guid, res[0].Title);
        }

        [TestMethod]
        public async Task find_outcome_entities_are_attached_to_session()
        {
            var guid = Guid.NewGuid().ToString();
            var guid1 = Guid.NewGuid().ToString();

            var entities = new[] {
                new Book{Title="one "+guid},
                new Book{Title="two "+guid},
                new Book{Title="thr "+guid}
            }.ToList();

            using (var TN = new DbContext())
            {
                TN.Attach(entities);
                await entities.SaveAsync();
                await TN.SaveChanges();
            }

            var res = await DB.Find<Book>().ManyAsync(b => b.Title.Contains(guid));
            Assert.AreEqual(3, res.Count);
            using (var db = new DbContext())
            {
                res = await db.Find<Book>().ManyAsync(b => b.Title.Contains(guid));
                Assert.AreEqual(entities.Count, res.Count);

                foreach (var ent in res)
                {
                    ent.Title = "updated " + guid1;
                }
                await res.SaveAsync();
                await db.AbortAsync();
            }
            res = await DB.Find<Book>().ManyAsync(b => b.Title.Contains(guid1));
            Assert.AreEqual(0, res.Count);
            using (var db = new DbContext())
            {
                res = await db.Find<Book>().ManyAsync(b => b.Title.Contains(guid));
                Assert.AreEqual(entities.Count, res.Count);

                foreach (var ent in res)
                {
                    ent.Title = "updated " + guid1;
                }
                await res.SaveAsync();
                await db.SaveChanges();
            }
            res = await DB.Find<Book>().ManyAsync(b => b.Title.Contains(guid1));
            Assert.AreEqual(3, res.Count);
            Assert.AreEqual("updated " + guid1, res[0].Title);

            //await db.SaveChanges();
            //res = await DB.Find<Book>().ManyAsync(b => b.Title.Contains(guid1));
            //Assert.AreEqual(3, res.Count);
            //Assert.AreEqual("updated " + guid1, res[0].Title);

        }

        [TestMethod]
        public async Task commit_event_should_work()
        {
            var guid = Guid.NewGuid().ToString();
            var triggered = false;

            var entities = new[] {
                new Book{Title="one "+guid},
                new Book{Title="two "+guid},
                new Book{Title="thr "+guid}
            }.ToList();

            using (var TN = new DbContext())
            {
                TN.Attach(entities);
                await entities.SaveAsync();
                TN.OnCommitted += async () =>
                 {
                     await Task.Delay(1000);
                     triggered = true;
                 };
                await TN.SaveChanges();
            }

            Assert.IsTrue(triggered);
        }
    }
}
