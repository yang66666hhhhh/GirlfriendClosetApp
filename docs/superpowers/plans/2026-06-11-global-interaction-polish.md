# 全局基础交互增强 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为按钮、下拉、输入框、卡片、弹窗建立更明显互动但整体安静精致的共享交互手感。

**Architecture:** 先调整全局 token，再改共享样式层，最后修正高频页面适配和测试。业务逻辑不改，重点放在圆角层级、hover/press/focus/open/modal 动效与视觉反馈统一。

**Tech Stack:** WPF, XAML ResourceDictionary, HandyControl, xUnit

---

### Task 1: 更新交互 Token

**Files:**
- Modify: `ClosetApp.UI/Themes/Tokens/Radius.xaml`
- Modify: `ClosetApp.UI/Themes/Tokens/Motion.xaml`
- Test: `ClosetApp.Tests/SettingsLayoutTests.cs`

- [ ] **Step 1: 写一个会失败的布局测试断言新的 token 存在**

- [ ] **Step 2: 运行指定测试确认失败**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter FullyQualifiedName~SettingsLayoutTests /m:1 /p:UseSharedCompilation=false`

- [ ] **Step 3: 增加统一圆角和动效 token**

- [ ] **Step 4: 重新运行测试确认通过**

- [ ] **Step 5: 提交本任务**

---

### Task 2: 统一按钮的 hover / press / 回弹语言

**Files:**
- Modify: `ClosetApp.UI/Themes/Controls/Buttons.xaml`
- Modify: `ClosetApp.UI/Themes/Controls/Cards.xaml`
- Test: `ClosetApp.Tests/SettingsLayoutTests.cs`

- [ ] **Step 1: 写或更新测试，保护按钮共享样式关键资源**

- [ ] **Step 2: 运行测试确认失败或至少覆盖新断言**

- [ ] **Step 3: 调整按钮共享动画、圆角、hover 和 press 反馈**

- [ ] **Step 4: 调整卡片内图标按钮壳层，使其跟按钮体系一致**

- [ ] **Step 5: 运行测试确认通过**

- [ ] **Step 6: 提交本任务**

---

### Task 3: 统一输入框、下拉和分段控件手感

**Files:**
- Modify: `ClosetApp.UI/Themes/Controls/Inputs.xaml`
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml`
- Modify: `ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml`
- Test: `ClosetApp.Tests/ComboBoxLayoutRulesTests.cs`
- Test: `ClosetApp.Tests/LoginWindowLayoutTests.cs`

- [ ] **Step 1: 先扩测试，保护对象型下拉的显示映射与共享样式**

- [ ] **Step 2: 运行相关测试确认当前覆盖到位**

- [ ] **Step 3: 调整输入框、下拉框、弹层、分段与 toggle 的 hover / focus / open / checked 反馈**

- [ ] **Step 4: 检查登录页最近账号下拉和设置页偏好下拉的适配**

- [ ] **Step 5: 运行相关测试确认通过**

- [ ] **Step 6: 提交本任务**

---

### Task 4: 增强弹窗壳层与过渡

**Files:**
- Modify: `ClosetApp.UI/Components/Shared/Modal/ModalCardStyles.xaml`
- Modify: `ClosetApp.UI/Components/ModalContainer.xaml`
- Modify: `ClosetApp.UI/Components/ModalContainer.xaml.cs`
- Modify: `ClosetApp.UI/Components/Shared/Modal/PersonalCenterDialog.xaml`
- Test: `ClosetApp.Tests/LoginWindowLayoutTests.cs`

- [ ] **Step 1: 补充测试，保护关键弹窗资源和个人中心结构**

- [ ] **Step 2: 运行测试确认断言生效**

- [ ] **Step 3: 增强弹窗圆角、关闭按钮和遮罩层**

- [ ] **Step 4: 为 modal 容器增加轻微入场 / 退场位移动画**

- [ ] **Step 5: 运行测试确认通过**

- [ ] **Step 6: 提交本任务**

---

### Task 5: 修正关键页面适配并补最终验证

**Files:**
- Modify: `ClosetApp.UI/Views/SettingsTab.xaml`
- Modify: `ClosetApp.UI/Views/OutfitsTab.xaml.cs`
- Modify: `ClosetApp.UI/Views/NavigationSidebar.xaml`
- Modify: `ClosetApp.UI/Components/Shared/ThemeCard.xaml`
- Modify: `ClosetApp.UI/Components/Shared/LocalUserAvatar.xaml`
- Test: `ClosetApp.Tests/SettingsTabLayoutTests.cs`
- Test: `ClosetApp.Tests/SettingsLayoutTests.cs`
- Test: `ClosetApp.Tests/WornDayDetailsDialogLayoutTests.cs`

- [ ] **Step 1: 目测并修正共享样式变化导致的局部挤压、错位或圆角不协调**

- [ ] **Step 2: 更新相关布局测试**

- [ ] **Step 3: 运行完整测试**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1 /p:UseSharedCompilation=false`

- [ ] **Step 4: 运行完整构建**

Run: `rtk dotnet build ClosetApp.slnx /m:1`

- [ ] **Step 5: 提交本任务**

