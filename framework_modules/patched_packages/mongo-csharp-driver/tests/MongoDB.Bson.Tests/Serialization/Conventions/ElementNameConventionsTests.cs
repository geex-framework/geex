﻿/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class ElementNameConventionsTests
    {
        private class TestClass
        {
            public string FirstName { get; set; }
            public int Age { get; set; }
            public string _DumbName { get; set; }
            public string lowerCase { get; set; }
        }

        [Fact]
        public void TestMemberNameElementNameConvention()
        {
            var convention = new MemberNameElementNameConvention();
            var classMap = new BsonClassMap<TestClass>();
            convention.Apply(classMap.MapMember(x => x.FirstName));
            convention.Apply(classMap.MapMember(x => x.Age));
            convention.Apply(classMap.MapMember(x => x._DumbName));
            convention.Apply(classMap.MapMember(x => x.lowerCase));
            Assert.Equal("FirstName", classMap.GetMemberMap(x => x.FirstName).ElementName);
            Assert.Equal("Age", classMap.GetMemberMap(x => x.Age).ElementName);
            Assert.Equal("_DumbName", classMap.GetMemberMap(x => x._DumbName).ElementName);
            Assert.Equal("lowerCase", classMap.GetMemberMap(x => x.lowerCase).ElementName);
        }

        [Fact]
        public void TestCamelCaseElementNameConvention()
        {
            var convention = new CamelCaseElementNameConvention();
            var classMap = new BsonClassMap<TestClass>();
            convention.Apply(classMap.MapMember(x => x.FirstName));
            convention.Apply(classMap.MapMember(x => x.Age));
            convention.Apply(classMap.MapMember(x => x._DumbName));
            convention.Apply(classMap.MapMember(x => x.lowerCase));
            Assert.Equal("firstName", classMap.GetMemberMap(x => x.FirstName).ElementName);
            Assert.Equal("age", classMap.GetMemberMap(x => x.Age).ElementName);
            Assert.Equal("_DumbName", classMap.GetMemberMap(x => x._DumbName).ElementName);
            Assert.Equal("lowerCase", classMap.GetMemberMap(x => x.lowerCase).ElementName);
        }
    }
}
