<template>
  <el-drawer v-model="visible" title="流程详情" size="620px" destroy-on-close>
    <div v-loading="loading">
      <template v-if="detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="标题" :span="2">{{ detail.title }}</el-descriptions-item>
          <el-descriptions-item label="流程">{{ detail.definitionName }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="instanceStatusMap[detail.status]?.type">{{ instanceStatusMap[detail.status]?.label }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="发起人">{{ detail.initiatorName }}</el-descriptions-item>
          <el-descriptions-item label="发起时间">{{ formatTime(detail.createdTime) }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.finishTime" label="结束时间" :span="2">
            {{ formatTime(detail.finishTime) }}
          </el-descriptions-item>
          <el-descriptions-item label="申请内容" :span="2">
            <template v-if="formDataPairs">
              <div v-for="(pair, i) in formDataPairs" :key="i">
                <span style="color: var(--el-text-color-secondary)">{{ pair.label }}：</span>{{ pair.value }}
              </div>
            </template>
            <div v-else style="white-space: pre-wrap">{{ detail.formData || '-' }}</div>
          </el-descriptions-item>
        </el-descriptions>

        <div class="section-title">流程图（绿=已过 / 蓝=当前 / 红=驳回）</div>
        <WorkflowGraphViewer :graph="detail.graph" :statuses="statuses" height="280px" />

        <div class="section-title">审批记录</div>
        <el-table :data="detail.tasks" border size="small">
          <el-table-column prop="nodeName" label="节点" width="120" />
          <el-table-column prop="approverName" label="审批人" width="100" />
          <el-table-column label="状态" width="90">
            <template #default="{ row }">
              <el-tag size="small" :type="taskStatusMap[row.status]?.type">{{ taskStatusMap[row.status]?.label }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="comment" label="意见" min-width="120" show-overflow-tooltip />
          <el-table-column label="处理时间" width="160">
            <template #default="{ row }">{{ row.handleTime ? formatTime(row.handleTime) : '-' }}</template>
          </el-table-column>
        </el-table>
      </template>
    </div>
  </el-drawer>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import {
  getInstance, instanceStatusMap, taskStatusMap,
  InstanceStatus, WfTaskStatus, type WorkflowInstanceDetail,
} from '@/api/workflow'
import { formatTime } from '@/utils/format'
import WorkflowGraphViewer from './WorkflowGraphViewer.vue'

const visible = defineModel<boolean>({ default: false })
const props = defineProps<{ instanceId: number | null }>()

const loading = ref(false)
const detail = ref<WorkflowInstanceDetail | null>(null)

/** 节点着色：任务通过=绿、驳回=红；审批中的当前节点=蓝（覆盖前两者） */
const statuses = computed(() => {
  const map: Record<string, 'done' | 'current' | 'rejected'> = {}
  if (!detail.value) return map
  for (const t of detail.value.tasks) {
    if (t.status === WfTaskStatus.Approved && !map[t.nodeId]) map[t.nodeId] = 'done'
    if (t.status === WfTaskStatus.Rejected) map[t.nodeId] = 'rejected'
  }
  if (detail.value.status === InstanceStatus.Running && detail.value.currentNodeId)
    map[detail.value.currentNodeId] = 'current'
  return map
})

/** 表单数据是 JSON 对象且有字段定义时，结构化展示 */
const formDataPairs = computed(() => {
  if (!detail.value) return null
  try {
    const obj = JSON.parse(detail.value.formData)
    if (typeof obj !== 'object' || obj === null || Array.isArray(obj)) return null
    const labelOf = (key: string) => detail.value!.formFields.find(f => f.key === key)?.label ?? key
    return Object.entries(obj).map(([k, v]) => ({ label: labelOf(k), value: String(v ?? '') }))
  } catch {
    return null
  }
})

watch([visible, () => props.instanceId], async ([v, id]) => {
  if (!v || !id) return
  loading.value = true
  try {
    detail.value = (await getInstance(id)).data
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.section-title {
  margin: 16px 0 10px;
  font-weight: 600;
}
</style>
