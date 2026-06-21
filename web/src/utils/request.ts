import axios, { type AxiosRequestConfig } from 'axios'
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

// 响应拦截：解析统一信封 { code, message, data }
service.interceptors.response.use(
  // 拦截器将响应解包为 ApiResult（非标准 AxiosResponse），返回类型放宽为 any
  (response): any => {
    const res = response.data as ApiResult
    // 非业务信封（如文件流）原样返回
    if (res == null || typeof res.code === 'undefined') {
      return res
    }
    if (res.code === 200) {
      return res
    }
    // 未登录 / 登录过期
    if (res.code === 401) {
      const userStore = useUserStore()
      userStore.reset()
      if (router.currentRoute.value.path !== '/login') {
        router.replace(`/login?redirect=${router.currentRoute.value.fullPath}`)
      }
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
