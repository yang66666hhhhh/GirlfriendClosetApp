# GirlfriendClosetApp — 私人数字衣橱

WPF 桌面端穿搭管理应用。Clean Architecture 分层，SQLite 持久化，Masonry 瀑布流布局。

## Tech Stack

- **UI**: WPF (.NET 10), HandyControl, CommunityToolkit.Mvvm
- **Architecture**: Clean Architecture (Domain / Application / Infrastructure / UI)
- **Database**: SQLite via EF Core
- **Images**: SixLabors.ImageSharp，图片资产分为 Original / Display / Thumbnail，存储于 `%LocalAppData%\ClosetApp\images\`

## Build & Run

```bash
dotnet build ClosetApp.slnx
dotnet run --project ClosetApp.UI
```

## Project Structure

```
ClosetApp.slnx
├── ClosetApp.Domain/          # 实体、枚举、仓储接口
│   ├── Entities/              # Clothing, Outfit, Tag, Favorite, OutfitWornRecord
│   ├── Enums/                 # ClothingType, Season, OutfitScene, TagCategory, AppThemeKind
│   ├── Interfaces/            # IRepository<T>, IClothingRepository, IOutfitRepository...
│   └── Clothing/              # GarmentType, DisplayCategory, LayerRole, ClothingMappings, ClothingTaxonomy
├── ClosetApp.Application/     # 服务接口、实现、DTO
│   ├── Interfaces/            # IClothingService, IOutfitService, ITagService, IFavoriteService...
│   ├── Services/              # 业务逻辑实现
│   ├── DTOs/                  # CreateOutfitDto, OutfitDto, BackupDtos, BatchClothingImportDtos...
│   ├── UseCases/              # GetWardrobeOverview, ImportClothesFromImages, RecordOutfitWorn...
│   └── Images/                # IImageAssetResolver, ImageAsset, ImageVariant
├── ClosetApp.Infrastructure/  # EF Core、仓储实现、图片存储
│   ├── Data/                  # ClosetDbContext (SQLite), ClosetDatabaseInitializer
│   ├── Repositories/          # 仓储实现
│   ├── Services/              # ImageStorageService, WeatherService, BackupService, RecommendationPreferencesService...
│   └── Migrations/            # EF Core 迁移
├── ClosetApp.UI/              # WPF 界面
│   ├── Views/                 # 页面和对话框
│   ├── Components/            # 可复用组件
│   │   ├── Outfit/
│   │   │   ├── Engine/        # OutfitCompositionEngine（布局算法）
│   │   │   ├── Controls/      # OutfitPreviewCanvas, OutfitCard
│   │   │   └── Editor/        # OutfitEditorPanel, OutfitSelectionRules
│   │   ├── Clothing/          # PremiumClothingCard, BatchClothingImportBuilder, BatchImportDuplicateChecker...
│   │   ├── Tags/              # TagEditorPanel, TagSelectionSection, SelectableTag
│   │   └── Shared/            # ThemeColorHelper, Modal, Form, States, Editor
│   ├── Converters/            # 值转换器
│   ├── ViewModels/            # MVVM ViewModel
│   ├── Services/              # ThemeService, ModalService, ToastService, WardrobeActionErrorPresenter
│   └── Themes/                # 设计 Token 和样式
│       ├── Tokens/            # Colors, Spacing, Radius, Shadows, Motion, Typography, Sizes
│       └── Controls/          # Buttons, Cards, Chips, Inputs, Pages
├── ClosetApp.UI.Logic/        # UI 纯逻辑共享工程（供测试引用）
└── ClosetApp.Tests/           # 纯逻辑测试工程（xUnit）
```

## Architecture

### Data Flow

```
View (XAML + code-behind)
  → Service (IClothingService / IOutfitService)
    → Repository (IClothingRepository)
      → EF Core (ClosetDbContext)
        → SQLite
