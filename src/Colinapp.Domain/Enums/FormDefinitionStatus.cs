namespace Colinapp.Domain.Enums;

/// <summary>表单定义状态</summary>
public enum FormDefinitionStatus
{
    /// <summary>草稿（可编辑，不可填报）</summary>
    Draft = 1,

    /// <summary>已发布（申请中心可见、可填报）</summary>
    Published = 2,

    /// <summary>已停用（不可填报，历史记录不受影响）</summary>
    Disabled = 3,
}
