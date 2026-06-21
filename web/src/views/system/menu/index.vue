<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-button v-auth="'sys:menu:add'" type="success" @click="openCreate(0)">新增根菜单</el-button>
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
        <el-table-column prop="name" label="菜单名称" min-width="180" />
        <el-table-column label="类型" width="90">
          <template #default="{ row }">
            <el-tag :type="menuTypeTag(row.menuType)">{{ menuTypeLabel(row.menuType) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="icon" label="图标" width="80">
          <template #default="{ row }">
            <el-icon v-if="row.icon"><component :is="row.icon" /></el-icon>
          </template>
        </el-table-column>
        <el-table-column prop="permission" label="权限标识" min-width="160" />
        <el-table-column prop="component" label="组件路径" min-width="180" />
        <el-table-column prop="orderNum" label="排序" width="70" />
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button v-auth="'sys:menu:add'" link type="success" @click="openCreate(row.id)">新增下级</el-button>
            <el-button v-auth="'sys:menu:edit'" link type="primary" @click="openEdit(row as MenuNode)">编辑</el-button>
            <el-button v-auth="'sys:menu:remove'" link type="danger" @click="onDelete(row as MenuNode)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editId ? '编辑菜单' : '新增菜单'" width="600px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="上级菜单">
          <el-tree-select
            v-model="form.parentId"
            :data="parentOptions"
            :props="{ label: 'name', children: 'children' }"
            check-strictly
            node-key="id"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="菜单类型">
          <el-radio-group v-model="form.menuType">
            <el-radio :value="MenuType.Catalog">目录</el-radio>
            <el-radio :value="MenuType.Menu">菜单</el-radio>
            <el-radio :value="MenuType.Button">按钮</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="菜单名称" prop="name"><el-input v-model="form.name" /></el-form-item>
        <el-form-item label="排序"><el-input-number v-model="form.orderNum" :min="0" /></el-form-item>
        <template v-if="form.menuType !== MenuType.Button">
          <el-form-item label="图标"><el-input v-model="form.icon" placeholder="Element Plus 图标名，如 Setting" /></el-form-item>
          <el-form-item label="路由路径"><el-input v-model="form.path" placeholder="如 /system 或 user" /></el-form-item>
          <el-form-item label="组件路径"><el-input v-model="form.component" placeholder="如 system/user/index" /></el-form-item>
        </template>
        <el-form-item label="权限标识"><el-input v-model="form.permission" placeholder="如 sys:user:list" /></el-form-item>
        <el-form-item label="显示"><el-switch v-model="form.visible" /></el-form-item>
        <el-form-item label="缓存"><el-switch v-model="form.cache" /></el-form-item>
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
  getMenuTree,
  getMenu,
  createMenu,
  updateMenu,
  deleteMenu,
  MenuType,
  type MenuNode,
  type MenuSaveDto,
} from '@/api/menu'

const menuTypeLabel = (t: MenuType) => ({ 1: '目录', 2: '菜单', 3: '按钮' })[t] ?? '-'
const menuTypeTag = (t: MenuType) => ({ 1: 'warning', 2: 'primary', 3: 'info' })[t] as never

const loading = ref(false)
const tree = ref<MenuNode[]>([])

async function loadData() {
  loading.value = true
  try {
    tree.value = (await getMenuTree()).data
  } finally {
    loading.value = false
  }
}

const parentOptions = computed<MenuNode[]>(() => [
  { id: 0, name: '顶级', children: tree.value } as unknown as MenuNode,
])

const dialogVisible = ref(false)
const saving = ref(false)
const editId = ref<number | null>(null)
const formRef = ref<FormInstance>()
const defaultForm = (): MenuSaveDto => ({
  name: '',
  parentId: 0,
  orderNum: 0,
  menuType: MenuType.Menu,
  path: '',
  component: '',
  permission: '',
  icon: '',
  isExternal: false,
  cache: false,
  visible: true,
  enabled: true,
})
const form = reactive<MenuSaveDto>(defaultForm())
const rules: FormRules = { name: [{ required: true, message: '请输入菜单名称', trigger: 'blur' }] }

function openCreate(parentId: number) {
  editId.value = null
  Object.assign(form, defaultForm(), { parentId })
  formRef.value?.clearValidate()
  dialogVisible.value = true
}

async function openEdit(row: MenuNode) {
  editId.value = row.id
  const detail = (await getMenu(row.id)).data
  Object.assign(form, detail)
  formRef.value?.clearValidate()
  dialogVisible.value = true
}

async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  saving.value = true
  try {
    if (editId.value) {
      await updateMenu(editId.value, form)
      ElMessage.success('保存成功')
    } else {
      await createMenu(form)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } finally {
    saving.value = false
  }
}

async function onDelete(row: MenuNode) {
  await ElMessageBox.confirm(`确认删除菜单「${row.name}」？`, '提示', { type: 'warning' })
  await deleteMenu(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadData()
</script>
