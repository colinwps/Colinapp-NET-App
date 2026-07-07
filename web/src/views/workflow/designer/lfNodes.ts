import type LogicFlow from '@logicflow/core'
import {
  CircleNode, CircleNodeModel,
  RectNode, RectNodeModel,
  DiamondNode, DiamondNodeModel,
} from '@logicflow/core'
import { ApproveMode } from '@/api/workflow'

// 节点在画布上的展示属性（properties），设计器与只读视图共用
export interface WfNodeProperties {
  approveMode?: ApproveMode
  approverType?: number
  approverIds?: number[]
  rejectStrategy?: number
  timeoutHours?: number
  userIds?: number[]
  /** 校验失败标红 */
  error?: boolean
  /** 只读视图着色：done 已过 / current 当前 / rejected 驳回 */
  status?: 'done' | 'current' | 'rejected'
  [key: string]: unknown
}

const COLORS = {
  start: { fill: '#f0f9eb', stroke: '#67c23a' },
  end: { fill: '#f4f4f5', stroke: '#909399' },
  approvalAny: { fill: '#ecf5ff', stroke: '#409eff' },
  approvalAll: { fill: '#fdf6ec', stroke: '#e6a23c' },
  condition: { fill: '#fdf6ec', stroke: '#e6a23c' },
  cc: { fill: '#f5f0ff', stroke: '#9370db' },
  error: '#f56c6c',
  done: '#67c23a',
  current: '#409eff',
  rejected: '#f56c6c',
}

/** 按 error/status 覆盖描边，返回是否已覆盖 */
function applyStateStroke(style: Record<string, unknown>, props: WfNodeProperties) {
  if (props.error) {
    style.stroke = COLORS.error
    style.strokeWidth = 2
    return true
  }
  if (props.status) {
    style.stroke = COLORS[props.status]
    style.strokeWidth = 2
    if (props.status === 'done') style.fill = '#f0f9eb'
    if (props.status === 'current') style.fill = '#ecf5ff'
    if (props.status === 'rejected') style.fill = '#fef0f0'
    return true
  }
  return false
}

class WfStartModel extends CircleNodeModel {
  initNodeData(data: LogicFlow.NodeConfig) {
    super.initNodeData(data)
    this.r = 26
  }
  getNodeStyle() {
    const style = super.getNodeStyle()
    style.fill = COLORS.start.fill
    style.stroke = COLORS.start.stroke
    applyStateStroke(style as Record<string, unknown>, this.properties as WfNodeProperties)
    return style
  }
}

class WfEndModel extends CircleNodeModel {
  initNodeData(data: LogicFlow.NodeConfig) {
    super.initNodeData(data)
    this.r = 26
  }
  getNodeStyle() {
    const style = super.getNodeStyle()
    style.fill = COLORS.end.fill
    style.stroke = COLORS.end.stroke
    applyStateStroke(style as Record<string, unknown>, this.properties as WfNodeProperties)
    return style
  }
}

class WfApprovalModel extends RectNodeModel {
  initNodeData(data: LogicFlow.NodeConfig) {
    super.initNodeData(data)
    this.width = 150
    this.height = 52
    this.radius = 8
  }
  getNodeStyle() {
    const style = super.getNodeStyle()
    const props = this.properties as WfNodeProperties
    const theme = props.approveMode === ApproveMode.All ? COLORS.approvalAll : COLORS.approvalAny
    style.fill = theme.fill
    style.stroke = theme.stroke
    applyStateStroke(style as Record<string, unknown>, props)
    return style
  }
}

class WfConditionModel extends DiamondNodeModel {
  initNodeData(data: LogicFlow.NodeConfig) {
    super.initNodeData(data)
    this.rx = 70
    this.ry = 36
  }
  getNodeStyle() {
    const style = super.getNodeStyle()
    style.fill = COLORS.condition.fill
    style.stroke = COLORS.condition.stroke
    applyStateStroke(style as Record<string, unknown>, this.properties as WfNodeProperties)
    return style
  }
}

class WfCcModel extends RectNodeModel {
  initNodeData(data: LogicFlow.NodeConfig) {
    super.initNodeData(data)
    this.width = 140
    this.height = 46
    this.radius = 8
  }
  getNodeStyle() {
    const style = super.getNodeStyle()
    style.fill = COLORS.cc.fill
    style.stroke = COLORS.cc.stroke
    style.strokeDasharray = '4 4'
    applyStateStroke(style as Record<string, unknown>, this.properties as WfNodeProperties)
    return style
  }
}

/** 注册全部自定义节点（设计器与只读视图共用）。类型名 = 'wf-' + 后端节点 type。 */
export function registerWfNodes(lf: LogicFlow) {
  lf.register({ type: 'wf-start', view: CircleNode, model: WfStartModel })
  lf.register({ type: 'wf-end', view: CircleNode, model: WfEndModel })
  lf.register({ type: 'wf-approval', view: RectNode, model: WfApprovalModel })
  lf.register({ type: 'wf-condition', view: DiamondNode, model: WfConditionModel })
  lf.register({ type: 'wf-cc', view: RectNode, model: WfCcModel })
}
