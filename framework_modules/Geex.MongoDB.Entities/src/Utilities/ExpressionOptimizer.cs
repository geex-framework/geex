#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// This is just a light-weight expression optimizer.
// It won't do any heavy stuff...

namespace Geex.MongoDB.Entities.Utilities;

using Expression = System.Linq.Expressions.Expression;

public static class ExpressionOptimizer {
    public static class Methods {
        /// <summary>
        ///  We want to eliminate enum-types and constants like 1 or "a".
        ///  But Constant value can be also another complex object like IQueryable.
        ///  We don't want to evaluate those!
        /// </summary>
        public static IComparable? ConstantBasicType(Expression parentExpr, Expression e) {
            IComparable? GetCorrectType(object? x) =>
                x is IComparable comparable && comparable.GetType() == parentExpr.Type
                    ? comparable
                    : null;

            IComparable? x;

            return (e.NodeType, e) switch {
                (ExpressionType.Constant, ConstantExpression ce) when
                    parentExpr.Type.GetTypeInfo().IsPrimitive =>
                    (ce.Value) is null
                        ? null
                        : (x = GetCorrectType(ce.Value)) switch {
                            null => GetCorrectType(Expression.Lambda(parentExpr).Compile().DynamicInvoke()),
                            _ => x
                        },
                _ => null
            };
        }

        /// <summary>
        /// Purpose of this is optimize away already known constant=constant style expressions.
        ///   7 > 8      -->   False
        /// "G" = "G"    -->   True
        /// </summary>
        public static Expression ReplaceConstantComparison(Expression e) {
            IComparable? Constant(Expression exp) =>
                (exp.NodeType, exp) switch {
                    (ExpressionType.Constant, ConstantExpression { Value: IComparable } constantExpression) => (IComparable)constantExpression.Value,
                    (ExpressionType.Convert, UnaryExpression parentExpr) => ConstantBasicType(parentExpr,
                        parentExpr.Operand),
                    _ => null
                };

            Expression CreateBool(object b) => Expression.Constant(b, typeof(bool));
            IComparable? l, r;

            return e switch {
                BinaryExpression ce =>
                    (l = Constant(ce.Left)) is { } && (r = Constant(ce.Right)) is { }
                        ? e.NodeType switch {
                            ExpressionType.Equal => CreateBool(l.CompareTo(r) == 0),
                            ExpressionType.LessThan => CreateBool(l.CompareTo(r) < 0),
                            ExpressionType.LessThanOrEqual => CreateBool(l.CompareTo(r) <= 0),
                            ExpressionType.GreaterThan => CreateBool(l.CompareTo(r) > 0),
                            ExpressionType.GreaterThanOrEqual => CreateBool(l.CompareTo(r) >= 0),
                            ExpressionType.NotEqual => CreateBool(l.CompareTo(r) != 0),
                            _ => e
                        }
                        : e,
                _ => e
            };
        }

        /// <summary>
        /// Purpose of this is to replace non-used anonymous types:
        /// new AnonymousObject(Item1 = x, Item2 = "").Item1    -->   x
        /// </summary>
        public static Expression RemoveAnonymousType(Expression e) {
            int? idxMember;
            return (e.NodeType, e) switch {
                // FSharp anonymous type:
                (ExpressionType.MemberAccess, MemberExpression me) when
                    me.Member.DeclaringType is { } dt &&
                    (dt.Name.ToUpper().StartsWith("ANONYMOUSOBJECT") || dt.Name.ToUpper().StartsWith("TUPLE")) =>
                    (me.Member.Name.StartsWith("Item") && me.Member.Name.Length > 4
                        ? int.TryParse(me.Member.Name[4..], out var i)
                            ? i as int?
                            : default
                        : default, me.Expression?.NodeType, me.Expression, me.Member) switch {
                        ({ }idx, ExpressionType.New, NewExpression ne, PropertyInfo p) =>
                            ne.Arguments.Count > idx - 1 && ne.Arguments[idx - 1].Type.Equals(p.PropertyType)
                                ? ne.Arguments[idx - 1] // We found it!
                                : e,
                        _ => e
                    },
                //CSharp anonymous type:
                (ExpressionType.MemberAccess, MemberExpression me) when
                    me.Member.DeclaringType is { } dt &&
                    (dt.Name.ToUpper().StartsWith("<>F__ANONYMOUSTYPE") || dt.Name.ToUpper().StartsWith("TUPLE")) =>
                    (me.Expression?.NodeType, me.Expression, me.Member) switch {
                        (ExpressionType.New, NewExpression { Arguments: { } } ne, PropertyInfo p) =>
                            ne.Arguments.Select(arg => arg switch {
                                MemberExpression { Member: { } } ame when ame.Member.Name == me.Member.Name && ame.Type == p.PropertyType =>
                                    (Expression)ame,
                                _ => default
                            }).FirstOrDefault() switch {
                                { } x => x,
                                _ when ne.Members is not null =>
                                    ne.Members.Select(m => m switch {
                                        _ when m.Name == me.Member.Name =>
                                            ne.Arguments.Count > (idxMember = ne.Members.IndexOf(m))
                                                ? ne.Arguments[idxMember.Value] switch {
                                                    ParameterExpression ape when ape.Type == p.PropertyType =>
                                                        (Expression)ape,
                                                    _ => default
                                                }
                                                : null,
                                        _ => null
                                    }).FirstOrDefault() switch { { } x => x, _ => e },
                                _ => e
                            },
                        _ => e
                    },
                _ => e
            };
        }

