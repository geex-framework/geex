using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Entities.Interceptors;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    class TestSaveInterceptor : DataInterceptor<InterceptedAndFiltered>
    {
        public override void Apply(InterceptedAndFiltered entity)
        {
            entity.Value += 1;
        }

        /// <inheritdoc />
        public override InterceptorExecuteTiming InterceptAt { get; } = InterceptorExecuteTiming.Save;
    }

    class TestAttachInterceptor : DataInterceptor<InterceptedAndFiltered>
    {
        public override void Apply(InterceptedAndFiltered entity)
        {
            entity.Value += 1;
        }

        /// <inheritdoc />
        public override InterceptorExecuteTiming InterceptAt { get; } = InterceptorExecuteTiming.Attach;
    }


    [TestClass]
    public class TestInterceptors
    {
        [TestMethod]
        public async Task should_register_interceptor()
        {
            DbContext.StaticDataInterceptors.TryAdd(typeof(InterceptedAndFiltered), (sp) => new TestSaveInterceptor());
            var dbContext = new DbContext();
            Assert.IsTrue(dbContext.DataInterceptors.Any());
            dbContext = new DbContext();
            dbContext.RemoveDataInterceptors(typeof(TestSaveInterceptor));
            Assert.IsFalse(dbContext.DataInterceptors.Any());
            Assert.IsTrue(DbContext.StaticDataInterceptors.Any());
            DbContext.RemoveDataInterceptorsForAll(typeof(TestSaveInterceptor));
            dbContext = new DbContext();
            Assert.IsFalse(dbContext.DataInterceptors.Any());
            Assert.IsFalse(DbContext.StaticDataInterceptors.Any(x => x.Value is Func<IServiceProvider, TestSaveInterceptor>));

            //Assert.IsTrue(dbContext.DataInterceptors.Any(x => x is Func<IServiceProvider,TestSaveInterceptor>));
            //Assert.IsTrue(DB.DataInterceptors.Any(x => x is Func<IServiceProvider,TestSaveInterceptor>));
        }

        [TestMethod]
        public async Task save_interceptors_should_work()
        {
             var dbContext = new DbContext();
            await dbContext.DeleteAsync<InterceptedAndFiltered>();
            await dbContext.CommitAsync();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.RegisterDataInterceptors(new TestSaveInterceptor());
            var testEntity = new InterceptedAndFiltered()
            {
                Value = 0
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            testEntity.Value.ShouldBe(1);
            await dbContext.CommitAsync();
            testEntity.Value.ShouldBe(2);
            dbContext = new DbContext();
            testEntity = await dbContext.Find<InterceptedAndFiltered>().Match(x => x.Id == testEntity.Id).ExecuteFirstAsync();
            testEntity.Value.ShouldBe(2);
        }

        [TestMethod]
        public async Task attach_interceptors_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InterceptedAndFiltered>();
            await dbContext.CommitAsync();
            dbContext.Dispose();
            dbContext = new DbContext();
            dbContext.RegisterDataInterceptors(new TestAttachInterceptor());
            var testEntity = new InterceptedAndFiltered()
            {
                Value = 0
            };
            dbContext.Attach(testEntity);
            testEntity.Value.ShouldBe(1);
            await dbContext.CommitAsync();
            testEntity.Value.ShouldBe(1);
            dbContext = new DbContext();
            testEntity = await dbContext.Find<InterceptedAndFiltered>().Match(x => x.Id == testEntity.Id).ExecuteFirstAsync();
            testEntity.Value.ShouldBe(1);
        }
    }
}
