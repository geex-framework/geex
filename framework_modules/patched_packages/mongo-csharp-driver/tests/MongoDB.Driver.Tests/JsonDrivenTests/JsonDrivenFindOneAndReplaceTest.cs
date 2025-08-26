﻿/* Copyright 2018-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenFindOneAndReplaceTest : JsonDrivenCollectionTest
    {
        // private fields
        private FilterDefinition<BsonDocument> _filter;
        private FindOneAndReplaceOptions<BsonDocument> _options = new FindOneAndReplaceOptions<BsonDocument>();
        private BsonDocument _replacement;
        private BsonDocument _result;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenFindOneAndReplaceTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(collection, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "collectionOptions", "arguments", "result");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
            _result.Should().Be(_expectedResult.AsBsonDocument);
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _result = _collection.FindOneAndReplace(_filter, _replacement, _options, cancellationToken);
            }
            else
            {
                _result = _collection.FindOneAndReplace(_session, _filter, _replacement, _options, cancellationToken);
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _result = await _collection.FindOneAndReplaceAsync(_filter, _replacement, _options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _result = await _collection.FindOneAndReplaceAsync(_session, _filter, _replacement, _options, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = new BsonDocumentFilterDefinition<BsonDocument>(value.AsBsonDocument);
                    return;

                case "replacement":
                    _replacement = value.AsBsonDocument;
                    return;

                case "returnDocument":
                    _options.ReturnDocument = (ReturnDocument)Enum.Parse(typeof(ReturnDocument), value.AsString);
                    return;

                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;

                case "upsert":
                    _options.IsUpsert = value.ToBoolean();
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
