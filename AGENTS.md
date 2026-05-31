# GirlfriendClosetApp — AI 编码规范

WPF 桌面端穿搭管理应用。Clean Architecture 分层，SQLite 持久化，Masonry 瀑布流布局。

> 本文档约束 AI 编码行为。修改代码前必须阅读。

---

## 1. 项目概述

私人数字衣橱桌面应用，管理个人衣物、搭配、标签、穿着记录和本地图片资产。

核心功能：衣柜管理、搭配创建与预览、标签管理、天气驱动今日推荐、批量导入、备份与恢复。

---

## 2. 技术栈

| 层 | 技术 | 说明 |
|---|---|---|
| UI | WPF (`net10.0-windows`) | 桌面端界面 |
| 核心类库 | .NET (`net8.0`) | Domain / Application / Infrastructure |
| UI 组件 | HandyControl | 基础控件与样式 |
| MVVM | CommunityToolkit.Mvvm | ViewModel 框架 |
| 数据访问 | EF Core + SQLite | 本地数据库 |
| 图片处理 | SixLabors.ImageSharp | 原图/缓存处理 |
| 日志 | Serilog | 本地滚动日志 |
| 测试 | xUnit | 单元测试框架 |

---

## 3. 构建与运行

```bash
# 构建
rtk dotnet build ClosetApp.slnx /m:1

# 运行
rtk dotnet run --project ClosetApp.UI

# 测试
rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1
```

---

## 4. 项目结构

```
ClosetApp.slnx
├── ClosetApp.Domain/          # 实体、枚举、仓储接口
│   ├── Entities/              # Clothing, Outfit, Tag, Favorite, OutfitWornRecord
│   ├── Enums/                 # ClothingType, Season, OutfitScene, TagCategory, RecommendationRotationStrategy
│   ├── Interfaces/            # IRepository<T>, IClothingRepository, IOutfitRepository...
│   └── Clothing/              # GarmentType, DisplayCategory, LayerRole, ClothingMappings, ClothingTaxonomy
├── ClosetApp.Application/     # 服务接口、实现、DTO、UseCases
│   ├── Interfaces/            # IClothingService, IOutfitService, ITagService, IFavoriteService...
│   ├── Services/              # 业务逻辑实现
│   ├── DTOs/                  # CreateOutfitDto, OutfitDto, BackupDtos, TodayRecommendationResult...
│   ├── UseCases/              # GetWardrobeOverview, GetTodayRecommendations, RecordOutfitWorn...
│   └── Images/                # IImageAssetResolver, ImageAsset, ImageVariant
├── ClosetApp.Infrastructure/  # EF Core、仓储实现、图片存储
│   ├── Data/                  # ClosetDbContext (SQLite), ClosetDatabaseInitializer
│   ├── Repositories/          # 仓储实现
│   ├── Services/              # ImageStorageService, WeatherService, BackupService...
│   └── Migrations/            # EF Core 迁移
├── ClosetApp.UI/              # WPF 界面
│   ├── Views/                 # ClothesTab, OutfitsTab, TagsTab, SettingsTab
│   ├── Components/
│   │   ├── Outfit/            # OutfitPreviewCanvas, OutfitCard, OutfitEditorPanel 等 WPF 控件
│   │   ├── Clothing/          # PremiumClothingCard, ClothingEditorPanel 等 WPF 控件
│   │   ├── Tags/              # TagEditorPanel, TagSelectionSection, SelectableTag
│   │   ├── Settings/          # ImageMaintenanceSettingsPanel, BackupSettingsPanel 等设置页稳定区块
│   │   └── Shared/            # EnumRadioGroup, ThemeCard, FileSizeFormatter, AnimationHelper, ThemeColorHelper, Modal, Form, States, Editor
│   ├── Converters/            # ImagePathConverter, BoolToFavoriteColorConverter...
│   ├── ViewModels/            # WardrobeViewModel, OutfitsViewModel, SettingsViewModel, TagsViewModel
│   ├── Services/              # ThemeService, ModalService, ToastService
│   └── Themes/
│       ├── Tokens/            # Colors, Spacing, Radius, Shadows, Motion, Typography, Sizes
│       └── Controls/          # Buttons, Cards, Chips, Inputs, Pages
├── ClosetApp.UI.Logic/        # UI 纯逻辑共享工程（State、Engine、Import、错误提示等逻辑源码归属处）
└── ClosetApp.Tests/           # xUnit 测试工程（当前同时引用 UI.Logic 与 UI 工程）
```

