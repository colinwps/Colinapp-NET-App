<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="任务名称 / 目标" clearable style="width: 220px" @keyup.enter="reload" />
        <el-select v-model="query.status" placeholder="状态" clearable style="width: 120px">
          <el-option :value="JobStatus.Running" label="正常" />
          <el-option :value="JobStatus.Paused" label="暂停" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'monitor:job:add'" type="success" @click="openCreate">新增</el-button>
        <el-button v-auth="'monitor:job:list'" @click="openLogs">执行日志</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="name" label="任务名称" min-width="140" />
        <el-table-column label="调用目标" min-width="160">
          <template #default="{ row }">
            <span>{{ targetName(row.invokeTarget) }}</span>
            <el-tag size="small" type="info" effect="plain" style="margin-left: 6px">{{ row.invokeTarget }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="Cron 表达式" min-width="140">
          <template #default="{ row }"><code class="cron">{{ row.cronExpression }}</code></template>
        </el-table-column>
        <el-table-column label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-switch
              :model-value="row.status === JobStatus.Running"
              :disabled="!canChangeStatus"
              @change="(v: any) => toggleStatus(row as ScheduledJob, v)"
            />
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="170">
          <template #default="{ row }">{{ formatTime(row.createdTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'monitor:job:run'" link type="warning" @click="onRun(row as ScheduledJob)">执行一次</el-button>
            <el-button v-auth="'monitor:job:edit'" link type="primary" @click="openEdit(row as ScheduledJob)">编辑</el-button>
            <el-button v-auth="'monitor:job:remove'" link type="danger" @click="onDelete(row as ScheduledJob)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total"
          layout="total, prev, pager, next" @current-change="loadData"
        />
      </div>
    </el-card>

    <!-- 新增/编辑 -->
    <el-dialog v-model="dialogVisible" :title="editId ? '编辑任务' : '新增任务'" width="620px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="96px">
        <el-form-item label="任务名称" prop="name"><el-input v-model="form.name" placeholder="如 每日日志清理" /></el-form-item>
        <el-form-item label="调用目标" prop="invokeTarget">
          <el-select v-model="form.invokeTarget" placeholder="选择内置任务" style="width: 100%" @change="onTargetChange">
            <el-option v-for="t in targets" :key="t.key" :value="t.key" :label="`${t.name}（${t.key}）`">
              <span>{{ t.name }}</span>
              <span style="float: right; color: var(--el-text-color-secondary); font-size: 12px">{{ t.key }}</span>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item v-if="currentTarget" label="">
          <el-alert :title="currentTarget.description" type="info" :closable="false" />
        </el-form-item>
        <el-form-item label="Cron 表达式" prop="cronExpression">
          <el-input v-model="form.cronExpression" placeholder="Quartz 格式，含秒位，如 0 0 3 * * ?" />
        </el-form-item>
        <el-form-item label="任务参数">
          <el-input v-model="form.jobData" placeholder="可选，由任务解释（如日志保留天数）" />
        </el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="form.status">
            <el-radio :value="JobStatus.Running">正常</el-radio>
            <el-radio :value="JobStatus.Paused">暂停</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" :rows="2" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="onSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 执行日志 -->
    <el-drawer v-model="logVisible" title="执行日志" size="60%">
      <div class="table-toolbar">
        <el-select v-model="logQuery.success" placeholder="结果" clearable style="width: 120px">
          <el-option :value="true" label="成功" />
          <el-option :value="false" label="失败" />
        </el-select>
        <el-button type="primary" @click="reloadLogs">查询</el-button>
        <el-button v-auth="'monitor:job:remove'" type="danger" @click="onClearLogs">清空</el-button>
      </div>
      <el-table v-loading="logLoading" :data="logs" border size="small">
        <el-table-column prop="jobName" label="任务" min-width="120" />
        <el-table-column label="结果" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.success ? 'success' : 'danger'" size="small">{{ row.success ? '成功' : '失败' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="message" label="信息" min-width="200" show-overflow-tooltip />
        <el-table-column label="耗时" width="90" align="right">
          <template #default="{ row }">{{ row.elapsedMs }} ms</template>
        </el-table-column>
        <el-table-column label="开始时间" width="170">
          <template #default="{ row }">{{ formatTime(row.startTime) }}</template>
        </el-table-column>
      </el-table>
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="logQuery.pageIndex" v-model:page-size="logQuery.pageSize" :total="logTotal"
          layout="total, prev, pager, next" @current-change="loadLogs"
        />
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  getJobs, getJob, getJobTargets, createJob, updateJob, deleteJob,
  changeJobStatus, runJobOnce, getJobLogs, clearJobLogs,
  JobStatus, type ScheduledJob, type ScheduledJobSaveDto, type ScheduledJobQuery,
  type JobTarget, type JobLog, type JobLogQuery,
} from '@/api/job'
import { formatTime } from '@/utils/format'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()
const canChangeStatus = computed(() => userStore.hasPermission('monitor:job:changeStatus'))

