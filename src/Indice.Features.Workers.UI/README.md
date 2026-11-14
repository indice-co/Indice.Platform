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
| **Dashboard** | Counters (enqueued, scheduled, retrying, poison, scheduled tasks, running, failing, active locks), the messages processed per minute, a live queue-depth chart sampled in the browser, the busiest queues and the tasks that need attention. |
| **Queues** | One row per logical queue — `orders` and `orders-poison` are one row — with a counter per status, the age of the oldest waiting message and its throughput (processed and failed per minute, average handler time, in progress). Queues the live workers consume appear even when empty, with their polling settings; a queue no live worker consumes, or whose workers have different settings, is flagged. **Add message** enqueues a new message into any queue. |
| **Queue detail** | The messages of a queue by status: **Enqueued**, **Scheduled** (delayed), **Retrying**, **Poison**. Select messages to **requeue** them (poison → back to the queue, scheduled → visible now, retrying → attempts reset) or **delete** them. **Add message** enqueues a new message, now or at a later date. |
| **Message detail** | Status, attempts, visibility date and the JSON payload. **Edit & requeue** fixes the payload of a message (typically a poison one) and puts it back in its queue. |
| **Scheduled tasks** | Every task that has run at least once or that a live worker hosts: cron expression, last run and its outcome, next run, run count, status, progress, an **Enabled** switch and **Run now**. |
| **Scheduled task detail** | Handler, worker of the last run, average and last run duration, the last error (full exception text) and the persisted state. |
| **Servers & locks** | The registered workers — **Alive** while they send their heartbeat, **Stale** when they miss it — with their process, version, uptime, queues, scheduled tasks and messages processed (per minute and since start), plus older workers seen only running scheduled tasks (**Unknown**). Then the distributed locks: **Release** force-deletes a lock left behind by a worker that died. |

All pages refresh by polling (30 s, 1 min, 2 min or 5 min; default 30 s). Polling pauses when the browser tab is hidden and can
be paused by hand. The overall counters (`GET stats`) are cached on the server for `StatsCacheDuration` (10 s), so many open
dashboards cost one query per period. Read-only queue queries run in `READ UNCOMMITTED` so that they never block, or wait
for, the workers that dequeue; the counters may be a little off while messages move.

The changes the dashboard makes are plain database writes, so they work from any process:

- enable / disable a scheduled task — the worker skips the next runs of a disabled task,
- run a scheduled task now (see below),
- requeue, edit and delete messages,
- enqueue a new message,
- release a lock.

### Run now

The scheduler runs inside each worker, so **Run now** leaves a request in the database (a message in the internal queue
`$run-now:{taskId}`, hidden from the dashboard). Every worker checks the requests of the tasks it hosts every
`WorkerHostOptions.RunNowPollingInterval` (default 15 s), claims one atomically and runs the task once, outside its schedule:

- it runs even when the task is **disabled**; the schedule stays disabled,
- a singleton task still takes its lock: when another run holds it, the requested run is skipped,
- while a request waits, the task shows **Run requested** and asking again does not queue a second run,
- a request no worker picks up within `WorkerHostOptions.RunNowExpiration` (default 10 min) is discarded.

Run now needs workers on a version of `Indice.Hosting` that has it, using the relational store (`UseStoreRelational`).

### What it can't show

Everything comes from the database, and the database keeps only what is pending:

