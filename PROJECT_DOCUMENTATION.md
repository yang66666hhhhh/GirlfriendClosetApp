# GirlfriendClosetApp 项目文档

> 最后更新时间：2026-05-18
> 当前状态：主流程可用，近期重点已转向设置中心、备份恢复与本地数据治理体验

---

## 1. 项目概览

GirlfriendClosetApp 是一款运行在 Windows 上的私人数字衣橱应用，目标是把个人衣物、搭配、标签、穿着记录和本地图片资产统一管理在一个离线桌面端里。

当前版本强调三件事：

- 衣柜与搭配的日常维护体验
- 本地图片与 SQLite 数据的稳定保存
- 备份、恢复、校验、修复等数据治理能力

---

## 2. 技术栈

| 层 | 技术 | 说明 |
|---|---|---|
| UI | WPF (`net10.0-windows`) | 桌面端界面 |
| UI 组件 | HandyControl | 基础控件与样式能力 |
| 应用层 | CommunityToolkit.Mvvm | 保留 ViewModel 能力，当前页面逻辑以 View + State + Service/UseCase 为主 |
| 数据访问 | EF Core + SQLite | 本地数据库持久化 |
| 图片处理 | SixLabors.ImageSharp | 图片保存与缩略图相关处理 |
| 日志 | Serilog.Sinks.File | 本地滚动日志 |

---

## 3. 当前架构

项目采用四层结构：

```text
View / Component / State
  -> Application Service / UseCase
    -> Repository
      -> EF Core / SQLite / File System
```

目录概览：

```text
GirlfriendClosetApp/
├── ClosetApp.Domain/
│   ├── Entities/                 # Clothing, Outfit, Tag, Favorite, OutfitWornRecord
│   ├── Enums/                    # ClothingType, Season, OutfitScene, TagCategory
│   ├── Interfaces/               # 仓储接口
│   └── Clothing/                 # 服饰分类模型与映射
├── ClosetApp.Application/
│   ├── DTOs/                     # Outfit DTO、Backup DTO
│   ├── Interfaces/               # 服务接口
│   ├── Services/                 # ClothingService, OutfitService, TagService...
│   ├── UseCases/                 # GetWardrobeOverview, RecordOutfitWorn...
│   └── Images/                   # 图片资产解析抽象
├── ClosetApp.Infrastructure/
│   ├── Data/                     # ClosetDbContext, DesignTimeDbContextFactory
│   ├── Repositories/             # 仓储实现
│   ├── Services/                 # BackupService, ImageStorageService, ImageMaintenanceService...
│   └── Migrations/               # EF Core 迁移
├── ClosetApp.UI/
│   ├── Views/                    # ClothesTab, OutfitsTab, TagsTab, SettingsTab
│   ├── Components/               # 服饰卡片、搭配引擎、共享弹层、标签组件
│   ├── States/                   # Tab 页面轻状态类
│   ├── Themes/                   # Tokens / Controls / 兼容资源
│   ├── Services/                 # ModalService, ToastService, ClothingImageLoader
│   └── ViewModels/               # 仍保留的 VM
├── ClosetApp.Tests/              # 纯逻辑测试工程
└── docs/
```

---

## 4. 领域模型

### 4.1 核心实体

#### Clothing

- `Guid Id`
- `string Name`
- `ClothingType Type`
- `GarmentType GarmentType`
- `string? ImagePath`
- `string? Color`
- `string? Brand`
- `string? Notes`
- `Season Season`
- `int FavoriteLevel`
- `bool IsFavorite`

#### Outfit

- `Guid Id`
- `string Name`
- `OutfitScene Scene`
- `Season Season`
- `int Rating`
- `string? Notes`
- `DateTime? WornDate`
- `int WearCount`

#### Tag

- `Guid Id`
- `string Name`
- `string Color`

#### 关联 / 衍生实体

- `ClothingTag`
- `OutfitClothing`
- `Favorite`
- `OutfitWornRecord`

### 4.2 枚举

