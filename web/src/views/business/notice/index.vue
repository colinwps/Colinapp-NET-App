<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="标题" clearable style="width: 200px" @keyup.enter="reload" />
        <el-select v-model="query.noticeType" placeholder="类型" clearable style="width: 120px">
          <el-option :value="NoticeType.Notification" label="通知" />
          <el-option :value="NoticeType.Announcement" label="公告" />
        </el-select>
        <el-select v-model="query.published" placeholder="状态" clearable style="width: 120px">
          <el-option :value="true" label="已发布" />
          <el-option :value="false" label="草稿" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'biz:notice:add'" type="success" @click="openCreate">新增</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="title" label="标题" min-width="200" />
        <el-table-column label="类型" width="100">
          <template #default="{ row }">
            <el-tag :type="row.noticeType === 2 ? 'warning' : 'primary'">
              {{ row.noticeType === 2 ? '公告' : '通知' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.published ? 'success' : 'info'">{{ row.published ? '已发布' : '草稿' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="170">
          <template #default="{ row }">{{ formatTime(row.createdTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'biz:notice:edit'" link type="primary" @click="openEdit(row as Notice)">编辑</el-button>
            <el-button v-auth="'biz:notice:remove'" link type="danger" @click="onDelete(row as Notice)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" v-model:page-size="query.pageSize" :total="total" layout="total, prev, pager, next" @current-change="loadData" />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editId ? '编辑公告' : '新增公告'" width="600px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="标题" prop="title"><el-input v-model="form.title" /></el-form-item>
        <el-form-item label="类型">
          <el-radio-group v-model="form.noticeType">
            <el-radio :value="NoticeType.Notification">通知</el-radio>
            <el-radio :value="NoticeType.Announcement">公告</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="内容" prop="content">
          <el-input v-model="form.content" type="textarea" :rows="6" />
        </el-form-item>
        <el-form-item label="发布"><el-switch v-model="form.published" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="onSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  getNotices, getNotice, createNotice, updateNotice, deleteNotice,
  NoticeType, type Notice, type NoticeQuery, type NoticeSaveDto,
} from '@/api/notice'
import { formatTime } from '@/utils/format'

const loading = ref(false)
const list = ref<Notice[]>([])
const total = ref(0)
const query = reactive<NoticeQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadData() {
  loading.value = true
  try {
    const res = await getNotices(query)
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
const defaultForm = (): NoticeSaveDto => ({ title: '', noticeType: NoticeType.Notification, content: '', published: false })
const form = reactive<NoticeSaveDto>(defaultForm())
const rules: FormRules = {
  title: [{ required: true, message: '请输入标题', trigger: 'blur' }],
  content: [{ required: true, message: '请输入内容', trigger: 'blur' }],
}

function openCreate() {
  editId.value = null
  Object.assign(form, defaultForm())
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
async function openEdit(row: Notice) {
  editId.value = row.id
  const detail = (await getNotice(row.id)).data
  Object.assign(form, { title: detail.title, noticeType: detail.noticeType, content: detail.content, published: detail.published })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  saving.value = true
  try {
    if (editId.value) await updateNotice(editId.value, form)
    else await createNotice(form)
    ElMessage.success('保存成功')
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}
async function onDelete(row: Notice) {
  await ElMessageBox.confirm(`确认删除公告「${row.title}」？`, '提示', { type: 'warning' })
  await deleteNotice(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadData()
</script>