        /// <summary>
        /// if false then x else y -> y
        /// </summary>
        public static Expression CutNotUsedCondition(Expression e) =>
            (e.NodeType, e) switch {
                (ExpressionType.Conditional, ConditionalExpression ce) =>
                    ce.Test switch { // For now, only direct booleans conditions are optimized to select query:
                        ConstantExpression c when c.Value?.Equals(true) ?? false => ce.IfTrue,
                        ConstantExpression c when c.Value?.Equals(false) ?? false => ce.IfFalse,
                        _ => e
                    },
                _ => e
            };

        /// <summary>
        /// not(false) -> true
        /// </summary>
        public static Expression NotFalseIsTrue(Expression e) =>
            (e.NodeType, e) switch {
                (ExpressionType.Not, UnaryExpression ue) =>
                    ue.Operand switch {
                        ConstantExpression c when c.Value?.Equals(false) ?? false => Expression.Constant(true, typeof(bool)),
                        ConstantExpression c when c.Value?.Equals(true) ?? false => Expression.Constant(false, typeof(bool)),
                        _ => e
                    },
                _ => e
            };

        // --------------- SOME BOOLEAN ALGEBRA ----------------------/
        // Idea from https://github.com/mavnn/Algebra.Boolean/blob/6b2099420ef605e3b3f818883db957154afa836a/Algebra.Boolean/Transforms.fs
        // But System.Linq.Expressions, not Microsoft.FSharp.Quotations

        // Reductions:
        // [associate; commute; distribute; gather; identity; annihilate; absorb; idempotence; complement; doubleNegation; deMorgan]

        internal static (object?, Type)? Value(Expression e) =>
            (e.NodeType, e) switch {
                (ExpressionType.Constant, ConstantExpression ce) => (ce.Value, ce.Type),
                _ => null
            };

        internal static (Expression, Expression, Expression)? IfThenElse(Expression e) =>
            (e.NodeType, e) switch {
                (ExpressionType.Conditional, ConditionalExpression ce) => (ce.Test, ce.IfTrue, ce.IfFalse),
                _ => null
            };

        internal static Expression? Not(Expression? e) =>
            (e?.NodeType, e) switch {
                (ExpressionType.Not, UnaryExpression ue) => (ue.Operand),
                _ => null
            };

        internal static Expression? True(Expression expr) =>
            expr switch {
                _ when Value(expr) is var (o, t) && t == typeof(bool) && o is true => expr,
                _ => null
            };

        internal static Expression? False(Expression expr) =>
            expr switch {
                _ when Value(expr) is var (o, t) && t == typeof(bool) && o is false => expr,
                _ => null
            };

        internal static (Expression left, Expression right)? Or(Expression e) =>
            (e.NodeType, e) switch {
                (ExpressionType.OrElse, BinaryExpression be) => (be.Left, be.Right),
                _ => IfThenElse(e) switch {
                    var (left, _, right) => (left, right),
                    _ => null
                }
            };

