import { http } from '@/utils/request'

export const enum MenuType {
  Catalog = 1,
  Menu = 2,
  Button = 3,
}

export interface MenuNode {
  id: number
  name: string
  parentId: number
  orderNum: number
  menuType: MenuType
  path?: string
  component?: string
  permission?: string
  icon?: string
  isExternal: boolean
  cache: boolean
  visible: boolean
  enabled: boolean
  children: MenuNode[]
}

export interface MenuSaveDto {
  name: string
  parentId: number
  orderNum: number
  menuType: MenuType
  path?: string
  component?: string
  permission?: string
  icon?: string
  isExternal: boolean
  cache: boolean
  visible: boolean
  enabled: boolean
}

/** 当前用户可见菜单树（动态路由用） */
export const getUserRoutes = () => http.get<MenuNode[]>('/menu/routes')
/** 全部菜单树（菜单管理用） */
export const getMenuTree = () => http.get<MenuNode[]>('/menu/tree')
export const getMenu = (id: number) => http.get<MenuNode>(`/menu/${id}`)
export const createMenu = (data: MenuSaveDto) => http.post('/menu', data)
export const updateMenu = (id: number, data: MenuSaveDto) => http.put(`/menu/${id}`, data)
export const deleteMenu = (id: number) => http.delete(`/menu/${id}`)
