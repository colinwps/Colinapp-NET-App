using Colinapp.Application.Common;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Colinapp.Infrastructure.Scheduling.Jobs;

/// <summary>
/// 过期令牌清理任务：物理删除已过期或已撤销的刷新令牌，避免 sys_refresh_token 无限膨胀。
/// </summary>
[DisallowConcurrentExecution]
public class RefreshTokenCleanupJob(IAppDbContext db) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var now = DateTime.UtcNow; // 刷新令牌时间统一用 UTC（见 AuthService）
        var removed = await db.RefreshTokens
            .Where(x => x.ExpiresAt < now || x.RevokedAt != null)
            .ExecuteDeleteAsync(context.CancellationToken);

        context.Result = $"清理失效刷新令牌 {removed} 条";
    }
}
