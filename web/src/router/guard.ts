import type { Router } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { usePermissionStore } from '@/stores/permission'

const WHITE_LIST = ['/login']

export function setupGuards(router: Router) {
  router.beforeEach(async (to) => {
    const userStore = useUserStore()

    // 已登录访问登录页 → 回首页
    if (to.path === '/login') {
      return userStore.token ? { path: '/' } : true
    }

    // 未登录 → 去登录
    if (!userStore.token) {
      if (WHITE_LIST.includes(to.path)) return true
      return { path: '/login', query: { redirect: to.fullPath } }
    }

    // 已登录但未加载用户信息/动态路由 → 加载后重新导航
    if (!userStore.userInfo) {
      try {
        await userStore.fetchInfo()
        const permStore = usePermissionStore()
        const routes = await permStore.buildRoutes()
        routes.forEach((r) => router.addRoute('Layout', r))
        // 兜底 404 放在动态路由之后，固定 name 以便覆盖而非重复
        router.addRoute({ path: '/:pathMatch(.*)*', name: 'NotFoundCatch', redirect: '/404' })
        return { ...to, replace: true }
      } catch {
        userStore.reset()
        ElMessage.error('登录状态失效，请重新登录')
        return { path: '/login' }
      }
    }

    return true
  })
}
