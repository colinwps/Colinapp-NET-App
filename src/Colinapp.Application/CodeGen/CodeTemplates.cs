using System.Text;

namespace Colinapp.Application.CodeGen;

/// <summary>
/// 代码模板。按 Colinapp 既有约定渲染各层产物（实体/配置/服务/控制器/前端）。
/// 使用 StringBuilder 拼接，避免与 Vue 模板的大括号冲突。
/// </summary>
internal static class CodeTemplates
{
    private static IEnumerable<GenColumnConfig> Biz(GenTableConfig c) => c.Columns.Where(x => !x.IsBase);

    private static string CsType(GenColumnConfig c)
    {
        if (c.CsharpType == "string") return c.Nullable ? "string?" : "string";
        return c.Nullable ? c.CsharpType + "?" : c.CsharpType;
    }

    private static string CsInit(GenColumnConfig c)
        => c is { CsharpType: "string", Nullable: false } ? " = string.Empty;" : string.Empty;

    private static string TsType(GenColumnConfig c)
        => c.Nullable && c.TsType != "string" ? c.TsType + " | null" : c.TsType;

    // ---------- 实体 ----------
    public static string Entity(GenTableConfig c)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Colinapp.Domain.Entities.{c.ModuleName};");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>{c.FunctionName}</summary>");
        sb.AppendLine($"public class {c.ClassName} : EntityBase");
        sb.AppendLine("{");
        foreach (var col in Biz(c))
        {
            sb.AppendLine($"    /// <summary>{col.Label}</summary>");
            sb.AppendLine($"    public {CsType(col)} {col.FieldName} {{ get; set; }}{CsInit(col)}");
            sb.AppendLine();
        }
        TrimTrailingBlank(sb);
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ---------- EF 配置 ----------
    public static string Configuration(GenTableConfig c)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using Colinapp.Domain.Entities.{c.ModuleName};");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
        sb.AppendLine();
        sb.AppendLine("namespace Colinapp.Infrastructure.Persistence.Configurations;");
        sb.AppendLine();
        sb.AppendLine($"public class {c.ClassName}Configuration : IEntityTypeConfiguration<{c.ClassName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public void Configure(EntityTypeBuilder<{c.ClassName}> builder)");
        sb.AppendLine("    {");
        sb.AppendLine($"        builder.ToTable(\"{c.TableName}\");");
        sb.AppendLine("        builder.HasKey(x => x.Id);");
        sb.AppendLine();
        foreach (var col in Biz(c).Where(x => x.CsharpType == "string"))
        {
            var line = $"        builder.Property(x => x.{col.FieldName})";
            if (col.HtmlType == "textarea" || col.MaxLength is null or > 8000)
                line += ".HasColumnType(\"text\")";
            else if (col.MaxLength is > 0)
                line += $".HasMaxLength({col.MaxLength})";
            if (col.IsRequired) line += ".IsRequired()";
            line += ";";
            sb.AppendLine(line);
        }
        sb.AppendLine();
        sb.AppendLine("        builder.HasIndex(x => x.CreatedTime);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ---------- 服务 + DTO ----------
    public static string Service(GenTableConfig c)
    {
        var cls = c.ClassName;
        var queryCols = Biz(c).Where(x => x.IsQuery).ToList();
        var formCols = Biz(c).Where(x => x.IsForm).ToList();
        var listCols = Biz(c).Where(x => x.IsList).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("using Colinapp.Application.Common;");
        sb.AppendLine($"using Colinapp.Domain.Entities.{c.ModuleName};");
        sb.AppendLine("using Colinapp.Shared.Exceptions;");
        sb.AppendLine("using Colinapp.Shared.Models;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace Colinapp.Application.{c.ModuleName};");
        sb.AppendLine();
        // Query
        sb.AppendLine("// ---------- DTO ----------");
        sb.AppendLine();
        sb.AppendLine($"public class {cls}Query : PagedRequest");
        sb.AppendLine("{");
        foreach (var col in queryCols)
            sb.AppendLine($"    public {CsType(MakeNullable(col))} {col.FieldName} {{ get; set; }}");
        sb.AppendLine("}");
        sb.AppendLine();
        // ListItem
        sb.AppendLine($"public class {cls}ListItemDto");
        sb.AppendLine("{");
        sb.AppendLine("    public long Id { get; set; }");
        foreach (var col in listCols)
            sb.AppendLine($"    public {CsType(col)} {col.FieldName} {{ get; set; }}{CsInit(col)}");
        sb.AppendLine("    public DateTime CreatedTime { get; set; }");
        sb.AppendLine("}");
        sb.AppendLine();
        // Save
        sb.AppendLine($"public class {cls}SaveDto");
        sb.AppendLine("{");
        foreach (var col in formCols)
            sb.AppendLine($"    public {CsType(col)} {col.FieldName} {{ get; set; }}{CsInit(col)}");
        sb.AppendLine("}");
        sb.AppendLine();
        // Interface
        sb.AppendLine("// ---------- Service ----------");
        sb.AppendLine();
        sb.AppendLine($"public interface I{cls}Service");
        sb.AppendLine("{");
        sb.AppendLine($"    Task<PagedResult<{cls}ListItemDto>> GetPagedAsync({cls}Query query, CancellationToken ct = default);");
        sb.AppendLine($"    Task<{cls}ListItemDto> GetAsync(long id, CancellationToken ct = default);");
        sb.AppendLine($"    Task<long> CreateAsync({cls}SaveDto dto, CancellationToken ct = default);");
        sb.AppendLine($"    Task UpdateAsync(long id, {cls}SaveDto dto, CancellationToken ct = default);");
        sb.AppendLine("    Task DeleteAsync(long id, CancellationToken ct = default);");
        sb.AppendLine("}");
        sb.AppendLine();
        // Impl
        sb.AppendLine($"public class {cls}Service(IAppDbContext db) : I{cls}Service");
        sb.AppendLine("{");
        // GetPaged
        sb.AppendLine($"    public async Task<PagedResult<{cls}ListItemDto>> GetPagedAsync({cls}Query query, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var q = db.Set<{cls}>().AsQueryable();");
        sb.AppendLine();
        foreach (var col in queryCols)
        {
            var field = col.FieldName;
            if (col.CsharpType == "string")
                sb.AppendLine($"        if (!string.IsNullOrWhiteSpace(query.{field}))\n            q = q.Where(x => x.{field}{(col.Nullable ? "!" : "")}.Contains(query.{field}!));");
            else
                sb.AppendLine($"        if (query.{field} is {{ }} v{field})\n            q = q.Where(x => x.{field} == v{field});");
        }
        sb.AppendLine();
        sb.AppendLine("        var total = await q.CountAsync(ct);");
        sb.AppendLine("        var rows = await q.OrderByDescending(x => x.Id)");
        sb.AppendLine("            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);");
        sb.AppendLine();
        sb.AppendLine($"        var items = rows.Select(ToDto).ToList();");
        sb.AppendLine($"        return new PagedResult<{cls}ListItemDto>(items, total, query.PageIndex, query.PageSize);");
        sb.AppendLine("    }");
        sb.AppendLine();
        // Get
        sb.AppendLine($"    public async Task<{cls}ListItemDto> GetAsync(long id, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await db.Set<{cls}>().FirstOrDefaultAsync(x => x.Id == id, ct)");
        sb.AppendLine($"            ?? throw BusinessException.NotFound(\"{c.FunctionName}不存在\");");
        sb.AppendLine("        return ToDto(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
        // Create
        sb.AppendLine($"    public async Task<long> CreateAsync({cls}SaveDto dto, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = new {cls}");
        sb.AppendLine("        {");
        foreach (var col in formCols)
            sb.AppendLine($"            {col.FieldName} = dto.{col.FieldName},");
        sb.AppendLine("        };");
        sb.AppendLine($"        db.Set<{cls}>().Add(entity);");
        sb.AppendLine("        await db.SaveChangesAsync(ct);");
        sb.AppendLine("        return entity.Id;");
        sb.AppendLine("    }");
        sb.AppendLine();
        // Update
        sb.AppendLine($"    public async Task UpdateAsync(long id, {cls}SaveDto dto, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await db.Set<{cls}>().FirstOrDefaultAsync(x => x.Id == id, ct)");
        sb.AppendLine($"            ?? throw BusinessException.NotFound(\"{c.FunctionName}不存在\");");
        foreach (var col in formCols)
            sb.AppendLine($"        entity.{col.FieldName} = dto.{col.FieldName};");
        sb.AppendLine("        await db.SaveChangesAsync(ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
        // Delete
        sb.AppendLine("    public async Task DeleteAsync(long id, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await db.Set<{cls}>().FirstOrDefaultAsync(x => x.Id == id, ct)");
        sb.AppendLine($"            ?? throw BusinessException.NotFound(\"{c.FunctionName}不存在\");");
        sb.AppendLine($"        db.Set<{cls}>().Remove(entity);");
        sb.AppendLine("        await db.SaveChangesAsync(ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
        // ToDto
        sb.AppendLine($"    private static {cls}ListItemDto ToDto({cls} x) => new()");
        sb.AppendLine("    {");
        sb.AppendLine("        Id = x.Id,");
        foreach (var col in listCols)
            sb.AppendLine($"        {col.FieldName} = x.{col.FieldName},");
        sb.AppendLine("        CreatedTime = x.CreatedTime,");
        sb.AppendLine("    };");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ---------- 控制器 ----------
    public static string Controller(GenTableConfig c)
    {
        var cls = c.ClassName;
        var perm = c.PermissionPrefix;
        var sb = new StringBuilder();
        sb.AppendLine("using Colinapp.Api.Authorization;");
        sb.AppendLine($"using Colinapp.Application.{c.ModuleName};");
        sb.AppendLine("using Colinapp.Shared.Common;");
        sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine();
        sb.AppendLine("namespace Colinapp.Api.Controllers;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>{c.FunctionName}管理</summary>");
        sb.AppendLine("[ApiController]");
        sb.AppendLine("[Authorize]");
        sb.AppendLine($"[Route(\"api/{c.BusinessName}\")]");
        sb.AppendLine($"public class {cls}Controller(I{cls}Service service) : ControllerBase");
        sb.AppendLine("{");
        sb.AppendLine("    [HttpGet]");
        sb.AppendLine($"    [HasPermission(\"{perm}:list\")]");
        sb.AppendLine($"    public async Task<ApiResult> List([FromQuery] {cls}Query query, CancellationToken ct)");
        sb.AppendLine("        => ApiResult.Ok(await service.GetPagedAsync(query, ct));");
        sb.AppendLine();
        sb.AppendLine("    [HttpGet(\"{id:long}\")]");
        sb.AppendLine($"    [HasPermission(\"{perm}:query\")]");
        sb.AppendLine("    public async Task<ApiResult> Get(long id, CancellationToken ct)");
        sb.AppendLine("        => ApiResult.Ok(await service.GetAsync(id, ct));");
        sb.AppendLine();
        sb.AppendLine("    [HttpPost]");
        sb.AppendLine($"    [HasPermission(\"{perm}:add\")]");
        sb.AppendLine($"    public async Task<ApiResult> Create([FromBody] {cls}SaveDto dto, CancellationToken ct)");
        sb.AppendLine("        => ApiResult.Ok(await service.CreateAsync(dto, ct));");
        sb.AppendLine();
        sb.AppendLine("    [HttpPut(\"{id:long}\")]");
        sb.AppendLine($"    [HasPermission(\"{perm}:edit\")]");
        sb.AppendLine($"    public async Task<ApiResult> Update(long id, [FromBody] {cls}SaveDto dto, CancellationToken ct)");
        sb.AppendLine("    {");
        sb.AppendLine("        await service.UpdateAsync(id, dto, ct);");
        sb.AppendLine("        return ApiResult.Ok();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [HttpDelete(\"{id:long}\")]");
        sb.AppendLine($"    [HasPermission(\"{perm}:remove\")]");
        sb.AppendLine("    public async Task<ApiResult> Delete(long id, CancellationToken ct)");
        sb.AppendLine("    {");
        sb.AppendLine("        await service.DeleteAsync(id, ct);");
        sb.AppendLine("        return ApiResult.Ok();");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ---------- 前端 api.ts ----------
    public static string ApiTs(GenTableConfig c)
    {
        var cls = c.ClassName;
        var bn = c.BusinessName;
        var listCols = Biz(c).Where(x => x.IsList).ToList();
        var formCols = Biz(c).Where(x => x.IsForm).ToList();
        var queryCols = Biz(c).Where(x => x.IsQuery).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("import { http } from '@/utils/request'");
        sb.AppendLine("import type { PagedQuery, PagedResult } from './types'");
        sb.AppendLine();
        sb.AppendLine($"export interface {cls} {{");
        sb.AppendLine("  id: number");
        foreach (var col in listCols)
            sb.AppendLine($"  {Camel(col.FieldName)}: {TsType(col)}");
        sb.AppendLine("  createdTime: string");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"export interface {cls}Query extends PagedQuery {{");
        foreach (var col in queryCols)
            sb.AppendLine($"  {Camel(col.FieldName)}?: {col.TsType}");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"export interface {cls}SaveDto {{");
        foreach (var col in formCols)
            sb.AppendLine($"  {Camel(col.FieldName)}: {TsType(col)}");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"export const get{cls}List = (params: {cls}Query) =>");
        sb.AppendLine($"  http.get<PagedResult<{cls}>>('/{bn}', params)");
        sb.AppendLine($"export const get{cls} = (id: number) => http.get<{cls}>(`/{bn}/${{id}}`)");
        sb.AppendLine($"export const create{cls} = (data: {cls}SaveDto) => http.post('/{bn}', data)");
        sb.AppendLine($"export const update{cls} = (id: number, data: {cls}SaveDto) => http.put(`/{bn}/${{id}}`, data)");
        sb.AppendLine($"export const delete{cls} = (id: number) => http.delete(`/{bn}/${{id}}`)");
        return sb.ToString();
    }

    // ---------- 前端 index.vue ----------
    public static string VueView(GenTableConfig c)
    {
        var cls = c.ClassName;
        var listCols = Biz(c).Where(x => x.IsList).ToList();
        var formCols = Biz(c).Where(x => x.IsForm).ToList();
        var queryCols = Biz(c).Where(x => x.IsQuery).ToList();
        var perm = c.PermissionPrefix;
        var fn = c.FunctionName;

        var sb = new StringBuilder();
        sb.AppendLine("<template>");
        sb.AppendLine("  <div class=\"page-container\">");
        sb.AppendLine("    <el-card>");
        sb.AppendLine("      <div class=\"table-toolbar\">");
        foreach (var col in queryCols)
            sb.AppendLine($"        <el-input v-model=\"query.{Camel(col.FieldName)}\" placeholder=\"{col.Label}\" clearable style=\"width: 180px\" @keyup.enter=\"reload\" />");
        sb.AppendLine("        <el-button type=\"primary\" @click=\"reload\">查询</el-button>");
        sb.AppendLine($"        <el-button v-auth=\"'{perm}:add'\" type=\"success\" @click=\"openCreate\">新增</el-button>");
        sb.AppendLine("      </div>");
        sb.AppendLine();
        sb.AppendLine("      <el-table v-loading=\"loading\" :data=\"list\" border>");
        foreach (var col in listCols)
        {
            var f = Camel(col.FieldName);
            if (col.CsharpType == "bool")
            {
                sb.AppendLine($"        <el-table-column label=\"{col.Label}\" width=\"100\">");
                sb.AppendLine("          <template #default=\"{ row }\">");
                sb.AppendLine($"            <el-tag :type=\"row.{f} ? 'success' : 'info'\">{{{{ row.{f} ? '是' : '否' }}}}</el-tag>");
                sb.AppendLine("          </template>");
                sb.AppendLine("        </el-table-column>");
            }
            else if (col.CsharpType == "DateTime")
            {
                sb.AppendLine($"        <el-table-column label=\"{col.Label}\" width=\"170\">");
                sb.AppendLine($"          <template #default=\"{{ row }}\">{{{{ formatTime(row.{f}) }}}}</template>");
                sb.AppendLine("        </el-table-column>");
            }
            else
            {
                sb.AppendLine($"        <el-table-column prop=\"{f}\" label=\"{col.Label}\" min-width=\"140\" show-overflow-tooltip />");
            }
        }
        sb.AppendLine("        <el-table-column label=\"操作\" width=\"140\" fixed=\"right\">");
        sb.AppendLine("          <template #default=\"{ row }\">");
        sb.AppendLine($"            <el-button v-auth=\"'{perm}:edit'\" link type=\"primary\" @click=\"openEdit(row as {cls})\">编辑</el-button>");
        sb.AppendLine($"            <el-button v-auth=\"'{perm}:remove'\" link type=\"danger\" @click=\"onDelete(row as {cls})\">删除</el-button>");
        sb.AppendLine("          </template>");
        sb.AppendLine("        </el-table-column>");
        sb.AppendLine("      </el-table>");
        sb.AppendLine();
        sb.AppendLine("      <div class=\"pagination-wrapper\">");
        sb.AppendLine("        <el-pagination v-model:current-page=\"query.pageIndex\" v-model:page-size=\"query.pageSize\" :total=\"total\" layout=\"total, prev, pager, next\" @current-change=\"loadData\" />");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </el-card>");
        sb.AppendLine();
        sb.AppendLine($"    <el-dialog v-model=\"dialogVisible\" :title=\"editId ? '编辑{fn}' : '新增{fn}'\" width=\"600px\">");
        sb.AppendLine("      <el-form ref=\"formRef\" :model=\"form\" :rules=\"rules\" label-width=\"100px\">");
        foreach (var col in formCols)
        {
            var f = Camel(col.FieldName);
            var control = col.HtmlType switch
            {
                "textarea" => $"<el-input v-model=\"form.{f}\" type=\"textarea\" :rows=\"4\" />",
                "number" => $"<el-input-number v-model=\"form.{f}\" :controls=\"false\" style=\"width: 100%\" />",
                "date" => $"<el-date-picker v-model=\"form.{f}\" type=\"datetime\" value-format=\"YYYY-MM-DD HH:mm:ss\" style=\"width: 100%\" />",
                "switch" => $"<el-switch v-model=\"form.{f}\" />",
                _ => $"<el-input v-model=\"form.{f}\" />",
            };
            sb.AppendLine($"        <el-form-item label=\"{col.Label}\" prop=\"{f}\">{control}</el-form-item>");
        }
        sb.AppendLine("      </el-form>");
        sb.AppendLine("      <template #footer>");
        sb.AppendLine("        <el-button @click=\"dialogVisible = false\">取消</el-button>");
        sb.AppendLine("        <el-button type=\"primary\" :loading=\"saving\" @click=\"onSubmit\">确定</el-button>");
        sb.AppendLine("      </template>");
        sb.AppendLine("    </el-dialog>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</template>");
        sb.AppendLine();
        // script
        sb.AppendLine("<script setup lang=\"ts\">");
        sb.AppendLine("import { reactive, ref } from 'vue'");
        sb.AppendLine("import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'");
        sb.AppendLine("import {");
        sb.AppendLine($"  get{cls}List, get{cls}, create{cls}, update{cls}, delete{cls},");
        sb.AppendLine($"  type {cls}, type {cls}Query, type {cls}SaveDto,");
        sb.AppendLine($"}} from '@/api/{c.BusinessName}'");
        sb.AppendLine("import { formatTime } from '@/utils/format'");
        sb.AppendLine();
        sb.AppendLine("const loading = ref(false)");
        sb.AppendLine($"const list = ref<{cls}[]>([])");
        sb.AppendLine("const total = ref(0)");
        sb.AppendLine($"const query = reactive<{cls}Query>({{ pageIndex: 1, pageSize: 20 }})");
        sb.AppendLine();
        sb.AppendLine("async function loadData() {");
        sb.AppendLine("  loading.value = true");
        sb.AppendLine("  try {");
        sb.AppendLine($"    const res = await get{cls}List(query)");
        sb.AppendLine("    list.value = res.data.items");
        sb.AppendLine("    total.value = res.data.total");
        sb.AppendLine("  } finally {");
        sb.AppendLine("    loading.value = false");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine("function reload() {");
        sb.AppendLine("  query.pageIndex = 1");
        sb.AppendLine("  loadData()");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("const dialogVisible = ref(false)");
        sb.AppendLine("const saving = ref(false)");
        sb.AppendLine("const editId = ref<number | null>(null)");
        sb.AppendLine("const formRef = ref<FormInstance>()");
        sb.AppendLine($"const defaultForm = (): {cls}SaveDto => ({{");
        foreach (var col in formCols)
            sb.AppendLine($"  {Camel(col.FieldName)}: {TsDefault(col)},");
        sb.AppendLine("})");
        sb.AppendLine($"const form = reactive<{cls}SaveDto>(defaultForm())");
        sb.AppendLine("const rules: FormRules = {");
        foreach (var col in formCols.Where(x => x.IsRequired))
            sb.AppendLine($"  {Camel(col.FieldName)}: [{{ required: true, message: '请填写{col.Label}', trigger: 'blur' }}],");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("function openCreate() {");
        sb.AppendLine("  editId.value = null");
        sb.AppendLine("  Object.assign(form, defaultForm())");
        sb.AppendLine("  formRef.value?.clearValidate()");
        sb.AppendLine("  dialogVisible.value = true");
        sb.AppendLine("}");
        sb.AppendLine($"async function openEdit(row: {cls}) {{");
        sb.AppendLine("  editId.value = row.id");
        sb.AppendLine($"  const detail = (await get{cls}(row.id)).data");
        sb.AppendLine("  Object.assign(form, {");
        foreach (var col in formCols)
            sb.AppendLine($"    {Camel(col.FieldName)}: detail.{Camel(col.FieldName)},");
        sb.AppendLine("  })");
        sb.AppendLine("  formRef.value?.clearValidate()");
        sb.AppendLine("  dialogVisible.value = true");
        sb.AppendLine("}");
        sb.AppendLine("async function onSubmit() {");
        sb.AppendLine("  if (!formRef.value) return");
        sb.AppendLine("  await formRef.value.validate()");
        sb.AppendLine("  saving.value = true");
        sb.AppendLine("  try {");
        sb.AppendLine($"    if (editId.value) await update{cls}(editId.value, form)");
        sb.AppendLine($"    else await create{cls}(form)");
        sb.AppendLine("    ElMessage.success('保存成功')");
        sb.AppendLine("    dialogVisible.value = false");
        sb.AppendLine("    loadData()");
        sb.AppendLine("  } finally {");
        sb.AppendLine("    saving.value = false");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine($"async function onDelete(row: {cls}) {{");
        sb.AppendLine($"  await ElMessageBox.confirm('确认删除该{fn}？', '提示', {{ type: 'warning' }})");
        sb.AppendLine($"  await delete{cls}(row.id)");
        sb.AppendLine("  ElMessage.success('删除成功')");
        sb.AppendLine("  loadData()");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("loadData()");
        sb.AppendLine("</script>");
        return sb.ToString();
    }

    // ---------- 菜单注册片段 ----------
    public static string MenuSnippet(GenTableConfig c)
    {
        var actions = "[\"query\", \"add\", \"edit\", \"remove\"]";
        var component = $"{c.ModuleName.ToLowerInvariant()}/{c.BusinessName}/index";
        var sb = new StringBuilder();
        sb.AppendLine("// 在 MenuSeeder.SeedAsync 中，挂到目标目录下（parentId 用对应 catalog.Id）：");
        sb.AppendLine($"await EnsureModuleAsync(db, catalog.Id, 99, \"{c.FunctionName}管理\", \"{c.BusinessName}\", \"{component}\", \"{c.PermissionPrefix}\",");
        sb.AppendLine($"    {actions}, ct);");
        return sb.ToString();
    }

    // ---------- 辅助 ----------
    private static string Camel(string pascal)
        => string.IsNullOrEmpty(pascal) ? pascal : char.ToLowerInvariant(pascal[0]) + pascal[1..];

    private static GenColumnConfig MakeNullable(GenColumnConfig col)
        => new()
        {
            FieldName = col.FieldName, CsharpType = col.CsharpType, TsType = col.TsType,
            Nullable = true, IsBase = col.IsBase,
        };

    private static string TsDefault(GenColumnConfig col) => col.TsType switch
    {
        "number" => "0",
        "boolean" => "false",
        _ => "''",
    };

    private static void TrimTrailingBlank(StringBuilder sb)
    {
        while (sb.Length >= 2 && sb[^1] == '\n')
        {
            // 去掉末尾多余空行
            var s = sb.ToString();
            var trimmed = s.TrimEnd('\r', '\n');
            sb.Clear();
            sb.Append(trimmed).Append('\n');
            break;
        }
    }
}
