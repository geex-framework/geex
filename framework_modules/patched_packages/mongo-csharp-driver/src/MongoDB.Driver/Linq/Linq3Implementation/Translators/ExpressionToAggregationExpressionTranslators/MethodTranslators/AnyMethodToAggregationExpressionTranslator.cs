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

using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class AnyMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __anyMethods =
        {
            EnumerableMethod.Any,
            QueryableMethod.Any
        };

        private static readonly MethodInfo[] __anyWithPredicateMethods =
        {
            EnumerableMethod.AnyWithPredicate,
            QueryableMethod.AnyWithPredicate,
            ArrayMethod.Exists
        };

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            var sourceExpression = method.IsStatic ? arguments[0] : expression.Object;
            var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
            NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

            if (method.IsOneOf(__anyMethods))
            {
                var ast = AstExpression.Gt(AstExpression.Size(sourceTranslation.Ast), 0);
                return new TranslatedExpression(expression, ast, new BooleanSerializer());
            }

            if (method.IsOneOf(__anyWithPredicateMethods) || ListMethod.IsExistsMethod(method))
            {
                var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, method.IsStatic ? arguments[1] : arguments[0]);
                var predicateParameter = predicateLambda.Parameters[0];
                var predicateParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                var predicateSymbol = context.CreateSymbol(predicateParameter, predicateParameterSerializer);
                var predicateContext = context.WithSymbol(predicateSymbol);
                var predicateTranslation = ExpressionToAggregationExpressionTranslator.Translate(predicateContext, predicateLambda.Body);

                var ast = AstExpression.AnyElementTrue(
                    AstExpression.Map(
                        input: sourceTranslation.Ast,
                        @as: predicateSymbol.Var,
                        @in: predicateTranslation.Ast));

                return new TranslatedExpression(expression, ast, BooleanSerializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