---

## 5. 架构

### 5.1 数据流

```
View (XAML + code-behind)
  → ViewModel (状态管理)
    → Service (IClothingService / IOutfitService)
      → Repository (IClothingRepository)
        → EF Core (ClosetDbContext)
          → SQLite
```

### 5.2 依赖方向

```
Domain ← Application ← Infrastructure
                      ← UI
                      ← UI.Logic
                      ← Tests
```

**禁止**：Domain 引用任何其他层；Application 引用 Infrastructure 或 UI。

### 5.3 DI 注册（App.xaml.cs）

| 类型 | 生命周期 | 示例 |
|------|----------|------|
| DbContext | Scoped | `AddDbContextFactory<ClosetDbContext>()` |
| Repository | Scoped | `IClothingRepository`, `IOutfitRepository` |
| Service | Scoped | `IClothingService`, `IOutfitService` |
| UseCase | Scoped | `GetWardrobeOverview`, `GetTodayRecommendations` |
| 图片服务 | Singleton | `IImageStorageService`, `IImageMaintenanceService` |
| UI 服务 | Singleton | `ThemeService`, `ModalService`, `ToastService` |
| 偏好服务 | Singleton | `IWeatherPreferencesService`, `IRecommendationPreferencesService` |
| 天气服务 | HttpClient | `IWeatherService` 通过 `AddHttpClient` 注册 |

使用方式：`App.Services.GetRequiredService<T>()`

---

## 6. 领域模型

### 6.1 实体

| Entity | Key Fields | Relationships |
|--------|-----------|---------------|
| `Clothing` | Name, Type, GarmentType, ImagePath, Color, Brand, Season, FavoriteLevel | M:N with Outfit (via OutfitClothing), M:N with Tag (via ClothingTag) |
| `Outfit` | Name, Scene, Season, Rating, WearCount, WornDate, OriginalClothingCount | M:N with Clothing, 1:N Favorite, 1:N OutfitWornRecord |
| `Tag` | Name, Color, Category | M:N with Clothing (via ClothingTag) |
| `Favorite` | OutfitId | FK to Outfit |
| `OutfitWornRecord` | OutfitId(nullable), WornDate, OutfitNameSnapshot, OutfitClothingIdsSnapshot, ClothingCountSnapshot, ClothingDetailsSnapshot, IsSnapshotComplete, PreviewSnapshotPath | Optional FK to Outfit; snapshot keeps history after outfit/clothing deletion |

所有实体继承 `BaseEntity`，使用 `Guid Id`（非 int）。

### 6.2 枚举

| Enum | Values | 位置 |
|------|--------|------|
| `ClothingType` | Unspecified, Top, Bottom, Outerwear, Dress, Skirt, Shoes, Accessory | Domain/Enums |
| `Season` | Unspecified, Spring, Summer, Autumn, Winter, AllSeason | Domain/Enums |
| `OutfitScene` | Work, Date, Travel, Party, Casual | Domain/Enums |
| `TagCategory` | Style, Scene, Season | Domain/Enums |
| `RecommendationRotationStrategy` | Balanced, PreferLessWorn, PreferFavorites | Domain/Enums |
| `AppThemeKind` | Rose, Blue | UI/Services |

### 6.3 衣物分类体系

| 类型 | 说明 | 示例 |
|------|------|------|
| `GarmentType` | 细粒度衣物类型（27 种） | TShirt, Shirt, Blouse, Jacket, Jeans, Dress, Sneakers, Bag |
| `DisplayCategory` | 展示分类 | Topwear, Bottom, Dress, Footwear, Accessory |
| `LayerRole` | 穿搭层级 | BaseTop, MidLayer, OuterLayer, Bottom, FullBody, Footwear, Accessory |

映射关系：`ClothingMappings.GetDisplayCategory(GarmentType)` / `ClothingMappings.GetLayerRole(GarmentType)`

---

## 7. UI 架构

### 7.1 导航

MainWindow 2 列布局：
- 左侧 `NavigationSidebar`（220px，可折叠到 72px）
- 右侧内容区：`ClothesTab`（默认）/ `OutfitsTab` / `TagsTab` / `SettingsTab`

### 7.2 页面职责

