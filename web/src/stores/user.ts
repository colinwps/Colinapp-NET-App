import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { getInfo, login, type LoginParams, type UserInfo } from '@/api/auth'

const TOKEN_KEY = 'colinapp_token'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>(localStorage.getItem(TOKEN_KEY) || '')
  const userInfo = ref<UserInfo | null>(null)

  const permissions = computed(() => userInfo.value?.permissions ?? [])
  const isAdmin = computed(() => userInfo.value?.isAdmin ?? false)

  async function loginAction(params: LoginParams) {
    const res = await login(params)
    token.value = res.data.accessToken
    localStorage.setItem(TOKEN_KEY, token.value)
  }

  async function fetchInfo() {
    const res = await getInfo()
    userInfo.value = res.data
    return res.data
  }

  function reset() {
    token.value = ''
    userInfo.value = null
    localStorage.removeItem(TOKEN_KEY)
  }

  /** 按钮级权限校验：管理员放行，否则匹配权限标识 */
  function hasPermission(permission: string) {
    if (isAdmin.value) return true
    return permissions.value.includes(permission)
  }

  return { token, userInfo, permissions, isAdmin, loginAction, fetchInfo, reset, hasPermission }
})
