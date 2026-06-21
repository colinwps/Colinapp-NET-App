using System.Text.Json;
using Colinapp.Shared.Common;
using Colinapp.Shared.Exceptions;

namespace Colinapp.Api.Middleware;

/// <summary>
/// 全局异常中间件：将业务异常与未处理异常统一转换为 ApiResult 结构。
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);

            // 鉴权中间件短路返回的 401/403 不会抛异常且 body 为空，统一包装为 ApiResult 信封。
            if (!context.Response.HasStarted && context.Response.ContentLength is null or 0)
            {
                switch (context.Response.StatusCode)
                {
                    case StatusCodes.Status401Unauthorized:
                        await WriteAsync(context, ResultCode.Unauthorized, "未登录或登录已过期");
                        break;
                    case StatusCodes.Status403Forbidden:
                        await WriteAsync(context, ResultCode.Forbidden, "无访问权限");
                        break;
                }
            }
        }
        catch (BusinessException ex)
        {
            await WriteAsync(context, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "未处理异常：{Message}", ex.Message);
            await WriteAsync(context, ResultCode.Error, "服务器内部错误");
        }
    }

    private static async Task WriteAsync(HttpContext context, int code, string message)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = JsonSerializer.Serialize(ApiResult.Fail(message, code), JsonOptions);
        await context.Response.WriteAsync(body);
    }
}
