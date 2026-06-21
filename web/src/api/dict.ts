import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

export interface DictType {
  id: number
  name: string
  type: string
  enabled: boolean
  remark?: string
  createdTime: string
}

export interface DictTypeSaveDto {
  name: string
  type: string
  enabled: boolean
  remark?: string
}

export interface DictData {
  id: number
  dictType: string
  label: string
  value: string
  orderNum: number
  tagType?: string
  isDefault: boolean
  enabled: boolean
  remark?: string
}

export interface DictDataSaveDto {
  dictType: string
  label: string
  value: string
  orderNum: number
  tagType?: string
  isDefault: boolean
  enabled: boolean
  remark?: string
}

export const getDictTypes = (params: PagedQuery) =>
  http.get<PagedResult<DictType>>('/dict/type', params)
export const createDictType = (data: DictTypeSaveDto) => http.post('/dict/type', data)
export const updateDictType = (id: number, data: DictTypeSaveDto) =>
  http.put(`/dict/type/${id}`, data)
export const deleteDictType = (id: number) => http.delete(`/dict/type/${id}`)

export const getDictData = (type: string, params: PagedQuery) =>
  http.get<PagedResult<DictData>>('/dict/data', { type, ...params })
export const getDictOptions = (type: string) => http.get<DictData[]>(`/dict/data/options/${type}`)
export const createDictData = (data: DictDataSaveDto) => http.post('/dict/data', data)
export const updateDictData = (id: number, data: DictDataSaveDto) =>
  http.put(`/dict/data/${id}`, data)
export const deleteDictData = (id: number) => http.delete(`/dict/data/${id}`)
