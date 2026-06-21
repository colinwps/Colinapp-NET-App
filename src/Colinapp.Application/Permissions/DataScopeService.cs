using Colinapp.Application.Common;
using Colinapp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Permissions;

/// <summary>
/// 数据范围解析实现。基于当前用户的角色集合，按 全部/自定义/本部门/本部门及下级/仅本人 取并集。
/// </summary>
public class DataScopeService(IAppDbContext db, ICurrentUser currentUser) : IDataScopeService
{
    public async Task<DataScopeResult> ResolveAsync(CancellationToken ct = default)
    {
        // 未登录视为无数据；管理员可见全部。
        if (currentUser.UserId is not { } userId)
            return new DataScopeResult { DeptIds = [] };

        if (currentUser.IsAdmin)
            return DataScopeResult.AllData;

        var roles = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where ur.UserId == userId && r.Enabled
            select new { r.Id, r.DataScope }).ToListAsync(ct);

        // 无角色 → 仅本人兜底，避免越权看到全部。
        if (roles.Count == 0)
            return new DataScopeResult { SelfOnly = true, DeptIds = [] };

        if (roles.Any(r => r.DataScope == DataScope.All))
            return DataScopeResult.AllData;

        var ownDeptId = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DeptId)
            .FirstOrDefaultAsync(ct);

        var deptIds = new HashSet<long>();
        var selfOnly = false;

        // 部门树（树小，整表加载在内存中计算子树，避免数据库 LIKE 的边界问题）。
        List<DeptNode>? deptTree = null;

        foreach (var role in roles)
        {
            switch (role.DataScope)
            {
                case DataScope.Self:
                    selfOnly = true;
                    break;

                case DataScope.Dept:
                    if (ownDeptId is { } d1) deptIds.Add(d1);
                    break;

                case DataScope.DeptAndChild:
                    if (ownDeptId is { } d2)
                    {
                        deptIds.Add(d2);
                        deptTree ??= await db.Departments
                            .Select(x => new DeptNode(x.Id, x.Ancestors))
                            .ToListAsync(ct);
                        var token = $",{d2},";
                        foreach (var dept in deptTree)
                            if ($",{dept.Ancestors},".Contains(token))
                                deptIds.Add(dept.Id);
                    }
                    break;

                case DataScope.Custom:
                    var custom = await db.RoleDepts
                        .Where(rd => rd.RoleId == role.Id)
                        .Select(rd => rd.DeptId)
                        .ToListAsync(ct);
                    foreach (var id in custom) deptIds.Add(id);
                    break;
            }
        }

        return new DataScopeResult { SelfOnly = selfOnly, DeptIds = deptIds };
    }

    private readonly record struct DeptNode(long Id, string Ancestors);
}
