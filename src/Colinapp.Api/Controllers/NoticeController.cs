using Colinapp.Api.Authorization;
using Colinapp.Application.Business;
using Colinapp.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

/// <summary>
/// 公告管理（业务扩展样例）。鉴权方式与平台模块完全一致：[HasPermission] + 数据范围（如需）。
/// </summary>
[ApiController]
[Authorize]
[Route("api/notice")]
public class NoticeController(INoticeService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("biz:notice:list")]
    public async Task<ApiResult> List([FromQuery] NoticeQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission("biz:notice:query")]
    public async Task<ApiResult> Get(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetAsync(id, ct));

    [HttpPost]
    [HasPermission("biz:notice:add")]
    public async Task<ApiResult> Create([FromBody] NoticeSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [HasPermission("biz:notice:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] NoticeSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("{id:long}")]
    [HasPermission("biz:notice:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }
}
