<template>
  <div class="page-container">
    <el-card>
      <el-empty :description="`「${title}」页面建设中`" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
const route = useRoute()
const title = computed(() => (route.meta.title as string) || '该')
</script>
