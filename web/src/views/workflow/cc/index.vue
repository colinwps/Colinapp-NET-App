<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="申请标题" clearable style="width: 200px" @keyup.enter="reload" />
        <el-checkbox v-model="unreadOnly" label="仅看未读" @change="reload" />
        <el-button type="primary" @click="reload">查询</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="instanceTitle" label="申请标题" min-width="180">
          <template #default="{ row }">
            <el-badge v-if="!row.readTime" is-dot style="margin-right: 8px" />
            {{ row.instanceTitle }}
          </template>
        </el-table-column>
        <el-table-column prop="definitionName" label="流程" width="130" />
        <el-table-column prop="nodeName" label="抄送节点" width="120" />
        <el-table-column prop="initiatorName" label="发起人" width="100" />
        <el-table-column label="流程状态" width="100">
          <template #default="{ row }">
            <el-tag :type="instanceStatusMap[row.instanceStatus]?.type">
              {{ instanceStatusMap[row.instanceStatus]?.label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="抄送时间" width="170">
          <template #default="{ row }">{{ formatTime(row.createdTime) }}</template>
        </el-table-column>
        <el-table-column label="阅读时间" width="170">
          <template #default="{ row }">{{ row.readTime ? formatTime(row.readTime) : '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openDetail(row as WorkflowCcItem)">详情</el-button>
            <el-button v-if="!row.readTime" link type="success" @click="onMarkRead(row as WorkflowCcItem)">标记已读</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="loadData" />
      </div>
    </el-card>

    <InstanceDetailDrawer v-model="detailVisible" :instance-id="detailId" />
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getMyCc, markCcRead, instanceStatusMap,
  type WorkflowCcItem, type WorkflowCcQuery,
} from '@/api/workflow'
import { formatTime } from '@/utils/format'
import InstanceDetailDrawer from '../components/InstanceDetailDrawer.vue'

const loading = ref(false)
const list = ref<WorkflowCcItem[]>([])
const total = ref(0)
const unreadOnly = ref(false)
const query = reactive<WorkflowCcQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadData() {
  loading.value = true
  try {
    query.unreadOnly = unreadOnly.value || undefined
    const res = await getMyCc(query)
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

async function onMarkRead(row: WorkflowCcItem) {
  await markCcRead(row.id)
  ElMessage.success('已标记为已读')
  loadData()
}

// 详情：打开即视为已读
const detailVisible = ref(false)
const detailId = ref<number | null>(null)
function openDetail(row: WorkflowCcItem) {
  detailId.value = row.instanceId
  detailVisible.value = true
  if (!row.readTime)
    markCcRead(row.id).then(() => { row.readTime = new Date().toISOString() })
}

loadData()
</script>
