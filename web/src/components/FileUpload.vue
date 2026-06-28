<template>
  <el-upload
    :action="UPLOAD_URL"
    :headers="headers"
    :data="extraData"
    :accept="accept"
    :show-file-list="false"
    :before-upload="beforeUpload"
    :on-success="onSuccess"
    :on-error="onError"
  >
    <slot>
      <el-button type="primary" :icon="Upload">{{ buttonText }}</el-button>
    </slot>
  </el-upload>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { ElMessage, type UploadRawFile } from 'element-plus'
import { Upload } from '@element-plus/icons-vue'
import type { ApiResult } from '@/api/types'
import { UPLOAD_URL, type FileRecord } from '@/api/file'
import { useUserStore } from '@/stores/user'

const props = withDefaults(
  defineProps<{
    bizType?: string
    accept?: string
    /** 单文件大小上限（MB） */
    maxSizeMB?: number
    buttonText?: string
  }>(),
  { maxSizeMB: 50, buttonText: '上传文件' },
)

const emit = defineEmits<{ success: [file: FileRecord] }>()

const userStore = useUserStore()
const headers = computed(() => ({ Authorization: `Bearer ${userStore.token}` }))
const extraData = computed(() => (props.bizType ? { bizType: props.bizType } : {}))

function beforeUpload(file: UploadRawFile) {
  if (file.size > props.maxSizeMB * 1024 * 1024) {
    ElMessage.error(`文件大小不能超过 ${props.maxSizeMB}MB`)
    return false
  }
  return true
}

function onSuccess(response: ApiResult<FileRecord>) {
  if (response.code === 200) {
    ElMessage.success('上传成功')
    emit('success', response.data)
  } else {
    ElMessage.error(response.message || '上传失败')
  }
}

function onError() {
  ElMessage.error('上传失败，请检查网络或登录状态')
}
</script>
