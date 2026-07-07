namespace Colinapp.Domain.Enums;

/// <summary>节点审批方式</summary>
public enum WorkflowApproveMode
{
    /// <summary>或签：任一审批人通过即进入下一节点</summary>
    Any = 1,

    /// <summary>会签：所有审批人通过才进入下一节点</summary>
    All = 2,
}
