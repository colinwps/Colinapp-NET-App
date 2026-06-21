import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

export interface ConfigItem {
  id: number
  name: string
  configKey: string
  configValue: string
  isSystem: boolean
  remark?: string
  createdTime: string
}

export interface ConfigSaveDto {
  name: string
  configKey: string
  configValue: string
  isSystem: boolean
  remark?: string
}

export const getConfigs = (params: PagedQuery) =>
  http.get<PagedResult<ConfigItem>>('/config', params)
export const getConfigValue = (key: string) => http.get<string>(`/config/key/${key}`)
export const createConfig = (data: ConfigSaveDto) => http.post('/config', data)
export const updateConfig = (id: number, data: ConfigSaveDto) => http.put(`/config/${id}`, data)
export const deleteConfig = (id: number) => http.delete(`/config/${id}`)
