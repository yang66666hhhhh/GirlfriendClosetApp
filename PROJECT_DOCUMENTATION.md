# GirlfriendClosetApp 项目文档 (详细版)

> 最后更新时间: 2026-05-10
> 项目状态: 功能基本完成，UI正在迭代优化

---

## 一、项目概述

### 1.1 项目定位

GirlfriendClosetApp（女朋友衣柜）是一款面向女性的私人穿搭收藏管理应用。核心价值在于帮助用户：

- **收藏管理**：整理记录喜欢的衣服
- **搭配组合**：将衣服组合成 outfit（穿搭）
- **场景推荐**：根据场景（上班/约会/派对）推荐搭配
- **情感价值**：提供类似 Pinterest/小红书 的视觉浏览体验

### 1.2 技术栈

| 层级 | 技术 | 说明 |
|-----|------|-----|
| UI | WPF (.NET 10) | 桌面客户端 |
| UI框架 | HandyControl | 辅助UI组件库 |
| 架构 | MVVM | CommunityToolkit.Mvvm |
| 数据访问 | Entity Framework Core | ORM |
| 数据库 | SQLite | 本地存储 |
| 依赖注入 | Microsoft.Extensions.DependencyInjection | 服务容器 |

### 1.3 项目结构

```
GirlfriendClosetApp/
├── ClosetApp.Domain/           # 领域层：实体、枚举、仓储接口
├── ClosetApp.Application/      # 应用层：服务接口、业务逻辑
├── ClosetApp.Infrastructure/     # 基础设施层：EF Core、SQLite、文件服务
└── ClosetApp.UI/               # 表现层：WPF视图、ViewModel、Converter
```

---

## 二、领域模型

### 2.1 实体关系图

```
┌─────────────┐     ┌─────────────────┐     ┌─────────────┐
│  Clothing   │────│  ClothingTag    │────│     Tag    │
│  (衣服)     │    │   (多对多关联)   │     │   (标签)   │
└─────────────┘     └─────────────────┘     └─────────────┘
       │
       │ 1:N
       ▼
┌─────────────────┐     ┌─────────────────┐
│  OutfitClothing  │────│     Outfit       │
│   (穿搭组合)      │     │    (穿搭)        │
└─────────────────┘     └─────────────────┘
                               │
                               │ 1:N
                               ▼
                        ┌─────────────────┐
                        │ Favorite/WornRecord
                        │ (收藏/穿着记录)
                        └─────────────────┘
```

### 2.2 核心实体

#### Clothing (衣服)
| 属性 | 类型 | 说明 |
|-----|------|-----|
| Id | Guid | 主键 |
| Name | string | 衣服名称 |
| Type | ClothingType | 类型（上衣/下装/外套/裙子/鞋子/配饰） |
| Season | Season | 季节（春夏秋冬/四季） |
| ImagePath | string? | 图片路径 |
| Color | string? | 颜色名称 |
| Brand | string? | 品牌 |
| Notes | string? | 备注 |
| IsFavorite | bool | 是否收藏 |
| FavoriteLevel | int | 收藏级别（0-3，显示为星级） |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |

#### Outfit (穿搭)
| 属性 | 类型 | 说明 |
|-----|------|-----|
| Id | Guid | 主键 |
| Name | string | 穿搭名称 |
| Scene | OutfitScene | 场景（上班/约会/出游/派对/日常） |
| Season | Season | 适合季节 |
| Rating | int | 评分（1-3星） |
| Notes | string? | 备注 |
| WornDate | DateTime? | 上次穿着日期 |
| WearCount | int | 穿着次数 |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |

#### Tag (标签)
| 属性 | 类型 | 说明 |
|-----|------|-----|
| Id | Guid | 主键 |
| Name | string | 标签名称 |
| Color | string | 标签颜色（hex） |

---

## 三、服务架构

### 3.1 服务接口

