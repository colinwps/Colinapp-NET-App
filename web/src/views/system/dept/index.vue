<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-button v-auth="'sys:dept:add'" type="success" @click="openCreate(0)">新增根部门</el-button>
        <el-button @click="loadData">刷新</el-button>
      </div>

      <el-table
        v-loading="loading"
        :data="tree"
        row-key="id"
        border
        default-expand-all
        :tree-props="{ children: 'children' }"
      >
        <el-table-column prop="name" label="部门名称" min-width="200" />
        <el-table-column prop="orderNum" label="排序" width="80" />
        <el-table-column prop="phone" label="电话" min-width="120" />
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.enabled ? 'success' : 'info'">{{ row.enabled ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'sys:dept:add'" link type="success" @click="openCreate(row.id)">新增下级</el-button>
            <el-button v-auth="'sys:dept:edit'" link type="primary" @click="openEdit(row as DeptNode)">编辑</el-button>
            <el-button v-auth="'sys:dept:remove'" link type="danger" @click="onDelete(row as DeptNode)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editId ? '编辑部门' : '新增部门'" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="上级部门">
          <el-tree-select
            v-model="form.parentId"
            :data="parentOptions"
            :props="{ label: 'name', children: 'children' }"
            check-strictly
            node-key="id"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="部门名称" prop="name"><el-input v-model="form.name" /></el-form-item>
        <el-form-item label="排序" prop="orderNum">
          <el-input-number v-model="form.orderNum" :min="0" />
        </el-form-item>
        <el-form-item label="电话"><el-input v-model="form.phone" /></el-form-item>
        <el-form-item label="邮箱"><el-input v-model="form.email" /></el-form-item>
        <el-form-item label="状态"><el-switch v-model="form.enabled" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="onSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  getDeptTree,
  createDept,
  updateDept,
  deleteDept,
  type DeptNode,
  type DeptSaveDto,
} from '@/api/dept'

const loading = ref(false)
const tree = ref<DeptNode[]>([])

async function loadData() {
  loading.value = true
  try {
    tree.value = (await getDeptTree()).data
  } finally {
    loading.value = false
  }
}

// 上级部门下拉（含「顶级」虚拟根）
const parentOptions = computed<DeptNode[]>(() => [
  { id: 0, name: '顶级', parentId: 0, children: tree.value } as unknown as DeptNode,
])

const dialogVisible = ref(false)
const saving = ref(false)
const editId = ref<number | null>(null)
const formRef = ref<FormInstance>()
const defaultForm = (): DeptSaveDto => ({
  name: '',
  parentId: 0,
  orderNum: 0,
  phone: '',
  email: '',
  enabled: true,
})
const form = reactive<DeptSaveDto>(defaultForm())
const rules: FormRules = {
  name: [{ required: true, message: '请输入部门名称', trigger: 'blur' }],
}

function openCreate(parentId: number) {
  editId.value = null
  Object.assign(form, defaultForm(), { parentId })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}

function openEdit(row: DeptNode) {
  editId.value = row.id
  Object.assign(form, {
    name: row.name,
    parentId: row.parentId,
    orderNum: row.orderNum,
    phone: row.phone,
    email: row.email,
    enabled: row.enabled,
  })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}

async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  saving.value = true
  try {
    if (editId.value) {
      await updateDept(editId.value, form)
      ElMessage.success('保存成功')
    } else {
      await createDept(form)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}

async function onDelete(row: DeptNode) {
  await ElMessageBox.confirm(`确认删除部门「${row.name}」？`, '提示', { type: 'warning' })
  await deleteDept(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadData()
</script>
