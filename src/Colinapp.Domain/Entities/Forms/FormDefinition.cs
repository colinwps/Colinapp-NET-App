using Colinapp.Domain.Enums;

namespace Colinapp.Domain.Entities.Forms;

/// <summary>
/// 表单定义：设计器产出的字段 schema 存于 <see cref="SchemaJson"/>（FormField 数组，
/// 是工作流 WorkflowFormField 的超集）。可绑定一个流程定义——填报提交时发起流程实例，
/// 并把本表单的 schema 快照进实例的 FormFieldsJson；未绑定则仅存为表单数据记录。
/// </summary>
public class FormDefinition : EntityBase
{
    /// <summary>表单编码（租户内唯一）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>表单名称（如"报销申请"）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>描述（申请中心卡片展示）</summary>
    public string? Description { get; set; }

    /// <summary>Element Plus 图标名</summary>
    public string? Icon { get; set; }

    /// <summary>字段 schema JSON（FormField 数组）</summary>
    public string SchemaJson { get; set; } = "[]";

    /// <summary>绑定的流程定义 Id，可空（不走审批，仅收集数据）</summary>
    public long? WorkflowDefinitionId { get; set; }

    /// <summary>状态</summary>
    public FormDefinitionStatus Status { get; set; } = FormDefinitionStatus.Draft;
}
