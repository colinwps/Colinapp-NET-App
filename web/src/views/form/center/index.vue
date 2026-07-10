<template>
  <div class="page-container">
    <el-card>
      <el-tabs v-model="activeTab">
        <!-- 发起申请：已发布表单卡片 -->
        <el-tab-pane label="发起申请" name="apply">
          <el-empty v-if="!loadingForms && forms.length === 0" description="暂无可用表单" />
          <div v-loading="loadingForms" class="form-grid">
            <div v-for="f in forms" :key="f.id" class="form-card" @click="openApply(f)">
              <el-icon class="form-icon" :size="28"><component :is="f.icon || 'Document'" /></el-icon>
              <div class="form-card-main">
                <div class="form-card-title">
                  {{ f.name }}
                  <el-tag v-if="f.workflowDefinitionId" size="small" type="warning">需审批</el-tag>
                </div>
                <div class="form-card-desc">{{ f.description || '点击填报' }}</div>
              </div>
            </div>
          </div>
        </el-tab-pane>

        <!-- 我的提交 -->
        <el-tab-pane label="我的提交" name="mine">
          <div class="table-toolbar">
            <el-input v-model="query.keyword" placeholder="标题/表单名" clearable style="width: 200px" @keyup.enter="reloadEntries" />
            <el-button type="primary" @click="reloadEntries">查询</el-button>
          </div>
          <el-table v-loading="loadingEntries" :data="entries" border>
            <el-table-column prop="formName" label="表单" width="160" />
            <el-table-column prop="title" label="标题" min-width="180" />
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
            <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="entryTotal" layout="total, prev, pager, next" @current-change="loadEntries" />
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 填报 -->
    <el-dialog v-model="applyVisible" :title="applyForm?.name ?? '填报'" width="640px" destroy-on-close>
      <el-form label-width="100px">
        <el-form-item label="标题">
          <el-input v-model="applyTitle" :placeholder="applyForm?.name" />
        </el-form-item>
      </el-form>
      <DynamicForm v-if="applyForm" ref="applyFormRef" v-model="applyValues" :fields="applyForm.fields" />
      <template #footer>
        <el-button @click="applyVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="onApply">提交</el-button>
      </template>
    </el-dialog>

    <!-- 未绑流程的记录详情 -->
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
import { ElMessage } from 'element-plus'
import {
  getPublishedForms, submitForm, getMyFormEntries, getFormEntry,
  type PublishedForm, type FormEntry, type FormEntryDetail, type FormEntryQuery,
} from '@/api/form'
import { instanceStatusMap } from '@/api/workflow'
import { formatTime } from '@/utils/format'
import DynamicForm from '@/components/DynamicForm.vue'
import InstanceDetailDrawer from '@/views/workflow/components/InstanceDetailDrawer.vue'

const activeTab = ref('apply')

// ---- 发起申请 ----

const loadingForms = ref(false)
const forms = ref<PublishedForm[]>([])
async function loadForms() {
  loadingForms.value = true
  try {
    forms.value = (await getPublishedForms()).data
  } finally {
    loadingForms.value = false
  }
}

const applyVisible = ref(false)
const submitting = ref(false)
const applyForm = ref<PublishedForm | null>(null)
const applyTitle = ref('')
const applyFormRef = ref<InstanceType<typeof DynamicForm>>()
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const applyValues = ref<Record<string, any>>({})

function openApply(f: PublishedForm) {
  applyForm.value = f
  applyTitle.value = ''
  applyValues.value = {}
  applyVisible.value = true
}
async function onApply() {
  if (!applyForm.value) return
  await applyFormRef.value?.validate()
  submitting.value = true
  try {
    const res = await submitForm(applyForm.value.id, {
      title: applyTitle.value,
      dataJson: JSON.stringify(applyValues.value),
    })
    ElMessage.success(res.data.instanceId ? '已提交并发起审批' : '已提交')
    applyVisible.value = false
    if (activeTab.value === 'mine') loadEntries()
  } finally {
    submitting.value = false
  }
}

// ---- 我的提交 ----

const loadingEntries = ref(false)
const entries = ref<FormEntry[]>([])
const entryTotal = ref(0)
const query = reactive<FormEntryQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadEntries() {
  loadingEntries.value = true
  try {
    const res = await getMyFormEntries(query)
    entries.value = res.data.items
    entryTotal.value = res.data.total
  } finally {
    loadingEntries.value = false
  }
}
function reloadEntries() {
  query.pageIndex = 1
  loadEntries()
}

// 查看：绑定实例 → 审批详情抽屉（实例数据为权威）；否则 → 快照渲染
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

loadForms()
loadEntries()
</script>

<style scoped>
.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 12px;
  min-height: 60px;
}
.form-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  border: 1px solid var(--el-border-color);
  border-radius: 8px;
  cursor: pointer;
  transition: border-color 0.15s, box-shadow 0.15s;
}
.form-card:hover {
  border-color: var(--el-color-primary);
  box-shadow: var(--el-box-shadow-light);
}
.form-icon {
  color: var(--el-color-primary);
  flex-shrink: 0;
}
.form-card-main {
  min-width: 0;
}
.form-card-title {
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 6px;
}
.form-card-desc {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
