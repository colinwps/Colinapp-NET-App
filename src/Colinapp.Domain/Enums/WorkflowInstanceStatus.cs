namespace Colinapp.Domain.Enums;

/// <summary>流程实例状态</summary>
public enum WorkflowInstanceStatus
{
    /// <summary>审批中</summary>
    Running = 1,

    /// <summary>已通过</summary>
    Approved = 2,

    /// <summary>已驳回</summary>
    Rejected = 3,

    /// <summary>已撤销</summary>
    Canceled = 4,

    /// <summary>已退回（驳回策略=退回发起人），发起人可修改后重新提交</summary>
    Returned = 5,
}
