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
│   ├── Enums/                 # ClothingType, Season, OutfitScene
│   └── Interfaces/            # IRepository<T>, IClothingRepository, IOutfitRepository...
├── ClosetApp.Application/     # 服务接口、实现、DTO
│   ├── Interfaces/            # IClothingService, IOutfitService, ITagService...
│   ├── Services/              # 业务逻辑实现
│   └── DTOs/                  # CreateOutfitDto, OutfitDto...
├── ClosetApp.Infrastructure/  # EF Core、仓储实现、图片存储
│   ├── Data/                  # ClosetDbContext (SQLite)
│   ├── Repositories/          # 仓储实现
│   ├── Services/              # ImageStorageService, WeatherService
│   └── Migrations/            # EF Core 迁移
└── ClosetApp.UI/              # WPF 界面
    ├── Views/                 # 页面和对话框
    │   └── _Legacy/           # 旧版（已归档，保留备份）
    │   └── _Deprecated/       # 废弃 Dialog（已归档）
    ├── Components/            # 可复用组件
    │   ├── Outfit/
    │   │   ├── Engine/        # OutfitCompositionEngine（布局算法）
    │   │   ├── Controls/      # OutfitPreviewCanvas, OutfitCard
    │   │   └── Editor/        # OutfitEditorPanel（统一编辑器）
    │   └── Clothing/          # PremiumClothingCard
    ├── Converters/            # 值转换器
    │   └── _Archive/          # 废弃 Converter（已归档）
    ├── ViewModels/            # MVVM ViewModel
    ├── Services/              # ModalService, ToastService
    └── Themes/                # 设计 Token 和样式
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

### ID Type

所有实体继承 `BaseEntity`，使用 `Guid Id`（非 int）。

## UI Architecture

### Navigation

MainWindow 2 列布局：
- 左侧 `NavigationSidebar`（220px，可折叠到 72px）
- 右侧内容区：`ClothesTab`（默认）/ `OutfitsTab` / `TagsTab`

### Tab Pages

| Tab | File | Description |
|-----|------|-------------|
| 衣柜 | `ClothesTab.xaml` | Masonry 瀑布流 + 搜索 + 分类筛选 |
| 搭配 | `OutfitsTab.xaml` | 搭配卡片列表 + 创建/编辑/删除 |
| 标签 | `TagsTab.xaml` | 标签管理 |

### Outfit Engine（穿搭视觉引擎）

三层架构：

```
OutfitCompositionEngine (布局算法)
  ↓ CalculateLayout()
OutfitRenderMetrics (渲染参数)
  ↓
OutfitPreviewCanvas (WPF 渲染) ← 用于 OutfitCard + OutfitEditorPanel
```

### Clothing Editor（衣服编辑器）

统一 `ClothingEditorPanel`（UserControl + Modal）同时服务 Create 和 Edit：

```
Components/Clothing/
├── ClothingEditorResult.cs    # record + enum（Saved/Deleted/Cancelled）
├── ClothingEditorPanel.xaml   # 统一 UI（Add+Edit 共用）
└── ClothingEditorPanel.xaml.cs
```

#### 使用方式

```csharp
// Add 模式
var panel = new ClothingEditorPanel();
panel.EditorCompleted += async (_, result) =>
{
    if (result.Type == ClothingEditorResultType.Saved)
        await _clothingService.AddClothingAsync(result.Clothing!);
    ModalService.Instance.Hide();
    await LoadClothesAsync();
};
ModalService.Instance.Show(panel);

// Edit 模式
var panel = new ClothingEditorPanel(clothing);
panel.EditorCompleted += async (_, result) =>
{
    if (result.Type == ClothingEditorResultType.Saved)
        await _clothingService.UpdateClothingAsync(result.Clothing!);
    else if (result.Type == ClothingEditorResultType.Deleted)
        await _clothingService.DeleteClothingAsync(clothing.Id);
    ModalService.Instance.Hide();
    await LoadClothesAsync();
};
ModalService.Instance.Show(panel);
```

#### 关键设计

- `ClothingEditorResult` 是 `sealed record`，语义清晰（Saved/Deleted/Cancelled）
- `_imageChanged` 标记避免重复 SaveImageAsync
- `IsDirty` 字段预留 dirty-check 未来扩展
- Edit 模式显示删除按钮（点击直接触发 `Deleted` 结果）
- 分类选项完整（含 Skirt）
- 情绪标签保留
- Notes 备注字段（Edit 模式可见）
- 图片支持拖拽上传、更换、移除

#### Engine 层 — `Components/Outfit/Engine/`

- `OutfitCompositionEngine.cs` — 4 种布局模式自动切换（Solo/Dress/TopBottom/Mixed），高度预算分配制
- `OutfitRenderMetrics.cs` — 渲染参数配置（StandardHeight/MaxItems/Spacing 等），消灭 magic numbers
- `CompositionMode.cs` — 枚举（Solo/Dress/TopBottom/Mixed）
- `OutfitLayoutItem.cs` — 布局数据模型（LayoutType/Item/Height/Y）

#### Controls 层 — `Components/Outfit/Controls/`

- `OutfitPreviewCanvas.xaml/.cs` — WPF Canvas，依赖属性 `Clothes` 驱动渲染，`MeasureCanvasWidth()`/`MeasureCanvasHeight()` 向上探测父级可用空间，`Loaded` + `SizeChanged` 双重触发渲染
- `OutfitCard.xaml/.cs` — 展示卡片，`Outfit` 依赖属性，`EditClicked`/`DeleteClicked` 路由事件，hover 放大动画

#### Editor 层 — `Components/Outfit/Editor/`

