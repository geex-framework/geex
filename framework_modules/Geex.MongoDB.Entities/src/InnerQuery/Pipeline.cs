// The MIT License (MIT)
//
// Copyright (c) 2015 Pathmatics, Inc
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;

using Geex.MongoDB.Entities.InnerQuery;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Entities.Utilities;

using ExpressionType = System.Linq.Expressions.ExpressionType;

namespace MongoDB.Entities.InnerQuery
{
    [Flags]
    internal enum PipelineResultType
    {
        Enumerable = 0x001,
        Aggregation = 0x002,
        Grouped = 0x004,
        OneResultFromEnumerable = 0x008,
        OrDefault = 0x010,
        First = 0x020,
        Single = 0x040,
        Any = 0x082,
    }

    internal class PipelineStage
    {
        public string PipelineOperator;
        public BsonValue Operation;
        public bool GroupNeedsCleanup;
    }

    internal class MongoPipeline<TDocType>
    {
        static Type ListType = typeof(List<>);
        static Type pipelineDocumentType = typeof(PipelineDocument<>);

        private Action<string> _loggingDelegate;
        internal const string PIPELINE_DOCUMENT_RESULT_NAME = "_result_";
        internal const string JOINED_DOC_PROPERTY_NAME = "__JOINED__";

        private JsonWriterSettings _jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict, Indent = true, NewLineChars = "\r\n" };

        private List<PipelineStage> _pipeline = [];
        private PipelineResultType _lastPipelineOperation = PipelineResultType.Enumerable;
        private IMongoCollection<TDocType> _collection;
        private readonly IClientSessionHandle _session;
        private readonly AggregateOptions _aggregateOptions;
        private int _nextUniqueVariableId;
        /// <summary>
        /// Whether the current working pipeline document has the current working value in the _result_ field or at the root.
        /// </summary>
        private bool _currentPipelineDocumentUsesResultHack;

        /// <summary>
        /// Dictionary to help resolve lambda parameter mongo field names in nested Select/SelectMany statements.
        /// .SelectMany(c => c.SubArray.Select(d => new { c.SomeRootElement, d.SomeChildElement }))
        /// </summary>
        private Dictionary<string, string> _subSelectParameterPrefixes = new Dictionary<string, string>();

        ///// <summary>
        ///// Custom converters to use when utilizing Json.net for deserialization
        ///// </summary>
        //private JsonConverter[] _customConverters = {
        //    new GroupingConverter(typeof(TDocType)),
        //    new MongoBsonConverter(),
        //};

        private readonly Dictionary<ExpressionType, string> NodeToMongoQueryBuilderFuncDict = new Dictionary<ExpressionType, string> {
            {ExpressionType.Equal, "$eq"},
            {ExpressionType.NotEqual, "$ne"},
            {ExpressionType.GreaterThan, "$gt"},
            {ExpressionType.GreaterThanOrEqual, "$gte"},
            {ExpressionType.LessThan, "$lt"},
            {ExpressionType.LessThanOrEqual, "$lte"},
        };

        private readonly Dictionary<ExpressionType, Func<FieldDefinition<TDocType>, int, FilterDefinition<TDocType>>> NodeToMongoQueryBuilderArrayLengthFuncDict =
            new Dictionary<ExpressionType, Func<FieldDefinition<TDocType>, int, FilterDefinition<TDocType>>> {
                {ExpressionType.Equal, Builders<TDocType>.Filter.Size},
                {ExpressionType.GreaterThan, Builders<TDocType>.Filter.SizeGt},
                {ExpressionType.GreaterThanOrEqual, Builders<TDocType>.Filter.SizeGte},
                {ExpressionType.LessThan, Builders<TDocType>.Filter.SizeLt},
                {ExpressionType.LessThanOrEqual, Builders<TDocType>.Filter.SizeLte},
            };

        private readonly Dictionary<ExpressionType, string> NodeToMongoBinaryOperatorDict = new Dictionary<ExpressionType, string> {
            {ExpressionType.Equal, "$eq"},
            {ExpressionType.NotEqual, "$ne"},
            {ExpressionType.AndAlso, "$and"},
            {ExpressionType.OrElse, "$or"},
            {ExpressionType.GreaterThan, "$gt"},
            {ExpressionType.GreaterThanOrEqual, "$gte"},
            {ExpressionType.LessThan, "$lt"},
            {ExpressionType.LessThanOrEqual, "$lte"},
            {ExpressionType.Add, "$add"},
            {ExpressionType.Subtract, "$subtract"},
            {ExpressionType.Multiply, "$multiply"},
            {ExpressionType.Divide, "$divide"},
            {ExpressionType.Modulo, "$mod"},
        };

        private readonly Dictionary<string, string> NodeToMongoAggregationOperatorDict = new Dictionary<string, string> {
            {"Sum", "sum"},
            {"Min", "min"},
            {"Max", "max"},
            {"Average", "avg"},
            {"First", "first"},
            {"Last", "last"},
            {"Count", "sum"},
            {"Select", "push"},
        };

        private readonly Dictionary<string, string> NodeToMongoDateOperatorDict = new Dictionary<string, string> {
            {"Year", "$year"},
            {"Month", "$month"},
            {"Day", "$dayOfMonth"},
            {"Hour", "$hour"},
            {"Minute", "$minute"},
            {"Second", "$second"},
            {"Millisecond", "$millisecond"},
            {"DayOfWeek", "$dayOfWeek"},
            {"DayOfYear", "$dayOfYear"},
        };

        // 新增：数学函数映射
        private readonly Dictionary<string, string> MathFunctionToMongoOperatorDict = new Dictionary<string, string> {
            {"Abs", "$abs"},
            {"Ceiling", "$ceil"},
            {"Floor", "$floor"},
            {"Round", "$round"},
            {"Sqrt", "$sqrt"},
            {"Pow", "$pow"},
            {"Log", "$log"},
            {"Log10", "$log10"},
            {"Exp", "$exp"},
            {"Sin", "$sin"},
            {"Cos", "$cos"},
            {"Tan", "$tan"},
            {"Asin", "$asin"},
            {"Acos", "$acos"},
            {"Atan", "$atan"},
            {"Sinh", "$sinh"},
            {"Cosh", "$cosh"},
            {"Tanh", "$tanh"},
            {"Asinh", "$asinh"},
            {"Acosh", "$acosh"},
            {"Atanh", "$atanh"},
            {"Min", "$min"},
            {"Max", "$max"},
        };

        // 新增：字符串函数映射
        private readonly Dictionary<string, string> StringFunctionToMongoOperatorDict = new Dictionary<string, string> {
            {"Trim", "$trim"},
            {"TrimStart", "$ltrim"},
            {"TrimEnd", "$rtrim"},
            {"Replace", "$replaceAll"},
            {"Split", "$split"},
            {"Join", "$concat"},
        };

        // 新增：数组函数映射
        private readonly Dictionary<string, string> ArrayFunctionToMongoOperatorDict = new Dictionary<string, string> {
            {"Reverse", "$reverseArray"},
            {"Sort", "$sortArray"},
            {"Slice", "$slice"},
            {"Zip", "$zip"},
            {"Range", "$range"},
        };

        /// <summary>Constructs a new MongoPipeline from a typed MongoCollection</summary>
        public MongoPipeline(IMongoCollection<TDocType> collection, IClientSessionHandle session, AggregateOptions aggregateOptions, Action<string> loggingDelegate)
        {
            _loggingDelegate = loggingDelegate;
            _collection = collection;
            _session = session;
            _aggregateOptions = aggregateOptions;
        }

        /// <summary>Log a string to the logging delegate</summary>
        private void LogLine(string s)
        {
            _loggingDelegate?.Invoke(s + Environment.NewLine);
        }

        /// <summary>Log a newline to the logging delegate</summary>
        private void LogLine()
        {
            LogLine(Environment.NewLine);
        }

        /// <summary>
        /// Adds a stage to our aggregation pipeline
        /// </summary>
        /// <param name="pipelineOperatorName">The operation name ($match, $group, etc)</param>
        /// <param name="operation">See MongoDB reference.  This is the operation to perform.</param>
        /// <param name="insertBefore">Pipeline stage to insert this stage before.  Defaults to the end.</param>
        public PipelineStage AddToPipeline(string pipelineOperatorName, BsonValue operation, int insertBefore = Int32.MaxValue)
        {
            var newStage = new PipelineStage
            {
                PipelineOperator = pipelineOperatorName,
                Operation = operation
            };

            _pipeline.Insert(Math.Min(insertBefore, _pipeline.Count()), newStage);
            return newStage;
        }

        /// <summary>
        /// Gets the name of the Mongo Field that the specified MemberExpression maps to.
        /// </summary>
        string GetMongoFieldNameInMatchStage(Expression expression, bool isNamedProperty)
        {
            // Don't support querying property members on DateTime in a $match stage
            if (expression is MemberExpression memberExp && (memberExp.Expression.Type == typeof(DateTime) ||
                                                             memberExp.Expression.Type == typeof(DateTimeOffset)))
            {
                throw new InvalidQueryException($"Can't access properties on {memberExp.Expression.Type.Name} in $match stage.");
            }

            return GetMongoFieldName(expression, isNamedProperty);
        }

        /// <summary>Gets the Mongo field name that the specified MemberInfo maps to.</summary>
        private string GetMongoFieldName(MemberInfo member)
        {
            // Get the BsonElementAttribute that MIGHT be decorating the field/property we're accessing
            var bsonElementAttribute = (BsonElementAttribute)member.GetCustomAttributes(typeof(BsonElementAttribute), true).SingleOrDefault();
            if (bsonElementAttribute != null)
                return bsonElementAttribute.ElementName;

            // Get the BsonIdAttribute that MIGHT be decorating the field/property we're accessing
            var bsonIdAttribute = (BsonIdAttribute)member.GetCustomAttributes(typeof(BsonIdAttribute), true).SingleOrDefault();
            if (bsonIdAttribute != null)
                return "_id";

            // IGrouping.Key maps to to the "_id" resulting from a $group
            if (member.DeclaringType != null && member.DeclaringType.Name == "IGrouping`2")
                return "_id";

            // TODO BUGBUG (Issue #8): Do a better fix for htis
            if (member.Name == "Id")
                return "_id";

            // At this point, we should just use the member name
            return member.Name;
        }

