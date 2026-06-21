<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input
          v-model="query.keyword"
          placeholder="职位名/编码"
          clearable
          style="width: 200px"
          @keyup.enter="reload"
        />
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'sys:post:add'" type="success" @click="openCreate">新增</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="name" label="职位名称" min-width="140" />
        <el-table-column prop="code" label="编码" min-width="120" />
        <el-table-column prop="orderNum" label="排序" width="80" />
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.enabled ? 'success' : 'info'">{{ row.enabled ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="160" />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'sys:post:edit'" link type="primary" @click="openEdit(row as PositionItem)">编辑</el-button>
            <el-button v-auth="'sys:post:remove'" link type="danger" @click="onDelete(row as PositionItem)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="query.pageIndex"
          v-model:page-size="query.pageSize"
          :total="total"
          layout="total, prev, pager, next"
          @current-change="loadData"
        />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editId ? '编辑职位' : '新增职位'" width="480px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="名称" prop="name"><el-input v-model="form.name" /></el-form-item>
        <el-form-item label="编码" prop="code"><el-input v-model="form.code" /></el-form-item>
        <el-form-item label="排序"><el-input-number v-model="form.orderNum" :min="0" /></el-form-item>
        <el-form-item label="状态"><el-switch v-model="form.enabled" /></el-form-item>
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
  getPositions,
  createPosition,
  updatePosition,
  deletePosition,
  type PositionItem,
  type PositionSaveDto,
} from '@/api/position'
import type { PagedQuery } from '@/api/types'

const loading = ref(false)
const list = ref<PositionItem[]>([])
const total = ref(0)
const query = reactive<PagedQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

async function loadData() {
  loading.value = true
  try {
    const res = await getPositions(query)
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
const defaultForm = (): PositionSaveDto => ({ name: '', code: '', orderNum: 0, enabled: true, remark: '' })
const form = reactive<PositionSaveDto>(defaultForm())
const rules: FormRules = {
  name: [{ required: true, message: '请输入职位名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入编码', trigger: 'blur' }],
}

function openCreate() {
  editId.value = null
  Object.assign(form, defaultForm())
  formRef.value?.clearValidate()
  dialogVisible.value = true
}
function openEdit(row: PositionItem) {
  editId.value = row.id
  Object.assign(form, { name: row.name, code: row.code, orderNum: row.orderNum, enabled: row.enabled, remark: row.remark })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}

async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  saving.value = true
  try {
    if (editId.value) {
      await updatePosition(editId.value, form)
      ElMessage.success('保存成功')
    } else {
      await createPosition(form)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}

async function onDelete(row: PositionItem) {
  await ElMessageBox.confirm(`确认删除职位「${row.name}」？`, '提示', { type: 'warning' })
  await deletePosition(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadData()
</script>
