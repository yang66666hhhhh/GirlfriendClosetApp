# GirlfriendClosetApp

Windows 桌面端私人数字衣橱应用，面向个人衣物整理、搭配管理、穿着记录，以及 AI 搭配效果图生成与管理。

> 最后更新：2026-06-14
> 当前运行时：`ClosetApp.UI` / `ClosetApp.Tests` 为 `net10.0-windows`，Domain / Application / Infrastructure 为 `net8.0`

## 项目定位

GirlfriendClosetApp 不是内容社区，也不是纯图片浏览器。它的核心目标是把一套个人穿搭数据闭环做稳：

- 管理衣物与标签
- 组合搭配并记录穿着历史
- 基于天气与偏好给出今日推荐
- 为搭配生成或上传 AI 效果图
- 支持本地多用户衣橱工作区，并使用本地账号 + 密码登录隔离会话
- 保障本地图片、数据库、备份与恢复的可维护性

## 当前能力

### 衣柜与搭配

- 新增、编辑、删除衣物
- 批量从图片导入衣物
- 基于 `GarmentType + LayerRole` 生成搭配预览
- 创建、编辑、删除搭配
- 收藏搭配、记录“今天穿了”
- 穿着记录保存名称、单品、数量与预览快照，删除 live 搭配后历史仍保留

### 推荐与记录

- 基于天气、季节、场景、收藏、穿着频次和推荐偏好给出今日搭配推荐
- 支持推荐准备度诊断和推荐详情调试
- 支持查看穿着历史、日历详情、数据洞察与年度报告

### AI 效果图

- 当前用户档案：昵称、身高、外形、风格关键词、效果图上半身照、效果图全身照、云端同意
- OpenAI 兼容接口配置：`Base URL`、模型、超时、API Key
- 支持远端生成搭配效果图
- 支持手动上传效果图，进入同一套历史管理链路
- 每套搭配可保留多张效果图历史，可设首选效果图、删除历史图
- 相同搭配 + 相同档案 + 相同参数会优先复用已保存结果，避免重复生成
- 生成状态包含 `Pending / Succeeded / Failed`

### 数据治理

- SQLite 本地数据库
- 本地用户隔离：衣物、搭配、标签、记录、效果图、个人档案和主要设置按用户隔离
- 三层图片缓存：默认全局为 `images/originals / display / thumbnails`，登录后按用户隔离到 `users/{userId}/images/*`
- AI 资产独立目录：默认全局为 `ai/profile`、`ai/renders/*`，登录后按用户隔离到 `users/{userId}/ai/profile` 与 `users/{userId}/ai/renders/*`
- 主题、天气城市、推荐偏好、AI 配置、搭配卡片展示模式等设置写入 `users/{userId}/*.json`
- ZIP 备份与恢复
- 图片缓存重建、缺图修复、孤儿原图清理、历史图片健康检查
- 本地滚动日志

## 当前 UI 结构

主窗口采用左侧导航 + 右侧内容区：

- `ClothesTab`：衣柜页
- `OutfitsTab`：搭配页
- `TagsTab`：标签页
- `SettingsTab`：设置页

几个和最近迭代强相关的入口：

- 左侧栏头像是当前账号入口，悬停有高亮反馈，点击打开带头像的用户菜单；可退出登录、编辑当前档案，超级管理员可进入“顶部状态栏 + 左侧成员导航 + 右侧选中用户工作台”的用户管理弹窗，维护用户的新增、编辑、头像上传/移除、重置凭证和删除；系统头像与效果图参考照按用户独立存储，用户管理不切换当前会话，更换账号必须先退出登录后重新输入账号密码
- 侧边栏“编辑当前档案”入口已经整合为 `个人中心`，在同一个弹窗里分成“账号资料 / 个人档案 / 安全”三段；账号头像、账号名、显示名、密码和保留的 PIN 凭证维护都从这里处理，AI 参考档案也统一在这里维护
- 登录页已经收敛为固定尺寸的居中表单：顶部只保留品牌头像、`我的衣橱` 与简短副标题，登录区聚焦最近使用账号、账号输入、密码输入和登录按钮；会保留最近成功登录的账号痕迹并自动预填最近一次登录账号，但不会保存密码，也不再提供 PIN 登录主流程
- `SettingsTab` 中使用 `AiImageGenerationSettingsPanel` 管理 AI 配置
- `OutfitCard` 只保留浏览职责
- 搭配页支持“搭配优先 / 效果图优先”卡片展示切换，并保存为当前用户默认偏好
- 点击搭配卡片会打开 `OutfitWorkspaceDialog`
- `OutfitWorkspaceDialog` 直接作为效果图工作台，主视觉优先展示当前效果图
- 设置页已经重排为总览工作台 + 稳定分区的结构，主题、卡片展示、AI 配置、天气推荐、图片维护和备份都按统一的卡片节奏组织；天气城市输入支持可编辑下拉建议，展示值允许保留 `城市 · 省/州 · 国家`，实际天气查询会自动只取主城市段，避免展示文案直接喂给接口导致查不到结果
- 字体大小支持 `小 / 标准 / 舒适 / 大 / 特大` 五档，按当前登录用户保存；重新登录后会自动恢复该用户上次的主题和字号选择
- 全局基础交互已经统一到共享主题资源：按钮、输入框、下拉框、卡片和弹窗都有克制但更明确的悬停、按压和回弹反馈；登录页、侧边栏头像、主题卡会优先吃到这套交互语言
- 登录页、个人中心、标签编辑器、衣物编辑器和效果图工作台已经继续向同一套共享按钮、输入框和 segmented tab 体系收口，减少局部自定义模板造成的割裂

