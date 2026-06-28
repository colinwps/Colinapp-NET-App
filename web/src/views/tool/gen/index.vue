<template>
  <div class="page-container gen-page">
    <el-card shadow="never" class="gen-card">
      <div class="gen-header">
        <div class="gen-title">
          <el-icon class="gen-title-icon"><MagicStick /></el-icon>
          <div>
            <h2>代码生成器</h2>
            <p>读取数据库表结构，一键生成实体、服务、控制器与前端页面，遵循框架既有约定。</p>
          </div>
        </div>
      </div>

      <el-steps :active="activeStep" align-center finish-status="success" class="gen-steps">
        <el-step title="选择数据表" description="挑选要生成的表" />
        <el-step title="配置生成项" description="字段与命名映射" />
        <el-step title="预览与下载" description="查看并导出代码" />
      </el-steps>

      <!-- 步骤一：选择数据表 -->
      <div v-show="activeStep === 0" class="step-pane">
        <div class="table-toolbar">
          <el-input
            v-model="tableKeyword"
            placeholder="按表名 / 注释搜索"
            clearable
            style="width: 240px"
            :prefix-icon="Search"
            @keyup.enter="loadTables"
            @clear="loadTables"
          />
          <el-button :icon="Refresh" @click="loadTables">刷新</el-button>
        </div>

        <el-table
          v-loading="tablesLoading"
          :data="tables"
          border
          highlight-current-row
          height="52vh"
          @current-change="onTableSelect"
        >
          <el-table-column width="56" align="center">
            <template #default="{ row }">
              <el-icon v-if="selected?.tableName === (row as DbTable).tableName" color="#409eff">
                <CircleCheckFilled />
              </el-icon>
              <el-icon v-else color="#dcdfe6"><CircleCheck /></el-icon>
            </template>
          </el-table-column>
          <el-table-column prop="tableName" label="表名" min-width="220" />
          <el-table-column prop="tableComment" label="表注释" min-width="200">
            <template #default="{ row }">{{ (row as DbTable).tableComment || '-' }}</template>
          </el-table-column>
          <el-table-column label="创建时间" width="180">
            <template #default="{ row }">{{ formatTime((row as DbTable).createTime) }}</template>
          </el-table-column>
        </el-table>
      </div>

      <!-- 步骤二：配置 -->
      <div v-show="activeStep === 1 && config" class="step-pane">
        <div class="section-title">基本信息</div>
        <el-form v-if="config" :model="config" label-width="92px" class="basic-form">
          <el-row :gutter="20">
            <el-col :span="8"><el-form-item label="实体类名"><el-input v-model="config.className" /></el-form-item></el-col>
            <el-col :span="8"><el-form-item label="功能名称"><el-input v-model="config.functionName" /></el-form-item></el-col>
            <el-col :span="8"><el-form-item label="模块/区域"><el-input v-model="config.moduleName" /></el-form-item></el-col>
            <el-col :span="8"><el-form-item label="业务名"><el-input v-model="config.businessName" /></el-form-item></el-col>
            <el-col :span="8"><el-form-item label="权限前缀"><el-input v-model="config.permissionPrefix" /></el-form-item></el-col>
            <el-col :span="8"><el-form-item label="数据表"><el-input v-model="config.tableName" disabled /></el-form-item></el-col>
          </el-row>
        </el-form>

        <div class="section-title">字段配置</div>
        <el-table :data="bizColumns" border size="small" max-height="44vh">
          <el-table-column prop="columnName" label="列名" min-width="140" fixed />
          <el-table-column label="说明" min-width="130">
            <template #default="{ row }"><el-input v-model="(row as GenColumnConfig).label" size="small" /></template>
          </el-table-column>
          <el-table-column label="属性名" min-width="130">
            <template #default="{ row }"><el-input v-model="(row as GenColumnConfig).fieldName" size="small" /></template>
          </el-table-column>
          <el-table-column label="类型" width="120">
            <template #default="{ row }">
              <el-select v-model="(row as GenColumnConfig).csharpType" size="small">
                <el-option v-for="t in CS_TYPES" :key="t" :value="t" :label="t" />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="控件" width="120">
            <template #default="{ row }">
              <el-select v-model="(row as GenColumnConfig).htmlType" size="small">
                <el-option v-for="t in HTML_TYPES" :key="t.value" :value="t.value" :label="t.label" />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="列表" width="60" align="center">
            <template #default="{ row }"><el-checkbox v-model="(row as GenColumnConfig).isList" /></template>
          </el-table-column>
          <el-table-column label="查询" width="60" align="center">
            <template #default="{ row }"><el-checkbox v-model="(row as GenColumnConfig).isQuery" /></template>
          </el-table-column>
          <el-table-column label="表单" width="60" align="center">
            <template #default="{ row }"><el-checkbox v-model="(row as GenColumnConfig).isForm" /></template>
          </el-table-column>
          <el-table-column label="必填" width="60" align="center">
            <template #default="{ row }"><el-checkbox v-model="(row as GenColumnConfig).isRequired" /></template>
          </el-table-column>
        </el-table>
      </div>

      <!-- 步骤三：预览 -->
      <div v-show="activeStep === 2" class="step-pane">
        <div class="preview-toolbar">
          <el-button v-auth="'tool:gen:code'" type="primary" :icon="Download" :loading="downloading" @click="onDownload">
            下载 ZIP
          </el-button>
          <el-button :icon="CopyDocument" @click="onCopy">复制当前文件</el-button>
          <span class="preview-hint">共 {{ files.length }} 个文件</span>
        </div>

        <el-tabs v-model="activeFile" type="border-card" class="code-tabs">
          <el-tab-pane v-for="f in files" :key="f.fileName" :label="f.fileName" :name="f.fileName">
            <div class="code-path">
              <el-icon><Folder /></el-icon>
              <span>{{ f.targetPath }}</span>
              <el-tag size="small" effect="plain" class="lang-tag">{{ f.language }}</el-tag>
            </div>
            <pre class="code-block"><code>{{ f.content }}</code></pre>
          </el-tab-pane>
        </el-tabs>
      </div>

      <!-- 步骤导航 -->
      <div class="step-footer">
        <el-button v-if="activeStep > 0" @click="activeStep--">上一步</el-button>
        <el-button v-if="activeStep === 0" type="primary" :disabled="!selected" @click="goConfig">
          下一步
        </el-button>
        <el-button v-if="activeStep === 1" type="primary" :loading="previewing" @click="goPreview">
          生成预览
        </el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  Search, Refresh, Download, CopyDocument, Folder, MagicStick,
  CircleCheck, CircleCheckFilled,
} from '@element-plus/icons-vue'
import {
  getTables, getGenConfig, previewCode, downloadZip,
  type DbTable, type GenTableConfig, type GenColumnConfig, type GeneratedFile,
} from '@/api/gen'
import { formatTime } from '@/utils/format'

