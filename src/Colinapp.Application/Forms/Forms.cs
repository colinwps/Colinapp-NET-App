using System.Text.Json;
using Colinapp.Application.Common;
using Colinapp.Application.Permissions;
using Colinapp.Application.Workflow;
using Colinapp.Domain.Entities.Forms;
using Colinapp.Domain.Enums;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Forms;

// ---------- DTO ----------

public class FormDefinitionQuery : PagedRequest
{
    public FormDefinitionStatus? Status { get; set; }
}

public class FormDefinitionDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public FormDefinitionStatus Status { get; set; }
    public long? WorkflowDefinitionId { get; set; }
    public string? WorkflowName { get; set; }
    public List<FormField> Fields { get; set; } = [];
    public DateTime CreatedTime { get; set; }
}

public class FormDefinitionSaveDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public long? WorkflowDefinitionId { get; set; }
    public List<FormField> Fields { get; set; } = [];
}

public class FormStatusDto
{
    public FormDefinitionStatus Status { get; set; }
}

/// <summary>申请中心卡片（仅已发布表单）</summary>
public class PublishedFormDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public long? WorkflowDefinitionId { get; set; }
    public string? WorkflowName { get; set; }
    public List<FormField> Fields { get; set; } = [];
}

public class FormSubmitDto
{
    /// <summary>提交标题，空则用表单名；绑流程时作实例标题</summary>
    public string? Title { get; set; }

    /// <summary>表单数据（JSON 对象字符串）</summary>
    public string DataJson { get; set; } = "{}";
}

public class FormSubmitResultDto
{
    public long EntryId { get; set; }

    /// <summary>发起的流程实例 Id（表单绑定流程时才有）</summary>
    public long? InstanceId { get; set; }
}

public class FormEntryQuery : PagedRequest
{
    public long? FormDefinitionId { get; set; }
}

public class FormEntryDto
{
    public long Id { get; set; }
    public long FormDefinitionId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SubmitterName { get; set; } = string.Empty;
    public long? WorkflowInstanceId { get; set; }

    /// <summary>关联流程实例的当前状态（未绑流程为 null）</summary>
    public WorkflowInstanceStatus? InstanceStatus { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class FormEntryDetailDto : FormEntryDto
{
    public string DataJson { get; set; } = "{}";
    public List<FormField> Fields { get; set; } = [];
}

// ---------- Service ----------

public interface IFormService
{
    // 表单定义
    Task<PagedResult<FormDefinitionDto>> GetPagedAsync(FormDefinitionQuery query, CancellationToken ct = default);
    Task<FormDefinitionDto> GetAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(FormDefinitionSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, FormDefinitionSaveDto dto, CancellationToken ct = default);
    Task SetStatusAsync(long id, FormStatusDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);

    // 填报
    Task<List<PublishedFormDto>> GetPublishedAsync(CancellationToken ct = default);
    Task<FormSubmitResultDto> SubmitAsync(long formId, FormSubmitDto dto, CancellationToken ct = default);

    // 提交记录
    Task<PagedResult<FormEntryDto>> GetEntriesAsync(FormEntryQuery query, CancellationToken ct = default);
    Task<PagedResult<FormEntryDto>> GetMyEntriesAsync(FormEntryQuery query, CancellationToken ct = default);
    Task<FormEntryDetailDto> GetEntryAsync(long id, CancellationToken ct = default);
}

public class FormService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IWorkflowService workflowService,
    IPermissionService permissionService) : IFormService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // ===== 表单定义 =====

    public async Task<PagedResult<FormDefinitionDto>> GetPagedAsync(FormDefinitionQuery query, CancellationToken ct = default)
    {
        var q = db.FormDefinitions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Name.Contains(query.Keyword) || x.Code.Contains(query.Keyword));
        if (query.Status is { } s)
            q = q.Where(x => x.Status == s);

