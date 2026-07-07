<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="标题" clearable style="width: 200px" @keyup.enter="reload" />
        <el-select v-model="query.status" placeholder="状态" clearable style="width: 120px">
          <el-option v-for="(v, k) in instanceStatusMap" :key="k" :value="Number(k)" :label="v.label" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'wf:instance:add'" type="success" @click="openSubmit">发起申请</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="title" label="标题" min-width="180" />
        <el-table-column prop="definitionName" label="流程" width="140" />
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="instanceStatusMap[row.status]?.type">{{ instanceStatusMap[row.status]?.label }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="当前节点" width="120">
          <template #default="{ row }">{{ row.currentNodeName || '-' }}</template>
        </el-table-column>
        <el-table-column label="发起时间" width="170">
          <template #default="{ row }">{{ formatTime(row.createdTime) }}</template>
        </el-table-column>
        <el-table-column label="结束时间" width="170">
          <template #default="{ row }">{{ row.finishTime ? formatTime(row.finishTime) : '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openDetail(row as WorkflowInstance)">详情</el-button>
            <el-button v-if="row.status === InstanceStatus.Returned" v-auth="'wf:instance:add'"
              link type="warning" @click="openResubmit(row as WorkflowInstance)">重新提交</el-button>
            <el-button v-if="row.status === InstanceStatus.Running || row.status === InstanceStatus.Returned"
              v-auth="'wf:instance:cancel'"
              link type="danger" @click="onCancel(row as WorkflowInstance)">撤销</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="loadData" />
      </div>
    </el-card>

    <el-dialog v-model="submitVisible" title="发起申请" width="600px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="流程" prop="definitionId">
          <el-select v-model="form.definitionId" placeholder="选择流程" style="width: 100%">
            <el-option v-for="d in defOptions" :key="d.id" :value="d.id" :label="d.name" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="selectedDef" label="审批节点">
          <el-tag v-for="node in getApprovalChain(selectedDef.graph)" :key="node.id" size="small" style="margin-right: 4px">
            {{ node.name }}({{ node.props.approveMode === ApproveMode.All ? '会签' : '或签' }})
          </el-tag>
        </el-form-item>
        <el-form-item label="标题" prop="title"><el-input v-model="form.title" /></el-form-item>

        <!-- 有表单字段定义时按字段渲染动态表单，否则退回自由文本 -->
        <template v-if="selectedDef && selectedDef.formFields.length">
          <el-form-item v-for="f in selectedDef.formFields" :key="f.key" :label="f.label" :required="f.required">
            <el-input-number v-if="f.type === 'number'" v-model="fieldValues[f.key]" style="width: 100%" :controls="false" />
            <el-date-picker v-else-if="f.type === 'date'" v-model="fieldValues[f.key]" type="date"
              value-format="YYYY-MM-DD" style="width: 100%" />
            <el-select v-else-if="f.type === 'select'" v-model="fieldValues[f.key]" clearable style="width: 100%">
              <el-option v-for="o in f.options" :key="o" :value="o" :label="o" />
            </el-select>
            <el-input v-else-if="f.type === 'textarea'" v-model="fieldValues[f.key]" type="textarea" :rows="3" />
            <el-input v-else v-model="fieldValues[f.key]" />
          </el-form-item>
        </template>
        <el-form-item v-else label="申请内容" prop="formData">
          <el-input v-model="form.formData" type="textarea" :rows="5" placeholder="填写申请事由、明细等" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="submitVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="onSubmit">提交</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="resubmitVisible" title="重新提交" width="600px">
      <el-form label-width="80px">
        <el-form-item label="标题">
          <el-input v-model="resubmitForm.title" />
        </el-form-item>

        <!-- 按实例快照的表单字段渲染动态表单，否则退回自由文本 -->
        <template v-if="resubmitFields.length">
          <el-form-item v-for="f in resubmitFields" :key="f.key" :label="f.label" :required="f.required">
            <el-input-number v-if="f.type === 'number'" v-model="resubmitValues[f.key]" style="width: 100%" :controls="false" />
            <el-date-picker v-else-if="f.type === 'date'" v-model="resubmitValues[f.key]" type="date"
              value-format="YYYY-MM-DD" style="width: 100%" />
            <el-select v-else-if="f.type === 'select'" v-model="resubmitValues[f.key]" clearable style="width: 100%">
              <el-option v-for="o in f.options" :key="o" :value="o" :label="o" />
            </el-select>
            <el-input v-else-if="f.type === 'textarea'" v-model="resubmitValues[f.key]" type="textarea" :rows="3" />
            <el-input v-else v-model="resubmitValues[f.key]" />
          </el-form-item>
        </template>
        <el-form-item v-else label="申请内容">
          <el-input v-model="resubmitForm.formData" type="textarea" :rows="5" placeholder="填写申请事由、明细等" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="resubmitVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="onResubmit">提交</el-button>
      </template>
    </el-dialog>

    <InstanceDetailDrawer v-model="detailVisible" :instance-id="detailId" />
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  getMyInstances, submitInstance, cancelInstance, resubmitInstance, getInstance,
  getDefinitionOptions, getApprovalChain,
  instanceStatusMap, ApproveMode, InstanceStatus,
  type WorkflowInstance, type WorkflowInstanceQuery, type WorkflowSubmitDto, type WorkflowDefinition,
  type WorkflowFormField,
} from '@/api/workflow'
import { formatTime } from '@/utils/format'
import InstanceDetailDrawer from '../components/InstanceDetailDrawer.vue'

