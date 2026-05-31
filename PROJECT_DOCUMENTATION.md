# GirlfriendClosetApp 项目文档

> 最后更新时间：2026-05-30
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
| 核心类库 | .NET (`net8.0`) | Domain / Application / Infrastructure 目标框架 |
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
│   ├── Components/               # 服饰卡片、穿搭预览、编辑器、设置页子面板、共享弹层、标签组件
│   ├── Themes/                   # Tokens / Controls / 兼容资源
│   ├── Services/                 # ModalService, ToastService, ThemeService...
│   └── ViewModels/               # 仍保留的 VM
├── ClosetApp.UI.Logic/           # UI 纯逻辑共享工程（State、Engine、Import、错误提示等逻辑源码归属处）
├── ClosetApp.Tests/              # xUnit 测试工程（当前同时引用 UI.Logic 与 UI 工程）
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
- `int OriginalClothingCount`（原始衣服数量，用于判断搭配是否变化）

#### Tag

- `Guid Id`
- `string Name`
- `string Color`

#### 关联 / 衍生实体

- `ClothingTag`
- `OutfitClothing`
- `Favorite`
- `OutfitWornRecord`
  - `Guid? OutfitId`（可空，支持搭配删除后保留记录）
  - `DateTime WornDate`
  - `string OutfitNameSnapshot`（搭配名称快照）
  - `string? OutfitClothingIdsSnapshot`（衣服 ID 列表快照，JSON 格式）
  - `int ClothingCountSnapshot`（衣服数量快照）
  - `string? ClothingDetailsSnapshot`（衣服详情快照，JSON 格式）
  - `bool IsSnapshotComplete`（快照完整性标记）
  - `string? PreviewSnapshotPath`（预览图快照路径）

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

- 标签按风格 / 场景分组整理（季节标签由系统管理，不在页面显示）
- 支持名称搜索、分类筛选、使用状态筛选与多种排序方式
- 展示标签当前关联的衣物数量和搭配使用次数，用于区分"已在使用"和"待整理"
- 标签卡片操作收纳为右上角轻量菜单，避免底部按钮影响信息密度
- 标签编辑器与可选择标签组件复用
- 依赖 `TagsTabState`

#### SettingsTab

当前是本轮重点页面，负责：

- 数据目录展示
- 日志清理
- 图片资产治理由 `ImageMaintenanceSettingsPanel` 承接，包括缓存清理、缺失缓存重建、缺失图片修复、孤儿原图清理和历史图片检查
- 天气与今日推荐偏好由 `WeatherPreferencesSettingsPanel` 承接，包括城市保存、天气刷新和推荐偏好设置
- 外观与应用信息由 `AppearanceSettingsPanel` 承接，包括主题切换、版本展示和应用目录入口
- 备份与恢复由 `BackupSettingsPanel` 承接，包括备份导出 / 导入、导出前校验、导入结果摘要和备份历史展示
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

- 页面轻状态放在 `ClosetApp.UI.Logic/States`
- `ClosetApp.UI.Logic` 中的纯逻辑类型使用 `ClosetApp.UI.Logic.*` 命名空间
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
  - 默认类型为"未选择"（`ClothingType.Unspecified`）
  - 名称字段为选填，留空自动命名为"未命名"
- `Components/Clothing/BatchClothingImportPanel`
- `Components/Clothing/BatchClothingCompletionPanel`
- `Components/Clothing/BatchClothingImportSummaryDialog`
- `Components/Clothing/BatchWardrobeClearPanel`
- `PremiumClothingCard`

#### Outfit

- `Components/Outfit/Engine/OutfitCompositionEngine`
- `Components/Outfit/Controls/OutfitPreviewCanvas`
- `Components/Outfit/Controls/OutfitCard`
  - 显示搭配变化提示（原 X 件，现 Y 件）
  - 使用玫瑰色背景突出显示
- `Components/Outfit/Editor/OutfitEditorPanel`
  - 名称字段为选填，留空自动命名为"未命名"

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

