<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="名称/编码" clearable style="width: 200px" @keyup.enter="reload" />
        <el-select v-model="query.status" placeholder="状态" clearable style="width: 120px">
          <el-option v-for="(v, k) in formStatusMap" :key="k" :value="Number(k)" :label="v.label" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'form:def:add'" type="success" @click="openCreate">新增</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="code" label="编码" width="140" />
        <el-table-column prop="name" label="名称" width="160" />
        <el-table-column prop="description" label="描述" min-width="180" show-overflow-tooltip />
        <el-table-column label="字段数" width="80" align="center">
          <template #default="{ row }">{{ (row as FormDefinition).fields.length }}</template>
        </el-table-column>
        <el-table-column label="绑定流程" width="150">
          <template #default="{ row }">
            <el-tag v-if="row.workflowName" size="small" type="warning">{{ row.workflowName }}</el-tag>
            <span v-else style="color: var(--el-text-color-secondary)">-</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="formStatusMap[row.status]?.type">{{ formStatusMap[row.status]?.label }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="170">
          <template #default="{ row }">{{ formatTime(row.createdTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'form:def:edit'" link type="success" @click="openDesigner(row as FormDefinition)">设计</el-button>
            <el-button v-auth="'form:def:edit'" link type="primary" @click="openEdit(row as FormDefinition)">编辑</el-button>
            <el-button v-if="row.status !== FormStatus.Published" v-auth="'form:def:edit'"
              link type="warning" @click="onSetStatus(row as FormDefinition, FormStatus.Published)">发布</el-button>
            <el-button v-else v-auth="'form:def:edit'"
              link type="info" @click="onSetStatus(row as FormDefinition, FormStatus.Disabled)">停用</el-button>
            <el-button v-auth="'form:def:remove'" link type="danger" @click="onDelete(row as FormDefinition)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="loadData" />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editId ? '编辑表单' : '新增表单'" width="560px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="编码" prop="code"><el-input v-model="form.code" placeholder="如 expense" /></el-form-item>
        <el-form-item label="名称" prop="name"><el-input v-model="form.name" placeholder="如 报销申请" /></el-form-item>
        <el-form-item label="描述"><el-input v-model="form.description" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="图标">
          <el-input v-model="form.icon" placeholder="Element Plus 图标名，如 Wallet" />
        </el-form-item>
        <el-form-item label="绑定流程">
          <el-select v-model="form.workflowDefinitionId" clearable placeholder="不绑定（仅收集数据）" style="width: 100%">
            <el-option v-for="d in wfOptions" :key="d.id" :value="d.id" :label="d.name" />
          </el-select>
          <div class="form-tip">绑定后，填报提交将发起该流程的审批实例；流程条件分支引用的字段须在本表单中定义。</div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="onSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  getFormDefinitions, getFormDefinition, createFormDefinition, updateFormDefinition,
  setFormStatus, deleteFormDefinition, formStatusMap, FormStatus,
  type FormDefinition, type FormDefinitionQuery, type FormDefinitionSaveDto, type FormField,
} from '@/api/form'
import { getDefinitionOptions, type WorkflowDefinition } from '@/api/workflow'
import { formatTime } from '@/utils/format'

const router = useRouter()
const loading = ref(false)
const list = ref<FormDefinition[]>([])
const total = ref(0)
const query = reactive<FormDefinitionQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadData() {
  loading.value = true
  try {
    const res = await getFormDefinitions(query)
    list.value = res.data.items
    total.value = res.data.total
  } finally {
    loading.value = false
  }
}
function reload() {
  query.pageIndex = 1
  loadData()
}

// 可绑定的流程（仅启用）
const wfOptions = ref<WorkflowDefinition[]>([])
async function loadWfOptions() {
  wfOptions.value = (await getDefinitionOptions()).data
}

const dialogVisible = ref(false)
const saving = ref(false)
const editId = ref<number | null>(null)
const formRef = ref<FormInstance>()
// 基本信息编辑不动字段 schema（字段在设计器里编辑），保存时原样带回
const editFields = ref<FormField[]>([])

interface EditorForm {
  code: string
  name: string
  description?: string
  icon?: string
  workflowDefinitionId?: number | null
}
const defaultForm = (): EditorForm => ({
  code: '', name: '', description: '', icon: '', workflowDefinitionId: null,
})
const form = reactive<EditorForm>(defaultForm())
const rules: FormRules = {
  code: [{ required: true, message: '请输入编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
}

function openDesigner(row: FormDefinition) {
  router.push(`/form/designer/${row.id}`)
}
function openCreate() {
  editId.value = null
  editFields.value = []
  Object.assign(form, defaultForm())
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
async function openEdit(row: FormDefinition) {
  const detail = (await getFormDefinition(row.id)).data
  editId.value = row.id
  editFields.value = detail.fields
  Object.assign(form, {
    code: detail.code,
    name: detail.name,
    description: detail.description ?? '',
    icon: detail.icon ?? '',
    workflowDefinitionId: detail.workflowDefinitionId ?? null,
  })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  saving.value = true
  try {
    const dto: FormDefinitionSaveDto = {
      code: form.code,
      name: form.name,
      description: form.description,
      icon: form.icon,
      workflowDefinitionId: form.workflowDefinitionId ?? null,
      fields: editFields.value,
    }
    if (editId.value) {
      await updateFormDefinition(editId.value, dto)
    } else {
      const res = await createFormDefinition(dto)
      // 新建后直接进设计器补字段
      ElMessage.success('已创建，请设计表单字段')
      dialogVisible.value = false
      router.push(`/form/designer/${res.data}`)
      return
    }
    ElMessage.success('保存成功')
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}
async function onSetStatus(row: FormDefinition, status: FormStatus) {
  const label = status === FormStatus.Published ? '发布' : '停用'
  await ElMessageBox.confirm(`确认${label}表单「${row.name}」？`, '提示', { type: 'warning' })
  await setFormStatus(row.id, status)
  ElMessage.success(`已${label}`)
  loadData()
}
async function onDelete(row: FormDefinition) {
  await ElMessageBox.confirm(`确认删除表单「${row.name}」？`, '提示', { type: 'warning' })
  await deleteFormDefinition(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadData()
loadWfOptions()
</script>

<style scoped>
.form-tip {
  width: 100%;
  font-size: 12px;
  line-height: 1.5;
  color: var(--el-text-color-secondary);
}
</style>
