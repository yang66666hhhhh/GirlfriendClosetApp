# GirlfriendClosetApp

私人数字衣橱桌面应用，面向个人衣物整理、搭配管理和本地数据治理场景。项目使用 WPF + SQLite，采用 Domain / Application / Infrastructure / UI 四层结构。

> 更新时间：2026-05-23
> 当前运行时：.NET 10 / WPF

## 当前能力

- 衣柜管理：新增、编辑、删除衣物，支持图片、季节、品牌、备注、收藏状态和批量导入；批量导入会提示同名/同尺寸图片风险并支持一键移除
- 搭配管理：创建和编辑搭配，按“人体区域 + 穿搭层级”生成预览，支持穿着记录和天气驱动的今日推荐；推荐会结合季节、收藏、穿着记录、场景、标签和颜色偏好
- 标签管理：标签维护与选择复用组件
- 设置中心：数据目录、日志、图片缓存、备份、导入恢复、缺失图片修复
- 本地数据治理：
  - ZIP 备份包：`backup.json` + `images/`
  - 兼容旧版 JSON 备份导入
  - 导出前校验、图片覆盖统计与警告提示
  - 导入结果摘要、缺失图片提示、备份历史

## 项目结构

```text
GirlfriendClosetApp/
├── ClosetApp.Domain/                 # 实体、枚举、仓储接口、衣物分类模型
├── ClosetApp.Application/            # DTO、服务接口/实现、UseCases
├── ClosetApp.Infrastructure/         # EF Core、SQLite、图片/备份/日志等基础设施
├── ClosetApp.UI/                     # WPF 页面、组件、状态类、主题资源
├── ClosetApp.Tests/                  # 逻辑测试（不直接引用整个 UI 工程）
├── docs/
│   └── ARCHITECTURE_CONVENTIONS.md   # 架构约定
└── PROJECT_DOCUMENTATION.md          # 详细项目文档
```

## UI 入口

当前主界面由左侧导航 + 右侧内容区组成，包含 4 个主页面：

- `ClothesTab`：衣柜页，瀑布流卡片、搜索、分类筛选
- `OutfitsTab`：搭配页，统一编辑器、穿搭预览与记录
- `TagsTab`：标签页，维护标签数据
- `SettingsTab`：设置页，负责数据治理与本地文件维护

## 关键实现

### 1. 统一编辑器与状态类

- 衣物、搭配、标签编辑逐步统一为 Editor Panel 模式
- Tab 页面状态下沉到 `ClosetApp.UI/States`
- 页面 code-behind 主要负责交互、动画和 modal 编排

### 2. 备份与恢复

- `ClosetApp.Application/Interfaces/IBackupService.cs`
- `ClosetApp.Infrastructure/Services/BackupService.cs`
- `ClosetApp.Application/DTOs/BackupDtos.cs`

当前支持：

- `ValidateExportAsync`：导出前校验
- `ExportAsync`：导出 ZIP 或 JSON
- `ImportAsync`：导入 ZIP 或 JSON
- `GetHistoryAsync` / `ClearHistoryAsync`：读取与清空备份历史
- `BuildDefaultBackupPath`：生成默认备份路径

备份历史默认保存于：

- `%LocalAppData%\ClosetApp\backups\backup-history.json`

### 3. 图片资产体系

- `ImageStorageService`：原图 / 主视觉 / 小预览存储
- `ImageMaintenanceService`：检测缺失图片、统计图片缓存缺口并执行重建
- `ImageAssetResolver`：统一图片解析

图片按视觉用途分层：

- `Original`：原始资产，编辑器和备份使用，保存时不压缩覆盖
- `Display`：衣柜瀑布流、搭配卡片、穿搭预览使用，默认最大边约 900px
- `Thumbnail`：小型选择卡、摘要列表等低成本预览使用，默认最大边约 200px

设置页可直接：

- 查看缺失图片数量
- 查看图片缓存健康状态并一键重建缺失缓存
- 扫描并清理数据库未引用的孤儿原图
- 查看备份前的数据规模、图片覆盖情况和导出提醒
- 选择旧图片目录批量修复
- 清理主视觉和小预览缓存

### 4. 应用层 UseCases

新业务流程优先放在 `ClosetApp.Application/UseCases`：

- `GetWardrobeOverview`
- `GetOutfitHistorySummary`
- `RecordOutfitWorn`
- `GetRecommendationReadinessSummary`
- `GetTagsForSelection`

### 5. 搭配预览模型

搭配预览不再按衣物分类简单堆叠，而是按人体区域表达：

- 上半身区域：外套为外层主图，上衣/中层作为内层露出
- 下半身区域：裤装或半裙二选一
- 脚部区域：鞋子位于底部
- 配饰区域：作为角标/侧边信息展示，不参与主轴高度

当前实现位于 `ClosetApp.UI/Components/Outfit/Engine/OutfitCompositionEngine.cs`，渲染由 `OutfitPreviewCanvas` 完成。

## 本地数据目录

由 `ClosetApp.Infrastructure/AppPaths.cs` 统一定义：

- 数据根目录：`%LocalAppData%\ClosetApp\`
- 数据库：`%LocalAppData%\ClosetApp\closet.db`
- 图片根目录：`%LocalAppData%\ClosetApp\images\`
- 原图：`%LocalAppData%\ClosetApp\images\originals\`
- 主视觉缓存：`%LocalAppData%\ClosetApp\images\display\`
- 小预览缓存：`%LocalAppData%\ClosetApp\images\thumbnails\`
- 日志：`%LocalAppData%\ClosetApp\logs\`
- 备份：`%LocalAppData%\ClosetApp\backups\`

## 开发命令

仓库约定：命令优先通过 `rtk` 执行。

```powershell
rtk dotnet build ClosetApp.slnx /m:1
rtk dotnet run --project ClosetApp.UI
rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1
```

如果需要查看目录或读取文件，使用：

```powershell
rtk pwsh -Command "Get-ChildItem -Force"
```

## 测试说明

`ClosetApp.Tests` 当前主要覆盖：

- `BackupService`
- `ImageMaintenanceService`
- `ImageStorageService`
- `OutfitCompositionEngine`
- `OutfitSelectionRules`
- `BatchClothingImportBuilder`
- `ClothesTabState` / `OutfitsTabState` / `TagsTabState`

测试工程已避免直接引用整个 `ClosetApp.UI.csproj`，而是按需链接纯逻辑源码文件，减少 WPF 生成链对测试的干扰。

## 当前已知说明

- `WeatherService` 已完整实现（Open-Meteo API，支持城市搜索、15 分钟缓存、天气代码映射）
- `ViewModels/` 仍存在，但当前页面主要由 View + Service / UseCase / State 驱动
- `Themes/Colors.xaml` 是兼容转发层，新设计 token 位于 `Themes/Tokens` 与 `Themes/Controls`

## 文档入口

- 详细项目文档：[`PROJECT_DOCUMENTATION.md`](./PROJECT_DOCUMENTATION.md)
- 架构约定：[`docs/ARCHITECTURE_CONVENTIONS.md`](./docs/ARCHITECTURE_CONVENTIONS.md)
- 协作约束：[`AGENTS.md`](./AGENTS.md)