        internal static (Expression left, Expression right)? And(Expression e) =>
            (e.NodeType, e) switch {
                (ExpressionType.AndAlso, BinaryExpression be) => (be.Left, be.Right),
                _ => IfThenElse(e) switch {
                    var(left, right, _) => (left, right),
                    _ => null
                }
            };

        /// <summary>
        /// Not in use, would cause looping...
        /// </summary>
        public static Expression Associate(Expression e) =>
            e switch {
                _ when Or(e) is var (l1, r1) && Or(l1) is var (l, r) => Expression.OrElse(Expression.OrElse(l, r), r1),
                _ when Or(e) is var (l, r1) && Or(r1) is var (l1, r) => Expression.OrElse(l, Expression.OrElse(l1, r)),
                _ when And(e) is var (l1, r1) && And(l1) is var (l, r) => Expression.AndAlso(Expression.AndAlso(l, r), r1),
                _ when And(e) is var (l, r1) && And(r1) is var (l1, r) => Expression.AndAlso(l, Expression.AndAlso(l1, r)),
                var noHit => noHit
            };

        /// <summary>
        /// We commute to AndAlso and OrElse, if not already in that format
        /// </summary>
        public static Expression Commute(Expression e) =>
            e switch {
                var comex when Or(e) is var (left, right) && comex.NodeType != ExpressionType.OrElse => Expression.OrElse(right, left),
                var comex when And(e) is var (left, right) && comex.NodeType != ExpressionType.AndAlso => Expression.AndAlso(right, left),
                var noHit => noHit
            };

        /// <summary>
        /// Not in use, would cause looping...
        /// </summary>
        public static Expression Distribute(Expression e) =>
            e switch {
                _ when And(e) is var (p, r) && Or(r) is var (p1, p2) => Expression.OrElse(Expression.AndAlso(p, p1), Expression.AndAlso(p, p2)),
                _ when Or(e) is var (p, r) && And(r) is var (p1, p2) => Expression.AndAlso(Expression.OrElse(p, p1), Expression.OrElse(p, p2)),
                var noHit => noHit
            };

        public static Expression Gather(Expression e) =>
            e switch {
                _ when And(e) is var (l, r) && Or(l) is var (p, p1) && Or(r) is var (p2, p3) && p.Equals(p2) => Expression.OrElse(p, Expression.AndAlso(p1, p3)),
                _ when Or(e) is var ( l, r) && And(l) is var (p, p1) && And(r) is var (p2,p3) && p.Equals(p2) => Expression.AndAlso(p, Expression.OrElse(p1, p3)),
                var noHit => noHit
            };

        public static Expression Identity(Expression e) =>
            e switch {
                _ when And(e) is var (l, r) => (l, r: r) switch {
                    _ when True(l) is { } => r,
                    _ when True(r) is { } => l,
                    _ => e
                },
                _ when Or(e) is var (l, r) => (l, r) switch {
                    _ when False(l) is { } => r,
                    _ when False(r) is { } => l,
                    _ => e
                },
                var noHit => noHit
            };

        public static Expression Annihilate(Expression e) =>
            e switch {
                _ when And(e) is var (f, _) && False(f) is { } => f,
                _ when And(e) is var (_, f) && False(f) is { } => f,
                _ when Or(e) is var (t, _) && True(t) is { } => t,
                _ when Or(e) is var (_, t) && True(t) is { } => t,
                var noHit => noHit
            };

        public static Expression Absorb(Expression e) =>
            e switch {
                _ when And(e) is var (l, r) => (l, r) switch {
                    _ when Or(r) is var (p1L, _) && l.Equals(p1L) => l,
                    _ when Or(r) is var (_, p1R) && l.Equals(p1R) => l,
                    _ when Or(l) is var (p1L, _) && r.Equals(p1L) => r,
                    _ when Or(l) is var (_, p1R) && r.Equals(p1R) => r,
                    _ => e
                },
                _ when Or(e) is var (l, r) => (l, r) switch {
                    _ when And(r) is var (p1L, _) && l.Equals(p1L) => l,
                    _ when And(r) is var (_, p1R) && l.Equals(p1R) => l,
                    _ when And(l) is var (p1L, _) && r.Equals(p1L) => r,
                    _ when And(l) is var (_, p1R) && r.Equals(p1R) => r,
                    _ => e
                },
                var noHit => noHit
            };

