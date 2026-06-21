using Colinapp.Application.Common;
using Colinapp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Colinapp.Infrastructure.Persistence.Interceptors;

/// <summary>
/// 审计与软删除拦截器：
/// - 新增时填充 CreatedBy/CreatedTime/TenantId；
/// - 修改时填充 UpdatedBy/UpdatedTime；
/// - 删除转为软删除（IsDeleted = true）。
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.Now;
        var userId = currentUser.UserId;

        foreach (EntityEntry<EntityBase> entry in context.ChangeTracker.Entries<EntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedTime = now;
                    entry.Entity.CreatedBy ??= userId;
                    entry.Entity.TenantId ??= currentUser.TenantId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedTime = now;
                    entry.Entity.UpdatedBy = userId;
                    break;

                case EntityState.Deleted:
                    // 软删除：拦截物理删除，改为更新 IsDeleted 标记
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedTime = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
    }
}
