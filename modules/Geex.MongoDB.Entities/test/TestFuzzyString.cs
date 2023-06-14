using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class FuzzyStringTest
    {
        [TestMethod]
        public async Task fuzzystring_type_saving_and_retrieval_worksAsync()
        {
            var guid = Guid.NewGuid().ToString();

            await new Book { Title = "fstsarw", Review = new Review { Fuzzy = guid } }.SaveAsync();

            var res = DB
                .Queryable<Book>()
                .Single(b => b.Review.Fuzzy.Value == guid);

            Assert.AreEqual(guid, res.Review.Fuzzy.Value);
        }

        [TestMethod]
        public async Task fuzzystring_type_with_nulls_workAsync()
        {
            var guid = Guid.NewGuid().ToString();

            await new Book { Title = guid, Review = new Review { Fuzzy = null } }.SaveAsync();

            var res = DB
                .Queryable<Book>()
                .Single(b => b.Title == guid);

            Assert.AreEqual(null, res.Review.Fuzzy?.Value);
        }
    }
}
