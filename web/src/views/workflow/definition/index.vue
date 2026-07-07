<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="名称/编码" clearable style="width: 200px" @keyup.enter="reload" />
        <el-select v-model="query.enabled" placeholder="状态" clearable style="width: 120px">
          <el-option :value="true" label="启用" />
          <el-option :value="false" label="停用" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'wf:def:add'" type="success" @click="openCreate">新增</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="code" label="编码" width="140" />
        <el-table-column prop="name" label="名称" width="160" />
        <el-table-column label="审批节点" min-width="240">
          <template #default="{ row }">
            <el-tag v-for="node in getApprovalChain((row as WorkflowDefinition).graph)" :key="node.id" size="small" style="margin-right: 4px">
              {{ node.name }}({{ node.props.approveMode === ApproveMode.All ? '会签' : '或签' }})
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.enabled ? 'success' : 'info'">{{ row.enabled ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" width="160" show-overflow-tooltip />
        <el-table-column label="创建时间" width="170">
          <template #default="{ row }">{{ formatTime(row.createdTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="190" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'wf:def:edit'" link type="success" @click="openDesigner(row as WorkflowDefinition)">设计</el-button>
            <el-button v-auth="'wf:def:edit'" link type="primary" @click="openEdit(row as WorkflowDefinition)">编辑</el-button>
            <el-button v-auth="'wf:def:remove'" link type="danger" @click="onDelete(row as WorkflowDefinition)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="loadData" />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editId ? '编辑流程' : '新增流程'" width="760px" top="6vh">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="编码" prop="code"><el-input v-model="form.code" placeholder="如 leave" /></el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="名称" prop="name"><el-input v-model="form.name" placeholder="如 请假审批" /></el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注"><el-input v-model="form.remark" /></el-form-item>
        <el-form-item label="启用"><el-switch v-model="form.enabled" /></el-form-item>

        <el-form-item label="审批节点" required>
          <div class="node-list">
            <div v-for="(node, i) in form.nodes" :key="i" class="node-item">
              <span class="node-index">{{ i + 1 }}</span>
              <el-input v-model="node.name" placeholder="节点名称" style="width: 130px" />
              <el-select v-model="node.approveMode" style="width: 90px">
                <el-option :value="ApproveMode.Any" label="或签" />
                <el-option :value="ApproveMode.All" label="会签" />
              </el-select>
              <el-select v-model="node.approverType" style="width: 90px" @change="node.approverIds = []">
                <el-option :value="ApproverType.Users" label="用户" />
                <el-option :value="ApproverType.Role" label="角色" />
              </el-select>
              <el-select v-model="node.approverIds" multiple collapse-tags filterable
                :placeholder="node.approverType === ApproverType.Role ? '选择角色' : '选择用户'" style="flex: 1">
                <template v-if="node.approverType === ApproverType.Role">
                  <el-option v-for="r in roleOptions" :key="r.id" :value="r.id" :label="r.name" />
                </template>
                <template v-else>
                  <el-option v-for="u in userOptions" :key="u.id" :value="u.id" :label="u.nickName" />
                </template>
              </el-select>
              <el-button link type="primary" :disabled="i === 0" @click="moveNode(i, -1)">上移</el-button>
              <el-button link type="primary" :disabled="i === form.nodes.length - 1" @click="moveNode(i, 1)">下移</el-button>
              <el-button link type="danger" @click="form.nodes.splice(i, 1)">删除</el-button>
            </div>
            <el-button type="primary" plain style="width: 100%" @click="addNode">+ 添加节点</el-button>
          </div>
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
  getDefinitions, getDefinition, createDefinition, updateDefinition, deleteDefinition,
  getApprovalChain, buildLinearGraph, decomposeLinearGraph,
  ApproveMode, ApproverType,
  type WorkflowDefinition, type WorkflowDefinitionQuery, type WorkflowFormField, type LinearNode,
} from '@/api/workflow'
import { getUsers, type UserListItem } from '@/api/user'
import { getRoleOptions, type RoleItem } from '@/api/role'
import { formatTime } from '@/utils/format'

