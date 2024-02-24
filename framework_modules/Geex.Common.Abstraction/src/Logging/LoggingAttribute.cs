using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using Geex.Common.Abstraction;

using MethodBoundaryAspect.Fody.Attributes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Common.Logging
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class LoggingAttribute : OnMethodBoundaryAspect
    {
        public bool LogInput { get; }
        public bool LogOutput { get; }

        public LoggingAttribute(bool logInput = true, bool logOutput = true)
        {
            LogInput = logInput;
            LogOutput = logOutput;
        }
        private static ILoggerFactory? _loggerFactory;

        static ILoggerFactory loggerFactory => _loggerFactory ??= ServiceLocator.Global.GetService<ILoggerFactory>();

        public override void OnEntry(MethodExecutionArgs args)
        {
            var categoryName = args.Method.DeclaringType?.Name ?? args.Method.ReflectedType?.Name ?? "UnknownType";
            var logger = loggerFactory.CreateLogger(categoryName);
            args.MethodExecutionTag = logger;
            logger.LogInformation("{methodName} entry." + (this.LogInput ? " Params: {params}." : ""), args.Method.Name, args.Arguments.ToJsonSafe());
        }


        public override void OnExit(MethodExecutionArgs args)
        {
            var logger = (args.MethodExecutionTag as ILogger)!;
            if (args.ReturnValue is Task t)
            {
                t.ContinueWith(task =>
                {
                    var taskType = task.GetType();
                    var resultField = taskType.GetField("m_result", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (resultField != null)
                    {
                        var result = resultField.GetValue(task);
                        logger.LogInformation("{methodName} exit." + (this.LogInput ? " Return: {return}." : ""), args.Method.Name, result.ToJsonSafe());
                    }
                    else
                    {
                        logger.LogInformation("{methodName} exit." + (this.LogInput ? " Return: {return}." : ""), args.Method.Name, "null");
                    }
                });
            }
            else
            {
                logger.LogInformation("{methodName} exit." + (this.LogInput ? " Return: {return}." : ""), args.Method.Name, args.ReturnValue?.ToJsonSafe() ?? "null");
            }
        }

        public override void OnException(MethodExecutionArgs args)
        {
            var logger = (args.MethodExecutionTag as ILogger)!;
            if (args.Exception is AggregateException aggregateException)
            {
                var exception = aggregateException.InnerExceptions.FirstOrDefault();
                logger.Log(exception.GetLogLevel(), exception, "{methodName} exception.", args.Method.Name);
            }
            else
            {
                logger.Log(args.Exception.GetLogLevel(), args.Exception, "{methodName} exception.", args.Method.Name);
            }
        }
    }
}
