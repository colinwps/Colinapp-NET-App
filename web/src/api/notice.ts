import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

export const enum NoticeType {
  Notification = 1,
  Announcement = 2,
}

export interface Notice {
  id: number
  title: string
  noticeType: NoticeType
  content: string
  published: boolean
  createdTime: string
}

export interface NoticeQuery extends PagedQuery {
  noticeType?: NoticeType
  published?: boolean
}

export interface NoticeSaveDto {
  title: string
  noticeType: NoticeType
  content: string
  published: boolean
}

export const getNotices = (params: NoticeQuery) =>
  http.get<PagedResult<Notice>>('/notice', params)
export const getNotice = (id: number) => http.get<Notice>(`/notice/${id}`)
export const createNotice = (data: NoticeSaveDto) => http.post('/notice', data)
export const updateNotice = (id: number, data: NoticeSaveDto) => http.put(`/notice/${id}`, data)
export const deleteNotice = (id: number) => http.delete(`/notice/${id}`)
