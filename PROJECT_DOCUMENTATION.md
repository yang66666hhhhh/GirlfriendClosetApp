# GirlfriendClosetApp 项目文档

> 最后更新时间：2026-05-29
> 当前状态：主流程可用，近期重点已转向推荐调试、数据洞察、性能优化与本地数据安全体验

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
| 图片处理 | SixLabors.ImageSharp | 原图保存、主视觉缓存与小预览缓存处理 |
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
│   ├── Enums/                    # ClothingType, Season, OutfitScene, TagCategory, AppThemeKind
│   ├── Interfaces/               # 仓储接口
│   └── Clothing/                 # GarmentType, DisplayCategory, LayerRole, ClothingMappings, ClothingTaxonomy
├── ClosetApp.Application/
│   ├── DTOs/                     # Outfit DTO、Backup DTO、BatchImport DTO...
│   ├── Interfaces/               # 服务接口（含 IFavoriteService, IWeatherPreferencesService）
│   ├── Services/                 # ClothingService, OutfitService, TagService, FavoriteService...
│   ├── UseCases/                 # GetWardrobeOverview, ImportClothesFromImages, RecordOutfitWorn...
│   └── Images/                   # 图片资产解析抽象（IImageAssetResolver, ImageAsset, ImageVariant）
├── ClosetApp.Infrastructure/
│   ├── Data/                     # ClosetDbContext, DesignTimeDbContextFactory, ClosetDatabaseInitializer
│   ├── Repositories/             # 仓储实现
│   ├── Services/                 # BackupService, ImageStorageService, WeatherService, RecommendationPreferencesService...
│   └── Migrations/               # EF Core 迁移
├── ClosetApp.UI/
│   ├── Views/                    # ClothesTab, OutfitsTab, TagsTab, SettingsTab, NavigationSidebar
│   ├── Components/               # 服饰卡片、搭配引擎、批量导入、共享弹层、标签组件
│   ├── States/                   # Tab 页面轻状态类
│   ├── Themes/                   # Tokens / Controls / 兼容资源
│   ├── Services/                 # ModalService, ToastService, ThemeService, WardrobeActionErrorPresenter...
│   └── ViewModels/               # 仍保留的 VM
├── ClosetApp.UI.Logic/           # UI 纯逻辑共享工程（供测试引用）
├── ClosetApp.Tests/              # 纯逻辑测试工程（xUnit）
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

- `ClothingType`: `Unspecified`, `Top`, `Bottom`, `Outerwear`, `Dress`, `Skirt`, `Shoes`, `Accessory`
- `Season`: `Unspecified`, `Spring`, `Summer`, `Autumn`, `Winter`, `AllSeason`
- `OutfitScene`: `Work`, `Date`, `Travel`, `Party`, `Casual`
- `TagCategory`: `Style`, `Scene`, `Season` — 用于标签选择与复用
- `RecommendationRotationStrategy`: `Balanced`, `PreferLessWorn`, `PreferFavorites` — 推荐轮换策略
- `AppThemeKind`: `Rose`, `Blue` — 应用主题（位于 `ClosetApp.UI/Services/`）

### 4.3 衣物分类体系

`ClosetApp.Domain/Clothing/` 定义了精细的衣物分类模型，与 `ClothingType` 枚举共存：

| 类型 | 说明 | 示例 |
|------|------|------|
| `GarmentType` | 细粒度衣物类型（27 种） | TShirt, Shirt, Blouse, Knitwear, Hoodie, Jacket, Coat, Jeans, Dress, Sneakers, Bag... |
| `DisplayCategory` | 展示分类 | Topwear, Bottom, Dress, Footwear, Accessory |
| `LayerRole` | 穿搭层级 | BaseTop, MidLayer, OuterLayer, Bottom, FullBody, Footwear, Accessory |

映射关系：

- `ClothingMappings`：GarmentType → DisplayCategory / LayerRole / 中文名称，以及 ClothingType → GarmentType 旧版推断
- `ClothingTaxonomy`：按 DisplayCategory 分组查询 GarmentType，提供分类标签和查询方法

GarmentType 与 ClothingType 的关系：`GarmentType` 是更细的分类，`ClothingType` 是旧版兼容枚举。`ClothingMappings.InferGarmentType(ClothingType)` 可从旧枚举推断 GarmentType。

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
- 批量导入图片并在导入前提示同名/同尺寸图片风险
- 依赖 `ClothesTabState` 维护页面状态