        public static Expression Idempotence(Expression e) =>
            e switch {
                _ when And(e) is var (p, p1) && p.Equals(p1) => p,
                _ when Or(e) is var ( p, p1) && p.Equals(p1) => p,
                var noHit => noHit
            };

        public static Expression Complement(Expression e) =>
            e switch {
                _ when And(e) is var (p, p1) => (p, p1) switch {
                    _ when Not(p1)?.Equals(p) is { } => Expression.Constant(false, typeof(bool)),
                    _ when Not(p)?.Equals(p1) is { } => Expression.Constant(false, typeof(bool)),
                    _ => e
                },
                _ when Or(e) is var (p, p1) => (p, p1) switch {
                    _ when Not(p1)?.Equals(p) is { } => Expression.Constant(true, typeof(bool)),
                    _ when Not(p)?.Equals(p1) is { } => Expression.Constant(true, typeof(bool)),
                    _ => e
                },
                var noHit => noHit
            };

        public static Expression DoubleNegation(Expression e) =>
            e switch {
                _ when Not(Not(e)) is { } p => p,
                var noHit => noHit
            };

        public static Expression DeMorgan(Expression e) =>
            e switch {
                _ when Or(e) is var (l, r) && Not(l) is { } p && Not(r) is { } p1 => Expression.Not(Expression.AndAlso(p, p1)),
                _ when And(e) is var (l, r) && Not(l) is { } p && Not(r) is { } p1 => Expression.Not(Expression.OrElse(p, p1)),
                var noHit => noHit
            };

        // ------------------------------------- /

        /// <summary>
        /// Balance tree that is too much weighted to other side.
        /// The real advantage is not-so-nested-stack
        /// </summary>
        public static Expression Balancetree(Expression e) =>
            e switch {
                _ when Or(e) is var (p1, r1) && Or(r1) is var (p2, r2) && Or(r2) is var (p3, r3) &&
                       Or(r3) is var (p4, r4) && Or(r4) is var (p5, r5) && Or(r5) is var (p6, r6) &&
                       Or(r6) is var (p7, p8) =>
                    Expression.OrElse(Expression.OrElse(Expression.OrElse(p1, p2), Expression.OrElse(p3, p4)),
                        Expression.OrElse(Expression.OrElse(p5, p6), Expression.OrElse(p7, p8))),
                _ when Or(e) is var (l1, p1) && Or(l1) is var (l2, p2) && Or(l2) is var (l3, p3) &&
                       Or(l3) is var (l4, p4) && Or(l4) is var (l5, p5) && Or(l5) is var (l6, p6) &&
                       Or(l6) is var (p7, p8) =>
                    Expression.OrElse(Expression.OrElse(Expression.OrElse(p1, p2), Expression.OrElse(p3, p4)),
                        Expression.OrElse(Expression.OrElse(p5, p6), Expression.OrElse(p7, p8))),
                _ when And(e) is var (p1, r1) && And(r1) is var (p2, r2) && And(r2) is var (p3, r3) &&
                       And(r3) is var (p4, r4) && And(r4) is var (p5, r5) && And(r5) is var (p6, r6) &&
                       And(r6) is var (p7, p8) =>
                    Expression.AndAlso(Expression.AndAlso(Expression.AndAlso(p1, p2), Expression.AndAlso(p3, p4)),
                        Expression.AndAlso(Expression.AndAlso(p5, p6), Expression.AndAlso(p7, p8))),
                _ when And(e) is var (l1, p1) && And(l1) is var (l2, p2) && And(l2) is var (l3, p3) &&
                       And(l3) is var (l4, p4) && And(l4) is var (l5, p5) && And(l5) is var (l6, p6) &&
                       And(l6) is var (p7, p8) =>
                    Expression.AndAlso(Expression.AndAlso(Expression.AndAlso(p1, p2), Expression.AndAlso(p3, p4)),
                        Expression.AndAlso(Expression.AndAlso(p5, p6), Expression.AndAlso(p7, p8))),
                var noHit => noHit
            };

        // ------------------------------------- //