- `Components/Settings/ImageMaintenanceSettingsPanel` — 设置页图片资产治理区，集中处理图片统计、缓存重建、缺图修复、历史图片检查和孤儿原图清理
- `Components/Settings/WeatherPreferencesSettingsPanel` — 设置页天气与推荐偏好区，集中处理天气城市、天气刷新和今日推荐偏好保存
- `Components/Settings/AppearanceSettingsPanel` — 设置页外观与应用信息区，集中处理主题切换、版本展示和应用目录入口
- `Components/Settings/BackupSettingsPanel` — 设置页备份与恢复区，集中处理导出、导入、备份校验、导入摘要、备份历史和文件定位
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

### 6.4 批量导入工作流

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

### 6.5 错误提示统一处理

`WardrobeActionErrorPresenter` 集中处理以下场景的异常分类与中文提示：

- 导入失败：校验失败、数据库忙、文件占用、权限不足
- 批量补全失败：校验失败、数据库忙
- 批量清空失败：校验失败、数据库忙、文件占用、权限不足
- 单件删除失败：校验失败、数据库忙、文件占用
- 编辑面板初始化失败：数据库忙
- 图片加载失败：文件占用、权限不足
- 保存失败：校验失败、数据库忙、文件占用、权限不足
- 搭配删除/记录穿着失败、标签删除失败

该提示器位于 `ClosetApp.UI.Logic/Services`，供 UI 与测试工程共用同一套异常分类规则。

### 6.6 标签页状态与交互约定

`TagsTab` 当前采用“View + ViewModel + State”的轻组合：

- `TagsViewModel` 负责把 `ITagService` 返回的数据映射到页面可绑定属性
- `TagsTabState` 负责标签搜索、分类筛选、使用状态筛选、排序、分组集合和汇总文案，并过滤系统季节标签
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

## 8. 业务规则与算法

### 8.1 穿着记录永久保留规则

穿着记录是用户历史数据，不跟随衣物或搭配删除而删除。

`OutfitWornRecord` 使用快照字段保存当时状态：

- `OutfitNameSnapshot`：记录当时的搭配名称
- `OutfitClothingIdsSnapshot`：记录当时的衣服 ID 列表
- `ClothingCountSnapshot`：记录当时的衣服数量
- `ClothingDetailsSnapshot`：记录当时的衣服明细 JSON，包含 `Id`、`Name`、`ImagePath`、`Color`、`Type`、`GarmentType`
- `IsSnapshotComplete`：标记快照是否可用于历史展示

历史展示算法：

1. `WornRecordSnapshotDisplayFactory.FromRecord()` 优先解析 `ClothingDetailsSnapshot`
2. 如果快照可用，历史名称、衣服列表和预览都使用快照
3. 当前 live `Outfit` 只用于判断状态：搭配已删除、搭配已变化、快照不完整
4. 旧快照缺少 `GarmentType` 时，先用 `Type` 推断；`Type` 也不可用时，可按名称兜底识别半裙、裤装、鞋、包、连衣裙、外套等常见类型
5. 快照图片路径失效时，历史仍保留文字和数量，但预览无法画出该单品图片；需要通过备份或缺失图片修复找回同名图片
6. 历史详情弹窗会标记缺图的快照单品，并允许为单条记录里的缺图单品重新选择图片

### 8.2 记录穿着算法

`OutfitService.RecordWornDateAsync(outfitId, date)`：

1. 读取搭配及其 `OutfitClothes -> Clothing`
2. 生成衣服 ID 快照和衣服详情快照
3. 如果当天已经记录过同一搭配，则更新该记录时间和快照
4. 否则新增 `OutfitWornRecord`
5. 更新 live `Outfit.WornDate` 和 `Outfit.WearCount`

### 8.3 删除衣服算法

删除单件衣服时，顺序很重要：

1. 读取将被删除的衣服和图片路径
2. `OutfitRepository.DeleteInvalidOutfitsAsync(clothingId)` 找出包含该衣服的搭配
3. 在移除衣服链接前，使用当前完整搭配补齐相关穿着记录快照
4. 如果旧记录被错误标记为完整，但 `ClothingDetailsSnapshot` 为空或快照数量小于当前完整搭配数量，也要刷新快照
5. 删除搭配中的衣服链接
6. 如果搭配剩余衣服少于 2 件，删除 live 搭配，并把相关 `OutfitWornRecord.OutfitId` 置空
7. 如果搭配仍有 2 件以上，保留 live 搭配，并保留 `OriginalClothingCount` 用于显示“搭配已变化”
8. 删除衣服记录
9. 如果该衣服图片仍被任意穿着历史快照引用，则保留图片资产；否则才允许删除原图和派生缓存

