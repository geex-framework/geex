﻿using System;
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
    [TestClass]
    public class TestInterceptors
    {
        [TestMethod]
        public async Task save_interceptors_should_work()
        {
             var dbContext = new DbContext();
            await dbContext.DeleteAsync<InterceptedAndFiltered>();
            await dbContext.CommitAsync();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new InterceptedAndFiltered()
            {
                Value = 0
            };
            dbContext.Attach(testEntity);
            await testEntity.SaveAsync();
            testEntity.Value.ShouldBe(2);
            await dbContext.CommitAsync();
            testEntity.Value.ShouldBe(3);
            dbContext = new DbContext();
            testEntity = await dbContext.Find<InterceptedAndFiltered>().Match(x => x.Id == testEntity.Id).ExecuteFirstAsync();
            testEntity.Value.ShouldBe(3);
        }

        [TestMethod]
        public async Task attach_interceptors_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<InterceptedAndFiltered>();
            await dbContext.CommitAsync();
            dbContext.Dispose();
            dbContext = new DbContext();
            var testEntity = new InterceptedAndFiltered()
            {
                Value = 0
            };
            dbContext.Attach(testEntity);
            testEntity.Value.ShouldBe(1);
            await dbContext.CommitAsync();
            testEntity.Value.ShouldBe(2);
            dbContext = new DbContext();
            testEntity = await dbContext.Find<InterceptedAndFiltered>().Match(x => x.Id == testEntity.Id).ExecuteFirstAsync();
            testEntity.Value.ShouldBe(2);
        }
    }
}
