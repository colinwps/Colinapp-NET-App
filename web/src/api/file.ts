import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

export interface FileRecord {
  id: number
  fileName: string
  url: string
  contentType: string | null
  size: number
  ext: string | null
  bizType: string | null
  storageType: string
  createdTime: string
}

export interface FileQuery extends PagedQuery {
  bizType?: string
}

export const getFiles = (params: FileQuery) => http.get<PagedResult<FileRecord>>('/file', params)
export const deleteFile = (id: number) => http.delete(`/file/${id}`)

/** 上传地址（供 el-upload 的 action 使用） */
export const UPLOAD_URL = '/api/file/upload'
