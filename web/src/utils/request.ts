import axios, { type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios'
import { ElMessage } from 'element-plus'
import type { ApiResult } from '@/api/types'
import { useUserStore } from '@/stores/user'
import router from '@/router'

const service = axios.create({
  baseURL: '/api',
  timeout: 15000,
})

// 请求拦截：注入 Bearer Token
service.interceptors.request.use((config) => {
  const userStore = useUserStore()
  if (userStore.token) {
    config.headers.Authorization = `Bearer ${userStore.token}`
  }
  return config
})

// ---- 401 静默刷新：并发请求共享同一次刷新，完成后重放原请求 ----
let isRefreshing = false
let pendingQueue: Array<(token: string | null) => void> = []

function flushQueue(token: string | null) {
  pendingQueue.forEach((cb) => cb(token))
  pendingQueue = []
}

function redirectToLogin() {
  const userStore = useUserStore()
  userStore.reset()
  if (router.currentRoute.value.path !== '/login') {
    router.replace(`/login?redirect=${router.currentRoute.value.fullPath}`)
  }
}

// 用裸 axios 调用刷新接口，绕过本实例拦截器，避免递归
async function doRefresh(): Promise<string | null> {
  const userStore = useUserStore()
  if (!userStore.refreshToken) return null
  try {
    const { data } = await axios.post<ApiResult<{ accessToken: string; refreshToken: string }>>(
      '/api/auth/refresh',
      { refreshToken: userStore.refreshToken },
    )
    if (data.code === 200 && data.data) {
      userStore.setTokens(data.data.accessToken, data.data.refreshToken)
      return data.data.accessToken
    }
    return null
  } catch {
    return null
  }
}

// 响应拦截：解析统一信封 { code, message, data }
service.interceptors.response.use(
  // 拦截器将响应解包为 ApiResult（非标准 AxiosResponse），返回类型放宽为 any
  async (response): Promise<any> => {
    const res = response.data as ApiResult
    // 非业务信封（如文件流）原样返回
    if (res == null || typeof res.code === 'undefined') {
      return res
    }
    if (res.code === 200) {
      return res
    }
    // 未登录 / 访问令牌过期 → 尝试静默刷新并重放
    if (res.code === 401) {
      const config = response.config as InternalAxiosRequestConfig & { _retried?: boolean }
      const userStore = useUserStore()

      // 刷新接口自身失败、无刷新令牌或已重试过 → 直接登出
      if (config.url?.includes('/auth/refresh') || !userStore.refreshToken || config._retried) {
        redirectToLogin()
        ElMessage.error(res.message || '登录已过期，请重新登录')
        return Promise.reject(new Error(res.message || 'Unauthorized'))
      }

      // 已有刷新在途 → 排队等待，拿到新令牌后重放
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          pendingQueue.push((token) => {
            if (token) {
              config._retried = true
              config.headers.Authorization = `Bearer ${token}`
              resolve(service(config))
            } else {
              reject(new Error('登录已过期'))
            }
          })
        })
      }

      isRefreshing = true
      const newToken = await doRefresh()
      isRefreshing = false
      flushQueue(newToken)

      if (newToken) {
        config._retried = true
        config.headers.Authorization = `Bearer ${newToken}`
        return service(config)
      }

      redirectToLogin()
      ElMessage.error('登录已过期，请重新登录')
      return Promise.reject(new Error('Unauthorized'))
    }
    ElMessage.error(res.message || '请求失败')
    return Promise.reject(new Error(res.message || 'Error'))
  },
  (error) => {
    ElMessage.error(error.message || '网络异常')
    return Promise.reject(error)
  },
)

// 类型化的便捷方法：解析后的值即为 ApiResult<T>
export const http = {
  get<T = any>(url: string, params?: object, config?: AxiosRequestConfig) {
    return service.get<any, ApiResult<T>>(url, { params, ...config })
  },
  post<T = any>(url: string, data?: object, config?: AxiosRequestConfig) {
    return service.post<any, ApiResult<T>>(url, data, config)
  },
  put<T = any>(url: string, data?: object, config?: AxiosRequestConfig) {
    return service.put<any, ApiResult<T>>(url, data, config)
  },
  delete<T = any>(url: string, params?: object, config?: AxiosRequestConfig) {
    return service.delete<any, ApiResult<T>>(url, { params, ...config })
  },
}

export default service
