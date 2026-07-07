using System.Text.Json;
using Colinapp.Domain.Enums;

namespace Colinapp.Application.Workflow;

// ============ 流程图 JSON Schema（详见 docs/工作流设计器规划.md） ============
// 定义存 GraphJson，发起时快照到实例。序列化统一 camelCase（JsonSerializerDefaults.Web）。

/// <summary>节点类型常量。会签不是独立类型：画布上的「会签节点」= approval + ApproveMode.All 预设。</summary>
public static class WfNodeTypes
{
    public const string Start = "start";
    public const string Approval = "approval";
    public const string Condition = "condition";
    public const string Cc = "cc";
    public const string End = "end";
}

public class WorkflowGraph
{
    public int Version { get; set; } = 1;
    public List<WfNode> Nodes { get; set; } = [];
    public List<WfEdge> Edges { get; set; } = [];

    public WfNode? FindNode(string id) => Nodes.FirstOrDefault(n => n.Id == id);

    public WfNode? StartNode => Nodes.FirstOrDefault(n => n.Type == WfNodeTypes.Start);

    public List<WfEdge> OutEdges(string nodeId) => Edges.Where(e => e.From == nodeId).ToList();

    /// <summary>
    /// 保存前的完整校验，返回全部问题（空列表 = 合法）。
    /// formFields 非空时同时校验条件边引用的字段是否存在。
    /// </summary>
    public List<string> Validate(List<WorkflowFormField>? formFields = null)
    {
        var errors = new List<string>();

        // 节点 Id 唯一
        var dupIds = Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dupIds.Count > 0) errors.Add($"节点 Id 重复：{string.Join(", ", dupIds)}");

        // 唯一开始、至少一个结束
        var starts = Nodes.Where(n => n.Type == WfNodeTypes.Start).ToList();
        if (starts.Count != 1) errors.Add("流程必须有且只有一个开始节点");
        if (Nodes.All(n => n.Type != WfNodeTypes.End)) errors.Add("流程至少需要一个结束节点");

        // 边引用存在的节点
        var nodeIds = Nodes.Select(n => n.Id).ToHashSet();
        foreach (var e in Edges)
        {
            if (!nodeIds.Contains(e.From) || !nodeIds.Contains(e.To))
                errors.Add($"连线「{e.Id}」引用了不存在的节点");
        }
        if (errors.Count > 0) return errors; // 结构性错误先返回，避免后续检查空引用

        var fieldKeys = formFields?.Select(f => f.Key).ToHashSet();
        foreach (var node in Nodes)
        {
            var label = string.IsNullOrEmpty(node.Name) ? node.Id : node.Name;
            var outs = OutEdges(node.Id);
            var ins = Edges.Count(e => e.To == node.Id);

            switch (node.Type)
            {
                case WfNodeTypes.Start:
                    if (ins > 0) errors.Add("开始节点不能有入线");
                    if (outs.Count != 1) errors.Add("开始节点必须有且只有一条出线");
                    break;
                case WfNodeTypes.End:
                    if (outs.Count > 0) errors.Add("结束节点不能有出线");
                    if (ins == 0) errors.Add($"结束节点「{label}」没有入线");
                    break;
                case WfNodeTypes.Condition:
                    if (ins == 0) errors.Add($"条件节点「{label}」没有入线");
                    if (outs.Count < 2) errors.Add($"条件节点「{label}」至少需要两条出线");
                    if (outs.Count(e => e.IsDefault) != 1)
                        errors.Add($"条件节点「{label}」必须有且只有一条默认出线");
                    foreach (var e in outs.Where(e => !e.IsDefault))
                    {
                        if (e.Condition is null || e.Condition.Rules.Count == 0)
                            errors.Add($"条件节点「{label}」的非默认出线必须配置条件");
                        else if (fieldKeys is not null)
                        {
                            foreach (var r in e.Condition.Rules.Where(r => !fieldKeys.Contains(r.Field)))
                                errors.Add($"条件节点「{label}」引用了不存在的表单字段「{r.Field}」");
                        }
                    }
                    break;
                case WfNodeTypes.Approval:
                    if (ins == 0) errors.Add($"审批节点「{label}」没有入线");
                    if (outs.Count != 1) errors.Add($"审批节点「{label}」必须有且只有一条出线");
                    if (node.Props.ApproverIds.Count == 0) errors.Add($"审批节点「{label}」未指定审批人");
                    break;
                case WfNodeTypes.Cc:
                    if (ins == 0) errors.Add($"抄送节点「{label}」没有入线");
                    if (outs.Count != 1) errors.Add($"抄送节点「{label}」必须有且只有一条出线");
                    if (node.Props.UserIds.Count == 0) errors.Add($"抄送节点「{label}」未指定抄送人");
                    break;
                default:
                    errors.Add($"节点「{label}」类型未知：{node.Type}");
                    break;
            }
        }
        if (errors.Count > 0) return errors;