const loading = ref(false)
const list = ref<ScheduledJob[]>([])
const total = ref(0)
const query = reactive<ScheduledJobQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

const targets = ref<JobTarget[]>([])
const targetName = (key: string) => targets.value.find((t) => t.key === key)?.name ?? key

async function loadTargets() {
  targets.value = (await getJobTargets()).data
}
async function loadData() {
  loading.value = true
  try {
    const res = await getJobs(query)
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

const dialogVisible = ref(false)
const saving = ref(false)
const editId = ref<number | null>(null)
const formRef = ref<FormInstance>()
const defaultForm = (): ScheduledJobSaveDto => ({
  name: '', jobGroup: 'DEFAULT', invokeTarget: '', cronExpression: '', jobData: '',
  status: JobStatus.Paused, concurrent: false, remark: '',
})
const form = reactive<ScheduledJobSaveDto>(defaultForm())
const currentTarget = computed(() => targets.value.find((t) => t.key === form.invokeTarget))
const rules: FormRules = {
  name: [{ required: true, message: '请输入任务名称', trigger: 'blur' }],
  invokeTarget: [{ required: true, message: '请选择调用目标', trigger: 'change' }],
  cronExpression: [{ required: true, message: '请输入 Cron 表达式', trigger: 'blur' }],
}

function onTargetChange(key: string) {
  const t = targets.value.find((x) => x.key === key)
  if (!t) return
  if (!form.cronExpression) form.cronExpression = t.defaultCron
  if (!form.jobData) form.jobData = t.defaultJobData ?? ''
}

function openCreate() {
  editId.value = null
  Object.assign(form, defaultForm())
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
async function openEdit(row: ScheduledJob) {
  editId.value = row.id
  const d = (await getJob(row.id)).data
  Object.assign(form, {
    name: d.name, jobGroup: d.jobGroup, invokeTarget: d.invokeTarget, cronExpression: d.cronExpression,
    jobData: d.jobData ?? '', status: d.status, concurrent: d.concurrent, remark: d.remark ?? '',
  })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  saving.value = true
  try {
    if (editId.value) await updateJob(editId.value, form)
    else await createJob(form)
    ElMessage.success('保存成功')
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}
async function onDelete(row: ScheduledJob) {
  await ElMessageBox.confirm(`确认删除任务「${row.name}」？`, '提示', { type: 'warning' })
  await deleteJob(row.id)
  ElMessage.success('删除成功')
  loadData()
}
async function toggleStatus(row: ScheduledJob, active: boolean) {
  const status = active ? JobStatus.Running : JobStatus.Paused
  try {
    await changeJobStatus(row.id, status)
    row.status = status
    ElMessage.success(active ? '已恢复调度' : '已暂停')
  } catch {
    loadData() // 失败回滚显示
  }
}
async function onRun(row: ScheduledJob) {
  await runJobOnce(row.id)
  ElMessage.success(`已触发「${row.name}」，稍后查看执行日志`)
}

// ---- 执行日志 ----
const logVisible = ref(false)
const logLoading = ref(false)
const logs = ref<JobLog[]>([])
const logTotal = ref(0)
const logQuery = reactive<JobLogQuery>({ pageIndex: 1, pageSize: 20 })

function openLogs() {
  logVisible.value = true
  reloadLogs()
}
async function loadLogs() {
  logLoading.value = true
  try {
    const res = await getJobLogs(logQuery)
    logs.value = res.data.items
    logTotal.value = res.data.total
  } finally {
    logLoading.value = false
  }
}
function reloadLogs() {
  logQuery.pageIndex = 1
  loadLogs()
}
async function onClearLogs() {
  await ElMessageBox.confirm('确认清空所有执行日志？', '提示', { type: 'warning' })
  await clearJobLogs()
  ElMessage.success('已清空')
  loadLogs()
}

loadTargets()
loadData()
</script>

<style scoped>
.cron {
  font-family: 'JetBrains Mono', Consolas, monospace;
  background: var(--el-fill-color-light);
  padding: 2px 6px;
  border-radius: 4px;
}
</style>