        /// <summary>
        /// Gets the name of the Mongo Field that the specified MemberExpression maps to.
        /// </summary>
        private string GetMongoFieldName(Expression expression, bool isNamedProperty)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                // A field that's an enum goes through a cast to an int.
                return GetMongoFieldName(((UnaryExpression)expression).Operand, isNamedProperty);
            }

            if (expression is MemberExpression memberExp)
            {
                // Special case!!
                // As of MongoDB 3.6, Mongo can treat the ObjectId as a DateTime.  We'll support this on the .Net side
                // via access on the CreationTime operation.  We'll just eat the property access.
                if (ExpressionIsObjectIdCreationTime(memberExp))
                    return GetMongoFieldName(memberExp.Expression, isNamedProperty);

                // Handle referencing the Value property on a Nullable<> type
                if (memberExp.Member.Name == "Value" && memberExp.Expression.Type.Name == "Nullable`1")
                {
                    // On input of 'foo.Value', ignore the 'Value' property and run this method again on 'foo'
                    return GetMongoFieldName(memberExp.Expression, isNamedProperty);
                }

                isNamedProperty = true;

                // We might have a nested MemberExpression like c.Key.Name
                // So recurse to build the whole mongo field name
                string prefix = "";
                if (memberExp.Expression is MemberExpression)
                {
                    prefix = GetMongoFieldName(memberExp.Expression, isNamedProperty) + '.';
                }
                else if (memberExp.Expression is ParameterExpression)
                {
                    var paramExpression = (ParameterExpression)memberExp.Expression;
                    _subSelectParameterPrefixes.TryGetValue(paramExpression.Name, out prefix);
                }
                else if (memberExp.Expression is MethodCallExpression)
                {
                    string fieldName = GetMongoFieldNameForMethodOnGrouping((MethodCallExpression)memberExp.Expression).AsString;

                    // Remove the '$'
                    prefix = (fieldName.StartsWith("$") ? fieldName.Substring(1) : fieldName) + '.';
                }

                // String.Length field not support in the Mongo Query syntax.
                if (memberExp.Member.Name == "Length" && memberExp.Member.DeclaringType == typeof(string))
                    throw new InvalidQueryException("Can't use String.Length in a legacy $match expression");

                var finalFieldName = prefix + GetMongoFieldName(memberExp.Member);
                if (_currentPipelineDocumentUsesResultHack)
                    finalFieldName = PIPELINE_DOCUMENT_RESULT_NAME + "." + finalFieldName;
                return finalFieldName;
            }

            if (expression.NodeType == ExpressionType.Parameter)
            {
                if (_subSelectParameterPrefixes.TryGetValue(((ParameterExpression)expression).Name, out string prefix))
                {
                    // The prefix will be something like "$foo."
                    // Remove the trailing . in this case since we're not going to make a subsequent member access.
                    return prefix.Substring(0, prefix.Length - 1);
                }

                return isNamedProperty ? PIPELINE_DOCUMENT_RESULT_NAME : null;
            }

            if (expression.NodeType == ExpressionType.Call)
            {
                string fieldName = GetMongoFieldNameForMethodOnGrouping((MethodCallExpression)expression).AsString;

                // Remove the '$'
                return fieldName.StartsWith("$") ? fieldName.Substring(1) : fieldName;
            }

            throw new InvalidQueryException("Can't get Mongo field name for expression type " + expression.NodeType);
        }

        /// <summary>
        /// Emits a pipeline stage for a GroupBy operation.
        /// By default, this stage ONLY includes the grouping key and does not include
        /// any aggregations (ie summing of grouped documents) or the grouped documents
        /// themselves.  Later pipeline stages may edit the output of this pipeline stage
        /// to include other fields.
        /// </summary>
        public void EmitPipelineStageForGroupBy(LambdaExpression lambdaExp)
        {
            // GroupBy supports the following modes:
            //    ParameterExpression: GroupBy(c => c)
            //    NewExpression:       GroupBy(c => new { c.Age, Name = c.FirstName })
            //    Other expressions:   GroupBy(c => c.Age + 1)

            // Handle the first case: GroupBy(c => c)
            if (lambdaExp.Body is ParameterExpression)
            {
                // This point was probably reached by doing something like:
                //   .Select(c => c.FirstName).GroupBy(c => c)

                // Perform the grouping on the _result_ document (which we'll assume we have)
                var pipelineOperation = new BsonDocument { new BsonElement("_id", "$" + PIPELINE_DOCUMENT_RESULT_NAME) };
                AddToPipeline("$group", pipelineOperation).GroupNeedsCleanup = true;
                _currentPipelineDocumentUsesResultHack = false;
                return;
            }

            // Handle an anonymous type: GroupBy(c => new { c.Age, Name = c.FirstName })
            if (lambdaExp.Body is NewExpression newExp)
            {
                var newExpProperties = newExp.Type.GetProperties();

                // Get the mongo field names for each property in the new {...}
                var fieldNames = newExp.Arguments
                    .Select((c, i) => new
                    {
                        KeyFieldName = newExpProperties[i].Name,
                        ValueMongoExpression = BuildMongoSelectExpression(c)
                    })
                    .Select(c => new BsonElement(c.KeyFieldName, c.ValueMongoExpression));

                // Perform the grouping on the multi-part key
                var pipelineOperation = new BsonDocument { new BsonElement("_id", new BsonDocument(fieldNames)) };
                AddToPipeline("$group", pipelineOperation).GroupNeedsCleanup = true;
                _currentPipelineDocumentUsesResultHack = false;
                return;
            }

            // Handle all other expression types
            var bsonValueExpression = BuildMongoSelectExpression(lambdaExp.Body);

            // Perform the grouping
            AddToPipeline("$group", new BsonDocument { new BsonElement("_id", bsonValueExpression) }).GroupNeedsCleanup = true;
            _currentPipelineDocumentUsesResultHack = false;
        }

        /// <summary>
        /// Gets a bson value from a constant.  Handles int, long, bool, and treats all other types as string
        /// </summary>
        private BsonValue GetBsonValueFromObject(object obj)
        {
            if (obj is int || obj is Enum)
                return new BsonInt32((int)obj);

            if (obj is long l)
                return new BsonInt64(l);

            if (obj is bool b)
                return new BsonBoolean(b);

            if (obj is double d)
                return new BsonDouble(d);

            if (obj is decimal dc)
                return new BsonDecimal128(dc);

            if (obj is DateTime date)
                return new BsonDateTime(date);

            if (obj is DateTimeOffset dateOffset)
                return new BsonDateTime(dateOffset.DateTime);

            if (obj is TimeSpan timeSpan)
                return BsonValue.Create(timeSpan.Ticks);

            if (obj == null)
                return BsonNull.Value;

            if (obj is string s)
            {
                return new BsonString(s);
            }
            if (obj is IStringPresentation stringObj)
                return new BsonString(stringObj.ToString());

            if (obj is Guid || obj is ObjectId)
                return BsonValue.Create(obj);

            if (TypeSystem.FindIEnumerable(obj.GetType()) != null)
            {
                var bsonArray = new BsonArray();
                foreach (var element in (IEnumerable)obj)
                    bsonArray.Add(GetBsonValueFromObject(element));

                return bsonArray;
            }

            throw new InvalidQueryException("Can't convert type " + obj.GetType().Name + " to BsonValue");
        }

        /// <summary>Returns true iff the Expression specified is for ObjectId.CreationTime</summary>
        bool ExpressionIsObjectIdCreationTime(Expression expression)
        {
            return expression is MemberExpression memberExp
                && memberExp.Member.Name == nameof(ObjectId.CreationTime)
                && memberExp.Expression.Type == typeof(ObjectId);
        }

        /// <summary>
        /// Builds an FilterDefinition from a given expression for use in a $match stage.
        /// We build an FilterDefinition rather than a BsonValue simply as a shortcut.
        /// It's easier to build an FilterDefinition and then call .ToBsonDocument on the result.
        /// </summary>
        /// <param name="expression">Expression to build a query for</param>
        /// <param name="isLambdaParamResultHack">
        /// Required for proper query sematics in $elemmatch expressions.
        ///
        /// Whether a reference to a lambda parameter in the expression should be treated as a reference
        /// to the field named PIPELINE_DOCUMENT_RESULT_NAME.  Generally this will be true.  This the case
        /// for something like this:
        /// .Select(c => c.Age)
        /// .Where(c => c > 15)
        /// In this case the variable 'c' is referring to the _result_ field in the document
        ///
        /// Pass in false when dealing with a sub-predicate like this:
        /// .Where(c => c.SubArrayOfInts.Any(d => d == 15))
        /// In this case the variable 'd' is is referring to a sub-item in the nested array and
        /// requires different syntax of any $elemmatch expressions.
        ///
        /// TODO: We should do a better job here entirely.  We currently don't do a good job
        /// of tracking our lambda params through the full Where call.  For example, this is undefined:
        /// .Where(c => c.SubArray.Any(d => d.Value == c.SomeOtherLocalProperty))
        /// </param>
        private FilterDefinition<TDocType> BuildMongoWhereExpressionAsQuery(Expression expression, bool isLambdaParamResultHack)
        {
            // C# doesn't support > and >= on System.String
            // So we have our own db function to support this.
            if (expression is MethodCallExpression gtCallExp
                && gtCallExp.Arguments.Count == 2
                && (gtCallExp.Arguments[0] is ConstantExpression || gtCallExp.Arguments[1] is ConstantExpression)
                && (gtCallExp.Arguments[0] is MemberExpression || gtCallExp.Arguments[1] is MemberExpression)
                && gtCallExp.Method.DeclaringType.FullName == "MongoLinqPlusPlus.MongoFunctions"
                && (gtCallExp.Method.Name == "GreaterThan" || gtCallExp.Method.Name == "GreaterThanOrEqual"))
            {

                MemberExpression leftExp;
                ConstantExpression rightExp;
                var expType = gtCallExp.Method.Name == "GreaterThan" ? ExpressionType.GreaterThan : ExpressionType.GreaterThanOrEqual;

                // Our Mongo syntax only supports the constant on the RHS.
                if (gtCallExp.Arguments[1].NodeType == ExpressionType.Constant)
                {
                    // Constant is on the RHS - easy
                    leftExp = (MemberExpression)gtCallExp.Arguments[0];
                    rightExp = (ConstantExpression)gtCallExp.Arguments[1];
                }
                else
                {
                    // Constant is on the LHS - flip the expression
                    expType = expType.Flip();
                    leftExp = (MemberExpression)gtCallExp.Arguments[1];
                    rightExp = (ConstantExpression)gtCallExp.Arguments[0];
                }

                // The left side of the operator MUST be a mongo field name
                string mongoFieldName = GetMongoFieldNameInMatchStage(leftExp, isLambdaParamResultHack);

                // Build the Mongo expression for the right side of the binary operator
                BsonValue rightValue = BuildMongoWhereExpressionAsBsonValue(rightExp, isLambdaParamResultHack);

                // Retrieve the function (like Builders<TDocType>.Filter.EQ) that we'll use to generate our mongo query
                var queryOperator = NodeToMongoQueryBuilderFuncDict[expType];

                // Generate the query and return it as a new BsonDocument
                var queryDoc = new BsonDocument(queryOperator, rightValue);
                if (mongoFieldName != null)
                    queryDoc = new BsonDocument(mongoFieldName, queryDoc);

                return new BsonDocumentFilterDefinition<TDocType>(queryDoc);
            }

            // Handle binary operators (&&, ==, >, etc)
            if (expression is BinaryExpression binExp)
            {
                // Very special case.  Support range queries on Object ID's by date:
                // .Where(c => c.ObjectId.CreationTime > new DateTime(2018,1,1)
                // We want to get this exactly right so we can take advantage of an index on ObjectId.
                // If we project our ObjectId into a date, then we'll blow the index.
                if ((binExp.NodeType == ExpressionType.GreaterThan
                    || binExp.NodeType == ExpressionType.GreaterThanOrEqual
                    || binExp.NodeType == ExpressionType.LessThan
                    || binExp.NodeType == ExpressionType.LessThanOrEqual
                    || binExp.NodeType == ExpressionType.Equal)
                    && (ExpressionIsObjectIdCreationTime(binExp.Left) || ExpressionIsObjectIdCreationTime(binExp.Right))
                    && (binExp.Left.NodeType == ExpressionType.Constant || binExp.Right.NodeType == ExpressionType.Constant))
                {
                    // We now have an expression in this form:
                    //     comparisonOperator(LHS, RHS)
                    // that can take two forms:
                    //     1) comparisonOperator(ObjectId.CreationDate, Constant)
                    //     2) comparisonOperator(Constant, ObjectId.CreationDate)

                    // We'll convert case 2 into case 1 to simplify this exercise for us.

                    ConstantExpression constExp;
                    MemberExpression memberExp;
                    ExpressionType comparisonType;


                    if (binExp.Left.NodeType == ExpressionType.Constant)
                    {
                        // Case 2 : Swap it around to look like case 1
                        constExp = (ConstantExpression)binExp.Left;
                        memberExp = (MemberExpression)binExp.Right;

                        comparisonType = binExp.NodeType.Flip();
                    }
                    else
                    {
                        // Case 1: nothing to swap
                        constExp = (ConstantExpression)binExp.Right;
                        memberExp = (MemberExpression)binExp.Left;
                        comparisonType = binExp.NodeType;
                    }

                    // Now our ObjectId.CreationDate is the LHS

                    // Pull the DateTime we're comparing our ObjectId.CreationDate to
                    var comparisonValue = (DateTime)constExp.Value;

                    var comparisonValueAsObjectIdMin = new ObjectId(comparisonValue, 0, 0, 0);
                    var comparisonValueAsObjectIdMax = new ObjectId(comparisonValue, 16777215, -1, 16777215);

                    // GT  memberExp > comparisonValueAsObjectIdMax
                    // GTE memberExp >= comparisonValueAsObjectIdMin
                    // LT memberExp < comparisonValueAsObjectIdMin
                    // LTE memberExp <= comparisonValueAsObjectIdMax
                    // EQ  memberExp >= comparisonValueAsObjectIdMin && memberExp <= comparisonValueAsObjectIdMax

                    string mongoFieldName = GetMongoFieldNameInMatchStage(memberExp, isLambdaParamResultHack);
                    if (comparisonType == ExpressionType.GreaterThan)
                        return Builders<TDocType>.Filter.Gt(mongoFieldName, comparisonValueAsObjectIdMax);
                    if (comparisonType == ExpressionType.GreaterThanOrEqual)
                        return Builders<TDocType>.Filter.Gte(mongoFieldName, comparisonValueAsObjectIdMin);
                    if (comparisonType == ExpressionType.LessThan)
                        return Builders<TDocType>.Filter.Lt(mongoFieldName, comparisonValueAsObjectIdMin);
                    if (comparisonType == ExpressionType.LessThanOrEqual)
                        return Builders<TDocType>.Filter.Lte(mongoFieldName, comparisonValueAsObjectIdMax);

                    // EQ
                    return Builders<TDocType>.Filter.And(Builders<TDocType>.Filter.Gte(mongoFieldName, comparisonValueAsObjectIdMin), Builders<TDocType>.Filter.Lte(mongoFieldName, comparisonValueAsObjectIdMax));
                }

                // If the LHS is an array length, then we use special operators.
                if (binExp.Left.NodeType == ExpressionType.ArrayLength)
                {
                    // Mongo doesn't natively support .Where(c => c.array.Length != 2)
                    // So translate that to           .Where(c => !(c.array.Length == 2))
                    var localNodeType = binExp.NodeType == ExpressionType.NotEqual ? ExpressionType.Equal : binExp.NodeType;
                    bool invert = binExp.NodeType == ExpressionType.NotEqual;

                    // Validate it's a supported operator
                    if (!NodeToMongoQueryBuilderArrayLengthFuncDict.Keys.Contains(localNodeType))
                        throw new InvalidQueryException("Unsupported binary operator '" + binExp.NodeType + "' on Array.Length");

                    // Validate we're comparing to a const
                    if (binExp.Right.NodeType != ExpressionType.Constant)
                        throw new InvalidQueryException("Array.Length can only be compared against a constant");

                    // Retrieve the function (like Builders<TDocType>.Filter.EQ) that we'll use to generate our mongo query
                    var queryFunc = NodeToMongoQueryBuilderArrayLengthFuncDict[localNodeType];

                    // Get our operands
                    int rhs = (int)((ConstantExpression)binExp.Right).Value;
                    string mongoFieldName = GetMongoFieldNameInMatchStage(((UnaryExpression)binExp.Left).Operand, isLambdaParamResultHack);
                    if (mongoFieldName == null)
                        throw new NotImplementedException("ExpressionType.ArrayLength on lambda parameters not supported. ie: .Where(c => c.SubArray.Any(d => d.Length > 5))");

                    // Generate the query
                    var query = queryFunc(mongoFieldName, rhs);

                    // Optionally invert our query
                    return invert ? Builders<TDocType>.Filter.Not(query) : query;
                }

                // If this binary expression is in our expression node type -> Mongo query dict, then use it
                if (NodeToMongoQueryBuilderFuncDict.Keys.Contains(expression.NodeType))
                {
                    // The left side of the operator MUST be a mongo field name
                    string mongoFieldName = GetMongoFieldNameInMatchStage(binExp.Left, isLambdaParamResultHack);

                    // Build the Mongo expression for the right side of the binary operator
                    BsonValue rightValue = BuildMongoWhereExpressionAsBsonValue(binExp.Right, isLambdaParamResultHack);

                    // Retrieve the function (like Builders<TDocType>.Filter.EQ) that we'll use to generate our mongo query
                    var queryOperator = NodeToMongoQueryBuilderFuncDict[expression.NodeType];

                    // Generate the query and return it as a new BsonDocument
                    var queryDoc = new BsonDocument(queryOperator, rightValue);
                    if (mongoFieldName != null)
                        queryDoc = new BsonDocument(mongoFieldName, queryDoc);
                    return new BsonDocumentFilterDefinition<TDocType>(queryDoc);
                }

                // Handle && and ||
                if (expression.NodeType == ExpressionType.AndAlso || expression.NodeType == ExpressionType.OrElse)
                {
                    // Build the Mongo expression for the left side of the binary operator
                    var leftQuery = BuildMongoWhereExpressionAsQuery(binExp.Left, isLambdaParamResultHack);
                    var rightQuery = BuildMongoWhereExpressionAsQuery(binExp.Right, isLambdaParamResultHack);
                    return expression.NodeType == ExpressionType.AndAlso ? Builders<TDocType>.Filter.And(leftQuery, rightQuery) : Builders<TDocType>.Filter.Or(leftQuery, rightQuery);
                }
            }

            // Handle unary operator not (!)
            if (expression.NodeType == ExpressionType.Not)
            {
                var unExp = (UnaryExpression)expression;
                return Builders<TDocType>.Filter.Not(BuildMongoWhereExpressionAsQuery(unExp.Operand, isLambdaParamResultHack));
            }

            // Handle .IsMale case in: .Where(c => c.IsMale || c.Age == 15)
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                return Builders<TDocType>.Filter.Eq(GetMongoFieldNameInMatchStage(expression, isLambdaParamResultHack), true);
            }

            // Handle method calls on sub properties
            // .Where(c => string.IsNullOrEmpty(c.Name))
            // .Where(c => c.Names.Contains("Bob"))
            // etc...
            if (expression.NodeType == ExpressionType.Call)
            {
                var callExp = (MethodCallExpression)expression;

                // Support .Where(c => c.ArrayProp.Contains(1)) and .Where(c => new[] { 1, 2, 3}.Contains(c.Id))
                if (callExp.Method.Name == "Contains")
                {
                    // Part 1 - Support .Where(c => someLocalEnumerable.Contains(c.Field))
                    // Extract the IEnumerable that .Contains is being called on
                    // Important to note that it can be in callExp.Object (for a List) or in callExp.Arguments[0] (for a constant, read-only array)
                    if ((callExp.Object ?? callExp.Arguments[0]) is ConstantExpression arrayConstantExpression)
                    {
                        var localEnumerable = arrayConstantExpression.Value;
                        if (TypeSystem.FindIEnumerable(localEnumerable.GetType()) == null)
                        {
                            throw new InvalidQueryException("In Where(), Contains() only supported on IEnumerable");
                        }

                        // Get the field that we're going to search for within the IEnumerable
                        var mongoFieldName = GetMongoFieldNameInMatchStage(callExp.Arguments.Last(), isLambdaParamResultHack);

                        // Evaluate the IEnumerable
                        var array = (BsonArray)GetBsonValueFromObject(localEnumerable);

                        if (mongoFieldName == null)
                            return new BsonDocumentFilterDefinition<TDocType>(new BsonDocument("$in", array));

                        return Builders<TDocType>.Filter.In(mongoFieldName, array.AsEnumerable());
                    }

                    // Par 2 - Support .Where(c => c.SomeArrayProperty.Contains("foo"))
                    string searchTargetMongoFieldName = GetMongoFieldNameInMatchStage(callExp.Object, isLambdaParamResultHack);
                    var searchItem = BsonValue.Create(((ConstantExpression)callExp.Arguments[0]).Value);
                    return Builders<TDocType>.Filter.AnyEq(searchTargetMongoFieldName, searchItem);
                }

                // Support .Where(c => string.IsNullOrEmpty(c.Name))
                if (callExp.Method.Name == "IsNullOrEmpty" && callExp.Object == null && callExp.Method.ReflectedType == typeof(string))
                {
                    var mongoFieldName = GetMongoFieldNameInMatchStage(callExp.Arguments.Single(), isLambdaParamResultHack);
                    return Builders<TDocType>.Filter.Or(Builders<TDocType>.Filter.Eq(mongoFieldName, BsonNull.Value), Builders<TDocType>.Filter.Eq(mongoFieldName, new BsonString("")));
                }

                // 新增：支持正则表达式匹配
                if (callExp.Method.ReflectedType == typeof(System.Text.RegularExpressions.Regex))
                {
                    if (callExp.Method.Name == "IsMatch" && callExp.Arguments.Count >= 2)
                    {
                        var input = GetMongoFieldNameInMatchStage(callExp.Arguments[0], isLambdaParamResultHack);

                        if (callExp.Arguments[1] is ConstantExpression patternExp && patternExp.Value is string pattern)
                        {
                            var regexOptions = System.Text.RegularExpressions.RegexOptions.None;
                            if (callExp.Arguments.Count > 2 && callExp.Arguments[2] is ConstantExpression optionsExp)
                            {
                                regexOptions = (System.Text.RegularExpressions.RegexOptions)optionsExp.Value;
                            }

                            string mongoRegexOptions = "";
                            if (regexOptions.HasFlag(System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                mongoRegexOptions += "i";
                            if (regexOptions.HasFlag(System.Text.RegularExpressions.RegexOptions.Multiline))
                                mongoRegexOptions += "m";
                            if (regexOptions.HasFlag(System.Text.RegularExpressions.RegexOptions.Singleline))
                                mongoRegexOptions += "s";

                            return Builders<TDocType>.Filter.Regex(input, new MongoDB.Bson.BsonRegularExpression(pattern, mongoRegexOptions));
                        }
                    }
                }

                // 新增：支持字符串的更多比较方法
                if (callExp.Object?.Type == typeof(string))
                {
                    // 新增：支持字符串长度比较
                    if (callExp.Method.Name == "Length")
                    {
                        // 这种情况在MemberExpression中已经处理，这里是为了完整性
                        throw new InvalidQueryException("String.Length should be handled as MemberExpression");
                    }
                }

                // 新增：支持DateTime的比较方法
                if (callExp.Method.ReflectedType == typeof(DateTime) && callExp.Object == null)
                {
                    if (callExp.Method.Name == "Compare" && callExp.Arguments.Count == 2)
                    {
                        var left = BuildMongoWhereExpressionAsBsonValue(callExp.Arguments[0], isLambdaParamResultHack);
                        var right = BuildMongoWhereExpressionAsBsonValue(callExp.Arguments[1], isLambdaParamResultHack);

                        // DateTime.Compare返回-1, 0, 或1，这里假设用于比较操作
                        // 实际使用中可能需要根据上下文进一步处理
                        return new BsonDocumentFilterDefinition<TDocType>(
                            new BsonDocument("$expr",
                                new BsonDocument("$cmp", new BsonArray([left, right]))));
                    }
                }

                // Support .Where(c => c.SomeArrayProp.Any()) and .Where(c => c.SomeArrayProp.Any(d => d.SubProp > 5))
                if (callExp.Method.Name == "Any")
                {
                    // Support .Where(c => c.SomeArrayProp.Any())
                    if (callExp.Arguments.Count() == 1)
                    {
                        var mFieldName = GetMongoFieldNameInMatchStage(callExp.Arguments[0], isLambdaParamResultHack);
                        return Builders<TDocType>.Filter.SizeGt(mFieldName, 0);
                    }

                    // Support .Where(c => c.SomeArrayProp.Any(d => d.SubProp > 5))
                    var mongoFieldName = GetMongoFieldNameInMatchStage(callExp.Arguments[0], isLambdaParamResultHack);
                    var predicateExpression = (LambdaExpression)callExp.Arguments[1];

                    var query = Builders<TDocType>.Filter.ElemMatch(mongoFieldName, BuildMongoWhereExpressionAsQuery(predicateExpression.Body, false));
                    return query;
                }

                throw new InvalidQueryException($"No translation for method {callExp.Method.Name}.  Mongo doesn't support very many expressions in a top level .Where ($match stage).  Consider doing .Select().Where() for better support.");
            }

            if (expression is ConstantExpression constantExpression)
            {
                // This is for handling .Where(c => true) and .Where(c => false).
                // We can use Builders<TDocType>.Filter.NotExists to achieve this
                bool expressionValue = (bool)constantExpression.Value;
                return expressionValue ? Builders<TDocType>.Filter.Exists("_this_field_does_not_exist_912419254012", false) : Builders<TDocType>.Filter.Exists("_this_field_does_not_exist_912419254012");
            }

            throw new InvalidQueryException("In Where(), can't build Mongo expression for node type" + expression.NodeType);
        }


        /// <summary>
        /// Builds a Mongo expression for use in a $match statement from a given expression.
        /// See documentation of BuildMongoWhereExpressionAsQuery for the explanation around isLambdaParamResultHack
        /// </summary>
        private BsonValue BuildMongoWhereExpressionAsBsonValue(Expression expression, bool isLambdaParamResultHack)
        {
            if (expression is MemberExpression)
            {
                throw new InvalidQueryException("Can't use field name on right hand side of expression.");

                // It would sure be nice if we could do this.
                // Except Mongo doesn't support this yet: .Where(c => c.Field1 == c.Field2).
                // return new BsonString("$" + GetMongoFieldName(expression));
            }

            if (expression is ConstantExpression constantExpression)
            {
                return GetBsonValueFromObject(constantExpression.Value);
            }

            return BuildMongoWhereExpressionAsQuery(expression, isLambdaParamResultHack).Render();
        }

        /// <summary>
        /// Gets the lambda (which may be null) from a MethodCallExpression
        /// </summary>
        /// <returns>A LambdaExpression, possibly null</returns>
        private static LambdaExpression GetLambda(MethodCallExpression mce)
        {
            if (mce.Arguments.Count < 2)
                return null;

            return (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
        }

        /// <summary>
        /// Allow an aggregation to be run on each group of a grouping.
        /// Examples:
        ///     .GroupBy(...).Where(c => c.Count() > 1)
        ///     .GroupBy(...).Select(c => c.Sum())
        /// This function modifies the previous .GroupBy ($group) pipeline operation
        /// by including this aggregation and then returns the fieldname containing
        /// the result of the aggregation.
        /// </summary>
        /// <param name="callExp">The MethodCallExpression being run on the grouping</param>
        /// <returns>The mongo field name</returns>
        public BsonString GetMongoFieldNameForMethodOnGrouping(MethodCallExpression callExp)
        {
            if (callExp.Arguments.Count == 0)
                throw new InvalidQueryException("Unsupported usage of " + callExp.Method.Name);

            // Support the case when a subdocument is mapped to a Dictionary<string, MyType>.
            // In this case, the key to the dictionary is returned as the document field name.
            if (callExp.Method.Name == "get_Item"
                && callExp.Method.DeclaringType.Name == typeof(Dictionary<,>).Name
                && callExp.Object is MemberExpression memberExpression
                && callExp.Arguments[0] is ConstantExpression constantExpression
                && callExp.Arguments[0].Type == typeof(string))
            {
                return string.Concat(
                            GetMongoFieldName(memberExpression?.Member), '.',
                            (string)constantExpression?.Value
                        );
            }

            // Only allow a function within a Select to be called on a group
            if (callExp.Arguments[0].Type.Name != "IGrouping`2")
                throw new InvalidQueryException("Aggregation \"" + callExp.Method.Name + "\" can only be run after a GroupBy");

            // Get the $group document from the most recent $group pipeline stage
            var groupDoc = GetLastOccurrenceOfPipelineStage("$group", false);

            // Handle aggregation functions within the select (Sum, Max, etc)
            //    .Select(c => c.Sum(d => d.Age))
            // Would get converted converted to this Bson for use in the project:
            //    $sum0
            // Then this element would be added to the prio $group pipeline stage:
            //    {$sum:"$age"}
            if (!NodeToMongoAggregationOperatorDict.ContainsKey(callExp.Method.Name))
                throw new InvalidQueryException("Method " + callExp.Method.Name + " not supported on Grouping");

            // Get the mongo operator (ie "$sum") that this method maps to
            string mongoOperator = NodeToMongoAggregationOperatorDict[callExp.Method.Name];

            // Create a temporary variable name for using in our project statement
            // This will look like "sum0" or "avg1"
            string tempVariableName = mongoOperator + _nextUniqueVariableId++;

            // Get the operand for the operator
            BsonValue mongoOperand;
            if ((callExp.Method.Name == "Count" && callExp.Arguments.Count == 1) || callExp.Method.Name == "First" || callExp.Method.Name == "Last")
            {
                // We don't support a lambda within the .Count
                // No good:   .Select(d => d.Count(e => e.Age > 15))
                if (callExp.Arguments.Count > 1)
                    throw new InvalidQueryException("Argument within " + callExp.Method.Name + " within Select not supported");

                // If we're counting, then the expression is {$count: 1}
                // .GroupBy(...).Select(g => g.Count())
                if (callExp.Method.Name == "Count")
                    mongoOperand = new BsonInt32(1);
                else
                {
                    // For First and Last, the expression is {$first: "$$ROOT"} or {$last: "$$ROOT"}
                    mongoOperand = new BsonString("$$ROOT");
                }
            }
            // supports .GroupBy(...).Select(g => new { Details = g.Select(d => new { d.Age, d.NumPets }) })
            else if (callExp.Method.Name == "Select" && callExp.Arguments.Count == 2)
            {
                var lambdaExp = (LambdaExpression)callExp.Arguments[1];
                // if the inner lambda is like d => new {...} in above
                if (lambdaExp.Body.NodeType == ExpressionType.New)
                {
                    var newExp = (NewExpression)lambdaExp.Body;
                    var newExpProperties = newExp.Type.GetProperties();

                    // Get the mongo field names for each property in the new {...}
                    var fieldNames = newExp.Arguments
                                           .Select((c, i) => new
                                           {
                                               FieldName = newExpProperties[i].Name,
                                               ExpressionValue = BuildMongoSelectExpression(c, true)
                                           })
                                           .Select(c => new BsonElement(c.FieldName, c.ExpressionValue))
                                           .ToList();

                    mongoOperand = new BsonDocument(fieldNames);
                }
                // if the inner lambda were d => d.Age instead
                else
                {
                    mongoOperand = BuildMongoSelectExpression(lambdaExp.Body);
                }
            }
            else if (callExp.Arguments.Count == 2)
            {
                // Get the inner lambda; "d => d.Age" from the above example
                var lambdaExp = (LambdaExpression)callExp.Arguments[1];

                // Get the operand for the operator
                mongoOperand = BuildMongoSelectExpression(lambdaExp.Body);
            }
            else
            {
                throw new InvalidQueryException("Unsupported usage of " + callExp.Method.Name + " within Select");
            }

            // Handle the special case for Count
            // .GroupBy(...).Select(g => g.Count(predicate))
            if (callExp.Method.Name == "Count")
            {
                // Mongo doesn't explicitly support Count(predicate)
                // So instead build this: Sum(predicate ? 1 : 0)
                mongoOperator = "sum";
                mongoOperand = new BsonDocument("$cond", new BsonDocument(new[] {
                    new BsonElement("if", mongoOperand),
                    new BsonElement("then", new BsonInt32(1)),
                    new BsonElement("else", new BsonInt32(0)),
                }.AsEnumerable()));
            }

            // Build the expression being aggregated
            var aggregationDoc = new BsonDocument("$" + mongoOperator, mongoOperand);

            // Add to the $group stage, a new variable which receives our aggregation
            var newGroupElement = new BsonDocument(tempVariableName, aggregationDoc);
            groupDoc.AddRange(newGroupElement);

            // Return our temp variable as the field name to use in this projection
            return new BsonString("$" + tempVariableName);
        }

        /// <summary>
        /// Accepts a mongo expression of the specified .Net type.
        /// If the expression is a string, then a new Mongo expression is returned
        /// equivalent to: mongoExpression ?? "".
        /// Else the original mongoExpression is returned unchanged.
        /// </summary>
        public BsonValue ReplaceNullStringWithEmptyString(Type expressionType, BsonValue mongoExpression)
        {
            return expressionType == typeof(string)
                       ? new BsonDocument("$ifNull", new BsonArray([mongoExpression, BsonValue.Create("")]))
                       : mongoExpression;
        }

        /// <summary>
        /// Builds a Mongo expression for use in a $project statement from a given expression
        /// </summary>
        /// <param name="expression">Expression to convert to a BsonValue representing an Aggregation Framework expression</param>
        /// <param name="specialTreatmentForConst">TODO: Explain what this was for again...</param>
        public BsonValue BuildMongoSelectExpression(Expression expression, bool specialTreatmentForConst = false)
        {
            // TODO: What about enums here?
            var visitor = new FindStringAsObjectIdVisitor<TDocType>();
            visitor.Visit(expression);
            var isStringAsObjectId = visitor.IsStringAsObjectId;

            // c.Age
            if (expression is MemberExpression memberExpression)
            {
                var member = memberExpression.Member;

                // Handle member access of string objects
                if (member.DeclaringType == typeof(string))
                {
                    if (member.Name == "Length")
                        return new BsonDocument("$strLenCP", BuildMongoSelectExpression(memberExpression.Expression));

                    throw new InvalidQueryException($"{member.Name} property on String not supported due to lack of Mongo support :(");
                }

                // Handle member access of TimeSpan objects
                if (member.DeclaringType == typeof(TimeSpan))
                {
                    var expressionDoc = BuildMongoSelectExpression(memberExpression.Expression);

                    // No-op - we store these natively as ticks
                    if (member.Name == "Ticks")
                        return expressionDoc;

                    // Handle our various Total properties
                    long divisor = 0;
                    switch (member.Name)
                    {
                        case "TotalMilliseconds": divisor = 10000; break;
                        case "TotalSeconds": divisor = 10000000; break;
                        case "TotalMinutes": divisor = 600000000; break;
                        case "TotalHours": divisor = 36000000000; break;
                        case "TotalDays": divisor = 864000000000; break;
                    }

                    if (divisor > 0)
                        return new BsonDocument("$divide", new BsonArray([expressionDoc, BsonValue.Create(divisor)]));

                    throw new InvalidQueryException($"{member.Name} property on TimeSpan not supported :(");
                }

                if (member.DeclaringType == typeof(DateTime))
                {
                    if (member.Name == "Date")
                    {
                        // Get the thing we're supposed to take the .Date from
                        BsonValue dateTimeExpression;

                        if (ExpressionIsObjectIdCreationTime(memberExpression.Expression))
                        {
                            // Special case for ObjectId.CreationTime
                            // Mongo's Date Operators ($year, $month, $dayOfMonth, etc) all work on an ObjectId.
                            // So let's remove the .CreationTime property from our expression because that will
                            // lead to an unnecessairly verbose Builders<TDocType>.Filter.
                            dateTimeExpression = BuildMongoSelectExpression(((MemberExpression)memberExpression.Expression).Expression);
                        }
                        else
                            dateTimeExpression = BuildMongoSelectExpression(memberExpression.Expression);


                        // No support for Date but we can hack it by building a new Date from the Year+Month+Day

                        var year = new BsonDocument("$year", dateTimeExpression);
                        var month = new BsonDocument("$month", dateTimeExpression);
                        var day = new BsonDocument("$dayOfMonth", dateTimeExpression);

                        var dateFromPartElements = new[] {
                            new BsonElement("year", year),
                            new BsonElement("month", month),
                            new BsonElement("day", day)
                        };

                        return new BsonDocument("$dateFromParts", new BsonDocument(dateFromPartElements.AsEnumerable()));
                    }

                    // .Net DayOfWeek is 0 indexed, Mongo is 1 indexed
                    if (member.Name == "DayOfWeek")
                    {
                        var array = new BsonArray([new BsonDocument("$dayOfWeek", BuildMongoSelectExpression(memberExpression.Expression)), (BsonValue)1
                        ]);
                        return new BsonDocument("$subtract", array);
                    }

                    if (NodeToMongoDateOperatorDict.TryGetValue(member.Name, out string mongoDateOperator))
                        return new BsonDocument(mongoDateOperator, BuildMongoSelectExpression(memberExpression.Expression));

                    throw new InvalidQueryException($"{member.Name} property on DateTime not supported due to lack of Mongo support :(");
                }

                // Handle a special case of member access: CreationTime on an ObjectId
                // Convert this to a proper Date
                if (ExpressionIsObjectIdCreationTime(memberExpression))
                {
                    var dateTimeExpression = BuildMongoSelectExpression(memberExpression.Expression);
                    var year = new BsonDocument("$year", dateTimeExpression);
                    var month = new BsonDocument("$month", dateTimeExpression);
                    var day = new BsonDocument("$dayOfMonth", dateTimeExpression);
                    var hour = new BsonDocument("$hour", dateTimeExpression);
                    var minute = new BsonDocument("$minute", dateTimeExpression);
                    var second = new BsonDocument("$second", dateTimeExpression);

                    var dateFromPartElements = new[] {
                        new BsonElement("year", year),
                        new BsonElement("month", month),
                        new BsonElement("day", day),
                        new BsonElement("hour", hour),
                        new BsonElement("minute", minute),
                        new BsonElement("second", second)
                    };

                    return new BsonDocument("$dateFromParts", new BsonDocument(dateFromPartElements.AsEnumerable()));
                }

                // Just throw a $ in front of the fully prefixed field name to get the proper aggregation pipeline syntax
                var mongoFieldName = GetMongoFieldName(memberExpression, true);

                return new BsonString("$" + mongoFieldName);
            }

            // 15
            if (expression is ConstantExpression constExp)
            {
                // Handle a special case:
                //     .Select(c => 15)
                // This would naturally tranlate to:
                //     {$project, {_result_:15}}
                // But that's not valid Mongo.  Instead, we need to build the 15 arithmetically:
                //     {$project, {_result:{$add:[0,15]}}}
                if (specialTreatmentForConst)
                {
                    if (constExp.Type == typeof(int))
                    {
                        // Build a binary-add expression that evaluates our 0 or 1
                        return BuildMongoSelectExpression(Expression.MakeBinary(ExpressionType.Add, Expression.Constant(0), constExp));
                    }

                    if (constExp.Type == typeof(string))
                    {
                        // Build an expression concatenating the string with nothing else
                        return new BsonDocument("$concat", new BsonArray([new BsonString((string)constExp.Value)]));
                    }

                    if (constExp.Type == typeof(bool))
                    {
                        return BuildMongoSelectExpression(Expression.MakeBinary(ExpressionType.OrElse, Expression.Constant(false), constExp));
                    }
                }

                return GetBsonValueFromObject(constExp.Value);
            }

            // c.Age + 10
            if (expression is BinaryExpression binExp && NodeToMongoBinaryOperatorDict.ContainsKey(expression.NodeType))
            {
                var left = binExp.Left;
                var right = binExp.Right;
                if (isStringAsObjectId)
                {
                    left = new StringToObjectIdVisitor().Visit(left);
                    right = new StringToObjectIdVisitor().Visit(right);
                }

                var leftValue = BuildMongoSelectExpression(left);
                var rightValue = BuildMongoSelectExpression(right);

                string mongoOperator = NodeToMongoBinaryOperatorDict[expression.NodeType];

                // Support string concatenation via the "+" operator.
                // If either side is a string, then replace $add with $concat
                if (binExp.NodeType == ExpressionType.Add && (binExp.Left.Type == typeof(string) || binExp.Right.Type == typeof(string)))
                {
                    mongoOperator = "$concat";

                    // In Mongo: $concat(null, "foo") => null.
                    // In  .Net: null + "foo" => foo
                    // So swap out null strings with empty strings to match .Net behavior
                    leftValue = ReplaceNullStringWithEmptyString(binExp.Left.Type, leftValue);
                    rightValue = ReplaceNullStringWithEmptyString(binExp.Right.Type, rightValue);
                }

                // When adding or substracting DateTime with TimeSpan, convert the TimeSpan back to milliseconds
                if ((expression.NodeType == ExpressionType.Subtract || expression.NodeType == ExpressionType.Add) &&
                    (binExp.Left.Type == typeof(DateTime) && binExp.Right.Type == typeof(TimeSpan)
                     || binExp.Left.Type == typeof(TimeSpan) && binExp.Right.Type == typeof(DateTime)))
                {
                    if (binExp.Left.Type == typeof(TimeSpan))
                        leftValue = new BsonDocument("$divide", new BsonArray([leftValue, BsonValue.Create(10000)]));
                    else
                        rightValue = new BsonDocument("$divide", new BsonArray([rightValue, BsonValue.Create(10000)]));
                }

                var array = new BsonArray([leftValue, rightValue]);
                var expressionDoc = new BsonDocument(mongoOperator, array);

                // Support integer division
                if (expression.NodeType == ExpressionType.Divide && (expression.Type == typeof(long) || expression.Type == typeof(int)))
                    expressionDoc = new BsonDocument("$trunc", expressionDoc);

                // Mongo returns the difference between DateTimes as milliseconds.  Convert them to 100-nanosecond chunks per .Net symantics
                if (expression.NodeType == ExpressionType.Subtract && binExp.Left.Type == typeof(DateTime) && binExp.Right.Type == typeof(DateTime))
                    expressionDoc = new BsonDocument("$multiply", new BsonArray([expressionDoc, BsonValue.Create(10000)
                    ]));

                return expressionDoc;
            }

            // !c.IsMale
            if (expression.NodeType == ExpressionType.Not)
            {
                var unExp = (UnaryExpression)expression;
                BsonValue operandValue = BuildMongoSelectExpression(unExp.Operand);
                return new BsonDocument("$not", operandValue);
            }

            // c.IsMale ? "Man" : "Woman"
            if (expression.NodeType == ExpressionType.Conditional)
            {
                // Build expression with the Mongo $cond operator
                var condExp = (ConditionalExpression)expression;
                BsonValue testValue = BuildMongoSelectExpression(condExp.Test);
                BsonValue ifTrueValue = BuildMongoSelectExpression(condExp.IfTrue);
                BsonValue ifFalseValue = BuildMongoSelectExpression(condExp.IfFalse);
                var condDocValue = new BsonDocument(new[] {
                    new BsonElement("if", testValue),
                    new BsonElement("then", ifTrueValue),
                    new BsonElement("else", ifFalseValue)
                }.AsEnumerable());
                return new BsonDocument("$cond", condDocValue);
            }

            // string.IsNullOrEmpty
            if (expression.NodeType == ExpressionType.Call)
            {
                var callExp = (MethodCallExpression)expression;

                // Special handling for method calls on IGrouping...
                // c.Sum(d => d.Age), c.Count(), etc
                if (callExp.Arguments.Count != 0 && callExp.Arguments[0].Type.Name == "IGrouping`2")
                {
                    return GetMongoFieldNameForMethodOnGrouping(callExp);
                }

                // Handle static methods on string
                if (callExp.Method.ReflectedType == typeof(string) && callExp.Object == null)
                {
                    if (callExp.Method.Name == "IsNullOrEmpty")
                    {
                        BsonValue expressionToTest = BuildMongoSelectExpression(callExp.Arguments.Single());

                        // Test for null
                        var ifNullDoc = new BsonDocument("$ifNull", new BsonArray([expressionToTest, new BsonString("")
                        ]));

                        // Test for empty
                        return new BsonDocument("$eq", new BsonArray([new BsonString(""), ifNullDoc.AsBsonValue]));
                    }

                    if (callExp.Method.Name == "Compare" || callExp.Method.Name == "CompareOrdinal")
                    {
                        if (callExp.Arguments.Count != 2)
                            throw new InvalidQueryException($"Only supported overload of string.Compare (and CompareOrdinal) is string.Compare(string, string)");

                        BsonValue exp1 = BuildMongoSelectExpression(callExp.Arguments[0]);
                        BsonValue exp2 = BuildMongoSelectExpression(callExp.Arguments[1]);

                        return new BsonDocument("$cmp", new BsonArray([exp1, exp2]));
                    }

                    throw new InvalidQueryException($"Can't translate static method string.{callExp.Method.Name} to Mongo expression");
                }

                if (callExp.Object?.Type == typeof(string))
                {
                    if (callExp.Method.Name == "StartsWith" && callExp.Arguments.Count() == 1)
                    {
                        if (!(callExp.Arguments.Single() is ConstantExpression constantExpression))
                            throw new InvalidQueryException(".StartsWith(...) only supports a single, constant, String argument");

                        // ^-- We could relax that requirement if we didn't implement this using $substr.
                        // By using $substr we need to have the length of our argument to StartsWith.

                        string searchString = (string)constantExpression.Value;
                        var substringDoc = new BsonDocument("$substrCP", new BsonArray([BuildMongoSelectExpression(callExp.Object), 0, searchString.Length
                        ]));

                        return new BsonDocument("$eq", new BsonArray(new[] { substringDoc, constantExpression.Value }));
                    }

                    if (callExp.Method.Name == "Contains" && callExp.Arguments.Count() == 1)
                    {
                        if (!(callExp.Arguments.Single() is ConstantExpression constantExpression))
                            throw new InvalidQueryException(".Contains(...) only supports a single, constant, String argument");

                        // ^-- We could relax that requirement if we didn't implement this using $substr.
                        // By using $substr we need to have the length of our argument to StartsWith.

                        string searchString = (string)constantExpression.Value;
                        var substringDoc = new BsonDocument("$indexOfCP", new BsonArray([BuildMongoSelectExpression(callExp.Object), new BsonString(searchString)
                        ]));
                        return new BsonDocument("$gte", new BsonArray([substringDoc, BsonValue.Create(0)]));
                    }

                    if (callExp.Method.Name == "EndsWith" && callExp.Arguments.Count() == 1)
                    {
                        if (!(callExp.Arguments.Single() is ConstantExpression constantExpression))
                            throw new InvalidQueryException(".EndsWith(...) only supports a single, constant, String argument");

                        // ^-- We could relax that requirement if we didn't implement this using $substr.
                        // By using $substr we need to have the length of our argument to StartsWith.

                        string searchString = (string)constantExpression.Value;
                        var substringDoc = new BsonDocument("$regexMatch", new BsonDocument(new BsonElement[] { new("input", BuildMongoSelectExpression(callExp.Object)), new("regex", new BsonRegularExpression($"/{searchString}$/")) }));

                        return substringDoc;
                    }


                    if (callExp.Method.Name == "ToUpper")
                        return new BsonDocument("$toUpper", BuildMongoSelectExpression(callExp.Object));
                    if (callExp.Method.Name == "ToLower")
                        return new BsonDocument("$toLower", BuildMongoSelectExpression(callExp.Object));

                    // 新增：Trim系列函数支持
                    if (callExp.Method.Name == "Trim")
                    {
                        if (callExp.Arguments.Count == 0)
                            return new BsonDocument("$trim", new BsonDocument("input", BuildMongoSelectExpression(callExp.Object)));

                        // 支持 Trim(char[])
                        if (callExp.Arguments.Count == 1 && callExp.Arguments[0] is ConstantExpression trimCharsExp)
                        {
                            var trimChars = (char[])trimCharsExp.Value;
                            var charsToTrim = string.Concat(trimChars);
                            return new BsonDocument("$trim", new BsonDocument
                            {
                                {"input", BuildMongoSelectExpression(callExp.Object)},
                                {"chars", charsToTrim}
                            });
                        }
                    }

                    if (callExp.Method.Name == "TrimStart")
                    {
                        if (callExp.Arguments.Count == 0)
                            return new BsonDocument("$ltrim", new BsonDocument("input", BuildMongoSelectExpression(callExp.Object)));

                        if (callExp.Arguments.Count == 1 && callExp.Arguments[0] is ConstantExpression trimCharsExp)
                        {
                            var trimChars = (char[])trimCharsExp.Value;
                            var charsToTrim = string.Concat(trimChars);
                            return new BsonDocument("$ltrim", new BsonDocument
                            {
                                {"input", BuildMongoSelectExpression(callExp.Object)},
                                {"chars", charsToTrim}
                            });
                        }
                    }

                    if (callExp.Method.Name == "TrimEnd")
                    {
                        if (callExp.Arguments.Count == 0)
                            return new BsonDocument("$rtrim", new BsonDocument("input", BuildMongoSelectExpression(callExp.Object)));

                        if (callExp.Arguments.Count == 1 && callExp.Arguments[0] is ConstantExpression trimCharsExp)
                        {
                            var trimChars = (char[])trimCharsExp.Value;
                            var charsToTrim = string.Concat(trimChars);
                            return new BsonDocument("$rtrim", new BsonDocument
                            {
                                {"input", BuildMongoSelectExpression(callExp.Object)},
                                {"chars", charsToTrim}
                            });
                        }
                    }

                    // 新增：Replace函数支持
                    if (callExp.Method.Name == "Replace" && callExp.Arguments.Count == 2)
                    {
                        return new BsonDocument("$replaceAll", new BsonDocument
                        {
                            {"input", BuildMongoSelectExpression(callExp.Object)},
                            {"find", BuildMongoSelectExpression(callExp.Arguments[0])},
                            {"replacement", BuildMongoSelectExpression(callExp.Arguments[1])}
                        });
                    }

                    // 新增：Split函数支持
                    if (callExp.Method.Name == "Split" && callExp.Arguments.Count >= 1)
                    {
                        return new BsonDocument("$split", new BsonArray([
                            BuildMongoSelectExpression(callExp.Object),
                            BuildMongoSelectExpression(callExp.Arguments[0])
                        ]));
                    }

                    if (callExp.Method.Name == "Substring")
                    {
                        // Handle string.SubString(index, count) and .Substring(index)

                        BsonValue characterCount = callExp.Arguments.Count == 1
                                                   ? int.MaxValue
                                                   : BuildMongoSelectExpression(callExp.Arguments[1]);

                        // Build {$substrCP: ["stringToTakeSubstringOf", index, count]}
                        return new BsonDocument("$substrCP",
                                                new BsonArray([
                                                    BuildMongoSelectExpression(callExp.Object),
                                                    BuildMongoSelectExpression(callExp.Arguments[0]),
                                                    characterCount
                                                ]));
                    }

                    if (callExp.Method.Name == "IndexOf")
                    {
                        // Build {indexOfCP: ["stringToSearchWithin", "stringToSearchFor"]}
                        return new BsonDocument("$indexOfCP",
                                                new BsonArray([
                                                    BuildMongoSelectExpression(callExp.Object),
                                                    BuildMongoSelectExpression(callExp.Arguments[0])
                                                ]));
                    }

                    throw new InvalidQueryException($"Can't translate method {callExp.Object.Type.Name}.{callExp.Method.Name} to Mongo expression");
                }




                // Support two cases for Contains:
                // 1: Contains on a property that is an Enumerable searching for some constant
                //    c.SomeEnumerableProperty.Contains("someConst")
                // 2: Contains on a constant expression searching for some property
                //    new[] { 1,2,3}.Contains(c.SomeProperty)
                if (callExp.Method.Name == "Contains" && (callExp.Method.ReflectedType.IsAssignableTo<IEnumerable>() || callExp.Method.ReflectedType == typeof(Enumerable)))
                {
                    BsonValue searchValue, arrayToSearch;

                    var left = callExp.Arguments.Count == 1 ? callExp.Object : callExp.Arguments[0];
                    var right = callExp.Arguments.Count == 1 ? callExp.Arguments[0] : callExp.Arguments[1];
                    // Case 1:
                    // list: c.SomeEnumerableProperty.Contains("someConst")
                    // array: Enumerable.Contains(c.SomeEnumerableProperty,"someConst")
                    // Case 2: new[] { 1,2,3}.Contains(c.SomeProperty)
                    searchValue = BuildMongoSelectExpression(right);
                    if (isStringAsObjectId)
                    {
                        left = new StringToObjectIdVisitor().Visit(left);
                    }
                    arrayToSearch = BuildMongoSelectExpression(left);


                    return new BsonDocument("$in", new BsonArray([searchValue, arrayToSearch]));
                }

                // c.Any(predicate) (where c is an Enumerable)
                if (callExp.Method.Name == "Any" && (callExp.Method.ReflectedType.IsAssignableTo<IEnumerable>() || callExp.Method.ReflectedType == typeof(Enumerable)))
                {
                    if (callExp.Arguments.Count == 1)
                    {
                        // c.Any()

                        var countDoc = new BsonDocument("$size", BuildMongoSelectExpression(callExp.Arguments[0]));
                        var gteZeroDoc = new BsonDocument("$gte", new BsonArray([countDoc, BsonValue.Create(1)]));
                        return gteZeroDoc;
                    }

                    if (callExp.Arguments.Count == 2)
                    {
                        // c.Any(d => ...)

                        var searchPredicate = (LambdaExpression)callExp.Arguments[1];

                        string subSelectLambdaParamName = searchPredicate.Parameters.Single().Name;
                        string internalVariableName = "foo" + ++_nextUniqueVariableId;
                        _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                        var searchValue = BuildMongoSelectExpression(searchPredicate.Body);

                        _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                        var searchTarget = BuildMongoSelectExpression(callExp.Arguments[0]);

                        // There's no direct translation for Any.  So we'll essentially build:
                        // c.Where(predicate).Count() > 0

                        var filterDoc = new BsonDocument("$filter", new BsonDocument {
                            new BsonElement("input", searchTarget),
                            new BsonElement("as", internalVariableName),
                            new BsonElement("cond", searchValue),
                        });

                        var countDoc = new BsonDocument("$size", filterDoc);
                        var gteZeroDoc = new BsonDocument("$gte", new BsonArray([countDoc, BsonValue.Create(1)]));
                        return gteZeroDoc;
                    }
                    else
                        throw new MongoLinqPlusPlusInternalException($"Unexpected number of arguments ({callExp.Arguments.Count}) to Any");
                }

                // c.All(predicate) (where c is an Enumerable)
                if (callExp.Method.Name == "All" && (callExp.Method.ReflectedType.IsAssignableTo<IEnumerable>() || callExp.Method.ReflectedType.FullName == "System.Linq.Enumerable"))
                {
                    var searchPredicate = (LambdaExpression)callExp.Arguments[1];

                    string subSelectLambdaParamName = searchPredicate.Parameters.Single().Name;
                    string internalVariableName = "foo" + ++_nextUniqueVariableId;
                    _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                    var searchValue = BuildMongoSelectExpression(searchPredicate.Body);

                    _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                    var searchTarget = BuildMongoSelectExpression(callExp.Arguments[0]);

                    // All(predicate) logic:
                    // For empty arrays, All should return true
                    // For non-empty arrays, All should return true if all elements match the predicate
                    // Implementation: (array.length == 0) || (filter(array, predicate).length == array.length)

                    var filterDoc = new BsonDocument("$filter", new BsonDocument {
                        new BsonElement("input", searchTarget),
                        new BsonElement("as", internalVariableName),
                        new BsonElement("cond", searchValue),
                    });

                    var filterCountDoc = new BsonDocument("$size", filterDoc);
                    var expectedCountDoc = new BsonDocument("$size", searchTarget);
                    var gteZeroDoc = new BsonDocument("$eq", new BsonArray([filterCountDoc, expectedCountDoc]));
                    return gteZeroDoc;
                }

                // c.Count(predicate) (where c is an Enumerable)
                if (callExp.Method.Name == "Count" && (callExp.Method.ReflectedType.IsAssignableTo<IEnumerable>() || callExp.Method.ReflectedType == typeof(Enumerable)))
                {
                    var arrayToSearch = BuildMongoSelectExpression(callExp.Arguments[0]);

                    // Handle the trival case (no predicate)
                    if (callExp.Arguments.Count() == 1)
                        return new BsonDocument("$size", arrayToSearch);

                    var searchPredicate = (LambdaExpression)callExp.Arguments[1];

                    string subSelectLambdaParamName = searchPredicate.Parameters.Single().Name;
                    string internalVariableName = "foo" + ++_nextUniqueVariableId;
                    _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                    var searchValue = BuildMongoSelectExpression(searchPredicate.Body);

                    _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);


                    // Emit the expression for:
                    // c.Where(predicate).Count()

                    var filterDoc = new BsonDocument("$filter", new BsonDocument {
                        new BsonElement("input", arrayToSearch),
                        new BsonElement("as", internalVariableName),
                        new BsonElement("cond", searchValue),
                    });

                    var filterCountDoc = new BsonDocument("$size", filterDoc);
                    return filterCountDoc;
                }

                // 新增：更多数组操作方法支持
                if (callExp.Method.ReflectedType.IsAssignableTo<IEnumerable>() || callExp.Method.ReflectedType == typeof(Enumerable))
                {
                    // First/FirstOrDefault with predicate
                    if ((callExp.Method.Name == "First" || callExp.Method.Name == "FirstOrDefault") && callExp.Arguments.Count == 2)
                    {
                        var arrayToSearch = BuildMongoSelectExpression(callExp.Arguments[0]);
                        var searchPredicate = (LambdaExpression)callExp.Arguments[1];

                        string subSelectLambdaParamName = searchPredicate.Parameters.Single().Name;
                        string internalVariableName = "foo" + ++_nextUniqueVariableId;
                        _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                        var searchValue = BuildMongoSelectExpression(searchPredicate.Body);
                        _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                        var filterDoc = new BsonDocument("$filter", new BsonDocument {
                            new BsonElement("input", arrayToSearch),
                            new BsonElement("as", internalVariableName),
                            new BsonElement("cond", searchValue),
                        });

                        return new BsonDocument("$arrayElemAt", new BsonArray([filterDoc, new BsonInt32(0)]));
                    }

                    // Last/LastOrDefault with predicate
                    if ((callExp.Method.Name == "Last" || callExp.Method.Name == "LastOrDefault") && callExp.Arguments.Count == 2)
                    {
                        var arrayToSearch = BuildMongoSelectExpression(callExp.Arguments[0]);
                        var searchPredicate = (LambdaExpression)callExp.Arguments[1];

                        string subSelectLambdaParamName = searchPredicate.Parameters.Single().Name;
                        string internalVariableName = "foo" + ++_nextUniqueVariableId;
                        _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                        var searchValue = BuildMongoSelectExpression(searchPredicate.Body);
                        _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                        var filterDoc = new BsonDocument("$filter", new BsonDocument {
                            new BsonElement("input", arrayToSearch),
                            new BsonElement("as", internalVariableName),
                            new BsonElement("cond", searchValue),
                        });

                        return new BsonDocument("$arrayElemAt", new BsonArray([filterDoc, new BsonInt32(-1)]));
                    }

                    // Where方法支持
                    if (callExp.Method.Name == "Where" && callExp.Arguments.Count == 2)
                    {
                        var arrayToSearch = BuildMongoSelectExpression(callExp.Arguments[0]);
                        var searchPredicate = (LambdaExpression)callExp.Arguments[1];

                        string subSelectLambdaParamName = searchPredicate.Parameters.Single().Name;
                        string internalVariableName = "foo" + ++_nextUniqueVariableId;
                        _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                        var searchValue = BuildMongoSelectExpression(searchPredicate.Body);
                        _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                        return new BsonDocument("$filter", new BsonDocument {
                            new BsonElement("input", arrayToSearch),
                            new BsonElement("as", internalVariableName),
                            new BsonElement("cond", searchValue),
                        });
                    }

                    // Select方法支持（数组映射）
                    if (callExp.Method.Name == "Select" && callExp.Arguments.Count == 2)
                    {
                        var arrayToMap = BuildMongoSelectExpression(callExp.Arguments[0]);
                        var mapExpression = (LambdaExpression)callExp.Arguments[1];

                        string subSelectLambdaParamName = mapExpression.Parameters.Single().Name;
                        string internalVariableName = "foo" + ++_nextUniqueVariableId;
                        _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                        var mappedValue = BuildMongoSelectExpression(mapExpression.Body);
                        _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                        return new BsonDocument("$map", new BsonDocument {
                            new BsonElement("input", arrayToMap),
                            new BsonElement("as", internalVariableName),
                            new BsonElement("in", mappedValue),
                        });
                    }

                    // Skip方法支持
                    if (callExp.Method.Name == "Skip" && callExp.Arguments.Count == 2)
                    {
                        var arrayToSlice = BuildMongoSelectExpression(callExp.Arguments[0]);
                        var skipCount = BuildMongoSelectExpression(callExp.Arguments[1]);

                        return new BsonDocument("$slice", new BsonArray([
                            arrayToSlice,
                            skipCount,
                            new BsonDocument("$size", arrayToSlice) // 取剩余所有元素
                        ]));
                    }

                    // Take方法支持
                    if (callExp.Method.Name == "Take" && callExp.Arguments.Count == 2)
                    {
                        var arrayToSlice = BuildMongoSelectExpression(callExp.Arguments[0]);
                        var takeCount = BuildMongoSelectExpression(callExp.Arguments[1]);

                        return new BsonDocument("$slice", new BsonArray([
                            arrayToSlice,
                            takeCount
                        ]));
                    }

                    // Reverse方法支持
                    if (callExp.Method.Name == "Reverse" && callExp.Arguments.Count == 1)
                    {
                        return new BsonDocument("$reverseArray", BuildMongoSelectExpression(callExp.Arguments[0]));
                    }

                    // Distinct方法支持
                    if (callExp.Method.Name == "Distinct" && callExp.Arguments.Count == 1)
                    {
                        return new BsonDocument("$setUnion", new BsonArray([
                            BuildMongoSelectExpression(callExp.Arguments[0]),
                            new BsonArray() // 空数组
                        ]));
                    }

                    // Union方法支持
                    if (callExp.Method.Name == "Union" && callExp.Arguments.Count == 2)
                    {
                        return new BsonDocument("$setUnion", new BsonArray([
                            BuildMongoSelectExpression(callExp.Arguments[0]),
                            BuildMongoSelectExpression(callExp.Arguments[1])
                        ]));
                    }

                    // Except方法支持
                    if (callExp.Method.Name == "Except" && callExp.Arguments.Count == 2)
                    {
                        return new BsonDocument("$setDifference", new BsonArray([
                            BuildMongoSelectExpression(callExp.Arguments[0]),
                            BuildMongoSelectExpression(callExp.Arguments[1])
                        ]));
                    }

                    // Concat方法支持
                    if (callExp.Method.Name == "Concat" && callExp.Arguments.Count == 2)
                    {
                        return new BsonDocument("$concatArrays", new BsonArray([
                            BuildMongoSelectExpression(callExp.Arguments[0]),
                            BuildMongoSelectExpression(callExp.Arguments[1])
                        ]));
                    }

                    // Sum方法支持（数组元素求和）
                    if (callExp.Method.Name == "Sum" && callExp.Arguments.Count >= 1)
                    {
                        if (callExp.Arguments.Count == 1)
                        {
                            // 直接对数组求和
                            return new BsonDocument("$sum", BuildMongoSelectExpression(callExp.Arguments[0]));
                        }
                        else if (callExp.Arguments.Count == 2)
                        {
                            // 带选择器的求和
                            var arrayToSum = BuildMongoSelectExpression(callExp.Arguments[0]);
                            var sumExpression = (LambdaExpression)callExp.Arguments[1];

                            string subSelectLambdaParamName = sumExpression.Parameters.Single().Name;
                            string internalVariableName = "foo" + ++_nextUniqueVariableId;
                            _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                            var sumValue = BuildMongoSelectExpression(sumExpression.Body);
                            _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                            var mapDoc = new BsonDocument("$map", new BsonDocument {
                                new BsonElement("input", arrayToSum),
                                new BsonElement("as", internalVariableName),
                                new BsonElement("in", sumValue),
                            });

                            return new BsonDocument("$sum", mapDoc);
                        }
                    }

                    // Average方法支持
                    if (callExp.Method.Name == "Average" && callExp.Arguments.Count >= 1)
                    {
                        if (callExp.Arguments.Count == 1)
                        {
                            return new BsonDocument("$avg", BuildMongoSelectExpression(callExp.Arguments[0]));
                        }
                        else if (callExp.Arguments.Count == 2)
                        {
                            var arrayToAvg = BuildMongoSelectExpression(callExp.Arguments[0]);
                            var avgExpression = (LambdaExpression)callExp.Arguments[1];

                            string subSelectLambdaParamName = avgExpression.Parameters.Single().Name;
                            string internalVariableName = "foo" + ++_nextUniqueVariableId;
                            _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                            var avgValue = BuildMongoSelectExpression(avgExpression.Body);
                            _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                            var mapDoc = new BsonDocument("$map", new BsonDocument {
                                new BsonElement("input", arrayToAvg),
                                new BsonElement("as", internalVariableName),
                                new BsonElement("in", avgValue),
                            });

                            return new BsonDocument("$avg", mapDoc);
                        }
                    }

                    // Min/Max方法支持
                    if ((callExp.Method.Name == "Min" || callExp.Method.Name == "Max") && callExp.Arguments.Count >= 1)
                    {
                        string mongoOp = callExp.Method.Name == "Min" ? "$min" : "$max";

                        if (callExp.Arguments.Count == 1)
                        {
                            return new BsonDocument(mongoOp, BuildMongoSelectExpression(callExp.Arguments[0]));
                        }
                        else if (callExp.Arguments.Count == 2)
                        {
                            var arrayToProcess = BuildMongoSelectExpression(callExp.Arguments[0]);
                            var processExpression = (LambdaExpression)callExp.Arguments[1];

                            string subSelectLambdaParamName = processExpression.Parameters.Single().Name;
                            string internalVariableName = "foo" + ++_nextUniqueVariableId;
                            _subSelectParameterPrefixes.Add(subSelectLambdaParamName, $"${internalVariableName}" + ".");

                            var processValue = BuildMongoSelectExpression(processExpression.Body);
                            _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                            var mapDoc = new BsonDocument("$map", new BsonDocument {
                                new BsonElement("input", arrayToProcess),
                                new BsonElement("as", internalVariableName),
                                new BsonElement("in", processValue),
                            });

                            return new BsonDocument(mongoOp, mapDoc);
                        }
                    }
                }

                // Handle DateTime methods (ie c.DateTime.AddMinutes(1))
                if (callExp.Method.ReflectedType == typeof(DateTime))
                {
                    // Handle AddSeconds, AddMinutes, etc
                    if (callExp.Method.Name.StartsWith("Add") || callExp.Method.Name == "Subtract")
                    {
                        double mSecToAdd;
                        if (!(callExp.Arguments[0] is ConstantExpression))
                        {
                            throw new InvalidQueryException($"Sorry, I can't translate DateTime.{callExp.Method.Name} with a non-constant expression.");
                        }

                        var callArg = ((ConstantExpression)callExp.Arguments[0]).Value;
                        switch (callExp.Method.Name)
                        {
                            case "Subtract": mSecToAdd = -((TimeSpan)callArg).TotalMilliseconds; break;
                            case "Add": mSecToAdd = ((TimeSpan)callArg).TotalMilliseconds; break;
                            case "AddMilliseconds": mSecToAdd = (double)callArg; break;
                            case "AddSeconds": mSecToAdd = 1000 * (double)callArg; break;
                            case "AddMinutes": mSecToAdd = 60000 * (double)callArg; break;
                            case "AddHours": mSecToAdd = 3600000 * (double)callArg; break;
                            case "AddDays": mSecToAdd = 86400000 * (double)callArg; break;
                            default: throw new InvalidQueryException($"Sorry, we can't translate method DateTime.{callExp.Method.Name}.");
                        }

                        return new BsonDocument("$add", new BsonArray([BuildMongoSelectExpression(callExp.Object), BsonValue.Create(mSecToAdd)
                        ]));
                    }

                    throw new InvalidQueryException($"Sorry, we can't translate method DateTime.{callExp.Method.Name}.");
                }

                // 新增：Math静态方法支持
                if (callExp.Method.ReflectedType == typeof(Math) && callExp.Object == null)
                {
                    if (MathFunctionToMongoOperatorDict.TryGetValue(callExp.Method.Name, out string mongoOperator))
                    {
                        switch (callExp.Method.Name)
                        {
                            case "Abs":
                            case "Ceiling":
                            case "Floor":
                            case "Sqrt":
                            case "Log10":
                            case "Exp":
                            case "Sin":
                            case "Cos":
                            case "Tan":
                            case "Asin":
                            case "Acos":
                            case "Atan":
                            case "Sinh":
                            case "Cosh":
                            case "Tanh":
                            case "Asinh":
                            case "Acosh":
                            case "Atanh":
                                // 单参数函数
                                if (callExp.Arguments.Count == 1)
                                    return new BsonDocument(mongoOperator, BuildMongoSelectExpression(callExp.Arguments[0]));
                                break;

                            case "Round":
                                // Round可以有1个或2个参数
                                if (callExp.Arguments.Count == 1)
                                    return new BsonDocument(mongoOperator, BuildMongoSelectExpression(callExp.Arguments[0]));
                                if (callExp.Arguments.Count == 2)
                                    return new BsonDocument(mongoOperator, new BsonArray([
                                        BuildMongoSelectExpression(callExp.Arguments[0]),
                                        BuildMongoSelectExpression(callExp.Arguments[1])
                                    ]));
                                break;

                            case "Pow":
                            case "Log":
                                // 双参数函数
                                if (callExp.Arguments.Count == 2)
                                    return new BsonDocument(mongoOperator, new BsonArray([
                                        BuildMongoSelectExpression(callExp.Arguments[0]),
                                        BuildMongoSelectExpression(callExp.Arguments[1])
                                    ]));
                                break;

                            case "Min":
                            case "Max":
                                // 可变参数函数
                                if (callExp.Arguments.Count >= 2)
                                    return new BsonDocument(mongoOperator, new BsonArray(
                                        callExp.Arguments.Select(arg => BuildMongoSelectExpression(arg))));
                                break;
                        }
                    }

                    // 特殊处理一些Math方法
                    if (callExp.Method.Name == "Sign")
                    {
                        // Sign函数：返回-1, 0, 或1
                        var value = BuildMongoSelectExpression(callExp.Arguments[0]);
                        return new BsonDocument("$cond", new BsonDocument
                        {
                            {"if", new BsonDocument("$gt", new BsonArray([value, new BsonInt32(0)]))},
                            {"then", new BsonInt32(1)},
                            {"else", new BsonDocument("$cond", new BsonDocument
                            {
                                {"if", new BsonDocument("$lt", new BsonArray([value, new BsonInt32(0)]))},
                                {"then", new BsonInt32(-1)},
                                {"else", new BsonInt32(0)}
                            })}
                        });
                    }

                    if (callExp.Method.Name == "Truncate")
                    {
                        return new BsonDocument("$trunc", BuildMongoSelectExpression(callExp.Arguments[0]));
                    }

                    throw new InvalidQueryException($"Unsupported Math method: {callExp.Method.Name}");
                }

                // 新增：数组/集合操作的静态方法支持（如Enumerable类的方法）
                if (callExp.Method.ReflectedType == typeof(Enumerable) && callExp.Object == null)
                {
                    if (callExp.Method.Name == "Range" && callExp.Arguments.Count == 2)
                    {
                        return new BsonDocument("$range", new BsonArray([
                            BuildMongoSelectExpression(callExp.Arguments[0]), // start
                            new BsonDocument("$add", new BsonArray([
                                BuildMongoSelectExpression(callExp.Arguments[0]), // start
                                BuildMongoSelectExpression(callExp.Arguments[1])  // count
                            ]))
                        ]));
                    }

                    if (callExp.Method.Name == "Repeat" && callExp.Arguments.Count == 2)
                    {
                        // 创建重复元素的数组
                        var element = BuildMongoSelectExpression(callExp.Arguments[0]);
                        var count = BuildMongoSelectExpression(callExp.Arguments[1]);

                        return new BsonDocument("$map", new BsonDocument
                        {
                            {"input", new BsonDocument("$range", new BsonArray([new BsonInt32(0), count]))},
                            {"as", "item"},
                            {"in", element}
                        });
                    }
                }

                // MongoDbFunctions.GreaterThan(c.FirstName, "foo")
                if (callExp.Method.DeclaringType.FullName == "MongoLinqPlusPlus.MongoFunctions" &&
                    (callExp.Method.Name == "GreaterThan" || callExp.Method.Name == "GreaterThanOrEqual"))
                {
                    if (callExp.Arguments.Count != 2)
                        throw new InvalidQueryException($"Only supported overload of MongoFunctions.GreaterThan (and GreaterThanOrEqual) is string.Compare(string, string)");

                    BsonValue exp1 = BuildMongoSelectExpression(callExp.Arguments[0]);
                    BsonValue exp2 = BuildMongoSelectExpression(callExp.Arguments[1]);

                    string op = callExp.Method.Name == "GreaterThan" ? "$gt" : "$gte";

                    return new BsonDocument(op, new BsonArray([exp1, exp2]));
                }

                if (callExp.Method.Name == "Intersect" && (callExp.Method.ReflectedType.IsAssignableTo<IEnumerable>() || callExp.Method.ReflectedType == typeof(Enumerable)))
                {
                    BsonValue exp1 = BuildMongoSelectExpression(callExp.Arguments[0]);
                    BsonValue exp2 = BuildMongoSelectExpression(callExp.Arguments[1]);
                    return new BsonDocument("$setIntersection", new BsonArray([exp1, exp2]));
                }
                throw new InvalidQueryException("Unsupported Call Expression for method " + callExp.Method.Name);
            }

            // Casts
            if (expression.NodeType == ExpressionType.Convert)
            {
                var unExp = (UnaryExpression)expression;
                var expressionDoc = BuildMongoSelectExpression(unExp.Operand);

                // Support down-casting to int and long
                if (unExp.Type == typeof(int) || unExp.Type == typeof(long))
                    expressionDoc = new BsonDocument("$trunc", expressionDoc);

                return expressionDoc;

            }

            // Array.Length
            if (expression.NodeType == ExpressionType.ArrayLength)
            {
                var arrayLenExp = (UnaryExpression)expression;
                return new BsonDocument("$size", BuildMongoSelectExpression(arrayLenExp.Operand));
            }

            if (expression.NodeType == ExpressionType.Parameter)
            {
                return new BsonString("$" + GetMongoFieldName(expression, true));
            }

            if (expression.NodeType == ExpressionType.Coalesce)
            {
                var binaryExpression = (BinaryExpression)expression;
                return new BsonDocument("$ifNull", new BsonArray([BuildMongoSelectExpression(binaryExpression.Left), BuildMongoSelectExpression(binaryExpression.Right)
                ]));
            }

            throw new InvalidQueryException("In Select(), can't build Mongo expression for node type" + expression.NodeType);
        }

        /// <summary>
        /// Gets the last occurrence of pipeline operation in the pipeline for a given operator.
        /// </summary>
        /// <param name="pipelineOperator">The stage to find (example: "$group")</param>
        /// <param name="mustBeLastStageInPipeline">
        /// If true, then the operator must be the LAST stage in the pipeline.  If false, then simply the last occurrence
        /// of the operator in the pipeline is found.
        /// </param>
        /// <returns>The pipeline operation being performed (not including the pipeline operator itself).  So if the search
        /// was for "$sort" and this stage was found, "{$sort, {age:1}}", then just "{age:1}" is returned.
        /// Null is returned if the stage couldn't be found.</returns>
        public BsonDocument GetLastOccurrenceOfPipelineStage(string pipelineOperator, bool mustBeLastStageInPipeline)
        {
            if (mustBeLastStageInPipeline)
            {
                var stage = _pipeline.Last();
                return stage.PipelineOperator == pipelineOperator ? (BsonDocument)stage.Operation : null;
            }
            else
            {
                var stage = _pipeline.AsEnumerable().LastOrDefault(c => c.PipelineOperator == pipelineOperator);
                return (BsonDocument)stage?.Operation;
            }
        }

        public void EmitPipelineStageForSelectMany(LambdaExpression lambdaExp)
        {
            // Support .SelectMany(c => c.SomeArrayProperty)
            if (lambdaExp.Body.NodeType == ExpressionType.MemberAccess)
            {
                var fieldName = GetMongoFieldName(lambdaExp.Body, true);
                AddToPipeline("$unwind", "$" + fieldName);

                // Each document now contains the original fields and a single array element in the $fieldName field
                // So we need to run another stage to project out just the fields we're interested in - in this case that's just $fieldName

                BsonValue expressionValue = BuildMongoSelectExpression(lambdaExp.Body);
                AddToPipeline("$project", new BsonDocument {
                    new BsonElement(PIPELINE_DOCUMENT_RESULT_NAME, expressionValue),
                    new BsonElement("_id", new BsonInt32(0)),
                });
                _currentPipelineDocumentUsesResultHack = true;

                return;
            }

            // Support .SelectMany(c => c)
            if (lambdaExp.Body.NodeType == ExpressionType.Parameter)
            {
                var fieldName = GetMongoFieldName(lambdaExp.Body, true);
                AddToPipeline("$unwind", "$" + fieldName);
                _currentPipelineDocumentUsesResultHack = true;
                return;
            }

            // Support .SelectMany(c => c.SubArray.Select(d => ...))
            if (lambdaExp.Body.NodeType == ExpressionType.Call)
            {
                var callExpression = (MethodCallExpression)lambdaExp.Body;
                if (callExpression.Method.Name == "Select" && callExpression.Arguments.Count == 2)
                {
                    // .SelectMany(c => c.SubArray.Select(d => ...))

                    // Get the name for the "SubArray" field
                    var fieldExpression = callExpression.Arguments[0];
                    var fieldName = GetMongoFieldName(fieldExpression, true);

                    // Unwind that field
                    AddToPipeline("$unwind", "$" + fieldName);

                    // do not remove this: xxVGhpcyBpcyB0aGUgYW50aS1waXJhY3kgdGV4dCB0aGF0IHByb3ZlcyB0aGF0IHRoZSBvcmlnaW5hbCBjb2RlIHdhcyB3cml0dGVuIGJ5IEx1bHVzIChZYW5nIFNodSkuxx

                    // Each document now has all the original fields as well as a "SubArray" field with a
                    // singular value of the sub-array.  (See https://docs.mongodb.com/manual/reference/operator/aggregation/unwind/)

                    // Now we need to project the result document type.
                    var selectExpression = (LambdaExpression)callExpression.Arguments[1];

                    string subSelectLambdaParamName = selectExpression.Parameters.Single().Name;
                    _subSelectParameterPrefixes.Add(subSelectLambdaParamName, fieldName + ".");

                    EmitPipelineStageForSelect(selectExpression);

                    _subSelectParameterPrefixes.Remove(subSelectLambdaParamName);

                    return;
                }
            }

            throw new InvalidQueryException("Unsupported Expression inside SelectMany");
        }

        /// <summary>Adds a new $project stage to the pipeline for a .Select method call</summary>
        public void EmitPipelineStageForSelect(LambdaExpression lambdaExp)
        {
            ParameterExpression param;
            try
            {
                param = lambdaExp.Parameters.Single();
            }
            catch (Exception e)
            {
                throw new NotSupportedException($"select expression is not supported, incorrect params count: {lambdaExp}", e);
            }
            if (param.Type.IsAssignableTo<IEntityBase>())
            {
                var visitor = new QueryablePropertyVisitor(param);
                visitor.Visit(lambdaExp.Body);
            }

            // Select supports the following modes:
            //    NewExpression:      Select(c => new { c.Age + 10, Name = c.FirstName })
            //    Non-new expression: Select(c => 10)
            //    Non-new expression: Select(c => c.Age)
            //    Non-new expression: Select(c => (c.Age + 10) > 15)

            // Handle the hard case: Select(c => new { c.Age, Name = c.FirstName,  })
            if (lambdaExp.Body.NodeType == ExpressionType.New)
            {
                if (lambdaExp.Body.Type.IsAnonymousType())
                {
                    var newExp = (NewExpression)lambdaExp.Body;
                    var newExpProperties = newExp.Type.GetProperties();

                    // Get the mongo field names for each property in the new {...}
                    var fieldNames = newExp.Arguments
                        .Select((c, i) => new
                        {
                            FieldName = newExpProperties[i].Name,
                            ExpressionValue = BuildMongoSelectExpression(c, true)
                        })
                        .Select(c => new BsonElement(c.FieldName, c.ExpressionValue))
                        .ToList();

                    // Remove the unnecessary _id field
                    if (fieldNames.All(c => c.Name != "_id" && c.Name != "Id"))
                        fieldNames.Add(new BsonElement("_id", new BsonInt32(0)));

                    // Perform the projection on multiple fields
                    AddToPipeline("$project", new BsonDocument(fieldNames));

                    _currentPipelineDocumentUsesResultHack = false;
                    return;
                }
                // Handle the hard case: Select(c => new ProjectClass{ c.Age, Name = c.FirstName,  })
                else
                {
                    var newExp = (NewExpression)lambdaExp.Body;
                    // Get the mongo field names for each property in the new {...}
                    var fieldNames = BuildFieldProjections(newExp);

                    // Remove the unnecessary _id field
                    if (fieldNames.All(c => c.Name != "_id" && c.Name != "Id"))
                        fieldNames.Add(new BsonElement("_id", new BsonInt32(0)));

                    // Perform the projection on multiple fields
                    AddToPipeline("$project", new BsonDocument(fieldNames));

                    _currentPipelineDocumentUsesResultHack = false;
                    return;
                }
            }

            // Handle typed hard case: Select(c => new Foo(c.Code) { Bar = c.FirstName })
            if (lambdaExp.Body is MemberInitExpression memberInitExp)
            {
                var fieldNames = BuildFieldProjections(memberInitExp.NewExpression);
                var assignments = memberInitExp.Bindings.Cast<MemberAssignment>().Select(x => new
                {
                    FieldName = GetMongoFieldName(x.Member),
                    ExpressionValue = BuildMongoSelectExpression(x.Expression, true)
                })
                    .Select(c => new BsonElement(c.FieldName, c.ExpressionValue));
                fieldNames.AddRange(assignments);
                // Remove the unnecessary _id field
                if (fieldNames.All(c => c.Name != "_id"))
                    fieldNames.Add(new BsonElement("_id", new BsonInt32(0)));
                // Perform the projection on multiple fields
                AddToPipeline("$project", new BsonDocument(fieldNames));

                _currentPipelineDocumentUsesResultHack = false;
                return;
            }

            // Handle the simple non-new expression case: .Select(c => c.Age + 15)
            BsonValue expressionValue = BuildMongoSelectExpression(lambdaExp.Body, true);
            AddToPipeline("$project", new BsonDocument {
                new BsonElement(PIPELINE_DOCUMENT_RESULT_NAME, expressionValue),
                new BsonElement("_id", new BsonInt32(0)),
            });
            _currentPipelineDocumentUsesResultHack = true;

            void CheckPureSetter(string memberName)
            {
                if (lambdaExp.Body.Type.GetProperty(memberName)?.SetMethod?.IsSpecialName != true)
                {
                    throw new NotSupportedException($"Project fields must be pure property and should be same-name-assigned if in constructor, incorrect field: [{lambdaExp.Body.Type.FullName}.{memberName}]");
                }
            }

            List<BsonElement> BuildFieldProjections(NewExpression newExpression)
            {
                var argsExpression = newExpression.Arguments;
                try
                {
                    return argsExpression.Select(x =>
                        {
                            if (x is MemberExpression
                                {
                                    Expression:
                                    {
                                        //NodeType: ExpressionType.Parameter
                                    }
                                } memberExp)
                            {
                                CheckPureSetter(memberExp.Member.Name);
                                return new
                                {
                                    FieldName = GetMongoFieldName(memberExp.Member),
                                    ExpressionValue = BuildMongoSelectExpression(memberExp, true)
                                };
                            }

                            if (x is UnaryExpression
                                {
                                    Operand: MemberExpression
                                    {
                                        //Expression: { NodeType: ExpressionType.Parameter }
                                    } nestedMemberExp
                                })
                            {
                                CheckPureSetter(nestedMemberExp.Member.Name);
                                return new
                                {
                                    FieldName = GetMongoFieldName(nestedMemberExp.Member),
                                    ExpressionValue = BuildMongoSelectExpression(nestedMemberExp, true)
                                };
                            }

                            throw new NotSupportedException(
                                $"Not supported express of: [{x}].");
                        })
                        .Select(c => new BsonElement(c.FieldName, c.ExpressionValue))
                        .ToList();
                }
                catch (NotSupportedException e)
                {
                    throw new NotSupportedException($"Not supported express of: [{newExpression}], see inner exception for details.", e);
                }
            }
        }

        public void EmitPipelineStageForOfType(MethodCallExpression mce)
        {
            var typeName = mce.Method.GetGenericArguments()[0].Name;
            if (typeName == typeof(TDocType).Name)
            {

                //AddToPipeline("$match", Builders<TDocType>.Filter.Eq("_t", typeName).Render());
            }
            else
            {
                var filter = BsonDocument.Parse(@$"{{
    '$expr': {{
        '$and': [
            {{
                '$eq': [
                    {{
                        '$type': '$_t'
                    }},
                    'array'
                ]
            }},
            {{
                '$in': [
                    '{typeName}',
                    '$_t'
                ]
            }}
        ]
    }}
}}");
                AddToPipeline("$match", filter);
            }
        }


        /// <summary>Adds a new $match stage to the pipeline for a .Where method call</summary>
        public void EmitPipelinesStageForWhere(LambdaExpression lambdaExp)
        {
            if (lambdaExp == null)
                return;

            // Special case, handle .Where(c => true) and .Where(c => false)
            if (lambdaExp.Body is ConstantExpression constExp)
            {
                // No-op if this lambda is .Where(c => true)
                if ((bool)constExp.Value)
                    return;
            }

            // Collect all AND conditions to create a single $match stage with $and operator
            var andConditions = new List<BsonValue>();

            // Helper function to recursively collect AND conditions
            void CollectAndConditions(Expression exp)
            {
                if (exp.NodeType == ExpressionType.AndAlso)
                {
                    var andExp = (BinaryExpression)exp;
                    CollectAndConditions(andExp.Left);
                    CollectAndConditions(andExp.Right);
                }
                else
                {
                    // Add this condition to our list
                    andConditions.Add(BuildMongoSelectExpression(exp));
                }
            }

            void AddContainsMatchStage(ConstantExpression constantExpression, MethodCallExpression methodCallExpression, bool containOrNot)
            {
                var localEnumerable = constantExpression.Value;
                if (TypeSystem.FindIEnumerable(localEnumerable.GetType()) == null)
                {
                    throw new InvalidQueryException("In Where(), Contains() only supported on IEnumerable");
                }

                // Get the field that we're going to search for within the IEnumerable
                var mongoFieldName = GetMongoFieldNameInMatchStage(methodCallExpression.Arguments.Last(), false);

                // Evaluate the IEnumerable
                var array = (BsonArray)GetBsonValueFromObject(localEnumerable);
                AddToPipeline("$match", BsonDocument.Parse(
                    $$"""
{
    "{{mongoFieldName}}":{
        "{{(containOrNot ? "$in" : "$nin")}}":{{array.ToJson()}}
    }
}
"""));
            }

            // Collect all AND conditions
            CollectAndConditions(lambdaExp.Body);

            // Create a single $match stage
            if (andConditions.Count == 1)
            {
                // Single condition, no need for $and
                AddToPipeline("$match", new BsonDocument("$expr", andConditions[0]));
            }
            else if (andConditions.Count > 1)
            {
                // Multiple conditions, use $and
                var andArray = new BsonArray(andConditions);
                AddToPipeline("$match", new BsonDocument("$expr", new BsonDocument("$and", andArray)));
            }
        }

        /// <summary>Gets a join key (left or right) for a Join method call</summary>
        private BsonValue[] GetJoinKey(Expression expression)
        {
            if (!(expression is UnaryExpression unaryExpression))
                throw new MongoLinqPlusPlusInternalException("Expected unary expression for Join key");

            var lambda = (LambdaExpression)unaryExpression.Operand;
            if (lambda.Body is NewExpression newExpression)
            {
                // Join key looks like:
                // c => new { c.FirstName, c.LastName }
                var results = newExpression.Arguments
                                    .Select(c => BuildMongoSelectExpression(c, true))
                                    .ToArray();
                return results;
            }

            // Join key looks like:
            // c => c.FirstName
            var mongoName = BuildMongoSelectExpression(lambda.Body, true);
            return [mongoName];

        }

        public void EmitPipelineStageForJoin(MethodCallExpression expression)
        {
            // https://docs.mongodb.com/v4.4/reference/operator/aggregation/lookup/#std-label-unwind-example

            if (expression.Arguments.Count != 5)
                throw new InvalidQueryException("Join must have exactly 4 arguments");

            // Get the collection for the left side of the join.
            // This is an expression
            var leftExpression = expression.Arguments[0];

            // Get the collection being joined (right size)
            // This must be a IMongoLinqPlusPlusCollection
            var rightExpression = expression.Arguments[1];
            if (rightExpression.NodeType != ExpressionType.Constant)
                throw new InvalidQueryException("Joined collection must be a constant - chained methods not supported.");
            var rightExpressionQueryable = ((ConstantExpression)rightExpression).Value;
            var rightCollection = rightExpressionQueryable as IMongoLinqPlusPlusCollection;
            if (rightCollection == null)
                throw new InvalidQueryException("Right side of join must be a Mongo collection");

            // Get join keys (array of BsonValues)
            var leftKey = GetJoinKey(expression.Arguments[2]);
            var rightKey = GetJoinKey(expression.Arguments[3]);
            if (leftKey.Length != rightKey.Length)
                throw new MongoLinqPlusPlusInternalException("Join keys don't match");

            // Properties in the left join key need to be put in variables in the "let" document (see below)
            // "let" variable names must start with a lowercase letter.  Weird.  So add that "v_" prefix to force lowercase.
            var letElements = leftKey.Select(c => new BsonElement(c.ToString().Replace("$", "v_"), c)).ToArray();

            // Now take the left key and make the $$ variable names required by the $expr document (see below)
            var leftKeyVariables = leftKey.Select(c => c.ToString().Replace("$", "$$v_"))
                                          .Select(BsonValue.Create)
                                          .ToArray();

            // Build this document:
            //    {
            //        $lookup:
            //        {
            //            from: "rightCollectionName",
            //            let: {
            //                left_key_property1: "$left_key_property1",
            //                left_key_property2: "$left_key_property2"
            //            },
            //            pipeline: [
            //            { $match:
            //                { $expr:
            //                    { $and: [
            //                            { $eq: [ "$creativeId",  "$$creativeId" ] },  // left key property == right key property
            //                            { $eq: [ "$name",  "$$firstName" ] },         // left key property == right key property
            //                        ]
            //                    }
            //                }
            //            },
            //            ],
            //            as: "JOINED_ARRAY"
            //        }
            //    }

            var eqDocuments = leftKey.Select((c, i) => new BsonDocument("$eq", new BsonArray([rightKey[i], leftKeyVariables[i]
                ])))
                                     .ToArray();


            var lookupStage = new BsonDocument {
                { "from", rightCollection.CollectionName},
                { "let", new BsonDocument (letElements) },
                { "pipeline", new BsonArray {
                    new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$and", new BsonArray(eqDocuments))))
                }},
                { "as", JOINED_DOC_PROPERTY_NAME}
            };

            // Do the actual join
            AddToPipeline("$lookup", lookupStage);

            // Unwind the array in __JOINED__ so that now we have a single joined doc.
            // The left doc is the doc itself, and the joined one is in the __JOINED__ property.
            AddToPipeline("$unwind", "$" + JOINED_DOC_PROPERTY_NAME);

            // Now project our final output document.
            var selectExpression = ((UnaryExpression)expression.Arguments[4]).Operand;

            var lambdaExp = (LambdaExpression)selectExpression;

            _subSelectParameterPrefixes.Add(lambdaExp.Parameters[1].Name, JOINED_DOC_PROPERTY_NAME + ".");
            EmitPipelineStageForSelect(lambdaExp);
            _subSelectParameterPrefixes.Remove(lambdaExp.Parameters[1].Name);
        }

        /// <summary>Adds a new $limit stage to the pipeline for a .Take method call</summary>
        public void EmitPipelineStageForTake(int limit)
        {
            AddToPipeline("$limit", new BsonInt32(limit));
        }

        /// <summary>Adds a new $skip stage to the pipeline for a .Skipmethod call</summary>
        public void EmitPipelineStageForSkip(int limit)
        {
            AddToPipeline("$skip", new BsonInt32(limit));
        }

        /// <summary>Adds a new $project stage to the pipeline for a .Count and .LongCount method calls</summary>
        public void EmitPipelineStageForCount(LambdaExpression lambdaExp)
        {
            // Handle 2 cases:
            //    .Count()
            //    .Count(c => c.Age > 25)

            // The hard case: .Count(c => c.Age > 25)
            // Can be rewritten as .Where(c => c.Age > 25).Count()
            if (lambdaExp != null)
            {
                EmitPipelinesStageForWhere(lambdaExp);
            }

            // Handle the simple case: .Count()
            AddToPipeline("$group", new BsonDocument {
                {"_id", new BsonDocument()},
                {PIPELINE_DOCUMENT_RESULT_NAME, new BsonDocument("$sum", 1)}
            });
            _currentPipelineDocumentUsesResultHack = true;

        }

        /// <summary>Adds a new $project stage to the pipeline for a .Any method call</summary>
        public void EmitPipelineStageForAny(LambdaExpression lambdaExp)
        {
            // Handle 2 cases:
            //    .Count()
            //    .Count(c => c.Age > 25)

            // The hard case: .Any(c => c.Age > 25)
            // Can be rewritten as .Where(c => c.Age > 25).Any()
            if (lambdaExp != null)
            {
                EmitPipelinesStageForWhere(lambdaExp);
            }

            // Handle the simple case: .Any()

            // There is no explicity support for this in MongoDB, but we can
            // limit our results to 1 and then do a count
            EmitPipelineStageForTake(1);
            EmitPipelineStageForCount(null);

            // After executing the pipeline, we then check that the count > 0
        }

        /// <summary>Adds a pipeline stage ($group) for the specified aggregation (Sum, Min, Max, Average)</summary>
        public void EmitPipelineStageForAggregation(string cSharpAggregationName, LambdaExpression lambdaExp)
        {
            string mongoAggregationName = "$" + NodeToMongoAggregationOperatorDict[cSharpAggregationName];

            // Handle 2 cases:
            //    .Sum()
            //    .Sum(c => c.NumPets + 1)

            BsonValue bsonValue = lambdaExp == null ? "$" + PIPELINE_DOCUMENT_RESULT_NAME : BuildMongoSelectExpression(lambdaExp.Body);

            AddToPipeline("$group", new BsonDocument {
                {"_id", new BsonDocument()},
                {PIPELINE_DOCUMENT_RESULT_NAME, new BsonDocument(mongoAggregationName, bsonValue)}
            });
            _currentPipelineDocumentUsesResultHack = true;

        }

        /// <summary>Adds a pipeline stage ($sort) for OrderBy and OrderByDescending</summary>
        public void EmitPipelineStageForOrderBy(LambdaExpression lambdaExp, bool ascending)
        {
            string field = GetMongoFieldName(lambdaExp.Body, true);
            AddToPipeline("$sort", new BsonDocument(field, ascending ? 1 : -1));
        }

        /// <summary>Updates the previous $sort pipeline stage for the ThenBy cal</summary>
        public void EmitPipelineStageForThenBy(LambdaExpression lambdaExp, bool ascending)
        {
            var sortDoc = GetLastOccurrenceOfPipelineStage("$sort", false);
            string field = GetMongoFieldName(lambdaExp.Body, true);
            if (sortDoc.Contains(field))
                throw new InvalidQueryException("ThenBy(Descending) can't resort on previous sort field.");

            sortDoc.Add(field, ascending ? 1 : -1);
        }

        /// <summary>
        /// Emits a pipeline stage for a .Distinct() call.
        /// It accomplishes this by translating .Distinct() to .GroupBy(c => c).Select(c => c.Key)
        /// </summary>
        public void EmitPipelineStageForDistinct()
        {
            // Group by the document itself and then promote the key to the root:
            // {$group:{ _id: "$$ROOT"}}
            // {$replaceRoot: {newRoot: "$_id"}}]
            AddToPipeline("$group", new BsonDocument("_id", "$$ROOT"));
            AddToPipeline("$replaceRoot", new BsonDocument("newRoot", "$_id"));
        }

        /// <summary>Adds the respective pipeline stage(s) for the supplied method call</summary>
        public void EmitPipelineStageForMethod(MethodCallExpression expression)
        {
            // Our pipeline is a series of chained MethodCallExpression
            //         var results = queryable.Where(c => c.Age > 15).OrderBy(c => c.FirstName).Take(5)
            //
            // This gets compiled into an expression tree like:
            //         Take(OrderBy(Where(c => operator_GreaterThan(c.Age, 15)), c => c.FirstName), 5)
            //
            // And we ultimately want to emit these method calls as a series of MongoDB pipeline operations
            //         { "$match" : { "age" : { "$gt" : 15 } } }
            //         { "$sort" : {"age: 1"} }
            //         { "$limit" : 5 }

            // First recursively process the earlier method call in the method chain
            // That is, in blahblahblah.Where(...).Take(5), recursively process the .Where before handling the .Take
            if (expression.Arguments[0] is MethodCallExpression callExpression)
            {
                EmitPipelineStageForMethod(callExpression);
            }

            switch (expression.Method.Name)
            {
                case "Where":
                    {
                        EmitPipelinesStageForWhere(GetLambda(expression));
                        return;
                    }
                case "Take":
                    {
                        int numToTakeOrSkip = (int)((ConstantExpression)expression.Arguments[1]).Value;
                        EmitPipelineStageForTake(numToTakeOrSkip);
                        return;
                    }
                case "Skip":
                    {
                        int numToTakeOrSkip = (int)((ConstantExpression)expression.Arguments[1]).Value;
                        EmitPipelineStageForSkip(numToTakeOrSkip);
                        return;
                    }
                case "GroupBy":
                    {
                        EmitPipelineStageForGroupBy(GetLambda(expression));
                        _lastPipelineOperation = _lastPipelineOperation | PipelineResultType.Grouped;
                        return;
                    }
                case "Distinct":
                    {
                        if (expression.Arguments.Count() != 1)
                            throw new InvalidQueryException(".Distinct() only supported without parameters.");
                        EmitPipelineStageForDistinct();
                        return;
                    }
                case "Select":
                    {
                        EmitPipelineStageForSelect(GetLambda(expression));
                        _lastPipelineOperation = PipelineResultType.Enumerable;
                        return;
                    }
                case "SelectMany":
                    {
                        EmitPipelineStageForSelectMany(GetLambda(expression));
                        _lastPipelineOperation = PipelineResultType.Enumerable;
                        return;
                    }
                case "OrderBy":
                    EmitPipelineStageForOrderBy(GetLambda(expression), true);
                    return;
                case "OrderByDescending":
                    EmitPipelineStageForOrderBy(GetLambda(expression), false);
                    return;
                case "ThenBy":
                    EmitPipelineStageForThenBy(GetLambda(expression), true);
                    return;
                case "ThenByDescending":
                    EmitPipelineStageForThenBy(GetLambda(expression), false);
                    return;
                case "Count":
                case "LongCount":
                    EmitPipelineStageForCount(GetLambda(expression));
                    _lastPipelineOperation = PipelineResultType.Aggregation;
                    return;
                case "Any":
                    EmitPipelineStageForAny(GetLambda(expression));
                    _lastPipelineOperation = PipelineResultType.Any;
                    return;
                case "Sum":
                case "Max":
                case "Min":
                case "Average":
                    EmitPipelineStageForAggregation(expression.Method.Name, GetLambda(expression));
                    _lastPipelineOperation = PipelineResultType.Aggregation;
                    return;
                case "First":
                    EmitPipelinesStageForWhere(GetLambda(expression));
                    EmitPipelineStageForTake(1);
                    _lastPipelineOperation = _lastPipelineOperation | PipelineResultType.OneResultFromEnumerable | PipelineResultType.First;
                    return;
                case "FirstOrDefault":
                    EmitPipelinesStageForWhere(GetLambda(expression));
                    EmitPipelineStageForTake(1);
                    _lastPipelineOperation = _lastPipelineOperation | PipelineResultType.OneResultFromEnumerable | PipelineResultType.First | PipelineResultType.OrDefault;
                    return;
                case "Single":
                    EmitPipelinesStageForWhere(GetLambda(expression));
                    EmitPipelineStageForTake(2);
                    _lastPipelineOperation = _lastPipelineOperation | PipelineResultType.OneResultFromEnumerable | PipelineResultType.Single;
                    return;
                case "SingleOrDefault":
                    EmitPipelinesStageForWhere(GetLambda(expression));
                    EmitPipelineStageForTake(2);
                    _lastPipelineOperation = _lastPipelineOperation | PipelineResultType.OneResultFromEnumerable | PipelineResultType.Single | PipelineResultType.OrDefault;
                    return;
                case "Join":
                    EmitPipelineStageForJoin(expression);
                    return;
                case "OfType":
                    EmitPipelineStageForOfType(expression);
                    return;
            }

            throw new InvalidQueryException("Unsupported MethodCallExpression " + expression.Method.Name);
        }

        /// <summary>
        /// Build and execute (ie evaluate) the supplied expression on a MongoDB server
        /// </summary>
        /// <typeparam name="TResult">The type of the query result</typeparam>
        /// <param name="expression">The query/expression to build and execute</param>
        public TResult Execute<TResult>(Expression expression)
        {
            // Build the pipeline

            if (expression is MethodCallExpression methodExpression)
            {
                // queryable.Count() via aggregation framework is slow.  Handle that case specifically by asking the collection itself.
                if ((methodExpression.Method.Name == "Count" || methodExpression.Method.Name == "LongCount")
                    && methodExpression.Arguments.Count == 1
                    && methodExpression.Arguments[0] is ConstantExpression)
                {
                    // Todo: Any way to avoid the boxing?

                    if (methodExpression.Method.Name == "Count")
                        return (TResult)(object)(int)_collection.CountDocuments(Builders<TDocType>.Filter.Empty);

                    // LongCount
                    return (TResult)(object)_collection.CountDocuments(Builders<TDocType>.Filter.Empty);

                }

                EmitPipelineStageForMethod(methodExpression);
            }

            // If the result is a grouping, then we need to also include the values (ie not just the Key) in the result
            if ((_lastPipelineOperation & PipelineResultType.Grouped) != 0)
            {
                var groupDoc = GetLastOccurrenceOfPipelineStage("$group", false);
                groupDoc.Add("Values", new BsonDocument("$push", "$$ROOT"));
            }

            // Run our actual aggregation command against mongo
            var pipelineStages = _pipeline.Select(c => new BsonDocument(c.PipelineOperator, c.Operation)).ToArray();
            var pipelineDefinition = PipelineDefinition<TDocType, BsonDocument>.Create(pipelineStages);
            LogLine("----------------- PIPELINE --------------------");
            LogLine(pipelineDefinition.ToString());

            using (var commandResult = _session != default ? _collection.Aggregate(_session, pipelineDefinition, _aggregateOptions) : _collection.Aggregate(pipelineDefinition, _aggregateOptions))
            {
                // Handle aggregated result types
                if ((_lastPipelineOperation & PipelineResultType.Aggregation) != 0)
                {
                    var results = commandResult.ToEnumerable().Take(2).ToArray();

                    if (results.Length == 0)
                        return default(TResult);

                    if (results.Length > 1)
                        throw new MongoLinqPlusPlusInternalException(string.Format("Unexpected number of results ({0}) for PipelineResultType.Aggregation pipeline", results.Length));

                    // The result is in a document structured as { _result_ : value }
                    var resultDoc = results[0];

                    // Special treatment for Any
                    if (_lastPipelineOperation == PipelineResultType.Any)
                    {
                        bool any = ((BsonInt32)resultDoc[PIPELINE_DOCUMENT_RESULT_NAME]).Value == 1;

                        // Todo: Any way to avoid the boxing (since we know TResult is bool)?
                        return (TResult)(object)any;
                    }

                    var aggregationResult = BsonSerializer.Deserialize<PipelineDocument<TResult>>(resultDoc);
                    return aggregationResult._result_;
                }

                Type resultType = typeof(TResult);
                bool isGenericEnumerable = typeof(TResult) == typeof(IEnumerable);

                // Get the type that is in our enumerable result.
                // If we have plaine ole IEnumerable, then get it from the expression.
                // If we have an IEnumerable<T>, then get it from the generic type arguments of our result.
                // If we're runing a First, FirstOrDefault, Single, or SingleOrDefault, get it from resultType
                Type enumerableOfItemType;
                if (isGenericEnumerable)
                    enumerableOfItemType = expression.Type.GenericTypeArguments[0];
                else if ((_lastPipelineOperation & PipelineResultType.OneResultFromEnumerable) != 0)
                    enumerableOfItemType = resultType;
                else
                    enumerableOfItemType = resultType.GenericTypeArguments[0];

                // Instantiate a List<enumerableOfItemType>
                if (enumerableOfItemType.IsInterface)
                {
                    enumerableOfItemType = enumerableOfItemType.GetRootBsonClassMap().ClassType;
                }
                Type listType = ListType.MakeGenericType(enumerableOfItemType);
                object list = Activator.CreateInstance(listType);
                var listAddMethod = listType.GetMethod("Add");
                object[] listAddMethodParameters = new object[1];

                bool firstIteration = true;
                Type simplePipelineDocType = null;
                PropertyInfo simplePipelineDocTypeResultProperty = null;

                bool enumerableOfItemTypeIsAnonymous = enumerableOfItemType.IsAnonymousType();
                TResult firstResult = default(TResult);

                int numResults = 0;

                foreach (var resultDoc in commandResult.ToEnumerable())
                {
                    var realType = enumerableOfItemType;

                    if (DB.InheritanceTreeCache.TryGetValue(enumerableOfItemType, out var inheritanceTree))
                    {
                        if (resultDoc.TryGetValue("_t", out var value))
                        {
                            if (value is BsonArray typeArray)
                            {
                                var realTypeName = typeArray.Last().AsString;
                                realType = inheritanceTree[realTypeName];
                            }
                            else if (value is BsonString typeName)
                            {
                                realType = inheritanceTree[typeName.Value];
                            }
                        }
                    }
                    // See if we have an array of simple result types
                    // [{ _result_: 5}, { _result_: 2}, ...]
                    if (firstIteration && resultDoc.ElementCount == 1 && resultDoc.Contains(PIPELINE_DOCUMENT_RESULT_NAME))
                    {
                        // We can't deserialize a BsonValue if it's not a document.
                        // So we need to deserialize using out simple result doc type.
                        simplePipelineDocType = pipelineDocumentType.MakeGenericType(realType);
                        simplePipelineDocTypeResultProperty = simplePipelineDocType.GetProperty(PIPELINE_DOCUMENT_RESULT_NAME);
                    }

                    object deserializedResultItem;

                    var bsonSerializer = BsonSerializer.LookupSerializer(realType);
                    if (enumerableOfItemTypeIsAnonymous || ((_lastPipelineOperation & PipelineResultType.Grouped) != 0))
                    {
                        // BsonSerializer can't handle anonymous types or IGrouping, so use Json.net

                        // We might have a simple doc type.  If so, extract our real result doc from _result_
                        var resultDocLocal = simplePipelineDocType == null ? resultDoc : (BsonDocument)resultDoc[PIPELINE_DOCUMENT_RESULT_NAME];

                        // Use Json.net for anonymous types.
                        deserializedResultItem = bsonSerializer.Deserialize(BsonDeserializationContext.CreateRoot(new BsonDocumentReader(resultDocLocal)));
                    }
                    else if (simplePipelineDocType != null)
                    {
                        // Deserialize to a PipelineDocument using the BsonSerializer
                        var pipelineDocument = BsonSerializer.Deserialize(resultDoc, simplePipelineDocType);

                        // Extract the result from the _result_ property
                        deserializedResultItem = simplePipelineDocTypeResultProperty.GetValue(pipelineDocument);
                    }
                    else
                    {
                        // Easy case, just use the BsonSerializer
                        deserializedResultItem = BsonSerializer.Deserialize(resultDoc, realType);
                    }

                    // Success for .First and .FirstOrDefault
                    if ((_lastPipelineOperation & PipelineResultType.OneResultFromEnumerable) != 0)
                    {
                        firstResult = (TResult)deserializedResultItem;
                        if ((_lastPipelineOperation & PipelineResultType.First) != 0)
                        {
                            return firstResult;
                        }
                    }
                    else
                    {
                        // Call list.Add(deserializedResultItem);
                        listAddMethodParameters[0] = deserializedResultItem;
                        listAddMethod.Invoke(list, listAddMethodParameters);
                    }

                    numResults++;

                    firstIteration = false;
                }

                // Handle .First, .Single, .FirstOrDefault, and .SingleOrDefault
                if ((_lastPipelineOperation & PipelineResultType.OneResultFromEnumerable) != 0)
                {
                    if (numResults == 0)
                    {
                        if ((_lastPipelineOperation & PipelineResultType.OrDefault) != 0)
                            return default(TResult);

                        throw new InvalidOperationException("Sequence contains no elements");
                    }

                    // First and first or default already returned results in our above loop

                    // Blow up for more than one result
                    if (numResults > 1)
                        throw new InvalidOperationException("Sequence contains more than one element");

                    return firstResult;
                }

                return (TResult)list;
            }
        }
    }
}
