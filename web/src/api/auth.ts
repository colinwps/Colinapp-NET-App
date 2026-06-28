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

export interface Profile {
  id: number
  userName: string
  nickName: string
  phone: string | null
  email: string | null
  isAdmin: boolean
}

export interface UpdateProfileParams {
  nickName: string
  phone?: string | null
  email?: string | null
}

export interface ChangePasswordParams {
  oldPassword: string
  newPassword: string
}

export const login = (data: LoginParams) => http.post<LoginResult>('/auth/login', data)
export const getInfo = () => http.get<UserInfo>('/auth/me')
export const refresh = (refreshToken: string) =>
  http.post<LoginResult>('/auth/refresh', { refreshToken })
export const logout = (refreshToken: string) => http.post('/auth/logout', { refreshToken })
export const getProfile = () => http.get<Profile>('/auth/profile')
export const updateProfile = (data: UpdateProfileParams) => http.put('/auth/profile', data)
export const changePassword = (data: ChangePasswordParams) => http.put('/auth/password', data)
