// 通用响应/分页类型，对应后端 ApiResult / PagedResult / PagedRequest

export interface ApiResult<T = any> {
  code: number
  message: string
  data: T
  success: boolean
}

export interface PagedResult<T> {
  items: T[]
  total: number
  pageIndex: number
  pageSize: number
}

export interface PagedQuery {
  pageIndex?: number
  pageSize?: number
  keyword?: string
}
