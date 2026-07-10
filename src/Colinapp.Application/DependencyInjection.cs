using Colinapp.Application.Auth;
using Colinapp.Application.Common;
using Colinapp.Application.Permissions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Colinapp.Application;

/// <summary>
/// 应用层 DI 注册入口。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<Storage.FileStorageOptions>(
            configuration.GetSection(Storage.FileStorageOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IExcelService, MiniExcelService>();
        services.AddScoped<IAuthService, AuthService>();

        // 权限与数据范围
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IDataScopeService, DataScopeService>();

        // 组织管理业务服务
        services.AddScoped<Organization.IDepartmentService, Organization.DepartmentService>();
        services.AddScoped<Organization.IPositionService, Organization.PositionService>();
        services.AddScoped<Organization.IRoleService, Organization.RoleService>();
        services.AddScoped<Organization.IMenuService, Organization.MenuService>();
        services.AddScoped<Organization.IUserService, Organization.UserService>();

        // 平台设置业务服务
        services.AddScoped<Platform.ILogService, Platform.LogService>();
        services.AddScoped<Platform.IDictService, Platform.DictService>();
        services.AddScoped<Platform.IConfigService, Platform.ConfigService>();

        // 文件存储
        services.AddScoped<Storage.IFileService, Storage.FileService>();

        // 代码生成器
        services.AddScoped<CodeGen.ICodeGenService, CodeGen.CodeGenService>();

        // 定时任务（调度运行时由 Infrastructure 的 Quartz 实现）
        services.AddScoped<Scheduling.IScheduledJobService, Scheduling.ScheduledJobService>();

        // 业务扩展样例
        services.AddScoped<Business.INoticeService, Business.NoticeService>();

        // 审批工作流
        services.AddScoped<Workflow.IWorkflowService, Workflow.WorkflowService>();

        // 表单中心
        services.AddScoped<Forms.IFormService, Forms.FormService>();

        return services;
    }
}
