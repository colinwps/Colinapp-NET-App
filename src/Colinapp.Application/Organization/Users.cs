using Colinapp.Application.Common;
using Colinapp.Application.Permissions;
using Colinapp.Domain.Entities.System;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Organization;

// ---------- DTO ----------

public class UserQuery : PagedRequest
{
    public long? DeptId { get; set; }
    public bool? Enabled { get; set; }
}

public class UserListItemDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public long? DeptId { get; set; }
    public string? DeptName { get; set; }
    public bool Enabled { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class UserDetailDto : UserListItemDto
{
    public List<long> RoleIds { get; set; } = [];
    public List<long> PostIds { get; set; } = [];
}

public class UserSaveDto
{
    public string UserName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    /// <summary>明文密码。新增时必填；编辑时留空表示不修改。</summary>
    public string? Password { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public long? DeptId { get; set; }
    public bool Enabled { get; set; } = true;
    public List<long> RoleIds { get; set; } = [];
    public List<long> PostIds { get; set; } = [];
}

// ---------- Service ----------

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> GetPagedAsync(UserQuery query, CancellationToken ct = default);
    Task<UserDetailDto> GetAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(UserSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, UserSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task ResetPasswordAsync(long id, string newPassword, CancellationToken ct = default);
    Task ChangeStatusAsync(long id, bool enabled, CancellationToken ct = default);
}

public class UserService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IDataScopeService dataScopeService,
    ICurrentUser currentUser) : IUserService
{
    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(UserQuery query, CancellationToken ct = default)
    {
        var q = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.UserName.Contains(query.Keyword) || x.NickName.Contains(query.Keyword));
        if (query.DeptId is { } deptId)
            q = q.Where(x => x.DeptId == deptId);
        if (query.Enabled is { } enabled)
            q = q.Where(x => x.Enabled == enabled);

        // ---- 数据权限过滤 ----
        var scope = await dataScopeService.ResolveAsync(ct);
        if (!scope.All)
        {
            var deptIds = scope.DeptIds.ToList();
            var selfOnly = scope.SelfOnly;
            var selfId = currentUser.UserId ?? 0;
            q = q.Where(x =>
                (x.DeptId != null && deptIds.Contains(x.DeptId.Value))
                || (selfOnly && x.Id == selfId));
        }

        var total = await q.CountAsync(ct);

        var users = await q
            .OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var deptNames = await GetDeptNameMapAsync(users.Select(u => u.DeptId), ct);

        var items = users.Select(u => ToListItem(u, deptNames)).ToList();
        return new PagedResult<UserListItemDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task<UserDetailDto> GetAsync(long id, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("用户不存在");

        var deptNames = await GetDeptNameMapAsync([user.DeptId], ct);
        var dto = new UserDetailDto
        {
            Id = user.Id,
            UserName = user.UserName,
            NickName = user.NickName,
            Phone = user.Phone,
            Email = user.Email,
            DeptId = user.DeptId,
            DeptName = user.DeptId is { } d && deptNames.TryGetValue(d, out var n) ? n : null,
            Enabled = user.Enabled,
            IsAdmin = user.IsAdmin,
            LastLoginTime = user.LastLoginTime,
            CreatedTime = user.CreatedTime,
            RoleIds = await db.UserRoles.Where(x => x.UserId == id).Select(x => x.RoleId).ToListAsync(ct),
            PostIds = await db.UserPosts.Where(x => x.UserId == id).Select(x => x.PositionId).ToListAsync(ct),
        };
        return dto;
    }

    public async Task<long> CreateAsync(UserSaveDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new BusinessException("密码不能为空");
        if (await db.Users.AnyAsync(x => x.UserName == dto.UserName, ct))
            throw new BusinessException("登录账号已存在");

        var user = new User
        {
            UserName = dto.UserName,
            NickName = dto.NickName,
            PasswordHash = passwordHasher.Hash(dto.Password!),
            Phone = dto.Phone,
            Email = dto.Email,
            DeptId = dto.DeptId,
            Enabled = dto.Enabled,
            IsAdmin = false,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        SyncRelations(user.Id, dto);
        await db.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task UpdateAsync(long id, UserSaveDto dto, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("用户不存在");

        if (await db.Users.AnyAsync(x => x.UserName == dto.UserName && x.Id != id, ct))
            throw new BusinessException("登录账号已存在");

        user.NickName = dto.NickName;
        user.Phone = dto.Phone;
        user.Email = dto.Email;
        user.DeptId = dto.DeptId;
        user.Enabled = dto.Enabled;
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = passwordHasher.Hash(dto.Password);

        var oldRoles = await db.UserRoles.Where(x => x.UserId == id).ToListAsync(ct);
        db.UserRoles.RemoveRange(oldRoles);
        var oldPosts = await db.UserPosts.Where(x => x.UserId == id).ToListAsync(ct);
        db.UserPosts.RemoveRange(oldPosts);

        SyncRelations(id, dto);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("用户不存在");
        if (user.IsAdmin)
            throw new BusinessException("超级管理员不能删除");

        var roles = await db.UserRoles.Where(x => x.UserId == id).ToListAsync(ct);
        db.UserRoles.RemoveRange(roles);
        var posts = await db.UserPosts.Where(x => x.UserId == id).ToListAsync(ct);
        db.UserPosts.RemoveRange(posts);
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(long id, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new BusinessException("密码不能为空");
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("用户不存在");
        user.PasswordHash = passwordHasher.Hash(newPassword);
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangeStatusAsync(long id, bool enabled, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("用户不存在");
        if (user.IsAdmin && !enabled)
            throw new BusinessException("超级管理员不能停用");
        user.Enabled = enabled;
        await db.SaveChangesAsync(ct);
    }

    private void SyncRelations(long userId, UserSaveDto dto)
    {
        foreach (var roleId in dto.RoleIds.Distinct())
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        foreach (var postId in dto.PostIds.Distinct())
            db.UserPosts.Add(new UserPost { UserId = userId, PositionId = postId });
    }

    private async Task<Dictionary<long, string>> GetDeptNameMapAsync(IEnumerable<long?> deptIds, CancellationToken ct)
    {
        var ids = deptIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];
        return await db.Departments
            .Where(d => ids.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);
    }

    private static UserListItemDto ToListItem(User u, Dictionary<long, string> deptNames) => new()
    {
        Id = u.Id,
        UserName = u.UserName,
        NickName = u.NickName,
        Phone = u.Phone,
        Email = u.Email,
        DeptId = u.DeptId,
        DeptName = u.DeptId is { } d && deptNames.TryGetValue(d, out var n) ? n : null,
        Enabled = u.Enabled,
        IsAdmin = u.IsAdmin,
        LastLoginTime = u.LastLoginTime,
        CreatedTime = u.CreatedTime,
    };
}
