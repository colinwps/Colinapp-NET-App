import { http } from '@/utils/request'
import type { PagedQuery, PagedResult } from './types'

// ---- 枚举（与后端 Domain.Enums 对应） ----

export const enum ApproveMode {
  /** 或签：任一人通过即可 */
  Any = 1,
  /** 会签：所有人通过 */
  All = 2,
}

export const enum ApproverType {
  Users = 1,
  Role = 2,
}

export const enum InstanceStatus {
  Running = 1,
  Approved = 2,
  Rejected = 3,
  Canceled = 4,
  /** 已退回（可修改后重新提交） */
  Returned = 5,
}

export const enum RejectStrategy {
  /** 整体驳回 */
  RejectInstance = 1,
  /** 退回上一审批节点 */
  BackToPrevious = 2,
  /** 退回发起人重新提交 */
  BackToInitiator = 3,
}

export const enum WfTaskStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3,
  Terminated = 4,
}

// ---- 流程图类型（对应后端 WorkflowGraph，Schema 见 docs/工作流设计器规划.md） ----

export type WfNodeType = 'start' | 'approval' | 'condition' | 'cc' | 'end'

export interface WfNodeProps {
  approveMode: ApproveMode
  approverType: ApproverType
  approverIds: number[]
  /** 驳回策略（approval 节点） */
  rejectStrategy?: RejectStrategy
  /** 审批超时提醒小时数，0 = 不限时（approval 节点） */
  timeoutHours?: number
  /** 抄送人（cc 节点） */
  userIds: number[]
}

export interface WfNode {
  id: string
  type: WfNodeType
  name: string
  x: number
  y: number
  props: WfNodeProps
}

export interface WfRule {
  field: string
  op: 'eq' | 'ne' | 'gt' | 'ge' | 'lt' | 'le' | 'in' | 'contains'
  value: unknown
}

export interface WfCondition {
  logic: 'and' | 'or'
  rules: WfRule[]
}

export interface WfEdge {
  id: string
  from: string
  to: string
  name?: string | null
  priority: number
  isDefault: boolean
  condition?: WfCondition | null
}

export interface WorkflowGraph {
  version: number
  nodes: WfNode[]
  edges: WfEdge[]
}

export interface WorkflowFormField {
  key: string
  label: string
  type: 'text' | 'textarea' | 'number' | 'date' | 'select'
  required: boolean
  options: string[]
}

// ---- 业务类型 ----

export interface WorkflowDefinition {
  id: number
  code: string
  name: string
  enabled: boolean
  remark?: string
  graph: WorkflowGraph
  formFields: WorkflowFormField[]
  createdTime: string
}

export interface WorkflowDefinitionQuery extends PagedQuery {
  enabled?: boolean
}

export interface WorkflowDefinitionSaveDto {
  code: string
  name: string
  enabled: boolean
  remark?: string
  graph: WorkflowGraph
  formFields: WorkflowFormField[]
}

export interface WorkflowSubmitDto {
  definitionId: number
  title: string
  /** 表单数据：JSON 对象字符串（key 对应表单字段） */
  formData: string
}

export interface WorkflowInstance {
  id: number
  definitionName: string
  title: string
  status: InstanceStatus
  currentNodeId: string
  currentNodeName: string
  initiatorName: string
  createdTime: string
  finishTime?: string
}

export interface WorkflowInstanceQuery extends PagedQuery {
  status?: InstanceStatus
}

export interface WorkflowTaskRecord {
  nodeId: string
  nodeName: string
  approverName: string
  status: WfTaskStatus
  comment?: string
  handleTime?: string
}

export interface WorkflowInstanceDetail extends WorkflowInstance {
  formData: string
  graph: WorkflowGraph
  formFields: WorkflowFormField[]
  tasks: WorkflowTaskRecord[]
}

export interface WorkflowTaskItem {
  id: number
  instanceId: number
  instanceTitle: string
  definitionName: string
  nodeName: string
  approveMode: ApproveMode
  initiatorName: string
  status: WfTaskStatus
  comment?: string
  createdTime: string
  handleTime?: string
  dueTime?: string
}

export interface WorkflowCcItem {
  id: number
  instanceId: number
  instanceTitle: string
  definitionName: string
  nodeName: string
  initiatorName: string
  instanceStatus: InstanceStatus
  createdTime: string
  readTime?: string
}

export interface WorkflowCcQuery extends PagedQuery {
  unreadOnly?: boolean
}

export interface WorkflowResubmitDto {
  title?: string
  formData: string
}

// ---- 接口 ----

export const getDefinitions = (params: WorkflowDefinitionQuery) =>
  http.get<PagedResult<WorkflowDefinition>>('/workflow/definition', params)
export const getDefinitionOptions = () =>
  http.get<WorkflowDefinition[]>('/workflow/definition/options')
export const getDefinition = (id: number) =>
  http.get<WorkflowDefinition>(`/workflow/definition/${id}`)
export const createDefinition = (data: WorkflowDefinitionSaveDto) =>
  http.post('/workflow/definition', data)
export const updateDefinition = (id: number, data: WorkflowDefinitionSaveDto) =>
  http.put(`/workflow/definition/${id}`, data)
export const deleteDefinition = (id: number) =>
  http.delete(`/workflow/definition/${id}`)

