<template>
  <div class="designer">
    <!-- 工具栏 -->
    <div class="toolbar">
      <el-button @click="goBack">返回</el-button>
      <span class="def-name">{{ defName }}</span>
      <div class="toolbar-right">
        <el-button @click="fieldsDialogVisible = true">表单字段（{{ formFields.length }}）</el-button>
        <el-button @click="onValidate">校验</el-button>
        <el-button type="primary" :loading="saving" @click="onSave">保存</el-button>
      </div>
    </div>

    <div class="body">
      <!-- 左侧节点面板 -->
      <div class="palette">
        <div class="palette-title">节点</div>
        <div v-for="(item, i) in paletteItems" :key="i" class="palette-item"
          :class="`palette-${item.type}`" @mousedown="onPaletteDrag(item)">
          {{ item.label }}
        </div>
        <div class="palette-tip">拖拽到画布；节点边缘拉线连接；Backspace 删除选中项</div>
      </div>

      <!-- 画布 -->
      <div ref="canvasRef" class="canvas" />

      <!-- 右侧属性面板 -->
      <div class="props-panel">
        <template v-if="selectedNode">
          <div class="palette-title">节点属性</div>
          <el-form label-width="70px" label-position="top" size="small">
            <el-form-item label="名称">
              <el-input v-model="nodeForm.name" @change="applyNodeName" />
            </el-form-item>
            <template v-if="selectedNode.type === 'wf-approval'">
              <el-form-item label="审批方式">
                <el-radio-group v-model="nodeForm.approveMode" @change="applyNodeProps">
                  <el-radio :value="ApproveMode.Any">或签</el-radio>
                  <el-radio :value="ApproveMode.All">会签</el-radio>
                </el-radio-group>
              </el-form-item>
              <el-form-item label="审批人类型">
                <el-radio-group v-model="nodeForm.approverType" @change="nodeForm.approverIds = []; applyNodeProps()">
                  <el-radio :value="ApproverType.Users">用户</el-radio>
                  <el-radio :value="ApproverType.Role">角色</el-radio>
                </el-radio-group>
              </el-form-item>
              <el-form-item :label="nodeForm.approverType === ApproverType.Role ? '角色' : '用户'">
                <el-select v-model="nodeForm.approverIds" multiple filterable collapse-tags style="width: 100%"
                  @change="applyNodeProps">
                  <template v-if="nodeForm.approverType === ApproverType.Role">
                    <el-option v-for="r in roleOptions" :key="r.id" :value="r.id" :label="r.name" />
                  </template>
                  <template v-else>
                    <el-option v-for="u in userOptions" :key="u.id" :value="u.id" :label="u.nickName" />
                  </template>
                </el-select>
              </el-form-item>
              <el-form-item label="驳回策略">
                <el-select v-model="nodeForm.rejectStrategy" style="width: 100%" @change="applyNodeProps">
                  <el-option :value="RejectStrategy.RejectInstance" label="整体驳回（流程结束）" />
                  <el-option :value="RejectStrategy.BackToPrevious" label="退回上一审批节点" />
                  <el-option :value="RejectStrategy.BackToInitiator" label="退回发起人重新提交" />
                </el-select>
              </el-form-item>
              <el-form-item label="超时提醒（小时，0 = 不限时）">
                <el-input-number v-model="nodeForm.timeoutHours" :min="0" :max="720" style="width: 100%"
                  @change="applyNodeProps" />
              </el-form-item>
            </template>
            <template v-if="selectedNode.type === 'wf-cc'">
              <el-form-item label="抄送人">
                <el-select v-model="nodeForm.userIds" multiple filterable collapse-tags style="width: 100%"
                  @change="applyNodeProps">
                  <el-option v-for="u in userOptions" :key="u.id" :value="u.id" :label="u.nickName" />
                </el-select>
              </el-form-item>
            </template>
            <el-button type="danger" plain style="width: 100%" @click="deleteSelected">删除节点</el-button>
          </el-form>
        </template>

        <template v-else-if="selectedEdge">
          <div class="palette-title">连线属性</div>
          <el-form label-width="70px" label-position="top" size="small">
            <el-form-item label="名称">
              <el-input v-model="edgeForm.name" placeholder="连线标签（选填）" @change="applyEdgeProps" />
            </el-form-item>
            <template v-if="edgeFromCondition">
              <el-form-item label="默认分支（其它条件都不满足时走这条）">
                <el-switch v-model="edgeForm.isDefault" @change="applyEdgeProps" />
              </el-form-item>
              <template v-if="!edgeForm.isDefault">
                <el-form-item label="优先级（小者先判断）">
                  <el-input-number v-model="edgeForm.priority" :min="0" @change="applyEdgeProps" />
                </el-form-item>
                <el-form-item label="条件（全部满足）">
                  <div v-for="(rule, i) in edgeForm.rules" :key="i" class="rule-row">
                    <el-select v-if="formFields.length" v-model="rule.field" placeholder="字段" style="width: 90px"
                      @change="applyEdgeProps">
                      <el-option v-for="f in formFields" :key="f.key" :value="f.key" :label="f.label" />
                    </el-select>
                    <el-input v-else v-model="rule.field" placeholder="字段" style="width: 90px" @change="applyEdgeProps" />
                    <el-select v-model="rule.op" style="width: 82px" @change="applyEdgeProps">
                      <el-option v-for="op in OPS" :key="op.value" :value="op.value" :label="op.label" />
                    </el-select>
                    <el-input v-model="rule.value" placeholder="值" style="flex: 1" @change="applyEdgeProps" />
                    <el-button link type="danger" @click="edgeForm.rules.splice(i, 1); applyEdgeProps()">×</el-button>
                  </div>
                  <el-button size="small" plain style="width: 100%"
                    @click="edgeForm.rules.push({ field: '', op: 'eq', value: '' })">+ 添加条件</el-button>
                </el-form-item>
              </template>
            </template>
            <el-button type="danger" plain style="width: 100%" @click="deleteSelected">删除连线</el-button>
          </el-form>
        </template>

        <template v-else>
          <div class="palette-title">属性</div>
          <div class="palette-tip">选中节点或连线以编辑属性</div>
          <template v-if="issues.length">
            <div class="palette-title" style="color: var(--el-color-danger)">校验问题</div>
            <div v-for="(msg, i) in issues" :key="i" class="issue-item">{{ msg }}</div>
          </template>
        </template>
      </div>
    </div>

    <!-- 表单字段 -->
    <el-dialog v-model="fieldsDialogVisible" title="表单字段（发起申请时填写，条件分支据此判断）" width="720px">
      <el-table :data="formFields" border size="small">
        <el-table-column label="字段 key" width="130">
          <template #default="{ row }"><el-input v-model="row.key" size="small" placeholder="如 amount" /></template>
        </el-table-column>
        <el-table-column label="名称" width="120">
          <template #default="{ row }"><el-input v-model="row.label" size="small" placeholder="如 金额" /></template>
        </el-table-column>
        <el-table-column label="类型" width="110">
          <template #default="{ row }">
            <el-select v-model="row.type" size="small">
              <el-option value="text" label="文本" />
              <el-option value="textarea" label="多行文本" />
              <el-option value="number" label="数字" />
              <el-option value="date" label="日期" />
              <el-option value="select" label="下拉选项" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="必填" width="70">
          <template #default="{ row }"><el-switch v-model="row.required" size="small" /></template>
        </el-table-column>
        <el-table-column label="选项（逗号分隔，仅下拉）">
          <template #default="{ row }">
            <el-input :model-value="row.options.join(',')" size="small" :disabled="row.type !== 'select'"
              @update:model-value="(v: string) => row.options = v ? v.split(/[,，]/).map((s: string) => s.trim()).filter(Boolean) : []" />
          </template>
        </el-table-column>
        <el-table-column label="" width="60">
          <template #default="{ $index }">
            <el-button link type="danger" @click="formFields.splice($index, 1)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-button size="small" plain style="width: 100%; margin-top: 8px"
        @click="formFields.push({ key: '', label: '', type: 'text', required: false, options: [] })">+ 添加字段</el-button>
      <template #footer>
        <el-button type="primary" @click="fieldsDialogVisible = false">完成</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import LogicFlow from '@logicflow/core'
