# 全局基础交互增强设计

**日期：** 2026-06-11  
**范围：** WPF 全局基础交互层  
**目标：** 在不重做页面信息架构的前提下，为按钮、下拉、输入框、卡片、弹窗建立一套“更明显互动，但整体仍然安静精致”的共享交互语言。

---

## 1. 设计目标

本轮不追求夸张动画，而是提升“触感”：

- 按钮可按压，而不是只换颜色
- 卡片 hover 有轻微抬升和内容聚焦
- 输入框与下拉在 focus / open 时更像被激活的交互面板
- 弹窗出现与关闭更柔和，但节奏短促
- 圆角更统一，让双主题下的组件更圆润、完整、有连续性

整体风格关键词：

- 更明显互动
- 更精致
- 更安静
- 不软萌

---

## 2. 不在本轮范围内

以下内容本轮明确不动：

- 页面信息架构
- 推荐逻辑、业务流程、数据结构
- AI 生成链路
- 大规模重排单页布局
- 重型页面级动画编排
- 新增额外交互入口

---

## 3. 交互语言

### 3.1 圆角层级

统一提升一个档位，但保留层级差异：

- `Small`：用于密集输入与小型面板
- `Medium`：用于按钮、下拉、输入框主壳
- `Large`：用于卡片、主题卡、分区卡、弹窗块面
- `XLarge`：用于主弹窗和更强展示型容器
- `Pill`：仅保留给极少数真正胶囊型元素

原则：

- 不让不同组件各自使用零散的 10 / 12 / 14 / 18 / 24
- 通过统一 token 驱动，而不是逐个硬编码

### 3.2 Hover

hover 不再只靠变色，统一改为三段式：

1. 背景轻微抬亮
2. 边框更清晰
3. 元素有极小位移或缩放

强度控制：

- 按钮：轻微上浮或收紧
- 卡片：轻微上浮
- 输入 / 下拉：不位移，主要提升边框和壳层
- 图标按钮：反馈略强于普通文字按钮

### 3.3 Press

press 统一做成“下沉 + 收紧”：

- 缩放略小于当前 hover 态
- 局部阴影收短
- Y 方向轻微向下

目标是让按钮、卡片上的操作点、分段切换都像有厚度，而不是平面闪色。

### 3.4 Focus / Open

focus 与展开态要更“被激活”：

- 输入框 focus：边框更清晰，背景略提亮，增加非常轻的 focus glow
- 下拉展开：弹层轻微浮起，关闭态壳层同步被激活
- 分段切换选中：像滑块落位，而不是简单换主色底

### 3.5 Modal

弹窗出现与关闭采用更精致的节奏：

- 打开：淡入 + 轻上移 + 短回弹
- 关闭：更快的淡出 + 回位
- 遮罩：略柔和，不是生硬黑灰蒙层

---

## 4. 共享层改造

### 4.1 Token

优先修改：

- `ClosetApp.UI/Themes/Tokens/Radius.xaml`
- `ClosetApp.UI/Themes/Tokens/Motion.xaml`

计划补充：

- 更清晰的圆角层级
- 更统一的快速 / 正常 / 弹窗动效时长
- 为 hover / press / modal 准备共享 motion 数值

### 4.2 Buttons

优先修改：

- `ClosetApp.UI/Themes/Controls/Buttons.xaml`

目标：

- 把现有 `HoverScaleUp / HoverScaleDown` 收敛成更克制的共享手感
- 按钮按压时增加轻微下沉，不只缩放
- 主按钮、ghost、capsule、icon button 统一节奏

重点收益区域：

- 登录页提交按钮
- 设置页保存 / 危险按钮
- 弹窗底部确认 / 关闭按钮
- 卡片上的更多 / 收藏 / 预览操作点

### 4.3 Inputs / ComboBox / Segmented

优先修改：

- `ClosetApp.UI/Themes/Controls/Inputs.xaml`

目标：

- 输入框 focus 更清晰，但不刺眼
- 下拉关闭态更像可按压面板
- 下拉展开态弹层更有浮起感
- segmented / toolbar toggle 的选中与按压手感更统一

重点收益区域：

- 登录页最近账号下拉
- 搭配页筛选与排序
- 设置页偏好输入与 AI 配置
- 个人中心表单

### 4.4 Cards

优先修改：

- `ClosetApp.UI/Themes/Controls/Cards.xaml`
- `ClosetApp.UI/Components/Shared/ThemeCard.xaml`
- `ClosetApp.UI/Components/Shared/LocalUserAvatar.xaml`

目标：

- 卡片 hover 从“轻放大”升级成“轻抬 + 聚焦”
- 收藏、更多、悬浮信息按钮的壳层更统一
- 主题卡、头像卡、设置概览卡的圆角和阴影节奏收口

### 4.5 Modal

优先修改：

- `ClosetApp.UI/Components/Shared/Modal/ModalCardStyles.xaml`
- `ClosetApp.UI/Components/ModalContainer.xaml`
- `ClosetApp.UI/Components/ModalContainer.xaml.cs`

目标：

- 弹窗卡面圆角与内部头尾壳层更完整
- close button 更像可点击点位
- 入场 / 退场增强为淡入 + 位移 + 轻回弹

---

## 5. 重点回归页面

虽然本轮核心是共享层，但需要重点目测以下页面：

- `LoginWindow`
- `SettingsTab`
- `OutfitsTab`
- `PersonalCenterDialog`
- `LocalUserManagementDialog`
- `WornDayDetailsDialog`

原因：

- 它们集中了按钮、输入框、下拉框、弹窗、卡片
- 是用户最容易感知交互手感变化的区域

---

## 6. 测试与验证

本轮至少需要：

- 保持现有布局测试通过
- 保持 `ComboBoxLayoutRulesTests` 通过
- 新增或更新与交互共享样式相关的布局测试，避免关键资源名丢失
- 运行：
  - `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1 /p:UseSharedCompilation=false`
  - `rtk dotnet build ClosetApp.slnx /m:1`

---

## 7. 实施策略

采用“共享样式先行，页面适配收尾”的方式：

1. 先改 token
2. 再改 Buttons / Inputs / Cards / Modal
3. 再修关键页面因为共享样式变化产生的局部对齐问题
4. 最后补测试与验证

这样可以在最小范围内获得最大的全局体感提升，同时避免把本轮扩成页面重构。