| Tab | 职责 | State 类 |
|-----|------|----------|
| ClothesTab | 瀑布流展示、搜索、分类筛选、批量导入 | `ClothesTabState` |
| OutfitsTab | 搭配列表、创建/编辑/删除、天气推荐、穿着记录 | `OutfitsTabState` |
| TagsTab | 风格/场景标签管理、使用状态筛选、使用频次统计；季节标签由系统管理 | `TagsTabState` |
| SettingsTab | 主题切换、天气、备份、图片维护；图片治理区由 `ImageMaintenanceSettingsPanel` 承接，备份恢复区由 `BackupSettingsPanel` 承接 | 无（使用 ViewModel） |

### 7.3 状态类约定

- 页面轻状态放在 `ClosetApp.UI.Logic/States`
- `ClosetApp.UI.Logic` 中的纯逻辑类型使用 `ClosetApp.UI.Logic.*` 命名空间
- State 负责：搜索文本、筛选器、加载标记、空状态、当前集合
- Code-behind 负责：点击处理、动画、弹窗编排

### 7.4 穿着记录快照约定

- `OutfitWornRecord.OutfitId` 可空，不能把搭配删除视为历史记录删除
- 记录穿着时保存搭配名称、衣服 ID 列表、衣服数量、衣服明细和预览图快照；衣服明细应包含 `Id`、`Name`、`ImagePath`、`Color`、`Type`、`GarmentType`
- 删除衣服或搭配前，先补齐相关穿着记录快照，并用 `IsSnapshotComplete` 标记完整性；旧快照即使已标完整，只要明细为空或数量不足也要刷新
- 历史弹窗需区分搭配已删除、搭配已变化和快照不完整状态，优先用快照展示历史内容
- 历史快照引用的图片是历史资产，删除衣物、批量清空和孤儿图清理不得物理删除这些图片
- live 搭配少于 2 件时可删除 live 搭配，但穿着记录必须保留，且 `OutfitId` 置空后继续使用快照展示
- 读取 live `Outfit.OutfitClothes` 时必须容忍 `Clothing` 导航为空；搭配卡片、预览和推荐评分要先过滤无效链接，再读取颜色、标签、类型或图片
- 历史快照图片缺失时，UI 仍需显示单品文字信息；单张修复只更新对应记录的 `ClothingDetailsSnapshot.ImagePath`，不得改写 live 搭配
- 历史缺图判断必须复用 `IImageAssetResolver`，不要在 UI 或 Application 中各自手写图片路径解析；修复失败时要清理本次新保存的图片，避免制造孤儿资产
- 历史图片健康检查结果应包含可导航的缺图记录摘要，不能只返回聚合数量；UI 应能引导用户打开对应日期详情

### 7.5 标签约定

- `TagCategory.Season` 是系统预设标签，不在标签页作为普通标签展示或整理
- 标签页只展示 `Style` / `Scene` 标签，并支持名称搜索、分类筛选、使用状态筛选与排序
- 标签使用统计同时关注衣物关联数和搭配使用次数

---

## 8. 设计系统

### 8.1 主题系统

双主题（柔粉 Rose / 清蓝 Blue），通过 `ThemeService` 全局切换。

```
ThemeService (Singleton)
  → ThemePalette.Create(AppThemeKind) → 返回完整调色板
  → ApplyPalette() → 更新 Application.Resources 中所有 Color/Brush
```

主题调色板包含 5 套辅助色系：Sky、Mint、Rose、Amber、Lavender。

### 8.2 Color Tokens

| Token | Rose | Blue | 用途 |
|-------|------|------|------|
| Primary | #CA9C9F | #5881D6 | 主色调 |
| Primary.Dark | #B08488 | #375AAA | 深色强调 |
| Primary.Light | #F7F0EE | #E0ECFF | 浅色背景 |
| Surface.Page | #F9F5F1 | #F0F5FC | 页面背景 |
| Surface.Card | #FFFFFF | #FFFFFF | 卡片背景 |
| Surface.Hero | #F7F1ED | #E6EEFA | 预览区背景 |
| Border.Light | #ECE2DF | #CDDAF0 | 边框 |

### 8.3 Card Design System

```xml
Card.Radius = 20
Card.HoverTranslateY = -4
Card.HoverScale = 1.01
Card.HoverShadowBlur = 28
Card.HoverDurationMs = 220
```

### 8.4 Button Styles

基于 `AppButtonBase` 共享模板：
- `PrimaryButton` — 主题色填充 + 阴影
- `CapsuleButton` — 白底 + 边框 + CornerRadius 12
- `SecondaryButton` — 灰色填充
- `DangerButton` — 红色填充
- `GhostButton` — 透明 + 白色边框