const router = useRouter()
const loading = ref(false)
const list = ref<WorkflowDefinition[]>([])
const total = ref(0)
const query = reactive<WorkflowDefinitionQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadData() {
  loading.value = true
  try {
    const res = await getDefinitions(query)
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

// 审批人选项
const userOptions = ref<UserListItem[]>([])
const roleOptions = ref<RoleItem[]>([])
async function loadOptions() {
  const [users, roles] = await Promise.all([
    getUsers({ pageIndex: 1, pageSize: 500 }),
    getRoleOptions(),
  ])
  userOptions.value = users.data.items
  roleOptions.value = roles.data
}

const dialogVisible = ref(false)
const saving = ref(false)
const editId = ref<number | null>(null)
const formRef = ref<FormInstance>()

// 桥接期：仍用线性列表编辑，保存时转成图；表单字段原样保留（阶段三提供编辑界面）
interface EditorForm {
  code: string
  name: string
  enabled: boolean
  remark?: string
  nodes: LinearNode[]
}
const editFormFields = ref<WorkflowFormField[]>([])
const defaultNode = (): LinearNode => ({
  name: '', approveMode: ApproveMode.Any, approverType: ApproverType.Users, approverIds: [],
})
const defaultForm = (): EditorForm => ({
  code: '', name: '', enabled: true, remark: '', nodes: [defaultNode()],
})
const form = reactive<EditorForm>(defaultForm())
const rules: FormRules = {
  code: [{ required: true, message: '请输入编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
}

function addNode() {
  form.nodes.push(defaultNode())
}
function moveNode(i: number, delta: number) {
  const target = i + delta
  const [node] = form.nodes.splice(i, 1)
  form.nodes.splice(target, 0, node)
}

function openDesigner(row: WorkflowDefinition) {
  router.push(`/workflow/designer/${row.id}`)
}

function openCreate() {
  editId.value = null
  editFormFields.value = []
  Object.assign(form, defaultForm())
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
async function openEdit(row: WorkflowDefinition) {
  const detail = (await getDefinition(row.id)).data
  const nodes = decomposeLinearGraph(detail.graph)
  if (!nodes) {
    ElMessage.warning('该流程包含条件/抄送节点，请使用流程设计器编辑（开发中）')
    return
  }
  editId.value = row.id
  editFormFields.value = detail.formFields
  Object.assign(form, {
    code: detail.code, name: detail.name, enabled: detail.enabled, remark: detail.remark ?? '',
    nodes,
  })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  if (form.nodes.length === 0) {
    ElMessage.warning('至少需要一个审批节点')
    return
  }
  for (const [i, node] of form.nodes.entries()) {
    if (!node.name.trim()) {
      ElMessage.warning(`第 ${i + 1} 个节点名称不能为空`)
      return
    }
    if (node.approverIds.length === 0) {
      ElMessage.warning(`节点「${node.name}」未指定审批人`)
      return
    }
  }
  saving.value = true
  try {
    const dto = {
      code: form.code,
      name: form.name,
      enabled: form.enabled,
      remark: form.remark,
      graph: buildLinearGraph(form.nodes),
      formFields: editFormFields.value,
    }
    if (editId.value) await updateDefinition(editId.value, dto)
    else await createDefinition(dto)
    ElMessage.success('保存成功')
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}
async function onDelete(row: WorkflowDefinition) {
  await ElMessageBox.confirm(`确认删除流程「${row.name}」？`, '提示', { type: 'warning' })
  await deleteDefinition(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadData()
loadOptions()
</script>

<style scoped>
.node-list {
  width: 100%;
}
.node-item {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 8px;
}
.node-index {
  width: 20px;
  text-align: center;
  color: var(--el-text-color-secondary);
}
</style>
