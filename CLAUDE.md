# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Colinapp is a general-purpose backend admin framework. Backend: ASP.NET Core (.NET 10), EF Core + MySQL, JWT. Frontend: Vue 3 + Vite + TypeScript + Element Plus + Pinia (in `web/`). It provides organization management (users/departments/positions/roles/menus), RBAC with data-scope permissions, platform settings (logs/dictionaries/parameters), menu-driven dynamic routing, and a reserved multi-tenancy switch. `docs/架构规划.md` is the authoritative architecture plan; `docs/多租户说明.md` documents the tenant switch.

## Commands

### Backend (run from repo root)
- Build: `dotnet build Colinapp.slnx` — the solution is `.slnx` (the .NET 10 XML format), **not** `.sln`.
- Run API: `dotnet run --project src/Colinapp.Api` (listens on http://localhost:5218; Swagger at `/swagger` in Development). Startup auto-applies migrations and seeds (`DbInitializer`).
- EF migrations need a reachable DB. The design-time factory reads connection from env var `COLINAPP_CONNECTION` (falls back to a localhost default):
  ```
  export COLINAPP_CONNECTION="server=...;port=...;database=colinapp;user=...;password=...;CharSet=utf8mb4;"
  dotnet ef migrations add <Name> --project src/Colinapp.Infrastructure --startup-project src/Colinapp.Api
  dotnet ef database update      --project src/Colinapp.Infrastructure --startup-project src/Colinapp.Api
  dotnet ef migrations has-pending-model-changes --project src/Colinapp.Infrastructure --startup-project src/Colinapp.Api
  ```
- Runtime DB connection + secrets live in `src/Colinapp.Api/appsettings.json` (`ConnectionStrings:Default`, `Jwt:SecretKey`, `MultiTenancy:Enabled`).
- Tests: `dotnet test tests/Colinapp.Application.Tests` — unit tests for the workflow graph engine (`WorkflowGraph.Validate` / `WorkflowGraphEngine`). The workflow module is graph-driven (nodes+edges JSON, schema in `docs/工作流设计器规划.md`); its engine lives in `Application/Workflow/WorkflowGraph*.cs` and is pure logic — extend tests there when touching it.

### Frontend (run from `web/`)
- `pnpm install`, then `pnpm dev` (http://localhost:5173, proxies `/api` → `:5218`), `pnpm build` (runs `vue-tsc` type-check then `vite build`), `pnpm type-check`.

### Default seeded login
`admin` / `Admin@123` (super admin; bypasses permission checks).

## Critical gotchas

- **EF Core version is pinned to 9.0.0, not 10.** Pomelo's MySQL provider has no EF Core 10 release yet, so all `Microsoft.EntityFrameworkCore.*` packages and the `dotnet-ef` tool stay 9.0.0 while running on the .NET 10 runtime. ASP.NET Core packages (e.g. `JwtBearer`) stay 10.x. Do not mix EF Core 10 packages in.
- **After `dotnet ef migrations add`, rebuild before running with `--no-build`.** The migration `.cs` is generated *after* the build that the EF tool ran, so a stale binary won't contain it and startup logs "pending model changes" / "already up to date".
- **The global query filter in `AppDbContext` must stay a generic typed lambda** (`SetGlobalFilter<TEntity>`). It references `_tenantFilterEnabled` / `CurrentTenantId` so EF parameterizes them. Rebuilding the filter manually with `Expression.Constant(this)` bakes context identity into the model snapshot and causes a permanent "model has pending changes" state.
- **Use an explicit `MySqlServerVersion`, not `ServerVersion.AutoDetect`** (config `Database:MySqlVersion`). AutoDetect opens a DB connection at startup/migration time, which fails before the DB exists.

## Backend architecture

Modular layered, dependency direction `Api → Application → Domain ← Infrastructure`, with `Shared` referenced by all:

- **Colinapp.Domain** — entities + enums, no dependencies. All business entities extend `EntityBase` (Id, CreatedBy/Time, UpdatedBy/Time, IsDeleted, TenantId). Pure join tables (`UserRole`, `RoleMenu`, `UserPost`, `RoleDept`) are plain classes (no `EntityBase`) so they get no soft-delete/tenant filter and are hard-deleted on reassignment.
- **Colinapp.Application** — business services + DTOs grouped by area (`Auth/`, `Permissions/`, `Organization/`, `Platform/`, `Business/`). Depends on the `IAppDbContext` abstraction and `ICurrentUser`, never on the concrete DbContext. Each area file typically holds DTOs + interface + service together. `DependencyInjection.AddApplication` registers everything.
- **Colinapp.Infrastructure** — `AppDbContext` (implements `IAppDbContext`), EF configurations (auto-loaded via `ApplyConfigurationsFromAssembly`), `AuditSaveChangesInterceptor`, `DbInitializer`, `MenuSeeder`, `DesignTimeDbContextFactory`. `AddInfrastructure` wires the DbContext.
- **Colinapp.Api** — controllers, middleware, auth plumbing, DI composition (`Program.cs`).
- **Colinapp.Shared** — `ApiResult`/`ApiResult<T>`, `ResultCode`, `BusinessException`, `PagedRequest`/`PagedResult`.

### Cross-cutting conventions

- **Unified response envelope.** Every endpoint returns `ApiResult { code, message, data }` with **HTTP 200**; clients branch on the body `code`. `ExceptionHandlingMiddleware` converts `BusinessException` to the envelope and also wraps framework-generated 401/403 (which otherwise have empty bodies) into the same shape. Throw `BusinessException` (or its `.NotFound()`/`.Forbidden()` helpers) for business errors.
- **Auditing / soft delete / tenant stamping** are automatic via `AuditSaveChangesInterceptor`: inserts fill CreatedBy/Time and `TenantId ??= currentUser.TenantId`; updates fill UpdatedBy/Time; deletes are converted to `IsDeleted = true`. Soft-deleted and cross-tenant rows are excluded by the `AppDbContext` global query filters. Use `IgnoreQueryFilters()` to bypass (e.g. log cleanup uses `ExecuteDeleteAsync` for real deletes; seeding checks existence with `IgnoreQueryFilters`).
- **`ICurrentUser`** is implemented in the Api layer (`CurrentUser`) from JWT claims. `TenantId` resolves JWT `tenant_id` first, then falls back to an `X-Tenant-Id` header (dev/testing aid — see `docs/多租户说明.md`; remove the header fallback for production).

### RBAC and data permissions

- **Functional permissions**: annotate actions with `[HasPermission("sys:user:list")]`. `PermissionPolicyProvider` turns the `PERM:<code>` policy name into a `PermissionRequirement` on the fly; `PermissionAuthorizationHandler` succeeds for admins, otherwise checks the user's aggregated permission set from `IPermissionService` (user → roles → menus' `Permission` strings). Permission strings follow `area:entity:action` (e.g. `sys:dept:add`, `biz:notice:edit`).
- **Data permissions**: `IDataScopeService` computes the visible department set from the user's roles' `DataScope` (All/Custom/Dept/DeptAndChild/Self, union = most permissive) and is applied inside list queries (see `UserService.GetPagedAsync` as the reference implementation).

### Menu-driven extensibility

`MenuSeeder.SeedAsync` runs on **every startup** (called by `DbInitializer`) and is **idempotent** — it ensures catalogs/pages/buttons exist by matching on permission string or name, so adding a module = registering it in the seeder and it auto-appears after restart. Menus carry a `Component` path string (e.g. `system/user/index`) that the frontend maps to a `.vue` file. The extension flow (see the `Notice` sample in `Domain/Entities/Business`, `Application/Business`, `NoticeController`, and `web/.../views/business/notice`) is: entity → EF config → service → controller → register in `MenuSeeder` → frontend view, with no core changes.

## Frontend architecture (`web/`)

- **Dynamic routing is data-driven.** After login the guard (`src/router/guard.ts`) fetches `/auth/me` (profile + permission strings) and `/menu/routes` (the current user's menu tree), then `router/dynamic.ts` flattens menu nodes into routes whose components are resolved from a `import.meta.glob('/src/views/**/*.vue')` map keyed by the menu's `component` string; missing components fall back to a placeholder. Routes are injected via `router.addRoute('Layout', ...)`. The Pinia `permission` store holds the tree for the sidebar; `user` store holds token/profile/permissions; `app` store holds UI state.
- **Request layer** (`src/utils/request.ts`) injects the Bearer token and unwraps the `ApiResult` envelope; body `code === 401` clears auth and redirects to login.
- **`v-auth="'perm:string'"`** directive removes elements the user lacks permission for (admin always passes).
- Element Plus is registered fully in `main.ts`; `unplugin-auto-import` only auto-imports `ElMessage`-style service functions. **Explicitly import Vue/Router APIs in components** — `vue-tsc -b` runs before Vite generates the auto-import `.d.ts`, so relying on auto-import breaks the type-check build.
- **Element Plus typing quirks** when editing views: el-table default slot `row` is typed `DefaultRow`, so cast at the call site (`openEdit(row as User)`); `el-tree-select`'s value field is `node-key`, not `props.value`.
