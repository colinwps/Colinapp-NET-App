using System.Text.Json;
using Colinapp.Application.Common;
using Colinapp.Domain.Entities.Workflow;
using Colinapp.Domain.Enums;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Workflow;

// ---------- DTO ----------

public class WorkflowDefinitionQuery : PagedRequest
{
    public bool? Enabled { get; set; }
}

public class WorkflowDefinitionDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? Remark { get; set; }
    public WorkflowGraph Graph { get; set; } = new();
    public List<WorkflowFormField> FormFields { get; set; } = [];
    public DateTime CreatedTime { get; set; }
}

public class WorkflowDefinitionSaveDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? Remark { get; set; }
    public WorkflowGraph Graph { get; set; } = new();
    public List<WorkflowFormField> FormFields { get; set; } = [];
}

public class WorkflowSubmitDto
{
    public long DefinitionId { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>表单数据（JSON 对象字符串，key 对应表单字段；旧流程可为自由文本）</summary>
    public string FormData { get; set; } = string.Empty;
}

public class WorkflowInstanceQuery : PagedRequest
{
    public WorkflowInstanceStatus? Status { get; set; }
}

public class WorkflowInstanceDto
{
    public long Id { get; set; }
    public string DefinitionName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public WorkflowInstanceStatus Status { get; set; }
    public string CurrentNodeId { get; set; } = string.Empty;
    public string CurrentNodeName { get; set; } = string.Empty;
    public string InitiatorName { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime? FinishTime { get; set; }
}

public class WorkflowTaskItemDto
{
    public long Id { get; set; }
    public long InstanceId { get; set; }
    public string InstanceTitle { get; set; } = string.Empty;
    public string DefinitionName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public WorkflowApproveMode ApproveMode { get; set; }
    public string InitiatorName { get; set; } = string.Empty;
    public WorkflowTaskStatus Status { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? HandleTime { get; set; }
    public DateTime? DueTime { get; set; }
}

public class WorkflowResubmitDto
{
    public string? Title { get; set; }
    public string FormData { get; set; } = string.Empty;
}

public class WorkflowCcQuery : PagedRequest
{
    public bool? UnreadOnly { get; set; }
}

public class WorkflowCcItemDto
{
    public long Id { get; set; }
    public long InstanceId { get; set; }
    public string InstanceTitle { get; set; } = string.Empty;
    public string DefinitionName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string InitiatorName { get; set; } = string.Empty;
    public WorkflowInstanceStatus InstanceStatus { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? ReadTime { get; set; }
}

public class WorkflowTaskRecordDto
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;
    public WorkflowTaskStatus Status { get; set; }
    public string? Comment { get; set; }
    public DateTime? HandleTime { get; set; }
}

public class WorkflowInstanceDetailDto : WorkflowInstanceDto
{
    public string FormData { get; set; } = string.Empty;
    public WorkflowGraph Graph { get; set; } = new();
    public List<WorkflowFormField> FormFields { get; set; } = [];
    public List<WorkflowTaskRecordDto> Tasks { get; set; } = [];
}

public class WorkflowApproveDto
{
    public bool Approved { get; set; }
    public string? Comment { get; set; }
}

// ---------- Service ----------

public interface IWorkflowService
{
    // 流程定义
    Task<PagedResult<WorkflowDefinitionDto>> GetDefinitionsAsync(WorkflowDefinitionQuery query, CancellationToken ct = default);
    Task<WorkflowDefinitionDto> GetDefinitionAsync(long id, CancellationToken ct = default);
    Task<List<WorkflowDefinitionDto>> GetDefinitionOptionsAsync(CancellationToken ct = default);
    Task<long> CreateDefinitionAsync(WorkflowDefinitionSaveDto dto, CancellationToken ct = default);
    Task UpdateDefinitionAsync(long id, WorkflowDefinitionSaveDto dto, CancellationToken ct = default);
    Task DeleteDefinitionAsync(long id, CancellationToken ct = default);