### 8.5 资源加载顺序

```
HandyControl → Tokens/* → Controls/* → Shared/Modal/* → Shared/Form/*
```

---

## 9. 关键组件

### 9.1 搭配引擎

```
OutfitCompositionEngine (布局算法)
  ↓ CalculateLayout()
OutfitRenderMetrics (渲染参数)
  ↓
OutfitPreviewCanvas (WPF 渲染)
```

### 9.2 图片处理

三层资产：Original（原图）/ Display（~900px）/ Thumbnail（~200px）

存储路径：`%LocalAppData%\ClosetApp\images\{originals|display|thumbnails}\`

前景提取：`ClothingImageLoader` 自动抠除浅色背景。

### 9.3 Modal 系统

```
ModalService (Singleton)
  → fires ModalShowRequested event
    → ModalContainer (overlay with fade animation)
      → shows UserControl as modal content
```

### 9.4 MasonryPanel

自定义 `Panel` 实现瀑布流，最短列优先放置算法。

### 9.5 共享组件

| 组件 | 用途 |
|------|------|
| `EnumRadioGroup<TEnum>` | 泛型 RadioButton 选择组 |
| `ThemeCard` | 主题选择卡片自定义控件 |
| `FileSizeFormatter` | 文件大小格式化（B/KB/MB/GB） |
| `AnimationHelper` | Shake 抖动动画 |
| `ThemeColorHelper` | 主题感知颜色解析 |

---

## 10. 关键模式（含代码示例）

### 10.1 添加新 Service

```csharp
// 1. 在 Application/Interfaces/ 创建接口
public interface IMyService
{
    Task<MyResult> DoSomethingAsync(string param);
}

// 2. 在 Infrastructure/Services/ 创建实现
public class MyService : IMyService
{
    public async Task<MyResult> DoSomethingAsync(string param) { ... }
}

// 3. 在 App.xaml.cs 注册
services.AddScoped<IMyService, MyService>();
// 或 Singleton: services.AddSingleton<IMyService, MyService>();

// 4. 在测试中创建 Fake
private sealed class FakeMyService : IMyService
{
    public Task<MyResult> DoSomethingAsync(string param) => Task.FromResult(new MyResult());
}
```

### 10.2 添加新 UseCase

```csharp
// 1. 在 Application/UseCases/ 创建类
public sealed class MyNewUseCase
{
    private readonly IMyService _service;

    public MyNewUseCase(IMyService service)
    {
        _service = service;
    }

    public async Task<MyResultDto> ExecuteAsync(MyRequestDto request)
    {
        // 业务逻辑
    }
}

// 2. 创建 Request/Result DTO（如需要）
public sealed record MyRequestDto(string Param);
public sealed record MyResultDto(string Data);

// 3. 在 App.xaml.cs 注册
services.AddScoped<MyNewUseCase>();

// 4. 在 ViewModel 中调用
var result = await _myNewUseCase.ExecuteAsync(new MyRequestDto("value"));
```

### 10.3 添加新 ViewModel 属性

```csharp
// 使用 CommunityToolkit.Mvvm 的 [ObservableProperty]
[ObservableProperty]
private string _myProperty = "default";

// 如需通知其他属性变化：
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(ComputedProperty))]
private int _count;

public string ComputedProperty => $"Count is {Count}";
```

### 10.4 添加新页面

```xml
<!-- 1. 创建 Views/MyPage.xaml -->
<UserControl x:Class="ClosetApp.UI.Views.MyPage" ...>
    <!-- XAML 内容 -->
</UserControl>

<!-- 2. 在 MainWindow.xaml 添加内容区 -->
<views:MyPage x:Name="MyPageContent" Visibility="Collapsed"/>

