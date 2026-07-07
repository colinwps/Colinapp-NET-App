using Colinapp.Api.Authorization;
using Colinapp.Application.Workflow;
using Colinapp.Shared.Common;
using Colinapp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

/// <summary>
/// 审批工作流：流程定义管理、发起/撤销申请、待办/已办审批。
/// </summary>
[ApiController]
[Authorize]
[Route("api/workflow")]
public class WorkflowController(IWorkflowService service) : ControllerBase
{
    // ===== 流程定义 =====

    [HttpGet("definition")]
    [HasPermission("wf:def:list")]
    public async Task<ApiResult> ListDefinitions([FromQuery] WorkflowDefinitionQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetDefinitionsAsync(query, ct));

    /// <summary>发起申请时的可选流程（仅启用），无需定义管理权限。</summary>
    [HttpGet("definition/options")]
    [HasPermission("wf:instance:add")]
    public async Task<ApiResult> DefinitionOptions(CancellationToken ct)
        => ApiResult.Ok(await service.GetDefinitionOptionsAsync(ct));

    [HttpGet("definition/{id:long}")]
    [HasPermission("wf:def:query")]
    public async Task<ApiResult> GetDefinition(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetDefinitionAsync(id, ct));

    [HttpPost("definition")]
    [HasPermission("wf:def:add")]
    public async Task<ApiResult> CreateDefinition([FromBody] WorkflowDefinitionSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateDefinitionAsync(dto, ct));

    [HttpPut("definition/{id:long}")]
    [HasPermission("wf:def:edit")]
    public async Task<ApiResult> UpdateDefinition(long id, [FromBody] WorkflowDefinitionSaveDto dto, CancellationToken ct)
    {
        await service.UpdateDefinitionAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("definition/{id:long}")]
    [HasPermission("wf:def:remove")]
    public async Task<ApiResult> DeleteDefinition(long id, CancellationToken ct)
    {
        await service.DeleteDefinitionAsync(id, ct);
        return ApiResult.Ok();
    }

    // ===== 流程实例 =====

    [HttpPost("instance")]
    [HasPermission("wf:instance:add")]
    public async Task<ApiResult> Submit([FromBody] WorkflowSubmitDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.SubmitAsync(dto, ct));

    [HttpGet("instance/my")]
    [HasPermission("wf:instance:list")]
    public async Task<ApiResult> MyInstances([FromQuery] WorkflowInstanceQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetMyInstancesAsync(query, ct));

    /// <summary>详情对参与者开放（发起人/审批人/管理员），服务内校验。</summary>
    [HttpGet("instance/{id:long}")]
    public async Task<ApiResult> GetInstance(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetInstanceAsync(id, ct));

    [HttpPut("instance/{id:long}/cancel")]
    [HasPermission("wf:instance:cancel")]
    public async Task<ApiResult> Cancel(long id, CancellationToken ct)
    {
        await service.CancelAsync(id, ct);
        return ApiResult.Ok();
    }

    /// <summary>重新提交「已退回」的流程（可修改标题与表单数据）。</summary>
    [HttpPut("instance/{id:long}/resubmit")]
    [HasPermission("wf:instance:add")]
    public async Task<ApiResult> Resubmit(long id, [FromBody] WorkflowResubmitDto dto, CancellationToken ct)
    {
        await service.ResubmitAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    // ===== 抄送 =====

    [HttpGet("cc/my")]
    [HasPermission("wf:cc:list")]
    public async Task<ApiResult> MyCc([FromQuery] WorkflowCcQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetMyCcAsync(query, ct));

    [HttpPut("cc/{id:long}/read")]
    [HasPermission("wf:cc:list")]
    public async Task<ApiResult> MarkCcRead(long id, CancellationToken ct)
    {
        await service.MarkCcReadAsync(id, ct);
        return ApiResult.Ok();
    }

    // ===== 审批任务 =====

    [HttpGet("task/todo")]
    [HasPermission("wf:task:list")]
    public async Task<ApiResult> Todo([FromQuery] PagedRequest query, CancellationToken ct)
        => ApiResult.Ok(await service.GetTodoAsync(query, ct));

    [HttpGet("task/done")]
    [HasPermission("wf:task:list")]
    public async Task<ApiResult> Done([FromQuery] PagedRequest query, CancellationToken ct)
        => ApiResult.Ok(await service.GetDoneAsync(query, ct));

    [HttpPut("task/{id:long}/approve")]
    [HasPermission("wf:task:approve")]
    public async Task<ApiResult> Approve(long id, [FromBody] WorkflowApproveDto dto, CancellationToken ct)
    {
        await service.ApproveAsync(id, dto, ct);
        return ApiResult.Ok();
    }
}