删除衣服后，live 搭配、推荐服务和 UI 组件都不能假设 `OutfitClothes -> Clothing` 导航对象永远非空。读取 live 搭配衣服时应先过滤无效链接：

- 搭配卡片、预览和颜色/轮廓标签只使用 `Clothing != null` 的衣服
- 今日推荐的标签偏好和颜色偏好统计只使用有效衣服
- 历史记录仍以 `ClothingDetailsSnapshot` 为准，不用 live 导航补历史内容

### 8.4 删除搭配算法

`OutfitService.DeleteOutfitAsync(outfitId)`：

1. 用可更新查询读取搭配、衣服、收藏和穿着记录
2. 删除 live 搭配前，补齐所有关联穿着记录快照
3. 对旧的空快照或数量不足快照执行刷新
4. 将关联穿着记录的 `OutfitId` 置空
5. 删除 live 搭配

删除搭配不删除穿着记录，也不应删除历史快照引用的图片资产。

### 8.5 图片保留与清理规则

图片文件不是单纯跟随衣物生命周期删除。判断规则：

- 衣物列表中仍引用的图片：保留
- 穿着历史快照中引用的图片：保留
- 删除衣物或批量清空衣柜时，如果图片被历史快照引用，跳过物理删除
- 设置页“清理孤儿原图”会把衣物引用和历史快照引用都视为有效引用
- 普通缓存清理只清理 `display/` 和 `thumbnails/`，不删除 `originals/`
- 缺失缓存可由 `ImageStorageService.EnsureDisplayAsync()` / `EnsureThumbnailAsync()` 从原图重建
- 设置页“检查历史图片”会统计穿着历史快照中的单品图片可用性，并列出缺图记录摘要；用户可从最近一条缺图记录直接打开对应日期详情
- 缺图修复只更新对应 `ClothingDetailsSnapshot` 的 `ImagePath`
- 历史快照缺图判断统一使用 `IImageAssetResolver`，确保设置页统计和历史弹窗提示一致
- 单张历史图片修复会先保存新图片，再更新快照；如果快照更新失败，UI 需要 best-effort 删除本次新保存的图片

### 8.6 批量清空算法

`ClearWardrobeByTypes.ExecuteAsync(request)`：

1. 校验至少选择一个 `ClothingType`
2. 查询命中的衣物集合
3. 批量删除衣物记录
4. 删除空搭配
5. 对每个待删图片去重
6. 如果图片被历史快照引用，则跳过删除
7. 否则 best-effort 删除原图、主视觉和缩略图

---

## 9. 备份与数据治理

### 9.1 备份接口

`IBackupService` 当前提供：

```csharp
Task<BackupValidationResult> ValidateExportAsync(string filePath);
Task<BackupExportResult> ExportAsync(string filePath);
Task<BackupImportResult> ImportAsync(string filePath);
Task<IReadOnlyList<BackupHistoryItem>> GetHistoryAsync(int maxCount = 8);
Task ClearHistoryAsync();
string BuildDefaultBackupPath();
```

### 9.2 支持格式

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

### 9.3 备份 DTO

位于 `ClosetApp.Application/DTOs/BackupDtos.cs`：

- `BackupValidationResult`
- `BackupExportResult`
- `BackupImportResult`
- `BackupHistoryItem`

其中：

- `BackupValidationResult` 提供导出前数据量和警告信息
- `BackupImportResult` 提供导入结果摘要、恢复图片数、缺失文件名、修复建议
- `BackupHistoryItem` 提供 UI 可直接展示的时间、状态、文件名与摘要

### 9.4 其他 DTO

