namespace Colinapp.Application.CodeGen;

// ---------- 数据库内省元数据 ----------

/// <summary>数据库表信息</summary>
public class DbTableInfo
{
    public string TableName { get; set; } = string.Empty;
    public string? TableComment { get; set; }
    public DateTime? CreateTime { get; set; }
}

/// <summary>数据库列信息</summary>
public class DbColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    /// <summary>数据类型，如 varchar、bigint</summary>
    public string DataType { get; set; } = string.Empty;
    /// <summary>完整列类型，如 varchar(200)、tinyint(1)</summary>
    public string ColumnType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    /// <summary>主键标记：PRI</summary>
    public string? ColumnKey { get; set; }
    public string? Extra { get; set; }
    public string? ColumnComment { get; set; }
    public int OrdinalPosition { get; set; }
    public long? MaxLength { get; set; }
}

// ---------- 内省服务抽象 ----------

public interface ITableSchemaProvider
{
    Task<List<DbTableInfo>> GetTablesAsync(string? keyword, CancellationToken ct = default);
    Task<List<DbColumnInfo>> GetColumnsAsync(string tableName, CancellationToken ct = default);
}

// ---------- 生成配置 ----------

/// <summary>列生成配置（前端可编辑后回传）</summary>
public class GenColumnConfig
{
    public string ColumnName { get; set; } = string.Empty;
    /// <summary>C# 属性名（PascalCase）</summary>
    public string FieldName { get; set; } = string.Empty;
    /// <summary>显示名称（取自列注释）</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>C# 类型：string/int/long/decimal/double/bool/DateTime</summary>
    public string CsharpType { get; set; } = "string";
    /// <summary>TS 类型：string/number/boolean</summary>
    public string TsType { get; set; } = "string";
    public bool Nullable { get; set; }
    public long? MaxLength { get; set; }

    public bool IsPk { get; set; }
    /// <summary>是否基类/审计字段（id、created_* 等，不生成业务属性）</summary>
    public bool IsBase { get; set; }

    /// <summary>列表列</summary>
    public bool IsList { get; set; } = true;
    /// <summary>查询条件</summary>
    public bool IsQuery { get; set; }
    /// <summary>表单字段</summary>
    public bool IsForm { get; set; } = true;
    /// <summary>必填</summary>
    public bool IsRequired { get; set; }
    /// <summary>表单控件：input/textarea/number/date/switch/select</summary>
    public string HtmlType { get; set; } = "input";
    /// <summary>查询匹配：eq/like</summary>
    public string QueryType { get; set; } = "eq";
}

/// <summary>表生成配置</summary>
public class GenTableConfig
{
    public string TableName { get; set; } = string.Empty;
    /// <summary>实体类名（PascalCase），如 Product</summary>
    public string ClassName { get; set; } = string.Empty;
    /// <summary>模块/区域（用于命名空间与目录），如 Business</summary>
    public string ModuleName { get; set; } = "Business";
    /// <summary>业务名（小写，用于路由/组件/接口文件），如 product</summary>
    public string BusinessName { get; set; } = string.Empty;
    /// <summary>功能名（中文，用于菜单与注释），如 产品</summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>权限前缀，如 biz:product</summary>
    public string PermissionPrefix { get; set; } = string.Empty;
    public List<GenColumnConfig> Columns { get; set; } = [];
}

/// <summary>生成的单个文件</summary>
public class GeneratedFile
{
    public string FileName { get; set; } = string.Empty;
    /// <summary>建议放置路径（相对仓库根）</summary>
    public string TargetPath { get; set; } = string.Empty;
    /// <summary>语言（用于前端高亮/标签）：csharp/vue/typescript/sql</summary>
    public string Language { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public interface ICodeGenService
{
    Task<List<DbTableInfo>> GetTablesAsync(string? keyword, CancellationToken ct = default);
    /// <summary>读取列并推断默认生成配置。</summary>
    Task<GenTableConfig> BuildConfigAsync(string tableName, CancellationToken ct = default);
    /// <summary>按配置生成全部产物。</summary>
    List<GeneratedFile> Generate(GenTableConfig config);
    /// <summary>生成并打包为 zip 字节流。</summary>
    byte[] GenerateZip(GenTableConfig config);
}
