using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestObjectIdConvert
    {
        [TestMethod]
        public async Task object_id_convert_works()
        {
            var id = ObjectId.GenerateNewId();
            var id2 = ObjectId.GenerateNewId();
            var id3 = ObjectId.GenerateNewId();
            var idList = new List<ObjectId> { id, id2, id3 };
            var idList2 = idList.Select(x => x.ToString()).ToList();
            var idList21 = idList.Cast<string>().ToList();
            var idList3 = idList2.Select(x => ObjectId.Parse(x)).ToList();
            var idList31 = idList21.Select(x => ObjectId.Parse(x)).ToList();
            Assert.AreEqual(idList.Count, idList3.Count);
            Assert.AreEqual(idList.Count, idList31.Count);
            for (int i = 0; i < idList.Count; i++)
            {
                Assert.AreEqual(idList[i], idList3[i]);
                Assert.AreEqual(idList[i], idList31[i]);
            }
        }
    }
}
