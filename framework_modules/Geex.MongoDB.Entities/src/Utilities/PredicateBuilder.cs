using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FastExpressionCompiler;

namespace MongoDB.Entities.Utilities
{

    //public static class PredicateBuilder
    //{
    //    public static Expression<Func<T, bool>> True<T>() { return f => true; }
    //    public static Expression<Func<T, bool>> False<T>() { return f => false; }

    //    public static LambdaExpression True(Type parameterType) { return Expression.Lambda(Expression.Constant(true), Expression.Parameter(parameterType, "x")); }
    //    public static LambdaExpression False(Type parameterType) { return Expression.Lambda(Expression.Constant(true), Expression.Parameter(parameterType, "x")); }

    //    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
    //                                                        Expression<Func<T, bool>> expr2)
    //    {
    //        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
    //        return Expression.Lambda<Func<T, bool>>
    //              (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
    //    }

    //    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
    //                                                         Expression<Func<T, bool>> expr2)
    //    {
    //        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
    //        return Expression.Lambda<Func<T, bool>>
    //              (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    //    }

    //    public static LambdaExpression And(this LambdaExpression expr1,
    //                                                         LambdaExpression expr2)
    //    {
    //        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
    //        return Expression.Lambda
    //              (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    //    }
    //}

    public static class PredicateBuilder
    {
        public static LambdaExpression True(Type parameterType) { return Expression.Lambda(Expression.Constant(true), Expression.Parameter(parameterType, "x")); }
        public static LambdaExpression False(Type parameterType) { return Expression.Lambda(Expression.Constant(true), Expression.Parameter(parameterType, "x")); }
        /// <summary> Start an expression </summary>
        public static ExpressionStarter<T> New<T>(Expression<Func<T, bool>> expr = null) => new ExpressionStarter<T>(expr);

        /// <summary> Create an expression with a stub expression true or false to use when the expression is not yet started. </summary>
        public static ExpressionStarter<T> New<T>(bool defaultExpression) => new ExpressionStarter<T>(defaultExpression);

        /// <summary> OR </summary>
        public static Expression<Func<T, bool>> Or<T>(
          this Expression<Func<T, bool>> expr1,
          Expression<Func<T, bool>> expr2)
        {
            Expression right = new PredicateBuilder.RebindParameterVisitor(expr2.Parameters[0], expr1.Parameters[0]).Visit(expr2.Body);
            return Expression.Lambda<Func<T, bool>>((Expression)Expression.OrElse(expr1.Body, right), (IEnumerable<ParameterExpression>)expr1.Parameters);
        }

        /// <summary> AND </summary>
        public static Expression<Func<T, bool>> And<T>(
          this Expression<Func<T, bool>> expr1,
          Expression<Func<T, bool>> expr2)
        {
            Expression right = new PredicateBuilder.RebindParameterVisitor(expr2.Parameters[0], expr1.Parameters[0]).Visit(expr2.Body);
            return Expression.Lambda<Func<T, bool>>((Expression)Expression.AndAlso(expr1.Body, right), (IEnumerable<ParameterExpression>)expr1.Parameters);
        }

        public static LambdaExpression And(this LambdaExpression expr1,
                                                     LambdaExpression expr2)
        {
            Expression right = new PredicateBuilder.RebindParameterVisitor(expr2.Parameters[0], expr1.Parameters[0]).Visit(expr2.Body);
            return Expression.Lambda((Expression)Expression.AndAlso(expr1.Body, right), (IEnumerable<ParameterExpression>)expr1.Parameters);
        }

        /// <summary>
        /// Extends the specified source Predicate with another Predicate and the specified PredicateOperator.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="first">The source Predicate.</param>
        /// <param name="second">The second Predicate.</param>
        /// <param name="operator">The Operator (can be "And" or "Or").</param>
        /// <returns>Expression{Func{T, bool}}</returns>
        public static Expression<Func<T, bool>> Extend<T>(
          this Expression<Func<T, bool>> first,
          Expression<Func<T, bool>> second,
          PredicateOperator @operator = PredicateOperator.Or)
        {
            return @operator != PredicateOperator.Or ? first.And<T>(second) : first.Or<T>(second);
        }

        /// <summary>
        /// Extends the specified source Predicate with another Predicate and the specified PredicateOperator.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="first">The source Predicate.</param>
        /// <param name="second">The second Predicate.</param>
        /// <param name="operator">The Operator (can be "And" or "Or").</param>
        /// <returns>Expression{Func{T, bool}}</returns>
        public static Expression<Func<T, bool>> Extend<T>(
          this ExpressionStarter<T> first,
          Expression<Func<T, bool>> second,
          PredicateOperator @operator = PredicateOperator.Or)
        {
            return @operator != PredicateOperator.Or ? first.And(second) : first.Or(second);
        }