const loading = ref(false)
const list = ref<WorkflowInstance[]>([])
const total = ref(0)
const query = reactive<WorkflowInstanceQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadData() {
  loading.value = true
  try {
    const res = await getMyInstances(query)
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

// 发起申请
const submitVisible = ref(false)
const saving = ref(false)
const formRef = ref<FormInstance>()
const defOptions = ref<WorkflowDefinition[]>([])
const form = reactive<WorkflowSubmitDto>({ definitionId: 0, title: '', formData: '' })
const rules: FormRules = {
  definitionId: [{ required: true, type: 'number', min: 1, message: '请选择流程', trigger: 'change' }],
  title: [{ required: true, message: '请输入标题', trigger: 'blur' }],
}
const selectedDef = computed(() => defOptions.value.find(d => d.id === form.definitionId))

// 动态表单值：切换流程时按字段定义重置（值类型随控件而变，序列化时原样带出）
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const fieldValues = reactive<Record<string, any>>({})
watch(selectedDef, (def) => {
  Object.keys(fieldValues).forEach(k => delete fieldValues[k])
  def?.formFields.forEach(f => (fieldValues[f.key] = undefined))
})

async function openSubmit() {
  Object.assign(form, { definitionId: 0, title: '', formData: '' })
  formRef.value?.clearValidate()
  submitVisible.value = true
  if (defOptions.value.length === 0)
    defOptions.value = (await getDefinitionOptions()).data
}
async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  const fields = selectedDef.value?.formFields ?? []
  if (fields.length) {
    const missing = fields.find(f => f.required && (fieldValues[f.key] === undefined || fieldValues[f.key] === ''))
    if (missing) {
      ElMessage.warning(`请填写「${missing.label}」`)
      return
    }
    form.formData = JSON.stringify(fieldValues)
  }
  saving.value = true
  try {
    await submitInstance(form)
    ElMessage.success('已提交')
    submitVisible.value = false
    reload()
  } finally {
    saving.value = false
  }
}

// 重新提交（仅「已退回」）：加载实例快照的表单字段并回填原表单数据
const resubmitVisible = ref(false)
const resubmitId = ref(0)
const resubmitForm = reactive({ title: '', formData: '' })
const resubmitFields = ref<WorkflowFormField[]>([])
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const resubmitValues = reactive<Record<string, any>>({})

async function openResubmit(row: WorkflowInstance) {
  const detail = (await getInstance(row.id)).data
  resubmitId.value = row.id
  resubmitForm.title = detail.title
  resubmitForm.formData = detail.formData
  resubmitFields.value = detail.formFields ?? []
  Object.keys(resubmitValues).forEach(k => delete resubmitValues[k])
  if (resubmitFields.value.length) {
    let parsed: Record<string, unknown> = {}
    try { parsed = JSON.parse(detail.formData || '{}') } catch { /* 自由文本时留空 */ }
    resubmitFields.value.forEach(f => (resubmitValues[f.key] = parsed[f.key]))
  }
  resubmitVisible.value = true
}
async function onResubmit() {
  if (!resubmitForm.title.trim()) {
    ElMessage.warning('请输入标题')
    return
  }
  let formData = resubmitForm.formData
  if (resubmitFields.value.length) {
    const missing = resubmitFields.value.find(
      f => f.required && (resubmitValues[f.key] === undefined || resubmitValues[f.key] === ''))
    if (missing) {
      ElMessage.warning(`请填写「${missing.label}」`)
      return
    }
    formData = JSON.stringify(resubmitValues)
  }
  saving.value = true
  try {
    await resubmitInstance(resubmitId.value, { title: resubmitForm.title, formData })
    ElMessage.success('已重新提交')
    resubmitVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}

async function onCancel(row: WorkflowInstance) {
  await ElMessageBox.confirm(`确认撤销申请「${row.title}」？`, '提示', { type: 'warning' })
  await cancelInstance(row.id)
  ElMessage.success('已撤销')
  loadData()
}

// 详情
const detailVisible = ref(false)
const detailId = ref<number | null>(null)
function openDetail(row: WorkflowInstance) {
  detailId.value = row.id
  detailVisible.value = true
}

loadData()
</script>
