import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'
import type { InstanceStatus } from './workflow'

// ---- 类型（与后端 Application.Forms 对应） ----

export type FormFieldType =
  | 'text' | 'textarea' | 'number' | 'date' | 'datetime'
  | 'select' | 'radio' | 'checkbox' | 'switch' | 'divider'

/**
 * 表单字段：WorkflowFormField 的超集（前 5 个属性同名同义），
 * WorkflowFormField[] 可直接赋给 FormField[]，共享 DynamicForm 组件一套通吃。
 */
export interface FormField {
  key: string
  label: string
  type: FormFieldType
  required: boolean
  options: string[]
  placeholder?: string
  /** 字段下方说明文字 */
  help?: string
  defaultValue?: unknown
  /** el-col 栅格宽度（1~24，默认 24） */
  span?: number
}

export const enum FormStatus {
  Draft = 1,
  Published = 2,
  Disabled = 3,
}

export interface FormDefinition {
  id: number
  code: string
  name: string
  description?: string
  icon?: string
  status: FormStatus
  workflowDefinitionId?: number
  workflowName?: string
  fields: FormField[]
  createdTime: string
}

export interface FormDefinitionQuery extends PagedQuery {
  status?: FormStatus
}

export interface FormDefinitionSaveDto {
  code: string
  name: string
  description?: string
  icon?: string
  workflowDefinitionId?: number | null
  fields: FormField[]
}

export interface PublishedForm {
  id: number
  name: string
  description?: string
  icon?: string
  workflowDefinitionId?: number
  workflowName?: string
  fields: FormField[]
}

export interface FormSubmitDto {
  title?: string
  /** 表单数据：JSON 对象字符串 */
  dataJson: string
}

export interface FormSubmitResult {
  entryId: number
  /** 表单绑定流程时为发起的实例 Id */
  instanceId?: number
}

export interface FormEntry {
  id: number
  formDefinitionId: number
  formName: string
  title: string
  submitterName: string
  workflowInstanceId?: number
  instanceStatus?: InstanceStatus
  createdTime: string
}

export interface FormEntryQuery extends PagedQuery {
  formDefinitionId?: number
}

export interface FormEntryDetail extends FormEntry {
  dataJson: string
  fields: FormField[]
}

// ---- 接口 ----

export const getFormDefinitions = (params: FormDefinitionQuery) =>
  http.get<PagedResult<FormDefinition>>('/form/definition', params)
export const getFormDefinition = (id: number) =>
  http.get<FormDefinition>(`/form/definition/${id}`)
export const createFormDefinition = (data: FormDefinitionSaveDto) =>
  http.post<number>('/form/definition', data)
export const updateFormDefinition = (id: number, data: FormDefinitionSaveDto) =>
  http.put(`/form/definition/${id}`, data)
export const setFormStatus = (id: number, status: FormStatus) =>
  http.put(`/form/definition/${id}/status`, { status })
export const deleteFormDefinition = (id: number) =>
  http.delete(`/form/definition/${id}`)

export const getPublishedForms = () =>
  http.get<PublishedForm[]>('/form/published')
export const submitForm = (id: number, data: FormSubmitDto) =>
  http.post<FormSubmitResult>(`/form/${id}/submit`, data)

export const getMyFormEntries = (params: FormEntryQuery) =>
  http.get<PagedResult<FormEntry>>('/form/entry/my', params)
export const getFormEntries = (params: FormEntryQuery) =>
  http.get<PagedResult<FormEntry>>('/form/entry', params)
export const getFormEntry = (id: number) =>
  http.get<FormEntryDetail>(`/form/entry/${id}`)

// ---- 展示辅助 ----

export const formStatusMap: Record<number, { label: string; type: 'info' | 'success' | 'warning' }> = {
  [FormStatus.Draft]: { label: '草稿', type: 'info' },
  [FormStatus.Published]: { label: '已发布', type: 'success' },
  [FormStatus.Disabled]: { label: '已停用', type: 'warning' },
}

/** 设计器字段面板（顺序即面板展示顺序） */
export const fieldTypeMap: Record<FormFieldType, string> = {
  text: '单行文本',
  textarea: '多行文本',
  number: '数字',
  date: '日期',
  datetime: '日期时间',
  select: '下拉选择',
  radio: '单选',
  checkbox: '多选',
  switch: '开关',
  divider: '分隔说明',
}

/** 需要配置选项的类型 */
export const optionTypes: FormFieldType[] = ['select', 'radio', 'checkbox']

/** 是否为录入字段（divider 纯展示，不产生数据） */
export const isInputField = (f: FormField) => f.type !== 'divider'

/**
 * 可作为流程条件分支引用的字段。
 * checkbox 为数组值，引擎只有 contains 语义可用；divider 无数据——两者都排除，
 * 由绑定校验（后端 WorkflowGraph.Validate）兜底。
 */
export const conditionEligible = (f: FormField) =>
  isInputField(f) && f.type !== 'checkbox'
