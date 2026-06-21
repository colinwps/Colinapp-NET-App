import type { App, Directive, DirectiveBinding } from 'vue'
import { useUserStore } from '@/stores/user'

// v-auth="'sys:user:add'" 或 v-auth="['sys:user:add','sys:user:edit']"
// 无权限时移除元素。管理员始终放行。
const auth: Directive = {
  mounted(el: HTMLElement, binding: DirectiveBinding<string | string[]>) {
    const userStore = useUserStore()
    const value = binding.value
    const perms = Array.isArray(value) ? value : [value]
    const ok = perms.some((p) => userStore.hasPermission(p))
    if (!ok) {
      el.parentNode?.removeChild(el)
    }
  },
}

export function setupAuthDirective(app: App) {
  app.directive('auth', auth)
}
