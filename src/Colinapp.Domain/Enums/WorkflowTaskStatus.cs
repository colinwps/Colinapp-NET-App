namespace Colinapp.Domain.Enums;

/// <summary>审批任务状态</summary>
public enum WorkflowTaskStatus
{
    /// <summary>待审批</summary>
    Pending = 1,

    /// <summary>已通过</summary>
    Approved = 2,

    /// <summary>已驳回</summary>
    Rejected = 3,

    /// <summary>已终止（或签被他人处理 / 流程被驳回或撤销时未处理的任务）</summary>
    Terminated = 4,
}
