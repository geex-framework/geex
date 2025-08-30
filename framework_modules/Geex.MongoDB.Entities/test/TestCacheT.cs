using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MongoDB.Entities.Tests.Models;
using Shouldly;
namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestCacheT
    {
        [TestMethod]
        public async Task inheritanceShouldWork()
        {
            var parentCache = Cache<InheritanceEntity>.MemberMaps;
            var childCache = Cache<InheritanceEntityChild>.MemberMaps;
            Cache<InheritanceEntity>.CollectionName.ShouldBe(Cache<InheritanceEntityChild>.CollectionName);
            var parentClassMap = Cache<InheritanceEntity>.ClassMap;
            var childClassMap = Cache<InheritanceEntityChild>.ClassMap;
            parentClassMap.Discriminator.ShouldBe(nameof(InheritanceEntity));
            childClassMap.Discriminator.ShouldBe(nameof(InheritanceEntityChild));
            parentClassMap.DiscriminatorIsRequired.ShouldBe(true);
            childClassMap.DiscriminatorIsRequired.ShouldBe(true);
            parentCache.Length.ShouldBe(3);
            childCache.Length.ShouldBe(3);
            parentCache[0].MemberName.ShouldBe(nameof(InheritanceEntity.Id));
            parentCache[1].MemberName.ShouldBe(nameof(InheritanceEntity.CreatedOn));
            parentCache[2].MemberName.ShouldBe(nameof(InheritanceEntity.Name));
            childCache[0].MemberName.ShouldBe(nameof(InheritanceEntityChild.Id));
            childCache[1].MemberName.ShouldBe(nameof(InheritanceEntityChild.CreatedOn));
            childCache[2].MemberName.ShouldBe(nameof(InheritanceEntityChild.Name));
        }

        [TestMethod]
        public void MeasureGetTypePerformance()
        {
            List<object> items = new List<object>(10000);
            // 填充数据
            for (int i = 0; i < 100000; i++)
            {
                items.Add(i % 2 == 0 ? "string" : 123);
            }

            Stopwatch sw = Stopwatch.StartNew();
            foreach (var item in items)
            {
                Type t = item.GetType();
            }
            sw.Stop();

            Debug.WriteLine($"调用100000次GetType()耗时: {sw.ElapsedMilliseconds}毫秒");
        }
    }
}
