using Colinapp.Domain.Enums;

namespace Colinapp.Domain.Entities.Workflow;

/// <summary>
/// 流程实例：一次具体的审批申请。持有发起时的节点快照，按 <see cref="CurrentNodeIndex"/> 逐节点推进。
/// </summary>
public class WorkflowInstance : EntityBase
{
    /// <summary>所属流程定义 Id</summary>
    public long DefinitionId { get; set; }

    /// <summary>流程名称（发起时冗余，防定义改名/删除后丢失）</summary>
    public string DefinitionName { get; set; } = string.Empty;

    /// <summary>申请标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>申请内容（表单数据 JSON 对象；旧数据可能是纯文本）</summary>
    public string FormData { get; set; } = string.Empty;

    /// <summary>流程图快照 JSON（发起时从定义复制）</summary>
    public string GraphJson { get; set; } = "{}";

    /// <summary>表单字段定义快照 JSON（发起时从定义复制，供详情回显）</summary>
    public string FormFieldsJson { get; set; } = "[]";

    /// <summary>当前停留的审批节点 Id（图中节点）</summary>
    public string CurrentNodeId { get; set; } = string.Empty;

    /// <summary>实例状态</summary>
    public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.Running;

    /// <summary>发起人 Id</summary>
    public long InitiatorId { get; set; }

    /// <summary>发起人姓名（冗余）</summary>
    public string InitiatorName { get; set; } = string.Empty;

    /// <summary>结束时间（通过/驳回/撤销）</summary>
    public DateTime? FinishTime { get; set; }
}