## 技术栈

| 层 | 技术 | 说明 |
|---|---|---|
| UI | WPF (`net10.0-windows`) | 桌面端界面 |
| 核心类库 | .NET (`net8.0`) | Domain / Application / Infrastructure |
| MVVM | CommunityToolkit.Mvvm | ViewModel 与可观察属性 |
| UI 组件 | HandyControl | 基础控件与样式资源 |
| 数据访问 | EF Core + SQLite | 本地持久化 |
| 图片处理 | SixLabors.ImageSharp | 原图、显示图、缩略图处理 |
| 日志 | Serilog | 本地滚动日志 |
| 测试 | xUnit | 单元与集成测试 |

## 项目结构

```text
GirlfriendClosetApp/
├── ClosetApp.Domain/
├── ClosetApp.Application/
├── ClosetApp.Infrastructure/
├── ClosetApp.UI/
├── ClosetApp.UI.Logic/
├── ClosetApp.Tests/
├── docs/
├── README.md
├── PROJECT_DOCUMENTATION.md
└── AGENTS.md
```

## 启动与运行

仓库约定命令优先通过 `rtk` 执行。

```powershell
rtk dotnet build ClosetApp.slnx /m:1
rtk dotnet run --project ClosetApp.UI
rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1
```

## 当前架构要点

- 分层依赖方向：`Domain <- Application <- Infrastructure`，UI 与 Tests 依赖前面各层
- DI 注册集中在 [`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.UI\App.xaml.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.UI/App.xaml.cs)
- 启动链路已优化为：
  1. 先初始化主题
  2. 先显示主窗口
  3. 再由 `AppStartupCoordinator` 在后台完成数据库初始化
  4. 各个 Tab 在真正加载数据前统一等待 readiness
  5. 搭配页首屏优先返回列表，最近穿着记录与日历状态改为后台补载，减少空白等待
- 本地路径由 [`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.Infrastructure\AppPaths.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.Infrastructure/AppPaths.cs) 统一定义
- 关键对象型下拉框现在统一要求显式声明显示映射：要么使用 `DisplayMemberPath`，要么使用 `ItemTemplate`，不能混用；项目里已有对应回归测试，避免选中态退回对象 `ToString()`

## AI 图片生成说明

当前 OpenAI 兼容实现位于 [`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.Infrastructure\Services\OpenAiCompatibleImageGenerationService.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.Infrastructure/Services/OpenAiCompatibleImageGenerationService.cs)。

关键规则：

- `gpt-image-2` 走 `images/generations`
- 其他 `gpt-image-*` 模型走 `images/edits`
- 非 `gpt-image-*` 模型走 `responses`
- `gpt-image-2` 文生图接入不强制要求效果图上半身照；其余参考图工作流仍至少需要效果图上半身照
- `Base URL` 已兼容是否自带 `/v1`
- 生成请求对超时和 502/503/504/522/524 做一次自动重试
- `TimeoutSeconds` 会被限制在 `30-300` 秒之间

## 测试现状

最近一次完整验证目标：

- `rtk dotnet build ClosetApp.slnx /m:1`
- `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1`

当前测试规模为 **322 个测试**。

## 文档入口

- 详细项目文档：[`PROJECT_DOCUMENTATION.md`](D:/03_Projects/Personal/GirlfriendClosetApp/PROJECT_DOCUMENTATION.md)
- 架构约定：[`docs/ARCHITECTURE_CONVENTIONS.md`](D:/03_Projects/Personal/GirlfriendClosetApp/docs/ARCHITECTURE_CONVENTIONS.md)
- 协作约束：[`AGENTS.md`](D:/03_Projects/Personal/GirlfriendClosetApp/AGENTS.md)
