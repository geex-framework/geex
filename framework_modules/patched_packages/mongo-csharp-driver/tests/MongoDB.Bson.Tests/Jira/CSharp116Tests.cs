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

using System.IO;
using MongoDB.Bson.IO;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp116
{
    public class CSharp116Tests
    {
        [Fact]
        public void TestFlushAndClose()
        {
            var stream = new MemoryStream();
            using (var bsonWriter = new BsonBinaryWriter(stream))
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteEndDocument();
                bsonWriter.Flush();
                bsonWriter.Close();
            }
        }

        [Fact]
        public void Test1Chunk()
        {
            var stream = new MemoryStream();
            using (var bsonWriter = new BsonBinaryWriter(stream))
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteBytes("Data", new byte[16 * 1024 - 16]);
                bsonWriter.WriteEndDocument();
                bsonWriter.Close();
            }
        }

        [Fact]
        public void Test1ChunkMinus1()
        {
            var stream = new MemoryStream();
            using (var bsonWriter = new BsonBinaryWriter(stream))
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteBytes("Data", new byte[16 * 1024 - 17]);
                bsonWriter.WriteEndDocument();
                bsonWriter.Close();
            }
        }

        [Fact]
        public void Test1ChunkPlus1()
        {
            var stream = new MemoryStream();
            using (var bsonWriter = new BsonBinaryWriter(stream))
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteBytes("Data", new byte[16 * 1024 - 15]);
                bsonWriter.WriteEndDocument();
                bsonWriter.Close();
            }
        }
    }
}
