namespace Colinapp.Api.Filters;

/// <summary>为接口指定操作日志标题，覆盖默认的「控制器/方法」。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OperationLogAttribute(string title) : Attribute
{
    public string Title { get; } = title;
}

/// <summary>标注此接口不记录操作日志。</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SkipOperationLogAttribute : Attribute;