const CS_TYPES = ['string', 'int', 'long', 'decimal', 'double', 'bool', 'DateTime']
const HTML_TYPES = [
  { value: 'input', label: '文本框' },
  { value: 'textarea', label: '文本域' },
  { value: 'number', label: '数字' },
  { value: 'date', label: '日期' },
  { value: 'switch', label: '开关' },
  { value: 'select', label: '下拉' },
]

const activeStep = ref(0)

// ---- 步骤一 ----
const tables = ref<DbTable[]>([])
const tablesLoading = ref(false)
const tableKeyword = ref('')
const selected = ref<DbTable | null>(null)

async function loadTables() {
  tablesLoading.value = true
  try {
    const res = await getTables(tableKeyword.value)
    tables.value = res.data
  } finally {
    tablesLoading.value = false
  }
}
function onTableSelect(row: DbTable | null) {
  if (row) selected.value = row
}

// ---- 步骤二 ----
const config = ref<GenTableConfig | null>(null)
const bizColumns = computed(() => config.value?.columns.filter((c) => !c.isBase) ?? [])

async function goConfig() {
  if (!selected.value) return
  const res = await getGenConfig(selected.value.tableName)
  config.value = res.data
  activeStep.value = 1
}

// ---- 步骤三 ----
const previewing = ref(false)
const downloading = ref(false)
const files = ref<GeneratedFile[]>([])
const activeFile = ref('')

async function goPreview() {
  if (!config.value) return
  previewing.value = true
  try {
    const res = await previewCode(config.value)
    files.value = res.data
    activeFile.value = files.value[0]?.fileName ?? ''
    activeStep.value = 2
  } finally {
    previewing.value = false
  }
}

async function onCopy() {
  const file = files.value.find((f) => f.fileName === activeFile.value)
  if (!file) return
  try {
    await navigator.clipboard.writeText(file.content)
    ElMessage.success('已复制到剪贴板')
  } catch {
    ElMessage.warning('复制失败，请手动选择')
  }
}

async function onDownload() {
  if (!config.value) return
  downloading.value = true
  try {
    const blob = await downloadZip(config.value)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${config.value.className}-generated.zip`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('已开始下载')
  } finally {
    downloading.value = false
  }
}

loadTables()
</script>

<style scoped lang="scss">
.gen-card {
  border: none;
}

.gen-header {
  margin-bottom: 8px;

  .gen-title {
    display: flex;
    align-items: center;
    gap: 14px;

    .gen-title-icon {
      font-size: 30px;
      color: #409eff;
      background: #ecf5ff;
      padding: 10px;
      border-radius: 12px;
    }

    h2 {
      margin: 0;
      font-size: 19px;
      font-weight: 600;
      color: #303133;
    }

    p {
      margin: 4px 0 0;
      font-size: 13px;
      color: #909399;
    }
  }
}

.gen-steps {
  margin: 22px 0 26px;
}

.step-pane {
  min-height: 320px;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  margin: 6px 0 14px;
  padding-left: 10px;
  border-left: 3px solid #409eff;
}

.basic-form {
  margin-bottom: 18px;
}

.preview-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 14px;

  .preview-hint {
    color: #909399;
    font-size: 13px;
    margin-left: auto;
  }
}

.code-tabs {
  border: none;
  box-shadow: none;
}

.code-path {
  display: flex;
  align-items: center;
  gap: 8px;
  font-family: 'JetBrains Mono', Consolas, Menlo, monospace;
  font-size: 12px;
  color: #909399;
  margin-bottom: 10px;

  .lang-tag {
    margin-left: auto;
    text-transform: uppercase;
  }
}

.code-block {
  margin: 0;
  background: #282c34;
  color: #abb2bf;
  border-radius: 8px;
  padding: 18px 20px;
  font-family: 'JetBrains Mono', Consolas, Menlo, 'Courier New', monospace;
  font-size: 13px;
  line-height: 1.7;
  tab-size: 4;
  overflow: auto;
  max-height: 56vh;
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.04);

  code {
    font-family: inherit;
    white-space: pre;
  }
}

.step-footer {
  display: flex;
  justify-content: center;
  gap: 12px;
  margin-top: 24px;
}
</style>
