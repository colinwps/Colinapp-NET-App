namespace Colinapp.Domain.Enums;

/// <summary>节点审批人指定方式</summary>
public enum WorkflowApproverType
{
    /// <summary>指定用户</summary>
    Users = 1,

    /// <summary>指定角色（发起时解析为该角色下的所有启用用户）</summary>
    Role = 2,
}