#### IClothingService
```csharp
Task<IEnumerable<Clothing>> GetAllClothesAsync();
Task<Clothing?> GetClothingByIdAsync(Guid id);
Task<Clothing> AddClothingAsync(Clothing clothing);
Task UpdateClothingAsync(Clothing clothing);
Task DeleteClothingAsync(Guid id);
Task<IEnumerable<Clothing>> GetClothesByTypeAsync(ClothingType type);
Task<IEnumerable<Clothing>> SearchClothesAsync(string keyword);
```

#### IOutfitService
```csharp
Task<IEnumerable<Outfit>> GetAllOutfitsAsync();
Task<Outfit?> GetOutfitByIdAsync(Guid id);
Task<Outfit> AddOutfitAsync(Outfit outfit);
Task UpdateOutfitAsync(Outfit outfit);
Task DeleteOutfitAsync(Guid id);
Task<IEnumerable<Outfit>> GetOutfitsBySceneAsync(OutfitScene scene);
Task<IEnumerable<Outfit>> GetRecentlyWornOutfitsAsync(int count);
Task RecordWornDateAsync(Guid outfitId, DateTime wornDate);
```

#### IImageStorageService
```csharp
Task<string> SaveImageAsync(string sourcePath);
Task<string> SaveThumbnailAsync(string sourcePath, int maxSize = 200);
Task<string> SaveThumbnailForImageAsync(string imageFileName, string sourcePath, int maxSize = 200);
Task DeleteImageAsync(string imagePath);
Task DeleteImageWithThumbnailAsync(string imagePath);
string GetImageFullPath(string relativePath);
string GetThumbnailFullPath(string relativePath);
```

### 3.2 服务实现注意事项

**ImageStorageService 当前问题：**
- `SaveThumbnailAsync` 和 `SaveThumbnailForImageAsync` 只是复制文件，并未真正压缩图片
- 建议后续使用 SixLabors.ImageSharp 进行实际图片压缩

---

## 四、UI架构

### 4.1 主窗口结构

```
MainWindow
├── NavigationSidebar (侧边导航，可折叠)
│   ├── 衣柜按钮
│   ├── 搭配按钮
│   ├── 标签按钮
│   └── 折叠/展开按钮
├── Content Area (内容区域)
│   ├── ClothesTab (衣柜页 - 默认显示)
│   ├── OutfitsTab (搭配页)
│   └── TagsTab (标签页)
├── SidebarOverlay (侧边栏遮罩)
└── SidebarPanel (添加穿搭面板)
```

### 4.2 侧边栏折叠

- **展开宽度**: 220px
- **折叠宽度**: 72px
- **动画时长**: 240ms, CubicEaseOut
- **折叠时**: 图标保留 + Tooltip 显示菜单名

### 4.3 页面布局 (ClothesTab)

```
┌──────────────────────────────────────────────────────┐
│ Header: [我的衣柜 8件]  [搜索框]  [+ 添加衣服]    │
├──────────────────────────────────────────────────────┤
│ Hero Banner (200px, 渐变背景)                       │
│ "今天也来整理漂亮衣柜 ✨" + "开始整理"按钮        │
├──────────────────────────────────────────────────────┤
│ Quick Actions: [添加衣服] [今日搭配] [最近收藏]    │
├──────────────────────────────────────────────────────┤
│ 最近添加 ──────────────────────────────→              │
│ [卡片][卡片][卡片][卡片][卡片][卡片]               │
├──────────────────────────────────────────────────────┤
│ 最近喜欢 ──────────────────────────→                │
│ [卡片][卡片][卡片][卡片]                           │
├──────────────────────────────────────────────────────┤
│ 所有衣服 (瀑布流 WrapPanel)                        │
│ [卡][卡][卡][卡]                                 │
│ [卡][卡][卡]                                     │
└──────────────────────────────────────────────────────┘
```

### 4.4 卡片设计 (PremiumClothingCard) V2

```
┌────────────────────────┐
│                        │
│                        │
│     UniformToFill      │  ← 图片自动裁切，84%高度
│     自动裁切主视觉      │  ← 顶部20px圆角
│                        │
│                 ♥ ⋯   │  ← Hover显示操作按钮
│                        │
├────────────────────────┤
│ 奶油毛衣                │  ← 标题14px
│ 春季 · 米白 · 温柔风    │  ← Meta: 季节·颜色·风格
└────────────────────────┘
```

