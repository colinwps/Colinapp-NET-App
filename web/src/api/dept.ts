import { http } from '@/utils/request'

export interface DeptNode {
  id: number
  name: string
  parentId: number
  orderNum: number
  leaderUserId?: number
  phone?: string
  email?: string
  enabled: boolean
  createdTime: string
  children: DeptNode[]
}

export interface DeptSaveDto {
  name: string
  parentId: number
  orderNum: number
  leaderUserId?: number
  phone?: string
  email?: string
  enabled: boolean
}

export const getDeptTree = () => http.get<DeptNode[]>('/dept/tree')
export const getDept = (id: number) => http.get<DeptNode>(`/dept/${id}`)
export const createDept = (data: DeptSaveDto) => http.post('/dept', data)
export const updateDept = (id: number, data: DeptSaveDto) => http.put(`/dept/${id}`, data)
export const deleteDept = (id: number) => http.delete(`/dept/${id}`)