        /// <summary>
        /// Evaluating constants to not mess with our expressions:
        /// </summary>
        public static Expression EvaluateConstants(Expression e) =>
            (e.NodeType, e) switch {
                (ExpressionType.MemberAccess, MemberExpression { Expression: { } } me) =>
                    ConstantBasicType(me, me.Expression) switch {
                        { } x => Expression.Constant(x, me.Type),
                        _ => e
                    },
                (ExpressionType.MemberAccess, MemberExpression { Expression: null } me) when
                    me.Member.DeclaringType is { } dt &&
                    (dt.Name.ToUpper().StartsWith("FSI_") || dt.Name.ToUpper().StartsWith("<>C__DISPLAYCLASS")) =>
                    me.Member switch {
                        PropertyInfo p when p.GetType().FullName is { } fn && fn.StartsWith("System") =>
                            Expression.Constant(Expression.Lambda(me).Compile().DynamicInvoke(null), me.Type),
                        _ => e
                    },
                _ => e
            };

        // ------------------------------------- //

        /// <summary>
        /// Evaluate simple math between two constants.
        ///  9  *  3     -->    27
        /// "G" + "G"    -->   "GG"
        /// </summary>
        public static Expression EvaluateBasicConstantMath(Expression e) =>
            e switch {
                BinaryExpression ce => !ce.Left.Type.Equals(ce.Right.Type)
                    ? e
                    : (ce.Left.NodeType, ce.Right.NodeType, ce.Left, ce.Right) switch {
                        (ExpressionType.Constant, ExpressionType.Constant, ConstantExpression le, ConstantExpression ri)
                            =>
                            // C# doesn't support macros so this code is a bit copy-and-paste, but it should be trivial.
                            e.NodeType switch {
                                ExpressionType.Add => (le.Value, ri.Value) switch {
                                    (string lstr, string rstr) when le.Type == typeof(string) => Expression.Constant(lstr + rstr, le.Type),
                                    (decimal lstr, decimal rstr) when le.Type == typeof(decimal) => Expression.Constant(lstr + rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr + rstr, le.Type),
                                    (double lstr, double rstr) when le.Type == typeof(double) => Expression.Constant(lstr + rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr + rstr, le.Type),
                                    (int lstr, int rstr) when le.Type == typeof(int) => Expression.Constant(lstr + rstr, le.Type),
                                    (long lstr, long rstr) when le.Type == typeof(long) => Expression.Constant(lstr + rstr, le.Type),
                                    (uint lstr, uint rstr) when le.Type == typeof(uint) => Expression.Constant(lstr + rstr, le.Type),
                                    (ulong lstr, ulong rstr) when le.Type == typeof(ulong) => Expression.Constant(lstr + rstr, le.Type),
                                    (short lstr, short rstr) when le.Type == typeof(short) => Expression.Constant(lstr + rstr, le.Type),
                                    (ushort lstr, ushort rstr) when le.Type == typeof(ushort) => Expression.Constant(lstr + rstr, le.Type),
                                    (sbyte lstr, sbyte rstr) when le.Type == typeof(sbyte) => Expression.Constant(lstr + rstr, le.Type),
                                    (byte lstr, byte rstr) when le.Type == typeof(byte) => Expression.Constant(lstr + rstr, le.Type),
                                    _ => e
                                },
                                ExpressionType.Subtract => (le.Value, ri.Value) switch {
                                    (decimal lstr, decimal rstr) when le.Type == typeof(decimal) => Expression.Constant(lstr - rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr - rstr, le.Type),
                                    (double lstr, double rstr) when le.Type == typeof(double) => Expression.Constant(lstr - rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr - rstr, le.Type),
                                    (int lstr, int rstr) when le.Type == typeof(int) => Expression.Constant(lstr - rstr, le.Type),
                                    (long lstr, long rstr) when le.Type == typeof(long) => Expression.Constant(lstr - rstr, le.Type),
                                    (uint lstr, uint rstr) when le.Type == typeof(uint) => Expression.Constant(lstr - rstr, le.Type),
                                    (ulong lstr, ulong rstr) when le.Type == typeof(ulong) => Expression.Constant(lstr - rstr, le.Type),
                                    (short lstr, short rstr) when le.Type == typeof(short) => Expression.Constant(lstr - rstr, le.Type),
                                    (ushort lstr, ushort rstr) when le.Type == typeof(ushort) => Expression.Constant(lstr - rstr, le.Type),
                                    (sbyte lstr, sbyte rstr) when le.Type == typeof(sbyte) => Expression.Constant(lstr - rstr, le.Type),
                                    (byte lstr, byte rstr) when le.Type == typeof(byte) => Expression.Constant(lstr - rstr, le.Type),
                                    _ => e
                                },
                                ExpressionType.Multiply => (le.Value, ri.Value) switch {
                                    (decimal lstr, decimal rstr) when le.Type == typeof(decimal) => Expression.Constant(lstr * rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr * rstr, le.Type),
                                    (double lstr, double rstr) when le.Type == typeof(double) => Expression.Constant(lstr * rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr * rstr, le.Type),
                                    (int lstr, int rstr) when le.Type == typeof(int) => Expression.Constant(lstr * rstr, le.Type),
                                    (long lstr, long rstr) when le.Type == typeof(long) => Expression.Constant(lstr * rstr, le.Type),
                                    (uint lstr, uint rstr) when le.Type == typeof(uint) => Expression.Constant(lstr * rstr, le.Type),
                                    (ulong lstr, ulong rstr) when le.Type == typeof(ulong) => Expression.Constant(lstr * rstr, le.Type),
                                    (short lstr, short rstr) when le.Type == typeof(short) => Expression.Constant(lstr * rstr, le.Type),
                                    (ushort lstr, ushort rstr) when le.Type == typeof(ushort) => Expression.Constant(lstr * rstr, le.Type),
                                    (sbyte lstr, sbyte rstr) when le.Type == typeof(sbyte) => Expression.Constant(lstr * rstr, le.Type),
                                    (byte lstr, byte rstr) when le.Type == typeof(byte) => Expression.Constant(lstr * rstr, le.Type),
                                    _ => e
                                },
                                ExpressionType.Divide => (le.Value, ri.Value) switch {
                                    (decimal lstr, decimal rstr) when le.Type == typeof(decimal) => Expression.Constant(lstr / rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr / rstr, le.Type),
                                    (double lstr, double rstr) when le.Type == typeof(double) => Expression.Constant(lstr / rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr / rstr, le.Type),
                                    (int lstr, int rstr) when le.Type == typeof(int) => Expression.Constant(lstr / rstr, le.Type),
                                    (long lstr, long rstr) when le.Type == typeof(long) => Expression.Constant(lstr / rstr, le.Type),
                                    (uint lstr, uint rstr) when le.Type == typeof(uint) => Expression.Constant(lstr / rstr, le.Type),
                                    (ulong lstr, ulong rstr) when le.Type == typeof(ulong) => Expression.Constant(lstr / rstr, le.Type),
                                    (short lstr, short rstr) when le.Type == typeof(short) => Expression.Constant(lstr / rstr, le.Type),
                                    (ushort lstr, ushort rstr) when le.Type == typeof(ushort) => Expression.Constant(lstr / rstr, le.Type),
                                    (sbyte lstr, sbyte rstr) when le.Type == typeof(sbyte) => Expression.Constant(lstr / rstr, le.Type),
                                    (byte lstr, byte rstr) when le.Type == typeof(byte) => Expression.Constant(lstr / rstr, le.Type),
                                    _ => e
                                },
                                ExpressionType.Modulo => (le.Value, ri.Value) switch {
                                    (decimal lstr, decimal rstr) when le.Type == typeof(decimal) => Expression.Constant(lstr % rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr % rstr, le.Type),
                                    (double lstr, double rstr) when le.Type == typeof(double) => Expression.Constant(lstr % rstr, le.Type),
                                    (float lstr, float rstr) when le.Type == typeof(float) => Expression.Constant(lstr % rstr, le.Type),
                                    (int lstr, int rstr) when le.Type == typeof(int) => Expression.Constant(lstr % rstr, le.Type),
                                    (long lstr, long rstr) when le.Type == typeof(long) => Expression.Constant(lstr % rstr, le.Type),
                                    (uint lstr, uint rstr) when le.Type == typeof(uint) => Expression.Constant(lstr % rstr, le.Type),
                                    (ulong lstr, ulong rstr) when le.Type == typeof(ulong) => Expression.Constant(lstr % rstr, le.Type),
                                    (short lstr, short rstr) when le.Type == typeof(short) => Expression.Constant(lstr % rstr, le.Type),
                                    (ushort lstr, ushort rstr) when le.Type == typeof(ushort) => Expression.Constant(lstr % rstr, le.Type),
                                    (sbyte lstr, sbyte rstr) when le.Type == typeof(sbyte) => Expression.Constant(lstr % rstr, le.Type),
                                    (byte lstr, byte rstr) when le.Type == typeof(byte) => Expression.Constant(lstr % rstr, le.Type),
                                    _ => e
                                },
                                _ => e
                            },
                        _ => e
                    },
                _ => e
            };
    }

