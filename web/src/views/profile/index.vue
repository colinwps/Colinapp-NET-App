<template>
  <div class="profile-page">
    <el-card>
      <el-tabs v-model="activeTab">
        <!-- 基本资料 -->
        <el-tab-pane label="基本资料" name="basic">
          <el-form ref="basicRef" :model="basic" :rules="basicRules" label-width="80px" class="form">
            <el-form-item label="账号">
              <el-input :value="basic.userName" disabled />
            </el-form-item>
            <el-form-item label="昵称" prop="nickName">
              <el-input v-model="basic.nickName" maxlength="50" />
            </el-form-item>
            <el-form-item label="手机号" prop="phone">
              <el-input v-model="basic.phone" maxlength="20" />
            </el-form-item>
            <el-form-item label="邮箱" prop="email">
              <el-input v-model="basic.email" maxlength="100" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="savingBasic" @click="onSaveBasic">保存</el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 修改密码 -->
        <el-tab-pane label="修改密码" name="password">
          <el-form ref="pwdRef" :model="pwd" :rules="pwdRules" label-width="80px" class="form">
            <el-form-item label="原密码" prop="oldPassword">
              <el-input v-model="pwd.oldPassword" type="password" show-password />
            </el-form-item>
            <el-form-item label="新密码" prop="newPassword">
              <el-input v-model="pwd.newPassword" type="password" show-password />
            </el-form-item>
            <el-form-item label="确认密码" prop="confirmPassword">
              <el-input v-model="pwd.confirmPassword" type="password" show-password />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="savingPwd" @click="onChangePassword">
                确认修改
              </el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { changePassword, getProfile, updateProfile } from '@/api/auth'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()
const activeTab = ref('basic')

// ---- 基本资料 ----
const basicRef = ref<FormInstance>()
const savingBasic = ref(false)
const basic = reactive({ userName: '', nickName: '', phone: '', email: '' })

const basicRules: FormRules = {
  nickName: [{ required: true, message: '请输入昵称', trigger: 'blur' }],
  email: [{ type: 'email', message: '邮箱格式不正确', trigger: 'blur' }],
}

async function loadProfile() {
  const res = await getProfile()
  basic.userName = res.data.userName
  basic.nickName = res.data.nickName
  basic.phone = res.data.phone ?? ''
  basic.email = res.data.email ?? ''
}

async function onSaveBasic() {
  if (!basicRef.value) return
  await basicRef.value.validate()
  savingBasic.value = true
  try {
    await updateProfile({ nickName: basic.nickName, phone: basic.phone, email: basic.email })
    ElMessage.success('资料已更新')
  } finally {
    savingBasic.value = false
  }
}

// ---- 修改密码 ----
const pwdRef = ref<FormInstance>()
const savingPwd = ref(false)
const pwd = reactive({ oldPassword: '', newPassword: '', confirmPassword: '' })

const pwdRules: FormRules = {
  oldPassword: [{ required: true, message: '请输入原密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    {
      validator: (_rule, value, cb) =>
        value === pwd.newPassword ? cb() : cb(new Error('两次输入的密码不一致')),
      trigger: 'blur',
    },
  ],
}

async function onChangePassword() {
  if (!pwdRef.value) return
  await pwdRef.value.validate()
  savingPwd.value = true
  try {
    await changePassword({ oldPassword: pwd.oldPassword, newPassword: pwd.newPassword })
    ElMessage.success('密码已修改，请重新登录')
    // 改密会撤销刷新令牌，主动登出回到登录页
    await userStore.logoutAction()
    window.location.href = '/login'
  } finally {
    savingPwd.value = false
  }
}

onMounted(loadProfile)
</script>

<style scoped lang="scss">
.profile-page {
  .form {
    max-width: 460px;
    margin-top: 8px;
  }
}
</style>