<!-- 3. 在 MainWindow.xaml.cs 添加导航逻辑 -->
private void ShowTab(int tabIndex)
{
    MyPageContent.Visibility = tabIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
}
```

### 10.5 添加新共享组件

```csharp
// 在 Components/Shared/ 创建
public partial class MyComponent : UserControl
{
    // 依赖属性
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(MyComponent));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
```

### 10.6 添加新测试

```csharp
// 在 ClosetApp.Tests/ 创建
public class MyServiceTests
{
    [Fact]
    public async Task DoSomethingAsync_ValidInput_ReturnsExpected()
    {
        // Arrange
        var service = new MyService();

        // Act
        var result = await service.DoSomethingAsync("test");

        // Assert
        Assert.NotNull(result);
    }
}
```

---

## 11. 编码约定

### 11.1 命名约定

| 类型 | 规则 | 示例 |
|------|------|------|
| 文件名 | PascalCase.cs | `ClothingService.cs` |
| 类名 | PascalCase | `ClothingService` |
| 接口 | I-prefix | `IClothingService` |
| 方法 | PascalCase | `GetAllClothesAsync` |
| 公共属性 | PascalCase | `SelectedType` |
| 私有字段 | _camelCase | `_clothingService` |
| 局部变量 | camelCase | `var result` |
| 枚举值 | PascalCase | `ClothingType.Top` |
| XAML x:Name | PascalCase | `TxtName`, `BtnSave` |
| 常量 | PascalCase | `DefaultThumbnailSize` |

### 11.2 异步模式

```
DO:
  - 优先使用 async/await
  - 事件处理器用 async void（仅此处允许）
  - 其他方法用 async Task
  - Infrastructure 层使用 ConfigureAwait(false)

DON'T:
  - 避免 async void（除事件处理器外）
  - 避免 .Result 或 .Wait()（会死锁）
  - 避免 fire-and-forget（除非有明确理由）
```

### 11.3 错误处理

```csharp
// 操作反馈：使用 ToastService
ToastService.Instance.ShowSuccess("已保存");
ToastService.Instance.ShowError("保存失败", ex.Message);

// 确认弹窗：使用 MessageBox
var result = MessageBox.Show("确定删除吗？", "确认", MessageBoxButton.OKCancel);

// 统一错误分类：使用 WardrobeActionErrorPresenter
var feedback = WardrobeActionErrorPresenter.ForClothingSave(ex, isEditMode);
ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
```

### 11.4 WPF/XAML 规则

```
DO:
  - 主题相关绑定使用 {DynamicResource ...}
  - 非主题绑定可使用 {StaticResource ...}
  - 新控件使用 DependencyProperties（非 CLR 属性）
  - 卡片组件使用路由事件（CardClicked, EditClicked, DeleteClicked）
  - 图片绑定使用 ImagePathConverter

DON'T:
  - 不要硬编码颜色/尺寸（用 Token 资源）
  - 不要用 Border.MouseLeftButtonDown 包裹卡片（会被内部事件消费）
  - 不要在 code-behind 中直接操作文件系统（用 Service）
  - 不要在 XAML 中使用 {Binding} 调用方法（用属性或转换器）
```

### 11.5 依赖注入规则

```
DO:
  - 接口定义在 Application/Interfaces
  - 实现在 Infrastructure/Services
  - UseCase 在 Application/UseCases
  - DI 注册在 App.xaml.cs ConfigureServices()

DON'T:
  - 不要在 Domain 层引用其他层
  - 不要在 Application 层引用 Infrastructure
  - 不要在 ViewModel 中 new Service（用 DI 注入）
```

### 11.6 文件组织

```
新 Service → Application/Interfaces + Infrastructure/Services
新 UseCase → Application/UseCases/{Feature}/
新 DTO → Application/DTOs/
新实体 → Domain/Entities
新枚举 → Domain/Enums
新页面 → Views/
新组件 → Components/{Feature}/
共享组件 → Components/Shared/
新状态类 → ClosetApp.UI.Logic/States/
新测试 → ClosetApp.Tests/
```

---

## 12. 测试

### 12.1 框架与运行

```bash
# 运行所有测试
rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1

# 运行指定测试
rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter "FullyQualifiedName~MyTest"
```

### 12.2 测试结构

```
ClosetApp.Tests/
├── BackupServiceTests.cs
├── ImageMaintenanceServiceTests.cs
├── OutfitCompositionEngineTests.cs
├── WardrobeViewModelTests.cs
└── ...
```

### 12.3 测试命名约定

```
MethodName_Scenario_ExpectedResult

示例：
GetAllClothesAsync_HasData_ReturnsClothes
SaveImageAsync_InvalidPath_ThrowsException
FormatSize_ZeroBytes_ReturnsZeroB
```

### 12.4 Fake 模式

```csharp
// 测试中使用内部 private sealed class 作为 Fake
private sealed class FakeClothingService : IClothingService
{
    public List<Clothing> Clothes { get; } = [];

    public Task<IReadOnlyList<Clothing>> GetAllClothesAsync()
        => Task.FromResult<IReadOnlyList<Clothing>>(Clothes);

