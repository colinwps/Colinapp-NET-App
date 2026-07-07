using System.Text.Json;
using Colinapp.Shared.Exceptions;

namespace Colinapp.Application.Workflow;

/// <summary>一次推进的结果：停在某个审批节点，或流程走到结束；途经的抄送节点一并带出。</summary>
public record WfAdvanceResult(WfNode? StopNode, List<WfNode> CcNodes, bool Completed);

/// <summary>
/// 图遍历引擎（纯逻辑、无 IO，可单测）。
/// 从某节点沿出边前进，条件节点按边规则选路，抄送节点收集后继续，
/// 直到停在审批节点或到达结束节点。审批人解析（含空审批人跳过）由服务层处理。
/// </summary>
public static class WorkflowGraphEngine
{
    /// <summary>
    /// 从 fromNodeId 的出边开始推进。fromNodeId 通常是开始节点或刚完成的审批节点。
    /// formDataJson 为发起时的表单数据（JSON 对象字符串），供条件边求值；非法/空时条件均视为不满足。
    /// </summary>
    public static WfAdvanceResult Advance(WorkflowGraph graph, string fromNodeId, string? formDataJson)
    {
        JsonElement? formData = null;
        if (!string.IsNullOrWhiteSpace(formDataJson))
        {
            try
            {
                var doc = JsonDocument.Parse(formDataJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    formData = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // 自由文本表单（旧数据）无法参与条件求值，按无数据处理
            }
        }

        var ccNodes = new List<WfNode>();
        var current = graph.FindNode(fromNodeId)
            ?? throw new BusinessException($"流程图中不存在节点「{fromNodeId}」");

        // DAG 上推进步数不会超过节点数；防御快照数据异常导致死循环
        for (var step = 0; step <= graph.Nodes.Count; step++)
        {
            var edge = PickEdge(graph, current, formData)
                ?? throw new BusinessException($"节点「{current.Name}」没有可走的出线，流程图快照异常");
            current = graph.FindNode(edge.To)
                ?? throw new BusinessException($"流程图中不存在节点「{edge.To}」");

            switch (current.Type)
            {
                case WfNodeTypes.Approval:
                    return new WfAdvanceResult(current, ccNodes, false);
                case WfNodeTypes.End:
                    return new WfAdvanceResult(null, ccNodes, true);
                case WfNodeTypes.Cc:
                    ccNodes.Add(current);
                    break;
                case WfNodeTypes.Condition:
                    break; // 下一轮 PickEdge 按条件选路
                default:
                    throw new BusinessException($"未知节点类型「{current.Type}」");
            }
        }

        throw new BusinessException("流程推进超出最大步数，流程图快照异常");
    }

    /// <summary>选择节点的出边：条件节点按优先级取首个满足条件的边，都不满足走默认边；其余节点取唯一出边。</summary>
    internal static WfEdge? PickEdge(WorkflowGraph graph, WfNode node, JsonElement? formData)
    {
        var outs = graph.OutEdges(node.Id);
        if (node.Type != WfNodeTypes.Condition)
            return outs.FirstOrDefault();

        foreach (var edge in outs.Where(e => !e.IsDefault).OrderBy(e => e.Priority))
        {
            if (edge.Condition is not null && Evaluate(edge.Condition, formData))
                return edge;
        }
        return outs.FirstOrDefault(e => e.IsDefault);
    }

    /// <summary>求值结构化条件。无表单数据时视为不满足（走默认边）。</summary>
    public static bool Evaluate(WfCondition condition, JsonElement? formData)
    {
        if (formData is null || condition.Rules.Count == 0) return false;
        return condition.Logic.Equals("or", StringComparison.OrdinalIgnoreCase)
            ? condition.Rules.Any(r => EvaluateRule(r, formData.Value))
            : condition.Rules.All(r => EvaluateRule(r, formData.Value));
    }

    private static bool EvaluateRule(WfRule rule, JsonElement data)
    {
        if (!data.TryGetProperty(rule.Field, out var actual)) return false;

        return rule.Op.ToLowerInvariant() switch
        {
            "eq" => JsonEquals(actual, rule.Value),
            "ne" => !JsonEquals(actual, rule.Value),
            "gt" => CompareNumeric(actual, rule.Value) is > 0,
            "ge" => CompareNumeric(actual, rule.Value) is >= 0,
            "lt" => CompareNumeric(actual, rule.Value) is < 0,
            "le" => CompareNumeric(actual, rule.Value) is <= 0,
            "in" => rule.Value.ValueKind == JsonValueKind.Array
                && rule.Value.EnumerateArray().Any(item => JsonEquals(actual, item)),
            "contains" => Contains(actual, rule.Value),
            _ => false,
        };
    }

    private static bool JsonEquals(JsonElement a, JsonElement b)
    {
        // 数字优先按数值比较（1 == 1.0），其余按标准化字符串比较（"1" 与 1 也视为相等，宽容表单类型差异）
        if (TryGetDecimal(a, out var da) && TryGetDecimal(b, out var db)) return da == db;
        return AsString(a) == AsString(b);
    }

    /// <summary>双方都能转数字时返回比较结果，否则 null（gt/lt 等对非数字不成立）。</summary>
    private static int? CompareNumeric(JsonElement a, JsonElement b)
    {
        if (TryGetDecimal(a, out var da) && TryGetDecimal(b, out var db)) return da.CompareTo(db);
        return null;
    }

    private static bool Contains(JsonElement actual, JsonElement expected)
    {
        if (actual.ValueKind == JsonValueKind.Array)
            return actual.EnumerateArray().Any(item => JsonEquals(item, expected));
        return AsString(actual).Contains(AsString(expected), StringComparison.Ordinal);
    }

    private static bool TryGetDecimal(JsonElement e, out decimal value)
    {
        value = 0;
        return e.ValueKind switch
        {
            JsonValueKind.Number => e.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(e.GetString(), out value),
            _ => false,
        };
    }

    private static string AsString(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString() ?? string.Empty,
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => e.GetRawText(),
    };
}