**卡片尺寸 (V2):**
| 类型 | 宽度 | 总高度 | 图片高度 |
|-----|------|-------|---------|
| 上衣 | 230 | 280 | 230 |
| 裙子 | 230 | 340 | 290 |
| 外套 | 230 | 320 | 270 |
| 下装 | 230 | 260 | 210 |
| 鞋子 | 230 | 240 | 190 |
| 配饰 | 230 | 220 | 170 |

**操作按钮:**
- ♥ 收藏按钮：毛玻璃圆形，Hover脉冲动画
- ⋯ 更多按钮：点击展开菜单（编辑/删除）

---

## 五、当前存在的问题

### 5.1 严重问题 (Critical)

#### 问题 1: Favorite 状态未持久化
- **描述**: `PremiumClothingCard.Favorite_Click` 更新了 `FavoriteLevel`，但未调用服务持久化到数据库
- **影响**: 用户切换收藏状态后重启应用，状态丢失
- **修复建议**: 在 ViewModel 中添加 `UpdateFavoriteAsync` 方法，或在卡片点击时调用 service

#### 问题 2: 图片未真正压缩
- **描述**: `ImageStorageService.SaveThumbnailAsync` 只是复制原图，未使用 SixLabors.ImageSharp 压缩
- **影响**: 缩略图和原图一样大，浪费存储空间，加载慢
- **修复建议**: 使用 SixLabors.ImageSharp 压缩到 maxSize (如 200px)

#### 问题 3: EditClothingDialog 缺少 Skirt 类型
- **描述**: 编辑对话框的类型选择器处理了 Top/Bottom/Outerwear/Dress/Shoes/Accessory，但 ClothingType 枚举中有 Skirt 未处理
- **影响**: 如果衣服类型是 Skirt，编辑时类型会显示为空或错误
- **修复建议**: 在 EditClothingDialog.xaml.cs 中添加 Skirt case

#### 问题 4: TagsTab 无删除功能
- **描述**: TagsTab 只有 `AddTag_Click`，没有删除标签的逻辑
- **影响**: 无法删除已创建的标签
- **修复建议**: 添加删除确认对话框和删除逻辑

### 5.2 功能缺失 (Feature Gaps)

#### 问题 5: 无真正的天气推荐
- **描述**: `WeatherService` 是 mock 实现，始终返回 22°C 晴天
- **影响**: Hero 区域的天气信息是假的
- **建议**: 接入真实天气 API（如和风天气）或移除天气显示

#### 问题 6: 无 Outfit 图片展示
- **描述**: OutfitsTab 只显示名称、场景图标、季节图标，没有显示穿搭的实际图片
- **影响**: 用户无法直观看到搭配效果
- **建议**: 通过 OutfitClothing 关联获取衣服图片组合展示

#### 问题 7: 无多条件筛选
- **描述**: 当前只能按类型或搜索筛选，无法同时按季节+类型+标签筛选
- **影响**: 数据多时难以找到想要的衣服
- **建议**: 实现高级筛选面板

#### 问题 8: 无数据导出/导入
- **描述**: 没有备份和恢复功能
- **影响**: 换设备或重装后数据丢失
- **建议**: 实现 JSON/Excel 格式的导入导出

### 5.3 代码质量问题 (Code Quality)

#### 问题 9: StyleDisplay 硬编码中文逻辑
- **描述**: `Clothing.StyleDisplay` 属性使用中文关键词判断风格（毛衣→温柔风），不维护且只支持中文
- **影响**: 非中文命名的衣服风格显示不准确
- **建议**: 迁移到 Tags 系统，用标签代替自动判断

#### 问题 10: 对话框代码behind过重
- **描述**: AddClothingDialog、EditClothingDialog 等对话框逻辑大多在 code-behind
- **影响**: 难以测试，业务逻辑分散
- **建议**: 迁移到 ViewModel 模式