- **No history of processed messages.** The relational queue store deletes a message when a worker dequeues it, so there is
  no Processing / Succeeded / Deleted list. A message that disappears while you look at it was most likely processed. The
  throughput and duration figures come from the workers themselves (see [Server registration](#server-registration)):
  they cover the last minute of the live workers only, not past days.
- **No cron expressions, polling settings or retry limits without server registration.** They live in the worker process;
  each worker writes them to the `work.Server` table when it starts (see below). Workers on older versions of
  `Indice.Hosting` don't, so their queues show no settings and a scheduled task appears **after its first run** (that run
  creates its row).
- **Servers that don't register** (older workers) appear under Servers only through the scheduled tasks they ran, and
  workers that only process queues don't appear at all.
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
3. The roles **`WorkersReader`** and **`WorkersAdmin`** (`BasicRoleNames.WorkersReader` / `BasicRoleNames.WorkersAdmin`), given
   to the users that operate the workers. The role claim must reach the API (the `role` scope / API resource claims).
4. *Optional, for machine-to-machine publishing:* the API scope **`workers:producer`** (`WorkerScopes.Producer`), and a client with
   grant type client credentials that is allowed it. The scope **`workers:consumer`** (`WorkerScopes.Consumer`) is reserved for the
   future dequeue endpoints and grants nothing yet.

The dashboard requires an authenticated user with the `workers` scope (`WorkerEndpointsOptions.RequiredScope`) **and** a role:

| Policy | Who | Allows |
| --- | --- | --- |
| `WorkerPolicies.BeWorkersReader` | `WorkersReader`, `WorkersAdmin` or an administrator (`IsAdmin()`) | Every `GET` — all pages, read only. |
| `WorkerPolicies.BeWorkersAdmin` | `WorkersAdmin` or an administrator (`IsAdmin()`) | Every change: enable/disable, run now, requeue, edit, delete, enqueue, release lock. |
| `WorkerPolicies.BeWorkersProducer` | `BeWorkersAdmin`, **or** the `workers:producer` scope | `POST queues/{queueName}/messages` (enqueue). |
| `WorkerPolicies.BeWorkersConsumer` | A machine client with the `workers:consumer` scope | Reserved for the dequeue endpoints, not available yet. |

Anyone else gets `403`; a client that only has the `workers` scope (e.g. a machine client) is not enough. The SPA asks `GET me` for the
permissions of the user and hides the actions of a reader.

**Machine-to-machine publishing.** A client credentials token (no `sub` claim) with the `workers:producer` sub-scope is enough to
enqueue messages — no role is needed. A *user* token with the sub-scope still needs the `WorkersAdmin` role. The sub-scope grants
nothing else: reading the queues and messages, scheduled tasks, locks, servers, requeue, edit and delete stay with the dashboard users.
Publish from another service with `AddEventDispatcherHostingHttp()` of `Indice.Services`. Set `ProducerScope` to `null` to turn this off.

> **Who gets in.** The `workers-ui` client requests the `workers` scope for everyone who signs in through it; the roles decide
> what each user can do. Set `WorkerEndpointsOptions.RequiredScope = null` to check the roles only.

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
| `RequiredScope` | `workers` | Scope that the caller must have, on top of the role (see above). Set it to `null` to check the roles only. |
| `ProducerScope` | `workers:producer` | Sub-scope that lets a machine client enqueue messages (`BeWorkersProducer`). `null` disables it. |
| `ConsumerScope` | `workers:consumer` | Sub-scope reserved for the future dequeue endpoints (`BeWorkersConsumer`). `null` disables it. |
| `StatsCacheDuration` | 10 s | How long the overall counters (`GET stats`) are cached. |
| `MaxPayloadLength` | 1,000,000 | The maximum length, in characters, of a payload enqueued or edited from the dashboard. |
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
| `Theme` | `workers` | The daisyUI theme name. The built-in `workers` theme follows the user's light/dark/system choice; any other name (e.g. `workers-dark`, or a custom theme added through `HeadContent`) is applied as-is and hides the theme switcher. A `?uitheme=name` query argument overrides it for the browser session. |
| `Lang` | the current UI culture | The `lang` attribute of the page. |
| `HeadContent` | — | Extra markup for the page `<head>` (e.g. a stylesheet with a custom theme). |
| `Enabled` | `true` | Set to `false` to stop serving the SPA. |

---

## Health checks

`Indice.Features.Workers.Server` also adds two health checks over the worker tables. They report `Degraded` by default and need
an `ITaskDbContext`, registered by `AddWorkerEndpoints()` (or by `UseStoreRelational()` in a worker process):

```csharp
builder.Services.AddHealthChecks()
    // Poison messages above the threshold, with the count per queue in the result data.
    .AddWorkerPoisonMessages(configure: options => {
        options.Threshold = 10;
        options.QueueNames = ["orders"];          // empty = every queue
    })
    // Tasks still running long after they started, and (optionally) enabled tasks that missed their schedule.
    .AddWorkerStuckTasks(configure: options => {
        options.StuckAfter = TimeSpan.FromHours(2);
        options.OverdueAfter = TimeSpan.FromMinutes(30);  // null (default) = don't check
        options.IgnoredTaskIds = ["InternalJobsGroup.long-etl"];
    });
```

---

## Database index

`Indice.Hosting` now indexes the queue table on `(QueueName, Date)` including `State` and `DequeueCount` (SQL Server), replacing
the index on `QueueName`. It serves both the dequeue query of the workers and the counters of the dashboard. Add a migration to
the context that holds the worker tables to apply it.

## Server registration

Each worker (`AddWorkerHost()` with `UseStoreRelational()`) registers itself in the `work.Server` table on startup, refreshes
its row every `WorkerHostOptions.ServerHeartbeatInterval` (30 s) and deletes it on shutdown. Every worker also deletes the
rows that have had no heartbeat for `WorkerHostOptions.ServerTimeout` (5 min), so a crashed worker goes away on its own.
The dashboard shows a server as **Stale** once it misses three heartbeats.

The row holds the queues the worker consumes with their settings and the scheduled tasks it hosts, so the dashboard can show
empty queues, queues no live worker consumes, settings that differ between workers (e.g. during a rolling deployment) and
scheduled tasks that have never run — which can also **Run now**. When workers declare different settings for a queue or
a scheduled task, the dashboard shows those of the most recently started live worker, i.e. the newest deployment. Add a
migration to the context that holds the worker tables to create the table (the `Indice.Hosting` changelog has the scripts).
Until then the dashboard works as before.

With every heartbeat the worker also stores a snapshot of its own metrics (the `Indice.Hosting` meter): messages processed
and failed since it started and in the last minute, messages in progress, average handler time and wait, and scheduled task
run durations. The dashboard sums them up across the live workers and shows throughput on the dashboard, the queues and the
servers, and run durations on the scheduled task page. The figures are as fresh as the last heartbeat; export the meter to
your monitoring system (OpenTelemetry) for history and alerts.

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