    // ------------------------------------- //

    /// <summary>
    /// Used optimization methods
    /// </summary>
    public static IList<Func<Expression, Expression>> ReductionMethods = new List<Func<Expression, Expression>>
    {
        Methods.EvaluateConstants,
        Methods.EvaluateBasicConstantMath,

        Methods.ReplaceConstantComparison,
        Methods.RemoveAnonymousType,
        Methods.CutNotUsedCondition,
        Methods.NotFalseIsTrue,
        // Methods.Associate,
        Methods.Commute,
        // Methods.Distribute,
        Methods.Gather,
        Methods.Identity,
        Methods.Annihilate,
        Methods.Absorb,
        Methods.Idempotence,
        Methods.Complement,
        Methods.DoubleNegation,
        Methods.DeMorgan,
        //Methods.Balancetree
    };

    /// <summary>
    /// Does reductions just for a current node.
    /// </summary>
    public static Expression? DoReduction(Expression? exp) {
        Expression opt;
        return exp switch { { } => ((opt = ReductionMethods.Aggregate(exp, (acc, f) => f(acc))).Equals(exp)) switch {
            true => exp,
            false => DoReduction(opt)
        },
            _ => exp
        };
    }

    // ------------------------------------- //

    // Expression tree visitor: go through the whole expression tree.

