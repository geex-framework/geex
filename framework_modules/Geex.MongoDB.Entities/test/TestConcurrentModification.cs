using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Entities.Exceptions;
using MongoDB.Entities.Tests.Models;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestConcurrentModification
    {
        [TestInitialize]
        public async Task Init()
        {
            // 清理测试数据
            await DB.DeleteTypedAsync<TestEntity>();
        }

        [TestMethod]
        public async Task concurrent_modification_same_field_should_throw_optimistic_concurrency_exception()
        {
            // 准备测试数据
            var originalEntity = new TestEntity
            {
                Name = "OriginalName",
                Value = 100
            };

            // 保存原始实体
            using var dbContext = new DbContext();
            dbContext.Attach(originalEntity);
            await dbContext.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 在两个不同的上下文中加载同一实体
            using var dbContext1 = new DbContext();
            using var dbContext2 = new DbContext();

            var entity1 = dbContext1.Query<TestEntity>().First(x => x.Id == originalEntity.Id);
            var entity2 = dbContext2.Query<TestEntity>().First(x => x.Id == originalEntity.Id);

            // 验证两个实体的初始状态相同
            entity1.Name.ShouldBe("OriginalName");
            entity2.Name.ShouldBe("OriginalName");
            entity1.ModifiedOn.ShouldBe(entity2.ModifiedOn);

            // 第一个上下文修改并保存
            entity1.Name = "ModifiedByContext1";
            await dbContext1.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 第二个上下文修改同一字段并尝试保存 - 应该抛出异常
            entity2.Name = "ModifiedByContext2";

            var exception = await Should.ThrowAsync<OptimisticConcurrencyException>(async () =>
            {
                await dbContext2.SaveChanges();
            });

            // 验证异常信息
            exception.EntityType.ShouldBe(typeof(TestEntity));
            exception.EntityId.ShouldBe(originalEntity.Id);
            exception.ConflictingFields.ShouldNotBeEmpty();
            exception.ConflictingFields.Any(f => f.FieldName == nameof(TestEntity.Name)).ShouldBeTrue();

            // 验证冲突字段的值
            var nameConflict = exception.ConflictingFields.First(f => f.FieldName == nameof(TestEntity.Name));
            nameConflict.BaseValue.ShouldBe("OriginalName");
            nameConflict.OurValue.ShouldBe("ModifiedByContext2");
            nameConflict.TheirValue.ShouldBe("ModifiedByContext1");
        }

        [TestMethod]
        public async Task concurrent_modification_different_fields_should_not_conflict()
        {
            // 准备测试数据
            var originalEntity = new TestEntity
            {
                Name = "OriginalName",
                Value = 100
            };

            // 保存原始实体
            using var dbContext1 = new DbContext();
            dbContext1.Attach(originalEntity);
            await dbContext1.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 在两个不同的上下文中加载同一实体
            using var dbContext2 = new DbContext();
            using var dbContext3 = new DbContext();

            var entity1 = dbContext2.Query<TestEntity>().First(x => x.Id == originalEntity.Id);
            var entity2 = dbContext3.Query<TestEntity>().First(x => x.Id == originalEntity.Id);

            // 验证初始状态
            entity1.Name.ShouldBe("OriginalName");
            entity1.Value.ShouldBe(100);
            entity2.Name.ShouldBe("OriginalName");
            entity2.Value.ShouldBe(100);

            // 第一个上下文修改Name字段
            entity1.Name = "ModifiedByContext1";
            await dbContext2.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 第二个上下文修改Value字段 - 不应该冲突
            entity2.Value = 200;

            // 这个保存应该成功，因为修改的是不同字段
            await Should.NotThrowAsync(async () =>
            {
                await dbContext3.SaveChanges();
            });

            // 验证最终状态
            using var dbContext4 = new DbContext();
            var finalEntity = dbContext4.Query<TestEntity>().First(x => x.Id == originalEntity.Id);
            finalEntity.Name.ShouldBe("ModifiedByContext1");
            finalEntity.Value.ShouldBe(200);
        }

        [TestMethod]
        public async Task concurrent_modification_entity_deleted_should_throw_optimistic_concurrency_exception()
        {
            // 准备测试数据
            var originalEntity = new TestEntity
            {
                Name = "ToBeDeleted",
                Value = 100
            };

            // 保存原始实体
            using var dbContext1 = new DbContext();
            dbContext1.Attach(originalEntity);
            await dbContext1.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 在两个不同的上下文中加载同一实体
            using var dbContext2 = new DbContext();
            using var dbContext3 = new DbContext();

            var entity1 = dbContext2.Query<TestEntity>().First(x => x.Id == originalEntity.Id);
            var entity2 = dbContext3.Query<TestEntity>().First(x => x.Id == originalEntity.Id);

            // 验证初始状态
            entity1.Name.ShouldBe("ToBeDeleted");
            entity2.Name.ShouldBe("ToBeDeleted");

            // 第一个上下文删除实体
            await DB.DeleteAsync<TestEntity>(entity1.Id, dbContext2);

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 第二个上下文尝试修改已删除的实体 - 应该抛出异常
            entity2.Name = "ModifiedAfterDelete";

            var exception = await Should.ThrowAsync<OptimisticConcurrencyException>(async () =>
            {
                await dbContext3.SaveChanges();
            });

            // 验证异常信息
            exception.EntityType.ShouldBe(typeof(TestEntity));
            exception.EntityId.ShouldBe(originalEntity.Id);
            exception.TheirModifiedOn.ShouldBe(default); // 实体被删除，所以TheirModifiedOn应该是default
            exception.ConflictingFields.ShouldBeEmpty(); // 删除冲突时没有字段冲突信息
        }

        [TestMethod]
        public async Task batch_save_with_concurrent_modification_should_auto_merge()
        {
            // 准备测试数据
            var entities = new[]
            {
                new TestEntity { Name = "Entity1", Value = 100 },
                new TestEntity { Name = "Entity2", Value = 200 },
                new TestEntity { Name = "Entity3", Value = 300 }
            };

            // 保存原始实体集合
            using var dbContext = new DbContext();
            dbContext.Attach(entities);
            await dbContext.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 在两个不同的上下文中加载实体
            using var dbContext1 = new DbContext();
            using var dbContext2 = new DbContext();

            var entities1 = dbContext1.Query<TestEntity>().Where(x => x.Name.StartsWith("Entity")).OrderBy(x => x.Name).ToList();
            var entities2 = dbContext2.Query<TestEntity>().Where(x => x.Name.StartsWith("Entity")).OrderBy(x => x.Name).ToList();

            // 验证初始状态
            entities1.Count.ShouldBe(3);
            entities2.Count.ShouldBe(3);
            entities1[1].Name.ShouldBe("Entity2");
            entities2[1].Name.ShouldBe("Entity2");

            // 第一个上下文修改第二个实体的Value
            var entity1ToModify = entities1.First(x => x.Name == "Entity2");
            entity1ToModify.Value = 999;
            await dbContext1.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 第二个上下文批量修改所有实体的Name，包括被第一个上下文修改的实体
            // 这会导致Entity2出现并发修改，因为Name字段被我们修改，Value字段被他们修改
            // 但Name字段没有被修改，所以不会出现冲突，会被自动合并
            foreach (var entity in entities2)
            {
                entity.Name = entity.Name + "_Modified";
            }

            // 批量保存应该不会抛出异常
            await Should.NotThrowAsync(async () =>
            {
                await dbContext2.SaveChanges();
            });

            // 验证最终状态
            using var dbContext3 = new DbContext();
            var finalEntities = dbContext3.Query<TestEntity>().Where(x => x.Name.StartsWith("Entity")).OrderBy(x => x.Name).ToList();
            finalEntities.Count.ShouldBe(3);
            finalEntities[1].Name.ShouldBe("Entity2_Modified");
            finalEntities[1].Value.ShouldBe(999);
        }

        [TestMethod]
        public async Task save_only_with_concurrent_modification_should_throw_optimistic_concurrency_exception()
        {
            // 准备测试数据
            var originalEntity = new TestEntity
            {
                Name = "OriginalName",
                Value = 100,
                Enum = TestEntityEnum.Value1
            };

            // 保存原始实体
            using var dbContext1 = new DbContext();
            dbContext1.Attach(originalEntity);
            await dbContext1.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 在两个不同的上下文中加载同一实体
            using var dbContext2 = new DbContext();
            using var dbContext3 = new DbContext();

            var entity1 = dbContext2.Query<TestEntity>().First(x => x.Id == originalEntity.Id);
            var entity2 = dbContext3.Query<TestEntity>().First(x => x.Id == originalEntity.Id);

            // 验证初始状态
            entity1.Name.ShouldBe("OriginalName");
            entity2.Name.ShouldBe("OriginalName");
            entity1.Value.ShouldBe(100);
            entity2.Value.ShouldBe(100);

            // 第一个上下文修改Name字段
            entity1.Name = "ModifiedByContext1";
            await dbContext2.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 第二个上下文使用SaveOnly修改Name字段 - 应该抛出异常
            entity2.Name = "ModifiedByContext2";
            entity2.Value = 999; // 这个字段不在SaveOnly范围内，所以不影响冲突检测

            var exception = await Should.ThrowAsync<OptimisticConcurrencyException>(async () =>
            {
                await DB.SaveOnlyAsync(entity2, x => new { x.Name }, dbContext3);
            });

            // 验证异常信息
            exception.EntityType.ShouldBe(typeof(TestEntity));
            exception.EntityId.ShouldBe(originalEntity.Id);
            exception.ConflictingFields.Any(f => f.FieldName == nameof(TestEntity.Name)).ShouldBeTrue();

            // 验证冲突字段的值
            var nameConflict = exception.ConflictingFields.First(f => f.FieldName == nameof(TestEntity.Name));
            nameConflict.BaseValue.ShouldBe("OriginalName");
            nameConflict.OurValue.ShouldBe("ModifiedByContext2");
            nameConflict.TheirValue.ShouldBe("ModifiedByContext1");
        }

        [TestMethod]
        public async Task conflict_field_info_should_contain_correct_values()
        {
            // 准备测试数据
            var originalEntity = new TestEntity
            {
                Name = "BaseValue",
                Value = 100
            };

            // 保存原始实体
            using var dbContext1 = new DbContext();
            dbContext1.Attach(originalEntity);
            await dbContext1.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 在两个不同的上下文中加载同一实体
            using var dbContext2 = new DbContext();
            using var dbContext3 = new DbContext();

            var entity1 = dbContext2.Query<TestEntity>().First(x => x.Id == originalEntity.Id);
            var entity2 = dbContext3.Query<TestEntity>().First(x => x.Id == originalEntity.Id);

            // 验证初始状态
            entity1.Name.ShouldBe("BaseValue");
            entity2.Name.ShouldBe("BaseValue");

            // 第一个上下文修改Name字段为"TheirValue"
            entity1.Name = "TheirValue";
            await dbContext2.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 第二个上下文修改同一字段为"OurValue" - 应该抛出异常
            entity2.Name = "OurValue";

            var exception = await Should.ThrowAsync<OptimisticConcurrencyException>(async () =>
            {
                await dbContext3.SaveChanges();
            });

            // 验证冲突字段信息的正确性
            var nameConflict = exception.ConflictingFields.First(f => f.FieldName == nameof(TestEntity.Name));
            nameConflict.BaseValue.ShouldBe("BaseValue");  // 原始值
            nameConflict.OurValue.ShouldBe("OurValue");    // 我们的修改值
            nameConflict.TheirValue.ShouldBe("TheirValue"); // 他们的修改值
            nameConflict.FieldType.ShouldBe(typeof(string));
        }

        [TestMethod]
        public async Task multiple_field_conflicts_should_be_detected()
        {
            // 准备测试数据
            var originalEntity = new TestEntity
            {
                Name = "OriginalName",
                Value = 100,
                Enum = TestEntityEnum.Default
            };

            // 保存原始实体
            using var dbContext1 = new DbContext();
            dbContext1.Attach(originalEntity);
            await dbContext1.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 在两个不同的上下文中加载同一实体
            using var dbContext2 = new DbContext();
            using var dbContext3 = new DbContext();

            var entity1 = dbContext2.Query<TestEntity>().First(x => x.Id == originalEntity.Id);
            var entity2 = dbContext3.Query<TestEntity>().First(x => x.Id == originalEntity.Id);

            // 验证初始状态
            entity1.Name.ShouldBe("OriginalName");
            entity1.Value.ShouldBe(100);
            entity1.Enum.ShouldBe(TestEntityEnum.Default);
            entity2.Name.ShouldBe("OriginalName");
            entity2.Value.ShouldBe(100);
            entity2.Enum.ShouldBe(TestEntityEnum.Default);

            // 第一个上下文修改多个字段
            entity1.Name = "TheirName";
            entity1.Value = 999;
            entity1.Enum = TestEntityEnum.Value1;
            await dbContext2.SaveChanges();

            // 确保有足够的时间差异
            await Task.Delay(50);

            // 第二个上下文修改相同的多个字段 - 应该检测到多个冲突
            entity2.Name = "OurName";
            entity2.Value = 888;
            entity2.Enum = TestEntityEnum.Value2;

            var exception = await Should.ThrowAsync<OptimisticConcurrencyException>(async () =>
            {
                await dbContext3.SaveChanges();
            });

            // 验证检测到多个冲突字段
            exception.ConflictingFields.Count.ShouldBeGreaterThanOrEqualTo(3);
            exception.ConflictingFields.Any(f => f.FieldName == nameof(TestEntity.Name)).ShouldBeTrue();
            exception.ConflictingFields.Any(f => f.FieldName == nameof(TestEntity.Value)).ShouldBeTrue();
            exception.ConflictingFields.Any(f => f.FieldName == nameof(TestEntity.Enum)).ShouldBeTrue();

            // 验证每个冲突字段的值
            var nameConflict = exception.ConflictingFields.First(f => f.FieldName == nameof(TestEntity.Name));
            nameConflict.BaseValue.ShouldBe("OriginalName");
            nameConflict.OurValue.ShouldBe("OurName");
            nameConflict.TheirValue.ShouldBe("TheirName");

            var valueConflict = exception.ConflictingFields.First(f => f.FieldName == nameof(TestEntity.Value));
            valueConflict.BaseValue.ShouldBe(100);
            valueConflict.OurValue.ShouldBe(888);
            valueConflict.TheirValue.ShouldBe(999);

            var enumConflict = exception.ConflictingFields.First(f => f.FieldName == nameof(TestEntity.Enum));
            enumConflict.BaseValue.ShouldBe(TestEntityEnum.Default);
            enumConflict.OurValue.ShouldBe(TestEntityEnum.Value2);
            enumConflict.TheirValue.ShouldBe(TestEntityEnum.Value1);
        }
    }
}
