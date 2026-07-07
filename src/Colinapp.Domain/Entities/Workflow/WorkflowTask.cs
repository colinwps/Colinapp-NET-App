using Colinapp.Domain.Enums;

namespace Colinapp.Domain.Entities.Workflow;

/// <summary>
/// 审批任务：实例推进到某节点时，为该节点每个审批人生成一条。
/// </summary>
public class WorkflowTask : EntityBase
{
    /// <summary>所属实例 Id</summary>
    public long InstanceId { get; set; }

    /// <summary>所在流程图节点 Id</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>节点名称（冗余）</summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>节点审批方式（冗余，便于列表展示）</summary>
    public WorkflowApproveMode ApproveMode { get; set; }

    /// <summary>审批人 Id</summary>
    public long ApproverId { get; set; }

    /// <summary>审批人姓名（冗余）</summary>
    public string ApproverName { get; set; } = string.Empty;

    /// <summary>任务状态</summary>
    public WorkflowTaskStatus Status { get; set; } = WorkflowTaskStatus.Pending;

    /// <summary>审批意见</summary>
    public string? Comment { get; set; }

    /// <summary>处理时间</summary>
    public DateTime? HandleTime { get; set; }

    /// <summary>超时时限（节点配置了超时小时数时 = 创建时间 + 时限；null 表示不限时）</summary>
    public DateTime? DueTime { get; set; }

    /// <summary>是否已发送超时提醒（由定时任务标记，避免重复提醒）</summary>
    public bool Reminded { get; set; }
}
