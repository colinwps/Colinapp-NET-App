namespace Colinapp.Domain.Entities.Workflow;

/// <summary>
/// 抄送记录：流程经过抄送节点时为每个抄送人生成一条，支撑「抄送我的」列表与已读标记。
/// </summary>
public class WorkflowCcRecord : EntityBase
{
    /// <summary>所属实例 Id</summary>
    public long InstanceId { get; set; }

    /// <summary>所在流程图节点 Id</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>节点名称（冗余）</summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>抄送人 Id</summary>
    public long UserId { get; set; }

    /// <summary>抄送人姓名（冗余）</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>已读时间</summary>
    public DateTime? ReadTime { get; set; }
}