        // 可达性 + 无环（DFS，三色标记）
        var start = starts[0];
        var color = Nodes.ToDictionary(n => n.Id, _ => 0); // 0 未访问 1 访问中 2 完成
        var hasCycle = false;
        void Dfs(string id)
        {
            color[id] = 1;
            foreach (var e in OutEdges(id))
            {
                if (color[e.To] == 1) hasCycle = true;
                else if (color[e.To] == 0) Dfs(e.To);
            }
            color[id] = 2;
        }
        Dfs(start.Id);
        if (hasCycle) errors.Add("流程图不允许存在环路");
        var unreachable = Nodes.Where(n => color[n.Id] == 0).Select(n => n.Name is { Length: > 0 } ? n.Name : n.Id).ToList();
        if (unreachable.Count > 0) errors.Add($"以下节点从开始节点不可达：{string.Join(", ", unreachable)}");

        // 至少经过一个审批节点（起点出发的所有路径无法静态保证，退而校验图中存在审批节点）
        if (Nodes.All(n => n.Type != WfNodeTypes.Approval))
            errors.Add("流程至少需要一个审批节点");

        return errors;
    }
}

public class WfNode
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>画布坐标，引擎忽略</summary>
    public double X { get; set; }
    public double Y { get; set; }

    public WfNodeProps Props { get; set; } = new();
}

/// <summary>节点属性合集（按类型取用：approval 用审批相关项，cc 用 UserIds）。</summary>
public class WfNodeProps
{
    public WorkflowApproveMode ApproveMode { get; set; } = WorkflowApproveMode.Any;
    public WorkflowApproverType ApproverType { get; set; } = WorkflowApproverType.Users;
    public List<long> ApproverIds { get; set; } = [];

    /// <summary>驳回策略（approval 节点）</summary>
    public WorkflowRejectStrategy RejectStrategy { get; set; } = WorkflowRejectStrategy.RejectInstance;

    /// <summary>审批超时提醒小时数，0 = 不限时（approval 节点）</summary>
    public int TimeoutHours { get; set; }

    /// <summary>抄送人（cc 节点）</summary>
    public List<long> UserIds { get; set; } = [];
}

public class WfEdge
{
    public string Id { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Name { get; set; }

    /// <summary>条件出线的求值顺序（小者优先），仅条件节点出线有意义</summary>
    public int Priority { get; set; }

    /// <summary>条件节点的默认出线（所有条件都不满足时走这条）</summary>
    public bool IsDefault { get; set; }

    public WfCondition? Condition { get; set; }
}

/// <summary>结构化条件（不做表达式 eval）：rules 按 logic（and/or）组合。</summary>
public class WfCondition
{
    public string Logic { get; set; } = "and";
    public List<WfRule> Rules { get; set; } = [];
}

public class WfRule
{
    /// <summary>表单字段 key</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>eq/ne/gt/ge/lt/le/in/contains</summary>
    public string Op { get; set; } = "eq";

    /// <summary>比较值：数字/字符串/布尔，in 时为数组</summary>
    public JsonElement Value { get; set; }
}

/// <summary>表单字段定义（定义存 FormFieldsJson，发起页据此渲染动态表单）。</summary>
public class WorkflowFormField
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    /// <summary>text / textarea / number / date / select</summary>
    public string Type { get; set; } = "text";

    public bool Required { get; set; }

    /// <summary>select 的选项</summary>
    public List<string> Options { get; set; } = [];
}
