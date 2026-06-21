<template>
  <div class="sidebar">
    <div class="logo">
      <span v-if="!appStore.sidebarCollapsed">Colinapp 管理后台</span>
      <span v-else>C</span>
    </div>
    <el-scrollbar>
      <el-menu
        :default-active="activeMenu"
        :collapse="appStore.sidebarCollapsed"
        :collapse-transition="false"
        background-color="#304156"
        text-color="#bfcbd9"
        active-text-color="#409eff"
        router
        unique-opened
      >
        <el-menu-item index="/dashboard">
          <el-icon><HomeFilled /></el-icon>
          <template #title>首页</template>
        </el-menu-item>
        <SidebarItem
          v-for="menu in permStore.menuTree"
          :key="menu.id"
          :item="menu"
          base-path=""
        />
      </el-menu>
    </el-scrollbar>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAppStore } from '@/stores/app'
import { usePermissionStore } from '@/stores/permission'
import SidebarItem from './SidebarItem.vue'

const appStore = useAppStore()
const permStore = usePermissionStore()
const route = useRoute()

const activeMenu = computed(() => route.path)
</script>

<style scoped lang="scss">
.sidebar {
  height: 100%;
  background-color: #304156;
  display: flex;
  flex-direction: column;

  .logo {
    height: 56px;
    line-height: 56px;
    text-align: center;
    color: #fff;
    font-size: 16px;
    font-weight: 600;
    background-color: #2b3649;
    white-space: nowrap;
    overflow: hidden;
  }

  .el-scrollbar {
    flex: 1;
  }

  :deep(.el-menu) {
    border-right: none;
  }
}
</style>
