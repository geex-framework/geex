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

using System;
using System.Linq;
using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonBinaryDataTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_bytes_is_null(
            [Range(1, 2)] int overload)
        {
            var bytes = (byte[])null;

            Exception exception = null;
            switch (overload)
            {
                case 1: exception = Record.Exception(() => new BsonBinaryData(bytes)); break;
                case 2: exception = Record.Exception(() => new BsonBinaryData(bytes, BsonBinarySubType.Binary)); break;
            }

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("bytes");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_bytes_length_is_not_16_and_sub_type_is_uuid(
            [Values(BsonBinarySubType.UuidLegacy, BsonBinarySubType.UuidStandard)] BsonBinarySubType subType,
            [Values(0, 15, 17)] int length)
        {
            var bytes = new byte[length];

            Exception exception = Record.Exception(() => new BsonBinaryData(bytes, subType));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().StartWith($"Length must be 16, not {length}, when subType is {subType}.");
            e.ParamName.Should().Be("bytes");
        }

        [Fact]
        public void constructor_should_throw_when_sub_type_is_uuid_and_guid_representation_is_invalid()
        {
            var guid = Guid.Empty;
            var guidRepresentation = (GuidRepresentation)5;

            Exception exception = Record.Exception(() => new BsonBinaryData(guid, guidRepresentation));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().StartWith($"Invalid guidRepresentation: 5.");
            e.ParamName.Should().Be("guidRepresentation");
        }

        [Fact]
        public void TestCreateNull()
        {
            object obj = null;
            Assert.Throws<ArgumentNullException>(() => { BsonBinaryData.Create(obj); });
        }

        [Fact]
        public void TestGuidCSharpLegacy()
        {
            var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
            var binaryData = new BsonBinaryData(guid, GuidRepresentation.CSharpLegacy);
            var expected = new byte[] { 4, 3, 2, 1, 6, 5, 8, 7, 9, 10, 11, 12, 13, 14, 15, 16 };
            Assert.True(expected.SequenceEqual(binaryData.Bytes));
            Assert.Equal(BsonBinarySubType.UuidLegacy, binaryData.SubType);
        }

        [Fact]
        public void TestGuidPythonLegacy()
        {
            var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
            var binaryData = new BsonBinaryData(guid, GuidRepresentation.PythonLegacy);
            var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            Assert.True(expected.SequenceEqual(binaryData.Bytes));
            Assert.Equal(BsonBinarySubType.UuidLegacy, binaryData.SubType);
        }

        [Fact]
        public void TestGuidJavaLegacy()
        {
            var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
            var binaryData = new BsonBinaryData(guid, GuidRepresentation.JavaLegacy);
            var expected = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1, 16, 15, 14, 13, 12, 11, 10, 9 };
            Assert.True(expected.SequenceEqual(binaryData.Bytes));
            Assert.Equal(BsonBinarySubType.UuidLegacy, binaryData.SubType);
        }
    }
}
