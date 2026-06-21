import { http } from '@/utils/request'

export interface LoginParams {
  userName: string
  password: string
}

export interface LoginResult {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: {
    id: number
    userName: string
    nickName: string
    isAdmin: boolean
  }
}

export interface UserInfo {
  userId: number
  userName: string
  isAdmin: boolean
  tenantId: number | null
  roleIds: number[]
  permissions: string[]
}

export const login = (data: LoginParams) => http.post<LoginResult>('/auth/login', data)
export const getInfo = () => http.get<UserInfo>('/auth/me')
