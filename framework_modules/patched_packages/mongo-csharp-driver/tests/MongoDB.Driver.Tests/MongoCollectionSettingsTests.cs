/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoCollectionSettingsTests
    {
        [Fact]
        public void TestAll()
        {
            var settings = new MongoCollectionSettings
            {
                AssignIdOnInsert = true,
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.Acknowledged
            };

            Assert.Equal(true, settings.AssignIdOnInsert);
            Assert.Equal(ReadConcern.Majority, settings.ReadConcern);
            Assert.Same(ReadPreference.Primary, settings.ReadPreference);
            Assert.Same(WriteConcern.Acknowledged, settings.WriteConcern);
        }

        [Fact]
        public void TestAssignIdOnInsert()
        {
            var settings = new MongoCollectionSettings();
            Assert.Equal(false, settings.AssignIdOnInsert);

            var assignIdOnInsert = !settings.AssignIdOnInsert;
            settings.AssignIdOnInsert = assignIdOnInsert;
            Assert.Equal(assignIdOnInsert, settings.AssignIdOnInsert);

            settings.Freeze();
            Assert.Equal(assignIdOnInsert, settings.AssignIdOnInsert);
            Assert.Throws<InvalidOperationException>(() => { settings.AssignIdOnInsert = assignIdOnInsert; });
        }

        [Fact]
        public void TestClone()
        {
            // set everything to non default values to test that all settings are cloned
            var settings = new MongoCollectionSettings
            {
                AssignIdOnInsert = !MongoDefaults.AssignIdOnInsert,
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Secondary,
                WriteConcern = WriteConcern.W2
            };
            var clone = settings.Clone();
            Assert.True(clone.Equals(settings));
        }

        [Fact]
        public void TestConstructor()
        {
            var settings = new MongoCollectionSettings();
            Assert.Equal(false, settings.AssignIdOnInsert);
            Assert.Equal(null, settings.ReadConcern);
            Assert.Equal(null, settings.ReadPreference);
            Assert.Equal(null, settings.WriteConcern);
        }

        [Fact]
        public void TestEquals()
        {
            var settings = new MongoCollectionSettings();
            var clone = settings.Clone();
            Assert.True(clone.Equals(settings));

            settings.Freeze();
            clone.Freeze();
            Assert.True(clone.Equals(settings));

            clone = settings.Clone();
            clone.AssignIdOnInsert = !clone.AssignIdOnInsert;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ReadConcern = ReadConcern.Majority;
            Assert.False(clone.Equals(settings));
            clone = settings.Clone();
            clone.ReadPreference = ReadPreference.Secondary;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.WriteConcern = WriteConcern.W2;
            Assert.False(clone.Equals(settings));
        }

        [Fact]
        public void TestFeeze()
        {
            var settings = new MongoCollectionSettings();
            Assert.False(settings.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();

            settings.Freeze();
            Assert.True(settings.IsFrozen);
            Assert.Equal(hashCode, settings.GetHashCode());
            Assert.Equal(stringRepresentation, settings.ToString());
        }

        [Fact]
        public void TestFrozenCopy()
        {
            var settings = new MongoCollectionSettings();
            Assert.False(settings.IsFrozen);

            var frozenCopy = settings.FrozenCopy();
            Assert.False(settings.IsFrozen);
            Assert.True(frozenCopy.IsFrozen);
            Assert.NotSame(settings, frozenCopy);

            var secondFrozenCopy = frozenCopy.FrozenCopy();
            Assert.True(secondFrozenCopy.IsFrozen);
            Assert.Same(frozenCopy, secondFrozenCopy);
        }

        [Fact]
        public void TestReadConcern()
        {
            var settings = new MongoCollectionSettings();
            Assert.Equal(null, settings.ReadConcern);

            var readConcern = ReadConcern.Majority;
            settings.ReadConcern = readConcern;
            Assert.Equal(readConcern, settings.ReadConcern);

            settings.Freeze();
            Assert.Equal(readConcern, settings.ReadConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadConcern = readConcern; });
        }

        [Fact]
        public void TestReadPreference()
        {
            var settings = new MongoCollectionSettings();
            Assert.Equal(null, settings.ReadPreference);

            var readPreference = ReadPreference.Secondary;
            settings.ReadPreference = readPreference;
            Assert.Equal(readPreference, settings.ReadPreference);

            settings.Freeze();
            Assert.Equal(readPreference, settings.ReadPreference);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadPreference = readPreference; });
        }

        [Fact]
        public void TestWriteConcern()
        {
            var settings = new MongoCollectionSettings();
            Assert.Equal(null, settings.WriteConcern);

            var writeConcern = WriteConcern.W2;
            settings.WriteConcern = writeConcern;
            Assert.Equal(writeConcern, settings.WriteConcern);

            settings.Freeze();
            Assert.Equal(writeConcern, settings.WriteConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.WriteConcern = writeConcern; });
        }
    }
}
