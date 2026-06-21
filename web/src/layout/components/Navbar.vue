<template>
  <div class="navbar">
    <el-icon class="collapse-btn" @click="appStore.toggleSidebar()">
      <Fold v-if="!appStore.sidebarCollapsed" />
      <Expand v-else />
    </el-icon>

    <el-breadcrumb separator="/" class="breadcrumb">
      <el-breadcrumb-item v-for="m in matched" :key="m.path">{{ m.meta.title }}</el-breadcrumb-item>
    </el-breadcrumb>

    <div class="flex-spacer" />

    <el-dropdown @command="onCommand">
      <span class="user-info">
        <el-icon><UserFilled /></el-icon>
        <span class="name">{{ userStore.userInfo?.userName }}</span>
        <el-icon><CaretBottom /></el-icon>
      </span>
      <template #dropdown>
        <el-dropdown-menu>
          <el-dropdown-item command="logout">退出登录</el-dropdown-item>
        </el-dropdown-menu>
      </template>
    </el-dropdown>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessageBox } from 'element-plus'
import { useAppStore } from '@/stores/app'
import { useUserStore } from '@/stores/user'
import { usePermissionStore } from '@/stores/permission'

const appStore = useAppStore()
const userStore = useUserStore()
const permStore = usePermissionStore()
const route = useRoute()

const matched = computed(() => route.matched.filter((m) => m.meta && m.meta.title))

async function onCommand(command: string) {
  if (command === 'logout') {
    await ElMessageBox.confirm('确认退出登录？', '提示', { type: 'warning' })
    userStore.reset()
    permStore.reset()
    // 重载以清空已动态添加的路由
    window.location.href = '/login'
  }
}
</script>

<style scoped lang="scss">
.navbar {
  height: 56px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  background: #fff;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);

  .collapse-btn {
    font-size: 20px;
    cursor: pointer;
    margin-right: 16px;
  }

  .breadcrumb {
    display: inline-flex;
  }

  .flex-spacer {
    flex: 1;
  }

  .user-info {
    display: flex;
    align-items: center;
    gap: 6px;
    cursor: pointer;
    outline: none;

    .name {
      font-size: 14px;
    }
  }
}
</style>
