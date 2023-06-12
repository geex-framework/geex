//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using JetBrains.Annotations;
//using MediatR;

//namespace Geex.Common.Abstractions
//{
//    public class TransactionalMediator : MediatR.Mediator
//    {
//        /// <summary>
//        /// Initializes a new instance of the <see cref="T:MediatR.Mediator" /> class.
//        /// </summary>
//        /// <param name="serviceFactory">The single instance factory.</param>
//        public TransactionalMediator([NotNull] ServiceFactory serviceFactory) : base(serviceFactory)
//        {
//        }

//        /// <summary>
//        /// Override in a derived class to control how the tasks are awaited. By default the implementation is a foreach and await of each handler
//        /// </summary>
//        /// <param name="allHandlers">Enumerable of tasks representing invoking each notification handler</param>
//        /// <param name="notification">The notification being published</param>
//        /// <param name="cancellationToken">The cancellation token</param>
//        /// <returns>A task representing invoking all handlers</returns>
//        protected override Task PublishCore(IEnumerable<Func<INotification, CancellationToken, Task>> allHandlers, INotification notification, CancellationToken cancellationToken)
//        {
//            var uow = base.
//            var result = base.PublishCore(allHandlers, notification, cancellationToken);

//            return result;
//        }
//    }
//}