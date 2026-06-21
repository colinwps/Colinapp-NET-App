<template>
  <div class="page-container">
    <el-row :gutter="16">
      <!-- 字典类型 -->
      <el-col :span="10">
        <el-card>
          <div class="table-toolbar">
            <el-input v-model="typeQuery.keyword" placeholder="字典名/类型" clearable style="width: 160px" @keyup.enter="reloadTypes" />
            <el-button type="primary" @click="reloadTypes">查询</el-button>
            <el-button v-auth="'sys:dict:add'" type="success" @click="openTypeCreate">新增</el-button>
          </div>
          <el-table v-loading="typeLoading" :data="types" border highlight-current-row @current-change="onSelectType">
            <el-table-column prop="name" label="字典名称" min-width="110" />
            <el-table-column prop="type" label="类型编码" min-width="130" />
            <el-table-column label="操作" width="130">
              <template #default="{ row }">
                <el-button v-auth="'sys:dict:edit'" link type="primary" @click.stop="openTypeEdit(row as DictType)">编辑</el-button>
                <el-button v-auth="'sys:dict:remove'" link type="danger" @click.stop="onTypeDelete(row as DictType)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
          <div class="pagination-wrapper">
            <el-pagination v-model:current-page="typeQuery.pageIndex" :total="typeTotal" :page-size="typeQuery.pageSize" layout="total, prev, pager, next" @current-change="loadTypes" />
          </div>
        </el-card>
      </el-col>

      <!-- 字典数据 -->
      <el-col :span="14">
        <el-card>
          <div class="table-toolbar">
            <span class="current-type">当前字典：<b>{{ currentType?.name ?? '请选择左侧字典' }}</b></span>
            <el-button v-auth="'sys:dict:add'" type="success" :disabled="!currentType" @click="openDataCreate">新增数据</el-button>
          </div>
          <el-table v-loading="dataLoading" :data="dataList" border>
            <el-table-column prop="label" label="标签" min-width="100">
              <template #default="{ row }">
                <el-tag v-if="row.tagType" :type="tagOf(row.tagType)">{{ row.label }}</el-tag>
                <span v-else>{{ row.label }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="value" label="键值" min-width="100" />
            <el-table-column prop="orderNum" label="排序" width="70" />
            <el-table-column label="操作" width="130">
              <template #default="{ row }">
                <el-button v-auth="'sys:dict:edit'" link type="primary" @click="openDataEdit(row as DictData)">编辑</el-button>
                <el-button v-auth="'sys:dict:remove'" link type="danger" @click="onDataDelete(row as DictData)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>

    <!-- 字典类型弹窗 -->
    <el-dialog v-model="typeDialog" :title="typeEditId ? '编辑字典类型' : '新增字典类型'" width="460px">
      <el-form ref="typeFormRef" :model="typeForm" :rules="typeRules" label-width="90px">
        <el-form-item label="字典名称" prop="name"><el-input v-model="typeForm.name" /></el-form-item>
        <el-form-item label="类型编码" prop="type"><el-input v-model="typeForm.type" /></el-form-item>
        <el-form-item label="状态"><el-switch v-model="typeForm.enabled" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="typeForm.remark" type="textarea" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="typeDialog = false">取消</el-button>
        <el-button type="primary" @click="onTypeSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字典数据弹窗 -->
    <el-dialog v-model="dataDialog" :title="dataEditId ? '编辑字典数据' : '新增字典数据'" width="460px">
      <el-form ref="dataFormRef" :model="dataForm" :rules="dataRules" label-width="90px">
        <el-form-item label="标签" prop="label"><el-input v-model="dataForm.label" /></el-form-item>
        <el-form-item label="键值" prop="value"><el-input v-model="dataForm.value" /></el-form-item>
        <el-form-item label="排序"><el-input-number v-model="dataForm.orderNum" :min="0" /></el-form-item>
        <el-form-item label="标签样式">
          <el-select v-model="dataForm.tagType" clearable placeholder="默认">
            <el-option v-for="t in tagTypes" :key="t" :value="t" :label="t" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态"><el-switch v-model="dataForm.enabled" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dataDialog = false">取消</el-button>
        <el-button type="primary" @click="onDataSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  getDictTypes, createDictType, updateDictType, deleteDictType,
  getDictData, createDictData, updateDictData, deleteDictData,
  type DictType, type DictTypeSaveDto, type DictData, type DictDataSaveDto,
} from '@/api/dict'
import type { PagedQuery } from '@/api/types'

