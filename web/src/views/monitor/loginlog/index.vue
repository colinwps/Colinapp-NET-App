<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="账号" clearable style="width: 200px" @keyup.enter="reload" />
        <el-select v-model="query.success" placeholder="结果" clearable style="width: 120px">
          <el-option :value="true" label="成功" />
          <el-option :value="false" label="失败" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'sys:logininfor:remove'" type="danger" @click="onClear">清空</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="userName" label="账号" min-width="120" />
        <el-table-column label="结果" width="80">
          <template #default="{ row }">
            <el-tag :type="row.success ? 'success' : 'danger'">{{ row.success ? '成功' : '失败' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="message" label="提示" min-width="140" />
        <el-table-column prop="ip" label="IP" width="140" />
        <el-table-column prop="userAgent" label="User-Agent" min-width="200" show-overflow-tooltip />
        <el-table-column label="时间" width="160">
          <template #default="{ row }">{{ formatTime(row.createdTime) }}</template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total" :page-sizes="[20, 50, 100]" layout="total, sizes, prev, pager, next" @current-change="loadData" @size-change="reload" />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getLoginLogs, clearLoginLogs, type LoginLog, type LogQuery } from '@/api/log'
import { formatTime } from '@/utils/format'

const loading = ref(false)
const list = ref<LoginLog[]>([])
const total = ref(0)
const query = reactive<LogQuery>({ pageIndex: 1, pageSize: 20, keyword: '', success: undefined })

async function loadData() {
  loading.value = true
  try {
    const res = await getLoginLogs(query)
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

async function onClear() {
  await ElMessageBox.confirm('确认清空所有登录日志？', '提示', { type: 'warning' })
  await clearLoginLogs()
  ElMessage.success('已清空')
  reload()
}

loadData()
</script>
