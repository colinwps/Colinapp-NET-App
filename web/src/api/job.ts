import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

export const enum JobStatus {
  Running = 0,
  Paused = 1,
}

export interface ScheduledJob {
  id: number
  name: string
  jobGroup: string
  invokeTarget: string
  cronExpression: string
  jobData?: string
  status: JobStatus
  concurrent: boolean
  remark?: string
  createdTime: string
}

export interface ScheduledJobSaveDto {
  name: string
  jobGroup: string
  invokeTarget: string
  cronExpression: string
  jobData?: string
  status: JobStatus
  concurrent: boolean
  remark?: string
}

export interface JobTarget {
  key: string
  name: string
  defaultCron: string
  defaultJobData?: string
  description: string
}

export interface ScheduledJobQuery extends PagedQuery {
  status?: JobStatus
}

export interface JobLog {
  id: number
  jobId: number
  jobName: string
  jobGroup: string
  invokeTarget: string
  success: boolean
  message?: string
  exception?: string
  startTime: string
  endTime: string
  elapsedMs: number
}

export interface JobLogQuery extends PagedQuery {
  success?: boolean
}

export const getJobs = (params: ScheduledJobQuery) => http.get<PagedResult<ScheduledJob>>('/job', params)
export const getJob = (id: number) => http.get<ScheduledJob>(`/job/${id}`)
export const getJobTargets = () => http.get<JobTarget[]>('/job/targets')
export const createJob = (data: ScheduledJobSaveDto) => http.post('/job', data)
export const updateJob = (id: number, data: ScheduledJobSaveDto) => http.put(`/job/${id}`, data)
export const deleteJob = (id: number) => http.delete(`/job/${id}`)
export const changeJobStatus = (id: number, status: JobStatus) =>
  http.put(`/job/${id}/status`, undefined, { params: { status } })
export const runJobOnce = (id: number) => http.post(`/job/${id}/run`)

export const getJobLogs = (params: JobLogQuery) => http.get<PagedResult<JobLog>>('/job/log', params)
export const clearJobLogs = () => http.delete('/job/log')
