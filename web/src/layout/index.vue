<template>
  <el-container class="layout">
    <el-aside :width="appStore.sidebarCollapsed ? '64px' : '210px'" class="aside">
      <Sidebar />
    </el-aside>
    <el-container>
      <el-header class="header">
        <Navbar />
      </el-header>
      <el-main class="main">
        <router-view v-slot="{ Component }">
          <transition name="fade-transform" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { useAppStore } from '@/stores/app'
import Sidebar from './components/Sidebar.vue'
import Navbar from './components/Navbar.vue'

const appStore = useAppStore()
</script>

<style scoped lang="scss">
.layout {
  height: 100%;

  .aside {
    transition: width 0.2s;
    overflow: hidden;
  }

  .header {
    padding: 0;
    height: 56px;
  }

  .main {
    background-color: #f0f2f5;
    padding: 0;
    overflow-y: auto;
  }
}
</style>

<style>
.fade-transform-enter-active,
.fade-transform-leave-active {
  transition: all 0.2s;
}
.fade-transform-enter-from {
  opacity: 0;
  transform: translateX(-10px);
}
.fade-transform-leave-to {
  opacity: 0;
  transform: translateX(10px);
}
</style>
