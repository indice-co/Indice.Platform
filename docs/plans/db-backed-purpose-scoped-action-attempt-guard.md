# DB-backed Purpose-Scoped Action Attempt Guard on User (Separate Table)

## Goal
Implement a reusable guard that stores action attempts in the database and enforces purpose-scoped limits using a sliding window (e.g. 24 hours), shared by both TOTP and non-MFA SMS flows.

## Scope
- Persist attempts in a dedicated table (`auth.UserActionAttempt`), not in a JSON column on `User`.
- Keep purpose as a free-form key (e.g. `Sms:ChangePhoneNumber`, `Sms:StrongCustomerAuthentication`).
- Provide an injectable guard service used by:
  - `TotpServiceUser<TUser>.SendAsync`
  - `BasePageModel.SendVerificationSmsAsync`

## Design

### 1) New entity: `UserActionAttempt`
Add model in `src/Indice.Features.Identity.Core/Data/Models/UserActionAttempt.cs` with:
- `string UserId`
- `string PurposeKey`
- `int Count`
- `DateTimeOffset WindowEnd`
- `DateTimeOffset LastAttemptDate` (for diagnostics)

### 2) User relationship
Keep FK relationship from `UserActionAttempt` to `User`, but do **not** add navigation collection on `User`.

### 3) EF mapping
Add `src/Indice.Features.Identity.Core/Data/Mappings/UserActionAttemptMap.cs`:
- table: `auth.UserActionAttempt`
- composite PK: `(UserId, PurposeKey)`
- required fields + max length for `PurposeKey`
- index on `WindowEnd` for maintenance/reporting
- FK to `User` with cascade delete and no principal navigation (`HasOne<TUser>().WithMany()`)

### 4) DbContext registration
Update `src/Indice.Features.Identity.Core/Data/IdentityDbContext.cs`:
- add `DbSet<UserActionAttempt> UserActionAttempts`
- add `builder.ApplyConfiguration(new UserActionAttemptMap<TUser>());`

### 5) Guard abstraction + implementation
Add `src/Indice.Features.Identity.Core/Guards/UserActionGuard.cs`:
- `IUserActionGuard`
  - `Task<bool> IsBlockedAsync(string userId, string purposeKey, CancellationToken ct = default)`
  - `Task<int> RecordAttemptAsync(string userId, string purposeKey, CancellationToken ct = default)`
- `UserActionGuard` implementation backed by EF/Identity context.

Sliding window behavior:
- If row missing => create with `Count = 1`, `WindowEnd = now + Window`.
- If row exists and `now > WindowEnd` => reset to `Count = 1`, `WindowEnd = now + Window`.
- Else increment `Count` and slide `WindowEnd = now + Window`.
- Block if `Count >= MaxAttempts` while `now <= WindowEnd`.

### 6) Options
Add `UserActionGuardOptions` (same file or separate):
- section name: `UserActionGuard`
- `int MaxAttempts`
- `TimeSpan Window` (default 24h)

### 7) DI registration
- Add extension in `src/Indice.Features.Identity.Core/Extensions/IServiceCollectionExtensions.cs` to register options + `IUserActionGuard`.
- Wire from `src/Indice.Features.Identity.Server/Extensions/ServiceCollectionExtensions.cs` in the identity setup path.

### 8) Integrate in TOTP path
Update `src/Indice.Features.Identity.Core/Totp/TotpServiceUser.cs`:
- Before send: `IsBlockedAsync(user.Id, $"{channel}:{purpose}")`
- After successful send: `RecordAttemptAsync(...)`
- Keep existing short cooldown behavior intact.

### 9) Integrate in non-MFA phone verification path
Update `src/Indice.Features.Identity.UI/Pages/BasePageModel.cs`:
- Guard `SendVerificationSmsAsync` with purpose key `Sms:ChangePhoneNumber`.
- Return/propagate blocked state.

Update callers:
- `src/Indice.Features.Identity.UI/Pages/AddPhone.cs`
- `src/Indice.Features.Identity.UI/Pages/MfaOnboardingAddPhone.cs`
- `src/Indice.Features.Identity.UI/Pages/VerifyPhone.cs`

### 10) Tests
Add tests under `test/Indice.Features.Identity.Tests/` for:
- per-purpose isolation
- sliding window reset
- block after max attempts
- integration points in TOTP + phone verification flows
- uniqueness behavior on `(UserId, PurposeKey)`

## Migration Note
Consumers must add an EF Core migration to create `auth.UserActionAttempt` with:
- composite primary key `(UserId, PurposeKey)`
- foreign key to `auth.User(UserId)` with cascade delete
- index on `WindowEnd`
