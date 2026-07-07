using Colinapp.Application.Workflow;
using Colinapp.Domain.Enums;

namespace Colinapp.Application.Tests.Workflow;

/// <summary>测试用图构造器。</summary>
internal static class GraphBuilder
{
    /// <summary>start → n0..nK（审批，审批人 = 用户1）→ end 的线性图。</summary>
    public static WorkflowGraph Linear(params string[] approvalNames)
    {
        var g = new WorkflowGraph();
        g.Nodes.Add(new WfNode { Id = "start", Type = WfNodeTypes.Start, Name = "开始" });
        for (var i = 0; i < approvalNames.Length; i++)
        {
            g.Nodes.Add(new WfNode
            {
                Id = $"n{i}", Type = WfNodeTypes.Approval, Name = approvalNames[i],
                Props = new WfNodeProps { ApproverIds = [1] },
            });
        }
        g.Nodes.Add(new WfNode { Id = "end", Type = WfNodeTypes.End, Name = "结束" });

        var chain = g.Nodes.Select(n => n.Id).ToList();
        for (var i = 0; i < chain.Count - 1; i++)
            g.Edges.Add(new WfEdge { Id = $"e{i}", From = chain[i], To = chain[i + 1] });
        return g;
    }

    /// <summary>
    /// start → g1(条件) →(amount>5000)→ big(审批) → end
    ///                   →(默认)→ small(审批) → end
    /// </summary>
    public static WorkflowGraph WithCondition(bool defaultEdge = true)
    {
        var g = new WorkflowGraph();
        g.Nodes.Add(new WfNode { Id = "start", Type = WfNodeTypes.Start, Name = "开始" });
        g.Nodes.Add(new WfNode { Id = "g1", Type = WfNodeTypes.Condition, Name = "金额判断" });
        g.Nodes.Add(new WfNode
        {
            Id = "big", Type = WfNodeTypes.Approval, Name = "总经理审批",
            Props = new WfNodeProps { ApproverIds = [2] },
        });
        g.Nodes.Add(new WfNode
        {
            Id = "small", Type = WfNodeTypes.Approval, Name = "主管审批",
            Props = new WfNodeProps { ApproverIds = [1] },
        });
        g.Nodes.Add(new WfNode { Id = "end", Type = WfNodeTypes.End, Name = "结束" });

        g.Edges.Add(new WfEdge { Id = "e0", From = "start", To = "g1" });
        g.Edges.Add(new WfEdge
        {
            Id = "e1", From = "g1", To = "big", Priority = 1,
            Condition = Cond("amount", "gt", 5000),
        });
        g.Edges.Add(defaultEdge
            ? new WfEdge { Id = "e2", From = "g1", To = "small", IsDefault = true }
            : new WfEdge { Id = "e2", From = "g1", To = "small", Priority = 2, Condition = Cond("amount", "le", 5000) });
        g.Edges.Add(new WfEdge { Id = "e3", From = "big", To = "end" });
        g.Edges.Add(new WfEdge { Id = "e4", From = "small", To = "end" });
        return g;
    }

    /// <summary>start → cc1(抄送用户3) → n0(审批) → end。</summary>
    public static WorkflowGraph WithCc()
    {
        var g = new WorkflowGraph();
        g.Nodes.Add(new WfNode { Id = "start", Type = WfNodeTypes.Start, Name = "开始" });
        g.Nodes.Add(new WfNode
        {
            Id = "cc1", Type = WfNodeTypes.Cc, Name = "抄送人事",
            Props = new WfNodeProps { UserIds = [3] },
        });
        g.Nodes.Add(new WfNode
        {
            Id = "n0", Type = WfNodeTypes.Approval, Name = "审批",
            Props = new WfNodeProps { ApproverIds = [1] },
        });
        g.Nodes.Add(new WfNode { Id = "end", Type = WfNodeTypes.End, Name = "结束" });
        g.Edges.Add(new WfEdge { Id = "e0", From = "start", To = "cc1" });
        g.Edges.Add(new WfEdge { Id = "e1", From = "cc1", To = "n0" });
        g.Edges.Add(new WfEdge { Id = "e2", From = "n0", To = "end" });
        return g;
    }

    public static WfCondition Cond(string field, string op, object value)
    {
        var json = System.Text.Json.JsonSerializer.SerializeToElement(value);
        return new WfCondition { Rules = [new WfRule { Field = field, Op = op, Value = json }] };
    }
}