- `ClothingType`: `Top`, `Bottom`, `Outerwear`, `Dress`, `Skirt`, `Shoes`, `Accessory`
- `Season`: `Spring`, `Summer`, `Autumn`, `Winter`, `AllSeason`
- `OutfitScene`: `Work`, `Date`, `Travel`, `Party`, `Casual`
- `TagCategory`: 用于标签选择与复用

---

## 5. UI 结构

### 5.1 主窗口

`MainWindow` 采用左侧导航 + 右侧内容区布局。

- 左侧：`NavigationSidebar`
- 右侧内容页：
  - `ClothesTab`
  - `OutfitsTab`
  - `TagsTab`
  - `SettingsTab`
- 覆盖层：`ModalContainer`

### 5.2 页面职责

#### ClothesTab

- 瀑布流展示衣物
- 搜索与分类筛选
- 打开衣物编辑器
- 依赖 `ClothesTabState` 维护页面状态

#### OutfitsTab

- 展示搭配列表
- 创建 / 编辑 / 删除搭配
- 记录穿着行为
- 使用 `OutfitEditorPanel` 与 `OutfitsTabState`

#### TagsTab

- 标签列表维护
- 标签编辑器与可选择标签组件复用
- 依赖 `TagsTabState`

#### SettingsTab

当前是本轮重点页面，负责：

- 数据目录展示
- 日志与缩略图缓存清理
- 备份导出 / 导入
- 导出前校验
- 导入结果摘要卡片
- 备份历史展示
- 缺失图片检测与修复入口

### 5.3 状态类约定

见 `docs/ARCHITECTURE_CONVENTIONS.md`：

- 页面轻状态放在 `ClosetApp.UI/States`
- State 负责搜索文本、筛选器、加载标记、当前集合与空状态
- 交互和 modal 编排仍可保留在 code-behind

---

## 6. 编辑器与组件模式

### 6.1 Editor Panel

当前项目逐步统一采用 Editor Panel 模式：

- 面板实现 `IEditorPanel<T>`
- 结果统一用 `EditorResult<T>`
- 结果类型：`Saved` / `Deleted` / `Cancelled`
- 建议通过 `EditorModal.Show(...)` 打开

### 6.2 关键组件

#### Clothing

- `Components/Clothing/ClothingEditorPanel`
- `PremiumClothingCard`

#### Outfit

- `Components/Outfit/Engine/OutfitCompositionEngine`
- `Components/Outfit/Controls/OutfitPreviewCanvas`
- `Components/Outfit/Controls/OutfitCard`
- `Components/Outfit/Editor/OutfitEditorPanel`

#### Shared / Tags

- `Components/Shared/Modal/*`
- `Components/Shared/States/EmptyState`
- `Components/Tags/Controls/*`

---

## 7. 应用层服务与 UseCase

### 7.1 核心服务

已在 `App.xaml.cs` 中注册：

- `IClothingService`
- `IOutfitService`
- `ITagService`
- `IOutfitRecommendationService`
- `IBackupService`
- `IImageMaintenanceService`
- `IImageStorageService`
- `IImageAssetResolver`
- `IWeatherService`

### 7.2 UseCase 目录

新的业务流程优先放在 `ClosetApp.Application/UseCases`：

- `Clothing/GetWardrobeOverview`
- `Insights/GetOutfitHistorySummary`
- `Outfits/RecordOutfitWorn`
- `Tags/GetTagsForSelection`

这样做的目标是把“页面怎么点”与“业务要完成什么”拆开，减少 code-behind 继续膨胀。

---

## 8. 备份与数据治理

### 8.1 备份接口

`IBackupService` 当前提供：

```csharp
Task<BackupValidationResult> ValidateExportAsync(string filePath);
Task<BackupExportResult> ExportAsync(string filePath);
Task<BackupImportResult> ImportAsync(string filePath);
Task<IReadOnlyList<BackupHistoryItem>> GetHistoryAsync(int maxCount = 8);
Task ClearHistoryAsync();
string BuildDefaultBackupPath();
```

