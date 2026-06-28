<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input
          v-model="query.keyword"
          placeholder="账号/姓名"
          clearable
          style="width: 200px"
          @keyup.enter="reload"
        />
        <el-select v-model="query.enabled" placeholder="状态" clearable style="width: 120px">
          <el-option :value="true" label="启用" />
          <el-option :value="false" label="停用" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'sys:user:add'" type="success" @click="openCreate">新增</el-button>
        <el-button v-auth="'sys:user:export'" :icon="Download" :loading="exporting" @click="onExport">
          导出
        </el-button>
        <el-button v-auth="'sys:user:import'" :icon="Upload" @click="openImport">导入</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="userName" label="账号" min-width="120" />
        <el-table-column prop="nickName" label="姓名" min-width="120" />
        <el-table-column prop="deptName" label="部门" min-width="120" />
        <el-table-column prop="phone" label="手机号" min-width="120" />
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-switch v-model="row.enabled" :disabled="row.isAdmin" @change="onToggle(row as UserListItem)" />
          </template>
        </el-table-column>
        <el-table-column prop="lastLoginTime" label="最后登录" min-width="160">
          <template #default="{ row }">{{ formatTime(row.lastLoginTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'sys:user:edit'" link type="primary" @click="openEdit(row as UserListItem)">编辑</el-button>
            <el-button v-auth="'sys:user:resetPwd'" link type="warning" @click="onResetPwd(row as UserListItem)">重置密码</el-button>
            <el-button
              v-auth="'sys:user:remove'"
              link
              type="danger"
              :disabled="row.isAdmin"
              @click="onDelete(row as UserListItem)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="query.pageIndex"
          v-model:page-size="query.pageSize"
          :total="total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          @current-change="loadData"
          @size-change="reload"
        />
      </div>
    </el-card>

    <!-- 新增/编辑 -->
    <el-dialog v-model="dialogVisible" :title="editId ? '编辑用户' : '新增用户'" width="560px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="账号" prop="userName">
          <el-input v-model="form.userName" :disabled="!!editId" />
        </el-form-item>
        <el-form-item label="姓名" prop="nickName">
          <el-input v-model="form.nickName" />
        </el-form-item>
        <el-form-item v-if="!editId" label="密码" prop="password">
          <el-input v-model="form.password" type="password" show-password />
        </el-form-item>
        <el-form-item label="部门">
          <el-tree-select
            v-model="form.deptId"
            :data="deptTree"
            :props="{ label: 'name', children: 'children' }"
            check-strictly
            clearable
            node-key="id"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="手机号"><el-input v-model="form.phone" /></el-form-item>
        <el-form-item label="邮箱"><el-input v-model="form.email" /></el-form-item>
        <el-form-item label="角色">
          <el-select v-model="form.roleIds" multiple style="width: 100%">
            <el-option v-for="r in roleOptions" :key="r.id" :value="r.id" :label="r.name" />
          </el-select>
        </el-form-item>
        <el-form-item label="职位">
          <el-select v-model="form.postIds" multiple style="width: 100%">
            <el-option v-for="p in postOptions" :key="p.id" :value="p.id" :label="p.name" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-switch v-model="form.enabled" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="onSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 导入 -->
    <el-dialog v-model="importVisible" title="导入用户" width="520px">
      <el-upload
        drag
        :auto-upload="false"
        :limit="1"
        accept=".xlsx,.xls"
        :on-change="onFileChange"
        :on-exceed="onExceed"
      >
        <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
        <div class="el-upload__text">将文件拖到此处，或<em>点击选择</em></div>
        <template #tip>
          <div class="import-tip">
            仅支持 .xlsx / .xls；未填初始密码默认 <b>123456</b>。
            <el-link type="primary" :underline="false" @click="onDownloadTemplate">下载导入模板</el-link>
          </div>
        </template>
      </el-upload>

      <el-checkbox v-model="updateExisting" style="margin-top: 8px">同名账号则更新（否则视为冲突跳过）</el-checkbox>

      <el-alert
        v-if="importResult"
        :title="`共 ${importResult.total} 行，成功 ${importResult.success}，失败 ${importResult.failed}`"
        :type="importResult.failed ? 'warning' : 'success'"
        :closable="false"
        style="margin-top: 12px"
      >
        <div v-if="importResult.errors.length" class="import-errors">
          <div v-for="(e, i) in importResult.errors" :key="i">{{ e }}</div>
        </div>
      </el-alert>

      <template #footer>
        <el-button @click="importVisible = false">关闭</el-button>
        <el-button type="primary" :loading="importing" :disabled="!importFile" @click="onImport">
          开始导入
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import {
  ElMessage,
  ElMessageBox,
  type FormInstance,
  type FormRules,
  type UploadFile,
  type UploadRawFile,
} from 'element-plus'
import { Download, Upload, UploadFilled } from '@element-plus/icons-vue'
import {
  getUsers,
  getUser,
  createUser,
  updateUser,
  deleteUser,
  resetPassword,
  changeStatus,
  exportUsers,
  downloadUserTemplate,
  importUsers,
  type UserListItem,
  type UserQuery,
  type UserSaveDto,
  type UserImportResult,
} from '@/api/user'
import { getRoleOptions, type RoleItem } from '@/api/role'
import { getPositionOptions, type PositionItem } from '@/api/position'
import { getDeptTree, type DeptNode } from '@/api/dept'
import { formatTime } from '@/utils/format'

