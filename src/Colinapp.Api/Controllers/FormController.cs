using Colinapp.Api.Authorization;
using Colinapp.Application.Forms;
using Colinapp.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

/// <summary>
/// 表单中心：表单定义（设计器）、申请中心填报、提交记录。
/// 绑定流程的表单提交会发起流程实例（见 FormService.SubmitAsync）。
/// </summary>
[ApiController]
[Authorize]
[Route("api/form")]
public class FormController(IFormService service) : ControllerBase
{
    // ===== 表单定义 =====

    [HttpGet("definition")]
    [HasPermission("form:def:list")]
    public async Task<ApiResult> List([FromQuery] FormDefinitionQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetPagedAsync(query, ct));

    [HttpGet("definition/{id:long}")]
    [HasPermission("form:def:query")]
    public async Task<ApiResult> Get(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetAsync(id, ct));

    [HttpPost("definition")]
    [HasPermission("form:def:add")]
    public async Task<ApiResult> Create([FromBody] FormDefinitionSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("definition/{id:long}")]
    [HasPermission("form:def:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] FormDefinitionSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpPut("definition/{id:long}/status")]
    [HasPermission("form:def:edit")]
    public async Task<ApiResult> SetStatus(long id, [FromBody] FormStatusDto dto, CancellationToken ct)
    {
        await service.SetStatusAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("definition/{id:long}")]
    [HasPermission("form:def:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }

    // ===== 申请中心 =====

    [HttpGet("published")]
    [HasPermission("form:apply:list")]
    public async Task<ApiResult> Published(CancellationToken ct)
        => ApiResult.Ok(await service.GetPublishedAsync(ct));

    [HttpPost("{id:long}/submit")]
    [HasPermission("form:apply:submit")]
    public async Task<ApiResult> Submit(long id, [FromBody] FormSubmitDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.SubmitAsync(id, dto, ct));

    [HttpGet("entry/my")]
    [HasPermission("form:apply:list")]
    public async Task<ApiResult> MyEntries([FromQuery] FormEntryQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetMyEntriesAsync(query, ct));

    // ===== 提交记录（管理端） =====

    [HttpGet("entry")]
    [HasPermission("form:entry:list")]
    public async Task<ApiResult> Entries([FromQuery] FormEntryQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetEntriesAsync(query, ct));

    /// <summary>记录详情：提交人本人 / 管理员 / 持有 form:entry:list 者（服务内校验）。</summary>
    [HttpGet("entry/{id:long}")]
    public async Task<ApiResult> Entry(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetEntryAsync(id, ct));
}
