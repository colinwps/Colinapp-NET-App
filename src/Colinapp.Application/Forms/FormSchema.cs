using System.Text.Json;
using Colinapp.Application.Workflow;

namespace Colinapp.Application.Forms;

/// <summary>
/// 表单字段：<see cref="WorkflowFormField"/> 的超集（继承保证不漂移）。
/// 表单 SchemaJson 直接快照进流程实例的 FormFieldsJson 时，多余属性会被
/// 反序列化忽略，工作流的条件求值 / 审批详情 / 退回重提零改动。
/// </summary>
public class FormField : WorkflowFormField
{
    // 继承：Key / Label / Type / Required / Options
    // Type 在表单侧扩展为：text / textarea / number / date / datetime / select / radio / checkbox / switch / divider

    /// <summary>占位提示</summary>
    public string? Placeholder { get; set; }

    /// <summary>字段下方说明文字</summary>
    public string? Help { get; set; }

    /// <summary>默认值（标量；checkbox 为数组）</summary>
    public JsonElement? DefaultValue { get; set; }

    /// <summary>el-col 栅格宽度（1~24）</summary>
    public int Span { get; set; } = 24;
}

/// <summary>字段类型集合与纯校验逻辑（无依赖，可单测）。</summary>
public static class FormFieldTypes
{
    public static readonly HashSet<string> All =
        ["text", "textarea", "number", "date", "datetime", "select", "radio", "checkbox", "switch", "divider"];

    /// <summary>必须配置选项的类型</summary>
    public static readonly HashSet<string> NeedOptions = ["select", "radio", "checkbox"];

    /// <summary>是否为录入字段（divider 纯展示，不产生数据、不参与必填/条件）</summary>
    public static bool IsInput(string type) => type != "divider";

    /// <summary>校验字段定义：key/label 非空、key 唯一、类型合法、选项类字段必须有选项。</summary>
    public static List<string> ValidateFields(List<FormField> fields)
    {
        var errors = new List<string>();
        var inputs = fields.Where(f => IsInput(f.Type)).ToList();

        foreach (var f in fields)
        {
            if (!All.Contains(f.Type))
                errors.Add($"字段「{f.Label}」类型「{f.Type}」不合法");
            if (!IsInput(f.Type)) continue;

            if (string.IsNullOrWhiteSpace(f.Key) || string.IsNullOrWhiteSpace(f.Label))
                errors.Add("录入字段的 key 与名称不能为空");
            if (NeedOptions.Contains(f.Type) && f.Options.Count == 0)
                errors.Add($"字段「{f.Label}」为选项类型，至少配置一个选项");
        }

        var dupKeys = inputs.Where(f => !string.IsNullOrWhiteSpace(f.Key))
            .GroupBy(f => f.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dupKeys.Count > 0)
            errors.Add($"字段 key 重复：{string.Join(", ", dupKeys)}");

        return errors.Distinct().ToList();
    }

    /// <summary>校验提交数据：必须是 JSON 对象；必填字段存在且非空（空串/null/空数组均判空）。</summary>
    public static List<string> ValidateData(List<FormField> fields, string dataJson)
    {
        var errors = new List<string>();
        JsonElement data;
        try
        {
            data = JsonSerializer.Deserialize<JsonElement>(
                string.IsNullOrWhiteSpace(dataJson) ? "{}" : dataJson);
        }
        catch (JsonException)
        {
            return ["表单数据不是合法的 JSON"];
        }
        if (data.ValueKind != JsonValueKind.Object)
            return ["表单数据必须是 JSON 对象"];

        foreach (var f in fields.Where(f => IsInput(f.Type) && f.Required))
        {
            if (!data.TryGetProperty(f.Key, out var v) || IsEmpty(v))
                errors.Add($"「{f.Label}」为必填项");
        }
        return errors;
    }

    private static bool IsEmpty(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => true,
        JsonValueKind.String => string.IsNullOrWhiteSpace(v.GetString()),
        JsonValueKind.Array => v.GetArrayLength() == 0,
        _ => false,
    };
}
