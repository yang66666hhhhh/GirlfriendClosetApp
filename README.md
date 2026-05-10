# GirlfriendClosetApp - 女生衣柜穿搭管理系统

> 更新时间: 2026-05-09
> .NET 8 + WPF + HandyControl + CommunityToolkit.Mvvm
> 四层架构: Domain / Application / Infrastructure / UI

---

## 目录

1. [项目概览](#1-项目概览)
2. [项目结构](#2-项目结构)
3. [技术栈与依赖](#3-技术栈与依赖)
4. [实体模型](#4-实体模型)
5. [枚举定义](#5-枚举定义)
6. [服务层](#6-服务层)
7. [仓储层](#7-仓储层)
8. [数据库配置](#8-数据库配置)
9. [UI层结构](#9-ui层结构)
10. [核心功能流程](#10-核心功能流程)
11. [转换器一览](#11-转换器一览)
12. [样式系统](#12-样式系统)
13. [DI注册](#13-di注册)
14. [启动流程](#14-启动流程)
15. [重要约定](#15-重要约定)
16. [扩展指南](#16-扩展指南)
17. [常见命令](#17-常见命令)
18. [文件路径速查](#18-文件路径速查)
19. [已知问题](#19-已知问题)

---

## 1. 项目概览

**项目类型**: WPF MVVM 桌面应用
**目标用户**: 女生管理个人衣柜和穿搭
**核心功能**: 衣服CRUD、穿搭组合创建编辑、智能推荐、日历穿搭记录、天气穿衣建议

**主要特性**:
- 衣服管理：添加/编辑/删除衣服，上传图片，按类型/标签筛选
- 穿搭管理：创建搭配组合，实时预览搭配效果，编辑/删除搭配
- 智能推荐：根据天气和场景推荐穿搭
- 日历记录：记录每日穿搭，查看穿搭统计（重复率）
- 主题支持：DynamicResource颜色系统，便于后续深色模式扩展

---

## 2. 项目结构

```
GirlfriendClosetApp/
├── ClosetApp.Domain/                    # 领域层：实体、枚举、仓储接口
│   ├── Entities/
│   │   ├── BaseEntity.cs               # 基类：Id, CreatedAt, UpdatedAt
│   │   ├── Clothing.cs                 # 衣服实体
│   │   ├── Outfit.cs                   # 穿搭组合实体
│   │   ├── Tag.cs                      # 标签实体
│   │   ├── ClothingTag.cs              # 衣服-标签多对多关联
│   │   ├── OutfitClothing.cs           # 搭配-衣服多对多关联
│   │   ├── Favorite.cs                 # 收藏实体
│   │   └── OutfitWornRecord.cs         # 穿搭记录实体
│   ├── Enums/
│   │   ├── ClothingType.cs             # 衣服类型枚举
│   │   ├── Season.cs                   # 季节枚举
│   │   └── OutfitScene.cs             # 穿搭场景枚举（新增）
│   └── Interfaces/
│       ├── IRepository.cs              # 通用仓储接口
│       ├── IClothingRepository.cs
│       ├── IOutfitRepository.cs
│       ├── ITagRepository.cs
│       ├── IFavoriteRepository.cs
│       └── IOutfitWornRecordRepository.cs
│
├── ClosetApp.Application/               # 应用层：服务、DTO
│   ├── DTOs/
│   │   ├── OutfitDto.cs
│   │   ├── CreateOutfitDto.cs
│   │   ├── UpdateOutfitDto.cs
│   │   └── OutfitSummaryDto.cs
│   ├── Interfaces/
│   │   ├── IClothingService.cs
│   │   ├── IOutfitService.cs
│   │   ├── ITagService.cs
│   │   ├── IFavoriteService.cs
│   │   └── IOutfitRecommendationService.cs
│   └── Services/
│       ├── ClothingService.cs
│       ├── OutfitService.cs
│       ├── TagService.cs
│       ├── FavoriteService.cs
│       └── OutfitRecommendationService.cs  # 智能推荐算法
│
├── ClosetApp.Infrastructure/            # 基础设施层
│   ├── Data/
│   │   └── ClosetDbContext.cs          # EF Core配置、种子数据
│   ├── Repositories/
│   │   ├── ClothingRepository.cs
│   │   ├── OutfitRepository.cs
│   │   ├── TagRepository.cs
│   │   ├── FavoriteRepository.cs
│   │   └── OutfitWornRecordRepository.cs
│   └── Services/
│       ├── ImageStorageService.cs       # 图片存储（SixLabors.ImageSharp）
│       └── WeatherService.cs            # 天气服务（预留）
│
└── ClosetApp.UI/                       # UI层
    ├── App.xaml / App.xaml.cs         # 应用入口、全局异常处理
    ├── MainWindow.xaml / .cs          # 主窗口
    ├── Themes/
    │   ├── Colors.xaml                # 颜色资源（40+）
    │   └── Styles.xaml                # 样式定义（20+）
    ├── Views/
    │   ├── AddClothingDialog.xaml/.cs # 添加衣服弹窗
    │   ├── EditClothingDialog.xaml/.cs # 编辑衣服弹窗
    │   ├── AddOutfitDialog.xaml/.cs   # 创建穿搭弹窗（重写）
    │   ├── EditOutfitDialog.xaml/.cs  # 编辑穿搭弹窗（新增）
    │   ├── RecordOutfitDialog.xaml/.cs # 记录穿搭弹窗
    │   ├── AddTagDialog.xaml/.cs      # 添加标签弹窗
    │   └── DeleteConfirmDialog.xaml/.cs # 删除确认弹窗
    ├── ViewModels/
    │   ├── MainViewModel.cs           # 主VM（所有Tab状态管理）
    │   ├── WardrobeViewModel.cs       # 衣柜Tab VM
    │   └── HomeViewModel.cs           # 首页Tab VM
    ├── Converters/
    │   ├── BooleanToVisibilityConverter.cs
    │   ├── InverseBoolToVisibilityConverter.cs
    │   ├── NullToVisibilityConverter.cs       # 新增
    │   ├── InverseNullToVisibilityConverter.cs # 新增
    │   ├── ImagePathConverter.cs              # 图片路径转换
    │   ├── EnumDisplayConverter.cs
    │   ├── SeasonToNameConverter.cs
    │   ├── ClothingTypeToNameConverter.cs
    │   ├── OutfitImagesConverter.cs          # 穿搭图片拼图
    │   ├── OutfitPreviewConverter.cs         # 穿搭结构化预览（新增）
    │   ├── OutfitSceneToIconConverter.cs     # 场景转图标
    │   └── OutfitSeasonToIconConverter.cs    # 季节转图标
    └── Services/
        └── ToastService.cs             # Toast服务（带动画）
```

---

## 3. 技术栈与依赖

| 包 | 版本 | 用途 |
|----|------|------|
| .NET | 8.0 | 运行时 |
| HandyControl | 3.5.1 | UI控件库 |
| CommunityToolkit.Mvvm | 8.2.2 | MVVM框架（源码生成） |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.0 | ORM + SQLite |
| SixLabors.ImageSharp | 3.1.4 | 图片处理（调整大小、缩略图） |

---

## 4. 实体模型

### BaseEntity（所有实体基类）
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

### Clothing（衣服）
```csharp
public class Clothing : BaseEntity
{
    public string Name { get; set; } = string.Empty;      // 衣服名称
    public ClothingType Type { get; set; }                // 衣服类型
    public string ImagePath { get; set; } = string.Empty; // 图片路径（相对）
    public string Color { get; set; } = string.Empty;     // 颜色描述
    public int WarmLevel { get; set; }                    // 保暖等级（1-5）
    public int FavoriteLevel { get; set; }                // 喜爱程度（1-5）
    public DateTime? PurchaseDate { get; set; }           // 购买日期
    public bool IsFavorite { get; set; }                 // 是否收藏

    public ICollection<ClothingTag> ClothingTags { get; set; } = new List<ClothingTag>();
}
```

### Outfit（穿搭组合）
```csharp
public class Outfit : BaseEntity
{
    public string Name { get; set; } = string.Empty;      // 搭配名称
    public OutfitScene Scene { get; set; }                // 场景（枚举）
    public Season Season { get; set; }                   // 季节（枚举）
    public int Rating { get; set; }                      // 满意度评分（1-5）
    public string? Notes { get; set; }                   // 备注
    public DateTime? WornDate { get; set; }              // 最后穿着日期
    public int WearCount { get; set; }                  // 穿着次数

    public ICollection<OutfitClothing> OutfitClothes { get; set; } = new List<OutfitClothing>();
    public ICollection<OutfitWornRecord> WornRecords { get; set; } = new List<OutfitWornRecord>();
}
```

### OutfitClothing（搭配-衣服关联）
```csharp
public class OutfitClothing
{
    public Guid OutfitId { get; set; }
    public Outfit Outfit { get; set; } = null!;
    public Guid ClothingId { get; set; }
    public Clothing Clothing { get; set; } = null!;
}
```

### OutfitWornRecord（穿搭穿着记录）
```csharp
public class OutfitWornRecord : BaseEntity
{
    public Guid OutfitId { get; set; }
    public Outfit Outfit { get; set; } = null!;
    public DateTime WornDate { get; set; }               // 穿着日期
    public string? Notes { get; set; }                  // 备注
}
```

### Tag（标签）
```csharp
public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public TagCategory Category { get; set; }            // 类别（风格/场景/季节）
}
```

### ClothingTag（衣服-标签关联）
```csharp
public class ClothingTag
{
    public Guid ClothingId { get; set; }
    public Clothing Clothing { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
```

---

## 5. 枚举定义

### ClothingType（衣服类型）
```csharp
public enum ClothingType
{
    Top,        // 上衣
    Bottom,     // 下装
    Dress,      // 连衣裙
    Outerwear,  // 外套
    Shoes,      // 鞋子
    Accessory   // 配饰
}
```

### Season（季节）
```csharp
public enum Season
{
    Spring,     // 春
    Summer,     // 夏
    Autumn,     // 秋
    Winter,     // 冬
    AllSeason   // 四季皆宜
}
```

### OutfitScene（穿搭场景）【新增】
```csharp
public enum OutfitScene
{
    Date,       // 约会
    Work,       // 上班
    Travel,     // 出游
    Party,      // 派对
    Casual      // 休闲
}
```

### TagCategory（标签类别）
```csharp
public enum TagCategory
{
    Style,      // 风格（韩系、通勤、可爱、辣妹、休闲）
    Scene,      // 场景（约会、上班、出游、派对）
    Season      // 季节（春、夏、秋、冬）
}
```

---

## 6. 服务层

### IClothingService / ClothingService
```csharp
// 方法
Task<IEnumerable<Clothing>> GetAllClothesAsync();
Task<Clothing?> GetClothingByIdAsync(Guid id);
Task<Clothing> AddClothingAsync(Clothing clothing);
Task UpdateClothingAsync(Clothing clothing);
Task DeleteClothingAsync(Guid id);
Task<IEnumerable<Clothing>> GetClothesByTypeAsync(ClothingType type);
Task<IEnumerable<Clothing>> SearchClothesAsync(string keyword);
```

### IOutfitService / OutfitService
```csharp
// 方法
Task<IEnumerable<Outfit>> GetAllOutfitsAsync();
Task<Outfit?> GetOutfitByIdAsync(Guid id);
Task<Outfit> AddOutfitAsync(Outfit outfit);
Task UpdateOutfitAsync(Outfit outfit);
Task DeleteOutfitAsync(Guid id);
Task<IEnumerable<Outfit>> GetOutfitsBySceneAsync(OutfitScene scene);
Task<IEnumerable<Outfit>> GetRecentlyWornOutfitsAsync(int count);
Task RecordWornDateAsync(Guid outfitId, DateTime date);  // 记录穿着
```

### IOutfitRecommendationService / OutfitRecommendationService
```csharp
// 方法
Task<Outfit?> GetRecommendationAsync(int temperature, OutfitScene? scene = null);
Task<IEnumerable<Outfit>> GetRecommendationsByRuleAsync(int temperature, OutfitScene? scene = null);
Task<IEnumerable<Outfit>> GetLowWearOutfitsAsync(int count = 5);
Task<IEnumerable<Outfit>> GetUnwornOutfitsAsync();

// 推荐算法评分规则
// Score = Rating*10 + SeasonMatch(±30) + SceneMatch(+25) + WearCountBonus(0-10)
```

### ITagService / TagService
```csharp
// 方法
Task<IEnumerable<Tag>> GetAllTagsAsync();
Task<Tag> AddTagAsync(Tag tag);
Task DeleteTagAsync(Guid id);
Task<IEnumerable<Tag>> GetTagsByCategoryAsync(TagCategory category);
```

### IImageStorageService / ImageStorageService
```csharp
// 方法
Task<string> SaveImageAsync(string sourcePath);    // 保存并返回相对路径
Task<string> SaveThumbnailAsync(string sourcePath, int maxSize = 200);
Task DeleteImageAsync(string imagePath);
Task DeleteImageWithThumbnailAsync(string imagePath);
string GetImageFullPath(string relativePath);     // 转为绝对路径
string GetThumbnailFullPath(string relativePath);

// 存储路径
// %LOCALAPPDATA%/ClosetApp/images/
// %LOCALAPPDATA%/ClosetApp/thumbnails/
```

---

## 7. 仓储层

### IRepository<T>（通用接口）
```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}
```

### 特殊仓储方法

**IOutfitRepository**
```csharp
Task<IEnumerable<Outfit>> GetBySceneAsync(OutfitScene scene);
Task<IEnumerable<Outfit>> GetBySeasonAsync(Season season);
Task<IEnumerable<Outfit>> GetRecentlyWornAsync(int count);
Task<Outfit?> GetWithClothesAsync(Guid id);  // 预加载OutfitClothes和Clothing
```

**IOutfitWornRecordRepository**
```csharp
Task<IEnumerable<OutfitWornRecord>> GetByDateRangeAsync(DateTime start, DateTime end);
Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId);
Task<OutfitWornRecord?> GetByDateAsync(DateTime date);
```

---

## 8. 数据库配置

### DbContext 实体配置

```csharp
// DbSets
DbSet<Clothing> Clothes
DbSet<Outfit> Outfits
DbSet<Tag> Tags
DbSet<ClothingTag> ClothingTags    // 多对多联接表
DbSet<OutfitClothing> OutfitClothes // 多对多联接表
DbSet<Favorite> Favorites
DbSet<OutfitWornRecord> OutfitWornRecords

// 关系配置
ClothingTag: composite PK (ClothingId, TagId), 分别指向Clothing和Tag
OutfitClothing: composite PK (OutfitId, ClothingId), 分别指向Outfit和Clothing
Favorite: unique index (UserId, TargetType, TargetId), 指向Outfit或Clothing
OutfitWornRecord: cascade delete on Outfit
```

### 种子数据（预设标签）
```csharp
// 风格标签
韩系, 通勤, 可爱, 辣妹, 休闲

// 场景标签
约会, 上班, 出游, 派对

// 季节标签
春, 夏, 秋, 冬
```

### 数据库位置
```
%LOCALAPPDATA%\ClosetApp\closet.db
```

---

## 9. UI层结构

### 主窗口布局（MainWindow.xaml）
```
┌─────────────────────────────────────────────────────────────┐
│ Header: 👗 我的衣橱                                         │
├─────────────────────────────────────────────────────────────┤
│ hc:TabControl                                              │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 👔 衣柜 │ 👔 搭配 │ 📅 日历 │ 🏠 首页 │               │ │
│ ├─────────────────────────────────────────────────────────┤ │
│ │                                                         │ │
│ │  TabContent (根据选中Tab显示)                           │ │
│ │                                                         │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Tab 1: 衣柜（Clothes）
- 筛选栏：搜索框、类型下拉、标签下拉
- 衣服卡片网格（WrapPanel）：图片 + 名称 + 颜色 + 类型
- Hover显示编辑/删除按钮
- 空状态提示

### Tab 2: 搭配（Outfits）【重写】
- 创建搭配按钮
- 搭配卡片网格：图片拼图 + 名称 + 场景图标 + 季节图标 + 评分
- Hover显示编辑/删除按钮
- 空状态提示

### Tab 3: 日历（Calendar）【修复】
- 左侧：日历控件 + 记录今日穿搭按钮 + 月度统计（穿搭次数、重复率）
- 右侧：该日穿搭列表

### Tab 4: 首页（Home）
- 天气卡片：问候语 + 温度 + 天气状况
- 推荐穿搭卡片：刷新推荐按钮 + 推荐列表
- 快捷操作：添加衣服、创建搭配、管理标签

### 弹窗（Dialogs）

**AddOutfitDialog（创建穿搭）【重写800x640】**
```
┌─────────────────────────────────────────────────────────────┐
│ 创建搭配                                              [✕]  │
│ 选择衣服并查看搭配效果                                     │
├──────────────────────────────┬────────────────────────────┤
│ 搭配名称: [____________]     │                            │
│ 场景: [▼ 休闲    ] 季节: [▼ 春] │    ┌────────────────┐  │
│ 满意度: ★★★★★              │    │    上衣位置      │  │
│                              │    ├────────────────┤  │
│ 👕 上衣                      │    │    下装位置      │  │
│ [ ✓白色T恤 ] [ 蓝色衬衫 ]    │    ├────────────────┤  │
│                              │    │    鞋子位置      │  │
│ 👖 下装                      │    ├────────────────┤  │
│ [ ✓黑色裤子 ]                │    │    配饰位置      │  │
│                              │    └────────────────┘  │
│ 👟 鞋子                      │                            │
│ [ 白色运动鞋 ]               │                            │
├──────────────────────────────┴────────────────────────────┤
│                              [取消]  [创建搭配]            │
└─────────────────────────────────────────────────────────────┘
```

**EditOutfitDialog（编辑穿搭）【新增】**
- 布局同AddOutfitDialog
- 预填已有搭配信息
- 预选已关联的衣服
- 保存按钮文字为"保存修改"

**RecordOutfitDialog（记录穿搭）【已修复】**
- 显示日期 + 搭配列表
- 选择搭配后确认

---

## 10. 核心功能流程

### 添加衣服流程
```
用户点击"添加衣服"
→ MainViewModel.OpenAddClothingDialogCommand
→ new AddClothingDialog + AddClothingViewModel
→ AddClothingViewModel.SelectImageCommand → OpenFileDialog选择图片
→ AddClothingViewModel.ConfirmCommand
  → IClothingService.AddClothingAsync
  → IImageStorageService.SaveImageAsync (保存到%LOCALAPPDATA%\ClosetApp\images\)
  → ToastService.ShowSuccess("衣服添加成功")
→ MainViewModel.LoadDataCommand.Execute(null)
```

### 创建穿搭流程【重写】
```
用户点击"创建搭配"
→ MainViewModel.OpenAddOutfitDialogCommand
→ new AddOutfitDialog + AddOutfitViewModel
→ 点击左侧衣服卡片 → ClothingItem.IsSelected = !IsSelected
  → OnIsSelectedChanged触发 → UpdatePreview()
  → 右侧预览区实时显示选中衣服图片
→ AddOutfitViewModel.ConfirmCommand
  → IOutfitService.AddOutfitAsync
  → ToastService.ShowSuccess("搭配添加成功")
→ MainViewModel.LoadDataCommand.Execute(null)
```

### 记录穿搭流程【修复】
```
用户选择日历日期 + 点击"记录今日穿搭"
→ MainViewModel.RecordOutfitCommand（修复：不再调用OpenAddOutfitDialog）
→ new RecordOutfitDialog(allOutfits, SelectedDate)
→ 用户选择搭配 + 确认
  → IOutfitService.RecordWornDateAsync(outfitId, date)
    → 创建OutfitWornRecord记录（不再覆盖WornDate）
    → 更新Outfit.WornDate和WearCount
  → ToastService.ShowSuccess("穿搭记录已保存")
→ MainViewModel.LoadDataCommand.Execute(null)
```

### 智能推荐流程
```
用户点击"刷新推荐"
→ MainViewModel.GetRecommendationCommand
→ IOutfitRecommendationService.GetRecommendationsByRuleAsync(temperature, scene)
  → 遍历所有Outfit，计算评分
  → Score = Rating*10 + SeasonMatch(±30) + SceneMatch(+25) + WearCountBonus(0-10)
  → 按评分降序返回前5个
→ 显示在首页推荐区
```

---

## 11. 转换器一览

| 转换器 | 命名空间 | 用途 |
|--------|----------|------|
| `ImagePathConverter` | ClosetApp.UI.Converters | 将相对图片路径转为BitmapImage |
| `BooleanToVisibilityConverter` | System.Windows.Controls | bool→Visibility |
| `InverseBoolToVisibilityConverter` | ClosetApp.UI.Converters | bool→Visibility（反转） |
| `NullToVisibilityConverter` | ClosetApp.UI.Converters | 非null→Visible（新增） |
| `InverseNullToVisibilityConverter` | ClosetApp.UI.Converters | null→Visible（新增） |
| `OutfitImagesConverter` | ClosetApp.UI.Converters | OutfitClothes→前3张BitmapImage列表 |
| `OutfitPreviewConverter` | ClosetApp.UI.Converters | OutfitClothes→结构化预览（Top/Bottom/Shoes/Accessory）（新增） |
| `OutfitSceneToIconConverter` | ClosetApp.UI.Converters | OutfitScene→emoji图标 |
| `OutfitSeasonToIconConverter` | ClosetApp.UI.Converters | Season→emoji图标 |

---

## 12. 样式系统

### 颜色资源（Colors.xaml）
```xml
<!-- 主色 -->
<SolidColorBrush x:Key="PrimaryBrush" Color="#0078D4"/>
<SolidColorBrush x:Key="PrimaryLightBrush" Color="#E3F2FD"/>
<SolidColorBrush x:Key="PrimaryDarkBrush" Color="#005A9E"/>

<!-- 背景 -->
<SolidColorBrush x:Key="BackgroundBrush" Color="#F5F5F5"/>
<SolidColorBrush x:Key="CardBackgroundBrush" Color="#FFFFFF"/>
<SolidColorBrush x:Key="SecondaryBrush" Color="#F8F9FA"/>

<!-- 文字 -->
<SolidColorBrush x:Key="TextPrimaryBrush" Color="#1A1A1A"/>
<SolidColorBrush x:Key="TextSecondaryBrush" Color="#666666"/>
<SolidColorBrush x:Key="TextTertiaryBrush" Color="#999999"/>

<!-- 边框 -->
<SolidColorBrush x:Key="BorderBrush" Color="#E0E0E0"/>
<SolidColorBrush x:Key="BorderFocusBrush" Color="#0078D4"/>
```

### 样式定义（Styles.xaml）
```xml
<!-- 按钮样式 -->
<Style x:Key="ButtonPrimary" TargetType="Button">   <!-- 主按钮 -->
<Style x:Key="ButtonSecondary" TargetType="Button"> <!-- 次按钮 -->
<Style x:Key="ButtonDanger" TargetType="Button">   <!-- 危险操作 -->
<Style x:Key="ButtonIcon" TargetType="Button">    <!-- 图标按钮 -->

<!-- 卡片样式 -->
<Style x:Key="Card" TargetType="Border">          <!-- 通用卡片 -->
<Style x:Key="CardHover" TargetType="Border">     <!-- 可悬停卡片 -->

<!-- 表单样式 -->
<Style x:Key="FormField" TargetType="hc:TextBox">
<Style x:Key="FormComboBox" TargetType="ComboBox">
<Style x:Key="FormLabel" TargetType="TextBlock">

<!-- 对话框样式 -->
<Style x:Key="DialogTitle" TargetType="TextBlock">
<Style x:Key="DialogFooter" TargetType="Border">
```

---

## 13. DI注册

```csharp
// App.xaml.cs ConfigureServices()

// DbContext
services.AddDbContext<ClosetDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 仓储
services.AddScoped<IClothingRepository, ClothingRepository>();
services.AddScoped<IOutfitRepository, OutfitRepository>();
services.AddScoped<ITagRepository, TagRepository>();
services.AddScoped<IFavoriteRepository, FavoriteRepository>();
services.AddScoped<IOutfitWornRecordRepository, OutfitWornRecordRepository>();

// 服务
services.AddScoped<IClothingService, ClothingService>();
services.AddScoped<IOutfitService, OutfitService>();
services.AddScoped<ITagService, TagService>();
services.AddScoped<IFavoriteService, FavoriteService>();
services.AddScoped<IOutfitRecommendationService, OutfitRecommendationService>();
services.AddSingleton<IImageStorageService, ImageStorageService>();
services.AddSingleton<IWeatherService, WeatherService>();

// ViewModels
services.AddTransient<MainViewModel>();
services.AddTransient<HomeViewModel>();
services.AddTransient<WardrobeViewModel>();
```

---

## 14. 启动流程

```
App.OnStartup()
  → RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly  // 强制软件渲染
  → AppDomain.CurrentDomain.UnhandledException += OnUnhandledException
  → DispatcherUnhandledException += OnDispatcherUnhandledException
  → TaskScheduler.UnobservedTaskException += OnUnobservedTaskException
  → ConfigureServices()        // 注册所有DI服务
  → Services.BuildServiceProvider()
  → InitializeDatabase()        // DbContext.Database.EnsureCreated()
  → new MainWindow()
  → mainWindow.DataContext = Services.GetRequiredService<MainViewModel>()
  → mainWindow.Show()
  → ToastService.Instance      // 初始化Toast服务
```

---

## 15. 重要约定

### 1. ViewModel模式
- 继承 `ObservableObject`（CommunityToolkit.Mvvm）
- 命令使用 `[RelayCommand]` 特性
- 可观察属性使用 `[ObservableProperty]` 特性

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [RelayCommand]
    private void DoSomething() { }
}
```

### 2. 图片路径约定
- `ImageStorageService.SaveImageAsync` 返回相对路径：`filename.jpg`
- `ImagePathConverter` 将相对路径转为绝对路径并加载BitmapImage
- 图片存储位置：`%LOCALAPPDATA%\ClosetApp\images\`

### 3. XAML资源使用
- 所有颜色使用 `DynamicResource` 引用 Colors.xaml 中的资源
- 样式使用 `DynamicResource` 引用 Styles.xaml 中的资源
- 转换器在需要使用的视图的 `Window.Resources` 中注册

### 4. 对话框模式
- 所有对话框继承 `Window`，使用 `ShowDialog()` 显示
- 对话框关闭通过 `CloseAction` 回调传递结果
- 对话框结果通过 `DialogResult` 属性返回

```csharp
vm.CloseAction += result =>
{
    dialog.DialogResult = result != null;
    dialog.Close();
    if (result != null) { /* 处理结果 */ }
};
```

### 5. 异常处理
- `DispatcherUnhandledException` 捕获UI线程异常，显示MessageBox后标记为已处理
- 使用 `_errorShown` 标志防止重复弹窗

---

## 16. 扩展指南

### 添加新实体
1. `Domain/Entities/` - 创建实体类继承 `BaseEntity`
2. `Domain/Interfaces/` - 创建 `I{Xxx}Repository` 接口
3. `Infrastructure/Repositories/` - 创建 `XxxRepository` 实现
4. `Infrastructure/Data/ClosetDbContext.cs` - 添加 `DbSet` 和关系配置
5. `Application/Interfaces/` - 创建 `I{Xxx}Service` 接口
6. `Application/Services/` - 创建 `XxxService` 实现
7. `App.xaml.cs` - 注册 DI
8. `UI/Views/` - 创建 XAML 页面（如果需要）

### 添加新页面
1. 在 `UI/Views/` 创建 `NewPage.xaml`
2. 在 `UI/ViewModels/` 创建 `NewPageViewModel.cs`
3. 在 `MainWindow.xaml` 的 TabControl 中添加 TabItem

### 添加新转换器
1. 在 `UI/Converters/` 创建 `MyConverter.cs` 实现 `IValueConverter`
2. 在需要使用的视图的 `Window.Resources` 中注册：
   ```xml
   <Window.Resources>
       <converters:MyConverter x:Key="MyConverter"/>
   </Window.Resources>
   ```

### 修改数据库（Schema变更）
```powershell
# 删除旧数据库
Remove-Item "$env:LOCALAPPDATA\ClosetApp\closet.db" -Force
# 运行应用，会自动创建新数据库
dotnet run --project ClosetApp.UI
```

---

## 17. 常见命令

```bash
# 运行项目
dotnet run --project ClosetApp.UI

# 构建项目
dotnet build

# 清理并重新构建
dotnet clean && dotnet build

# 重新初始化数据库
Remove-Item "$env:LOCALAPPDATA\ClosetApp\closet.db" -Force
dotnet run --project ClosetApp.UI

# 查看构建错误
dotnet build 2>&1 | Select-String "error"
```

---

## 18. 文件路径速查

| 功能 | 路径 |
|------|------|
| 应用入口 | `ClosetApp.UI/App.xaml.cs` |
| 主窗口 | `ClosetApp.UI/MainWindow.xaml` |
| 主VM | `ClosetApp.UI/ViewModels/MainViewModel.cs` |
| 衣柜VM | `ClosetApp.UI/ViewModels/WardrobeViewModel.cs` |
| 首页VM | `ClosetApp.UI/ViewModels/HomeViewModel.cs` |
| 添加衣服弹窗 | `ClosetApp.UI/Views/AddClothingDialog.xaml/.cs` |
| 编辑衣服弹窗 | `ClosetApp.UI/Views/EditClothingDialog.xaml/.cs` |
| 创建穿搭弹窗【重写】 | `ClosetApp.UI/Views/AddOutfitDialog.xaml/.cs` |
| 编辑穿搭弹窗【新增】 | `ClosetApp.UI/Views/EditOutfitDialog.xaml/.cs` |
| 记录穿搭弹窗 | `ClosetApp.UI/Views/RecordOutfitDialog.xaml/.cs` |
| 添加标签弹窗 | `ClosetApp.UI/Views/AddTagDialog.xaml/.cs` |
| 删除确认弹窗 | `ClosetApp.UI/Views/DeleteConfirmDialog.xaml/.cs` |
| 颜色资源 | `ClosetApp.UI/Themes/Colors.xaml` |
| 样式资源 | `ClosetApp.UI/Themes/Styles.xaml` |
| Toast服务 | `ClosetApp.UI/Services/ToastService.cs` |
| 图片存储 | `ClosetApp.Infrastructure/Services/ImageStorageService.cs` |
| 数据库Context | `ClosetApp.Infrastructure/Data/ClosetDbContext.cs` |
| 智能推荐 | `ClosetApp.Application/Services/OutfitRecommendationService.cs` |
| 数据库文件 | `%LOCALAPPDATA%\ClosetApp\closet.db` |
| 图片目录 | `%LOCALAPPDATA%\ClosetApp\images\` |
| 异常日志 | `%LOCALAPPDATA%\ClosetApp\error.log` |

---

## 19. 已知问题

### 已修复（需重新测试）
1. ~~`UCEERR_MISSINGENDCOMMAND` 错误~~ - 移除透明窗口 + 软件渲染模式
2. ~~衣服预览图片路径错误~~ - ImageStorageService返回路径格式修复
3. ~~GradientStop.Color类型错误~~ - 直接使用Color值而非Brush
4. ~~穿搭记录打开错误对话框~~ - RecordOutfit正确打开RecordOutfitDialog
5. ~~日历统计使用WornDate~~ - 改为使用WornRecords表查询
6. ~~衣服选择在ItemsControl中不工作~~ - AddOutfitDialog重写为ListBox+DataTemplate
7. ~~搭配预览不更新~~ - NullToVisibilityConverter修复
8. ~~OutfitViewModel/CalendarViewModel死代码~~ - 已删除

### 待优化
1. SixLabors.ImageSharp 3.1.4 已知安全漏洞（NU1903警告）
2. 收藏功能实体和服务已创建，但UI未集成
3. 天气服务预留，尚未对接真实API

---

## 更新日志

### 2026-05-09
- 新增OutfitScene枚举，Outfit.Scene从string改为枚举
- RecordWornDateAsync改为创建OutfitWornRecord记录而非覆盖WornDate
- 重写AddOutfitDialog：左侧衣服选择+右侧实时预览
- 新增EditOutfitDialog：编辑搭配
- 重写搭配卡片：图片拼图+场景/季节图标+编辑/删除按钮
- 新增OutfitImagesConverter、OutfitSceneToIconConverter、OutfitSeasonToIconConverter
- 新增NullToVisibilityConverter、InverseNullToVisibilityConverter
- 修复RecordOutfit命令打开错误对话框
- 修复日历统计和查询使用WornRecords而非WornDate
- 删除OutfitViewModel和CalendarViewModel死代码
- 移除所有AllowsTransparency="True"窗口以修复UCEERR错误