    // 流程实例
    Task<long> SubmitAsync(WorkflowSubmitDto dto, CancellationToken ct = default);

    /// <summary>发起实例，可用 <paramref name="formFieldsJsonOverride"/> 覆盖实例快照的表单字段
    /// （表单中心绑定流程的表单提交时传入表单 schema，条件求值与详情展示按表单字段解析）。</summary>
    Task<long> SubmitAsync(WorkflowSubmitDto dto, string? formFieldsJsonOverride, CancellationToken ct = default);
    Task<PagedResult<WorkflowInstanceDto>> GetMyInstancesAsync(WorkflowInstanceQuery query, CancellationToken ct = default);
    Task<WorkflowInstanceDetailDto> GetInstanceAsync(long id, CancellationToken ct = default);
    Task CancelAsync(long id, CancellationToken ct = default);
    Task ResubmitAsync(long id, WorkflowResubmitDto dto, CancellationToken ct = default);

    // 抄送
    Task<PagedResult<WorkflowCcItemDto>> GetMyCcAsync(WorkflowCcQuery query, CancellationToken ct = default);
    Task MarkCcReadAsync(long id, CancellationToken ct = default);

    // 审批任务
    Task<PagedResult<WorkflowTaskItemDto>> GetTodoAsync(PagedRequest query, CancellationToken ct = default);
    Task<PagedResult<WorkflowTaskItemDto>> GetDoneAsync(PagedRequest query, CancellationToken ct = default);
    Task ApproveAsync(long taskId, WorkflowApproveDto dto, CancellationToken ct = default);
}

public class WorkflowService(IAppDbContext db, ICurrentUser currentUser) : IWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // ===== 流程定义 =====

    public async Task<PagedResult<WorkflowDefinitionDto>> GetDefinitionsAsync(WorkflowDefinitionQuery query, CancellationToken ct = default)
    {
        var q = db.WorkflowDefinitions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Name.Contains(query.Keyword) || x.Code.Contains(query.Keyword));
        if (query.Enabled is { } e)
            q = q.Where(x => x.Enabled == e);

        var total = await q.CountAsync(ct);
        var entities = await q.OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);

        return new PagedResult<WorkflowDefinitionDto>(entities.Select(ToDto).ToList(), total, query.PageIndex, query.PageSize);
    }

