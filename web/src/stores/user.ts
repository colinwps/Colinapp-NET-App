import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { getInfo, login, logout as logoutApi, type LoginParams, type UserInfo } from '@/api/auth'

const TOKEN_KEY = 'colinapp_token'
const REFRESH_KEY = 'colinapp_refresh_token'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>(localStorage.getItem(TOKEN_KEY) || '')
  const refreshToken = ref<string>(localStorage.getItem(REFRESH_KEY) || '')
  const userInfo = ref<UserInfo | null>(null)

  const permissions = computed(() => userInfo.value?.permissions ?? [])
  const isAdmin = computed(() => userInfo.value?.isAdmin ?? false)

  /** 写入令牌对并持久化 */
  function setTokens(access: string, refresh: string) {
    token.value = access
    refreshToken.value = refresh
    localStorage.setItem(TOKEN_KEY, access)
    localStorage.setItem(REFRESH_KEY, refresh)
  }

  async function loginAction(params: LoginParams) {
    const res = await login(params)
    setTokens(res.data.accessToken, res.data.refreshToken)
  }

  async function fetchInfo() {
    const res = await getInfo()
    userInfo.value = res.data
    return res.data
  }

  /** 登出：撤销服务端刷新令牌后清空本地状态（撤销失败不阻断登出） */
  async function logoutAction() {
    if (refreshToken.value) {
      try {
        await logoutApi(refreshToken.value)
      } catch {
        /* 忽略：本地仍需清空 */
      }
    }
    reset()
  }

  function reset() {
    token.value = ''
    refreshToken.value = ''
    userInfo.value = null
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_KEY)
  }

  /** 按钮级权限校验：管理员放行，否则匹配权限标识 */
  function hasPermission(permission: string) {
    if (isAdmin.value) return true
    return permissions.value.includes(permission)
  }

  return {
    token,
    refreshToken,
    userInfo,
    permissions,
    isAdmin,
    setTokens,
    loginAction,
    fetchInfo,
    logoutAction,
    reset,
    hasPermission,
  }
})