    // .NET has already this System.Linq.Expressions.Expressionvisitor
    // Too bad this was so simple and faster than what it would have taken to get to know that 700 rows of source code!


    /// <remarks>
    /// This method conflicts with the nullable semantics of C#, but is retained for fidelity with the original F#.
    /// </remarks>
    internal static Expression DoVisit(Expression exp) =>
        //bottom up:
        exp is null
            ? null
            : DoReduction(VisitChilds(exp)) ?? null;

    internal static Expression VisitChilds(Expression exp) {
        Expression? visited;
        Expression? v, v1, v2, v3, obje, b;
        Expression[] args, visitedMethodCalls;

        return exp switch {
            ConstantExpression e => WhereSelectEnumerableIteratorVisitor(e),
            ParameterExpression e => e,
            UnaryExpression e => (visited = DoVisit(e.Operand)) == e.Operand
                ? e
                : Expression.MakeUnary(e.NodeType, visited, e.Type, e.Method),
            BinaryExpression e => e.NodeType == ExpressionType.Coalesce && e.Conversion is not null
                ? ((v1 = DoVisit(e.Left)) == e.Left) & ((v2 = DoVisit(e.Right)) == e.Right) & ((v3 = DoVisit(e.Conversion)) == e.Conversion)
                    ? e
                    : Expression.Coalesce(v1, v2, (LambdaExpression)v3)
                : ((v1 = DoVisit(e.Left)) == e.Left) & ((v2 = DoVisit(e.Right)) == e.Right)
                    ? e
                    : Expression.MakeBinary(e.NodeType, v1, v2, e.IsLiftedToNull, e.Method),
            MemberExpression e => (v = DoVisit(e.Expression)) == e.Expression
                ? e
                : Expression.MakeMemberAccess(v, e.Member),
            // The following condition is buggy, but retained for fidelity with the original F#. If the Select returns a null, ToArray will throw
            // an ArgumentNullException. This is basically the same behaviour as the F# code.
            MethodCallExpression e => (obje = DoVisit(e.Object)) == e.Object & (visitedMethodCalls = (args = e.Arguments.ToArray()).Select(a => (v = DoVisit(a)) is not null ? v : null).ToArray()).Equals(args)
                ? e
                : Expression.Call(obje, e.Method, visitedMethodCalls),
            LambdaExpression e => (b = DoVisit(e.Body)) == e.Body
                ? e
                : Expression.Lambda(e.Type, b, e.Parameters),
            TypeBinaryExpression e => (v = DoVisit(e.Expression)) == e.Expression
                ? e
                : Expression.TypeIs(v, e.TypeOperand),
            ConditionalExpression e => (v1 = DoVisit(e.Test)) == e.Test & (v2 = DoVisit(e.IfTrue)) == e.IfTrue & (v3 = DoVisit(e.IfFalse)) == e.IfFalse
                ? e
                : Expression.Condition(v1, v2, v3),
            NewExpression { Members: null, Constructor: not null } e => Expression.New(e.Constructor, e.Arguments.Select(DoVisit)),
            NewExpression { Members: not null, Constructor: not null } e => Expression.New(e.Constructor, e.Arguments.Select(DoVisit), e.Members),
            // The following two lines are buggy, but are retained for fidelity with the original F#. In both cases, GetElementType() can return a null
            // which, in turn, will cause an ArgumentNullException to be thrown when NewArrayBounds or NewArrayInit is invoked.
            NewArrayExpression { NodeType: ExpressionType.NewArrayBounds } e => Expression.NewArrayBounds(e.Type.GetElementType(), e.Expressions.Select(DoVisit)),
            NewArrayExpression e => Expression.NewArrayInit(e.Type.GetElementType(), e.Expressions.Select(DoVisit)),
            InvocationExpression e => Expression.Invoke(DoVisit(e.Expression), e.Arguments.Select(DoVisit)),
            MemberInitExpression e => Expression.MemberInit((NewExpression)DoVisit(e.NewExpression), e.Bindings), //probably should visit also bindings
            ListInitExpression e => Expression.ListInit((NewExpression)DoVisit(e.NewExpression), e.Initializers), //probably should visit' also initializers
            _ => throw new SystemException($"encountered unknown LINQ expression: {exp.NodeType} {exp}")
        };
    }

