namespace Colinapp.Domain.Entities.Workflow;

/// <summary>
/// 流程定义：有向图驱动的审批流。流程图（节点+边）存于 <see cref="GraphJson"/>，
/// 表单字段定义存于 <see cref="FormFieldsJson"/>；发起实例时两者都会快照到实例上，
/// 因此修改定义不影响在途流程。Schema 见 docs/工作流设计器规划.md。
/// </summary>
public class WorkflowDefinition : EntityBase
{
    /// <summary>流程编码（唯一）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>流程名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>是否启用（停用后不可发起新实例）</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>流程图 JSON（WorkflowGraph：nodes + edges）</summary>
    public string GraphJson { get; set; } = "{}";

    /// <summary>表单字段定义 JSON（WorkflowFormField 数组），条件分支据此求值</summary>
    public string FormFieldsJson { get; set; } = "[]";

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
