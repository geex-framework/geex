# @geexcode/geex-extensions-authorization

Frontend counterpart of backend `Geex.Extensions.Authorization`.

Provides:

- `AuthorizeGuard` — token + tenant claim check
- `LocalStorageACLService` — Delon ACL bridge persisted to localStorage
- `provideGeexAuthorization()` — register providers

`IdentityClaims` lives in `@geexcode/geex-angular` (session shape = Core).

```ts
import { provideGeexCommon } from "@geexcode/geex-angular";
import { provideGeexAuthorization, AuthorizeGuard } from "@geexcode/geex-extensions-authorization";

providers: [
  ...provideGeexCommon(),
  ...provideGeexAuthorization(),
]

// routes
canActivate: [AuthorizeGuard]
```
