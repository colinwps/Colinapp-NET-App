<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input
          v-model="query.keyword"
          placeholder="角色名/编码"
          clearable
          style="width: 200px"
          @keyup.enter="reload"
        />
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button v-auth="'sys:role:add'" type="success" @click="openCreate">新增</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="name" label="角色名称" min-width="140" />
        <el-table-column prop="code" label="编码" min-width="120" />
        <el-table-column label="数据范围" min-width="130">
          <template #default="{ row }">{{ dataScopeLabel(row.dataScope) }}</template>
        </el-table-column>
        <el-table-column prop="orderNum" label="排序" width="80" />
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.enabled ? 'success' : 'info'">{{ row.enabled ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'sys:role:edit'" link type="primary" @click="openEdit(row as RoleItem)">编辑</el-button>
            <el-button v-auth="'sys:role:remove'" link type="danger" @click="onDelete(row as RoleItem)">删除</el-button>
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

    <el-dialog v-model="dialogVisible" :title="editId ? '编辑角色' : '新增角色'" width="600px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="角色名称" prop="name"><el-input v-model="form.name" /></el-form-item>
        <el-form-item label="角色编码" prop="code"><el-input v-model="form.code" /></el-form-item>
        <el-form-item label="排序"><el-input-number v-model="form.orderNum" :min="0" /></el-form-item>
        <el-form-item label="数据范围">
          <el-select v-model="form.dataScope" style="width: 100%">
            <el-option v-for="s in dataScopes" :key="s.value" :value="s.value" :label="s.label" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="form.dataScope === DataScope.Custom" label="自定义部门">
          <el-tree
            ref="deptTreeRef"
            :data="deptTree"
            :props="{ label: 'name', children: 'children' }"
            show-checkbox
            node-key="id"
            style="width: 100%; border: 1px solid #dcdfe6; border-radius: 4px; padding: 6px"
          />
        </el-form-item>
        <el-form-item label="菜单权限">
          <el-tree
            ref="menuTreeRef"
            :data="menuTree"
            :props="{ label: 'name', children: 'children' }"
            show-checkbox
            node-key="id"
            style="width: 100%; border: 1px solid #dcdfe6; border-radius: 4px; padding: 6px; max-height: 260px; overflow: auto"
          />
        </el-form-item>
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
import { nextTick, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import type { ElTree } from 'element-plus'
import {
  getRoles,
  getRole,
  createRole,
  updateRole,
  deleteRole,
  DataScope,
  type RoleItem,
  type RoleSaveDto,
} from '@/api/role'
import { getMenuTree, type MenuNode } from '@/api/menu'
import { getDeptTree, type DeptNode } from '@/api/dept'
import type { PagedQuery } from '@/api/types'

const dataScopes = [
  { value: DataScope.All, label: '全部数据' },
  { value: DataScope.Custom, label: '自定义部门' },
  { value: DataScope.Dept, label: '本部门' },
  { value: DataScope.DeptAndChild, label: '本部门及下级' },
  { value: DataScope.Self, label: '仅本人' },
]
const dataScopeLabel = (v: DataScope) => dataScopes.find((s) => s.value === v)?.label ?? '-'

const loading = ref(false)
const list = ref<RoleItem[]>([])
const total = ref(0)
const query = reactive<PagedQuery>({ pageIndex: 1, pageSize: 20, keyword: '' })

const menuTree = ref<MenuNode[]>([])
const deptTree = ref<DeptNode[]>([])
const menuTreeRef = ref<InstanceType<typeof ElTree>>()
const deptTreeRef = ref<InstanceType<typeof ElTree>>()

async function loadData() {
  loading.value = true
  try {
    const res = await getRoles(query)
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
const defaultForm = (): RoleSaveDto => ({
  name: '',
  code: '',
  dataScope: DataScope.All,
  orderNum: 0,
  enabled: true,
  remark: '',
  menuIds: [],
  deptIds: [],
})
const form = reactive<RoleSaveDto>(defaultForm())
const rules: FormRules = {
  name: [{ required: true, message: '请输入角色名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入角色编码', trigger: 'blur' }],
}

async function ensureTrees() {
  if (menuTree.value.length === 0) menuTree.value = (await getMenuTree()).data
  if (deptTree.value.length === 0) deptTree.value = (await getDeptTree()).data
}

function setTreeChecked(menuIds: number[], deptIds: number[]) {
  // 仅勾选叶子，父节点由半选自动体现，避免父勾选误传整棵
  menuTreeRef.value?.setCheckedKeys(menuIds, false)
  deptTreeRef.value?.setCheckedKeys(deptIds, false)
}

async function openCreate() {
  editId.value = null
  Object.assign(form, defaultForm())
  formRef.value?.clearValidate()
  dialogVisible.value = true
  await ensureTrees()
  await nextTick()
  setTreeChecked([], [])
}

async function openEdit(row: RoleItem) {
  editId.value = row.id
  await ensureTrees()
  const detail = (await getRole(row.id)).data
  Object.assign(form, {
    name: detail.name,
    code: detail.code,
    dataScope: detail.dataScope,
    orderNum: detail.orderNum,
    enabled: detail.enabled,
    remark: detail.remark,
    menuIds: detail.menuIds,
    deptIds: detail.deptIds,
  })
  formRef.value?.clearValidate()
  dialogVisible.value = true
  await nextTick()
  setTreeChecked(detail.menuIds, detail.deptIds)
}

function collectChecked(treeRef?: InstanceType<typeof ElTree>): number[] {
  if (!treeRef) return []
  return [...treeRef.getCheckedKeys(false), ...treeRef.getHalfCheckedKeys()] as number[]
}

async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  form.menuIds = collectChecked(menuTreeRef.value)
  form.deptIds =
    form.dataScope === DataScope.Custom ? (deptTreeRef.value?.getCheckedKeys(false) as number[]) : []
  saving.value = true
  try {
    if (editId.value) {
      await updateRole(editId.value, form)
      ElMessage.success('保存成功')
    } else {
      await createRole(form)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}

async function onDelete(row: RoleItem) {
  await ElMessageBox.confirm(`确认删除角色「${row.name}」？`, '提示', { type: 'warning' })
  await deleteRole(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadData()
</script>