import '@logicflow/core/lib/index.css'
import {
  getDefinition, updateDefinition,
  ApproveMode, ApproverType, RejectStrategy,
  type WorkflowFormField, type WfRule,
} from '@/api/workflow'
import { getUsers, type UserListItem } from '@/api/user'
import { getRoleOptions, type RoleItem } from '@/api/role'
import { registerWfNodes, type WfNodeProperties } from './lfNodes'
import { toLogicFlow, fromLogicFlow, validateGraph, paletteItems, nodeDefaults, edgeLabel, toLfType } from './lfAdapter'

const route = useRoute()
const router = useRouter()
const defId = Number(route.params.id)

const canvasRef = ref<HTMLDivElement>()
let lf: LogicFlow | null = null

const defName = ref('')
const defMeta = reactive({ code: '', enabled: true, remark: '' as string | undefined })
const formFields = ref<WorkflowFormField[]>([])
const saving = ref(false)
const issues = ref<string[]>([])
const fieldsDialogVisible = ref(false)

const OPS = [
  { value: 'eq', label: '等于' }, { value: 'ne', label: '不等于' },
  { value: 'gt', label: '大于' }, { value: 'ge', label: '大于等于' },
  { value: 'lt', label: '小于' }, { value: 'le', label: '小于等于' },
  { value: 'contains', label: '包含' },
] as const

