# GirlfriendClosetApp

私人数字衣橱桌面应用，面向个人衣物整理、搭配管理和本地数据治理场景。项目使用 WPF + SQLite，采用 Domain / Application / Infrastructure / UI 四层结构。

> 更新时间：2026-05-30
> 当前运行时：UI 与测试工程为 .NET 10 / WPF；Domain、Application、Infrastructure 为 .NET 8 类库

## 当前能力

- 衣柜管理：新增、编辑、删除衣物，支持图片、季节、品牌、备注、收藏状态和批量导入；衣物类型默认未选择，名称留空会自动保存为"未命名"
- 搭配管理：创建和编辑搭配，按"人体区域 + 穿搭层级"生成预览，支持穿着记录和天气驱动的今日推荐；推荐会结合季节、收藏、穿着记录、场景、标签、颜色偏好和手动推荐偏好
- 穿着记录：记录穿着时保存搭配名称、衣物数量、衣物明细和预览图快照；删除衣服或搭配后历史记录仍保留，并提示"搭配已删除 / 搭配已变化 / 快照不完整"等状态
- 推荐调试：点击推荐搭配的"详情"按钮，查看完整评分分解（季节、收藏、穿着、场景、偏好等维度）
- 数据洞察：查看衣柜使用统计，包括穿着次数、活跃天数、连续记录、最常穿 Top5、场景/季节分布、闲置预警
- 年度报告：查看当年穿搭数据总结，包括月度统计、Top5 搭配、场景/季节分布、精彩瞬间
- 标签管理：标签页展示风格 / 场景标签，季节标签由系统管理；支持搜索、分类筛选、使用状态筛选、按使用频次 / 名称 / 最近添加排序，并展示衣物使用数和搭配使用次数
- 设置中心：数据目录、日志、图片缓存、备份、导入恢复、缺失图片修复、天气城市和今日推荐偏好
- 本地数据治理：
  - ZIP 备份包：`backup.json` + `images/`
  - 兼容旧版 JSON 备份导入
  - 导出前校验、图片覆盖统计与警告提示
  - 导入结果摘要、缺失图片提示、备份历史

## 项目结构

```text
GirlfriendClosetApp/
├── ClosetApp.Domain/                 # 实体、枚举、仓储接口、衣物分类模型
├── ClosetApp.Application/            # DTO、服务接口/实现、UseCases、图片资产抽象
├── ClosetApp.Infrastructure/         # EF Core、SQLite、图片/备份/天气等基础设施
├── ClosetApp.UI/                     # WPF 页面、组件、状态类、主题资源
├── ClosetApp.UI.Logic/               # UI 纯逻辑共享工程（State、Engine、Import 等逻辑源码归属处）
├── ClosetApp.Tests/                  # xUnit 测试工程（当前同时引用 UI.Logic 与 UI 工程）
├── docs/
│   └── ARCHITECTURE_CONVENTIONS.md   # 架构约定
└── PROJECT_DOCUMENTATION.md          # 详细项目文档
```

## UI 入口

当前主界面由左侧导航 + 右侧内容区组成，包含 4 个主页面：

- `ClothesTab`：衣柜页，瀑布流卡片、搜索、分类筛选
- `OutfitsTab`：搭配页，统一编辑器、穿搭预览与记录
- `TagsTab`：标签页，维护标签数据，并按使用情况做整理与筛选
- `SettingsTab`：设置页，负责数据治理与本地文件维护

## 关键实现

### 1. 统一编辑器与状态类

- 衣物、搭配、标签编辑逐步统一为 Editor Panel 模式
- Tab 页面状态下沉到 `ClosetApp.UI.Logic/States`
- 页面 code-behind 主要负责交互、动画和 modal 编排

### 1.1 标签页整理体验

- `TagsTabState` 负责标签搜索词、分类筛选、排序方式、分组集合和汇总文案
- 标签页会过滤系统季节标签，只展示可整理的风格 / 场景标签
- 标签页会统计每个标签当前关联的衣物数量和搭配使用次数，用于显示"已在使用 / 待整理"
- 使用状态筛选支持"全部 / 正在使用 / 未使用"，排序支持"使用最多 / 名称 / 使用最少 / 最近添加"
- 标签卡片操作已收为右上角的轻量 `⋯` 菜单，避免底部操作按钮挤占信息区
- 标签卡片 hover 会提升边框、阴影和状态胶囊，用来强调当前可整理对象

### 1.2 衣物分类体系

