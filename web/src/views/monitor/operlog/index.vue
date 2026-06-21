<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="标题/操作人" clearable style="width: 200px" @keyup.enter="reload" />
        <el-select v-model="query.success" placeholder="结果" clearable style="width: 120px">
          <el-option :value="true" label="成功" />
          <el-option :value="false" label="失败" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'sys:log:remove'" type="danger" @click="onClear">清空</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="title" label="操作" min-width="140" />
        <el-table-column prop="requestMethod" label="方法" width="80" />
        <el-table-column prop="requestUrl" label="URL" min-width="180" show-overflow-tooltip />
        <el-table-column prop="operatorName" label="操作人" width="110" />
        <el-table-column prop="ip" label="IP" width="120" />
        <el-table-column label="结果" width="80">
          <template #default="{ row }">
            <el-tag :type="row.success ? 'success' : 'danger'">{{ row.success ? '成功' : '失败' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="duration" label="耗时" width="90">
          <template #default="{ row }">{{ row.duration }}ms</template>
        </el-table-column>
        <el-table-column label="时间" width="160">
          <template #default="{ row }">{{ formatTime(row.createdTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="80" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openDetail(row as OperationLog)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total" :page-sizes="[20, 50, 100]" layout="total, sizes, prev, pager, next" @current-change="loadData" @size-change="reload" />
      </div>
    </el-card>

    <el-dialog v-model="detailVisible" title="操作日志详情" width="600px">
      <el-descriptions v-if="current" :column="1" border>
        <el-descriptions-item label="操作">{{ current.title }}</el-descriptions-item>
        <el-descriptions-item label="方法">{{ current.method }}</el-descriptions-item>
        <el-descriptions-item label="请求">{{ current.requestMethod }} {{ current.requestUrl }}</el-descriptions-item>
        <el-descriptions-item label="参数">
          <pre class="params">{{ current.requestParams }}</pre>
        </el-descriptions-item>
        <el-descriptions-item label="UA">{{ current.userAgent ?? '-' }}</el-descriptions-item>
        <el-descriptions-item v-if="!current.success" label="错误">
          <span style="color: #f56c6c">{{ current.errorMessage }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="耗时">{{ current.duration }}ms</el-descriptions-item>
        <el-descriptions-item label="时间">{{ formatTime(current.createdTime) }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getOperationLogs, clearOperationLogs, type OperationLog, type LogQuery } from '@/api/log'
import { formatTime } from '@/utils/format'

const loading = ref(false)
const list = ref<OperationLog[]>([])
const total = ref(0)
const query = reactive<LogQuery>({ pageIndex: 1, pageSize: 20, keyword: '', success: undefined })

async function loadData() {
  loading.value = true
  try {
    const res = await getOperationLogs(query)
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

const detailVisible = ref(false)
const current = ref<OperationLog & { userAgent?: string }>()
function openDetail(row: OperationLog) {
  current.value = row
  detailVisible.value = true
}

async function onClear() {
  await ElMessageBox.confirm('确认清空所有操作日志？', '提示', { type: 'warning' })
  await clearOperationLogs()
  ElMessage.success('已清空')
  reload()
}

loadData()
</script>

<style scoped>
.params {
  white-space: pre-wrap;
  word-break: break-all;
  margin: 0;
  font-family: inherit;
}
</style>
