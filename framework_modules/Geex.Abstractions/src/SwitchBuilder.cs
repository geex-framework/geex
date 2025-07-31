using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using FastExpressionCompiler;

/// <summary>
/// 通用Switch扩展方法
/// </summary>
public static class SwitchExtensions
{
    public static SwitchBuilder<T> Switch<T>(this T value) => new SwitchBuilder<T>(value);
}

/// <summary>
/// 通用的action-based switch builder
/// </summary>
public struct SwitchBuilder<T>
{
    internal readonly T _value;
    private bool _executed;

    internal SwitchBuilder(T value)
    {
        _value = value;
        _executed = false;
    }

    /// <summary>
    /// 值匹配
    /// </summary>
    public SwitchBuilder<T> Case(T caseValue, Action action)
    {
        if (!_executed && _value?.Equals(caseValue) == true)
        {
            action.Invoke();
            _executed = true;
        }
        return this;
    }

    /// <summary>
    /// 多值匹配
    /// </summary>
    public SwitchBuilder<T> Case(T[] caseValues, Action action)
    {
        if (!_executed && Array.IndexOf(caseValues, _value) >= 0)
        {
            action.Invoke();
            _executed = true;
        }
        return this;
    }

    /// <summary>
    /// 表达式匹配
    /// </summary>
    public SwitchBuilder<T> Case(Func<T, bool> predicate, Action action)
    {
        if (!_executed && predicate(_value))
        {
            action.Invoke();
            _executed = true;
        }
        return this;
    }

    /// <summary>
    /// 表达式匹配 (支持LINQ表达式)
    /// </summary>
    public SwitchBuilder<T> Case(Expression<Func<T, bool>> predicate, Action action)
    {
        if (!_executed && predicate.CompileFast()(_value))
        {
            action.Invoke();
            _executed = true;
        }
        return this;
    }

    /// <summary>
    /// 类型匹配 (when T is object)
    /// </summary>
    public SwitchBuilder<T> Case<TCase>(Action<TCase> action) where TCase : class
    {
        if (!_executed && _value is TCase typedValue)
        {
            action.Invoke(typedValue);
            _executed = true;
        }
        return this;
    }


    /// <summary>
    /// 值匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TResult>(T caseValue, Func<TResult> func)
    {
        if (!_executed && _value?.Equals(caseValue) == true)
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(), true);
        }
        return new SwitchBuilder<T, TResult>(_value);
    }

    /// <summary>
    /// 值匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TResult>(T caseValue, TResult result)
    {
        if (!_executed && _value?.Equals(caseValue) == true)
        {
            return new SwitchBuilder<T, TResult>(_value, result, true);
        }
        return new SwitchBuilder<T, TResult>(_value);
    }

    /// <summary>
    /// 多值匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TResult>(T[] caseValues, Func<TResult> func)
    {
        if (!_executed && Array.IndexOf(caseValues, _value) >= 0)
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(), true);
        }
        return new SwitchBuilder<T, TResult>(_value);
    }

    /// <summary>
    /// 多值匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TResult>(T[] caseValues, TResult result)
    {
        if (!_executed && Array.IndexOf(caseValues, _value) >= 0)
        {
            return new SwitchBuilder<T, TResult>(_value, result, true);
        }
        return new SwitchBuilder<T, TResult>(_value);
    }

    /// <summary>
    /// 表达式匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TResult>(Func<T, bool> predicate, Func<TResult> func)
    {
        if (!_executed && predicate(_value))
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(), true);
        }
        return new SwitchBuilder<T, TResult>(_value);
    }

    /// <summary>
    /// 表达式匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TResult>(Func<T, bool> predicate, TResult result)
    {
        if (!_executed && predicate(_value))
        {
            return new SwitchBuilder<T, TResult>(_value, result, true);
        }
        return new SwitchBuilder<T, TResult>(_value);
    }

    /// <summary>
    /// 表达式匹配 (支持LINQ表达式)
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TResult>(Expression<Func<T, bool>> predicate, Func<TResult> func)
    {
        if (!_executed && predicate.CompileFast()(_value))
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(), true);
        }
        return new SwitchBuilder<T, TResult>(_value);
    }

    /// <summary>
    /// 表达式匹配 (支持LINQ表达式)
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TResult>(Expression<Func<T, bool>> predicate, TResult result)
    {
        if (!_executed && predicate.CompileFast()(_value))
        {
            return new SwitchBuilder<T, TResult>(_value, result, true);
        }
        return new SwitchBuilder<T, TResult>(_value);
    }

    /// <summary>
    /// 类型匹配和转换
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TCase, TResult>(Func<TCase, TResult> func) where TCase : class
    {
        if (!_executed && _value is TCase typedValue)
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(typedValue), true);
        }
        return new SwitchBuilder<T, TResult>(_value);
    }

    /// <summary>
    /// 默认处理，执行后自动终止链式调用
    /// </summary>
    public void Default(Action action)
    {
        if (!_executed)
        {
            action.Invoke();
        }
    }
}

