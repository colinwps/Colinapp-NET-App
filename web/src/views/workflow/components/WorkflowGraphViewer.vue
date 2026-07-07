<template>
  <div ref="canvasRef" class="wf-viewer" :style="{ height }" />
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import LogicFlow from '@logicflow/core'
import '@logicflow/core/lib/index.css'
import type { WorkflowGraph } from '@/api/workflow'
import { registerWfNodes } from '../designer/lfNodes'
import { toLogicFlow } from '../designer/lfAdapter'

const props = withDefaults(defineProps<{
  graph: WorkflowGraph | null
  /** 节点着色：nodeId → done(绿)/current(蓝)/rejected(红) */
  statuses?: Record<string, 'done' | 'current' | 'rejected'>
  height?: string
}>(), { statuses: () => ({}), height: '260px' })

const canvasRef = ref<HTMLDivElement>()
let lf: LogicFlow | null = null

function render() {
  if (!lf || !props.graph) return
  const data = toLogicFlow(props.graph)
  for (const node of data.nodes ?? []) {
    const status = props.statuses[node.id!]
    if (status) node.properties = { ...node.properties, status }
  }
  lf.render(data)
  lf.fitView(20, 20)
}

onMounted(() => {
  lf = new LogicFlow({
    container: canvasRef.value!,
    isSilentMode: true,
    grid: false,
    background: { backgroundColor: 'transparent' },
  })
  registerWfNodes(lf)
  render()
})

watch(() => [props.graph, props.statuses], render, { deep: true })

onBeforeUnmount(() => {
  lf?.destroy()
  lf = null
})
</script>

<style scoped>
.wf-viewer {
  width: 100%;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
}
</style>
