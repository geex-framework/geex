using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestFilters
    {
        [TestMethod]
        public async Task should_register_data_filter()
        {
            Expression<Func<InterceptedAndFiltered, bool>> exp = (x => true);
            DbContext.StaticDataFilters.TryAdd(typeof(InterceptedAndFiltered), (sp) => new ExpressionDataFilter<InterceptedAndFiltered>(exp, null));
            //DbContext.RegisterDataFiltersForAll(new ExpressionDataFilter<InterceptedAndFiltered>(exp, null));
            var dbContext = new DbContext();
            Assert.IsTrue(dbContext.DataFilters.Any(x => x.Value is IDataFilter<InterceptedAndFiltered>));
            dbContext.RemoveDataFilters(typeof(InterceptedAndFiltered));
            Assert.IsFalse(dbContext.DataFilters.Any(x => x.Value is IDataFilter<InterceptedAndFiltered>));
            Assert.IsTrue(DbContext.StaticDataFilters.Any());
            DbContext.RemoveDataFiltersForAll(typeof(InterceptedAndFiltered));
            dbContext = new DbContext();
            Assert.IsFalse(dbContext.DataFilters.Any(x => x.Value is IDataFilter<InterceptedAndFiltered>));
            Assert.IsFalse(DbContext.StaticDataFilters.Any());
            //Assert.IsTrue(dbContext.SaveInterceptors.Any(x => x is TestSaveInterceptor));
            //Assert.IsTrue(DB.SaveInterceptors.Any(x => x is TestSaveInterceptor));
        }

        [TestMethod]
        public async Task query_data_filters_should_work()
        {
            var dbContext = new DbContext();
            Expression<Func<InterceptedAndFiltered, bool>> exp = (x => x.Value == 1);
            DbContext.StaticDataFilters.TryAdd(typeof(InterceptedAndFiltered), (sp) => new ExpressionDataFilter<InterceptedAndFiltered>(exp, null));
            await dbContext.DeleteTypedAsync<InterceptedAndFiltered>();
            var testEntity = new InterceptedAndFiltered()
            {
                Value = 0
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            await dbContext.SaveChanges();
            dbContext = new DbContext();
            //var result = await dbContext.Find<InterceptedAndFiltered>().Match(x => x.Id == testEntity.Id).ExecuteFirstAsync();
            //Assert.IsNull(result);
            var result = dbContext.Query<InterceptedAndFiltered>().FirstOrDefault(x => x.Id == testEntity.Id);
            Assert.IsNull(result);
            //var resultList = await dbContext.Find<InterceptedAndFiltered>().ExecuteAsync();
            //Assert.AreEqual(0, resultList.Count);
            var resultList = dbContext.Query<InterceptedAndFiltered>().ToList();
            Assert.AreEqual(0, resultList.Count);
        }
    }
}