| 文件 | 用途 |
|------|------|
| `CreateOutfitDto` / `UpdateOutfitDto` / `OutfitDto` / `OutfitSummaryDto` | 搭配 CRUD |
| `RecommendedOutfitDto` / `RecommendationReadinessSummaryDto` | 推荐相关 |
| `TodayRecommendationResult` / `TodayRecommendationRequest` | 今日推荐编排结果与请求 |
| `ImageMaintenanceDtos` | 图片维护 |
| `BatchClothingImportDtos` | 批量导入预览与选项 |
| `BatchClothingCompletionDtos` | 批量补全元数据 |
| `BatchWardrobeClearDtos` | 按分类批量清空 |

### 9.5 SettingsTab 中的数据治理体验

设置页当前已经落地：

- 导出前校验与二次确认
- 导出前数据规模、图片覆盖与风险提醒
- 一键导出到默认备份目录
- 导入结果摘要卡片
- 最近备份历史列表
- 打开备份文件 / 打开所在目录
- 清空备份历史
- 图片资产治理区已拆为 `ImageMaintenanceSettingsPanel`，`SettingsTab` 只负责刷新协调和其它设置分组
- 天气与推荐偏好区已拆为 `WeatherPreferencesSettingsPanel`，保存后通过事件通知父页刷新搭配页
- 外观与应用信息区已拆为 `AppearanceSettingsPanel`，主题切换通过事件交还父页执行
- 备份与恢复区已拆为 `BackupSettingsPanel`，导入成功后通过事件通知父页刷新衣柜、搭配和标签
- 图片缓存健康状态展示与缺失缓存重建
- 孤儿原图扫描与确认清理
- 导入后根据缺失图片情况给出修复建议

### 9.6 备份历史

默认保存位置：

```text
%LocalAppData%\ClosetApp\backups\backup-history.json
```

历史最多保留 24 条，UI 默认读取最近 8 条。

### 9.7 今日推荐偏好

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

## 10. 图片存储与修复

### 10.1 目录

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

### 10.2 图片解析

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

原图不会随普通缓存清理删除；只有在删除衣物、更换图片或用户确认“孤儿原图清理”时，才会删除数据库未引用的原图及其同名派生缓存。这里的“引用”同时包括衣物表中的 `ImagePath` 和穿着历史快照 `ClothingDetailsSnapshot` 中的 `ImagePath`。

### 10.3 图片修复与维护

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

## 11. 主题与设计系统

按照当前约定：

- 设计 token 在 `ClosetApp.UI/Themes/Tokens`
- 控件样式在 `ClosetApp.UI/Themes/Controls`
- `Themes/Colors.xaml` 为兼容转发层

主题通过 `ThemeService` 全局切换，支持 `Rose`（柔粉）和 `Blue`（清蓝）两套主题。切换时通过 `ThemePalette.Create(AppThemeKind)` 生成完整调色板，然后更新 `Application.Resources` 中所有 Color/Brush。

### 11.1 主题调色板

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

## 12. DI 与启动流程

### 12.1 依赖注册

在 `ClosetApp.UI/App.xaml.cs` 中注册：

- `AddDbContextFactory<ClosetDbContext>()`
- 仓储：`IClothingRepository`、`IOutfitRepository`、`ITagRepository`、`IFavoriteRepository`、`IOutfitWornRecordRepository`
- 服务：衣物 / 搭配 / 标签 / 收藏 / 推荐 / 备份 / 图片治理 / 图片存储 / 天气 / 天气偏好 / 推荐偏好
- UseCase：`GetWardrobeOverview`、`ImportClothesFromImages`、`CompleteClothingMetadataBatch`、`ClearWardrobeByTypes`、`GetOutfitHistorySummary`、`RecordOutfitWorn`、`GetRecommendationReadinessSummary`、`GetTodayRecommendations`、`GetTagsForSelection`
- UI 服务：`ToastService`、`ModalService`、`ThemeService`、`ThemePreferencesService`

### 12.2 启动行为

启动流程大致为：

1. 初始化 Serilog 日志目录与文件输出
2. 注册全局异常处理（AppDomain / Dispatcher / TaskScheduler）
3. 构建 DI 容器
4. `ThemeService.InitializeAsync()` 加载保存的主题偏好
5. `ClosetDatabaseInitializer.InitializeAsync()` 初始化 SQLite 数据库（含迁移链）
6. 打开主窗口

---

## 13. 测试与验证

### 13.1 测试工程结构

