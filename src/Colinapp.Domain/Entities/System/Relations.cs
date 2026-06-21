namespace Colinapp.Domain.Entities.System;

/// <summary>用户-角色关联（多对多）。纯关联表，重新分配时整体覆盖。</summary>
public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
}

/// <summary>角色-菜单关联（多对多）。决定角色拥有的功能权限。</summary>
public class RoleMenu
{
    public long RoleId { get; set; }
    public long MenuId { get; set; }
}

/// <summary>用户-职位关联（多对多）。</summary>
public class UserPost
{
    public long UserId { get; set; }
    public long PositionId { get; set; }
}

/// <summary>角色-自定义部门关联。仅当角色 DataScope=Custom 时生效。</summary>
public class RoleDept
{
    public long RoleId { get; set; }
    public long DeptId { get; set; }
}
