using Colinapp.Application.Common;
using Colinapp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Colinapp.Infrastructure.Scheduling.Jobs;

/// <summary>
/// 审批超时提醒任务：扫描已超过时限（DueTime）且未提醒的待办审批任务，
/// 记录提醒日志并打标记（Reminded）避免重复。接入消息中心/邮件时在此处扩展发送渠道。
/// </summary>
[DisallowConcurrentExecution]
public class WorkflowTimeoutRemindJob(IAppDbContext db, ILogger<WorkflowTimeoutRemindJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var now = DateTime.Now;
        var overdue = await db.WorkflowTasks
            .Where(x => x.Status == WorkflowTaskStatus.Pending && !x.Reminded
                        && x.DueTime != null && x.DueTime < now)
            .ToListAsync(context.CancellationToken);

        foreach (var task in overdue)
        {
            // TODO: 接入站内信/邮件后在此发送给 task.ApproverId
            logger.LogWarning("审批超时提醒：任务 {TaskId}（节点「{Node}」，审批人 {Approver}）已超过时限 {Due}",
                task.Id, task.NodeName, task.ApproverName, task.DueTime);
            task.Reminded = true;
        }

        if (overdue.Count > 0)
            await db.SaveChangesAsync(context.CancellationToken);

        context.Result = $"超时提醒 {overdue.Count} 条";
    }
}