/// <summary>
/// 通用的function-based switch builder
/// </summary>
public struct SwitchBuilder<T, TResult>
{
    private readonly T _value;
    private readonly TResult _result;
    private readonly bool _hasResult;

    internal SwitchBuilder(T value)
    {
        _value = value;
        _result = default(TResult)!;
        _hasResult = false;
    }

    internal SwitchBuilder(T value, TResult result, bool hasResult)
    {
        _value = value;
        _result = result;
        _hasResult = hasResult;
    }

    /// <summary>
    /// 值匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case(T caseValue, Func<TResult> func)
    {
        if (!_hasResult && _value?.Equals(caseValue) == true)
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(), true);
        }
        return this;
    }

    /// <summary>
    /// 值匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case(T caseValue, TResult result)
    {
        if (!_hasResult && _value?.Equals(caseValue) == true)
        {
            return new SwitchBuilder<T, TResult>(_value, result, true);
        }
        return this;
    }

    /// <summary>
    /// 多值匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case(T[] caseValues, Func<TResult> func)
    {
        if (!_hasResult && Array.IndexOf(caseValues, _value) >= 0)
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(), true);
        }
        return this;
    }

    /// <summary>
    /// 多值匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case(T[] caseValues, TResult result)
    {
        if (!_hasResult && Array.IndexOf(caseValues, _value) >= 0)
        {
            return new SwitchBuilder<T, TResult>(_value, result, true);
        }
        return this;
    }

    /// <summary>
    /// 表达式匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case(Func<T, bool> predicate, Func<TResult> func)
    {
        if (!_hasResult && predicate(_value))
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(), true);
        }
        return this;
    }

    /// <summary>
    /// 表达式匹配
    /// </summary>
    public SwitchBuilder<T, TResult> Case(Func<T, bool> predicate, TResult result)
    {
        if (!_hasResult && predicate(_value))
        {
            return new SwitchBuilder<T, TResult>(_value, result, true);
        }
        return this;
    }

    /// <summary>
    /// 表达式匹配 (支持LINQ表达式)
    /// </summary>
    public SwitchBuilder<T, TResult> Case(Expression<Func<T, bool>> predicate, Func<TResult> func)
    {
        if (!_hasResult && predicate.CompileFast()(_value))
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(), true);
        }
        return this;
    }

    /// <summary>
    /// 表达式匹配 (支持LINQ表达式)
    /// </summary>
    public SwitchBuilder<T, TResult> Case(Expression<Func<T, bool>> predicate, TResult result)
    {
        if (!_hasResult && predicate.CompileFast()(_value))
        {
            return new SwitchBuilder<T, TResult>(_value, result, true);
        }
        return this;
    }

    /// <summary>
    /// 类型匹配和转换
    /// </summary>
    public SwitchBuilder<T, TResult> Case<TCase>(Func<TCase, TResult> func) where TCase : class
    {
        if (!_hasResult && _value is TCase typedValue)
        {
            return new SwitchBuilder<T, TResult>(_value, func.Invoke(typedValue), true);
        }
        return this;
    }

    /// <summary>
    /// 默认处理
    /// </summary>
    public TResult Default(Func<TResult> func)
    {
        if (!_hasResult)
        {
            return func.Invoke();
        }
        return _result;
    }

    /// <summary>
    /// 默认处理
    /// </summary>
    public TResult Default(TResult result)
    {
        if (!_hasResult)
        {
            return result;
        }
        return _result;
    }
}