// ---- 选中状态 ----
interface SelectedItem { id: string; type: string }
const selectedNode = ref<SelectedItem | null>(null)
const selectedEdge = ref<SelectedItem | null>(null)
const edgeFromCondition = ref(false)
const nodeForm = reactive({
  name: '',
  approveMode: ApproveMode.Any,
  approverType: ApproverType.Users,
  approverIds: [] as number[],
  rejectStrategy: RejectStrategy.RejectInstance,
  timeoutHours: 0,
  userIds: [] as number[],
})
interface RuleRow { field: string; op: string; value: string }
const edgeForm = reactive({ name: '', priority: 0, isDefault: false, rules: [] as RuleRow[] })

// ---- 选项数据 ----
const userOptions = ref<UserListItem[]>([])
const roleOptions = ref<RoleItem[]>([])

onMounted(async () => {
  const [detail, users, roles] = await Promise.all([
    getDefinition(defId),
    getUsers({ pageIndex: 1, pageSize: 500 }),
    getRoleOptions(),
  ])
  userOptions.value = users.data.items
  roleOptions.value = roles.data
  const def = detail.data
  defName.value = def.name
  defMeta.code = def.code
  defMeta.enabled = def.enabled
  defMeta.remark = def.remark
  formFields.value = def.formFields.map(f => ({ ...f, options: [...f.options] }))

  lf = new LogicFlow({
    container: canvasRef.value!,
    grid: { size: 10, visible: true },
    keyboard: { enabled: true },
    edgeTextDraggable: true,
    adjustEdgeStartAndEnd: true,
  })
  registerWfNodes(lf)
  lf.setDefaultEdgeType('polyline')
  lf.render(toLogicFlow(def.graph))

  lf.on('node:click', ({ data }) => selectNode(data.id, data.type as string))
  lf.on('edge:click', ({ data }) => selectEdge(data.id))
  lf.on('blank:click', clearSelection)
  lf.on('node:delete,edge:delete', clearSelection)
  // 新画的连线补默认业务属性
  lf.on('edge:add', ({ data }) => {
    const props = (data.properties ?? {}) as Record<string, unknown>
    if (props.priority === undefined)
      lf!.setProperties(data.id, { name: '', priority: 0, isDefault: false, condition: null })
  })
})

onBeforeUnmount(() => {
  lf?.destroy()
  lf = null
})

function goBack() {
  router.push('/workflow/definition')
}

// ---- 面板拖入 ----
function onPaletteDrag(item: (typeof paletteItems)[number]) {
  lf?.dnd.startDrag({
    type: toLfType(item.type),
    text: item.label === '会签' ? '会签' : nodeDefaults[item.type].name,
    properties: { ...nodeDefaults[item.type].props, ...item.preset } as Record<string, unknown>,
  })
}

// ---- 选中与属性回填 ----
function selectNode(id: string, type: string) {
  const model = lf!.getNodeModelById(id)
  if (!model) return
  selectedEdge.value = null
  selectedNode.value = { id, type }
  const p = model.properties as WfNodeProperties
  nodeForm.name = typeof model.text === 'object' ? model.text.value : String(model.text ?? '')
  nodeForm.approveMode = p.approveMode ?? ApproveMode.Any
  nodeForm.approverType = (p.approverType as ApproverType) ?? ApproverType.Users
  nodeForm.approverIds = [...(p.approverIds ?? [])]
  nodeForm.rejectStrategy = (p.rejectStrategy as RejectStrategy) ?? RejectStrategy.RejectInstance
  nodeForm.timeoutHours = p.timeoutHours ?? 0
  nodeForm.userIds = [...(p.userIds ?? [])]
}

function selectEdge(id: string) {
  const model = lf!.getEdgeModelById(id)
  if (!model) return
  selectedNode.value = null
  selectedEdge.value = { id, type: model.type }
  const source = lf!.getNodeModelById(model.sourceNodeId)
  edgeFromCondition.value = (source?.type as string) === 'wf-condition'
  const p = model.properties as Record<string, unknown>
  edgeForm.name = (p.name as string) ?? ''
  edgeForm.priority = (p.priority as number) ?? 0
  edgeForm.isDefault = (p.isDefault as boolean) ?? false
  const cond = p.condition as { rules?: WfRule[] } | null
  edgeForm.rules = (cond?.rules ?? []).map(r => ({ field: r.field, op: r.op, value: String(r.value ?? '') }))
}

