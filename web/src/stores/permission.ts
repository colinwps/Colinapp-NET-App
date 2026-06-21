import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { RouteRecordRaw } from 'vue-router'
import { getUserRoutes, type MenuNode } from '@/api/menu'
import { generateRoutes } from '@/router/dynamic'

export const usePermissionStore = defineStore('permission', () => {
  // 侧边栏用的原始菜单树
  const menuTree = ref<MenuNode[]>([])
  const dynamicRoutes = ref<RouteRecordRaw[]>([])

  async function buildRoutes() {
    const res = await getUserRoutes()
    menuTree.value = res.data
    dynamicRoutes.value = generateRoutes(res.data)
    return dynamicRoutes.value
  }

  function reset() {
    menuTree.value = []
    dynamicRoutes.value = []
  }

  return { menuTree, dynamicRoutes, buildRoutes, reset }
})