`ClosetApp.Tests` 当前是 xUnit 测试工程：

- 直接引用 `ClosetApp.Infrastructure`
- 直接引用 `ClosetApp.UI`
- 通过 `ClosetApp.UI.Logic` 复用 State、Engine、Import 等纯逻辑源码文件

这样可以：

- 让 State、Engine、Import 等纯逻辑代码归属在 `ClosetApp.UI.Logic`，并在 UI 与测试中共用同一份源码
- 支持 ViewModel / WPF 相关测试继续覆盖 UI 工程中的实际类型

### 13.2 当前覆盖范围

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

### 13.3 常用命令

仓库约定命令优先走 `rtk`：

```powershell
rtk dotnet build ClosetApp.slnx /m:1
rtk dotnet run --project ClosetApp.UI
rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1
```

---

## 14. 文件路径速查

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
| 搭配布局引擎 | `ClosetApp.UI.Logic/Components/Outfit/Engine/OutfitCompositionEngine.cs` |
| 标签编辑器 | `ClosetApp.UI/Components/Tags/Controls/TagEditorPanel.xaml` |
| 标签选择组件 | `ClosetApp.UI/Components/Tags/Controls/TagSelectionSection.xaml` |
| 穿着历史弹窗 | `ClosetApp.UI/Components/Shared/Modal/OutfitHistoryDialog.xaml` |
| 穿着日期详情弹窗 | `ClosetApp.UI/Components/Shared/Modal/WornDayDetailsDialog.xaml` |
| 推荐详情弹窗 | `ClosetApp.UI/Components/Shared/Modal/RecommendationDebugDialog.xaml` |
| 数据洞察弹窗 | `ClosetApp.UI/Components/Shared/Modal/WardrobeInsightsDialog.xaml` |
| 确认弹窗 | `ClosetApp.UI/Components/Shared/Modal/ConfirmDialog.xaml` |
| 错误提示器 | `ClosetApp.UI.Logic/Services/WardrobeActionErrorPresenter.cs` |
| 页面状态类 | `ClosetApp.UI.Logic/States/` |
| UI 逻辑共享工程 | `ClosetApp.UI.Logic/ClosetApp.UI.Logic.csproj` |
| 泛型 RadioButton 选择组 | `ClosetApp.UI/Components/Shared/EnumRadioGroup.cs` |
| 主题选择卡片控件 | `ClosetApp.UI/Components/Shared/ThemeCard.xaml` |
| 文件大小格式化 | `ClosetApp.UI/Components/Shared/FileSizeFormatter.cs` |
| 动画工具 | `ClosetApp.UI/Components/Shared/AnimationHelper.cs` |
| 视觉树工具 | `ClosetApp.UI/Components/Shared/VisualTreeHelperExtensions.cs` |
| 今日推荐 UseCase | `ClosetApp.Application/UseCases/Outfits/GetTodayRecommendations.cs` |
| 今日推荐结果 DTO | `ClosetApp.Application/DTOs/TodayRecommendationResult.cs` |
| 推荐调试 DTO | `ClosetApp.Application/DTOs/RecommendationDebugDto.cs` |
| 数据洞察 DTO | `ClosetApp.Application/DTOs/WardrobeInsightsDto.cs` |
| 年度报告 DTO | `ClosetApp.Application/DTOs/AnnualOutfitReportDto.cs` |
| 推荐轮换策略枚举 | `ClosetApp.Domain/Enums/RecommendationRotationStrategy.cs` |
| 备份接口 | `ClosetApp.Application/Interfaces/IBackupService.cs` |
| 备份 DTO | `ClosetApp.Application/DTOs/BackupDtos.cs` |
| 备份实现 | `ClosetApp.Infrastructure/Services/BackupService.cs` |
| 图片修复 | `ClosetApp.Infrastructure/Services/ImageMaintenanceService.cs` |
| 数据洞察 UseCase | `ClosetApp.Application/UseCases/Insights/GetWardrobeInsights.cs` |
| 年度报告 UseCase | `ClosetApp.Application/UseCases/Insights/GetAnnualOutfitReport.cs` |
| 本地路径定义 | `ClosetApp.Infrastructure/AppPaths.cs` |
| 穿着记录实体 | `ClosetApp.Domain/Entities/OutfitWornRecord.cs` |
| 衣服快照 DTO | `ClosetApp.Application/DTOs/ClothingSnapshotDto.cs` |
| 穿着记录快照迁移 | `ClosetApp.Infrastructure/Migrations/*AddOutfitWornRecord*Snapshot*.cs` |
| 测试工程 | `ClosetApp.Tests/ClosetApp.Tests.csproj` |
| 架构约定 | `docs/ARCHITECTURE_CONVENTIONS.md` |