    public async Task<WorkflowDefinitionDto> GetDefinitionAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("流程定义不存在");
        return ToDto(entity);
    }

    public async Task<List<WorkflowDefinitionDto>> GetDefinitionOptionsAsync(CancellationToken ct = default)
    {
        var entities = await db.WorkflowDefinitions.Where(x => x.Enabled)
            .OrderBy(x => x.Id).ToListAsync(ct);
        return entities.Select(ToDto).ToList();
    }

    public async Task<long> CreateDefinitionAsync(WorkflowDefinitionSaveDto dto, CancellationToken ct = default)
    {
        await ValidateDefinitionAsync(dto, null, ct);
        var entity = new WorkflowDefinition
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            Enabled = dto.Enabled,
            Remark = dto.Remark,
            GraphJson = JsonSerializer.Serialize(dto.Graph, JsonOptions),
            FormFieldsJson = JsonSerializer.Serialize(dto.FormFields, JsonOptions),
        };
        db.WorkflowDefinitions.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateDefinitionAsync(long id, WorkflowDefinitionSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("流程定义不存在");
        await ValidateDefinitionAsync(dto, id, ct);
        entity.Code = dto.Code.Trim();
        entity.Name = dto.Name.Trim();
        entity.Enabled = dto.Enabled;
        entity.Remark = dto.Remark;
        entity.GraphJson = JsonSerializer.Serialize(dto.Graph, JsonOptions);
        entity.FormFieldsJson = JsonSerializer.Serialize(dto.FormFields, JsonOptions);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteDefinitionAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("流程定义不存在");
        if (await db.WorkflowInstances.AnyAsync(
                x => x.DefinitionId == id && x.Status == WorkflowInstanceStatus.Running, ct))
            throw new BusinessException("该流程存在审批中的实例，不能删除");
        if (await db.FormDefinitions.AnyAsync(x => x.WorkflowDefinitionId == id, ct))
            throw new BusinessException("该流程已被表单绑定，请先在表单中解除绑定");
        db.WorkflowDefinitions.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private async Task ValidateDefinitionAsync(WorkflowDefinitionSaveDto dto, long? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Code)) throw new BusinessException("流程编码不能为空");
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new BusinessException("流程名称不能为空");

        // 表单字段：key 非空且唯一
        var badField = dto.FormFields.FirstOrDefault(f => string.IsNullOrWhiteSpace(f.Key) || string.IsNullOrWhiteSpace(f.Label));
        if (badField is not null) throw new BusinessException("表单字段的 key 与名称不能为空");
        var dupKeys = dto.FormFields.GroupBy(f => f.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dupKeys.Count > 0) throw new BusinessException($"表单字段 key 重复：{string.Join(", ", dupKeys)}");

        var errors = dto.Graph.Validate(dto.FormFields);
        if (errors.Count > 0) throw new BusinessException(string.Join("；", errors));

        var code = dto.Code.Trim();
        if (await db.WorkflowDefinitions.AnyAsync(x => x.Code == code && x.Id != excludeId, ct))
            throw new BusinessException($"流程编码「{code}」已存在");
    }

    // ===== 流程实例 =====

    public Task<long> SubmitAsync(WorkflowSubmitDto dto, CancellationToken ct = default)
        => SubmitAsync(dto, null, ct);

    public async Task<long> SubmitAsync(WorkflowSubmitDto dto, string? formFieldsJsonOverride, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) throw new BusinessException("标题不能为空");

        var def = await db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == dto.DefinitionId, ct)
            ?? throw BusinessException.NotFound("流程定义不存在");
        if (!def.Enabled) throw new BusinessException("该流程已停用，不能发起");

        var graph = ParseGraph(def.GraphJson);
        var start = graph.StartNode ?? throw new BusinessException("流程图缺少开始节点");

        var uid = currentUser.UserId ?? throw BusinessException.Forbidden("未登录");
        var initiatorName = await db.Users.Where(u => u.Id == uid)
            .Select(u => u.NickName).FirstOrDefaultAsync(ct) ?? currentUser.UserName ?? string.Empty;

        // 落库前先推进到首个可用审批节点，避免留下无任务的悬挂实例
        var (stop, approvers, ccNodes) = await AdvanceResolveAsync(graph, start.Id, dto.FormData, ct);
        if (stop is null)
            throw new BusinessException("流程未经过任何可用审批节点（审批人为空），请检查流程定义");

        var instance = new WorkflowInstance
        {
            DefinitionId = def.Id,
            DefinitionName = def.Name,
            Title = dto.Title.Trim(),
            FormData = dto.FormData,
            GraphJson = def.GraphJson,
            FormFieldsJson = formFieldsJsonOverride ?? def.FormFieldsJson,
            CurrentNodeId = stop.Id,
            InitiatorId = uid,
            InitiatorName = initiatorName,
        };
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync(ct); // 先拿到实例 Id，再生成任务与抄送

        AddTasks(instance, stop, approvers);
        await AddCcRecordsAsync(instance.Id, ccNodes, ct);
        await db.SaveChangesAsync(ct);
        return instance.Id;
    }

    public async Task<PagedResult<WorkflowInstanceDto>> GetMyInstancesAsync(WorkflowInstanceQuery query, CancellationToken ct = default)
    {
        var uid = currentUser.UserId;
        var q = db.WorkflowInstances.Where(x => x.InitiatorId == uid);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Title.Contains(query.Keyword));
        if (query.Status is { } s)
            q = q.Where(x => x.Status == s);

        var total = await q.CountAsync(ct);
        var entities = await q.OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);

        return new PagedResult<WorkflowInstanceDto>(
            entities.Select(x => ToDto(x, new WorkflowInstanceDto())).ToList(), total, query.PageIndex, query.PageSize);
    }

    public async Task<WorkflowInstanceDetailDto> GetInstanceAsync(long id, CancellationToken ct = default)
    {
        var instance = await db.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("流程实例不存在");

        var tasks = await db.WorkflowTasks.Where(x => x.InstanceId == id)
            .OrderBy(x => x.Id).ToListAsync(ct);

        // 参与者（发起人/审批人）与管理员可查看
        var uid = currentUser.UserId;
        if (!currentUser.IsAdmin && instance.InitiatorId != uid && tasks.All(t => t.ApproverId != uid))
            throw BusinessException.Forbidden("无权查看该流程");

        var dto = ToDto(instance, new WorkflowInstanceDetailDto());
        dto.FormData = instance.FormData;
        dto.Graph = ParseGraph(instance.GraphJson);
        dto.FormFields = ParseFormFields(instance.FormFieldsJson);
        dto.Tasks = tasks.Select(t => new WorkflowTaskRecordDto
        {
            NodeId = t.NodeId,
            NodeName = t.NodeName,
            ApproverName = t.ApproverName,
            Status = t.Status,
            Comment = t.Comment,
            HandleTime = t.HandleTime,
        }).ToList();
        return dto;
    }

    public async Task CancelAsync(long id, CancellationToken ct = default)
    {
        var instance = await db.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("流程实例不存在");
        if (!currentUser.IsAdmin && instance.InitiatorId != currentUser.UserId)
            throw BusinessException.Forbidden("只能撤销自己发起的流程");
        if (instance.Status is not (WorkflowInstanceStatus.Running or WorkflowInstanceStatus.Returned))
            throw new BusinessException("流程已结束，不能撤销");

        instance.Status = WorkflowInstanceStatus.Canceled;
        instance.FinishTime = DateTime.Now;
        await TerminatePendingTasksAsync(instance.Id, ct);
        await db.SaveChangesAsync(ct);
    }

    // ===== 审批任务 =====

    public Task<PagedResult<WorkflowTaskItemDto>> GetTodoAsync(PagedRequest query, CancellationToken ct = default)
        => GetTasksAsync(query, pending: true, ct);

    public Task<PagedResult<WorkflowTaskItemDto>> GetDoneAsync(PagedRequest query, CancellationToken ct = default)
        => GetTasksAsync(query, pending: false, ct);

    private async Task<PagedResult<WorkflowTaskItemDto>> GetTasksAsync(PagedRequest query, bool pending, CancellationToken ct)
    {
        var uid = currentUser.UserId;
        var q = from t in db.WorkflowTasks
                join i in db.WorkflowInstances on t.InstanceId equals i.Id
                where t.ApproverId == uid
                select new { t, i };

        q = pending
            ? q.Where(x => x.t.Status == WorkflowTaskStatus.Pending)
            : q.Where(x => x.t.Status == WorkflowTaskStatus.Approved || x.t.Status == WorkflowTaskStatus.Rejected);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.i.Title.Contains(query.Keyword));

        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.t.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);

        var items = rows.Select(x => new WorkflowTaskItemDto
        {
            Id = x.t.Id,
            InstanceId = x.i.Id,
            InstanceTitle = x.i.Title,
            DefinitionName = x.i.DefinitionName,
            NodeName = x.t.NodeName,
            ApproveMode = x.t.ApproveMode,
            InitiatorName = x.i.InitiatorName,
            Status = x.t.Status,
            Comment = x.t.Comment,
            CreatedTime = x.t.CreatedTime,
            HandleTime = x.t.HandleTime,
            DueTime = x.t.DueTime,
        }).ToList();
        return new PagedResult<WorkflowTaskItemDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task ApproveAsync(long taskId, WorkflowApproveDto dto, CancellationToken ct = default)
    {
        var task = await db.WorkflowTasks.FirstOrDefaultAsync(x => x.Id == taskId, ct)
            ?? throw BusinessException.NotFound("审批任务不存在");
        if (task.ApproverId != currentUser.UserId)
            throw BusinessException.Forbidden("只能处理自己的审批任务");
        if (task.Status != WorkflowTaskStatus.Pending)
            throw new BusinessException("该任务已处理");

        var instance = await db.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == task.InstanceId, ct)
            ?? throw BusinessException.NotFound("流程实例不存在");
        if (instance.Status != WorkflowInstanceStatus.Running || task.NodeId != instance.CurrentNodeId)
            throw new BusinessException("流程状态已变化，该任务不可处理");

        var now = DateTime.Now;
        task.Status = dto.Approved ? WorkflowTaskStatus.Approved : WorkflowTaskStatus.Rejected;
        task.Comment = dto.Comment;
        task.HandleTime = now;

        var siblings = await db.WorkflowTasks
            .Where(x => x.InstanceId == instance.Id && x.NodeId == task.NodeId
                        && x.Id != task.Id && x.Status == WorkflowTaskStatus.Pending)
            .ToListAsync(ct);

        if (!dto.Approved)
        {
            foreach (var s in siblings) s.Status = WorkflowTaskStatus.Terminated;
            await ApplyRejectStrategyAsync(instance, task, now, ct);
        }
        else if (task.ApproveMode == WorkflowApproveMode.Any)
        {
            // 或签：一人通过即推进，其余待办终止
            foreach (var s in siblings) s.Status = WorkflowTaskStatus.Terminated;
            await MoveNextAsync(instance, task.NodeId, ct);
        }
        else if (siblings.Count == 0)
        {
            // 会签：本节点全部通过后推进
            await MoveNextAsync(instance, task.NodeId, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task ResubmitAsync(long id, WorkflowResubmitDto dto, CancellationToken ct = default)
    {
        var instance = await db.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("流程实例不存在");
        if (!currentUser.IsAdmin && instance.InitiatorId != currentUser.UserId)
            throw BusinessException.Forbidden("只能重新提交自己发起的流程");
        if (instance.Status != WorkflowInstanceStatus.Returned)
            throw new BusinessException("仅「已退回」的流程可以重新提交");

        if (!string.IsNullOrWhiteSpace(dto.Title)) instance.Title = dto.Title.Trim();
        instance.FormData = dto.FormData;

        // 沿用发起时的图快照，从头重新走（表单可能已修改，条件分支重新求值）
        var graph = ParseGraph(instance.GraphJson);
        var start = graph.StartNode ?? throw new BusinessException("流程图快照缺少开始节点");
        var (stop, approvers, ccNodes) = await AdvanceResolveAsync(graph, start.Id, instance.FormData, ct);
        if (stop is null)
            throw new BusinessException("流程未经过任何可用审批节点（审批人为空），无法重新提交");

        instance.Status = WorkflowInstanceStatus.Running;
        instance.CurrentNodeId = stop.Id;
        AddTasks(instance, stop, approvers);
        await AddCcRecordsAsync(instance.Id, ccNodes, ct);
        await db.SaveChangesAsync(ct);
    }

    // ===== 抄送 =====

    public async Task<PagedResult<WorkflowCcItemDto>> GetMyCcAsync(WorkflowCcQuery query, CancellationToken ct = default)
    {
        var uid = currentUser.UserId;
        var q = from c in db.WorkflowCcRecords
                join i in db.WorkflowInstances on c.InstanceId equals i.Id
                where c.UserId == uid
                select new { c, i };

        if (query.UnreadOnly == true)
            q = q.Where(x => x.c.ReadTime == null);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.i.Title.Contains(query.Keyword));

        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.c.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);

        var items = rows.Select(x => new WorkflowCcItemDto
        {
            Id = x.c.Id,
            InstanceId = x.i.Id,
            InstanceTitle = x.i.Title,
            DefinitionName = x.i.DefinitionName,
            NodeName = x.c.NodeName,
            InitiatorName = x.i.InitiatorName,
            InstanceStatus = x.i.Status,
            CreatedTime = x.c.CreatedTime,
            ReadTime = x.c.ReadTime,
        }).ToList();
        return new PagedResult<WorkflowCcItemDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task MarkCcReadAsync(long id, CancellationToken ct = default)
    {
        var record = await db.WorkflowCcRecords.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("抄送记录不存在");
        if (record.UserId != currentUser.UserId)
            throw BusinessException.Forbidden("只能标记自己的抄送");
        record.ReadTime ??= DateTime.Now;
        await db.SaveChangesAsync(ct);
    }

    // ===== 内部 =====

    /// <summary>按被驳回节点配置的策略处理：整体驳回 / 退回上一审批节点 / 退回发起人。</summary>
    private async Task ApplyRejectStrategyAsync(WorkflowInstance instance, WorkflowTask task, DateTime now, CancellationToken ct)
    {
        var graph = ParseGraph(instance.GraphJson);
        var strategy = graph.FindNode(task.NodeId)?.Props.RejectStrategy ?? WorkflowRejectStrategy.RejectInstance;

        if (strategy == WorkflowRejectStrategy.BackToInitiator)
        {
            instance.Status = WorkflowInstanceStatus.Returned;
            instance.CurrentNodeId = string.Empty;
            return;
        }

        if (strategy == WorkflowRejectStrategy.BackToPrevious)
        {
            // 上一审批节点 = 本实例中最近一个「非当前节点」的已处理任务所在节点
            var prevNodeId = await db.WorkflowTasks
                .Where(x => x.InstanceId == instance.Id && x.NodeId != task.NodeId && x.HandleTime != null)
                .OrderByDescending(x => x.Id)
                .Select(x => x.NodeId)
                .FirstOrDefaultAsync(ct);
            var prevNode = prevNodeId is null ? null : graph.FindNode(prevNodeId);
            var approvers = prevNode is null ? [] : await ResolveApproversAsync(prevNode.Props, ct);
            if (prevNode is not null && approvers.Count > 0)
            {
                instance.CurrentNodeId = prevNode.Id;
                AddTasks(instance, prevNode, approvers);
                return;
            }
            // 没有上一节点（首节点被驳回）或其审批人已失效 → 退化为整体驳回
        }

        instance.Status = WorkflowInstanceStatus.Rejected;
        instance.FinishTime = now;
    }

    /// <summary>
    /// 图推进 + 审批人解析：从 fromNodeId 沿图前进，跳过解析不出审批人的审批节点，
    /// 返回最终停留节点及其审批人；走到结束返回 (null, [])。途经抄送节点一并累计。
    /// </summary>
    private async Task<(WfNode? Stop, List<(long Id, string Name)> Approvers, List<WfNode> CcNodes)> AdvanceResolveAsync(
        WorkflowGraph graph, string fromNodeId, string? formData, CancellationToken ct)
    {
        var ccNodes = new List<WfNode>();
        var cursor = fromNodeId;
        while (true)
        {
            var result = WorkflowGraphEngine.Advance(graph, cursor, formData);
            ccNodes.AddRange(result.CcNodes);
            if (result.Completed) return (null, [], ccNodes);

            var approvers = await ResolveApproversAsync(result.StopNode!.Props, ct);
            if (approvers.Count > 0) return (result.StopNode, approvers, ccNodes);
            cursor = result.StopNode.Id; // 审批人为空 → 跳过该节点继续（图为 DAG，必然终止）
        }
    }

    /// <summary>审批完成后推进实例：生成下一节点任务或结束流程，落抄送记录。</summary>
    private async Task MoveNextAsync(WorkflowInstance instance, string fromNodeId, CancellationToken ct)
    {
        var graph = ParseGraph(instance.GraphJson);
        var (stop, approvers, ccNodes) = await AdvanceResolveAsync(graph, fromNodeId, instance.FormData, ct);
        await AddCcRecordsAsync(instance.Id, ccNodes, ct);

        if (stop is null)
        {
            instance.Status = WorkflowInstanceStatus.Approved;
            instance.FinishTime = DateTime.Now;
            return;
        }
        instance.CurrentNodeId = stop.Id;
        AddTasks(instance, stop, approvers);
    }

    private void AddTasks(WorkflowInstance instance, WfNode node, List<(long Id, string Name)> approvers)
    {
        var dueTime = node.Props.TimeoutHours > 0
            ? DateTime.Now.AddHours(node.Props.TimeoutHours)
            : (DateTime?)null;
        foreach (var (approverId, approverName) in approvers)
        {
            db.WorkflowTasks.Add(new WorkflowTask
            {
                InstanceId = instance.Id,
                NodeId = node.Id,
                NodeName = node.Name,
                ApproveMode = node.Props.ApproveMode,
                ApproverId = approverId,
                ApproverName = approverName,
                DueTime = dueTime,
            });
        }
    }

    private async Task AddCcRecordsAsync(long instanceId, List<WfNode> ccNodes, CancellationToken ct)
    {
        foreach (var node in ccNodes)
        {
            var users = await db.Users.Where(u => node.Props.UserIds.Contains(u.Id) && u.Enabled)
                .Select(u => new { u.Id, u.NickName }).ToListAsync(ct);
            foreach (var u in users)
            {
                db.WorkflowCcRecords.Add(new WorkflowCcRecord
                {
                    InstanceId = instanceId,
                    NodeId = node.Id,
                    NodeName = node.Name,
                    UserId = u.Id,
                    UserName = u.NickName,
                });
            }
        }
    }

    /// <summary>解析节点审批人为具体用户（角色 → 该角色下所有启用用户）。</summary>
    private async Task<List<(long Id, string Name)>> ResolveApproversAsync(WfNodeProps props, CancellationToken ct)
    {
        List<long> userIds;
        if (props.ApproverType == WorkflowApproverType.Role)
        {
            userIds = await db.UserRoles.Where(ur => props.ApproverIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId).Distinct().ToListAsync(ct);
        }
        else
        {
            userIds = props.ApproverIds;
        }

        var users = await db.Users.Where(u => userIds.Contains(u.Id) && u.Enabled)
            .Select(u => new { u.Id, u.NickName }).ToListAsync(ct);
        return users.Select(u => (u.Id, u.NickName)).ToList();
    }

    private async Task TerminatePendingTasksAsync(long instanceId, CancellationToken ct)
    {
        var tasks = await db.WorkflowTasks
            .Where(x => x.InstanceId == instanceId && x.Status == WorkflowTaskStatus.Pending)
            .ToListAsync(ct);
        foreach (var t in tasks) t.Status = WorkflowTaskStatus.Terminated;
    }

    private static WorkflowGraph ParseGraph(string json)
        => JsonSerializer.Deserialize<WorkflowGraph>(json, JsonOptions) ?? new WorkflowGraph();

    private static List<WorkflowFormField> ParseFormFields(string json)
        => string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<WorkflowFormField>>(json, JsonOptions) ?? [];

    private static WorkflowDefinitionDto ToDto(WorkflowDefinition x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Enabled = x.Enabled,
        Remark = x.Remark,
        Graph = ParseGraph(x.GraphJson),
        FormFields = ParseFormFields(x.FormFieldsJson),
        CreatedTime = x.CreatedTime,
    };

    private static T ToDto<T>(WorkflowInstance x, T dto) where T : WorkflowInstanceDto
    {
        dto.Id = x.Id;
        dto.DefinitionName = x.DefinitionName;
        dto.Title = x.Title;
        dto.Status = x.Status;
        dto.CurrentNodeId = x.CurrentNodeId;
        dto.CurrentNodeName = x.Status == WorkflowInstanceStatus.Running
            ? ParseGraph(x.GraphJson).FindNode(x.CurrentNodeId)?.Name ?? string.Empty
            : string.Empty;
        dto.InitiatorName = x.InitiatorName;
        dto.CreatedTime = x.CreatedTime;
        dto.FinishTime = x.FinishTime;
        return dto;
    }
}