const loading = ref(false)
const list = ref<UserListItem[]>([])
const total = ref(0)
const query = reactive<UserQuery>({ pageIndex: 1, pageSize: 20, keyword: '', enabled: undefined })

const roleOptions = ref<RoleItem[]>([])
const postOptions = ref<PositionItem[]>([])
const deptTree = ref<DeptNode[]>([])

async function loadData() {
  loading.value = true
  try {
    const res = await getUsers(query)
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

async function loadOptions() {
  roleOptions.value = (await getRoleOptions()).data
  postOptions.value = (await getPositionOptions()).data
  deptTree.value = (await getDeptTree()).data
}

// 弹窗
const dialogVisible = ref(false)
const saving = ref(false)
const editId = ref<number | null>(null)
const formRef = ref<FormInstance>()
const defaultForm = (): UserSaveDto => ({
  userName: '',
  nickName: '',
  password: '',
  phone: '',
  email: '',
  deptId: undefined,
  enabled: true,
  roleIds: [],
  postIds: [],
})
const form = reactive<UserSaveDto>(defaultForm())
const rules: FormRules = {
  userName: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  nickName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
}

function resetForm() {
  Object.assign(form, defaultForm())
  formRef.value?.clearValidate()
}

function openCreate() {
  editId.value = null
  resetForm()
  dialogVisible.value = true
}

async function openEdit(row: UserListItem) {
  editId.value = row.id
  resetForm()
  const detail = (await getUser(row.id)).data
  Object.assign(form, {
    userName: detail.userName,
    nickName: detail.nickName,
    password: '',
    phone: detail.phone,
    email: detail.email,
    deptId: detail.deptId,
    enabled: detail.enabled,
    roleIds: detail.roleIds,
    postIds: detail.postIds,
  })
  dialogVisible.value = true
}

async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  saving.value = true
  try {
    if (editId.value) {
      await updateUser(editId.value, form)
      ElMessage.success('保存成功')
    } else {
      await createUser(form)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}

async function onToggle(row: UserListItem) {
  try {
    await changeStatus(row.id, row.enabled)
    ElMessage.success('已更新状态')
  } catch {
    row.enabled = !row.enabled
  }
}

async function onResetPwd(row: UserListItem) {
  const { value } = await ElMessageBox.prompt('请输入新密码', `重置 ${row.userName} 的密码`, {
    inputType: 'password',
  })
  await resetPassword(row.id, value)
  ElMessage.success('密码已重置')
}

async function onDelete(row: UserListItem) {
  await ElMessageBox.confirm(`确认删除用户「${row.userName}」？`, '提示', { type: 'warning' })
  await deleteUser(row.id)
  ElMessage.success('删除成功')
  loadData()
}

// ---- 导出 / 导入 ----
const exporting = ref(false)
const importVisible = ref(false)
const importing = ref(false)
const importFile = ref<UploadRawFile | null>(null)
const updateExisting = ref(false)
const importResult = ref<UserImportResult | null>(null)

function saveBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

async function onExport() {
  exporting.value = true
  try {
    const blob = await exportUsers(query)
    saveBlob(blob, `用户_${Date.now()}.xlsx`)
  } finally {
    exporting.value = false
  }
}

function openImport() {
  importFile.value = null
  importResult.value = null
  updateExisting.value = false
  importVisible.value = true
}
function onFileChange(file: UploadFile) {
  importFile.value = (file.raw as UploadRawFile) ?? null
}
function onExceed(files: File[]) {
  importFile.value = files[0] as UploadRawFile
}
async function onDownloadTemplate() {
  const blob = await downloadUserTemplate()
  saveBlob(blob, '用户导入模板.xlsx')
}
async function onImport() {
  if (!importFile.value) return
  importing.value = true
  try {
    const res = await importUsers(importFile.value, updateExisting.value)
    importResult.value = res.data
    ElMessage.success('导入完成')
    loadData()
  } finally {
    importing.value = false
  }
}

loadData()
loadOptions()
</script>

<style scoped lang="scss">
.import-tip {
  font-size: 12px;
  color: #909399;
  margin-top: 8px;
  display: flex;
  align-items: center;
  gap: 10px;
}
.import-errors {
  max-height: 160px;
  overflow: auto;
  margin-top: 6px;
  font-size: 12px;
  line-height: 1.6;
}
</style>
