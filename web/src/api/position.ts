import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

export interface PositionItem {
  id: number
  name: string
  code: string
  orderNum: number
  enabled: boolean
  remark?: string
  createdTime: string
}

export interface PositionSaveDto {
  name: string
  code: string
  orderNum: number
  enabled: boolean
  remark?: string
}

export const getPositions = (params: PagedQuery) =>
  http.get<PagedResult<PositionItem>>('/position', params)
export const getPositionOptions = () => http.get<PositionItem[]>('/position/options')
export const createPosition = (data: PositionSaveDto) => http.post('/position', data)
export const updatePosition = (id: number, data: PositionSaveDto) =>
  http.put(`/position/${id}`, data)
export const deletePosition = (id: number) => http.delete(`/position/${id}`)