- `OutfitEditorPanel.xaml/.cs` — 统一编辑器（Create + Edit），Modal 弹出，`SelectableClothing` 包装类（IsSelected/IsEnabled），`OnLoadedForEdit` 修复 Edit 模式加载时序

### Modal System

```
ModalService (Singleton)
  → fires ModalShowRequested event
    → ModalContainer (overlay with fade animation)
      → shows UserControl as modal content
```

使用方式（穿搭编辑器）：
```csharp
var panel = new OutfitEditorPanel(); // Create 模式
panel.SaveCompleted += async () => await LoadData();
panel.CloseRequested += () => ModalService.Instance.Hide();
ModalService.Instance.Show(panel);

// 或 Edit 模式
var panel = new OutfitEditorPanel(existingOutfit);
```

### MasonryPanel

自定义 `Panel` 实现瀑布流：
- 最短列优先放置算法
- `ColumnWidth` / `Spacing` 依赖属性
- `ArrangeOverride` 返回实际内容高度（修复 ScrollViewer 滚不到底）
- 卡片在 `MeasureOverride` 中通过 `FindMasonryColumnWidth()` 获取列宽，计算图片高度

### PremiumClothingCard

卡片组件：
- 图片高度由图片宽高比动态计算（`CalcImageHeight`）
- `Stretch="Uniform"` 不裁切
- Hover 动画：TranslateY -4, ImageScale 1.04, Shadow Blur 16→24 (200ms)
- 悬停显示 ⋯ + ♥ 按钮（毛玻璃圆形）
- ⋯ 菜单：编辑 / 删除 → 触发 `EditClicked` / `DeleteClicked` 路由事件
- ♥ 动画：通过 `Template.FindName` 跨 namescope 访问 ControlTemplate 内的 HeartIcon

### Image Path Resolution

三级路径查找（`ImagePathConverter` + code-behind）：
1. 绝对路径 `File.Exists(path)`
2. 相对路径 `AppDomain.BaseDirectory + path`
3. LocalAppData `%LocalAppData%\ClosetApp\images\ + path`

图片存储：`AddClothingPanel` 通过 `IImageStorageService.SaveImageAsync()` 复制到 LocalAppData，数据库存 GUID 文件名。

## Design System

### Color Tokens (`ButtonTokens.xaml`)

| Token | Value | Usage |
|-------|-------|-------|
| Primary | #D9A299 | 主色调（暖粉） |
| Danger | #E88D8D | 删除/警告 |
| Text.Primary | #2D2A26 | 标题文字 |
| Text.Secondary | #9E958D | 副标题 |
| Text.Placeholder | #C8C0B8 | 占位文字 |
| Surface.Card | #FDFC | 卡片背景 |
| Surface.Page | #F6F3EE | 页面背景 |

### Button Styles (`ButtonStyles.xaml`)

基于 `AppButtonBase` 共享模板（hover scale + press scale 动画）：
- `PrimaryButton` — 粉色填充 + 阴影
- `CapsuleButton` — 白底 + 边框 + CornerRadius 12
- `SecondaryButton` — 灰色填充
- `DangerButton` — 红色填充
- `GhostButton` — 透明 + 白色边框
- `IconButton` — 圆形 36px

### Resource Loading Order (`App.xaml`)

```
HandyControl (SkinDefault + Theme)
→ ButtonTokens.xaml
→ Colors.xaml
→ Styles.xaml
→ ButtonStyles.xaml
→ PremiumCardStyles.xaml
```

## Key Patterns

### XAML Resources

- 全局资源在 `App.xaml` merged dictionaries 中定义
- 页面级资源在 `UserControl.Resources` 中定义（颜色、DataTemplate、Converter）
- 避免 `DynamicResource` 引用全局资源（可能解析失败），优先用 `StaticResource` + 本地定义

### Converter Usage

- `ImagePathConverter` — 图片路径解析（支持三级路径）
- `InverseNullToVisibilityConverter` — null 时显示（用于图片 fallback）
- `BoolToFavoriteColorConverter` — 收藏状态颜色
- `SeasonToNameConverter` — Season 枚举转中文
- `ClothingTypeToNameConverter` — ClothingType 枚举转中文

### Event Handling

- `PremiumClothingCard` 使用 WPF 路由事件（`CardClicked`, `EditClicked`, `DeleteClicked`）
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

预览画布按“人体区域 + 穿搭层级”表达，不按分类简单堆叠：
- 上半身区域：外套为外层主图，上衣/中层作为内层露出
- 下半身区域：裤装或半裙
- 脚部区域：鞋子
- 配饰区域：侧边或角标小卡

## Known Issues / Notes

- `WeatherService` 是 stub 实现（固定返回 22°C 晴天）
- ViewModels 目前未被 Views 使用（Views 直接调用 Services）
- `Colors.xaml` 定义了蓝色 `PrimaryBrush` (#667eea)，与 `ButtonTokens.xaml` 的粉色 `PrimaryBrush` (#D9A299) 冲突，但 Colors.xaml 加载在前会被覆盖
- 命名空间歧义：文件目录 `Components/Outfit/` 和 `Components/Clothing/` 被编译器视为 namespace，与 `Domain.Entities.Outfit/Clothing` 冲突。使用 `global::ClosetApp.Domain.Entities.Outfit/Clothing` 显式引用实体类型
- `Components/_Archive/` 保留旧版 `AddClothingPanel` 备份
- `Views/_Deprecated/` 保留旧版 `EditClothingDialog`、`EditOutfitDialog`、`RecordOutfitDialog`、`DeleteConfirmDialog`、`ModernDialog` 备份
- `Converters/_Archive/` 保留废弃 Converter 备份（WPF 不允许删除被 XAML 引用的资源）