`ClosetApp.Domain/Clothing/` 定义了精细衣物分类模型，与 `ClothingType` 枚举共存：

- `GarmentType`：细粒度衣物类型（T恤、衬衫、针织、外套、牛仔裤、靴子、包包等 27 种）
- `DisplayCategory`：展示分类（Topwear / Bottom / Dress / Footwear / Accessory）
- `LayerRole`：穿搭层级（BaseTop / MidLayer / OuterLayer / Bottom / FullBody / Footwear / Accessory）
- `ClothingMappings`：GarmentType ↔ DisplayCategory / LayerRole / 中文名称映射
- `ClothingTaxonomy`：按 DisplayCategory 分组查询 GarmentType

### 1.3 共享组件

`ClosetApp.UI/Components/Shared/` 下的可复用组件：

- `EnumRadioGroup<TEnum>`：泛型 RadioButton 选择组，将 nullable enum 映射为布尔属性，含 `IEnumRadioGroup` 非泛型接口
- `ThemeCard`：主题选择卡片自定义控件，通过 `IsSelected` 属性驱动视觉状态
- `FileSizeFormatter`：文件大小格式化工具（B/KB/MB/GB）
- `AnimationHelper`：可复用动画工具（Shake 抖动效果）
- `ThemeColorHelper`：主题感知的颜色解析和混合工具

### 1.4 穿着记录快照

- `OutfitWornRecord.OutfitId` 已改为可空，支持搭配删除后保留历史记录
- 记录穿着时保存 `OutfitNameSnapshot`、`OutfitClothingIdsSnapshot`、`ClothingCountSnapshot`、`ClothingDetailsSnapshot`、`PreviewSnapshotPath` 和 `IsSnapshotComplete`
- `Outfit.OriginalClothingCount` 用于判断搭配内容是否已变化，`OutfitCard` 会显示"搭配已变化"提示
- 删除衣服或搭配前会补齐相关穿着记录快照，历史弹窗优先使用快照展示已删除或已变化的搭配
- 历史快照引用的图片会被视为有效资产；删除衣物、批量清空和孤儿原图清理都不能物理删除这些图片
- 如果旧快照缺少细分类，历史展示会用 `Type` 和名称兜底推断半裙、裤装、鞋、包等常见单品位置
- live 搭配读取会跳过 `Clothing` 导航为空的无效链接，避免删除衣物后搭配卡片或今日推荐刷新崩溃

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

### 3. 批量导入

- `BatchClothingImportBuilder`：从图片文件列表构建导入预览项
- `BatchImportDuplicateChecker`：检测同名/同尺寸图片风险
- `BatchClothingImportSummaryBuilder`：构建导入结果摘要
- `ImportClothesFromImages`：批量导入 UseCase
- `CompleteClothingMetadataBatch`：批量补全衣物元数据 UseCase
- `ClearWardrobeByTypes`：按分类批量清空衣柜 UseCase

### 4. 图片资产体系

- `ImageStorageService`：原图 / 主视觉 / 小预览存储
- `ImageMaintenanceService`：检测缺失图片、统计图片缓存缺口并执行重建、清理日志/缓存、统计文件数量和大小
- `ImageAssetResolver`：统一图片解析

图片按视觉用途分层：

- `Original`：原始资产，编辑器和备份使用，保存时不压缩覆盖
- `Display`：衣柜瀑布流、搭配卡片、穿搭预览使用，默认最大边约 900px
- `Thumbnail`：小型选择卡、摘要列表等低成本预览使用，默认最大边约 200px

设置页可直接：

- 查看缺失图片数量
- 查看图片缓存健康状态并一键重建缺失缓存
- 扫描并清理数据库未引用的孤儿原图（衣物记录和穿着历史快照引用都会被视为有效引用）
- 查看备份前的数据规模、图片覆盖情况和导出提醒
- 选择旧图片目录批量修复
- 清理主视觉和小预览缓存

### 4.1 今日推荐偏好

设置页支持保存今日推荐偏好：

- 默认场景：不限 / 通勤 / 约会 / 出游 / 派对 / 休闲
- 避开今天已穿过：开启后天气推荐会过滤当天已经记录穿过的搭配
- 轮换策略：
  - `均衡推荐`：保持推荐服务原始排序
  - `优先少穿`：优先穿着次数更少的搭配
  - `优先收藏`：收藏搭配优先

偏好保存于：

- `%LocalAppData%\ClosetApp\recommendation-settings.json`

