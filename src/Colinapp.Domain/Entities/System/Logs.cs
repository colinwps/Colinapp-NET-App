namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 操作日志。由全局操作日志过滤器自动记录写操作（POST/PUT/DELETE）。
/// 继承审计基类：CreatedTime=操作时间，CreatedBy=操作人 Id。
/// </summary>
public class OperationLog : EntityBase
{
    /// <summary>操作标题（模块/动作，默认取 控制器-方法，可由特性覆盖）</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>操作人账号</summary>
    public string? OperatorName { get; set; }

    /// <summary>处理方法（Controller.Action）</summary>
    public string? Method { get; set; }

    /// <summary>HTTP 方法</summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>请求 URL</summary>
    public string RequestUrl { get; set; } = string.Empty;

    /// <summary>请求参数（JSON，超长截断）</summary>
    public string? RequestParams { get; set; }

    /// <summary>客户端 IP</summary>
    public string? Ip { get; set; }

    /// <summary>User-Agent</summary>
    public string? UserAgent { get; set; }

    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>错误信息（失败时）</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>耗时（毫秒）</summary>
    public long Duration { get; set; }
}

/// <summary>
/// 登录日志。记录每次登录尝试（成功/失败）。
/// </summary>
public class LoginLog : EntityBase
{
    /// <summary>登录账号</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>提示信息（如「登录成功」「账号或密码错误」）</summary>
    public string? Message { get; set; }

    /// <summary>客户端 IP</summary>
    public string? Ip { get; set; }

    /// <summary>User-Agent</summary>
    public string? UserAgent { get; set; }
}
