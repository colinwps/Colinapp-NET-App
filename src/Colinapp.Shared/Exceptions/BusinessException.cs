using Colinapp.Shared.Common;

namespace Colinapp.Shared.Exceptions;

/// <summary>
/// 业务异常。在业务逻辑中主动抛出，由全局异常中间件捕获并转换为统一返回结构。
/// </summary>
public class BusinessException : Exception
{
    public int Code { get; }

    public BusinessException(string message, int code = ResultCode.Error) : base(message)
    {
        Code = code;
    }

    public static BusinessException NotFound(string message = "资源不存在")
        => new(message, ResultCode.NotFound);

    public static BusinessException Forbidden(string message = "无访问权限")
        => new(message, ResultCode.Forbidden);
}
