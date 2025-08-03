#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace Geex.Extensions.Authorization.Core.Utils
{
    internal sealed class AuthorizeMiddleware
    {
        private readonly AuthorizeDirective _directive;

        public AuthorizeMiddleware(AuthorizeDirective directive)
        {
            this._directive = directive ?? throw new ArgumentNullException(nameof(directive));
        }

        public async Task InvokeAsync(IMiddlewareContext context, FieldDelegate next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            
            IAuthorizationHandler handler = context.GetGlobalStateOrDefault<IAuthorizationHandler>("HotChocolate.Authorization.AuthorizationHandler");
            if (handler == null)
                throw new MissingStateException("Authorization", "HotChocolate.Authorization.AuthorizationHandler", StateKind.Global);
            switch (this._directive.Apply)
            {
                case ApplyPolicy.BeforeResolver:
                    AuthorizeResult state1 = await handler.AuthorizeAsync(context, this._directive).ConfigureAwait(false);
                    if (state1 == AuthorizeResult.Allowed)
                    {
                        await next(context).ConfigureAwait(false);
                        break;
                    }
                    this.SetError(context, state1);

                    break;
                case ApplyPolicy.AfterResolver:
                    await next(context).ConfigureAwait(false);
                    if (context.Result == null)
                    {
                        break;
                    }
                    AuthorizeResult state2 = await handler.AuthorizeAsync(context, this._directive).ConfigureAwait(false);
                    if (state2 == AuthorizeResult.Allowed)
                    {
                        break;
                    }
                    if (AuthorizeMiddleware.IsErrorResult(context))
                    {
                        break;
                    }
                    this.SetError(context, state2);
                    break;
                default:
                    await next(context).ConfigureAwait(false);
                    break;
            }
        }

        private static bool IsErrorResult(IMiddlewareContext context)
        {
            bool flag;
            switch (context.Result)
            {
                case IError _:
                case IEnumerable<IError> _:
                    flag = true;
                    break;
                default:
                    flag = false;
                    break;
            }
            return flag;
        }

        private void SetError(IMiddlewareContext context, AuthorizeResult state)
        {
            IMiddlewareContext middlewareContext = context;
            IError error;
            switch (state)
            {
                case AuthorizeResult.NoDefaultPolicy:
                    error = ErrorBuilder.New().SetMessage("AuthCoreResources.AuthorizeMiddleware_NoDefaultPolicy").SetCode("AUTH_NO_DEFAULT_POLICY").SetPath(context.Path).AddLocation((ISyntaxNode)context.Selection.SyntaxNode).Build();
                    break;
                case AuthorizeResult.PolicyNotFound:
                    error = ErrorBuilder.New().SetMessage("AuthCoreResources.AuthorizeMiddleware_PolicyNotFound", (object)this._directive.Policy).SetCode("AUTH_POLICY_NOT_FOUND").SetPath(context.Path).AddLocation((ISyntaxNode)context.Selection.SyntaxNode).Build();
                    break;
                default:
                    error = ErrorBuilder.New().SetMessage($"Current user is not authorized for required permission of [{_directive.Policy}]").SetCode(state == AuthorizeResult.NotAllowed ? "AUTH_NOT_AUTHORIZED" : "AUTH_NOT_AUTHENTICATED").SetPath(context.Path).AddLocation((ISyntaxNode)context.Selection.SyntaxNode).Build();
                    break;
            }
            middlewareContext.Result = (object)error;
            middlewareContext.ReportError(error);
        }
    }
}
