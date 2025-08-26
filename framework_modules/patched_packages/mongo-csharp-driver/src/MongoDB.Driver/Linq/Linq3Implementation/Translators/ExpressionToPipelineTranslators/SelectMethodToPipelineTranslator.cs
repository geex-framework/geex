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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class SelectMethodToPipelineTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.Select))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                var selectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                if (selectorLambda.Body == selectorLambda.Parameters[0])
                {
                    return pipeline; // ignore identity projection: Select(x => x)
                }

                // check for client side projection after handling identity projection
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var sourceSerializer = pipeline.OutputSerializer;
                AstProjectStage projectStage;
                IBsonSerializer projectionSerializer;
                try
                {
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorLambda, sourceSerializer, asRoot: true);
                    (projectStage, projectionSerializer) = ProjectionHelper.CreateProjectStage(selectorTranslation);
                }
                catch (ExpressionNotSupportedException) when (context.TranslationOptions?.EnableClientSideProjections ?? false)
                {
                    (projectStage, projectionSerializer) = ClientSideProjectionTranslator.CreateProjectSnippetsStage(context, selectorLambda, sourceSerializer);
                }

                return projectStage == null ?
                    pipeline.WithNewOutputSerializer(projectionSerializer) : // project directly off $$ROOT with no $project stage
                    pipeline.AddStage(projectStage, projectionSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
