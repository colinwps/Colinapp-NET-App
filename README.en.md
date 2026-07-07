# Colinapp — General-Purpose Admin Framework

[中文](./README.md) | **English**

A ready-to-use foundation for backend admin systems: **ASP.NET Core (.NET 10)** on the backend, **Vue 3 + Element Plus** on the frontend.
It ships with organization management, RBAC with data-scope permissions, platform settings (logs/dictionaries/parameters), scheduled jobs, code generation, a visual approval workflow, menu-driven dynamic routing, and a reserved multi-tenancy switch — a solid base for building business systems on top of.

## Tech Stack

| Layer | Choice |
|-------|--------|
| Backend framework | ASP.NET Core (.NET 10) Web API |
| ORM / Database | EF Core 9 (Pomelo) + MySQL (Code First + migrations) |
| Authentication | JWT (access + refresh token), BCrypt password hashing |
| Scheduling | Quartz.NET (built-in job registry + DB-backed job config) |
| Logging | Serilog (console + file), operation/login logs persisted to DB |
| Docs / Tooling | Swagger, Mapster, FluentValidation |
| Frontend | Vue 3 + Vite + TypeScript + Pinia + Vue Router + Element Plus + Axios (pnpm) |
| Flow designer | LogicFlow (custom nodes, graphs stored as JSON) |

> Note: the Pomelo MySQL provider has no EF Core 10 release yet, so all EF packages are pinned to 9.0.0 while running on the .NET 10 runtime; ASP.NET Core packages stay on 10.x. See `CLAUDE.md` for details.

## Features

### Organization & Permissions
- **Organization management**: users, departments (tree), positions, roles, menus.
- **Functional permissions**: RBAC (`[HasPermission("area:entity:action")]` dynamic policies + permission strings), with a matching `v-auth` directive on the frontend.
- **Data permissions**: row filtering by role data scope (all / custom / own dept / dept and children / self only; multiple roles take the union).

### Platform Settings
- **Logs**: operation logs (collected automatically via AOP) and login logs.
- **Dictionaries / Parameters**: data dictionaries and parameter configuration (with caching).
- **File management**: upload and management.
- **Scheduled jobs**: Quartz scheduling — register a built-in job in `JobRegistry` and it's ready to use; supports cron config, pause/resume, run-once, and execution logs (built-ins include log cleanup, token cleanup, and workflow timeout reminders).
- **Code generation**: generates backend/frontend boilerplate from database tables to speed up module development.

### Approval Workflow
- **Visual flow designer**: drag-and-drop editing based on LogicFlow with start / approval / condition / cc / end nodes; flows are stored as graph JSON (nodes + edges), validated on both client and server before saving (structure, reachability, acyclicity).
- **Approval modes**: any-approver (one approval passes) / all-approvers (countersign); approvers can be specific users or a role.
- **Conditional branches**: conditions live on outgoing edges (structured field/operator/value rules with priority and a default branch), evaluated against the form data filled in at submission.
- **Dynamic forms**: flow definitions can declare form fields (text/number/date/select, etc.) rendered when submitting a request.
- **Reject strategies**: each approval node can be configured to reject the whole instance / return to the previous approval node / return to the initiator (who can edit and resubmit).
- **CC (carbon copy)**: passing a cc node automatically creates cc records; the "CC to Me" page supports an unread filter and mark-as-read.
- **Timeout reminders**: approval nodes can set a time limit (hours); overdue tasks are flagged in the todo list and reminded by a scheduled job.
- **Snapshot mechanism**: the flow graph and form definition are snapshotted into the instance at submission, so editing a definition never affects in-flight approvals.
- The engine is pure logic (`WorkflowGraphEngine`) with unit test coverage; design details in `docs/工作流设计器规划.md`.

### Framework Mechanics
- **Menu-driven extensibility**: menus carry a component path; after login the frontend fetches the menu tree and builds routes dynamically — adding a module requires no routing code changes.
- **Multi-tenancy switch**: entities carry a built-in `TenantId` + global query filters, toggled with `MultiTenancy:Enabled` (off by default; single-tenant works out of the box).
- **Unified conventions**: unified response envelope `{ code, message, data }`, global exception handling, soft delete, and automatic auditing/tenant stamping.

