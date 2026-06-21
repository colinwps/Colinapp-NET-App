<template>
  <!-- 有可显示子菜单 → 子菜单分组 -->
  <el-sub-menu v-if="renderableChildren.length > 0" :index="fullPath">
    <template #title>
      <el-icon v-if="item.icon"><component :is="item.icon" /></el-icon>
      <span>{{ item.name }}</span>
    </template>
    <SidebarItem
      v-for="child in renderableChildren"
      :key="child.id"
      :item="child"
      :base-path="fullPath"
    />
  </el-sub-menu>

  <!-- 叶子菜单项 -->
  <el-menu-item v-else :index="fullPath">
    <el-icon v-if="item.icon"><component :is="item.icon" /></el-icon>
    <template #title>{{ item.name }}</template>
  </el-menu-item>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { MenuType, type MenuNode } from '@/api/menu'

const props = defineProps<{ item: MenuNode; basePath: string }>()

function resolvePath(path?: string): string {
  const p = path ?? ''
  if (p.startsWith('/')) return p
  return `${props.basePath}/${p}`.replace(/\/+/g, '/')
}

const fullPath = computed(() => resolvePath(props.item.path))

// 仅渲染目录/菜单，忽略按钮
const renderableChildren = computed(
  () => props.item.children?.filter((c) => c.menuType !== MenuType.Button) ?? [],
)
</script>