#### 问题 11: ToastService 使用全局静态实例
- **描述**: `ToastService.Instance` 是静态单例，违反依赖注入原则
- **影响**: 难以 mock 测试
- **建议**: 改为依赖注入 `IToastService`

#### 问题 12: 重复 Converter
- **描述**: `LevelToActive`、`LevelToActive2`、`LevelToActive3` 三个 Converter 功能重复
- **影响**: 代码冗余
- **建议**: 合并为带参数的 `LevelToActiveConverter(level)`

### 5.4 UI/UX 问题

#### 问题 13: 搜索框无实时筛选
- **描述**: ClothesTab 的搜索框绑定到 ViewModel.SearchText，但筛选逻辑未实现
- **影响**: 用户输入搜索内容后不会自动过滤
- **修复**: 已在 `UpdateWaterfallColumns()` 中实现

#### 问题 14: 最近添加/喜欢 区域无数据时仍显示
- **描述**: 当没有数据时，这些区域应该隐藏或显示空状态
- **影响**: 页面有空白区域
- **建议**: 添加 `Visibility="{Binding HasRecentlyAdded, Converter=...}"` 控制

#### 问题 15: 瀑布流高度差异化后 WrapPanel 可能出现列不平衡
- **描述**: 不同高度卡片混合时，WrapPanel 从左到右排列，可能导致右侧列过高或过低
- **影响**: 视觉上不如真正的 Masonry 布局
- **建议**: 这是第一版权衡，后续可考虑自定义 MasonryPanel

---

## 六、待优化功能 (Roadmap)

### P0 - 必须修复
1. Favorite 状态持久化
2. 图片真正压缩
3. Skirt 类型修复
4. 标签删除功能

### P1 - 应该实现
5. 多条件筛选面板
6. 最近添加/喜欢区域空状态处理
7. 瀑布流列平衡优化

### P2 - 建议实现
8. 天气 API 接入（或移除天气显示）
9. Outfit 图片预览
10. 数据导入导出
11. 单元测试覆盖

### P3 - 未来考虑
12. 云同步
13. 分享功能
14. AI 搭配推荐
15. 测量数据管理

---

## 七、数据库

### 7.1 数据库位置
```
C:\Users\YANG\AppData\Local\ClosetApp\closet.db
```

### 7.2 图片存储
```
C:\Users\YANG\AppData\Local\ClosetApp\images\     # 原图
C:\Users\YANG\AppData\Local\ClosetApp\thumbnails\ # 缩略图 (当前未压缩)
```

### 7.3 表结构

| 表名 | 说明 |
|-----|------|
| Clothes | 衣服表 |
| Outfits | 穿搭表 |
| Tags | 标签表 |
| ClothingTags | 衣服-标签关联表 |
| OutfitClothes | 穿搭-衣服关联表 |
| Favorites | 收藏表 |
| OutfitWornRecords | 穿着记录表 |

---

## 八、部署说明

### 8.1 开发环境
```bash
dotnet build ClosetApp.sln
dotnet run --project ClosetApp.UI/ClosetApp.UI.csproj
```

### 8.2 数据库迁移
```bash
dotnet ef database update --project ClosetApp.Infrastructure --startup-project ClosetApp.UI
```

### 8.3 发布
```bash
dotnet publish ClosetApp.UI/ClosetApp.UI.csproj -c Release -o ./publish
```

---

## 九、总结

GirlfriendClosetApp 是一个架构清晰、层次分明的 WPF 应用。当前版本在 UI 上已经完成了从"后台管理风格"到"女性内容产品"的转型（V2卡片设计），但在功能完整性和代码质量上仍有提升空间。

**核心优势:**
- 清晰的分层架构
- MVVM 模式的合理使用
- Pinterest 风格的 UI 设计方向

**核心改进方向:**
- Favorite/收藏状态需要持久化
- 图片处理需要真正压缩
- 对话框逻辑需要迁移到 ViewModel
- 筛选功能需要完善

---

*文档版本: 1.2*
*最后更新: 2026-05-10*
