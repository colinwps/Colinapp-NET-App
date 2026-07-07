using System.Text.Json;
using Colinapp.Application.Workflow;
using Colinapp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Colinapp.Infrastructure.Persistence;

/// <summary>
/// 一次性数据升级：把旧版「线性节点数组」JSON（以 '[' 开头）转换为流程图 JSON。
/// 幂等：新格式（'{' 开头）的行直接跳过；配合 WorkflowGraph 迁移里的 NodeIndex→"n{下标}" 转换，
/// 旧任务/实例的节点引用保持一致。转换稳定后此文件可删除。
/// </summary>
internal static class WorkflowGraphMigrator
{
    /// <summary>旧版节点定义（仅供反序列化历史数据）。</summary>
    private sealed class LegacyNodeDef
    {
        public string Name { get; set; } = string.Empty;
        public WorkflowApproveMode ApproveMode { get; set; } = WorkflowApproveMode.Any;
        public WorkflowApproverType ApproverType { get; set; } = WorkflowApproverType.Users;
        public List<long> ApproverIds { get; set; } = [];
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task MigrateAsync(AppDbContext db, ILogger logger)
    {
        // IgnoreQueryFilters：软删除/跨租户的行也要转换
        var definitions = await db.WorkflowDefinitions.IgnoreQueryFilters()
            .Where(x => x.GraphJson.StartsWith("[")).ToListAsync();
        var instances = await db.WorkflowInstances.IgnoreQueryFilters()
            .Where(x => x.GraphJson.StartsWith("[")).ToListAsync();
        if (definitions.Count == 0 && instances.Count == 0) return;

        foreach (var def in definitions)
            def.GraphJson = ConvertToGraphJson(def.GraphJson);
        foreach (var inst in instances)
            inst.GraphJson = ConvertToGraphJson(inst.GraphJson);

        await db.SaveChangesAsync();
        logger.LogInformation("工作流旧数据已转换为图格式：定义 {DefCount} 条，实例 {InstCount} 条",
            definitions.Count, instances.Count);
    }

    /// <summary>[A,B] → start → n0(A) → n1(B) → end 的线性图（坐标自动横排）。</summary>
    private static string ConvertToGraphJson(string legacyJson)
    {
        var legacy = JsonSerializer.Deserialize<List<LegacyNodeDef>>(legacyJson, JsonOptions) ?? [];
        var graph = new WorkflowGraph();

        graph.Nodes.Add(new WfNode { Id = "start", Type = WfNodeTypes.Start, Name = "开始", X = 80, Y = 200 });
        for (var i = 0; i < legacy.Count; i++)
        {
            graph.Nodes.Add(new WfNode
            {
                Id = $"n{i}",
                Type = WfNodeTypes.Approval,
                Name = legacy[i].Name,
                X = 240 + i * 180,
                Y = 200,
                Props = new WfNodeProps
                {
                    ApproveMode = legacy[i].ApproveMode,
                    ApproverType = legacy[i].ApproverType,
                    ApproverIds = legacy[i].ApproverIds,
                },
            });
        }
        graph.Nodes.Add(new WfNode { Id = "end", Type = WfNodeTypes.End, Name = "结束", X = 240 + legacy.Count * 180, Y = 200 });

        var chain = graph.Nodes.Select(n => n.Id).ToList(); // start, n0..nK, end 顺序即链路
        for (var i = 0; i < chain.Count - 1; i++)
            graph.Edges.Add(new WfEdge { Id = $"e{i}", From = chain[i], To = chain[i + 1] });

        return JsonSerializer.Serialize(graph, JsonOptions);
    }
}
