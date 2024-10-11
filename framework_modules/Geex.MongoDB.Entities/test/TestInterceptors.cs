using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestInterceptors
    {
        [TestMethod]
        public async Task save_interceptors_should_work()
        {
             var dbContext = new DbContext();
            await dbContext.DeleteAsync<InterceptedAndFiltered>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new InterceptedAndFiltered()
            {
                Value = 0
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            testEntity.Value.ShouldBe(2);
            await dbContext.SaveChanges();
            testEntity.Value.ShouldBe(3);
            dbContext = new DbContext();
            testEntity = dbContext.Query<InterceptedAndFiltered>().FirstOrDefault(x => x.Id == testEntity.Id);
            testEntity.Value.ShouldBe(4);
        }

        [TestMethod]
        public async Task attach_interceptors_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InterceptedAndFiltered>();
            await dbContext.SaveChanges();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new InterceptedAndFiltered()
            {
                Value = 0
            };
            dbContext.Attach(testEntity);
            testEntity.Value.ShouldBe(1);
            await dbContext.SaveChanges();
            testEntity.Value.ShouldBe(2);
            dbContext = new DbContext();
            testEntity = dbContext.Query<InterceptedAndFiltered>().FirstOrDefault(x => x.Id == testEntity.Id);
            testEntity.Value.ShouldBe(3);
        }
    }
}
