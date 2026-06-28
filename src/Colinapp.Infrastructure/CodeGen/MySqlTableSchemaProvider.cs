using System.Data.Common;
using Colinapp.Application.CodeGen;
using Colinapp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Infrastructure.CodeGen;

/// <summary>
/// 基于 MySQL information_schema 的表结构内省。复用 AppDbContext 的连接，仅读当前库。
/// </summary>
public class MySqlTableSchemaProvider(AppDbContext db) : ITableSchemaProvider
{
    public async Task<List<DbTableInfo>> GetTablesAsync(string? keyword, CancellationToken ct = default)
    {
        var conn = db.Database.GetDbConnection();
        await EnsureOpenAsync(conn, ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT TABLE_NAME, TABLE_COMMENT, CREATE_TIME
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'
              AND TABLE_NAME <> '__EFMigrationsHistory'
              AND (@kw = '' OR TABLE_NAME LIKE CONCAT('%', @kw, '%') OR TABLE_COMMENT LIKE CONCAT('%', @kw, '%'))
            ORDER BY TABLE_NAME
            """;
        AddParam(cmd, "@kw", keyword ?? string.Empty);

        var result = new List<DbTableInfo>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new DbTableInfo
            {
                TableName = reader.GetString(0),
                TableComment = reader.IsDBNull(1) ? null : reader.GetString(1),
                CreateTime = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
            });
        }
        return result;
    }

    public async Task<List<DbColumnInfo>> GetColumnsAsync(string tableName, CancellationToken ct = default)
    {
        var conn = db.Database.GetDbConnection();
        await EnsureOpenAsync(conn, ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COLUMN_NAME, DATA_TYPE, COLUMN_TYPE, IS_NULLABLE, COLUMN_KEY, EXTRA,
                   COLUMN_COMMENT, ORDINAL_POSITION, CHARACTER_MAXIMUM_LENGTH
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t
            ORDER BY ORDINAL_POSITION
            """;
        AddParam(cmd, "@t", tableName);

        var result = new List<DbColumnInfo>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new DbColumnInfo
            {
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1),
                ColumnType = reader.GetString(2),
                IsNullable = string.Equals(reader.GetString(3), "YES", StringComparison.OrdinalIgnoreCase),
                ColumnKey = reader.IsDBNull(4) ? null : reader.GetString(4),
                Extra = reader.IsDBNull(5) ? null : reader.GetString(5),
                ColumnComment = reader.IsDBNull(6) ? null : reader.GetString(6),
                OrdinalPosition = Convert.ToInt32(reader.GetValue(7)),
                MaxLength = reader.IsDBNull(8) ? null : Convert.ToInt64(reader.GetValue(8)),
            });
        }
        return result;
    }

    private static async Task EnsureOpenAsync(DbConnection conn, CancellationToken ct)
    {
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);
    }

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
