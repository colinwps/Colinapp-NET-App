<template>
  <el-form ref="formRef" :model="model" :rules="rules" :disabled="disabled" :label-width="labelWidth ?? '100px'">
    <el-row :gutter="12">
      <el-col v-for="f in fields" :key="f.key || f.label" :span="f.span ?? 24">
        <el-divider v-if="f.type === 'divider'" content-position="left">{{ f.label }}</el-divider>
        <el-form-item v-else :label="f.label" :prop="f.key">
          <el-input-number v-if="f.type === 'number'" v-model="model[f.key]"
            style="width: 100%" :controls="false" :placeholder="f.placeholder" />
          <el-date-picker v-else-if="f.type === 'date'" v-model="model[f.key]"
            type="date" value-format="YYYY-MM-DD" style="width: 100%" :placeholder="f.placeholder" />
          <el-date-picker v-else-if="f.type === 'datetime'" v-model="model[f.key]"
            type="datetime" value-format="YYYY-MM-DD HH:mm:ss" style="width: 100%" :placeholder="f.placeholder" />
          <el-select v-else-if="f.type === 'select'" v-model="model[f.key]" clearable
            style="width: 100%" :placeholder="f.placeholder">
            <el-option v-for="o in f.options" :key="o" :value="o" :label="o" />
          </el-select>
          <el-radio-group v-else-if="f.type === 'radio'" v-model="model[f.key]">
            <el-radio v-for="o in f.options" :key="o" :value="o">{{ o }}</el-radio>
          </el-radio-group>
          <el-checkbox-group v-else-if="f.type === 'checkbox'" v-model="model[f.key]">
            <el-checkbox v-for="o in f.options" :key="o" :value="o">{{ o }}</el-checkbox>
          </el-checkbox-group>
          <el-switch v-else-if="f.type === 'switch'" v-model="model[f.key]" />
          <el-input v-else-if="f.type === 'textarea'" v-model="model[f.key]"
            type="textarea" :rows="3" :placeholder="f.placeholder" />
          <!-- 未知类型（向前兼容）与 text 一律退回单行输入 -->
          <el-input v-else v-model="model[f.key]" :placeholder="f.placeholder" />
          <div v-if="f.help" class="field-help">{{ f.help }}</div>
        </el-form-item>
      </el-col>
    </el-row>
  </el-form>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { isInputField, type FormField } from '@/api/form'

/**
 * 动态表单渲染器：按字段 schema 渲染控件并做必填校验。
 * 表单中心填报、工作流发起/重提、设计器预览共用。
 * 值类型随控件而变（number/string/boolean/string[]），序列化时原样带出。
 */
const props = defineProps<{
  fields: FormField[]
  disabled?: boolean
  labelWidth?: string
}>()

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const model = defineModel<Record<string, any>>({ default: () => ({}) })

const formRef = ref<FormInstance>()

/** 字段列表变化时初始化值：已有值保留（回填场景），否则 defaultValue 优先；checkbox 需数组、switch 需布尔 */
watch(() => props.fields, (fields) => {
  const next: Record<string, unknown> = {}
  for (const f of fields.filter(isInputField)) {
    const existing = model.value[f.key]
    if (existing !== undefined) {
      next[f.key] = existing
    } else if (f.defaultValue !== undefined && f.defaultValue !== null) {
      next[f.key] = f.defaultValue
    } else if (f.type === 'checkbox') {
      next[f.key] = []
    } else if (f.type === 'switch') {
      next[f.key] = false
    } else {
      next[f.key] = undefined
    }
  }
  model.value = next
}, { immediate: true })

const rules = computed<FormRules>(() => {
  const r: FormRules = {}
  for (const f of props.fields.filter(isInputField)) {
    if (!f.required) continue
    r[f.key] = f.type === 'checkbox'
      ? [{ required: true, type: 'array', min: 1, message: `请选择「${f.label}」`, trigger: 'change' }]
      : [{ required: true, message: `请填写「${f.label}」`, trigger: ['blur', 'change'] }]
  }
  return r
})

async function validate() {
  if (!formRef.value) return true
  return formRef.value.validate()
}

defineExpose({ validate })
</script>

<style scoped>
.field-help {
  width: 100%;
  font-size: 12px;
  line-height: 1.5;
  color: var(--el-text-color-secondary);
}
</style>
