# GirlfriendClosetApp

Windows 桌面端私人数字衣橱应用，面向个人衣物整理、搭配管理、穿着记录，以及 AI 搭配效果图生成与管理。

> 最后更新：2026-06-06
> 当前运行时：`ClosetApp.UI` / `ClosetApp.Tests` 为 `net10.0-windows`，Domain / Application / Infrastructure 为 `net8.0`

## 项目定位

GirlfriendClosetApp 不是内容社区，也不是纯图片浏览器。它的核心目标是把一套个人穿搭数据闭环做稳：

- 管理衣物与标签
- 组合搭配并记录穿着历史
- 基于天气与偏好给出今日推荐
- 为搭配生成或上传 AI 效果图
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

- 个人档案：昵称、身高、外形、风格关键词、头像照、全身照、云端同意
- OpenAI 兼容接口配置：`Base URL`、模型、超时、API Key
- 支持远端生成搭配效果图
- 支持手动上传效果图，进入同一套历史管理链路
- 每套搭配可保留多张效果图历史，可设首选效果图、删除历史图
- 相同搭配 + 相同档案 + 相同参数会优先复用已保存结果，避免重复生成
- 生成状态包含 `Pending / Succeeded / Failed`

### 数据治理

- SQLite 本地数据库
- 三层图片缓存：`originals / display / thumbnails`
- AI 资产独立目录：`ai/profile`、`ai/renders/*`
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

- 左侧栏头像可点击，打开 `PersonalProfileEditorPanel`
- `SettingsTab` 中使用 `AiImageGenerationSettingsPanel` 管理 AI 配置
- `OutfitCard` 只保留浏览职责
- 搭配页支持“搭配优先 / 效果图优先”卡片展示切换，并保存为全局默认偏好
- 点击搭配卡片会打开 `OutfitWorkspaceDialog`
- `OutfitWorkspaceDialog` 直接作为效果图工作台，主视觉优先展示当前效果图

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
- 本地路径由 [`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.Infrastructure\AppPaths.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.Infrastructure/AppPaths.cs) 统一定义

## AI 图片生成说明

当前 OpenAI 兼容实现位于 [`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.Infrastructure\Services\OpenAiCompatibleImageGenerationService.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.Infrastructure/Services/OpenAiCompatibleImageGenerationService.cs)。

关键规则：

- `gpt-image-2` 走 `images/generations`
- 其他 `gpt-image-*` 模型走 `images/edits`
- 非 `gpt-image-*` 模型走 `responses`
- `gpt-image-2` 文生图接入不强制要求头像照；其余参考图工作流仍至少需要头像照
- `Base URL` 已兼容是否自带 `/v1`
- 生成请求对超时和 502/503/504/522/524 做一次自动重试
- `TimeoutSeconds` 会被限制在 `30-300` 秒之间

## 测试现状

最近一次完整验证目标：

- `rtk dotnet build ClosetApp.slnx /m:1`
- `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1`

当前测试规模为 **203 个测试**。

## 文档入口

- 详细项目文档：[`PROJECT_DOCUMENTATION.md`](D:/03_Projects/Personal/GirlfriendClosetApp/PROJECT_DOCUMENTATION.md)
- 架构约定：[`docs/ARCHITECTURE_CONVENTIONS.md`](D:/03_Projects/Personal/GirlfriendClosetApp/docs/ARCHITECTURE_CONVENTIONS.md)
- 协作约束：[`AGENTS.md`](D:/03_Projects/Personal/GirlfriendClosetApp/AGENTS.md)