    /// <summary>
    /// Look also inside a LINQ-wrapper
    /// https://referencesource.microsoft.com/#System.Core/System/Linq/Enumerable.cs,8bf16962931637d3,references
    /// </summary>
    internal static ConstantExpression WhereSelectEnumerableIteratorVisitor(ConstantExpression ce) {
        PropertyInfo? enuProp;
        object? enu, src, expr;
        FieldInfo? srcProp, exprItm;
        Expression opt;

        ConstantExpression DoSetValue() {
            exprItm?.SetValue(src, opt);
            return ce;
        }

        // ce.Type is never null here, but we will test for nullability for the sake of fidelity to the F# original
        // which performs this redundant test.
        return ce.Value is null || ce.Type is null || !(ce.Type.FullName ?? string.Empty).StartsWith("System.Linq")
            ? ce
            : (enuProp = ce.Type.GetProperty("Enumerable", BindingFlags.NonPublic | BindingFlags.Instance)) is null
                ? ce
                : (enu = enuProp.GetValue(ce.Value, null)) is null
                    ? ce
                    : (srcProp = enu.GetType().GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance)) is null
                        ? ce
                        : (src = srcProp.GetValue(enu)) is null
                            ? ce
                            : (exprItm = src.GetType().GetField("_expression",
                                  BindingFlags.NonPublic | BindingFlags.Instance)) is not null &
                              (expr = exprItm?.GetValue(src)) is not null
                                // The compiler can't see that expr can never be null here.
                                ? !(opt = DoVisit((Expression)expr)).Equals((Expression)expr)
                                    ? DoSetValue()
                                    : ce
                                : ce;
    }

    /// <summary>
    /// Expression tree visitor: go through the whole expression tree.
    /// </summary>
    public static Expression Visit(Expression exp) => DoVisit(exp);

    /// <summary>
    /// Expression tree visitor: go through the whole expression tree.
    /// Catches the exceptions.
    /// </summary>
    public static Expression TryVisit(Expression exp) {
        try {
            return DoVisit(exp);
        }
        catch {
            return exp;
        }
    }
}