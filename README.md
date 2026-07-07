# Colinapp 通用后台管理框架

一个开箱即用的通用后台管理框架基础设施：后端 **ASP.NET Core (.NET 10)**，前端 **Vue 3 + Element Plus**。
内置组织管理、RBAC 与数据权限、平台设置（日志/字典/参数）、定时任务、代码生成、可视化审批工作流、菜单驱动的动态路由，以及预留的多租户开关，可作为业务系统的二次开发底座。

## 技术栈

| 层 | 选型 |
|----|------|
| 后端框架 | ASP.NET Core (.NET 10) Web API |
| ORM / 数据库 | EF Core 9（Pomelo）+ MySQL（Code First + 迁移） |
| 认证 | JWT（access + refresh token），BCrypt 口令哈希 |
| 调度 | Quartz.NET（内置任务注册表 + 数据库任务配置） |
| 日志 | Serilog（控制台 + 文件），操作/登录日志落库 |
| 文档 / 工具 | Swagger、Mapster、FluentValidation |
| 前端 | Vue 3 + Vite + TypeScript + Pinia + Vue Router + Element Plus + Axios（pnpm） |
| 流程设计器 | LogicFlow（自定义节点，图存 JSON） |

> 说明：Pomelo MySQL 驱动暂无 EF Core 10 版本，故 EF 相关包固定 9.0.0、运行在 .NET 10 运行时上；ASP.NET Core 包仍用 10.x。详见 `CLAUDE.md`。

## 功能

### 组织与权限
- **组织管理**：用户、部门（树形）、职位、角色、菜单。
- **功能权限**：RBAC（`[HasPermission("area:entity:action")]` 动态策略 + 权限标识），前端配套 `v-auth` 指令。
- **数据权限**：按角色数据范围过滤（全部/自定义/本部门/本部门及下级/仅本人，多角色取并集）。

### 平台设置
- **日志**：操作日志（AOP 自动采集）、登录日志。
- **字典 / 参数**：数据字典、参数配置（带缓存）。
- **文件管理**：上传与管理。
- **定时任务**：Quartz 调度，内置任务在 `JobRegistry` 登记即用；支持 Cron 配置、暂停/恢复、手动执行、执行日志（内置日志清理、令牌清理、审批超时提醒等任务）。
- **代码生成**：按数据表生成后端/前端样板代码，加速业务模块开发。

### 审批工作流
- **可视化流程设计器**：基于 LogicFlow 拖拽绘制，支持 开始/审批/条件判断/抄送/结束 节点，流程以图 JSON（节点+边）存储；保存前前后端双侧校验（结构、可达性、无环）。
- **审批模式**：或签（任一人通过）/ 会签（全员通过）；审批人支持指定用户或角色。
- **条件分支**：条件挂在出边上（字段/操作符/值 的结构化规则，支持优先级与默认分支），按发起时填写的表单数据求值。
- **动态表单**：流程定义可配置表单字段（文本/数字/日期/下拉等），发起申请时按字段渲染。
- **驳回策略**：审批节点可配置 整体驳回 / 退回上一审批节点 / 退回发起人（发起人可修改后重新提交）。
- **抄送**：流程经过抄送节点自动生成抄送记录，「抄送我的」页面支持未读筛选与标记已读。
- **超时提醒**：审批节点可设时限（小时），到期未处理由定时任务提醒，待办列表标注超时。
- **快照机制**：发起时把流程图与表单定义快照进实例，修改流程定义不影响在途审批。
- 引擎为纯逻辑实现（`WorkflowGraphEngine`），配套单元测试；设计详见 `docs/工作流设计器规划.md`。

### 框架机制
- **菜单驱动扩展**：菜单挂载组件路径，前端登录后拉取菜单树动态生成路由；新增模块无需改路由代码。
- **多租户开关**：实体内置 `TenantId` + 全局查询过滤器，`MultiTenancy:Enabled` 一键开关（默认关闭，单租户开箱即用）。
- **统一约定**：统一响应信封 `{ code, message, data }`、全局异常处理、软删除、审计字段与租户自动填充。

