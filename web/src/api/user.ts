import service, { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

export interface UserListItem {
  id: number
  userName: string
  nickName: string
  phone?: string
  email?: string
  deptId?: number
  deptName?: string
  enabled: boolean
  isAdmin: boolean
  lastLoginTime?: string
  createdTime: string
}

export interface UserDetail extends UserListItem {
  roleIds: number[]
  postIds: number[]
}

export interface UserQuery extends PagedQuery {
  deptId?: number
  enabled?: boolean
}

export interface UserSaveDto {
  userName: string
  nickName: string
  password?: string
  phone?: string
  email?: string
  deptId?: number
  enabled: boolean
  roleIds: number[]
  postIds: number[]
}

export const getUsers = (params: UserQuery) =>
  http.get<PagedResult<UserListItem>>('/user', params)
export const getUser = (id: number) => http.get<UserDetail>(`/user/${id}`)
export const createUser = (data: UserSaveDto) => http.post('/user', data)
export const updateUser = (id: number, data: UserSaveDto) => http.put(`/user/${id}`, data)
export const deleteUser = (id: number) => http.delete(`/user/${id}`)
export const resetPassword = (id: number, password: string) =>
  http.put(`/user/${id}/password`, { password })
export const changeStatus = (id: number, enabled: boolean) =>
  http.put(`/user/${id}/status`, { enabled })

export interface UserImportResult {
  total: number
  success: number
  failed: number
  errors: string[]
}

/** 导出（拦截器对 Blob 原样返回） */
export const exportUsers = (params: UserQuery) =>
  service.get('/user/export', { params, responseType: 'blob' }) as unknown as Promise<Blob>
export const downloadUserTemplate = () =>
  service.get('/user/import-template', { responseType: 'blob' }) as unknown as Promise<Blob>
export const importUsers = (file: File, updateExisting: boolean) => {
  const fd = new FormData()
  fd.append('file', file)
  fd.append('updateExisting', String(updateExisting))
  return http.post<UserImportResult>('/user/import', fd)
}
