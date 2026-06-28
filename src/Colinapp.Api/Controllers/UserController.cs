using Colinapp.Api.Authorization;
using Colinapp.Application.Organization;
using Colinapp.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/user")]
public class UserController(IUserService service) : ControllerBase
{
    /// <summary>用户分页（按当前用户数据范围过滤）</summary>
    [HttpGet]
    [HasPermission("sys:user:list")]
    public async Task<ApiResult> List([FromQuery] UserQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission("sys:user:query")]
    public async Task<ApiResult> Get(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetAsync(id, ct));

    [HttpPost]
    [HasPermission("sys:user:add")]
    public async Task<ApiResult> Create([FromBody] UserSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [HasPermission("sys:user:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] UserSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("{id:long}")]
    [HasPermission("sys:user:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }

    [HttpPut("{id:long}/password")]
    [HasPermission("sys:user:resetPwd")]
    public async Task<ApiResult> ResetPassword(long id, [FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        await service.ResetPasswordAsync(id, req.Password, ct);
        return ApiResult.Ok();
    }

    [HttpPut("{id:long}/status")]
    [HasPermission("sys:user:edit")]
    public async Task<ApiResult> ChangeStatus(long id, [FromBody] ChangeStatusRequest req, CancellationToken ct)
    {
        await service.ChangeStatusAsync(id, req.Enabled, ct);
        return ApiResult.Ok();
    }

    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>按当前查询条件与数据范围导出用户为 Excel。</summary>
    [HttpGet("export")]
    [HasPermission("sys:user:export")]
    public async Task<IActionResult> Export([FromQuery] UserQuery query, CancellationToken ct)
    {
        var bytes = await service.ExportAsync(query, ct);
        return File(bytes, XlsxContentType, $"用户_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    /// <summary>下载用户导入模板。</summary>
    [HttpGet("import-template")]
    [HasPermission("sys:user:import")]
    public IActionResult ImportTemplate()
        => File(service.ImportTemplate(), XlsxContentType, "用户导入模板.xlsx");

    /// <summary>导入用户。updateExisting=true 时同名账号将被更新，否则视为冲突跳过。</summary>
    [HttpPost("import")]
    [HasPermission("sys:user:import")]
    public async Task<ApiResult> Import(IFormFile file, [FromForm] bool updateExisting, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new Colinapp.Shared.Exceptions.BusinessException("请选择要导入的文件");
        await using var stream = file.OpenReadStream();
        return ApiResult.Ok(await service.ImportAsync(stream, updateExisting, ct));
    }

    public record ResetPasswordRequest(string Password);
    public record ChangeStatusRequest(bool Enabled);
}