## 项目结构

```
Colinapp.slnx                 # .NET 10 解决方案（.slnx 格式）
├─ src/
│  ├─ Colinapp.Api            # 启动层：Controllers、中间件、鉴权、DI 组合
│  ├─ Colinapp.Application    # 应用层：业务服务、DTO、接口、权限/数据范围、工作流引擎
│  ├─ Colinapp.Domain         # 领域层：实体（含 EntityBase 审计基类）、枚举
│  ├─ Colinapp.Infrastructure # 基础设施：DbContext、EF 配置、迁移、拦截器、种子、调度任务
│  └─ Colinapp.Shared         # 通用层：统一响应、分页、异常
├─ tests/
│  └─ Colinapp.Application.Tests  # 单元测试（工作流图校验与引擎）
├─ web/                       # 前端工程（Vue3 + Vite）
└─ docs/                      # 架构规划、多租户说明、工作流设计器规划
```

依赖方向：`Api → Application → Domain ← Infrastructure`，`Shared` 被各层引用。

## 快速开始

### 先决条件
- .NET 10 SDK
- MySQL 8.x
- Node.js 18+ 与 pnpm

### 后端

1. 配置数据库连接与密钥：编辑 `src/Colinapp.Api/appsettings.json` 的 `ConnectionStrings:Default` 与 `Jwt:SecretKey`。
2. 运行（启动时自动建表迁移 + 写入种子数据）：
   ```bash
   dotnet run --project src/Colinapp.Api
   ```
   API 默认地址 http://localhost:5218 ，开发环境 Swagger 在 `/swagger`。

> 手动管理迁移（需可连通的数据库）：
> ```bash
> export COLINAPP_CONNECTION="server=HOST;port=PORT;database=colinapp;user=USER;password=PWD;CharSet=utf8mb4;"
> dotnet ef migrations add <Name> --project src/Colinapp.Infrastructure --startup-project src/Colinapp.Api
> dotnet ef database update      --project src/Colinapp.Infrastructure --startup-project src/Colinapp.Api
> ```
> 注意：`dotnet ef migrations add` 之后需重新 `dotnet build` 再以 `--no-build` 运行，否则旧程序集不含新迁移。

### 前端

```bash
cd web
pnpm install
pnpm dev      # http://localhost:5173 （已将 /api 代理到 :5218）
```

构建产物：`pnpm build`（先 `vue-tsc` 类型检查再打包）。

### 测试

```bash
dotnet test tests/Colinapp.Application.Tests
```

### 默认账号

`admin` / `Admin@123`（超级管理员，跳过权限校验）。

## 扩展一个业务模块

以内置的「公告」样例为参考，标准流程（无需改动框架核心）：

1. 在 `Colinapp.Domain` 建实体（继承 `EntityBase`）。
2. 在 `Colinapp.Infrastructure` 加 EF 配置（自动被扫描加载）。
3. 在 `Colinapp.Application` 写服务 + DTO，并在 `DependencyInjection` 注册。
4. 在 `Colinapp.Api` 加控制器，接口用 `[HasPermission("area:entity:action")]` 标注。
5. 在 `MenuSeeder` 登记菜单与权限点（每次启动幂等补齐）。
6. 在 `web/src/views/...` 按菜单的组件路径建页面、加 `web/src/api` 封装。

也可用「系统工具 → 代码生成」按表生成上述样板代码后再做调整。

## 文档

- 架构规划：`docs/架构规划.md`
- 多租户说明：`docs/多租户说明.md`
- 工作流设计器规划：`docs/工作流设计器规划.md`
- 面向 AI 协作的工程说明：`CLAUDE.md`

## 安全提示

生产部署前务必替换 `appsettings.json` 中的占位 `Jwt:SecretKey`（建议放用户机密/环境变量），并修改默认管理员口令。多租户启用时建议移除 `CurrentUser` 中的 `X-Tenant-Id` 请求头回退，仅信任 JWT 声明。
