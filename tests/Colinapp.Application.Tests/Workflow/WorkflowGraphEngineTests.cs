using System.Text.Json;
using Colinapp.Application.Workflow;
using Xunit;

namespace Colinapp.Application.Tests.Workflow;

/// <summary>图遍历与条件求值测试。</summary>
public class WorkflowGraphEngineTests
{
    // ===== 遍历 =====

    [Fact]
    public void Advance_FromStart_StopsAtFirstApproval()
    {
        var g = GraphBuilder.Linear("A", "B");
        var r = WorkflowGraphEngine.Advance(g, "start", null);
        Assert.False(r.Completed);
        Assert.Equal("n0", r.StopNode!.Id);
    }

    [Fact]
    public void Advance_FromLastApproval_Completes()
    {
        var g = GraphBuilder.Linear("A", "B");
        var r = WorkflowGraphEngine.Advance(g, "n1", null);
        Assert.True(r.Completed);
        Assert.Null(r.StopNode);
    }

    [Fact]
    public void Advance_ConditionMatch_TakesConditionEdge()
    {
        var g = GraphBuilder.WithCondition();
        var r = WorkflowGraphEngine.Advance(g, "start", """{"amount": 8000}""");
        Assert.Equal("big", r.StopNode!.Id);
    }

    [Fact]
    public void Advance_ConditionNoMatch_TakesDefaultEdge()
    {
        var g = GraphBuilder.WithCondition();
        var r = WorkflowGraphEngine.Advance(g, "start", """{"amount": 100}""");
        Assert.Equal("small", r.StopNode!.Id);
    }

    [Fact]
    public void Advance_FreeTextFormData_TakesDefaultEdge()
    {
        var g = GraphBuilder.WithCondition();
        var r = WorkflowGraphEngine.Advance(g, "start", "随便写的文本，不是 JSON");
        Assert.Equal("small", r.StopNode!.Id);
    }

    [Fact]
    public void Advance_CollectsCcNodes_AndContinues()
    {
        var g = GraphBuilder.WithCc();
        var r = WorkflowGraphEngine.Advance(g, "start", null);
        Assert.Equal("n0", r.StopNode!.Id);
        Assert.Single(r.CcNodes);
        Assert.Equal("cc1", r.CcNodes[0].Id);
    }

    // ===== 条件求值 =====

    [Theory]
    [InlineData("""{"amount": 6000}""", true)]   // 数字 >
    [InlineData("""{"amount": "6000"}""", true)] // 字符串数字也可比较
    [InlineData("""{"amount": 5000}""", false)]  // 不满足严格大于
    [InlineData("""{"other": 1}""", false)]      // 字段缺失 → 不满足
    public void Evaluate_Gt(string formData, bool expected)
    {
        var cond = GraphBuilder.Cond("amount", "gt", 5000);
        Assert.Equal(expected, WorkflowGraphEngine.Evaluate(cond, Parse(formData)));
    }

    [Theory]
    [InlineData("""{"type": "年假"}""", true)]
    [InlineData("""{"type": "事假"}""", false)]
    public void Evaluate_Eq_String(string formData, bool expected)
    {
        var cond = GraphBuilder.Cond("type", "eq", "年假");
        Assert.Equal(expected, WorkflowGraphEngine.Evaluate(cond, Parse(formData)));
    }

    [Fact]
    public void Evaluate_Eq_NumberToleratesStringForm()
    {
        // 表单控件常把数字提交成字符串，1 与 "1" 应视为相等
        var cond = GraphBuilder.Cond("days", "eq", 1);
        Assert.True(WorkflowGraphEngine.Evaluate(cond, Parse("""{"days": "1"}""")));
    }

    [Fact]
    public void Evaluate_In()
    {
        var cond = GraphBuilder.Cond("type", "in", new[] { "年假", "调休" });
        Assert.True(WorkflowGraphEngine.Evaluate(cond, Parse("""{"type": "调休"}""")));
        Assert.False(WorkflowGraphEngine.Evaluate(cond, Parse("""{"type": "事假"}""")));
    }

    [Fact]
    public void Evaluate_Contains()
    {
        var cond = GraphBuilder.Cond("reason", "contains", "出差");
        Assert.True(WorkflowGraphEngine.Evaluate(cond, Parse("""{"reason": "去上海出差一周"}""")));
    }

    [Fact]
    public void Evaluate_OrLogic()
    {
        var cond = new WfCondition
        {
            Logic = "or",
            Rules =
            [
                GraphBuilder.Cond("amount", "gt", 5000).Rules[0],
                GraphBuilder.Cond("days", "gt", 3).Rules[0],
            ],
        };
        Assert.True(WorkflowGraphEngine.Evaluate(cond, Parse("""{"amount": 100, "days": 5}""")));
        Assert.False(WorkflowGraphEngine.Evaluate(cond, Parse("""{"amount": 100, "days": 1}""")));
    }

    // ===== 数组值（checkbox 多选字段）与布尔值（switch 字段）的行为钉子 =====

    [Fact]
    public void Evaluate_Contains_ArrayValue_MatchesElement()
    {
        // 多选字段提交数组，contains = 「包含某项」
        var cond = GraphBuilder.Cond("tags", "contains", "发票");
        Assert.True(WorkflowGraphEngine.Evaluate(cond, Parse("""{"tags": ["发票", "行程单"]}""")));
        Assert.False(WorkflowGraphEngine.Evaluate(cond, Parse("""{"tags": ["行程单"]}""")));
        Assert.False(WorkflowGraphEngine.Evaluate(cond, Parse("""{"tags": []}""")));
    }

    [Theory]
    [InlineData("eq")]
    [InlineData("gt")]
    public void Evaluate_ArrayValue_NonContainsOps_AlwaysFalse(string op)
    {
        // 数组值配 eq/gt 等标量操作符恒为 false（前端条件设计器据此只对 checkbox 开放 contains）
        var cond = GraphBuilder.Cond("tags", op, "发票");
        Assert.False(WorkflowGraphEngine.Evaluate(cond, Parse("""{"tags": ["发票"]}""")));
    }

    [Fact]
    public void Evaluate_In_ArrayActual_AlwaysFalse()
    {
        var cond = GraphBuilder.Cond("tags", "in", new[] { "发票" });
        Assert.False(WorkflowGraphEngine.Evaluate(cond, Parse("""{"tags": ["发票"]}""")));
    }

    [Fact]
    public void Evaluate_Eq_BoolToleratesStringForm()
    {
        // switch 字段提交布尔值，与规则里的字符串 "true" 宽容相等
        var cond = GraphBuilder.Cond("urgent", "eq", "true");
        Assert.True(WorkflowGraphEngine.Evaluate(cond, Parse("""{"urgent": true}""")));
        Assert.False(WorkflowGraphEngine.Evaluate(cond, Parse("""{"urgent": false}""")));
        var condBool = GraphBuilder.Cond("urgent", "eq", true);
        Assert.True(WorkflowGraphEngine.Evaluate(condBool, Parse("""{"urgent": true}""")));
    }

    private static JsonElement? Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
