using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Colinapp.Application.Common;
using Colinapp.Application.Platform;
using Colinapp.Domain.Entities.System;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Colinapp.Api.Filters;

/// <summary>
/// 全局操作日志过滤器。自动记录已登录用户的写操作（POST/PUT/DELETE/PATCH），
/// 标题取 [OperationLog] 特性或「控制器/方法」，可用 [SkipOperationLog] 跳过。
/// 日志写入独立作用域的 DbContext，避免污染业务请求的工作单元。
/// </summary>
public class OperationLogFilter(
    ICurrentUser currentUser,
    IServiceScopeFactory scopeFactory,
    ILogger<OperationLogFilter> logger) : IAsyncActionFilter
{
    private const int MaxParamLength = 2000;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var method = http.Request.Method;
        var isMutating = HttpMethods.IsPost(method) || HttpMethods.IsPut(method)
            || HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method);

        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var skip = descriptor?.MethodInfo.GetCustomAttribute<SkipOperationLogAttribute>() is not null
            || descriptor?.ControllerTypeInfo.GetCustomAttribute<SkipOperationLogAttribute>() is not null;

        // 捕获参数（在动作执行前，避免被修改）
        var requestParams = SerializeParams(context.ActionArguments);

        var sw = Stopwatch.StartNew();
        var executed = await next();
        sw.Stop();

        // 仅记录已登录用户的写操作
        if (!isMutating || skip || currentUser.UserId is null)
            return;

        var title = descriptor?.MethodInfo.GetCustomAttribute<OperationLogAttribute>()?.Title
            ?? $"{descriptor?.ControllerName}/{descriptor?.ActionName}";

        var log = new OperationLog
        {
            Title = title,
            OperatorName = currentUser.UserName,
            Method = descriptor is null ? null : $"{descriptor.ControllerName}.{descriptor.ActionName}",
            RequestMethod = method,
            RequestUrl = $"{http.Request.Path}{http.Request.QueryString}",
            RequestParams = requestParams,
            Ip = http.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http.Request.Headers.UserAgent.ToString(),
            Success = executed.Exception is null,
            ErrorMessage = Truncate(executed.Exception?.Message, 2000),
            Duration = sw.ElapsedMilliseconds,
        };

        // 独立作用域写入，不阻断主流程
        try
        {
            using var scope = scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<ILogService>();
            await logService.AddOperationLogAsync(log, http.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "写入操作日志失败：{Title}", title);
        }
    }

    private static string? SerializeParams(IDictionary<string, object?> args)
    {
        if (args.Count == 0) return null;
        try
        {
            var json = JsonSerializer.Serialize(args, JsonOpts);
            // 含密码字段的请求脱敏，避免明文落库
            if (json.Contains("assword", StringComparison.OrdinalIgnoreCase)
                || json.Contains("\"pwd\"", StringComparison.OrdinalIgnoreCase))
                return "[含敏感字段，已脱敏]";
            return Truncate(json, MaxParamLength);
        }
        catch
        {
            return null;
        }
    }

    private static string? Truncate(string? s, int max)
        => s is null ? null : s.Length <= max ? s : s[..max];

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
}
