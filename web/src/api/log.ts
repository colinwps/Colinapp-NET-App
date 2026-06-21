import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

export interface OperationLog {
  id: number
  title: string
  operatorName?: string
  method?: string
  requestMethod: string
  requestUrl: string
  requestParams?: string
  ip?: string
  success: boolean
  errorMessage?: string
  duration: number
  createdTime: string
}

export interface LoginLog {
  id: number
  userName: string
  success: boolean
  message?: string
  ip?: string
  userAgent?: string
  createdTime: string
}

export interface LogQuery extends PagedQuery {
  success?: boolean
}

export const getOperationLogs = (params: LogQuery) =>
  http.get<PagedResult<OperationLog>>('/log/operation', params)
export const clearOperationLogs = () => http.delete('/log/operation')
export const getLoginLogs = (params: LogQuery) =>
  http.get<PagedResult<LoginLog>>('/log/login', params)
export const clearLoginLogs = () => http.delete('/log/login')
