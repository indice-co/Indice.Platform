# Indice.Features.Workers.UI

A dashboard for [`Indice.Hosting`](../Indice.Hosting/README.md) background work, in the spirit of the Hangfire Dashboard.
The dashboard is split across two packages:

- the single page application (Angular, embedded in the assembly), served at **`/workers`**, and
- the minimal API it calls, mapped at **`/api/workers`** by the separate `Indice.Features.Workers.Server` package.

The API reads the worker tables (schema `work`) **straight from the database**. It is meant to run in its own host — a
back-office app, an admin site — **not** in the worker process that calls `AddWorkerHost()`. It needs no worker registration
and registers its own `ITaskDbContext` and `ILockDbContext`.

---

## What it shows

| Page | Contents |
| --- | --- |
| **Dashboard** | Counters (enqueued, scheduled, retrying, poison, scheduled tasks, running, failing, active locks), a live queue-depth chart sampled in the browser, the busiest queues and the tasks that need attention. |
| **Queues** | One row per logical queue — `orders` and `orders-poison` are one row — with a counter per status and the age of the oldest waiting message. |
| **Queue detail** | The messages of a queue by status: **Enqueued**, **Scheduled** (delayed), **Retrying**, **Poison**. Select messages to **requeue** them (poison → back to the queue, scheduled → visible now, retrying → attempts reset) or **delete** them. |
| **Message detail** | Status, attempts, visibility date and the JSON payload. |
| **Scheduled tasks** | Every task that has run at least once: last run and its outcome, next run, run count, status, progress, and an **Enabled** switch. |
| **Scheduled task detail** | Handler, worker of the last run, the last error (full exception text) and the persisted state. |
| **Servers & locks** | The workers seen running scheduled tasks, and the distributed locks. |

All pages refresh by polling (2 s – 30 s, default 5 s). Polling pauses when the browser tab is hidden and can be paused by hand.

The only changes the dashboard makes are plain database updates, so they work from any process:

- enable / disable a scheduled task — the worker skips the next runs of a disabled task,
- requeue messages,
- delete messages.

### What it can't show

Everything comes from the database, and the database keeps only what is pending:

- **No history of processed messages.** The relational queue store deletes a message when a worker dequeues it, so there is
  no Processing / Succeeded / Deleted list and no throughput graph. A message that disappears while you look at it was most
  likely processed.
- **No "Trigger now".** Scheduled tasks run on the in-process Quartz scheduler of each worker; there is nothing to signal it
  from another process.
- **No cron expressions, polling settings or retry limits.** These live in the worker's `AddJob<…>()` registrations, not in the
  database. A scheduled task appears **after its first run** (that run creates its row), with its next fire time.
- **Workers that only process queues don't appear under Servers.** Servers are derived from the scheduled task rows.
- **Locks kept outside the database** (`UseLockManagerAzure()`, `UseLockManagerInMemory()`) are not visible.
- **A task stuck in Running** means a worker stopped in the middle of a run. It is shown as it is stored.

---

## Install and configure

```csharp
var builder = WebApplication.CreateBuilder(args);

// Bearer tokens from your identity server (the dashboard API requires a signed-in user, see below).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = builder.Configuration["General:Authority"];
        options.Audience = "workers";   // the API resource that carries the `workers` scope
    });
builder.Services.AddAuthorization();

// The API requires the separate Indice.Features.Workers.Server package.
builder.Services.AddWorkersEndpoints(options => {
    options.ConfigureDbContext = db => db.UseSqlServer(builder.Configuration.GetConnectionString("WorkerDb"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseWorkersUI();  // the SPA at /workers; provided by Indice.Features.Workers.UI
app.MapWorkers();    // the API at /api/workers; provided by Indice.Features.Workers.Server

app.Run();
```

`ConfigureDbContext` on `WorkerEndpointsOptions` must point at the database the workers use, i.e. the one they pass to
`UseStoreRelational(...)`. Without it, the API uses SQL Server with the `WorkerDb` connection string — the same default as
`UseStoreRelational()`.

For PostgreSQL, add the Npgsql EF Core provider to the host and configure it the same way:

```csharp
builder.Services.AddWorkersEndpoints(options => {
    options.ConfigureDbContext = db => db.UseNpgsql(builder.Configuration.GetConnectionString("WorkerDb"));
});
```

### Worker tables in your own `DbContext`

When the workers use `UseStoreRelational<TContext>()` with your own context (one that calls `ApplyWorkerConfiguration`), use the
generic overload with the same context:

