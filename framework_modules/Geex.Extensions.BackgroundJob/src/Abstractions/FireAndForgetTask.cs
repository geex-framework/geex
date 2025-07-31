using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;

using MongoDB.Bson;

namespace Geex.Extensions.BackgroundJob;

public abstract class FireAndForgetTask<TParam> : IFireAndForgetTask<TParam>
{
    protected FireAndForgetTask(TParam param)
    {
        Param = param;
    }

    /// <inheritdoc />
    public abstract Task Run(CancellationToken token);

    public IServiceProvider ServiceProvider { get; set; }

    IServiceProvider IFireAndForgetTask.ServiceProvider
    {
        get => ServiceProvider;
        set => ServiceProvider = value;
    }

    /// <inheritdoc />
    public TParam Param { get; }

    /// <summary>
    /// 任务唯一标识, Id相同的任务不会被重复Schedule, 直至前一个任务完成
    /// </summary>
    public virtual string Id { get; } = ObjectId.GenerateNewId().ToString();
}

public interface IFireAndForgetTask<out TParam> : IFireAndForgetTask
{
    public TParam Param { get; }
}
public interface IFireAndForgetTask : IHasId
{
    public IServiceProvider ServiceProvider { get; internal set; }
    Task Run(CancellationToken token);
}