### 8.2 支持格式

#### ZIP 备份包

默认推荐格式，包含：

- `backup.json`
- `images/` 目录下的图片文件

特点：

- 同时备份数据库核心数据与图片资产
- 导入时可直接恢复图片
- 导出前会检查缺失图片并给出警告

#### JSON 备份

兼容旧格式，仅保存核心数据，不附带图片文件。

特点：

- 可用于轻量导出
- 导入后可能需要配合“缺失图片修复”

### 8.3 备份 DTO

位于 `ClosetApp.Application/DTOs/BackupDtos.cs`：

- `BackupValidationResult`
- `BackupExportResult`
- `BackupImportResult`
- `BackupHistoryItem`

其中：

- `BackupValidationResult` 提供导出前数据量和警告信息
- `BackupImportResult` 提供导入结果摘要、恢复图片数、缺失文件名、修复建议
- `BackupHistoryItem` 提供 UI 可直接展示的时间、状态、文件名与摘要

### 8.4 SettingsTab 中的数据治理体验

设置页当前已经落地：

- 导出前校验与二次确认
- 一键导出到默认备份目录
- 导入结果摘要卡片
- 最近备份历史列表
- 打开备份文件 / 打开所在目录
- 清空备份历史
- 导入后根据缺失图片情况给出修复建议

### 8.5 备份历史

默认保存位置：

```text
%LocalAppData%\ClosetApp\backups\backup-history.json
```

历史最多保留 24 条，UI 默认读取最近 8 条。

---

## 9. 图片存储与修复

### 9.1 目录

由 `AppPaths` 统一管理：

```text
%LocalAppData%\ClosetApp\
├── closet.db
├── images/
├── thumbnails/
├── logs/
└── backups/
```

### 9.2 图片解析

图片链路的关键组件：

- `ImageStorageService`：保存、删除、恢复图片
- `ImageAssetResolver`：统一判断图片是否存在并给出解析结果
- `ImagePathConverter`：UI 图片路径转换
- `ClothingImageLoader`：UI 端图片加载辅助

### 9.3 图片修复

`ImageMaintenanceService` 提供：

```csharp
Task<int> CountMissingImagesAsync();
Task<int> RelinkMissingImagesAsync(string sourceDirectory);
```

修复策略：

1. 扫描数据库中的衣物图片路径
2. 找出失效路径
3. 在用户选择的目录中按文件名匹配
4. 找到后重新保存到应用图片目录，并更新数据库路径

---

## 10. 主题与设计系统

按照当前约定：

- 设计 token 在 `ClosetApp.UI/Themes/Tokens`
- 控件样式在 `ClosetApp.UI/Themes/Controls`
- `Themes/Colors.xaml` 为兼容转发层

现有资源包括：

- `Tokens/Colors.xaml`
- `Tokens/Spacing.xaml`
- `Tokens/Radius.xaml`
- `Tokens/Sizes.xaml`
- `Tokens/Shadows.xaml`
- `Tokens/Motion.xaml`
- `Tokens/Typography.xaml`

控件样式包括：

- `Controls/Buttons.xaml`
- `Controls/Cards.xaml`
- `Controls/Inputs.xaml`
- `Controls/Chips.xaml`
- `Controls/Pages.xaml`

---

## 11. DI 与启动流程

### 11.1 依赖注册

在 `ClosetApp.UI/App.xaml.cs` 中注册：

- `AddDbContextFactory<ClosetDbContext>()`
- 仓储：`IClothingRepository`、`IOutfitRepository`、`ITagRepository`、`IFavoriteRepository`、`IOutfitWornRecordRepository`
- 服务：衣物 / 搭配 / 标签 / 推荐 / 备份 / 图片治理 / 图片存储 / 天气
- UseCase：衣柜概览、穿着记录、标签选择、历史摘要
- UI 服务：`ToastService`、`ModalService`

### 11.2 启动行为

启动流程大致为：

