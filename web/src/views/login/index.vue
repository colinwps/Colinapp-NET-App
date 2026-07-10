<template>
  <div class="login-page">
    <!-- 背景装饰 -->
    <div class="bg-decoration">
      <div class="glow glow-1" />
      <div class="glow glow-2" />
      <div class="glow glow-3" />
      <div class="grid-overlay" />
    </div>

    <div class="login-panel">
      <!-- 品牌区 -->
      <div class="brand">
        <div class="logo">
          <svg viewBox="0 0 36 36" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect x="3" y="3" width="13" height="13" rx="3.5" fill="url(#lg1)" />
            <rect x="20" y="3" width="13" height="13" rx="3.5" fill="url(#lg1)" opacity="0.55" />
            <rect x="3" y="20" width="13" height="13" rx="3.5" fill="url(#lg1)" opacity="0.55" />
            <rect x="20" y="20" width="13" height="13" rx="3.5" fill="url(#lg1)" opacity="0.3" />
            <defs>
              <linearGradient id="lg1" x1="0" y1="0" x2="36" y2="36" gradientUnits="userSpaceOnUse">
                <stop stop-color="#6ab8ff" />
                <stop offset="1" stop-color="#409eff" />
              </linearGradient>
            </defs>
          </svg>
        </div>
        <h1 class="brand-name">Colinapp</h1>
        <p class="brand-desc">通用后台管理框架</p>
      </div>

      <!-- 登录卡片 -->
      <div class="login-card">
        <div class="card-header">
          <span class="card-title">欢迎回来</span>
          <span class="card-subtitle">请使用您的账号登录系统</span>
        </div>

        <el-form
          ref="formRef"
          :model="form"
          :rules="rules"
          size="large"
          class="login-form"
          @keyup.enter="onSubmit"
        >
          <el-form-item prop="userName">
            <el-input
              v-model="form.userName"
              placeholder="用户名"
              :prefix-icon="User"
              autocomplete="username"
            />
          </el-form-item>
          <el-form-item prop="password">
            <el-input
              v-model="form.password"
              type="password"
              placeholder="密码"
              show-password
              :prefix-icon="Lock"
              autocomplete="current-password"
            />
          </el-form-item>
          <el-button type="primary" class="submit" :loading="loading" @click="onSubmit">
            {{ loading ? '登录中…' : '登 录' }}
          </el-button>
        </el-form>

        <div class="tip">
          <el-icon><InfoFilled /></el-icon>
          <span>默认管理员：admin / Admin@123</span>
        </div>
      </div>

      <div class="footer">Colinapp · ASP.NET Core &amp; Vue 3</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { User, Lock, InfoFilled } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

const formRef = ref<FormInstance>()
const loading = ref(false)
const form = reactive({ userName: 'admin', password: 'Admin@123' })

const rules: FormRules = {
  userName: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
}