const tagTypes = ['primary', 'success', 'info', 'warning', 'danger']
// el-tag 的 type 是字面量联合，字典里存的是普通字符串，这里做一次放宽
const tagOf = (t?: string) => t as 'primary' | 'success' | 'info' | 'warning' | 'danger'

// ---- 字典类型 ----
const typeLoading = ref(false)
const types = ref<DictType[]>([])
const typeTotal = ref(0)
const typeQuery = reactive<PagedQuery>({ pageIndex: 1, pageSize: 10, keyword: '' })
const currentType = ref<DictType | null>(null)

async function loadTypes() {
  typeLoading.value = true
  try {
    const res = await getDictTypes(typeQuery)
    types.value = res.data.items
    typeTotal.value = res.data.total
  } finally {
    typeLoading.value = false
  }
}
function reloadTypes() {
  typeQuery.pageIndex = 1
  loadTypes()
}
function onSelectType(row: DictType | null) {
  currentType.value = row
  if (row) loadData()
  else dataList.value = []
}

const typeDialog = ref(false)
const typeEditId = ref<number | null>(null)
const typeFormRef = ref<FormInstance>()
const typeForm = reactive<DictTypeSaveDto>({ name: '', type: '', enabled: true, remark: '' })
const typeRules: FormRules = {
  name: [{ required: true, message: '请输入字典名称', trigger: 'blur' }],
  type: [{ required: true, message: '请输入类型编码', trigger: 'blur' }],
}
function openTypeCreate() {
  typeEditId.value = null
  Object.assign(typeForm, { name: '', type: '', enabled: true, remark: '' })
  typeDialog.value = true
}
function openTypeEdit(row: DictType) {
  typeEditId.value = row.id
  Object.assign(typeForm, { name: row.name, type: row.type, enabled: row.enabled, remark: row.remark })
  typeDialog.value = true
}
async function onTypeSubmit() {
  await typeFormRef.value?.validate()
  if (typeEditId.value) await updateDictType(typeEditId.value, typeForm)
  else await createDictType(typeForm)
  ElMessage.success('保存成功')
  typeDialog.value = false
  loadTypes()
}
async function onTypeDelete(row: DictType) {
  await ElMessageBox.confirm(`确认删除字典「${row.name}」？`, '提示', { type: 'warning' })
  await deleteDictType(row.id)
  ElMessage.success('删除成功')
  loadTypes()
}

// ---- 字典数据 ----
const dataLoading = ref(false)
const dataList = ref<DictData[]>([])
const dataQuery = reactive<PagedQuery>({ pageIndex: 1, pageSize: 100, keyword: '' })

async function loadData() {
  if (!currentType.value) return
  dataLoading.value = true
  try {
    const res = await getDictData(currentType.value.type, dataQuery)
    dataList.value = res.data.items
  } finally {
    dataLoading.value = false
  }
}

const dataDialog = ref(false)
const dataEditId = ref<number | null>(null)
const dataFormRef = ref<FormInstance>()
const defaultData = (): DictDataSaveDto => ({
  dictType: '', label: '', value: '', orderNum: 0, tagType: undefined, isDefault: false, enabled: true,
})
const dataForm = reactive<DictDataSaveDto>(defaultData())
const dataRules: FormRules = {
  label: [{ required: true, message: '请输入标签', trigger: 'blur' }],
  value: [{ required: true, message: '请输入键值', trigger: 'blur' }],
}
function openDataCreate() {
  dataEditId.value = null
  Object.assign(dataForm, defaultData(), { dictType: currentType.value!.type })
  dataDialog.value = true
}
function openDataEdit(row: DictData) {
  dataEditId.value = row.id
  Object.assign(dataForm, { ...row })
  dataDialog.value = true
}
async function onDataSubmit() {
  await dataFormRef.value?.validate()
  if (dataEditId.value) await updateDictData(dataEditId.value, dataForm)
  else await createDictData(dataForm)
  ElMessage.success('保存成功')
  dataDialog.value = false
  loadData()
}
async function onDataDelete(row: DictData) {
  await ElMessageBox.confirm(`确认删除「${row.label}」？`, '提示', { type: 'warning' })
  await deleteDictData(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadTypes()
</script>

<style scoped>
.current-type {
  margin-right: auto;
}
</style>
