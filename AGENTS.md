# GirlfriendClosetApp — 私人数字衣橱

WPF 桌面端穿搭管理应用。Clean Architecture 分层，SQLite 持久化，Masonry 瀑布流布局。

## Tech Stack

- **UI**: WPF (.NET 10), HandyControl, CommunityToolkit.Mvvm
- **Architecture**: Clean Architecture (Domain / Application / Infrastructure / UI)
- **Database**: SQLite via EF Core
- **Images**: SixLabors.ImageSharp (缩略图), 图片存储于 `%LocalAppData%\ClosetApp\images\`

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
    ├── Components/            # 可复用组件
    ├── Converters/            # 值转换器
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

### Modal System

```
ModalService (Singleton)
  → fires ModalShowRequested event
    → ModalContainer (overlay with fade animation)
      → shows UserControl as modal content
```

使用方式：
```csharp
var panel = new AddOutfitPanel();
panel.SaveCompleted += async () => await LoadData();
panel.CloseRequested += () => ModalService.Instance.Hide();
ModalService.Instance.Show(panel);
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
- 连衣裙 (Dress) — 选了 → 上装+下装禁用
- 上装 (Top/Outerwear) — 选了 → 连衣裙禁用
- 下装 (Bottom/Skirt) — 选了 → 连衣裙禁用
- 鞋子 (Shoes) — 独立
- 装饰 (Accessory) — 独立
- 每层最多选 1 件

预览画布按穿着顺序从上到下拼接。

## Known Issues / Notes

- `ClothingTypeToHeightConverter` 已不再使用（卡片高度改为动态计算）
- `AddClothingDialog` (Window) 是旧版，当前使用 `AddClothingPanel` (UserControl via ModalService)
- `WeatherService` 是 stub 实现（固定返回 22°C 晴天）
- ViewModels 目前未被 Views 使用（Views 直接调用 Services）
- `Colors.xaml` 定义了蓝色 `PrimaryBrush` (#667eea)，与 `ButtonTokens.xaml` 的粉色 `PrimaryBrush` (#D9A299) 冲突，但 Colors.xaml 加载在前会被覆盖