1. 初始化 Serilog 日志目录与文件输出
2. 注册全局异常处理
3. 构建 DI 容器
4. `EnsureCreated()` 初始化 SQLite 数据库
5. 打开主窗口

---

## 12. 测试与验证

### 12.1 测试工程结构

`ClosetApp.Tests` 当前是纯逻辑测试工程：

- 直接引用 `ClosetApp.Infrastructure`
- 按需链接 `ClosetApp.UI` 中的纯逻辑源码文件
- 不直接引用整个 `ClosetApp.UI.csproj`

这样可以避免：

- WPF 生成链干扰测试
- UI 资源编译导致测试变慢或易碎

### 12.2 当前覆盖范围

- `BackupServiceTests`
- `ImageMaintenanceServiceTests`
- `ImageStorageServiceTests`
- `OutfitCompositionEngineTests`
- `ClothesTabStateTests`
- `TabStateTests`

### 12.3 常用命令

仓库约定命令优先走 `rtk`：

```powershell
rtk dotnet build ClosetApp.slnx /m:1
rtk dotnet run --project ClosetApp.UI
rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1
```

---

## 13. 文件路径速查

| 功能 | 路径 |
|---|---|
| 应用入口 | `ClosetApp.UI/App.xaml.cs` |
| 主窗口 | `ClosetApp.UI/MainWindow.xaml` |
| 衣柜页 | `ClosetApp.UI/Views/ClothesTab.xaml` |
| 搭配页 | `ClosetApp.UI/Views/OutfitsTab.xaml` |
| 标签页 | `ClosetApp.UI/Views/TagsTab.xaml` |
| 设置页 | `ClosetApp.UI/Views/SettingsTab.xaml` |
| 衣物编辑器 | `ClosetApp.UI/Components/Clothing/ClothingEditorPanel.xaml` |
| 搭配编辑器 | `ClosetApp.UI/Components/Outfit/Editor/OutfitEditorPanel.xaml` |
| 搭配布局引擎 | `ClosetApp.UI/Components/Outfit/Engine/OutfitCompositionEngine.cs` |
| 页面状态类 | `ClosetApp.UI/States/` |
| 备份接口 | `ClosetApp.Application/Interfaces/IBackupService.cs` |
| 备份 DTO | `ClosetApp.Application/DTOs/BackupDtos.cs` |
| 备份实现 | `ClosetApp.Infrastructure/Services/BackupService.cs` |
| 图片修复 | `ClosetApp.Infrastructure/Services/ImageMaintenanceService.cs` |
| 本地路径定义 | `ClosetApp.Infrastructure/AppPaths.cs` |
| 测试工程 | `ClosetApp.Tests/ClosetApp.Tests.csproj` |
| 架构约定 | `docs/ARCHITECTURE_CONVENTIONS.md` |

---

## 14. 已知说明

### 14.1 当前保留项

- `WeatherService` 仍为 stub，暂不接真实 API
- `ViewModels` 仍存在，但不是当前页面交互的唯一主轴
- 仓库里保留 `_Archive` / `_Deprecated` 目录作为历史备份

### 14.2 风险与后续方向

- SixLabors.ImageSharp 版本告警仍需后续评估
- 继续减少 code-behind 里的非 UI 逻辑
- 导入导出能力已经可用，后续可补更细粒度的冲突策略或预览能力

---

## 15. 近期变更摘要

### 2026-05 中旬

- 完成 `SettingsTab` 数据治理体验增强
- 备份从纯 JSON 升级为 ZIP + JSON 双格式
- 增加导出前校验、导入结果摘要、备份历史
- 增加缺失图片检测与目录重连修复
- 引入 `States/` 页面轻状态类结构
- 应用层新增 `UseCases/`
- 测试工程与整个 WPF UI 工程解耦，逻辑测试可独立运行

---

## 16. 相关文档

- `README.md`：项目快速入口
- `docs/ARCHITECTURE_CONVENTIONS.md`：架构约定
- `AGENTS.md`：仓库协作与命令执行规范
