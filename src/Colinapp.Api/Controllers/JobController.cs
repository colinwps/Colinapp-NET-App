using Colinapp.Api.Authorization;
using Colinapp.Application.Scheduling;
using Colinapp.Domain.Enums;
using Colinapp.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

/// <summary>
/// 定时任务管理。基于 Quartz 调度，支持新增/编辑/暂停/恢复/立即执行与执行日志查询。
/// </summary>
[ApiController]
[Authorize]
[Route("api/job")]
public class JobController(IScheduledJobService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("monitor:job:list")]
    public async Task<ApiResult> List([FromQuery] ScheduledJobQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetPagedAsync(query, ct));

    /// <summary>可选调用目标（内置任务清单），供新增/编辑下拉。</summary>
    [HttpGet("targets")]
    [HasPermission("monitor:job:list")]
    public ApiResult Targets()
        => ApiResult.Ok(service.GetAvailableTargets());

    [HttpGet("{id:long}")]
    [HasPermission("monitor:job:query")]
    public async Task<ApiResult> Get(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetAsync(id, ct));

    [HttpPost]
    [HasPermission("monitor:job:add")]
    public async Task<ApiResult> Create([FromBody] ScheduledJobSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [HasPermission("monitor:job:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] ScheduledJobSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("{id:long}")]
    [HasPermission("monitor:job:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }

    /// <summary>切换状态：status=running 恢复调度，status=paused 暂停。</summary>
    [HttpPut("{id:long}/status")]
    [HasPermission("monitor:job:changeStatus")]
    public async Task<ApiResult> ChangeStatus(long id, [FromQuery] JobStatus status, CancellationToken ct)
    {
        await service.ChangeStatusAsync(id, status, ct);
        return ApiResult.Ok();
    }

    /// <summary>立即执行一次（不影响既有计划）。</summary>
    [HttpPost("{id:long}/run")]
    [HasPermission("monitor:job:run")]
    public async Task<ApiResult> RunOnce(long id, CancellationToken ct)
    {
        await service.RunOnceAsync(id, ct);
        return ApiResult.Ok();
    }

    [HttpGet("log")]
    [HasPermission("monitor:job:list")]
    public async Task<ApiResult> Logs([FromQuery] JobLogQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetLogsAsync(query, ct));

    [HttpDelete("log")]
    [HasPermission("monitor:job:remove")]
    public async Task<ApiResult> ClearLogs(CancellationToken ct)
    {
        await service.ClearLogsAsync(ct);
        return ApiResult.Ok();
    }
}
