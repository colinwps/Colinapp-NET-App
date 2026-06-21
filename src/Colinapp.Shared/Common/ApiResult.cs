namespace Colinapp.Shared.Common;

/// <summary>
/// 统一 API 返回结构。所有接口均返回该结构，前端按 Code 判断业务结果。
/// </summary>
public class ApiResult
{
    public int Code { get; set; }

    public string Message { get; set; } = string.Empty;

    public object? Data { get; set; }

    public bool Success => Code == ResultCode.Success;

    public static ApiResult Ok(object? data = null, string message = "操作成功")
        => new() { Code = ResultCode.Success, Message = message, Data = data };

    public static ApiResult Fail(string message, int code = ResultCode.Error)
        => new() { Code = code, Message = message };
}

/// <summary>
/// 泛型返回结构，便于 Swagger 展示数据类型。
/// </summary>
public class ApiResult<T> : ApiResult
{
    public new T? Data { get; set; }

    public static ApiResult<T> Ok(T data, string message = "操作成功")
        => new() { Code = ResultCode.Success, Message = message, Data = data };
}