### 5. 应用层 UseCases

新业务流程优先放在 `ClosetApp.Application/UseCases`：

- `Clothing/GetWardrobeOverview`
- `Clothing/ImportClothesFromImages`
- `Clothing/CompleteClothingMetadataBatch`
- `Clothing/ClearWardrobeByTypes`
- `Insights/GetOutfitHistorySummary`
- `Outfits/RecordOutfitWorn`
- `Outfits/GetRecommendationReadinessSummary`
- `Outfits/GetTodayRecommendations`
- `Tags/GetTagsForSelection`

### 6. 搭配预览模型

搭配预览不再按衣物分类简单堆叠，而是按人体区域表达：

- 上半身区域：外套为外层主图，上衣/中层作为内层露出
- 下半身区域：裤装或半裙二选一
- 脚部区域：鞋子位于底部
- 配饰区域：作为角标/侧边信息展示，不参与主轴高度

当前布局算法位于 `ClosetApp.UI.Logic/Components/Outfit/Engine/OutfitCompositionEngine.cs`，渲染由 UI 工程中的 `OutfitPreviewCanvas` 完成。

### 7. 错误提示统一处理

`WardrobeActionErrorPresenter` 集中处理导入、保存、删除等操作的异常分类与中文提示：

- 数据库忙 → 提示关闭其他编辑窗口
- 文件占用 → 提示关闭看图工具或同步盘
- 权限不足 → 提示确认目录权限
- 校验失败 → 直接展示验证消息

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

- 备份：`BackupServiceTests`
- 图片治理：`ImageMaintenanceServiceTests`、`ImageStorageServiceTests`
- 搭配引擎：`OutfitCompositionEngineTests`、`OutfitSelectionRulesTests`
- 批量导入：`BatchClothingImportBuilderTests`、`BatchClothingImportSummaryBuilderTests`、`BatchImportDuplicateCheckerTests`、`ImportClothesFromImagesTests`、`CompleteClothingMetadataBatchTests`
- 批量清空：`ClearWardrobeByTypesTests`
- 页面状态：`ClothesTabStateTests`、`OutfitsTabStateTests`、`TagsTabStateTests`、`TabStateTests`
- ViewModel：`OutfitsViewModelTests`、`SettingsViewModelTests`
- 推荐：`OutfitRecommendationServiceTests`、`RecommendationPreferencesServiceTests`、`RecommendationReadinessSummaryTests`
- 天气：`WeatherServiceTests`、`WeatherPreferencesServiceTests`
- 数据层：`ClothingRepositoryTests`、`DatabaseLifecycleTests`
- 搭配服务：`OutfitServiceTests`
- 错误提示：`WardrobeActionErrorPresenterTests`
- 数据洞察：`GetWardrobeInsightsTests`
- 推荐调试：`OutfitRecommendationServiceTests`（包含 `GetRecommendationDebugAsync` 测试）

测试工程当前同时引用 `ClosetApp.UI.Logic` 与 `ClosetApp.UI`。其中 State、Engine、Import 等纯逻辑源码已归属 `ClosetApp.UI.Logic`，供 UI 与测试复用；部分 ViewModel / WPF 相关测试仍直接依赖 UI 工程。

## 当前已知说明

- `WeatherService` 已完整实现（Open-Meteo API，支持城市搜索、15 分钟缓存、天气代码映射）
- `ViewModels/` 已开始接管搭配页与设置页的业务状态，View 侧主要保留弹窗、导航、文件选择和控件事件桥接
- `Themes/Colors.xaml` 是兼容转发层，新设计 token 位于 `Themes/Tokens` 与 `Themes/Controls`
- `ClosetApp.UI.Logic` 是纯逻辑共享工程，承载 State、Engine、Import 等文件，供 UI 与测试工程直接引用复用
- `WardrobeActionErrorPresenter` 统一处理数据库忙/文件占用/权限不足等异常的中文提示
- 季节标签是系统预设，不在标签页作为普通标签展示；标签页当前只整理风格 / 场景标签
- 穿着记录依赖快照系统保留历史，删除搭配或衣物前需要先确保关联记录快照完整

## 文档入口

- 详细项目文档：[`PROJECT_DOCUMENTATION.md`](./PROJECT_DOCUMENTATION.md)
- 架构约定：[`docs/ARCHITECTURE_CONVENTIONS.md`](./docs/ARCHITECTURE_CONVENTIONS.md)
- 协作约束：[`AGENTS.md`](./AGENTS.md)
