﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Dates
    {
        [TestMethod]
        public async Task not_setting_date_doesnt_cause_issuesAsync()
        {
            var book = new Book { Title = "nsddci" };
            await book.SaveAsync();

            var res = await DB.Find<Book>().OneAsync(book.Id);

            Assert.AreEqual(res.Title, book.Title);
            Assert.IsNull(res.PublishedOn);
        }

        [TestMethod]
        public async Task date_props_contain_correct_value()
        {
            var pubDate = DateTime.UtcNow;

            var book = new Book
            {
                Title = "dpccv",
                PublishedOn = pubDate
            };
            await book.SaveAsync();

            var res = await DB.Find<Book>().OneAsync(book.Id);

            Assert.AreEqual(pubDate.Ticks, res.PublishedOn.Ticks);
            Assert.AreEqual(pubDate.ToUniversalTime(), res.PublishedOn.DateTime);
            Assert.AreEqual(pubDate, res.PublishedOn.DateTime);
            Assert.AreEqual(DateTimeKind.Utc, res.PublishedOn.DateTime.Kind);
        }

        [TestMethod]
        public async Task querying_on_ticks_when_null()
        {
            var book = new Book
            {
                Title = "qotwn",
            };
            await book.SaveAsync();

            var res = DB
                .Queryable<Book>()
                .SingleOrDefault(b => b.Id == book.Id && b.PublishedOn.Ticks > 0);

            Assert.IsNull(res);
        }

        [TestMethod]
        public async Task querying_on_ticks_work()
        {
            var pubDate = DateTime.UtcNow;

            var book = new Book
            {
                Title = "qotw",
                PublishedOn = pubDate
            };
            await book.SaveAsync();

            var res = (await DB.Find<Book>()
                        .Match(b => b.Id == book.Id && b.PublishedOn.Ticks == pubDate.Ticks)
                        .ExecuteAsync())
                        .Single();

            Assert.AreEqual(book.Id, res.Id);

            res = (await DB.Find<Book>()
                    .Match(b => b.Id == book.Id && b.PublishedOn.Ticks < pubDate.Ticks + TimeSpan.FromSeconds(1).Ticks)
                    .ExecuteAsync())
                    .Single();

            Assert.AreEqual(book.Id, res.Id);
        }

        [TestMethod]
        public async Task querying_on_datetime_prop_works()
        {
            var pubDate = DateTime.UtcNow;

            var book = new Book
            {
                Title = "qodtpw",
                PublishedOn = pubDate
            };
            await book.SaveAsync();

            var res = (await DB.Find<Book>()
            .Match(b => b.Id == book.Id && b.PublishedOn.DateTime == pubDate)
            .ExecuteAsync())
            .Single();

            Assert.AreEqual(book.Id, res.Id);

            res = (await DB.Find<Book>()
                    .Match(b => b.Id == book.Id && b.PublishedOn.DateTime < pubDate.AddSeconds(1))
                    .ExecuteAsync())
                    .Single();

            Assert.AreEqual(book.Id, res.Id);
        }

        [TestMethod]
        public void setting_ticks_creates_correct_datetime()
        {
            var now = DateTime.UtcNow;

            var date = new Date() { Ticks = now.Ticks };

            Assert.AreEqual(now, date.DateTime);
        }
    }
}
