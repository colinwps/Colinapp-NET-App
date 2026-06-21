import type { RouteRecordRaw } from 'vue-router'
import { MenuType, type MenuNode } from '@/api/menu'

// 所有业务页面组件，按 component 字符串（相对 views，去扩展名）动态映射
const viewModules = import.meta.glob('/src/views/**/*.vue')

function loadView(component?: string) {
  if (!component) return () => import('@/views/placeholder/index.vue')
  const key = `/src/views/${component}.vue`
  return viewModules[key] ?? (() => import('@/views/placeholder/index.vue'))
}

function resolvePath(parentPath: string, path?: string): string {
  const p = path ?? ''
  if (p.startsWith('/')) return p
  return `${parentPath}/${p}`.replace(/\/+/g, '/')
}

/**
 * 将后端菜单树转换为 Vue Router 路由（扁平为 Layout 的子路由）。
 * 目录(Catalog)仅贡献路径前缀与侧边栏分组，菜单(Menu)生成真实路由，按钮(Button)忽略。
 */
export function generateRoutes(menuTree: MenuNode[], parentPath = ''): RouteRecordRaw[] {
  const routes: RouteRecordRaw[] = []

  for (const node of menuTree) {
    if (node.menuType === MenuType.Button) continue

    const fullPath = resolvePath(parentPath, node.path)

    if (node.menuType === MenuType.Catalog) {
      routes.push(...generateRoutes(node.children ?? [], fullPath))
    } else {
      if (node.isExternal) continue // 外链不生成路由
      routes.push({
        path: fullPath,
        name: `menu_${node.id}`,
        component: loadView(node.component),
        meta: {
          title: node.name,
          icon: node.icon,
          permission: node.permission,
          cache: node.cache,
        },
      })
    }
  }

  return routes
}