        var total = await q.CountAsync(ct);
        var entities = await q.OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);

        var wfNames = await LoadWorkflowNamesAsync(entities.Select(x => x.WorkflowDefinitionId), ct);
        var items = entities.Select(x => ToDto(x, wfNames)).ToList();
        return new PagedResult<FormDefinitionDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task<FormDefinitionDto> GetAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.FormDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("表单不存在");
        var wfNames = await LoadWorkflowNamesAsync([entity.WorkflowDefinitionId], ct);
        return ToDto(entity, wfNames);
    }

    public async Task<long> CreateAsync(FormDefinitionSaveDto dto, CancellationToken ct = default)
    {
        await ValidateSaveAsync(dto, null, ct);
        var entity = Apply(new FormDefinition(), dto);
        db.FormDefinitions.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(long id, FormDefinitionSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.FormDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("表单不存在");
        await ValidateSaveAsync(dto, id, ct);
        Apply(entity, dto);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetStatusAsync(long id, FormStatusDto dto, CancellationToken ct = default)
    {
        var entity = await db.FormDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("表单不存在");

        if (dto.Status == FormDefinitionStatus.Published)
        {
            // 发布前重跑完整校验（防绑定的流程在草稿期间被停用/修改）
            var fields = ParseFields(entity.SchemaJson);
            var errors = FormFieldTypes.ValidateFields(fields);
            if (errors.Count > 0) throw new BusinessException(string.Join("；", errors));
            await ValidateWorkflowBindingAsync(entity.WorkflowDefinitionId, fields, ct);
        }

        entity.Status = dto.Status;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.FormDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("表单不存在");
        if (entity.Status == FormDefinitionStatus.Published)
            throw new BusinessException("已发布的表单不能删除，请先停用");
        db.FormDefinitions.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    // ===== 填报 =====

    public async Task<List<PublishedFormDto>> GetPublishedAsync(CancellationToken ct = default)
    {
        var entities = await db.FormDefinitions
            .Where(x => x.Status == FormDefinitionStatus.Published)
            .OrderBy(x => x.Id).ToListAsync(ct);
        var wfNames = await LoadWorkflowNamesAsync(entities.Select(x => x.WorkflowDefinitionId), ct);
        return entities.Select(x => new PublishedFormDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Icon = x.Icon,
            WorkflowDefinitionId = x.WorkflowDefinitionId,
            WorkflowName = x.WorkflowDefinitionId is { } wid ? wfNames.GetValueOrDefault(wid) : null,
            Fields = ParseFields(x.SchemaJson),
        }).ToList();
    }

    public async Task<FormSubmitResultDto> SubmitAsync(long formId, FormSubmitDto dto, CancellationToken ct = default)
    {
        var form = await db.FormDefinitions.FirstOrDefaultAsync(x => x.Id == formId, ct)
            ?? throw BusinessException.NotFound("表单不存在");
        if (form.Status != FormDefinitionStatus.Published)
            throw new BusinessException("该表单未发布，不能填报");

        var fields = ParseFields(form.SchemaJson);
        var errors = FormFieldTypes.ValidateData(fields, dto.DataJson);
        if (errors.Count > 0) throw new BusinessException(string.Join("；", errors));

        var uid = currentUser.UserId ?? throw BusinessException.Forbidden("未登录");
        var submitterName = await db.Users.Where(u => u.Id == uid)
            .Select(u => u.NickName).FirstOrDefaultAsync(ct) ?? currentUser.UserName ?? string.Empty;
        var title = string.IsNullOrWhiteSpace(dto.Title) ? form.Name : dto.Title.Trim();

        long? instanceId = null;
        if (form.WorkflowDefinitionId is { } wfId)
        {
            var wfEnabled = await db.WorkflowDefinitions
                .Where(x => x.Id == wfId).Select(x => (bool?)x.Enabled).FirstOrDefaultAsync(ct);
            if (wfEnabled is null) throw new BusinessException("该表单绑定的流程已被删除，请联系管理员");
            if (wfEnabled is false) throw new BusinessException("该表单绑定的流程已停用，请联系管理员");

            // 用表单 schema 覆盖实例快照的 FormFieldsJson，条件求值与审批详情按表单字段解析
            instanceId = await workflowService.SubmitAsync(new WorkflowSubmitDto
            {
                DefinitionId = wfId,
                Title = title,
                FormData = dto.DataJson,
            }, form.SchemaJson, ct);
        }

        var entry = new FormEntry
        {
            FormDefinitionId = form.Id,
            FormName = form.Name,
            Title = title,
            SchemaJson = form.SchemaJson,
            DataJson = dto.DataJson,
            WorkflowInstanceId = instanceId,
            SubmitterId = uid,
            SubmitterName = submitterName,
        };
        db.FormEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return new FormSubmitResultDto { EntryId = entry.Id, InstanceId = instanceId };
    }

    // ===== 提交记录 =====

    public Task<PagedResult<FormEntryDto>> GetEntriesAsync(FormEntryQuery query, CancellationToken ct = default)
        => QueryEntriesAsync(query, null, ct);

    public Task<PagedResult<FormEntryDto>> GetMyEntriesAsync(FormEntryQuery query, CancellationToken ct = default)
    {
        var uid = currentUser.UserId ?? throw BusinessException.Forbidden("未登录");
        return QueryEntriesAsync(query, uid, ct);
    }

    public async Task<FormEntryDetailDto> GetEntryAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.FormEntries.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("提交记录不存在");

        var uid = currentUser.UserId ?? throw BusinessException.Forbidden("未登录");
        if (entity.SubmitterId != uid && !currentUser.IsAdmin)
        {
            var perms = await permissionService.GetUserPermissionsAsync(uid, ct);
            if (!perms.Contains("form:entry:list"))
                throw BusinessException.Forbidden("无权查看该提交记录");
        }

        var status = await LoadInstanceStatusesAsync([entity.WorkflowInstanceId], ct);
        return new FormEntryDetailDto
        {
            Id = entity.Id,
            FormDefinitionId = entity.FormDefinitionId,
            FormName = entity.FormName,
            Title = entity.Title,
            SubmitterName = entity.SubmitterName,
            WorkflowInstanceId = entity.WorkflowInstanceId,
            InstanceStatus = entity.WorkflowInstanceId is { } iid ? status.GetValueOrDefault(iid) : null,
            CreatedTime = entity.CreatedTime,
            DataJson = entity.DataJson,
            Fields = ParseFields(entity.SchemaJson),
        };
    }

    private async Task<PagedResult<FormEntryDto>> QueryEntriesAsync(FormEntryQuery query, long? submitterId, CancellationToken ct)
    {
        var q = db.FormEntries.AsQueryable();
        if (submitterId is { } sid)
            q = q.Where(x => x.SubmitterId == sid);
        if (query.FormDefinitionId is { } fid)
            q = q.Where(x => x.FormDefinitionId == fid);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Title.Contains(query.Keyword) || x.FormName.Contains(query.Keyword));

        var total = await q.CountAsync(ct);
        var entities = await q.OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);

        var statuses = await LoadInstanceStatusesAsync(entities.Select(x => x.WorkflowInstanceId), ct);
        var items = entities.Select(x => new FormEntryDto
        {
            Id = x.Id,
            FormDefinitionId = x.FormDefinitionId,
            FormName = x.FormName,
            Title = x.Title,
            SubmitterName = x.SubmitterName,
            WorkflowInstanceId = x.WorkflowInstanceId,
            InstanceStatus = x.WorkflowInstanceId is { } iid ? statuses.GetValueOrDefault(iid) : null,
            CreatedTime = x.CreatedTime,
        }).ToList();
        return new PagedResult<FormEntryDto>(items, total, query.PageIndex, query.PageSize);
    }

    // ===== 私有 =====

    private async Task ValidateSaveAsync(FormDefinitionSaveDto dto, long? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Code)) throw new BusinessException("表单编码不能为空");
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new BusinessException("表单名称不能为空");

        var code = dto.Code.Trim();
        if (await db.FormDefinitions.AnyAsync(x => x.Code == code && x.Id != excludeId, ct))
            throw new BusinessException($"表单编码「{code}」已存在");

        var errors = FormFieldTypes.ValidateFields(dto.Fields);
        if (errors.Count > 0) throw new BusinessException(string.Join("；", errors));

        await ValidateWorkflowBindingAsync(dto.WorkflowDefinitionId, dto.Fields, ct);
    }

    /// <summary>绑定流程时校验：流程存在且启用，且流程条件边引用的字段都在表单中。</summary>
    private async Task ValidateWorkflowBindingAsync(long? workflowDefinitionId, List<FormField> fields, CancellationToken ct)
    {
        if (workflowDefinitionId is not { } wfId) return;

        var wf = await db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == wfId, ct)
            ?? throw new BusinessException("绑定的流程不存在");
        if (!wf.Enabled) throw new BusinessException($"绑定的流程「{wf.Name}」已停用");

        var graph = JsonSerializer.Deserialize<WorkflowGraph>(wf.GraphJson, JsonOptions) ?? new WorkflowGraph();
        var inputFields = fields.Where(f => FormFieldTypes.IsInput(f.Type))
            .Cast<WorkflowFormField>().ToList();
        var errors = graph.Validate(inputFields);
        if (errors.Count > 0)
            throw new BusinessException($"与流程「{wf.Name}」不匹配：{string.Join("；", errors)}");
    }

    private async Task<Dictionary<long, string>> LoadWorkflowNamesAsync(IEnumerable<long?> ids, CancellationToken ct)
    {
        var wanted = ids.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (wanted.Count == 0) return [];
        return await db.WorkflowDefinitions.Where(x => wanted.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
    }

    private async Task<Dictionary<long, WorkflowInstanceStatus?>> LoadInstanceStatusesAsync(IEnumerable<long?> ids, CancellationToken ct)
    {
        var wanted = ids.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (wanted.Count == 0) return [];
        return await db.WorkflowInstances.Where(x => wanted.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => (WorkflowInstanceStatus?)x.Status, ct);
    }

    private static FormDefinition Apply(FormDefinition e, FormDefinitionSaveDto dto)
    {
        e.Code = dto.Code.Trim();
        e.Name = dto.Name.Trim();
        e.Description = dto.Description;
        e.Icon = dto.Icon;
        e.WorkflowDefinitionId = dto.WorkflowDefinitionId;
        e.SchemaJson = JsonSerializer.Serialize(dto.Fields, JsonOptions);
        return e;
    }

    private static FormDefinitionDto ToDto(FormDefinition x, Dictionary<long, string> wfNames) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        Icon = x.Icon,
        Status = x.Status,
        WorkflowDefinitionId = x.WorkflowDefinitionId,
        WorkflowName = x.WorkflowDefinitionId is { } wid ? wfNames.GetValueOrDefault(wid) : null,
        Fields = ParseFields(x.SchemaJson),
        CreatedTime = x.CreatedTime,
    };

    private static List<FormField> ParseFields(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<FormField>>(json, JsonOptions) ?? []; }
        catch (JsonException) { return []; }
    }
}