#### OutfitsTab

- 展示搭配列表
- 创建 / 编辑 / 删除搭配
- 记录穿着行为
- 根据天气、季节、收藏、最近穿着、穿着频次、场景、标签、颜色偏好和手动推荐偏好给出今日推荐
- 推荐不足时提示缺少的季节或搭配整理缺口
- 使用 `OutfitEditorPanel` 与 `OutfitsTabState`

#### TagsTab

- 标签按风格 / 场景 / 季节分组整理
- 支持名称搜索、分类筛选与按使用频次排序
- 展示标签当前关联的衣物数量，用于区分“已在使用”和“待整理”
- 标签卡片操作收纳为右上角轻量菜单，避免底部按钮影响信息密度
- 标签编辑器与可选择标签组件复用
- 依赖 `TagsTabState`

#### SettingsTab

当前是本轮重点页面，负责：

- 数据目录展示
- 日志与图片缓存清理
- 主视觉 / 小预览缓存缺失统计与一键重建
- 备份导出 / 导入
- 导出前校验与图片覆盖展示
- 导入结果摘要卡片
- 备份历史展示
- 缺失图片检测与修复入口
- 天气城市和今日推荐偏好设置

今日推荐偏好当前支持：

- 默认场景：不限 / 通勤 / 约会 / 出游 / 派对 / 休闲
- 避开今天已穿过：过滤当天已经记录穿过的搭配
- 轮换策略：
  - `Balanced` / 均衡推荐：保持推荐服务原始排序
  - `PreferLessWorn` / 优先少穿：优先穿着次数更少的搭配
  - `PreferFavorites` / 优先收藏：收藏搭配优先

### 5.3 状态类约定

见 `docs/ARCHITECTURE_CONVENTIONS.md`：

- 页面轻状态放在 `ClosetApp.UI/States`
- State 负责搜索文本、筛选器、加载标记、当前集合与空状态
- 当页面存在分组视图时，State 也负责分组集合、汇总计数和筛选摘要
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
- `Components/Clothing/BatchClothingImportPanel`
- `Components/Clothing/BatchClothingCompletionPanel`
- `Components/Clothing/BatchClothingImportSummaryDialog`
- `Components/Clothing/BatchWardrobeClearPanel`
- `PremiumClothingCard`

#### Outfit

- `Components/Outfit/Engine/OutfitCompositionEngine`
- `Components/Outfit/Controls/OutfitPreviewCanvas`
- `Components/Outfit/Controls/OutfitCard`
- `Components/Outfit/Editor/OutfitEditorPanel`

### 6.3 搭配预览模型

搭配预览采用“人体区域 + 穿搭层级”模型，而不是按衣物分类从上到下堆叠。

当前视觉结构：

- 上半身区域：外套为外层主图，上衣或中层衣物作为内层露出
- 下半身区域：裤装或半裙共用下身位，二选一
- 脚部区域：鞋子位于底部
- 配饰区域：以角标或侧边小卡展示，不参与主轴高度

选择规则与视觉规则分开：

- 外套不占上衣位，可与上衣/中层同时选择
- 连衣裙与上衣、裤装、半裙互斥，但可搭外套
- 裤装与半裙共用下身位
- 鞋子单选，配饰可多选
- 待分类衣物不参与搭配选择

这样做的目标是让搭配卡片表达“穿在人身上的关系”，而不是暴露后台分类结构。

#### Shared / Tags

- `Components/Shared/Modal/*`
- `Components/Shared/Editor/*`
- `Components/Shared/Form/*`
- `Components/Shared/States/EmptyState`
- `Components/Shared/ThemeColorHelper`
- `Components/Shared/EnumRadioGroup` — 泛型 RadioButton 选择组，含 `IEnumRadioGroup` 接口
- `Components/Shared/ThemeCard` — 主题选择卡片自定义控件
- `Components/Shared/FileSizeFormatter` — 文件大小格式化工具
- `Components/Shared/AnimationHelper` — 可复用动画工具（Shake）
- `Components/Tags/Controls/TagEditorPanel`
- `Components/Tags/Controls/TagSelectionSection`
- `Components/Tags/Models/SelectableTag`

### 6.3 批量导入工作流

批量导入允许用户从本地图片目录快速导入衣物到衣柜：

