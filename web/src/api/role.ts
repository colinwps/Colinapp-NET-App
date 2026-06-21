import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

export const enum DataScope {
  All = 1,
  Custom = 2,
  Dept = 3,
  DeptAndChild = 4,
  Self = 5,
}

export interface RoleItem {
  id: number
  name: string
  code: string
  dataScope: DataScope
  orderNum: number
  enabled: boolean
  remark?: string
  createdTime: string
}

export interface RoleDetail extends RoleItem {
  menuIds: number[]
  deptIds: number[]
}

export interface RoleSaveDto {
  name: string
  code: string
  dataScope: DataScope
  orderNum: number
  enabled: boolean
  remark?: string
  menuIds: number[]
  deptIds: number[]
}

export const getRoles = (params: PagedQuery) => http.get<PagedResult<RoleItem>>('/role', params)
export const getRoleOptions = () => http.get<RoleItem[]>('/role/options')
export const getRole = (id: number) => http.get<RoleDetail>(`/role/${id}`)
export const createRole = (data: RoleSaveDto) => http.post('/role', data)
export const updateRole = (id: number, data: RoleSaveDto) => http.put(`/role/${id}`, data)
export const deleteRole = (id: number) => http.delete(`/role/${id}`)