```

### DI Registration (`App.xaml.cs`)

所有服务在 `ConfigureServices()` 中注册：
- `ClosetDbContext` — Scoped
- 仓储 — Scoped (`IClothingRepository`, `IOutfitRepository`, `ITagRepository`...)
- 服务 — Scoped (`IClothingService`, `IOutfitService`...)
- `IImageStorageService` — Singleton
- `ThemeService` — Singleton
- `ModalService`, `ToastService` — Singleton

使用方式：`App.Services.GetRequiredService<T>()`

## Domain Model

### Entities

| Entity | Key Fields | Relationships |
|--------|-----------|---------------|
| `Clothing` | Name, Type, ImagePath, Color, Brand, Season, FavoriteLevel, IsFavorite | M:N with Outfit (via OutfitClothing), M:N with Tag (via ClothingTag) |
| `Outfit` | Name, Scene, Season, Rating, WearCount | M:N with Clothing, 1:N Favorite, 1:N OutfitWornRecord |
| `Tag` | Name, Color | M:N with Clothing (via ClothingTag) |
| `Favorite` | OutfitId | FK to Outfit |
| `OutfitWornRecord` | OutfitId, WornDate | FK to Outfit |

### Enums

- `ClothingType`: Top, Bottom, Outerwear, Dress, Skirt, Shoes, Accessory
- `Season`: Spring, Summer, Autumn, Winter, AllSeason
- `OutfitScene`: Work, Date, Travel, Party, Casual
- `TagCategory`: Style, Scene, Season
- `AppThemeKind`: Rose, Blue

### Clothing Taxonomy

- `GarmentType`: 细粒度衣物类型（TShirt, Shirt, Blouse, Knitwear, Hoodie, Jacket, Coat, Jeans, Dress, Sneakers, Bag... 共 27 种）
- `DisplayCategory`: Topwear, Bottom, Dress, Footwear, Accessory
- `LayerRole`: BaseTop, MidLayer, OuterLayer, Bottom, FullBody, Footwear, Accessory
- `ClothingMappings`: GarmentType → DisplayCategory / LayerRole / 中文名称
- `ClothingTaxonomy`: 按 DisplayCategory 分组查询 GarmentType

### ID Type

所有实体继承 `BaseEntity`，使用 `Guid Id`（非 int）。

## UI Architecture

### Navigation

MainWindow 2 列布局：
- 左侧 `NavigationSidebar`（220px，可折叠到 72px）
- 右侧内容区：`ClothesTab`（默认）/ `OutfitsTab` / `TagsTab` / `SettingsTab`

### Tab Pages

| Tab | File | Description |
|-----|------|-------------|
| 衣柜 | `ClothesTab.xaml` | Masonry 瀑布流 + 搜索 + 分类筛选 |
| 搭配 | `OutfitsTab.xaml` | 搭配卡片列表 + 创建/编辑/删除 |
| 标签 | `TagsTab.xaml` | 标签管理 |
| 设置 | `SettingsTab.xaml` | 主题切换 + 天气 + 备份 + 维护 |

## Design System

### Theme System

双主题（柔粉 / 清蓝），通过 `ThemeService` 全局切换。

```
ThemeService (Singleton)
  → ThemePalette.Create(AppThemeKind) → 返回完整调色板
  → ApplyPalette() → 更新 Application.Resources 中所有 Color/Brush
