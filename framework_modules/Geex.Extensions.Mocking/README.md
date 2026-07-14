# Geex.Extensions.Mocking

Runtime mocking for business external dependencies (WeChat, SMS, Payments, ExternalTenantSync).

## Enable

1. Reference the project and add `[DependsOn(typeof(MockingModule))]`.
2. Set environment variable `MockingModuleOptions__Enabled=true` and restart.

When disabled, the module does not register GraphQL types, decorators, or endpoints.

## Frontend

Install `@geexcode/geex-extensions-mocking`, call `provideGeexMocking()` after `provideWechatAuth()`, and lazy-load `mockingRoutes` at `/mocking`.

## Security

- Management GraphQL APIs require authenticated SuperAdmin (`sub == 000000000000000000000001`).
- No additional permissions are introduced.
- WeChat authorize and payment checkout use opaque short-lived tokens.

## Pages

- `/mocking` management home
- `/mocking/wechat` mock profiles
- `/mocking/sms` SMS inbox
- `/mocking/wechat/authorize/{token}` public confirm page
- `/mocking/payments/{token}` public checkout page