export const submitInstance = (data: WorkflowSubmitDto) =>
  http.post<number>('/workflow/instance', data)
export const getMyInstances = (params: WorkflowInstanceQuery) =>
  http.get<PagedResult<WorkflowInstance>>('/workflow/instance/my', params)
export const getInstance = (id: number) =>
  http.get<WorkflowInstanceDetail>(`/workflow/instance/${id}`)
export const cancelInstance = (id: number) =>
  http.put(`/workflow/instance/${id}/cancel`)
export const resubmitInstance = (id: number, data: WorkflowResubmitDto) =>
  http.put(`/workflow/instance/${id}/resubmit`, data)

export const getMyCc = (params: WorkflowCcQuery) =>
  http.get<PagedResult<WorkflowCcItem>>('/workflow/cc/my', params)
export const markCcRead = (id: number) =>
  http.put(`/workflow/cc/${id}/read`)

export const getTodoTasks = (params: PagedQuery) =>
  http.get<PagedResult<WorkflowTaskItem>>('/workflow/task/todo', params)
export const getDoneTasks = (params: PagedQuery) =>
  http.get<PagedResult<WorkflowTaskItem>>('/workflow/task/done', params)
export const approveTask = (id: number, approved: boolean, comment?: string) =>
  http.put(`/workflow/task/${id}/approve`, { approved, comment })

// ---- 图辅助（设计器上线前的桥接工具） ----

const defaultProps = (): WfNodeProps => ({
  approveMode: ApproveMode.Any,
  approverType: ApproverType.Users,
  approverIds: [],
  userIds: [],
})

/**
 * 沿边从开始节点走一遍，抽出审批节点链（条件节点走默认边）。
 * 用于列表/详情的节点预览；设计器上线后详情将改为渲染完整图。
 */
export function getApprovalChain(graph: WorkflowGraph): WfNode[] {
  const chain: WfNode[] = []
  const nodeMap = new Map(graph.nodes.map(n => [n.id, n]))
  let current = graph.nodes.find(n => n.type === 'start')
  for (let step = 0; current && step <= graph.nodes.length; step++) {
    const outs = graph.edges.filter(e => e.from === current!.id)
    const edge = current.type === 'condition' ? (outs.find(e => e.isDefault) ?? outs[0]) : outs[0]
    if (!edge) break
    current = nodeMap.get(edge.to)
    if (!current || current.type === 'end') break
    if (current.type === 'approval') chain.push(current)
  }
  return chain
}

/** 编辑器里的线性节点（旧列表编辑器的数据形态） */
export interface LinearNode {
  name: string
  approveMode: ApproveMode
  approverType: ApproverType
  approverIds: number[]
}

/** 线性节点列表 → start → n0..nK → end 的图（与后端旧数据转换器同构） */
export function buildLinearGraph(nodes: LinearNode[]): WorkflowGraph {
  const gNodes: WfNode[] = [
    { id: 'start', type: 'start', name: '开始', x: 80, y: 200, props: defaultProps() },
    ...nodes.map((n, i) => ({
      id: `n${i}`,
      type: 'approval' as const,
      name: n.name,
      x: 240 + i * 180,
      y: 200,
      props: { ...defaultProps(), approveMode: n.approveMode, approverType: n.approverType, approverIds: n.approverIds },
    })),
    { id: 'end', type: 'end', name: '结束', x: 240 + nodes.length * 180, y: 200, props: defaultProps() },
  ]
  const edges: WfEdge[] = []
  for (let i = 0; i < gNodes.length - 1; i++)
    edges.push({ id: `e${i}`, from: gNodes[i].id, to: gNodes[i + 1].id, priority: 0, isDefault: false })
  return { version: 1, nodes: gNodes, edges }
}

/** 图 → 线性节点列表；含条件/抄送节点（非纯线性）时返回 null，需用设计器编辑 */
export function decomposeLinearGraph(graph: WorkflowGraph): LinearNode[] | null {
  if (graph.nodes.some(n => n.type === 'condition' || n.type === 'cc')) return null
  return getApprovalChain(graph).map(n => ({
    name: n.name,
    approveMode: n.props.approveMode,
    approverType: n.props.approverType,
    approverIds: [...n.props.approverIds],
  }))
}

// ---- 展示辅助 ----

export const instanceStatusMap: Record<number, { label: string; type: 'primary' | 'success' | 'danger' | 'info' | 'warning' }> = {
  [InstanceStatus.Running]: { label: '审批中', type: 'primary' },
  [InstanceStatus.Approved]: { label: '已通过', type: 'success' },
  [InstanceStatus.Rejected]: { label: '已驳回', type: 'danger' },
  [InstanceStatus.Canceled]: { label: '已撤销', type: 'info' },
  [InstanceStatus.Returned]: { label: '已退回', type: 'warning' },
}

export const taskStatusMap: Record<number, { label: string; type: 'primary' | 'success' | 'danger' | 'info' }> = {
  [WfTaskStatus.Pending]: { label: '待审批', type: 'primary' },
  [WfTaskStatus.Approved]: { label: '已通过', type: 'success' },
  [WfTaskStatus.Rejected]: { label: '已驳回', type: 'danger' },
  [WfTaskStatus.Terminated]: { label: '已终止', type: 'info' },
}
