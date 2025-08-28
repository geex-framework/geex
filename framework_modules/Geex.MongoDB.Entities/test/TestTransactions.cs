using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Driver;

using System;
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
                TN.session.StartTransaction();
                await TN.Update<Author>()
                  .Match(a => a.Surname == guid)
                  .Modify(a => a.Name, guid)
                  .Modify(a => a.Surname, author1.Name)
                  .ExecuteAsync();

                await TN.session.AbortTransactionAsync();
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
                //await res.SaveAsync();
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
                TN.PostSaveChanges += async () =>
                 {
                     await Task.Delay(1000);
                     triggered = true;
                 };
                await TN.SaveChanges();
            }

            Assert.IsTrue(triggered);
        }

        [TestMethod]
        public async Task explicit_transaction_commit_modifies_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "explicit_test1", Surname = guid }; 
            await author1.SaveAsync();

            using (var TN = new DbContext())
            {
                // Start explicit transaction
                TN.StartExplicitTransaction();
                Assert.IsTrue(TN.IsInExplicitTransaction);

                // Modify data within explicit transaction
                await TN.Update<Author>()
                  .Match(a => a.Id == author1.Id)
                  .Modify(a => a.Name, "explicit_modified")
                  .ExecuteAsync();

                // Commit explicit transaction
                await TN.CommitExplicitTransaction();
                Assert.IsFalse(TN.IsInExplicitTransaction);
            }

            var res = await DB.Find<Author>().OneAsync(author1.Id);
            Assert.AreEqual("explicit_modified", res.Name);
        }

        [TestMethod]
        public async Task explicit_transaction_rollback_on_dispose_doesnt_modify_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "explicit_rollback_test", Surname = guid }; 
            await author1.SaveAsync();

            using (var TN = new DbContext())
            {
                // Start explicit transaction
                TN.StartExplicitTransaction();
                Assert.IsTrue(TN.IsInExplicitTransaction);

                // Modify data within explicit transaction
                await TN.Update<Author>()
                  .Match(a => a.Id == author1.Id)
                  .Modify(a => a.Name, "should_not_be_saved")
                  .ExecuteAsync();

                // Don't commit - let it rollback on dispose
            }

            var res = await DB.Find<Author>().OneAsync(author1.Id);
            Assert.AreEqual("explicit_rollback_test", res.Name);
        }

        [TestMethod]
        public async Task explicit_transaction_property_works_correctly()
        {
            using (var TN = new DbContext())
            {
                // Initially not in explicit transaction
                Assert.IsFalse(TN.IsInExplicitTransaction);

                // Start explicit transaction
                TN.StartExplicitTransaction();
                Assert.IsTrue(TN.IsInExplicitTransaction);

                // Commit explicit transaction
                await TN.CommitExplicitTransaction();
                Assert.IsFalse(TN.IsInExplicitTransaction);
            }
        }

        [TestMethod]
        public async Task explicit_transaction_multiple_operations_work()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "multi_op_test1", Surname = guid };
            var author2 = new Author { Name = "multi_op_test2", Surname = guid };
            var book1 = new Book { Title = "multi_op_book1" };

            using (var TN = new DbContext())
            {
                // Start explicit transaction
                TN.StartExplicitTransaction();
                Assert.IsTrue(TN.IsInExplicitTransaction);

                // Multiple operations within the same transaction
                TN.Attach(author1);
                TN.Attach(author2);
                TN.Attach(book1);
                
                await author1.SaveAsync();
                await author2.SaveAsync();
                await book1.SaveAsync();

                // Update operations
                await TN.Update<Author>()
                  .Match(a => a.Id == author1.Id)
                  .Modify(a => a.Name, "multi_op_updated1")
                  .ExecuteAsync();

                await TN.Update<Book>()
                  .Match(b => b.Id == book1.Id)
                  .Modify(b => b.Title, "multi_op_book_updated")
                  .ExecuteAsync();

                // Commit explicit transaction
                await TN.CommitExplicitTransaction();
            }

            // Verify all operations were committed
            var resAuthor1 = await DB.Find<Author>().OneAsync(author1.Id);
            var resAuthor2 = await DB.Find<Author>().OneAsync(author2.Id);
            var resBook1 = await DB.Find<Book>().OneAsync(book1.Id);

            Assert.AreEqual("multi_op_updated1", resAuthor1.Name);
            Assert.AreEqual("multi_op_test2", resAuthor2.Name);
            Assert.AreEqual("multi_op_book_updated", resBook1.Title);
        }

        [TestMethod]
        public async Task explicit_transaction_error_scenarios()
        {
            using (var TN = new DbContext())
            {
                // Test committing without starting
                Assert.IsFalse(TN.IsInExplicitTransaction);
                await TN.CommitExplicitTransaction(); // Should not throw, but log warning
                Assert.IsFalse(TN.IsInExplicitTransaction);

                // Test starting transaction twice (if transaction is supported)
                if (TN.SupportTransaction)
                {
                    TN.StartExplicitTransaction();
                    Assert.IsTrue(TN.IsInExplicitTransaction);
                    
                    // Try to start again - should log warning but not change state
                    TN.StartExplicitTransaction();
                    Assert.IsTrue(TN.IsInExplicitTransaction);

                    await TN.CommitExplicitTransaction();
                    Assert.IsFalse(TN.IsInExplicitTransaction);
                }
            }
        }

        [TestMethod]
        public async Task explicit_transaction_prevents_auto_transaction()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "prevent_auto_test", Surname = guid };

            using (var TN = new DbContext())
            {
                // Start explicit transaction
                TN.StartExplicitTransaction();
                Assert.IsTrue(TN.IsInExplicitTransaction);

                // Operations should not create auto-transactions
                TN.Attach(author1);
                await author1.SaveAsync();
                
                // Verify we're still in the same explicit transaction
                Assert.IsTrue(TN.IsInExplicitTransaction);

                // Don't commit - let it rollback
            }

            // Verify data was not saved due to rollback
            var res = await DB.Find<Author>().OneAsync(author1.Id);
            Assert.IsNull(res);
        }
    }
}
