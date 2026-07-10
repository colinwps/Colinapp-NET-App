<template>
  <div class="designer">
    <!-- 工具栏 -->
    <div class="toolbar">
      <el-button @click="goBack">返回</el-button>
      <span class="def-name">{{ defName }}</span>
      <el-tag v-if="wfName" size="small" type="warning">绑定流程：{{ wfName }}</el-tag>
      <div class="toolbar-right">
        <el-button @click="openPreview">预览</el-button>
        <el-button type="primary" :loading="saving" @click="onSave">保存</el-button>
      </div>
    </div>

    <div class="body">
      <!-- 左侧字段面板 -->
      <div class="palette">
        <div class="palette-title">字段控件</div>
        <div v-for="(label, type) in fieldTypeMap" :key="type" class="palette-item" @click="addField(type)">
          {{ label }}
        </div>
        <div class="palette-tip">点击添加到表单；画布内拖拽卡片调整顺序</div>
      </div>

      <!-- 中间画布：字段卡片列表 -->
      <div class="canvas">
        <el-empty v-if="fields.length === 0" description="从左侧点击字段控件开始设计" />
        <div v-for="(f, i) in fields" :key="i" class="field-card"
          :class="{ selected: selectedIndex === i, dragging: dragIndex === i }"
          draggable="true"
          @click="selectedIndex = i"
          @dragstart="onDragStart(i, $event)"
          @dragover.prevent="onDragOver(i)"
          @dragend="dragIndex = null">
          <div class="field-card-head">
            <span class="field-label">
              <span v-if="f.required && isInputField(f)" class="required-mark">*</span>{{ f.label || '（未命名）' }}
            </span>
            <span class="field-type">{{ fieldTypeMap[f.type] }}</span>
            <span v-if="isInputField(f)" class="field-key">{{ f.key }}</span>
            <span class="field-actions">
              <el-button link type="primary" :disabled="i === 0" @click.stop="moveField(i, -1)">上移</el-button>
              <el-button link type="primary" :disabled="i === fields.length - 1" @click.stop="moveField(i, 1)">下移</el-button>
              <el-button link type="danger" @click.stop="removeField(i)">删除</el-button>
            </span>
          </div>
          <!-- 控件示意（禁用态渲染） -->
          <div class="field-card-body">
            <DynamicForm :fields="[f]" disabled label-width="0px" />
          </div>
        </div>
      </div>

      <!-- 右侧属性面板 -->
      <div class="props-panel">
        <template v-if="selected">
          <div class="palette-title">字段属性</div>
          <el-form label-position="top" size="small">
            <el-form-item label="标签名称">
              <el-input v-model="selected.label" />
            </el-form-item>
            <el-form-item v-if="isInputField(selected)" label="字段 key（条件分支按此取值）">
              <el-input v-model="selected.key" />
            </el-form-item>
            <el-form-item label="类型">
              <el-select v-model="selected.type" style="width: 100%" @change="onTypeChange">
                <el-option v-for="(label, type) in fieldTypeMap" :key="type" :value="type" :label="label" />
              </el-select>
            </el-form-item>
            <template v-if="isInputField(selected)">
              <el-form-item label="必填">
                <el-switch v-model="selected.required" />
              </el-form-item>
              <el-form-item v-if="optionTypes.includes(selected.type)" label="选项（逗号分隔）">
                <el-input v-model="optionsText" placeholder="如 年假,事假,病假" />
              </el-form-item>
              <el-form-item v-if="hasPlaceholder(selected)" label="占位提示">
                <el-input v-model="selected.placeholder" />
              </el-form-item>
              <el-form-item label="说明文字">
                <el-input v-model="selected.help" />
              </el-form-item>
              <el-form-item label="栅格宽度（24 = 整行）">
                <el-input-number v-model="selected.span" :min="6" :max="24" :step="6" style="width: 100%" />
              </el-form-item>
            </template>
          </el-form>
        </template>
        <template v-else>
          <div class="palette-title">表单信息</div>
          <div class="info-item">名称：{{ defName }}</div>
          <div class="info-item">字段数：{{ fields.length }}</div>
        </template>

        <!-- 绑定流程的条件字段核对 -->
        <template v-if="wfName && conditionKeys.length">
          <div class="palette-title" style="margin-top: 16px">流程条件引用的字段</div>
          <div v-for="k in conditionKeys" :key="k" class="cond-key" :class="{ missing: !fieldKeys.has(k) }">
            {{ k }}<span v-if="!fieldKeys.has(k)">（表单中缺失）</span>
          </div>
        </template>
      </div>
    </div>

    <!-- 预览 -->
    <el-dialog v-model="previewVisible" title="表单预览" width="640px" destroy-on-close>
      <DynamicForm v-model="previewValues" :fields="fields" />
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import {
  getFormDefinition, updateFormDefinition, fieldTypeMap, optionTypes, isInputField,
  type FormDefinition, type FormField, type FormFieldType,
} from '@/api/form'
import { getDefinition as getWorkflowDefinition } from '@/api/workflow'
import DynamicForm from '@/components/DynamicForm.vue'

const route = useRoute()
const router = useRouter()
const formId = Number(route.params.id)

const def = ref<FormDefinition | null>(null)
const defName = computed(() => def.value?.name ?? '')
const wfName = ref('')
const fields = ref<FormField[]>([])
const saving = ref(false)

