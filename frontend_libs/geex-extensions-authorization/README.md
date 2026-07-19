# @geexcode/geex-extensions-authorization

Frontend counterpart of backend `Geex.Extensions.Authorization`.

Canonical runtime entry: `geex.authorization`.

Provides:

- `geex.authorization` — tenant claim check, ACL load/persist/sync
- `AuthorizeGuard` — thin route adapter delegating to `geex.authorization`
- `LocalStorageACLService` — thin Delon ACL adapter
- `provideGeexAuthorization()` — register module + adapters

```ts
import { provideGeexCommon, geex } from "@geexcode/geex-angular";
import { provideGeexAuthorization, AuthorizeGuard } from "@geexcode/geex-extensions-authorization";

providers: [
  ...provideGeexCommon(),
  ...provideGeexAuthorization(),
]

// routes (adapter)
canActivate: [AuthorizeGuard]

// business API
await geex.authorization.canActivate(route, state);
```