---

## 15. 已知说明

### 15.1 当前保留项

- `WeatherService` 已完整实现（Open-Meteo API，支持城市搜索、15 分钟缓存、天气代码映射）
- `ViewModels` 仍存在，但不是当前页面交互的唯一主轴
- `ClosetApp.UI.Logic` 是纯逻辑共享工程，承载 State、Engine、Import 等文件，供 UI 与测试工程直接引用复用
- `WardrobeActionErrorPresenter` 统一处理数据库忙/文件占用/权限不足等异常的中文提示
- 穿着记录快照系统：记录穿着时保存完整搭配快照，删除衣服或搭配时自动更新快照，确保历史记录永久保留

### 15.2 风险与后续方向

- SixLabors.ImageSharp 版本告警仍需后续评估
- 继续减少 code-behind 里的非 UI 逻辑
- 批量导入已具备导入前重复风险提示，后续可补失败回滚的更细粒度 UI 展示
- MasonryPanel 虚拟化支持（当前不支持虚拟化，大量卡片时内存开销较大）

---

## 16. 近期变更摘要

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
- 测试工程同时引用 `ClosetApp.UI.Logic` 与 `ClosetApp.UI`；纯逻辑文件归属 `UI.Logic` 并被 UI/测试复用，ViewModel / WPF 相关测试直接覆盖 UI 工程类型
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

- **年度穿搭报告**：点击"年度报告"按钮，查看当年穿搭数据总结
  - 新增 `AnnualOutfitReportDto`：总览、Top5、月度统计、场景/季节分布、精彩瞬间
  - 新增 `GetAnnualOutfitReport` UseCase：生成年度报告数据
  - 新增 `AnnualOutfitReportDialog` 弹窗：可视化展示年度报告

- **分页加载**：搭配页和衣柜页支持分页加载
  - 初始只显示前 20 个卡片，点击"加载更多"按钮查看剩余内容
  - 新增 `DisplayedOutfits` / `DisplayedClothes` 属性和 `LoadMoreOutfitsCommand` / `LoadMoreClothes()` 方法

#### 性能优化

- **推荐详情缓存**：缓存推荐调试结果，避免每次打开详情重新计算
- **数据洞察缓存**：缓存统计结果，数据变化时自动清除
- **日历按需加载**：初始加载不再获取日历数据，打开"查看记录"弹窗时才加载
- **图片懒加载**：
  - `PremiumClothingCard` 添加可见性检测，卡片可见时才加载图片
  - `OutfitPreviewCanvas` 添加懒加载支持，不可见时跳过渲染
- **推荐详情加载优化**：直接使用 ViewModel 已有的天气数据，避免重复网络请求
- **SizeChanged 防抖**：`ClothesTab` 添加 `DispatcherTimer` 防抖（100ms），减少窗口拖拽时的 CPU 开销
- **移除不必要的 Dispatcher 调用**：简化 PropertyChanged 回调和 RefreshAsync 方法

#### UI 优化

- **搭配页面布局优化**：
  - 简化 Hero 区域 secondary actions 按钮（只保留查看记录、推荐详情、刷新推荐）
  - 在页面顶部添加工具栏（数据洞察、年度报告）
  - 移除独立的年度报告卡片，改为顶部工具栏入口

- **批量导入提示**：批量添加衣服成功后显示 Toast 提示消息

#### Bug 修复

- 修复 `OutfitPreviewCanvas.Render()` 在 Popup 内部调用 `UpdateLayout()` 导致的 `NullReferenceException`
- 修复日历数据同步问题：记录穿着后日历自动刷新
- 修复批量导入成功后没有提示消息的问题

#### 代码清理