1. 用户选择图片目录 → `BatchClothingImportBuilder` 扫描图片文件
2. `BatchImportDuplicateChecker` 检测同名/同尺寸图片风险，标记可疑重复项
3. `BatchClothingImportPanel` 展示导入预览，支持一键移除可疑项
4. 用户确认后调用 `ImportClothesFromImages` UseCase 执行导入
5. `BatchClothingImportSummaryBuilder` 构建导入结果摘要
6. `BatchClothingImportSummaryDialog` 展示导入结果

相关 DTO：

- `BatchClothingImportDtos`：导入预览项、导入选项
- `BatchClothingCompletionDtos`：批量补全元数据
- `BatchWardrobeClearDtos`：按分类批量清空

### 6.4 错误提示统一处理

`WardrobeActionErrorPresenter` 集中处理以下场景的异常分类与中文提示：

- 导入失败：校验失败、数据库忙、文件占用、权限不足
- 批量补全失败：校验失败、数据库忙
- 批量清空失败：校验失败、数据库忙、文件占用、权限不足
- 单件删除失败：校验失败、数据库忙、文件占用
- 编辑面板初始化失败：数据库忙
- 图片加载失败：文件占用、权限不足
- 保存失败：校验失败、数据库忙、文件占用、权限不足
- 搭配删除/记录穿着失败、标签删除失败

### 6.4 标签页状态与交互约定

`TagsTab` 当前采用“View + ViewModel + State”的轻组合：

- `TagsViewModel` 负责把 `ITagService` 返回的数据映射到页面可绑定属性
- `TagsTabState` 负责标签搜索、分类筛选、排序、分组集合和汇总文案
- `TagRepository` 查询标签列表时会一并加载 `ClothingTags`，用于计算每个标签的当前使用次数
- `TagsTab.xaml.cs` 主要保留分类切换、排序切换、清空筛选和卡片菜单事件

标签页当前交互目标：

- 先快速看见标签库总量、已用数量和待整理数量
- 再按名称 / 分类缩小范围
- 最后在分组卡片里完成编辑或删除，而不打断浏览节奏

---

## 7. 应用层服务与 UseCase

### 7.1 核心服务

已在 `App.xaml.cs` 中注册：

- `IClothingService`
- `IOutfitService`
- `ITagService`
- `IFavoriteService`
- `IOutfitRecommendationService`
- `IBackupService`
- `IImageMaintenanceService`
- `IImageStorageService`
- `IImageAssetResolver`
- `IWeatherService`
- `IWeatherPreferencesService`
- `IRecommendationPreferencesService`

### 7.2 UseCase 目录

新的业务流程优先放在 `ClosetApp.Application/UseCases`：

- `Clothing/GetWardrobeOverview`
- `Clothing/ImportClothesFromImages`
- `Clothing/CompleteClothingMetadataBatch`
- `Clothing/ClearWardrobeByTypes`
- `Insights/GetOutfitHistorySummary`
- `Outfits/GetRecommendationReadinessSummary`
- `Outfits/GetTodayRecommendations`
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

### 8.4 其他 DTO

| 文件 | 用途 |
|------|------|
| `CreateOutfitDto` / `UpdateOutfitDto` / `OutfitDto` / `OutfitSummaryDto` | 搭配 CRUD |
| `RecommendedOutfitDto` / `RecommendationReadinessSummaryDto` | 推荐相关 |
| `TodayRecommendationResult` / `TodayRecommendationRequest` | 今日推荐编排结果与请求 |
| `ImageMaintenanceDtos` | 图片维护 |
| `BatchClothingImportDtos` | 批量导入预览与选项 |
| `BatchClothingCompletionDtos` | 批量补全元数据 |
| `BatchWardrobeClearDtos` | 按分类批量清空 |

### 8.4 SettingsTab 中的数据治理体验

设置页当前已经落地：

- 导出前校验与二次确认
- 导出前数据规模、图片覆盖与风险提醒
- 一键导出到默认备份目录
- 导入结果摘要卡片
- 最近备份历史列表
- 打开备份文件 / 打开所在目录
- 清空备份历史
- 图片缓存健康状态展示与缺失缓存重建
- 孤儿原图扫描与确认清理
- 导入后根据缺失图片情况给出修复建议

### 8.5 备份历史

默认保存位置：

```text
%LocalAppData%\ClosetApp\backups\backup-history.json
```

历史最多保留 24 条，UI 默认读取最近 8 条。

