using Colinapp.Application.Workflow;
using Colinapp.Domain.Enums;
using Xunit;

namespace Colinapp.Application.Tests.Workflow;

/// <summary>流程图校验规则测试。</summary>
public class WorkflowGraphTests
{
    [Fact]
    public void Validate_LinearGraph_Passes()
    {
        var g = GraphBuilder.Linear("主管审批", "人事会签");
        Assert.Empty(g.Validate());
    }

    [Fact]
    public void Validate_MissingStart_Fails()
    {
        var g = GraphBuilder.Linear("A");
        g.Nodes.RemoveAll(n => n.Type == WfNodeTypes.Start);
        g.Edges.RemoveAll(e => e.From == "start");
        Assert.Contains(g.Validate(), e => e.Contains("开始节点"));
    }

    [Fact]
    public void Validate_ApprovalWithoutApprover_Fails()
    {
        var g = GraphBuilder.Linear("A");
        g.FindNode("n0")!.Props.ApproverIds.Clear();
        Assert.Contains(g.Validate(), e => e.Contains("未指定审批人"));
    }

    [Fact]
    public void Validate_ConditionWithoutDefaultEdge_Fails()
    {
        var g = GraphBuilder.WithCondition(defaultEdge: false);
        Assert.Contains(g.Validate(), e => e.Contains("默认出线"));
    }

    [Fact]
    public void Validate_Cycle_Fails()
    {
        var g = GraphBuilder.Linear("A", "B");
        g.Edges.Add(new WfEdge { Id = "loop", From = "n1", To = "n0" }); // n1 → n0 回环
        var errors = g.Validate();
        Assert.Contains(errors, e => e.Contains("环路") || e.Contains("出线")); // 环 + n1 出线数同时违规
    }

    [Fact]
    public void Validate_UnreachableNode_Fails()
    {
        // 两个互连的孤岛审批节点：出入线数量都合规，但从开始节点不可达
        var g = GraphBuilder.Linear("A");
        g.Nodes.Add(new WfNode
        {
            Id = "orphanA", Type = WfNodeTypes.Approval, Name = "孤岛A",
            Props = new WfNodeProps { ApproverIds = [9] },
        });
        g.Nodes.Add(new WfNode
        {
            Id = "orphanB", Type = WfNodeTypes.Approval, Name = "孤岛B",
            Props = new WfNodeProps { ApproverIds = [9] },
        });
        g.Edges.Add(new WfEdge { Id = "eo1", From = "orphanA", To = "orphanB" });
        g.Edges.Add(new WfEdge { Id = "eo2", From = "orphanB", To = "orphanA" });
        Assert.Contains(g.Validate(), e => e.Contains("不可达"));
    }

    [Fact]
    public void Validate_ConditionFieldNotInForm_Fails()
    {
        var g = GraphBuilder.WithCondition();
        var fields = new List<WorkflowFormField> { new() { Key = "other", Label = "其它" } };
        Assert.Contains(g.Validate(fields), e => e.Contains("不存在的表单字段"));
    }

    [Fact]
    public void Validate_NoApprovalNode_Fails()
    {
        var g = new WorkflowGraph();
        g.Nodes.Add(new WfNode { Id = "start", Type = WfNodeTypes.Start, Name = "开始" });
        g.Nodes.Add(new WfNode { Id = "end", Type = WfNodeTypes.End, Name = "结束" });
        g.Edges.Add(new WfEdge { Id = "e0", From = "start", To = "end" });
        Assert.Contains(g.Validate(), e => e.Contains("至少需要一个审批节点"));
    }
}
