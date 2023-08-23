using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Bson;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestDynamicObject
    {
        //[TestMethod]
        //public async Task create_anonymous_object_should_work()
        //{
        //    var dbContext = new DbContext();
        //    var tempName = Guid.NewGuid().ToString();
        //    var data = new TableAnonymousData()
        //    {
        //        TableType = tempName,
        //        Data = new { customerType = "VIP", customerName = tempName }
        //    };
        //    dbContext.Attach(data);
        //    await dbContext.CommitAsync();
        //    dbContext.Dispose();
        //    dbContext = new DbContext();
        //    dbContext.Queryable<TableAnonymousData>().FirstOrDefault(x => x.TableType == tempName).ShouldNotBeNull();
        //}

        [TestMethod]
        public async Task create_json_object_should_work()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TableData>();
            dbContext.Dispose();
            dbContext = new DbContext();
            var tempName = Guid.NewGuid().ToString();
            var data = new TableData()
            {
                DataType = "object",
                Data = new JsonObject { { "customerType", "VIP" }, { "customerName", tempName }, { "test", new JsonObject { { "key", "value" } } } }
            };
            dbContext.Attach(data);
            var data2 = new TableData()
            {
                DataType = "array",
                Data = new JsonArray(new JsonObject { { "customerType", "VIP" }, { "customerName", tempName }, { "test", new JsonObject { { "key", "value" } } } }, new JsonArray(new JsonObject { { "customerType", "test" }, { "customerName", tempName }, { "test", new JsonObject { { "key", "value" } } } }))
            };
            dbContext.Attach(data2);
            var dataNull = new TableData()
            {
                DataType = "null",
                Data = null
            };
            dbContext.Attach(dataNull);
            var dataSimple = new TableData()
            {
                DataType = "simple",
                Data = "test"
            };
            dbContext.Attach(dataSimple);
            await dbContext.CommitAsync();
            dbContext.Dispose();
            dbContext = new DbContext();
            var item = dbContext.Queryable<TableData>().ToList();
            item.Count.ShouldBe(4);
            var objectData = item.First(x => x.DataType == "object");
            objectData.Data["customerType"].GetValue<string>().ShouldBe("VIP");
            objectData.Data.ShouldBeOfType<JsonObject>();
            var arrayData = item.First(x => x.DataType == "array");
            arrayData.Data.ShouldBeOfType<JsonArray>();
            arrayData.Data[1].ShouldBeOfType<JsonArray>();
            arrayData.Data[1][0]["customerType"].GetValue<string>().ShouldBe("test");
            var nullData = item.First(x => x.DataType == "null");
            nullData.Data.ShouldBeNull();
            var simpleData = item.First(x => x.DataType == "simple");
            simpleData.Data.GetValue<string>().ShouldBe("test");

        }

        [TestMethod]
        public async Task json_object_should_response_in_correct_format()
        {
            var dbContext = new DbContext();
            await dbContext.DeleteAsync<TableData>();
            dbContext.Dispose();
            dbContext = new DbContext();
            var tempName = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            var data = new TableData()
            {
                DataType = "object",
                Data = new JsonObject { { "int", 1 }, { "decimal", 1m }, { "datetime", now }, { "bool", true } }
            };
            dbContext.Attach(data);
            await dbContext.CommitAsync();
            dbContext.Dispose();
            dbContext = new DbContext();
            var item = dbContext.Queryable<TableData>().ToList();
            var objectData = item.First(x => x.DataType == "object");
            objectData.Data["int"].GetValue<int>().ShouldBe(1);
            objectData.Data["decimal"].GetValue<decimal>().ShouldBe(1m);
            objectData.Data["datetime"].GetValue<DateTime>().ShouldBe(now);
            objectData.Data["bool"].GetValue<bool>().ShouldBe(true);
            objectData.Data.ShouldBeOfType<JsonObject>();
        }


        [TestMethod]
        public async Task query_dynamic_object_should_work()
        {
            //var dbContext = new DbContext();
            //await dbContext.DeleteAsync<TableData>();
            //var data = new TableData()
            //{
            //    DataType = "object",
            //    Data = new JsonObject { { "customerType", "VIP" }, { "customerName", "1" }, { "test", new JsonObject { { "key", "value" } } } }
            //};
            //dbContext.Attach(data);
            //dbContext.Dispose();
            //dbContext = new DbContext();
            //var result = await dbContext.Find<TableData>().MatchExpression("{\n        '$eq': [\n          '$Data.customerType', 'VIP'\n        ]\n      }").ExecuteAsync();
            //result.ShouldNotBeNull().ShouldNotBeEmpty();
        }
    }

    public class TableData : EntityBase<TableData>
    {
        public string DataType { get; set; }
        public JsonNode Data { get; set; }
    }

    public class TableAnonymousData : EntityBase<TableAnonymousData>
    {
        public string TableType { get; set; }
        public object Data { get; set; }
    }
}
