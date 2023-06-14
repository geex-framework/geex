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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

namespace MongoDB.Entities.InnerQuery
{
    internal class ExpressionSimplifier : ExpressionVisitor
    {
        /// <summary>
        /// Simplify an expression by evaluating immediately any sub-trees
        /// that exist entirely locally.  For now, that means anything that's
        /// not referenced from a Lambda parameter.  If nothing can be evaulated
        /// locally, then the original expression is returned.  Otherwise, a new
        /// expression is returned.
        /// </summary>
        /// <param name="rootQueryable">Queryable this expression is being executed against</param>
        /// <param name="expression">Expression to simplify</param>
        public static Expression Simplify(object rootQueryable, Expression expression)
        {
            // Simplification is accomplished in two passes.

            // Pass one, find ALL nodes that can be evaluated locally.
            var simplifier = new ExpressionSimplifier();
            simplifier.SaveIfSimplifiable(rootQueryable, expression);

            // Pass two, do a depth first search of the tree greedily evaluating
            // nodes whenever possible.
            return simplifier.Visit(expression);
        }

        //////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set of all expressions that can be evaluated locally.
        /// This gets built by SaveIfSimplifiable.
        /// </summary>
        private HashSet<Expression> _simplifiableExpressions = new HashSet<Expression>();

        /// <summary>Prevent instantiation.  Public access is through the static Simplify method.</summary>
        private ExpressionSimplifier() { }

        /// <summary>
        /// Saves the expression to _simplifiableExpressions if it can be simplified.   All child expressions
        /// (and their children etc) that can also be simplified are also saved to _simplifiableExpressions.
        /// </summary>
        /// <param name="rootQueryable">Queryable this expression is being executed against</param>
        /// <param name="expression">Expression to save if simplifiable</param>
        /// <returns>True if the specified expression can be simplified (ie evaluated locally).  Otherwise false.</returns>
        private bool SaveIfSimplifiable(object rootQueryable, Expression expression)
        {
            var children = GetChildren(expression);

            if (expression.NodeType == ExpressionType.Constant)
            {
                if (children.Any())
                    throw new InvalidOperationException("Invalid internal state.");

                // Handle special case.  Don't allow our queryable constant node to be simplified.
                var constExp = (ConstantExpression) expression;
                if (constExp.Value == rootQueryable)
                    return false;

                // Be sneaky, we don't want to bother compiling ConstantExpressions by themselves.
                // So don't actually include ConstantExpression in _simplifiableExpressions.
                // Still return true though so the Parent of THIS node can determine if it itself
                // can be evaluated locally.
                return true;
            }

            // TODO: Improve logic here so that some parameter expressions can be evaluated locally.
            // Right now, this doesn't simplify: .Where(c => c.Age == localArray.Count(d => d.Years))
            if (expression.NodeType == ExpressionType.Parameter)
            {
                if (children.Any())
                    throw new InvalidOperationException("Invalid internal state.");

                return false;
            }

            // Recurse!
            var childrenResults = children.Select(c => new {
                                              Child = c,
                                              CanBeUsedInSimplification = SaveIfSimplifiable(rootQueryable, c)
                                          })
                                          .ToArray();

            // Don't allow Lambdas to be simplified by themselves but allow them to be used in other
            // expressions that can be evaluated locally.
            if (expression.NodeType == ExpressionType.Lambda || expression.NodeType == ExpressionType.Quote)
                return true;

            // This node can be used in a simplification if all children can
            if (childrenResults.All(c => c.CanBeUsedInSimplification))
            {
                _simplifiableExpressions.Add(expression);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the child expression nodes for a given expression.  If none, an empty array is returned.
        /// </summary>
        private Expression[] GetChildren(Expression expression)
        {
            if (expression is ParameterExpression)
                return new Expression[0];

            if (expression is ConstantExpression)
                return new Expression[0];

            if (expression is BinaryExpression binExp)
                return new[] { binExp.Left, binExp.Right };

            if (expression is UnaryExpression unExp)
                return new[] { unExp.Operand };

            if (expression is ConditionalExpression condExp)
                return new[] { condExp.Test, condExp.IfTrue, condExp.IfFalse };

            if (expression is MethodCallExpression callExp)
            {
                var children = callExp.Arguments.Where(c => c != null).ToList();
                if (callExp.Object != null)
                    children.Add(callExp.Object);
                return children.ToArray();
            }

            if (expression is MemberExpression memExp)
            {
                // memExp can be null if accessing a member of a static class
                return memExp.Expression == null ? new Expression[0] : new[] { memExp.Expression };
            }

            if (expression is NewExpression newExp)
                return newExp.Arguments.ToArray();

            if (expression is LambdaExpression lambdaExp)
                return new[] { lambdaExp.Body };

            if (expression is NewArrayExpression newArrayExp)
                return newArrayExp.Expressions.ToArray();

            if (expression is MemberInitExpression memberInitExp)
            {
                var children = memberInitExp.Bindings
                                            .Cast<MemberAssignment>()
                                            .Select(c => c.Expression)
                                            .Union(new[] { memberInitExp.NewExpression })
                                            .ToArray();
                return children;
            }

            throw new InvalidQueryException("Unhandled type " + expression.NodeType + " in ExpressionSimplifier.GetChildren");
        }

        /// <summary>
        /// Evaluate an expression locally.  If the expression is a ConstantExpression then we
        /// don't bother to evaluate it and just return it back.  Otherwise, we compile the
        /// expression and evaluate it locally and return the value as a new ConstantExpression.
        /// </summary>
        /// <param name="expression">Expression to evaluate</param>
        private Expression EvaluateLocally(Expression expression)
        {
            // If it's already a constant, then no-op!
            if (expression.NodeType == ExpressionType.Constant)
                return expression;

            // Compile and evaluate!
            LambdaExpression lambda = Expression.Lambda(expression);
            Delegate function = lambda.CompileFast();
            try
            {
                object result = function.DynamicInvoke();
                return Expression.Constant(result, expression.Type);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException == null)
                    throw;

                throw e.InnerException;
            }
        }

        /// <summary>
        /// Utilize the ExpressionVisitor base class to depth-first search our expression tree.
        /// Whenever we encounter a node that we can simplify, we evaluate it locally thereby
        /// pruning the entire subtree.
        /// </summary>
        /// <param name="expression">An expression to simplify</param>
        /// <returns>Possibly a simplified expression, possibly the same expression.</returns>
        public override Expression Visit(Expression expression)
        {
            return _simplifiableExpressions.Contains(expression) ? EvaluateLocally(expression) : base.Visit(expression);
        }

        /// <summary>The default visitor on MemberInitExpression crashes.  So we need to implement our own.</summary>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            // Visit the children
            var visitedBindings = node.Bindings
                                      .Select(c => new {
                                          MemberAssignment = (MemberAssignment) c,
                                          Expression = base.Visit(((MemberAssignment) c).Expression)
                                      })
                                      .ToArray();

            // See if any children were simplified
            if (visitedBindings.Any(c => c.MemberAssignment.Expression != c.Expression))
            {
                var newBindings = visitedBindings.Select(c => Expression.Bind(c.MemberAssignment.Member, c.Expression));

                // Build a new MemberInitExpression
                return Expression.MemberInit(node.NewExpression, newBindings.Cast<MemberBinding>().ToArray());
            }

            // Nothing changed, just return this node as-is
            return node;
        }
    }
}