```csharp
builder.Services.AddWorkerEndpoints<MyDbContext>(options => {
    options.ConfigureDbContext = db => db.UseSqlServer(builder.Configuration.GetConnectionString("MyDb"));
});
```

If the host registers `MyDbContext` itself (`AddDbContext<MyDbContext>(...)`) **before** calling `AddWorkerEndpoints<MyDbContext>()`,
that registration is reused and `ConfigureDbContext` is ignored.

---

## Prerequisites in the identity server

1. An API scope **`workers`**.
2. A single page application client **`workers-ui`**:
   - grant type authorization code with PKCE, no client secret,
   - redirect URIs `{host}/workers/auth-callback` and `{host}/workers/auth-renew`,
   - post-logout redirect URI `{host}/workers/logged-out`,
   - allowed scopes `openid profile role email workers`.
The API accepts an authenticated caller that **has the `workers` scope, or is an administrator** (`IsAdmin()`). Anyone else gets
`403`.

> **Who gets in.** The `workers-ui` client requests the `workers` scope for everyone who signs in through it, so every user who
> can sign in to that client can use the dashboard. Restrict who can sign in to `workers-ui`, or set
> `WorkerEndpointsOptions.RequiredScope = null` (and drop `workers` from the client scopes) to allow administrators only.

The SPA reads the identity server address and its own public address from the host configuration:

```json
{
  "General": {
    "Authority": "https://identity.example.com",
    "Host": "https://backoffice.example.com"
  }
}
```

---

## Options

### `WorkerEndpointsOptions` — the API (`AddWorkersEndpoints` / `AddWorkerEndpoints<TContext>`)

| Property | Default | Description |
| --- | --- | --- |
| `PathPrefix` | `/api/workers` | Route prefix of the API. Must match `WorkerUIOptions.ApiBase`. |
| `GroupName` | `workers` | OpenAPI document (group) name of the endpoints. |
| `RequiredScope` | `workers` | Scope that grants access. Administrators are always allowed. Set it to `null` to allow administrators only. |
| `ConfigureDbContext` | SQL Server, connection string `WorkerDb` | Configures the database that holds the worker tables. Ignored when the host already registered the context type. |

### `WorkerUIOptions` — the SPA (`UseWorkersUI`)

Inherits the common options of `Indice.AspNetCore.EmbeddedUI` (`SpaUIOptions`).

| Property | Default | Description |
| --- | --- | --- |
| `PathPrefix` | `/workers` | Where the SPA is served. |
| `ApiBase` | `/api/workers` | Where the SPA calls the API. Same origin when relative. Must match `WorkerEndpointsOptions.PathPrefix`. |
| `ClientId` | `workers-ui` | The client id in the identity server. |
| `Scope` | `openid profile role email workers` | The scopes the SPA requests. |
| `Authority` | `General:Authority` | The identity server address. |
| `Host` | `General:Host` | The public address of this host; the redirect URIs are built from it. |
| `DocumentTitle` | `Workers` | The browser tab title. |
| `Theme` | `indice` | The daisyUI theme name. A `?uitheme=name` query argument overrides it for the browser session. |
| `Lang` | the current UI culture | The `lang` attribute of the page. |
| `HeadContent` | — | Extra markup for the page `<head>` (e.g. a stylesheet with a custom theme). |
| `Enabled` | `true` | Set to `false` to stop serving the SPA. |

---

## Changing the paths

The SPA and the API are configured separately, so change both sides together:

```csharp
builder.Services.AddWorkersEndpoints(options => {
    options.PathPrefix = "/admin/api/jobs";
});

app.UseWorkersUI(options => {
    options.PathPrefix = "/admin/jobs";
    options.ApiBase = "/admin/api/jobs";   // = WorkerEndpointsOptions.PathPrefix
});
```

When the UI `PathPrefix` changes, update the redirect URIs of the `workers-ui` client too
(`{host}/admin/jobs/auth-callback`, `{host}/admin/jobs/auth-renew`, `{host}/admin/jobs/logged-out`).

The API can also live on another host: set `ApiBase` to its absolute URL (`https://jobs-api.example.com/api/workers`) and allow
CORS for the dashboard origin there.

---

## Developing the SPA

The SPA source is in [`Indice.Features.Workers.App`](../Indice.Features.Workers.App/README.md). A Debug build of this project
builds it (`npm run build:template`) and embeds `dist/workers-app/browser/*`. A Release build embeds whatever is already in `dist`.

To run the SPA with `ng serve` against a real host, the host must allow CORS for `http://localhost:4200`, and the `workers-ui`
client must accept the `http://localhost:4200/...` redirect URIs.
