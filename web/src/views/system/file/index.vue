<template>
  <div class="page-container">
    <el-card>
      <div class="table-toolbar">
        <el-input
          v-model="query.keyword"
          placeholder="文件名"
          clearable
          style="width: 200px"
          @keyup.enter="reload"
        />
        <el-input
          v-model="query.bizType"
          placeholder="业务分类"
          clearable
          style="width: 160px"
          @keyup.enter="reload"
        />
        <el-button type="primary" @click="reload">查询</el-button>
        <FileUpload v-auth="'sys:file:upload'" button-text="上传文件" @success="onUploaded" />
      </div>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column label="预览" width="90" align="center">
          <template #default="{ row }">
            <el-image
              v-if="isImage(row as FileRecord)"
              :src="(row as FileRecord).url"
              :preview-src-list="[(row as FileRecord).url]"
              preview-teleported
              fit="cover"
              style="width: 44px; height: 44px; border-radius: 4px"
            />
            <el-tag v-else type="info">{{ (row as FileRecord).ext || '文件' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="fileName" label="文件名" min-width="220" show-overflow-tooltip />
        <el-table-column label="大小" width="110">
          <template #default="{ row }">{{ formatSize((row as FileRecord).size) }}</template>
        </el-table-column>
        <el-table-column prop="bizType" label="业务分类" width="120">
          <template #default="{ row }">{{ (row as FileRecord).bizType || '-' }}</template>
        </el-table-column>
        <el-table-column prop="storageType" label="存储" width="90" />
        <el-table-column label="上传时间" width="170">
          <template #default="{ row }">{{ formatTime((row as FileRecord).createdTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="onDownload(row as FileRecord)">下载</el-button>
            <el-button link type="primary" @click="onCopy(row as FileRecord)">复制链接</el-button>
            <el-button v-auth="'sys:file:remove'" link type="danger" @click="onDelete(row as FileRecord)">
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
          layout="total, prev, pager, next"
          @current-change="loadData"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getFiles, deleteFile, type FileRecord, type FileQuery } from '@/api/file'
import FileUpload from '@/components/FileUpload.vue'
import { formatTime } from '@/utils/format'

const loading = ref(false)
const list = ref<FileRecord[]>([])
const total = ref(0)
const query = reactive<FileQuery>({ pageIndex: 1, pageSize: 20, keyword: '', bizType: '' })

async function loadData() {
  loading.value = true
  try {
    const res = await getFiles(query)
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

function onUploaded() {
  reload()
}

function isImage(row: FileRecord) {
  return (row.contentType ?? '').startsWith('image/')
}

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(2)} MB`
}

function onDownload(row: FileRecord) {
  window.open(row.url, '_blank')
}

async function onCopy(row: FileRecord) {
  const link = `${window.location.origin}${row.url}`
  try {
    await navigator.clipboard.writeText(link)
    ElMessage.success('链接已复制')
  } catch {
    ElMessage.warning(link)
  }
}

async function onDelete(row: FileRecord) {
  await ElMessageBox.confirm(`确认删除文件「${row.fileName}」？`, '提示', { type: 'warning' })
  await deleteFile(row.id)
  ElMessage.success('删除成功')
  loadData()
}

loadData()
</script>
