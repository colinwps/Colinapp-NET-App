<template>
  <div class="page-container">
    <el-card>
      <el-tabs v-model="activeTab" @tab-change="reload">
        <el-tab-pane label="待办" name="todo" />
        <el-tab-pane label="已办" name="done" />
      </el-tabs>

      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="申请标题" clearable style="width: 200px" @keyup.enter="reload" />
        <el-button type="primary" @click="reload">查询</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="instanceTitle" label="申请标题" min-width="180">
          <template #default="{ row }">
            {{ row.instanceTitle }}
            <el-tag v-if="activeTab === 'todo' && isOverdue(row as WorkflowTaskItem)" size="small" type="danger"
              style="margin-left: 4px">超时</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="definitionName" label="流程" width="130" />
        <el-table-column prop="nodeName" label="节点" width="110" />
        <el-table-column label="方式" width="80">
          <template #default="{ row }">
            <el-tag size="small" :type="row.approveMode === ApproveMode.All ? 'warning' : 'primary'">
              {{ row.approveMode === ApproveMode.All ? '会签' : '或签' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="initiatorName" label="发起人" width="100" />
        <el-table-column v-if="activeTab === 'done'" label="结果" width="90">
          <template #default="{ row }">
            <el-tag :type="taskStatusMap[row.status]?.type">{{ taskStatusMap[row.status]?.label }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column v-if="activeTab === 'done'" prop="comment" label="意见" min-width="120" show-overflow-tooltip />
        <el-table-column :label="activeTab === 'todo' ? '到达时间' : '处理时间'" width="170">
          <template #default="{ row }">
            {{ formatTime(activeTab === 'todo' ? row.createdTime : row.handleTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="130" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openDetail(row as WorkflowTaskItem)">详情</el-button>
            <el-button v-if="activeTab === 'todo'" v-auth="'wf:task:approve'"
              link type="success" @click="openApprove(row as WorkflowTaskItem)">审批</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="loadData" />
      </div>
    </el-card>

    <el-dialog v-model="approveVisible" title="审批" width="480px">
      <el-form label-width="80px">
        <el-form-item label="申请标题">{{ approving?.instanceTitle }}</el-form-item>
        <el-form-item label="审批结果">
          <el-radio-group v-model="approveForm.approved">
            <el-radio :value="true">通过</el-radio>
            <el-radio :value="false">驳回</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="审批意见">
          <el-input v-model="approveForm.comment" type="textarea" :rows="3" placeholder="选填" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="approveVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="onApprove">确定</el-button>
      </template>
    </el-dialog>

    <InstanceDetailDrawer v-model="detailVisible" :instance-id="detailId" />
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getTodoTasks, getDoneTasks, approveTask, taskStatusMap, ApproveMode,
  type WorkflowTaskItem,
} from '@/api/workflow'
import type { PagedQuery } from '@/api/types'
import { formatTime } from '@/utils/format'
import InstanceDetailDrawer from '../components/InstanceDetailDrawer.vue'

const activeTab = ref<'todo' | 'done'>('todo')
const loading = ref(false)
const list = ref<WorkflowTaskItem[]>([])
const total = ref(0)
const query = reactive<PagedQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadData() {
  loading.value = true
  try {
    const api = activeTab.value === 'todo' ? getTodoTasks : getDoneTasks
    const res = await api(query)
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

function isOverdue(row: WorkflowTaskItem) {
  return !!row.dueTime && new Date(row.dueTime).getTime() < Date.now()
}

// 审批
const approveVisible = ref(false)
const saving = ref(false)
const approving = ref<WorkflowTaskItem | null>(null)
const approveForm = reactive({ approved: true, comment: '' })

function openApprove(row: WorkflowTaskItem) {
  approving.value = row
  Object.assign(approveForm, { approved: true, comment: '' })
  approveVisible.value = true
}
async function onApprove() {
  if (!approving.value) return
  saving.value = true
  try {
    await approveTask(approving.value.id, approveForm.approved, approveForm.comment || undefined)
    ElMessage.success('审批完成')
    approveVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}

// 详情
const detailVisible = ref(false)
const detailId = ref<number | null>(null)
function openDetail(row: WorkflowTaskItem) {
  detailId.value = row.instanceId
  detailVisible.value = true
}

loadData()
</script>