        private class RebindParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public RebindParameterVisitor(
              ParameterExpression oldParameter,
              ParameterExpression newParameter)
            {
                this._oldParameter = oldParameter;
                this._newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node) => node != this._oldParameter ? base.VisitParameter(node) : (Expression)this._newParameter;
        }
    }

    public class ExpressionStarter<T>
    {
        private Expression<Func<T, bool>> _predicate;

        public ExpressionStarter()
          : this(false)
        {
        }

        public ExpressionStarter(bool defaultExpression)
        {
            if (defaultExpression)
                this.DefaultExpression = (Expression<Func<T, bool>>)(f => true);
            else
                this.DefaultExpression = (Expression<Func<T, bool>>)(f => false);
        }

        public ExpressionStarter(Expression<Func<T, bool>> exp)
          : this(false)
        {
            this._predicate = exp;
        }

        /// <summary>The actual Predicate. It can only be set by calling Start.</summary>
        private Expression<Func<T, bool>> Predicate => !this.IsStarted && this.UseDefaultExpression ? this.DefaultExpression : this._predicate;

        /// <summary>Determines if the predicate is started.</summary>
        public bool IsStarted => this._predicate != null;

        /// <summary> A default expression to use only when the expression is null </summary>
        public bool UseDefaultExpression => this.DefaultExpression != null;

        /// <summary>The default expression</summary>
        public Expression<Func<T, bool>> DefaultExpression { get; set; }

        /// <summary>Set the Expression predicate</summary>
        /// <param name="exp">The first expression</param>
        public Expression<Func<T, bool>> Start(Expression<Func<T, bool>> exp)
        {
            if (this.IsStarted)
                throw new Exception("Predicate cannot be started again.");
            return this._predicate = exp;
        }

        /// <summary>Or</summary>
        public Expression<Func<T, bool>> Or(Expression<Func<T, bool>> expr2) => !this.IsStarted ? this.Start(expr2) : (this._predicate = this.Predicate.Or<T>(expr2));

        /// <summary>And</summary>
        public Expression<Func<T, bool>> And(Expression<Func<T, bool>> expr2) => !this.IsStarted ? this.Start(expr2) : (this._predicate = this.Predicate.And<T>(expr2));

        /// <summary> Show predicate string </summary>
        public override string ToString() => this.Predicate?.ToString();

        /// <summary>
        /// Allows this object to be implicitely converted to an Expression{Func{T, bool}}.
        /// </summary>
        /// <param name="right"></param>
        public static implicit operator Expression<Func<T, bool>>(
          ExpressionStarter<T> right)
        {
            return right?.Predicate;
        }

        /// <summary>
        /// Allows this object to be implicitely converted to an Expression{Func{T, bool}}.
        /// </summary>
        /// <param name="right"></param>
        public static implicit operator Func<T, bool>(ExpressionStarter<T> right)
        {
            if (right == null)
                return (Func<T, bool>)null;
            return !right.IsStarted && !right.UseDefaultExpression ? (Func<T, bool>)null : right.Predicate.CompileFast();
        }

        /// <summary>
        /// Allows this object to be implicitely converted to an Expression{Func{T, bool}}.
        /// </summary>
        /// <param name="right"></param>
        public static implicit operator ExpressionStarter<T>(
          Expression<Func<T, bool>> right)
        {
            return right != null ? new ExpressionStarter<T>(right) : (ExpressionStarter<T>)null;
        }

        /// <summary></summary>
        public Func<T, bool> Compile() => this.Predicate.CompileFast();

        /// <summary></summary>
        public Expression Body => this.Predicate.Body;

        /// <summary></summary>
        public ExpressionType NodeType => this.Predicate.NodeType;

        /// <summary></summary>
        public ReadOnlyCollection<ParameterExpression> Parameters => this.Predicate.Parameters;

        /// <summary></summary>
        public Type Type => this.Predicate.Type;

        /// <summary></summary>
        public string Name => this.Predicate.Name;

        /// <summary></summary>
        public Type ReturnType => this.Predicate.ReturnType;

        /// <summary></summary>
        public bool TailCall => this.Predicate.TailCall;

        /// <summary></summary>
        public virtual bool CanReduce => this.Predicate.CanReduce;
    }
    /// <summary> The Predicate Operator </summary>
    public enum PredicateOperator
    {
        /// <summary> The "Or" </summary>
        Or,
        /// <summary> The "And" </summary>
        And,
    }
}