## Project Structure

```
Colinapp.slnx                 # .NET 10 solution (.slnx format)
├─ src/
│  ├─ Colinapp.Api            # Startup layer: controllers, middleware, auth, DI composition
│  ├─ Colinapp.Application    # Application layer: services, DTOs, interfaces, permissions/data scope, workflow engine
│  ├─ Colinapp.Domain         # Domain layer: entities (with EntityBase audit base class), enums
│  ├─ Colinapp.Infrastructure # Infrastructure: DbContext, EF configs, migrations, interceptors, seeders, jobs
│  └─ Colinapp.Shared         # Shared: unified response, paging, exceptions
├─ tests/
│  └─ Colinapp.Application.Tests  # Unit tests (workflow graph validation & engine)
├─ web/                       # Frontend project (Vue 3 + Vite)
└─ docs/                      # Architecture plan, multi-tenancy notes, workflow designer plan
```

Dependency direction: `Api → Application → Domain ← Infrastructure`, with `Shared` referenced by all layers.

## Getting Started

### Prerequisites
- .NET 10 SDK
- MySQL 8.x
- Node.js 18+ and pnpm

### Backend

1. Configure the database connection and secret: edit `ConnectionStrings:Default` and `Jwt:SecretKey` in `src/Colinapp.Api/appsettings.json`.
2. Run (migrations are applied and seed data written automatically at startup):
   ```bash
   dotnet run --project src/Colinapp.Api
   ```
   The API listens on http://localhost:5218 ; Swagger is at `/swagger` in Development.

> Managing migrations manually (requires a reachable database):
> ```bash
> export COLINAPP_CONNECTION="server=HOST;port=PORT;database=colinapp;user=USER;password=PWD;CharSet=utf8mb4;"
> dotnet ef migrations add <Name> --project src/Colinapp.Infrastructure --startup-project src/Colinapp.Api
> dotnet ef database update      --project src/Colinapp.Infrastructure --startup-project src/Colinapp.Api
> ```
> Note: after `dotnet ef migrations add`, rebuild with `dotnet build` before running with `--no-build`, otherwise the stale assembly won't contain the new migration.

### Frontend

```bash
cd web
pnpm install
pnpm dev      # http://localhost:5173 (proxies /api to :5218)
```

Production build: `pnpm build` (runs the `vue-tsc` type check first, then bundles).

### Tests

```bash
dotnet test tests/Colinapp.Application.Tests
```

### Default Account

`admin` / `Admin@123` (super admin; bypasses permission checks).

## Extending with a Business Module

Using the built-in "Notice" sample as a reference, the standard flow (no framework core changes needed):

1. Create the entity in `Colinapp.Domain` (extend `EntityBase`).
2. Add the EF configuration in `Colinapp.Infrastructure` (auto-discovered).
3. Write the service + DTOs in `Colinapp.Application` and register them in `DependencyInjection`.
4. Add the controller in `Colinapp.Api`, annotating actions with `[HasPermission("area:entity:action")]`.
5. Register the menus and permission points in `MenuSeeder` (idempotently ensured on every startup).
6. Create the page under `web/src/views/...` matching the menu's component path, and add the API wrapper under `web/src/api`.

You can also use "System Tools → Code Generation" to generate the boilerplate above from a table, then adjust it.

## Documentation

- Architecture plan: `docs/架构规划.md`
- Multi-tenancy notes: `docs/多租户说明.md`
- Workflow designer plan: `docs/工作流设计器规划.md`
- Engineering notes for AI collaboration: `CLAUDE.md`

## Security Notes

Before deploying to production, replace the placeholder `Jwt:SecretKey` in `appsettings.json` (prefer user secrets / environment variables) and change the default admin password. When multi-tenancy is enabled, remove the `X-Tenant-Id` header fallback in `CurrentUser` and trust only the JWT claim.
