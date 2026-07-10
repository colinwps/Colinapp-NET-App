using System.Text.Json;
using Colinapp.Application.Forms;
using Colinapp.Application.Workflow;
using Xunit;

namespace Colinapp.Application.Tests.Forms;

/// <summary>表单字段 schema 校验与「WorkflowFormField 超集」快照契约测试。</summary>
public class FormSchemaTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static FormField Field(string key, string type = "text", bool required = false,
        List<string>? options = null, string label = "字段")
        => new() { Key = key, Label = label, Type = type, Required = required, Options = options ?? [] };

    // ===== ValidateFields =====

    [Fact]
    public void ValidateFields_Valid_NoErrors()
    {
        var fields = new List<FormField>
        {
            Field("amount", "number", required: true),
            Field("type", "select", options: ["年假", "事假"]),
            new() { Label = "分组说明", Type = "divider" }, // divider 无 key 也合法
        };
        Assert.Empty(FormFieldTypes.ValidateFields(fields));
    }

    [Fact]
    public void ValidateFields_EmptyKeyOrLabel_Error()
    {
        Assert.NotEmpty(FormFieldTypes.ValidateFields([Field("", label: "有名无 key")]));
        Assert.NotEmpty(FormFieldTypes.ValidateFields([Field("k1", label: "")]));
    }

    [Fact]
    public void ValidateFields_DuplicateKey_Error()
    {
        var errors = FormFieldTypes.ValidateFields([Field("k1"), Field("k1")]);
        Assert.Contains(errors, e => e.Contains("重复"));
    }

    [Fact]
    public void ValidateFields_UnknownType_Error()
    {
        var errors = FormFieldTypes.ValidateFields([Field("k1", "magic")]);
        Assert.Contains(errors, e => e.Contains("不合法"));
    }

    [Theory]
    [InlineData("select")]
    [InlineData("radio")]
    [InlineData("checkbox")]
    public void ValidateFields_OptionTypeWithoutOptions_Error(string type)
    {
        var errors = FormFieldTypes.ValidateFields([Field("k1", type)]);
        Assert.Contains(errors, e => e.Contains("选项"));
    }

    // ===== ValidateData =====

    [Fact]
    public void ValidateData_RequiredPresent_NoErrors()
    {
        var fields = new List<FormField> { Field("amount", "number", required: true) };
        Assert.Empty(FormFieldTypes.ValidateData(fields, """{"amount": 100}"""));
    }

    [Theory]
    [InlineData("""{}""")]                    // 缺失
    [InlineData("""{"amount": null}""")]      // null
    [InlineData("""{"amount": ""}""")]        // 空串
    [InlineData("""{"amount": "  "}""")]      // 空白串
    public void ValidateData_RequiredMissingOrEmpty_Error(string dataJson)
    {
        var fields = new List<FormField> { Field("amount", "text", required: true) };
        Assert.NotEmpty(FormFieldTypes.ValidateData(fields, dataJson));
    }

    [Fact]
    public void ValidateData_RequiredCheckbox_EmptyArray_Error()
    {
        var fields = new List<FormField> { Field("tags", "checkbox", required: true, options: ["a"]) };
        Assert.NotEmpty(FormFieldTypes.ValidateData(fields, """{"tags": []}"""));
        Assert.Empty(FormFieldTypes.ValidateData(fields, """{"tags": ["a"]}"""));
    }

    [Fact]
    public void ValidateData_NotJsonObject_Error()
    {
        List<FormField> fields = [Field("k1")];
        Assert.NotEmpty(FormFieldTypes.ValidateData(fields, "自由文本"));
        Assert.NotEmpty(FormFieldTypes.ValidateData(fields, """[1, 2]"""));
    }

    [Fact]
    public void ValidateData_RequiredDivider_Skipped()
    {
        // divider 不产生数据，required 即使被误置为 true 也不校验
        var fields = new List<FormField>
        {
            new() { Label = "说明", Type = "divider", Required = true },
        };
        Assert.Empty(FormFieldTypes.ValidateData(fields, "{}"));
    }

    // ===== 超集快照契约：FormField JSON 可被 WorkflowFormField 反序列化 =====

    [Fact]
    public void Snapshot_FormFieldJson_RoundTripsToWorkflowFormField()
    {
        var fields = new List<FormField>
        {
            new()
            {
                Key = "amount", Label = "金额", Type = "number", Required = true,
                Placeholder = "请输入", Help = "单位：元", Span = 12,
                DefaultValue = JsonSerializer.SerializeToElement(100),
            },
            new()
            {
                Key = "tags", Label = "标签", Type = "checkbox", Required = false,
                Options = ["a", "b"],
            },
        };

        // 表单 SchemaJson 直接快照进 wf_instance.FormFieldsJson，
        // 工作流 ParseFormFields 用 WorkflowFormField 回读：五个共有属性保留，多余属性被忽略
        var json = JsonSerializer.Serialize(fields, Web);
        var parsed = JsonSerializer.Deserialize<List<WorkflowFormField>>(json, Web);

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed!.Count);
        Assert.Equal("amount", parsed[0].Key);
        Assert.Equal("金额", parsed[0].Label);
        Assert.Equal("number", parsed[0].Type);
        Assert.True(parsed[0].Required);
        Assert.Equal(["a", "b"], parsed[1].Options);
    }
}
