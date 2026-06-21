<template>
  <div class="page-container">
    <el-card>
      <h2>欢迎使用 Colinapp 通用后台管理框架</h2>
      <p class="welcome">
        当前登录：<b>{{ userStore.userInfo?.userName }}</b>
        <el-tag v-if="userStore.isAdmin" type="danger" size="small" style="margin-left: 8px">
          超级管理员
        </el-tag>
      </p>
      <el-descriptions :column="2" border style="margin-top: 16px">
        <el-descriptions-item label="技术栈（后端）">
          ASP.NET Core (.NET 10) · EF Core · MySQL · JWT
        </el-descriptions-item>
        <el-descriptions-item label="技术栈（前端）">
          Vue 3 · Vite · TypeScript · Element Plus · Pinia
        </el-descriptions-item>
        <el-descriptions-item label="角色数">{{ userStore.userInfo?.roleIds?.length ?? 0 }}</el-descriptions-item>
        <el-descriptions-item label="权限点数">{{ userStore.permissions.length }}</el-descriptions-item>
      </el-descriptions>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { useUserStore } from '@/stores/user'
const userStore = useUserStore()
</script>

<style scoped>
.welcome {
  margin-top: 12px;
  color: #606266;
}
</style>
