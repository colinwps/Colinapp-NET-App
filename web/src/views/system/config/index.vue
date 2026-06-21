<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input v-model="query.keyword" placeholder="参数名/键" clearable style="width: 200px" @keyup.enter="reload" />
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'sys:config:add'" type="success" @click="openCreate">新增</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="name" label="参数名称" min-width="140" />
        <el-table-column prop="configKey" label="参数键" min-width="160" />
        <el-table-column prop="configValue" label="参数值" min-width="160" />
        <el-table-column label="内置" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isSystem ? 'warning' : 'info'">{{ row.isSystem ? '是' : '否' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="140" />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'sys:config:edit'" link type="primary" @click="openEdit(row as ConfigItem)">编辑</el-button>
            <el-button v-auth="'sys:config:remove'" link type="danger" :disabled="row.isSystem" @click="onDelete(row as ConfigItem)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination v-model:current-page="query.pageIndex" :total="total" :page-size="query.pageSize" layout="total, prev, pager, next" @current-change="loadData" />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editId ? '编辑参数' : '新增参数'" width="480px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="参数名称" prop="name"><el-input v-model="form.name" /></el-form-item>
        <el-form-item label="参数键" prop="configKey"><el-input v-model="form.configKey" /></el-form-item>
        <el-form-item label="参数值" prop="configValue"><el-input v-model="form.configValue" /></el-form-item>
        <el-form-item label="系统内置"><el-switch v-model="form.isSystem" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" /></el-form-item>
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
  getConfigs, createConfig, updateConfig, deleteConfig,
  type ConfigItem, type ConfigSaveDto,
} from '@/api/config'
import type { PagedQuery } from '@/api/types'

const loading = ref(false)
const list = ref<ConfigItem[]>([])
const total = ref(0)
const query = reactive<PagedQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadData() {
  loading.value = true
  try {
    const res = await getConfigs(query)
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
const defaultForm = (): ConfigSaveDto => ({ name: '', configKey: '', configValue: '', isSystem: false, remark: '' })
const form = reactive<ConfigSaveDto>(defaultForm())
const rules: FormRules = {
  name: [{ required: true, message: '请输入参数名称', trigger: 'blur' }],
  configKey: [{ required: true, message: '请输入参数键', trigger: 'blur' }],
  configValue: [{ required: true, message: '请输入参数值', trigger: 'blur' }],
}

function openCreate() {
  editId.value = null
  Object.assign(form, defaultForm())
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
function openEdit(row: ConfigItem) {
  editId.value = row.id
  Object.assign(form, { name: row.name, configKey: row.configKey, configValue: row.configValue, isSystem: row.isSystem, remark: row.remark })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  saving.value = true
  try {
    if (editId.value) await updateConfig(editId.value, form)
    else await createConfig(form)
    ElMessage.success('保存成功')
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}
async function onDelete(row: ConfigItem) {
  await ElMessageBox.confirm(`确认删除参数「${row.name}」？`, '提示', { type: 'warning' })
  await deleteConfig(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadData()
</script>