```

所有 UI 组件使用 `{DynamicResource ...}` 绑定主题资源，切换时自动刷新。

主题调色板包含 5 套辅助色系：Sky（天空蓝）、Mint（薄荷绿）、Rose（玫瑰粉）、Amber（琥珀）、Lavender（薰衣草），每套各有 Surface / Border / Text 三个变体。

### Color Tokens (`Themes/Tokens/Colors.xaml`)

| Token | Rose | Blue | Usage |
|-------|------|------|-------|
| Primary | #CA9C9F | #5881D6 | 主色调 |
| Primary.Dark | #B08488 | #375AAA | 深色强调 |
| Primary.Light | #F7F0EE | #E0ECFF | 浅色背景 |
| Primary.Glow | 60%透明 | 60%透明 | 发光/标签 |
| Surface.Page | #F9F5F1 | #F0F5FC | 页面背景 |
| Surface.Card | #FFFFFF | #FFFFFF | 卡片背景 |
| Surface.Hero | #F7F1ED | #E6EEFA | 预览区背景 |
| Surface.Section | #FBF6F3 | #EAF1FC | 区域背景 |
| Surface.ImageArea | #F7F2EE | #EEF4FC | 图片区背景 |
| Border.Light | #ECE2DF | #CDDAF0 | 边框 |
| Shadow.Color | #30927C76 | #303C5078 | 阴影 |
| Theme.Rose.* | 玫瑰粉系 | 蓝灰色系 | 主题辅助色 |
| Theme.Sky.* | 粉色系 | 蓝色系 | 主题辅助色 |
| Theme.Mint.* | 暖绿色系 | 青色系 | 主题辅助色 |
| Theme.Amber.* | 琥珀色系 | 蓝灰色系 | 主题辅助色 |
| Theme.Lavender.* | 薰衣草色系 | 靛蓝色系 | 主题辅助色 |

### Card Design System (`Themes/Controls/Cards.xaml`)

统一卡片设计语言，OutfitCard 和 PremiumClothingCard 共用。

#### Tokens

```xml
Card.Radius = 20
Card.InfoPadding = 16,12,16,14
Card.TitleFontSize = 15
Card.SubtitleFontSize = 11
Card.ChipFontSize = 10
Card.FavoriteFontSize = 18
```

#### Motion Tokens — Soft Elevation

```xml
Card.HoverTranslateY = -4
Card.HoverScale = 1.01
Card.HoverShadowBlur = 28
Card.HoverShadowOpacity = 0.12
Card.HoverImageScale = 1.02    <!-- 仅衣服卡片 -->
Card.HoverDurationMs = 220
Card.IdleShadowBlur = 16
Card.IdleShadowOpacity = 0.06
```

#### Shared Styles

| Style | Target | Usage |
|-------|--------|-------|
| `Card.Container` | Border | 卡片外壳（背景、圆角、光标） |
| `Card.PreviewArea` | Border | 预览区（上半圆角、主题背景） |
| `Card.InfoArea` | Border | 信息区（底部背景、内边距） |
| `Card.Title` | TextBlock | 标题（15px SemiBold） |
| `Card.Subtitle` | TextBlock | 副标题（11px Secondary） |
| `Card.Tertiary` | TextBlock | 三级文字（11px Tertiary） |
| `Card.ChipPanel` | WrapPanel | 标签面板 |
| `Card.FavoriteButtonBase` | Button | 收藏按钮基础 |
| `Card.ActionOverlay` | Border | 操作覆盖层 |
| `Card.OverlayCapsuleButton` | Button | 覆盖层按钮 |

#### Chip Palette (`ThemeColorHelper.ResolveChipPalette`)

标签芯片配色，主题感知，支持季节/场景/分类标签：
- 春/夏/秋/冬/四季 — 暖色系
- 通勤/约会/出游/派对/休闲 — 场景色系
- 上衣/裤装/连衣裙/半裙/外套/鞋子/配饰 — 分类色系

### Card Hover — Soft Elevation

统一悬停效果，模拟"柔和空间抬升"：

```
Idle:   TranslateY=0, Scale=1.0, Shadow.Blur=16, Shadow.Opacity=0.06
Hover:  TranslateY=-4, Scale=1.01, Shadow.Blur=28, Shadow.Opacity=0.12
```

衣服卡片额外效果：`ImageScale=1.02`（像被"拿起来"）

动画方式：代码直接动画（`AnimateTranslate`/`AnimateScale`/`AnimateShadow`），不依赖 Storyboard Key。

### Button Styles (`Themes/Controls/Buttons.xaml`)

基于 `AppButtonBase` 共享模板（hover scale + press scale 动画）：
- `PrimaryButton` — 主题色填充 + 阴影
- `CapsuleButton` — 白底 + 边框 + CornerRadius 12
- `SecondaryButton` — 灰色填充
- `DangerButton` — 红色填充
- `GhostButton` — 透明 + 白色边框
- `IconButton` — 圆形 36px

### Resource Loading Order (`App.xaml`)

```
HandyControl (SkinDefault + Theme)
→ Tokens/Colors.xaml
→ Tokens/Typography.xaml
→ Tokens/Spacing.xaml
→ Tokens/Radius.xaml
→ Tokens/Shadows.xaml
→ Tokens/Motion.xaml
→ Tokens/Sizes.xaml
→ Controls/LegacyStyles.xaml
→ Controls/Buttons.xaml
→ Controls/Inputs.xaml
→ Controls/Chips.xaml
→ Controls/Cards.xaml
→ Controls/Pages.xaml
→ Shared/Modal/ModalCardStyles.xaml
→ Shared/Modal/ModalFooterStyles.xaml
→ Shared/Form/FormStyles.xaml
```

## Key Components

### Outfit Engine（穿搭视觉引擎）

三层架构：

```
OutfitCompositionEngine (布局算法)
  ↓ CalculateLayout()
