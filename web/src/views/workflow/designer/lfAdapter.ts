import type LogicFlow from '@logicflow/core'
import {
  ApproveMode, ApproverType, RejectStrategy,
  type WorkflowGraph, type WfNode, type WfEdge, type WfNodeType, type WorkflowFormField,
} from '@/api/workflow'
import type { WfNodeProperties } from './lfNodes'

// WorkflowGraph（后端 Schema）⇄ LogicFlow GraphConfigData 的双向转换。
// 约定：LF 节点 type = 'wf-' + 后端 type；边的业务属性放 properties。

const LF_PREFIX = 'wf-'

export const nodeDefaults: Record<WfNodeType, { name: string; props: WfNodeProperties }> = {
  start: { name: '开始', props: {} },
  end: { name: '结束', props: {} },
  approval: {
    name: '审批',
    props: {
      approveMode: ApproveMode.Any, approverType: ApproverType.Users, approverIds: [],
      rejectStrategy: RejectStrategy.RejectInstance, timeoutHours: 0,
    },
  },
  condition: { name: '条件判断', props: {} },
  cc: { name: '抄送', props: { userIds: [] } },
}

/** 会签在面板里是独立入口，但落图仍是 approval 类型 + All 预设 */
export const paletteItems: { type: WfNodeType; label: string; preset?: Partial<WfNodeProperties> }[] = [
  { type: 'approval', label: '审批（或签）' },
  { type: 'approval', label: '会签', preset: { approveMode: ApproveMode.All } },
  { type: 'condition', label: '条件判断' },
  { type: 'cc', label: '抄送' },
  { type: 'start', label: '开始' },
  { type: 'end', label: '结束' },
]

export function toLfType(t: WfNodeType): string {
  return LF_PREFIX + t
}
export function fromLfType(t: string): WfNodeType {
  return t.replace(LF_PREFIX, '') as WfNodeType
}

/** 边在画布上的标签：优先 name，条件边兜底「条件」，默认边兜底「默认」 */
export function edgeLabel(e: Pick<WfEdge, 'name' | 'isDefault' | 'condition'>): string {
  if (e.name) return e.name
  if (e.isDefault) return '默认'
  if (e.condition && e.condition.rules.length > 0) return '条件'
  return ''
}

export function toLogicFlow(graph: WorkflowGraph): LogicFlow.GraphConfigData {
  return {
    nodes: graph.nodes.map((n) => ({
      id: n.id,
      type: toLfType(n.type),
      x: n.x,
      y: n.y,
      text: n.name,
      properties: { ...n.props } as Record<string, unknown>,
    })),
    edges: graph.edges.map((e) => ({
      id: e.id,
      type: 'polyline',
      sourceNodeId: e.from,
      targetNodeId: e.to,
      text: edgeLabel(e),
      properties: {
        name: e.name ?? '',
        priority: e.priority ?? 0,
        isDefault: e.isDefault ?? false,
        condition: e.condition ?? null,
      },
    })),
  }
}

function textValue(text: unknown): string {
  if (typeof text === 'string') return text
  if (text && typeof text === 'object' && 'value' in text) return String((text as { value: unknown }).value ?? '')
  return ''
}

export function fromLogicFlow(data: LogicFlow.GraphData): WorkflowGraph {
  const nodes: WfNode[] = data.nodes.map((n) => {
    const p = (n.properties ?? {}) as WfNodeProperties
    return {
      id: n.id,
      type: fromLfType(n.type as string),
      name: textValue(n.text),
      x: n.x,
      y: n.y,
      props: {
        approveMode: p.approveMode ?? ApproveMode.Any,
        approverType: (p.approverType as ApproverType) ?? ApproverType.Users,
        approverIds: p.approverIds ?? [],
        rejectStrategy: p.rejectStrategy ?? RejectStrategy.RejectInstance,
        timeoutHours: p.timeoutHours ?? 0,
        userIds: p.userIds ?? [],
      },
    }
  })
  const edges: WfEdge[] = data.edges.map((e) => {
    const p = (e.properties ?? {}) as Record<string, unknown>
    return {
      id: e.id,
      from: e.sourceNodeId,
      to: e.targetNodeId,
      name: (p.name as string) || null,
      priority: (p.priority as number) ?? 0,
      isDefault: (p.isDefault as boolean) ?? false,
      condition: (p.condition as WfEdge['condition']) ?? null,
    }
  })
  return { version: 1, nodes, edges }
}

