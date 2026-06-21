namespace Colinapp.Shared.Common;

/// <summary>
/// 业务返回码约定。沿用 HTTP 习惯，便于前端统一拦截处理。
/// </summary>
public static class ResultCode
{
    /// <summary>成功</summary>
    public const int Success = 200;

    /// <summary>参数校验失败</summary>
    public const int BadRequest = 400;

    /// <summary>未认证 / 登录失效</summary>
    public const int Unauthorized = 401;

    /// <summary>无权限</summary>
    public const int Forbidden = 403;

    /// <summary>资源不存在</summary>
    public const int NotFound = 404;

    /// <summary>业务错误 / 服务器错误</summary>
    public const int Error = 500;
}