// 绑定流程条件边引用的字段 key（缺失标红；服务端保存时兜底强校验）
const conditionKeys = ref<string[]>([])
const fieldKeys = computed(() => new Set(fields.value.filter(isInputField).map(f => f.key)))

async function load() {
  const detail = (await getFormDefinition(formId)).data
  def.value = detail
  fields.value = detail.fields
  if (detail.workflowDefinitionId) {
    const wf = (await getWorkflowDefinition(detail.workflowDefinitionId)).data
    wfName.value = wf.name
    const keys = new Set<string>()
    for (const e of wf.graph.edges)
      for (const r of e.condition?.rules ?? []) if (r.field) keys.add(r.field)
    conditionKeys.value = [...keys]
  }
}

// ---- 字段增删排序 ----

let keySeq = 0
const genKey = () => `f_${Date.now().toString(36)}${(keySeq++).toString(36)}`

const selectedIndex = ref<number | null>(null)
const selected = computed(() =>
  selectedIndex.value === null ? null : fields.value[selectedIndex.value] ?? null)

function addField(type: FormFieldType) {
  const f: FormField = {
    key: type === 'divider' ? '' : genKey(),
    label: fieldTypeMap[type],
    type,
    required: false,
    options: optionTypes.includes(type) ? ['选项1', '选项2'] : [],
    span: 24,
  }
  fields.value.push(f)
  selectedIndex.value = fields.value.length - 1
}
function removeField(i: number) {
  fields.value.splice(i, 1)
  if (selectedIndex.value === i) selectedIndex.value = null
  else if (selectedIndex.value !== null && selectedIndex.value > i) selectedIndex.value--
}
function moveField(i: number, delta: number) {
  const target = i + delta
  const [f] = fields.value.splice(i, 1)
  fields.value.splice(target, 0, f)
  if (selectedIndex.value === i) selectedIndex.value = target
}

// HTML5 拖拽排序：dragover 时实时交换位置
const dragIndex = ref<number | null>(null)
function onDragStart(i: number, e: DragEvent) {
  dragIndex.value = i
  e.dataTransfer?.setData('text/plain', String(i))
  if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move'
}
function onDragOver(i: number) {
  if (dragIndex.value === null || dragIndex.value === i) return
  const [f] = fields.value.splice(dragIndex.value, 1)
  fields.value.splice(i, 0, f)
  if (selectedIndex.value === dragIndex.value) selectedIndex.value = i
  dragIndex.value = i
}

// ---- 属性编辑 ----

const optionsText = computed({
  get: () => selected.value?.options.join(',') ?? '',
  set: (v: string) => {
    if (selected.value)
      selected.value.options = v.split(/[,，]/).map(s => s.trim()).filter(Boolean)
  },
})
const hasPlaceholder = (f: FormField) =>
  ['text', 'textarea', 'number', 'date', 'datetime', 'select'].includes(f.type)

function onTypeChange() {
  const f = selected.value
  if (!f) return
  if (optionTypes.includes(f.type) && f.options.length === 0) f.options = ['选项1', '选项2']
  if (f.type === 'divider') f.required = false
  else if (!f.key) f.key = genKey()
  delete f.defaultValue
}

// ---- 预览 / 保存 ----

const previewVisible = ref(false)
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const previewValues = ref<Record<string, any>>({})
function openPreview() {
  previewValues.value = {}
  previewVisible.value = true
}

async function onSave() {
  if (!def.value) return
  saving.value = true
  try {
    await updateFormDefinition(formId, {
      code: def.value.code,
      name: def.value.name,
      description: def.value.description,
      icon: def.value.icon,
      workflowDefinitionId: def.value.workflowDefinitionId ?? null,
      fields: fields.value,
    })
    ElMessage.success('保存成功')
  } finally {
    saving.value = false
  }
}
function goBack() {
  router.push('/form/definition')
}

load()
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
  cursor: pointer;
  user-select: none;
  background: var(--el-bg-color);
}
.palette-item:hover {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
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
  padding: 16px;
  overflow-y: auto;
}
.field-card {
  border: 1px solid var(--el-border-color);
  border-radius: 6px;
  padding: 8px 12px;
  margin-bottom: 8px;
  background: var(--el-bg-color);
  cursor: grab;
}
.field-card.selected {
  border-color: var(--el-color-primary);
  box-shadow: 0 0 0 1px var(--el-color-primary);
}
.field-card.dragging {
  opacity: 0.5;
}
.field-card-head {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
}
.field-label {
  font-weight: 600;
}
.required-mark {
  color: var(--el-color-danger);
  margin-right: 2px;
}
.field-type {
  color: var(--el-color-primary);
  font-size: 12px;
}
.field-key {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
.field-actions {
  margin-left: auto;
}
.field-card-body {
  margin-top: 6px;
  pointer-events: none;
}
.props-panel {
  width: 260px;
  padding: 10px;
  border-left: 1px solid var(--el-border-color-light);
  background: var(--el-bg-color);
  overflow-y: auto;
}
.info-item {
  font-size: 13px;
  color: var(--el-text-color-secondary);
  line-height: 2;
}
.cond-key {
  font-size: 12px;
  line-height: 1.9;
  color: var(--el-text-color-regular);
}
.cond-key.missing {
  color: var(--el-color-danger);
}
</style>