- 删除 `ClosetApp.UI/Converters/_Archive/` 目录（9 个废弃的 Converter 文件）
- 删除 `Themes/Chips.xaml`、`Themes/ButtonStyles.xaml`、`Themes/Styles.xaml`（与 Controls 目录下重复）
- 移除未使用的 `GetRecommendationParamsAsync()` 方法
- 提取 `VisualTreeHelperExtensions` 共享工具类，消除 3 个文件中的重复 `FindVisualChild<T>` 实现

### 2026-05-30

#### 数据模型更新

- **OutfitWornRecord 快照系统**：记录穿着时保存完整的搭配快照
  - 新增 `OutfitNameSnapshot`：搭配名称快照
  - 新增 `OutfitClothingIdsSnapshot`：衣服 ID 列表快照（JSON 格式）
  - 新增 `ClothingCountSnapshot`：衣服数量快照
  - 新增 `ClothingDetailsSnapshot`：衣服详情快照（JSON 格式，包含名称、图片路径、类型）
  - 新增 `IsSnapshotComplete`：标记快照是否完整
  - 外键 `OutfitId` 改为可空，支持搭配删除后保留穿着记录

- **Outfit 实体更新**：
  - 新增 `OriginalClothingCount`：记录原始衣服数量，用于判断搭配是否变化

- **ClothingSnapshotDto**：新增衣服快照 DTO
  - `Id`：衣服 ID
  - `Name`：衣服名称
  - `ImagePath`：图片路径
  - `Type`：衣服类型

#### 搭配变化提示

- **OutfitCard 搭配变化提示**：删除衣服后，搭配卡片显示变化提示
  - 显示"搭配已变化（原 X 件，现 Y 件）"
  - 使用玫瑰色背景突出显示

- **历史记录状态提示**：穿着记录显示搭配状态
  - 搭配已删除：显示"搭配已删除"
  - 搭配已变化：显示"搭配已变化"
  - 快照不完整：显示"搭配已删除（快照不完整）"或"搭配已变化（快照不完整）"

#### 删除逻辑优化

- **删除衣服时更新快照**：在删除搭配之前，更新相关穿着记录的快照
  - 确保快照包含所有衣服（包括即将被删除的衣服）
  - 设置 `IsSnapshotComplete = true`

- **删除搭配时更新快照**：在删除搭配之前，更新相关穿着记录的快照
  - 确保快照包含所有衣服
  - 设置 `IsSnapshotComplete = true`
  - 将 `OutfitId` 设为 null

- **删除结果反馈**：删除衣服时显示受影响的搭配列表
  - 显示哪些搭配被删除
  - 显示哪些搭配剩余多少件衣服

#### 标签页重构

- **移除季节标签显示**：季节标签（春/夏/秋/冬/四季）改为系统预设，页面不显示
- **简化页面结构**：只显示风格标签和场景标签两个区域
- **改进总览描述**：标题改为"整理你的风格和场景词"
- **新增使用状态筛选**：支持按"正在使用"和"未使用"筛选标签
- **新增最近添加排序**：支持按创建时间排序
- **增强使用统计**：显示衣物使用数量和搭配使用次数

#### 衣物编辑器优化

- **默认类型改为未选择**：添加衣服时默认选中"❓ 未选择"
- **允许空名称**：不填写名称时自动使用"未命名"

#### Toast 提示优化

- 收藏/取消收藏：显示搭配名称
- 推荐详情加载失败：包含当前温度信息
- 搭配列表刷新失败：说明无法加载最新数据
- 添加穿搭记录：显示搭配名称和日期
- 撤销记录：显示日期

#### 数据库迁移

- `AddOutfitWornRecordSnapshot`：添加快照字段
- `AddOutfitWornRecordClothingSnapshot`：添加衣服详情快照字段
- `AddIsSnapshotCompleteToOutfitWornRecord`：添加快照完整性标记
- `AddOutfitOriginalClothingCountAndClothingDetailsSnapshot`：添加原始衣服数量和衣服详情快照

---

## 17. 相关文档

- `README.md`：项目快速入口
- `docs/ARCHITECTURE_CONVENTIONS.md`：架构约定
- `AGENTS.md`：仓库协作与命令执行规范