// ---- 客户端校验（与后端 WorkflowGraph.Validate 同规则，保存前先标红） ----

export interface GraphIssues {
  errors: string[]
  /** 有问题的节点 id，用于画布标红 */
  nodeIds: string[]
}

export function validateGraph(graph: WorkflowGraph, formFields: WorkflowFormField[]): GraphIssues {
  const errors: string[] = []
  const badNodes = new Set<string>()
  const flag = (id: string, msg: string) => {
    errors.push(msg)
    badNodes.add(id)
  }

  const starts = graph.nodes.filter(n => n.type === 'start')
  if (starts.length !== 1) errors.push('流程必须有且只有一个开始节点')
  if (!graph.nodes.some(n => n.type === 'end')) errors.push('流程至少需要一个结束节点')
  if (!graph.nodes.some(n => n.type === 'approval')) errors.push('流程至少需要一个审批节点')

  const nodeIds = new Set(graph.nodes.map(n => n.id))
  for (const e of graph.edges) {
    if (!nodeIds.has(e.from) || !nodeIds.has(e.to)) errors.push('存在连接到已删除节点的连线')
  }

  const fieldKeys = new Set(formFields.map(f => f.key))
  for (const node of graph.nodes) {
    const label = node.name || node.id
    const outs = graph.edges.filter(e => e.from === node.id)
    const ins = graph.edges.filter(e => e.to === node.id).length

    switch (node.type) {
      case 'start':
        if (ins > 0) flag(node.id, '开始节点不能有入线')
        if (outs.length !== 1) flag(node.id, '开始节点必须有且只有一条出线')
        break
      case 'end':
        if (outs.length > 0) flag(node.id, '结束节点不能有出线')
        if (ins === 0) flag(node.id, `结束节点「${label}」没有入线`)
        break
      case 'condition':
        if (ins === 0) flag(node.id, `条件节点「${label}」没有入线`)
        if (outs.length < 2) flag(node.id, `条件节点「${label}」至少需要两条出线`)
        if (outs.filter(e => e.isDefault).length !== 1)
          flag(node.id, `条件节点「${label}」必须有且只有一条默认出线`)
        for (const e of outs.filter(e => !e.isDefault)) {
          if (!e.condition || e.condition.rules.length === 0)
            flag(node.id, `条件节点「${label}」的非默认出线必须配置条件`)
          else {
            for (const r of e.condition.rules) {
              if (!fieldKeys.has(r.field))
                flag(node.id, `条件节点「${label}」引用了不存在的表单字段「${r.field}」`)
            }
          }
        }
        break
      case 'approval':
        if (ins === 0) flag(node.id, `审批节点「${label}」没有入线`)
        if (outs.length !== 1) flag(node.id, `审批节点「${label}」必须有且只有一条出线`)
        if (!node.props.approverIds.length) flag(node.id, `审批节点「${label}」未指定审批人`)
        break
      case 'cc':
        if (ins === 0) flag(node.id, `抄送节点「${label}」没有入线`)
        if (outs.length !== 1) flag(node.id, `抄送节点「${label}」必须有且只有一条出线`)
        if (!node.props.userIds.length) flag(node.id, `抄送节点「${label}」未指定抄送人`)
        break
    }
  }

  // 可达性 + 环（三色 DFS），仅在结构大体成立时检查
  if (starts.length === 1 && errors.length === 0) {
    const color = new Map(graph.nodes.map(n => [n.id, 0]))
    let hasCycle = false
    const dfs = (id: string) => {
      color.set(id, 1)
      for (const e of graph.edges.filter(x => x.from === id)) {
        const c = color.get(e.to)
        if (c === 1) hasCycle = true
        else if (c === 0) dfs(e.to)
      }
      color.set(id, 2)
    }
    dfs(starts[0].id)
    if (hasCycle) errors.push('流程图不允许存在环路')
    const unreachable = graph.nodes.filter(n => color.get(n.id) === 0)
    for (const n of unreachable) flag(n.id, `节点「${n.name || n.id}」从开始节点不可达`)
  }

  return { errors, nodeIds: [...badNodes] }
}