### 8.6 今日推荐偏好

推荐偏好由 `ClosetApp.Infrastructure/Services/RecommendationPreferencesService.cs` 管理，默认保存到：

```text
%LocalAppData%\ClosetApp\recommendation-settings.json
```

当前模型：

```csharp
public class RecommendationPreferences
{
    public OutfitScene? DefaultScene { get; set; }
    public bool AvoidWornToday { get; set; } = true;
    public RecommendationRotationStrategy RotationStrategy { get; set; } = RecommendationRotationStrategy.Balanced;
}
```

`OutfitsViewModel.RefreshWeatherRecommendationsAsync()` 会：

1. 读取天气城市偏好和推荐偏好
2. 调用 `IWeatherService.GetCurrentWeatherAsync()` 获取天气，失败时使用 `GetFallbackTemperature()` 按季节推算
3. 构造 `TodayRecommendationRequest` 并调用 `GetTodayRecommendations.ExecuteAsync()`
4. UseCase 内部：调用 `IOutfitRecommendationService.GetRecommendationsByRuleAsync()`，过滤当天已穿过的，按 `RotationStrategy` 排序，取前 3 套
5. UseCase 返回 `TodayRecommendationResult`（含天气、推荐、准备度、状态文本）
6. ViewModel 更新 UI 状态

---

## 9. 图片存储与修复

### 9.1 目录

由 `AppPaths` 统一管理：

```text
%LocalAppData%\ClosetApp\
├── closet.db
├── images/
│   ├── originals/
│   ├── display/
│   └── thumbnails/
├── logs/
└── backups/
```

### 9.2 图片解析

图片链路的关键组件：

- `ImageStorageService`：保存、删除、恢复图片
- `ImageAssetResolver`：统一判断图片是否存在并给出解析结果
- `ImagePathConverter`：UI 图片路径转换
- `ClothingImageLoader`：UI 端图片加载辅助

图片资产按用途分为三类：

- `Original`：原始资产，编辑器、备份和图片修复使用，保存时不压缩覆盖
- `Display`：衣柜瀑布流、搭配卡片、穿搭预览等主视觉使用，默认最大边约 900px
- `Thumbnail`：小型选择卡、摘要列表等轻量入口使用，默认最大边约 200px

当前图片解析只面向三层资产目录；历史旧目录兼容已移除，缺图时通过“图片修复”按文件名从用户选择的目录重新导入。

原图不会随普通缓存清理删除；只有在删除衣物、更换图片或用户确认“孤儿原图清理”时，才会删除数据库未引用的原图及其同名派生缓存。

### 9.3 图片修复与维护

`ImageMaintenanceService` 提供：