    public Task AddClothingAsync(Clothing clothing)
    {
        Clothes.Add(clothing);
        return Task.CompletedTask;
    }
    // ... 其他接口方法
}
```

### 12.5 UI 逻辑测试

- UI 纯逻辑文件归属 `ClosetApp.UI.Logic`，便于 UI 与测试复用 State、Engine、Import 等逻辑
- 当前测试工程也直接引用 `ClosetApp.UI.csproj`，用于覆盖 ViewModel / WPF 相关类型
- 测试文件放在 `ClosetApp.Tests/`，与源文件同名加 `Tests` 后缀

---

## 13. 常见陷阱

### 13.1 命名空间冲突

**问题**：`Components/Outfit/` 和 `Components/Clothing/` 被编译器视为 namespace，与 `Domain.Entities.Outfit/Clothing` 冲突。

**解决**：使用 `global::` 别名：
```csharp
using OutfitEntity = global::ClosetApp.Domain.Entities.Outfit;
using ClothingEntity = global::ClosetApp.Domain.Entities.Clothing;
```

### 13.2 XAML 绑定到方法

**问题**：WPF 不支持 `{Binding MethodName}` 绑定到方法。

**解决**：使用属性或转换器：
```xml
<!-- 错误 -->
<TextBlock Text="{Binding FormatSize}" />

<!-- 正确 -->
<TextBlock Text="{Binding FileSize}" />
```

### 13.3 文件编码

**问题**：PowerShell `Out-File -Encoding utf8` 会引入 BOM，导致 C# 编译错误。

**解决**：使用 `[System.IO.File]::WriteAllText($file, $content)` 或直接用 Edit 工具。

### 13.4 async void

**问题**：`async void` 方法的异常无法被调用方捕获。

**解决**：仅在事件处理器中使用 `async void`，其他地方用 `async Task`：
```csharp
// 事件处理器：允许 async void
private async void Button_Click(object sender, RoutedEventArgs e) { ... }

// 其他方法：必须 async Task
public async Task DoSomethingAsync() { ... }
```

### 13.5 XAML 编辑替换失败

**问题**：XAML 文件中的替换可能因缩进不匹配而失败。

**解决**：先用 Read 工具读取精确内容，再用 Edit 工具替换。避免猜测缩进。

### 13.6 Application 层引用 Infrastructure

**问题**：Application 层不能引用 Infrastructure 层（依赖方向约束）。

**解决**：接口定义在 Application，实现在 Infrastructure。UseCase 只依赖 Application 层接口。

### 13.7 枚举位置

**问题**：枚举如果定义在 Infrastructure，Application 层无法使用。

**解决**：枚举应定义在 Domain/Enums，所有层都可引用。

---

## 14. 验证清单

完成任务前，检查以下项目：

### 编译与测试
- [ ] `rtk dotnet build ClosetApp.slnx /m:1` 编译通过，0 错误
- [ ] `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1` 全部通过
- [ ] 测试 Fake 已更新（如新增了接口方法）

### 代码质量
- [ ] 新文件放在正确的目录（参见 11.6 文件组织）
- [ ] 接口在 Application 层，实现在 Infrastructure 层
- [ ] 枚举在 Domain 层
- [ ] DI 注册在 App.xaml.cs
- [ ] 命名符合约定（参见 11.1）

### WPF/XAML
- [ ] XAML 使用 DynamicResource 绑定主题资源
- [ ] 没有硬编码颜色/尺寸
- [ ] 卡片组件使用路由事件
- [ ] 新控件使用 DependencyProperties

### 错误处理
- [ ] 使用 WardrobeActionErrorPresenter 处理用户可见错误
- [ ] 使用 ToastService.ShowSuccess/ShowError 反馈操作结果
- [ ] 使用 MessageBox 进行确认对话

### 文档
- [ ] 任何代码行为、业务规则、接口、UI 入口或维护流程变化，都已同步更新对应文档（至少检查 `PROJECT_DOCUMENTATION.md`、`README.md`、`docs/ARCHITECTURE_CONVENTIONS.md`、`AGENTS.md`）
- [ ] 如有新组件/服务/UseCase，已更新 PROJECT_DOCUMENTATION.md
- [ ] 如有架构变更或 AI 编码约束变化，已更新 AGENTS.md

---

## 15. 参考文档

- `README.md`：项目快速入口
- `PROJECT_DOCUMENTATION.md`：详细项目文档
- `docs/ARCHITECTURE_CONVENTIONS.md`：架构约定