async function onSubmit() {
  if (!formRef.value) return
  await formRef.value.validate()
  loading.value = true
  try {
    await userStore.loginAction({ ...form })
    ElMessage.success('登录成功')
    const redirect = (route.query.redirect as string) || '/'
    router.push(redirect)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped lang="scss">
.login-page {
  position: relative;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  background: linear-gradient(160deg, #0e1726 0%, #17253c 55%, #1c2f4a 100%);
}

/* ---------- 背景装饰 ---------- */
.bg-decoration {
  position: absolute;
  inset: 0;
  pointer-events: none;

  .glow {
    position: absolute;
    border-radius: 50%;
    filter: blur(90px);
    opacity: 0.5;
    animation: drift 18s ease-in-out infinite alternate;
  }

  .glow-1 {
    width: 480px;
    height: 480px;
    top: -140px;
    left: -100px;
    background: radial-gradient(circle, rgba(64, 158, 255, 0.55), transparent 70%);
  }

  .glow-2 {
    width: 420px;
    height: 420px;
    bottom: -160px;
    right: -80px;
    background: radial-gradient(circle, rgba(99, 102, 241, 0.45), transparent 70%);
    animation-delay: -6s;
  }

  .glow-3 {
    width: 300px;
    height: 300px;
    top: 40%;
    left: 58%;
    background: radial-gradient(circle, rgba(45, 212, 191, 0.22), transparent 70%);
    animation-delay: -12s;
  }

  .grid-overlay {
    position: absolute;
    inset: 0;
    background-image:
      linear-gradient(rgba(255, 255, 255, 0.035) 1px, transparent 1px),
      linear-gradient(90deg, rgba(255, 255, 255, 0.035) 1px, transparent 1px);
    background-size: 44px 44px;
    mask-image: radial-gradient(ellipse 70% 60% at 50% 45%, #000 30%, transparent 100%);
  }
}

@keyframes drift {
  from {
    transform: translate(0, 0) scale(1);
  }
  to {
    transform: translate(50px, 34px) scale(1.12);
  }
}

/* ---------- 面板 ---------- */
.login-panel {
  position: relative;
  z-index: 1;
  width: 400px;
  max-width: calc(100vw - 32px);
  animation: rise 0.55s cubic-bezier(0.22, 0.61, 0.36, 1) both;
}

@keyframes rise {
  from {
    opacity: 0;
    transform: translateY(18px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* ---------- 品牌区 ---------- */
.brand {
  text-align: center;
  margin-bottom: 26px;

  .logo {
    width: 52px;
    height: 52px;
    margin: 0 auto 14px;
    padding: 10px;
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.06);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 8px 24px rgba(64, 158, 255, 0.18);

    svg {
      width: 100%;
      height: 100%;
      display: block;
    }
  }

  .brand-name {
    font-size: 26px;
    font-weight: 700;
    letter-spacing: 1px;
    color: #f4f7fb;
  }

  .brand-desc {
    margin-top: 6px;
    font-size: 13px;
    letter-spacing: 3px;
    color: rgba(226, 235, 248, 0.55);
  }
}

/* ---------- 登录卡片 ---------- */
.login-card {
  padding: 30px 30px 24px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.07);
  border: 1px solid rgba(255, 255, 255, 0.12);
  backdrop-filter: blur(18px);
  -webkit-backdrop-filter: blur(18px);
  box-shadow:
    0 20px 50px rgba(4, 12, 26, 0.45),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

.card-header {
  margin-bottom: 24px;

  .card-title {
    display: block;
    font-size: 19px;
    font-weight: 600;
    color: #f4f7fb;
  }

  .card-subtitle {
    display: block;
    margin-top: 5px;
    font-size: 13px;
    color: rgba(226, 235, 248, 0.5);
  }
}

.login-form {
  :deep(.el-form-item) {
    margin-bottom: 22px;
  }

  :deep(.el-input__wrapper) {
    height: 46px;
    padding: 0 14px;
    border-radius: 10px;
    background: rgba(9, 18, 33, 0.45);
    box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.12) inset;
    transition:
      box-shadow 0.25s,
      background 0.25s;

    &:hover {
      box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.24) inset;
    }

    &.is-focus {
      background: rgba(9, 18, 33, 0.6);
      box-shadow:
        0 0 0 1px rgba(96, 172, 255, 0.85) inset,
        0 0 0 3px rgba(64, 158, 255, 0.18);
    }
  }

  :deep(.el-input__inner) {
    color: #eaf1fa;
    caret-color: #7cbcff;

    &::placeholder {
      color: rgba(226, 235, 248, 0.35);
    }
  }

  :deep(.el-input__prefix),
  :deep(.el-input__suffix) {
    color: rgba(226, 235, 248, 0.45);
  }

  :deep(.el-form-item__error) {
    color: #ff9d9d;
  }

  :deep(.el-form-item.is-error .el-input__wrapper) {
    box-shadow: 0 0 0 1px rgba(255, 128, 128, 0.7) inset;
  }
}

.submit {
  width: 100%;
  height: 46px;
  margin-top: 4px;
  border: none;
  border-radius: 10px;
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 6px;
  text-indent: 6px;
  background: linear-gradient(135deg, #4facff 0%, #3a7dff 100%);
  box-shadow: 0 10px 22px rgba(58, 125, 255, 0.32);
  transition:
    transform 0.2s,
    box-shadow 0.2s,
    filter 0.2s;

  &:hover:not(.is-loading) {
    filter: brightness(1.08);
    transform: translateY(-1px);
    box-shadow: 0 14px 28px rgba(58, 125, 255, 0.4);
  }

  &:active {
    transform: translateY(0);
    box-shadow: 0 6px 16px rgba(58, 125, 255, 0.3);
  }
}

.tip {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  margin-top: 20px;
  padding: 9px 12px;
  border-radius: 8px;
  font-size: 12px;
  color: rgba(226, 235, 248, 0.55);
  background: rgba(64, 158, 255, 0.08);
  border: 1px dashed rgba(64, 158, 255, 0.28);

  .el-icon {
    font-size: 14px;
    color: rgba(124, 188, 255, 0.8);
  }
}

.footer {
  margin-top: 22px;
  text-align: center;
  font-size: 12px;
  letter-spacing: 0.5px;
  color: rgba(226, 235, 248, 0.3);
}

@media (max-width: 480px) {
  .login-card {
    padding: 24px 20px 20px;
  }
}
</style>
