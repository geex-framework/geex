using System;
using System.Threading.Tasks;

using Geex.Abstractions;

namespace Geex.Common.BackgroundJob;

public abstract class FireAndForgetTask<TImplementation, TParam> : Enumeration<FireAndForgetTask<TImplementation, TParam>>, IFireAndForgetTask<TParam>
{
    protected FireAndForgetTask(TParam param) : base(nameof(TImplementation))
    {
        Param = param;
    }
    /// <inheritdoc />
    public abstract Task Run();

    public IServiceProvider ServiceProvider { get; internal set; }

    IServiceProvider IFireAndForgetTask.ServiceProvider
    {
        get => ServiceProvider;
        set => ServiceProvider = value;
    }

    /// <inheritdoc />
    public TParam Param { get; }
}

public interface IFireAndForgetTask<out TParam> : IFireAndForgetTask
{
    public TParam Param { get; }
}
public interface IFireAndForgetTask
{
    public IServiceProvider ServiceProvider { get; internal set; }
    Task Run();
}
