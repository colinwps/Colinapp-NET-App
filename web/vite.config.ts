import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import AutoImport from 'unplugin-auto-import/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    // Element Plus 整体注册在 main.ts；此处仅自动导入 Vue/Router/Pinia API 与 ElMessage 等服务函数
    AutoImport({
      imports: ['vue', 'vue-router', 'pinia'],
      resolvers: [ElementPlusResolver({ importStyle: false })],
      dts: 'src/types/auto-imports.d.ts',
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      // 开发期将 /api 代理到后端，规避跨域
      '/api': {
        target: 'http://localhost:5218',
        changeOrigin: true,
      },
      // 上传文件的静态访问前缀，同样代理到后端
      '/uploads': {
        target: 'http://localhost:5218',
        changeOrigin: true,
      },
    },
  },
})
