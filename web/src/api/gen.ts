import service, { http } from '@/utils/request'

export interface DbTable {
  tableName: string
  tableComment: string | null
  createTime: string | null
}

export interface GenColumnConfig {
  columnName: string
  fieldName: string
  label: string
  csharpType: string
  tsType: string
  nullable: boolean
  maxLength: number | null
  isPk: boolean
  isBase: boolean
  isList: boolean
  isQuery: boolean
  isForm: boolean
  isRequired: boolean
  htmlType: string
  queryType: string
}

export interface GenTableConfig {
  tableName: string
  className: string
  moduleName: string
  businessName: string
  functionName: string
  permissionPrefix: string
  columns: GenColumnConfig[]
}

export interface GeneratedFile {
  fileName: string
  targetPath: string
  language: string
  content: string
}

export const getTables = (keyword?: string) =>
  http.get<DbTable[]>('/gen/tables', { keyword })
export const getGenConfig = (tableName: string) =>
  http.get<GenTableConfig>('/gen/config', { tableName })
export const previewCode = (config: GenTableConfig) =>
  http.post<GeneratedFile[]>('/gen/preview', config)

/** 下载 zip：拦截器对非信封响应（Blob）原样返回 */
export const downloadZip = (config: GenTableConfig) =>
  service.post('/gen/download', config, { responseType: 'blob' }) as unknown as Promise<Blob>
