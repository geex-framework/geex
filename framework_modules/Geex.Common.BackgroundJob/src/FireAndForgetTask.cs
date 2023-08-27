using System;
using System.Threading.Tasks;

namespace Geex.Common.BackgroundJob;

public abstract class FireAndForgetTask<T> : IFireAndForgetTask<T>
{
    protected FireAndForgetTask(T param)
    {
        Param = param;
    }
    /// <inheritdoc />
    public abstract Task Run();

    public IServiceProvider ServiceProvider { get; internal set;}

    IServiceProvider IFireAndForgetTask.ServiceProvider
    {
        get => ServiceProvider;
        set => ServiceProvider = value;
    }

    /// <inheritdoc />
    public T Param { get; }
}

public interface IFireAndForgetTask<out T> : IFireAndForgetTask
{
    public T Param { get; }
}
public interface IFireAndForgetTask
{
    public IServiceProvider ServiceProvider { get; internal set; }
    Task Run();
}