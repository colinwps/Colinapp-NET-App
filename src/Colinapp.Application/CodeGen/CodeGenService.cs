using System.IO.Compression;
using System.Text;

namespace Colinapp.Application.CodeGen;

public class CodeGenService(ITableSchemaProvider schema) : ICodeGenService
{
    /// <summary>
    /// 基类/审计列：由 EntityBase 提供，不生成业务属性。
    /// 同时覆盖 PascalCase（EF 默认列名）与 snake_case 两种命名约定。
    /// </summary>
    private static readonly HashSet<string> BaseColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "CreatedBy", "CreatedTime", "UpdatedBy", "UpdatedTime", "IsDeleted", "TenantId",
        "id", "created_by", "created_time", "updated_by", "updated_time", "is_deleted", "tenant_id",
    };

    public Task<List<DbTableInfo>> GetTablesAsync(string? keyword, CancellationToken ct = default)
        => schema.GetTablesAsync(keyword, ct);

    public async Task<GenTableConfig> BuildConfigAsync(string tableName, CancellationToken ct = default)
    {
        var columns = await schema.GetColumnsAsync(tableName, ct);

        // 去掉表名常见前缀（sys_/biz_/t_）作为类名基础
        var bare = StripPrefix(tableName);
        var className = NameHelper.ToPascal(bare);

        var config = new GenTableConfig
        {
            TableName = tableName,
            ClassName = className,
            BusinessName = NameHelper.ToCamel(bare),
            FunctionName = className,
            ModuleName = "Business",
            PermissionPrefix = $"biz:{NameHelper.ToCamel(bare)}",
            Columns = columns.Select(MapColumn).ToList(),
        };
        return config;
    }

    private static GenColumnConfig MapColumn(DbColumnInfo c)
    {
        var (csharp, ts) = MapType(c);
        var isPk = string.Equals(c.ColumnKey, "PRI", StringComparison.OrdinalIgnoreCase);
        var isBase = BaseColumns.Contains(c.ColumnName);
        var field = NameHelper.ToPascal(c.ColumnName);

        var html = csharp switch
        {
            "bool" => "switch",
            "DateTime" => "date",
            "int" or "long" or "decimal" or "double" => "number",
            _ when c.DataType is "text" or "longtext" or "mediumtext" => "textarea",
            _ => "input",
        };

        return new GenColumnConfig
        {
            ColumnName = c.ColumnName,
            FieldName = field,
            Label = string.IsNullOrWhiteSpace(c.ColumnComment) ? field : c.ColumnComment!,
            CsharpType = csharp,
            TsType = ts,
            Nullable = c.IsNullable,
            MaxLength = c.MaxLength,
            IsPk = isPk,
            IsBase = isBase,
            IsList = !isBase,
            IsForm = !isBase,
            IsQuery = false,
            IsRequired = !c.IsNullable && !isBase,
            HtmlType = html,
            QueryType = csharp == "string" ? "like" : "eq",
        };
    }

    private static (string csharp, string ts) MapType(DbColumnInfo c)
    {
        var dt = c.DataType.ToLowerInvariant();
        return dt switch
        {
            "tinyint" when c.ColumnType.Contains("tinyint(1)") => ("bool", "boolean"),
            "bit" => ("bool", "boolean"),
            "tinyint" or "smallint" or "mediumint" or "int" or "integer" => ("int", "number"),
            "bigint" => ("long", "number"),
            "decimal" or "numeric" => ("decimal", "number"),
            "float" => ("float", "number"),
            "double" => ("double", "number"),
            "datetime" or "timestamp" or "date" => ("DateTime", "string"),
            _ => ("string", "string"),
        };
    }

    public List<GeneratedFile> Generate(GenTableConfig config)
    {
        var area = config.ModuleName;
        var bn = config.BusinessName;
        return
        [
            new GeneratedFile
            {
                FileName = $"{config.ClassName}.cs", Language = "csharp",
                TargetPath = $"src/Colinapp.Domain/Entities/{area}/{config.ClassName}.cs",
                Content = CodeTemplates.Entity(config),
            },
            new GeneratedFile
            {
                FileName = $"{config.ClassName}Configuration.cs", Language = "csharp",
                TargetPath = $"src/Colinapp.Infrastructure/Persistence/Configurations/{config.ClassName}Configuration.cs",
                Content = CodeTemplates.Configuration(config),
            },
            new GeneratedFile
            {
                FileName = $"{config.ClassName}s.cs", Language = "csharp",
                TargetPath = $"src/Colinapp.Application/{area}/{config.ClassName}s.cs",
                Content = CodeTemplates.Service(config),
            },
            new GeneratedFile
            {
                FileName = $"{config.ClassName}Controller.cs", Language = "csharp",
                TargetPath = $"src/Colinapp.Api/Controllers/{config.ClassName}Controller.cs",
                Content = CodeTemplates.Controller(config),
            },
            new GeneratedFile
            {
                FileName = $"{bn}.ts", Language = "typescript",
                TargetPath = $"web/src/api/{bn}.ts",
                Content = CodeTemplates.ApiTs(config),
            },
            new GeneratedFile
            {
                FileName = "index.vue", Language = "vue",
                TargetPath = $"web/src/views/{area.ToLowerInvariant()}/{bn}/index.vue",
                Content = CodeTemplates.VueView(config),
            },
            new GeneratedFile
            {
                FileName = "MenuSeeder.snippet.cs", Language = "csharp",
                TargetPath = "（粘贴到 MenuSeeder.SeedAsync 对应目录下）",
                Content = CodeTemplates.MenuSnippet(config),
            },
        ];
    }

    public byte[] GenerateZip(GenTableConfig config)
    {
        var files = Generate(config);
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var f in files)
            {
                // zip 内按建议路径归档，菜单片段放根目录
                var entryPath = f.TargetPath.StartsWith("src/") || f.TargetPath.StartsWith("web/")
                    ? f.TargetPath
                    : f.FileName;
                var entry = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                writer.Write(f.Content);
            }
        }
        return ms.ToArray();
    }

    private static string StripPrefix(string table)
    {
        foreach (var prefix in new[] { "sys_", "biz_", "t_", "tb_" })
            if (table.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return table[prefix.Length..];
        return table;
    }
}

/// <summary>命名转换：snake_case ↔ PascalCase / camelCase。</summary>
public static class NameHelper
{
    public static string ToPascal(string name)
    {
        // 保留原有大小写（DB 列名可能已是 PascalCase），仅在分隔处拼接并首字母大写。
        var parts = name.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var p in parts)
            sb.Append(char.ToUpperInvariant(p[0])).Append(p[1..]);
        return sb.Length == 0 ? name : sb.ToString();
    }

    public static string ToCamel(string snake)
    {
        var pascal = ToPascal(snake);
        return pascal.Length == 0 ? pascal : char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }
}
