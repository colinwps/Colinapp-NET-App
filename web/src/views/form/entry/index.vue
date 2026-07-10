<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-select v-model="query.formDefinitionId" placeholder="表单" clearable filterable style="width: 200px">
          <el-option v-for="f in formOptions" :key="f.id" :value="f.id" :label="f.name" />
        </el-select>
        <el-input v-model="query.keyword" placeholder="标题/表单名" clearable style="width: 200px" @keyup.enter="reload" />
        <el-button type="primary" @click="reload">查询</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="formName" label="表单" width="160" />
        <el-table-column prop="title" label="标题" min-width="180" />
        <el-table-column prop="submitterName" label="提交人" width="120" />
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag v-if="row.instanceStatus" :type="instanceStatusMap[row.instanceStatus]?.type">
              {{ instanceStatusMap[row.instanceStatus]?.label }}
            </el-tag>
            <el-tag v-else type="success">已保存</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="提交时间" width="170">
          <template #default="{ row }">{{ formatTime(row.createdTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="90" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEntry(row as FormEntry)">查看</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="loadData" />
      </div>
    </el-card>

    <!-- 未绑流程的记录详情（快照渲染） -->
    <el-dialog v-model="entryVisible" :title="entryDetail?.title ?? '提交详情'" width="640px" destroy-on-close>
      <el-descriptions v-if="entryDetail" :column="2" border style="margin-bottom: 16px">
        <el-descriptions-item label="表单">{{ entryDetail.formName }}</el-descriptions-item>
        <el-descriptions-item label="提交人">{{ entryDetail.submitterName }}</el-descriptions-item>
        <el-descriptions-item label="提交时间" :span="2">{{ formatTime(entryDetail.createdTime) }}</el-descriptions-item>
      </el-descriptions>
      <DynamicForm v-if="entryDetail" v-model="entryValues" :fields="entryDetail.fields" disabled />
    </el-dialog>

    <!-- 绑定流程的记录：审批详情抽屉 -->
    <InstanceDetailDrawer v-model="instanceVisible" :instance-id="instanceId" />
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import {
  getFormEntries, getFormEntry, getFormDefinitions,
  type FormDefinition, type FormEntry, type FormEntryDetail, type FormEntryQuery,
} from '@/api/form'
import { instanceStatusMap } from '@/api/workflow'
import { formatTime } from '@/utils/format'
import DynamicForm from '@/components/DynamicForm.vue'
import InstanceDetailDrawer from '@/views/workflow/components/InstanceDetailDrawer.vue'

const loading = ref(false)
const list = ref<FormEntry[]>([])
const total = ref(0)
const query = reactive<FormEntryQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadData() {
  loading.value = true
  try {
    const res = await getFormEntries(query)
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

// 表单过滤下拉（需要 form:def:list 权限；无权限时静默降级为无下拉选项）
const formOptions = ref<FormDefinition[]>([])
async function loadFormOptions() {
  try {
    formOptions.value = (await getFormDefinitions({ pageIndex: 1, pageSize: 200 })).data.items
  } catch { /* 无表单定义查看权限时忽略 */ }
}

// 查看：绑定实例 → 审批详情抽屉；否则 → 快照渲染
const entryVisible = ref(false)
const entryDetail = ref<FormEntryDetail | null>(null)
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const entryValues = ref<Record<string, any>>({})
const instanceVisible = ref(false)
const instanceId = ref<number | null>(null)

async function openEntry(row: FormEntry) {
  if (row.workflowInstanceId) {
    instanceId.value = row.workflowInstanceId
    instanceVisible.value = true
    return
  }
  const detail = (await getFormEntry(row.id)).data
  let parsed: Record<string, unknown> = {}
  try { parsed = JSON.parse(detail.dataJson || '{}') } catch { /* 忽略非法数据 */ }
  entryValues.value = parsed
  entryDetail.value = detail
  entryVisible.value = true
}

loadData()
loadFormOptions()
</script>
