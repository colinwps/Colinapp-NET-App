namespace Colinapp.Domain.Entities.Forms;

/// <summary>
/// 表单提交记录。SchemaJson/DataJson 为提交时的快照，表单定义后续修改不影响记录渲染。
/// 绑定流程的表单提交会同时发起流程实例并记下 <see cref="WorkflowInstanceId"/>；
/// 记录定位为"初始提交"的审计——实例退回重提只更新实例 FormData，本记录不同步（实例为权威）。
/// </summary>
public class FormEntry : EntityBase
{
    /// <summary>表单定义 Id</summary>
    public long FormDefinitionId { get; set; }

    /// <summary>表单名称（快照）</summary>
    public string FormName { get; set; } = string.Empty;

    /// <summary>提交标题（默认为表单名，绑流程时作实例标题）</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>字段 schema 快照 JSON</summary>
    public string SchemaJson { get; set; } = "[]";

    /// <summary>提交数据 JSON（对象，key 对应字段 key）</summary>
    public string DataJson { get; set; } = "{}";

    /// <summary>发起的流程实例 Id（表单绑定流程时才有）</summary>
    public long? WorkflowInstanceId { get; set; }

    /// <summary>提交人 Id</summary>
    public long SubmitterId { get; set; }

    /// <summary>提交人姓名（快照）</summary>
    public string SubmitterName { get; set; } = string.Empty;
}
