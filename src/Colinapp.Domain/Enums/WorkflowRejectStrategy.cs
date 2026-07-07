namespace Colinapp.Domain.Enums;

/// <summary>审批节点的驳回策略（节点属性，存于流程图 JSON）</summary>
public enum WorkflowRejectStrategy
{
    /// <summary>整体驳回：流程直接结束为已驳回（默认）</summary>
    RejectInstance = 1,

    /// <summary>退回上一审批节点重审；无上一节点或其审批人为空时退化为整体驳回</summary>
    BackToPrevious = 2,

    /// <summary>退回发起人：实例转「已退回」，发起人可修改表单后重新提交</summary>
    BackToInitiator = 3,
}