function clearSelection() {
  selectedNode.value = null
  selectedEdge.value = null
}

// ---- 属性应用到画布 ----
function applyNodeName() {
  if (selectedNode.value) lf!.updateText(selectedNode.value.id, nodeForm.name)
}
function applyNodeProps() {
  if (!selectedNode.value) return
  lf!.setProperties(selectedNode.value.id, {
    approveMode: nodeForm.approveMode,
    approverType: nodeForm.approverType,
    approverIds: [...nodeForm.approverIds],
    rejectStrategy: nodeForm.rejectStrategy,
    timeoutHours: nodeForm.timeoutHours,
    userIds: [...nodeForm.userIds],
  })
}
function applyEdgeProps() {
  if (!selectedEdge.value) return
  // 值为数字样式时转 number，让 gt/lt 等按数值比较
  const rules = edgeForm.rules
    .filter(r => r.field)
    .map(r => ({ field: r.field, op: r.op, value: /^-?\d+(\.\d+)?$/.test(r.value) ? Number(r.value) : r.value }))
  const condition = edgeForm.isDefault || rules.length === 0 ? null : { logic: 'and', rules }
  lf!.setProperties(selectedEdge.value.id, {
    name: edgeForm.name,
    priority: edgeForm.priority,
    isDefault: edgeForm.isDefault,
    condition,
  })
  lf!.updateText(selectedEdge.value.id, edgeLabel({
    name: edgeForm.name || null,
    isDefault: edgeForm.isDefault,
    condition: condition as never,
  }))
}

function deleteSelected() {
  if (selectedNode.value) lf!.deleteNode(selectedNode.value.id)
  else if (selectedEdge.value) lf!.deleteEdge(selectedEdge.value.id)
  clearSelection()
}

// ---- 校验与保存 ----
function runValidate(): boolean {
  const graph = fromLogicFlow(lf!.getGraphRawData())
  const { errors, nodeIds } = validateGraph(graph, formFields.value)
  // 清旧标红，再标新问题
  for (const n of graph.nodes) lf!.setProperties(n.id, { error: false })
  for (const id of nodeIds) lf!.setProperties(id, { error: true })
  issues.value = errors
  return errors.length === 0
}

function onValidate() {
  clearSelection()
  if (runValidate()) ElMessage.success('校验通过')
  else ElMessage.warning(`发现 ${issues.value.length} 个问题，见右侧面板`)
}

async function onSave() {
  clearSelection()
  if (!runValidate()) {
    ElMessage.warning(`存在 ${issues.value.length} 个问题，请先修正（见右侧面板）`)
    return
  }
  saving.value = true
  try {
    const graph = fromLogicFlow(lf!.getGraphRawData())
    await updateDefinition(defId, {
      code: defMeta.code,
      name: defName.value,
      enabled: defMeta.enabled,
      remark: defMeta.remark,
      graph,
      formFields: formFields.value,
    })
    ElMessage.success('保存成功')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.designer {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 90px);
}
.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  background: var(--el-bg-color);
  border-bottom: 1px solid var(--el-border-color-light);
}
.def-name {
  font-weight: 600;
}
.toolbar-right {
  margin-left: auto;
  display: flex;
  gap: 8px;
}
.body {
  flex: 1;
  display: flex;
  min-height: 0;
}
.palette {
  width: 140px;
  padding: 10px;
  border-right: 1px solid var(--el-border-color-light);
  background: var(--el-bg-color);
  overflow-y: auto;
}
.palette-title {
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 8px;
}
.palette-item {
  padding: 8px;
  margin-bottom: 8px;
  border: 1px solid var(--el-border-color);
  border-radius: 6px;
  text-align: center;
  font-size: 13px;
  cursor: grab;
  user-select: none;
  background: var(--el-bg-color);
}
.palette-item:hover {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
}
.palette-condition {
  border-style: dashed;
}
.palette-cc {
  border-style: dotted;
}
.palette-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.6;
  margin-top: 8px;
}
.canvas {
  flex: 1;
  min-width: 0;
}
.props-panel {
  width: 260px;
  padding: 10px;
  border-left: 1px solid var(--el-border-color-light);
  background: var(--el-bg-color);
  overflow-y: auto;
}
.rule-row {
  display: flex;
  gap: 4px;
  margin-bottom: 6px;
  width: 100%;
}
.issue-item {
  font-size: 12px;
  color: var(--el-color-danger);
  line-height: 1.8;
  border-bottom: 1px dashed var(--el-border-color-lighter);
}
</style>