```csharp
Task<int> CountMissingImagesAsync();
Task<int> CountMissingThumbnailsAsync();
Task<ThumbnailRebuildResult> RebuildMissingThumbnailsAsync(int maxSize = 200);
Task<int> RelinkMissingImagesAsync(string sourceDirectory);
Task<OrphanOriginalsResult> AnalyzeOrphanOriginalsAsync();
Task<OrphanOriginalsCleanupResult> CleanupOrphanOriginalsAsync();
Task CleanupLogsAsync();
Task CleanupImageCacheAsync();
Task<int> CountFilesAsync(string directory);
Task<long> GetDirectorySizeAsync(string directory);
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

主题通过 `ThemeService` 全局切换，支持 `Rose`（柔粉）和 `Blue`（清蓝）两套主题。切换时通过 `ThemePalette.Create(AppThemeKind)` 生成完整调色板，然后更新 `Application.Resources` 中所有 Color/Brush。

### 10.1 主题调色板

每套主题包含以下色系：

| 色系 | 用途 |
|------|------|
| Primary / PrimaryDark / PrimaryLight / PrimaryGlow | 主色调及变体 |
| Surface.Page / Card / Hero / Section / Elevated / ImageArea / Modal | 表面色 |
| Border.Light / Divider | 边框色 |
| Sidebar.Background / SidebarBorder | 侧边栏 |
| Shadow.Color | 阴影色 |
| Theme.Sky.* | 天空蓝辅助色 |
| Theme.Mint.* | 薄荷绿辅助色 |
| Theme.Rose.* | 玫瑰粉辅助色 |
| Theme.Amber.* | 琥珀辅助色 |
| Theme.Lavender.* | 薰衣草辅助色 |

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
- 服务：衣物 / 搭配 / 标签 / 收藏 / 推荐 / 备份 / 图片治理 / 图片存储 / 天气 / 天气偏好 / 推荐偏好
- UseCase：`GetWardrobeOverview`、`ImportClothesFromImages`、`CompleteClothingMetadataBatch`、`ClearWardrobeByTypes`、`GetOutfitHistorySummary`、`RecordOutfitWorn`、`GetRecommendationReadinessSummary`、`GetTodayRecommendations`、`GetTagsForSelection`
- UI 服务：`ToastService`、`ModalService`、`ThemeService`、`ThemePreferencesService`

### 11.2 启动行为

启动流程大致为：

1. 初始化 Serilog 日志目录与文件输出
2. 注册全局异常处理（AppDomain / Dispatcher / TaskScheduler）
3. 构建 DI 容器
4. `ThemeService.InitializeAsync()` 加载保存的主题偏好
5. `ClosetDatabaseInitializer.InitializeAsync()` 初始化 SQLite 数据库（含迁移链）
6. 打开主窗口

---

## 12. 测试与验证

### 12.1 测试工程结构

`ClosetApp.Tests` 当前是纯逻辑测试工程：

- 直接引用 `ClosetApp.Infrastructure`
- 通过 `ClosetApp.UI.Logic` 间接引用 UI 纯逻辑源码文件
- `ClosetApp.UI.Logic` 通过 `<Compile Include>` 链接 UI 中的 State、Engine、Import 等文件
- 不直接引用整个 `ClosetApp.UI.csproj`

这样可以避免：

- WPF 生成链干扰测试
- UI 资源编译导致测试变慢或易碎

### 12.2 当前覆盖范围

| 领域 | 测试文件 |
|------|---------|
| 备份 | `BackupServiceTests` |
| 图片治理 | `ImageMaintenanceServiceTests`、`ImageStorageServiceTests` |
| 搭配引擎 | `OutfitCompositionEngineTests`、`OutfitSelectionRulesTests` |
| 批量导入 | `BatchClothingImportBuilderTests`、`BatchClothingImportSummaryBuilderTests`、`BatchImportDuplicateCheckerTests`、`ImportClothesFromImagesTests`、`CompleteClothingMetadataBatchTests` |
| 批量清空 | `ClearWardrobeByTypesTests` |
| 页面状态 | `ClothesTabStateTests`、`OutfitsTabStateTests`、`TagsTabStateTests`、`TabStateTests` |
| ViewModel | `OutfitsViewModelTests`、`SettingsViewModelTests` |
| 推荐 | `OutfitRecommendationServiceTests`、`RecommendationPreferencesServiceTests`、`RecommendationReadinessSummaryTests` |
| 天气 | `WeatherServiceTests`、`WeatherPreferencesServiceTests` |
| 数据层 | `ClothingRepositoryTests`、`DatabaseLifecycleTests` |
| 搭配服务 | `OutfitServiceTests` |
| 错误提示 | `WardrobeActionErrorPresenterTests` |

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
| 批量导入面板 | `ClosetApp.UI/Components/Clothing/BatchClothingImportPanel.xaml` |
| 批量补全面板 | `ClosetApp.UI/Components/Clothing/BatchClothingCompletionPanel.xaml` |
| 批量清空面板 | `ClosetApp.UI/Components/Clothing/BatchWardrobeClearPanel.xaml` |
| 搭配编辑器 | `ClosetApp.UI/Components/Outfit/Editor/OutfitEditorPanel.xaml` |
| 搭配布局引擎 | `ClosetApp.UI/Components/Outfit/Engine/OutfitCompositionEngine.cs` |
| 标签编辑器 | `ClosetApp.UI/Components/Tags/Controls/TagEditorPanel.xaml` |
| 标签选择组件 | `ClosetApp.UI/Components/Tags/Controls/TagSelectionSection.xaml` |
| 穿着历史弹窗 | `ClosetApp.UI/Components/Shared/Modal/OutfitHistoryDialog.xaml` |
| 推荐详情弹窗 | `ClosetApp.UI/Components/Shared/Modal/RecommendationDebugDialog.xaml` |
| 数据洞察弹窗 | `ClosetApp.UI/Components/Shared/Modal/WardrobeInsightsDialog.xaml` |
| 确认弹窗 | `ClosetApp.UI/Components/Shared/Modal/ConfirmDialog.xaml` |
| 错误提示器 | `ClosetApp.UI/Services/WardrobeActionErrorPresenter.cs` |
| 页面状态类 | `ClosetApp.UI/States/` |
| UI 逻辑共享工程 | `ClosetApp.UI.Logic/ClosetApp.UI.Logic.csproj` |
| 泛型 RadioButton 选择组 | `ClosetApp.UI/Components/Shared/EnumRadioGroup.cs` |
| 主题选择卡片控件 | `ClosetApp.UI/Components/Shared/ThemeCard.xaml` |
| 文件大小格式化 | `ClosetApp.UI/Components/Shared/FileSizeFormatter.cs` |
| 动画工具 | `ClosetApp.UI/Components/Shared/AnimationHelper.cs` |
| 今日推荐 UseCase | `ClosetApp.Application/UseCases/Outfits/GetTodayRecommendations.cs` |
| 今日推荐结果 DTO | `ClosetApp.Application/DTOs/TodayRecommendationResult.cs` |
| 推荐调试 DTO | `ClosetApp.Application/DTOs/RecommendationDebugDto.cs` |
| 数据洞察 DTO | `ClosetApp.Application/DTOs/WardrobeInsightsDto.cs` |
| 推荐轮换策略枚举 | `ClosetApp.Domain/Enums/RecommendationRotationStrategy.cs` |
| 备份接口 | `ClosetApp.Application/Interfaces/IBackupService.cs` |
| 备份 DTO | `ClosetApp.Application/DTOs/BackupDtos.cs` |
| 备份实现 | `ClosetApp.Infrastructure/Services/BackupService.cs` |
| 图片修复 | `ClosetApp.Infrastructure/Services/ImageMaintenanceService.cs` |
| 数据洞察 UseCase | `ClosetApp.Application/UseCases/Insights/GetWardrobeInsights.cs` |
| 本地路径定义 | `ClosetApp.Infrastructure/AppPaths.cs` |
| 测试工程 | `ClosetApp.Tests/ClosetApp.Tests.csproj` |
| 架构约定 | `docs/ARCHITECTURE_CONVENTIONS.md` |

---

## 14. 已知说明

### 14.1 当前保留项

- `WeatherService` 已完整实现（Open-Meteo API，支持城市搜索、15 分钟缓存、天气代码映射）
- `ViewModels` 仍存在，但不是当前页面交互的唯一主轴
- `ClosetApp.UI.Logic` 是纯逻辑共享工程，通过 `<Compile Include>` 引用 UI 中的 State、Engine、Import 等文件，供测试工程独立引用
- `WardrobeActionErrorPresenter` 统一处理数据库忙/文件占用/权限不足等异常的中文提示

### 14.2 风险与后续方向

- SixLabors.ImageSharp 版本告警仍需后续评估
- 继续减少 code-behind 里的非 UI 逻辑
- 批量导入已具备导入前重复风险提示，后续可补失败回滚的更细粒度 UI 展示
- MasonryPanel 虚拟化支持（当前不支持虚拟化，大量卡片时内存开销较大）

---

## 15. 近期变更摘要

### 2026-05 中旬

- 增加天气驱动的今日穿搭推荐，支持推荐理由、准备度诊断和一键记录"穿了"
- 今日推荐增加场景、标签和颜色偏好权重，会从穿着历史与收藏中自动推断常用偏好
- 批量导入增加同名/同尺寸图片风险原因提示，并支持一键移除可疑重复项
- 完成 `SettingsTab` 数据治理体验增强
- 增加衣柜批量导入，默认名称为"未命名"，未设置字段保持空值或待整理状态
- 衣柜分类补齐半裙，并将外套、半裙从上衣/裤装大类中拆出精确筛选
- 搭配预览升级为"人体区域 + 穿搭层级"模型，外套和上衣在同一上半身区域表达层级关系
- 图片资产升级为 `Original / Display / Thumbnail` 三层，衣柜主瀑布流改用 Display 主视觉缓存
- 设置页增加孤儿原图扫描与确认清理，避免原图资产无限增长
- 备份从纯 JSON 升级为 ZIP + JSON 双格式
- 增加导出前校验、导入结果摘要、备份历史
- 增加缺失图片检测与目录重连修复
- 引入 `States/` 页面轻状态类结构
- 应用层新增 `UseCases/`
- 测试工程通过 `ClosetApp.UI.Logic` 间接引用 UI 纯逻辑文件，避免 WPF 生成链干扰
- 引入 `GarmentType` / `DisplayCategory` / `LayerRole` 精细衣物分类体系
- 引入 `WardrobeActionErrorPresenter` 统一错误提示
- 引入 `ThemePreferencesService` 和 `WeatherPreferencesService` 持久化偏好
- 引入 `TagEditorPanel`、`TagSelectionSection`、`SelectableTag` 标签组件
- 引入 `ConfirmDialog`、`OutfitHistoryDialog`、`WornDayDetailsDialog` 共享弹窗
- 主题调色板扩展为 5 套辅助色系（Sky / Mint / Rose / Amber / Lavender）

### 2026-05-28

- 提取 `GetTodayRecommendations` UseCase，`OutfitsViewModel` 天气推荐编排逻辑独立为可复用 UseCase
- `RecommendationRotationStrategy` 枚举从 Infrastructure 迁移到 Domain 层，解除 Application → Infrastructure 依赖
- `IWeatherService` 新增 `GetFallbackTemperature()`，季节温度推算逻辑集中管理
- `IImageStorageService` 新增 `TryDeleteImageAsync()`，安全删除模式（忽略空路径和异常）
- `IImageMaintenanceService` 新增 `CleanupLogsAsync()`、`CleanupImageCacheAsync()`、`CountFilesAsync()`、`GetDirectorySizeAsync()`
- `SettingsTab` 重构：新建 `ThemeCard` 自定义控件驱动主题选择视觉状态，文件操作迁移到 Service 层
- `WardrobeViewModel` 重构：引入 `EnumRadioGroup<TEnum>` 泛型 RadioButton 选择组，减少 ~160 行样板代码
- `ClothingEditorPanel` 重构：`Save_Click` 提取 `BuildClothingFromFormAsync` + `ApplyTagChanges`，`ShakeElement` 迁移到 `AnimationHelper`
- 新建共享组件：`EnumRadioGroup`、`ThemeCard`、`FileSizeFormatter`、`AnimationHelper`
- 搭配编辑器名称字段改为选填，留空自动命名为"未命名"

### 2026-05-29

#### 新功能

- **推荐调试视图**：点击推荐搭配的"详情"按钮，查看完整评分分解
  - 新增 `RecommendationDebugDto`：包含总分、各维度分数明细、偏好权重
  - 新增 `IOutfitRecommendationService.GetRecommendationDebugAsync()` / `GetRecommendationDebugForOutfitAsync()`
  - 新增 `RecommendationDebugDialog` 弹窗：展示评分明细、推荐理由、偏好权重分布
  - Hero 区域和 secondary 推荐卡片均支持查看推荐详情

- **数据洞察**：点击"数据洞察"按钮，查看衣柜使用统计
  - 新增 `WardrobeInsightsDto`：总览、Top5、场景/季节分布、闲置预警
  - 新增 `GetWardrobeInsights` UseCase：计算穿着率、活跃天数、连续记录天数
  - 新增 `WardrobeInsightsDialog` 弹窗：可视化展示统计数据
  - 包含缓存机制，避免重复计算

#### 性能优化

- **推荐详情缓存**：缓存推荐调试结果，避免每次打开详情重新计算
- **数据洞察缓存**：缓存统计结果，数据变化时自动清除
- **日历按需加载**：初始加载不再获取日历数据，打开"查看记录"弹窗时才加载
- **图片懒加载**：
  - `PremiumClothingCard` 添加可见性检测，卡片可见时才加载图片
  - `OutfitPreviewCanvas` 添加懒加载支持，不可见时跳过渲染
- **推荐详情加载优化**：直接使用 ViewModel 已有的天气数据，避免重复网络请求

#### Bug 修复

- 修复 `OutfitPreviewCanvas.Render()` 在 Popup 内部调用 `UpdateLayout()` 导致的 `NullReferenceException`

#### 代码清理

- 删除 `ClosetApp.UI/Converters/_Archive/` 目录（9 个废弃的 Converter 文件）
- 移除未使用的 `GetRecommendationParamsAsync()` 方法

---

## 16. 相关文档

- `README.md`：项目快速入口
- `docs/ARCHITECTURE_CONVENTIONS.md`：架构约定
- `AGENTS.md`：仓库协作与命令执行规范