OutfitRenderMetrics (渲染参数)
  ↓
OutfitPreviewCanvas (WPF 渲染) ← 用于 OutfitCard + OutfitEditorPanel
```

### Image Processing

#### 前景提取 (`ClothingImageLoader`)

自动抠除图片边缘连通的浅色背景：
- 从图片四边采样背景种子色
- Flood-fill 标记连通的背景像素
- 前景保护：中性衣物色（亮度 60-220、饱和度 ≤35）不被误删
- 裁边：找最大前景连通域，收紧边界

参数：
```
LightBackgroundThreshold = 240
NeutralBackgroundThreshold = 232
BackgroundSeedTolerance = 10
ForegroundProtectionLuminanceGap = 55
NeutralClothingMin = 60, Max = 220, SatMax = 35
```

#### 衣物颜色背景

`ThemeColorHelper.ResolveClothingBackdrop(colorField)` — 根据衣物颜色字段计算主题感知背景色，与主题基础色混合（45% 衣物色 + 55% 主题色）。

### Modal System

```
ModalService (Singleton)
  → fires ModalShowRequested event
    → ModalContainer (overlay with fade animation)
      → shows UserControl as modal content
```

### MasonryPanel

自定义 `Panel` 实现瀑布流：
- 最短列优先放置算法
- `ColumnWidth` / `Spacing` 依赖属性
- `ArrangeOverride` 返回实际内容高度（修复 ScrollViewer 滚不到底）
- 卡片在 `MeasureOverride` 中通过 `FindMasonryColumnWidth()` 获取列宽，计算图片高度

### PremiumClothingCard

- 图片高度由图片宽高比动态计算（`CalcImageHeight`）
- 信息区高度动态计算（`CalcInfoAreaHeight`）：基础 80px + 标签 26px + 品牌 16px
- `Stretch="Uniform"` 不裁切
- 前景提取：`extractForeground: true` 自动抠除白底/浅灰底
- 悬停：Soft Elevation（TranslateY -4, Scale 1.01, Shadow 16→28, ImageScale 1.02）
- 底部横条覆盖层：编辑 / 删除
- 信息区：标题 + 品牌 + 标签芯片 + 收藏按钮

### OutfitCard

- 预览区：`OutfitPreviewCanvas` 渲染穿搭组合
- 背景色：根据衣物颜色/季节动态计算（`ThemeColorHelper.ResolveOutfitBackdrop`）
- 悬停：Soft Elevation（TranslateY -4, Scale 1.01, Shadow 16→28）
- 底部横条覆盖层：编辑 / 删除 / 今天穿了
- 信息区：标题 + 氛围描述 + 标签芯片 + 穿着信息 + 收藏按钮

### Image Path Resolution

三级路径查找（`ImagePathConverter` + code-behind）：
1. 绝对路径 `File.Exists(path)`
2. 相对路径 `AppDomain.BaseDirectory + path`
3. LocalAppData `%LocalAppData%\ClosetApp\images\ + path`

`ImagePathConverter` 支持参数：`Variant:Width:trim:fg`（如 `Thumbnail:160:fg`）

图片存储：通过 `IImageStorageService.SaveImageAsync()` 复制到 LocalAppData，数据库存 GUID 文件名。

## Key Patterns

### XAML Resources

- 全局资源在 `App.xaml` merged dictionaries 中定义
- 页面级资源在 `UserControl.Resources` 中定义
- 主题相关绑定使用 `{DynamicResource ...}`（确保主题切换时刷新）
- 非主题绑定可使用 `{StaticResource ...}`

### Converter Usage

- `ImagePathConverter` — 图片路径解析（支持三级路径 + `fg` 前景提取参数）
- `InverseNullToVisibilityConverter` — null 时显示（用于图片 fallback）
- `BoolToFavoriteColorConverter` — 收藏状态颜色
- `SeasonToNameConverter` — Season 枚举转中文
- `ClothingTypeToNameConverter` — ClothingType 枚举转中文

### Event Handling

- `PremiumClothingCard` 和 `OutfitCard` 使用 WPF 路由事件（`CardClicked`, `EditClicked`, `DeleteClicked`）
- 在 DataTemplate 中绑定：`<components:PremiumClothingCard EditClicked="Handler"/>`
- 不要用 `Border.MouseLeftButtonDown` 包裹卡片（会被卡片内部事件消费）

### Outfit Creation Rules

创建搭配时的衣服选择互斥规则：
- 连衣裙 (Dress) — 选了 → 上衣 + 裤装 / 半裙禁用，可搭外套
- 上衣 (Top) — 选了 → 连衣裙禁用
- 外套 (Outerwear) — 不占上衣位，可与上衣或连衣裙同时选择
- 下身 (Bottom/Skirt) — 裤装与半裙二选一，选了 → 连衣裙禁用
- 鞋子 (Shoes) — 独立
- 配饰 (Accessory) — 可多选
- 待分类 (Unspecified) — 不参与搭配选择

预览画布按"人体区域 + 穿搭层级"表达，不按分类简单堆叠：
- 上半身区域：外套为外层主图，上衣/中层作为内层露出
- 下半身区域：裤装或半裙
- 脚部区域：鞋子
- 配饰区域：侧边或角标小卡

## Known Issues / Notes

- `WeatherService` 已完整实现（Open-Meteo API，支持城市搜索、15 分钟缓存、天气代码映射）
- ViewModels 目前未被 Views 使用（Views 直接调用 Services）
- 命名空间歧义：文件目录 `Components/Outfit/` 和 `Components/Clothing/` 被编译器视为 namespace，与 `Domain.Entities.Outfit/Clothing` 冲突。使用 `global::ClosetApp.Domain.Entities.Outfit/Clothing` 显式引用实体类型
- `Components/_Archive/` 保留旧版 `AddClothingPanel` 备份
- `Views/_Deprecated/` 保留旧版 Dialog 备份
- `Converters/_Archive/` 保留废弃 Converter 备份
- `ClosetApp.UI.Logic` 是纯逻辑共享工程，通过 `<Compile Include>` 引用 UI 中的 State、Engine、Import 等文件，供测试工程独立引用
- `WardrobeActionErrorPresenter` 统一处理数据库忙/文件占用/权限不足等异常的中文提示
