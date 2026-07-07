using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Colinapp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先加新列并从旧下标列迁移数据（NodeId = "n" + 下标，与 DbInitializer 的旧图 JSON 转换器一致），再删旧列
            migrationBuilder.AddColumn<string>(
                name: "NodeId",
                table: "wf_task",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE wf_task SET NodeId = CONCAT('n', NodeIndex);");

            migrationBuilder.DropColumn(
                name: "NodeIndex",
                table: "wf_task");

            migrationBuilder.AddColumn<string>(
                name: "CurrentNodeId",
                table: "wf_instance",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE wf_instance SET CurrentNodeId = CONCAT('n', CurrentNodeIndex);");

            migrationBuilder.DropColumn(
                name: "CurrentNodeIndex",
                table: "wf_instance");

            migrationBuilder.RenameColumn(
                name: "NodesJson",
                table: "wf_instance",
                newName: "GraphJson");

            migrationBuilder.RenameColumn(
                name: "NodesJson",
                table: "wf_definition",
                newName: "GraphJson");

            migrationBuilder.AddColumn<string>(
                name: "FormFieldsJson",
                table: "wf_instance",
                type: "text",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FormFieldsJson",
                table: "wf_definition",
                type: "text",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wf_cc_record",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InstanceId = table.Column<long>(type: "bigint", nullable: false),
                    NodeId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NodeName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    UserName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReadTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wf_cc_record", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_wf_cc_record_InstanceId",
                table: "wf_cc_record",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_cc_record_UserId_ReadTime",
                table: "wf_cc_record",
                columns: new[] { "UserId", "ReadTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wf_cc_record");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "wf_task");

            migrationBuilder.DropColumn(
                name: "CurrentNodeId",
                table: "wf_instance");

            migrationBuilder.DropColumn(
                name: "FormFieldsJson",
                table: "wf_instance");

            migrationBuilder.DropColumn(
                name: "FormFieldsJson",
                table: "wf_definition");

            migrationBuilder.RenameColumn(
                name: "GraphJson",
                table: "wf_instance",
                newName: "NodesJson");

            migrationBuilder.RenameColumn(
                name: "GraphJson",
                table: "wf_definition",
                newName: "NodesJson");

            migrationBuilder.AddColumn<int>(
                name: "NodeIndex",
                table: "wf_task",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentNodeIndex",
                table: "wf_instance",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
