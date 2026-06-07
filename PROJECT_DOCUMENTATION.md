# GirlfriendClosetApp 项目文档

> 最后更新时间：2026-06-07
> 文档目标：对齐当前代码真实状态，作为项目结构、运行机制、AI 效果图能力与维护约定的主说明文档

---

## 1. 项目概览

GirlfriendClosetApp 是一款运行在 Windows 上的私人数字衣橱应用。它围绕“衣物管理、搭配管理、穿着记录、本地图片治理、AI 效果图生成”这几条主线工作。

当前版本已经不是单纯的衣柜 CRUD 工具，而是一个带有稳定本地数据层、历史快照系统、天气推荐和 AI 图像工作流的桌面应用。

核心目标：

- 管理衣物与搭配
- 保留可追溯的穿着历史
- 基于天气和偏好辅助每日决策
- 为搭配生成或上传 AI 效果图
- 在搭配列表中按“搭配优先 / 效果图优先”切换浏览重心
- 支持本地多用户衣橱工作区，每个用户拥有独立衣柜、搭配、标签、记录、个人档案和设置，并通过本地账号 + 密码/PIN 登录
- 让数据库、图片资产、备份恢复都可维护

---

## 2. 技术栈

| 层 | 技术 | 说明 |
|---|---|---|
| UI | WPF (`net10.0-windows`) | 桌面端界面 |
| 核心类库 | .NET (`net8.0`) | Domain / Application / Infrastructure |
| UI 组件 | HandyControl | 样式资源与基础控件 |
| MVVM | CommunityToolkit.Mvvm | ViewModel、命令与可观察属性 |
| 数据访问 | EF Core + SQLite | 本地数据库 |
| 图片处理 | SixLabors.ImageSharp | 图片保存、重采样与缓存 |
| 日志 | Serilog | 本地滚动日志 |
| 测试 | xUnit | 单元 / 集成测试 |

---

## 3. 解决方案结构

```text
ClosetApp.slnx
├── ClosetApp.Domain/          # 实体、枚举、仓储接口、衣物分类模型
├── ClosetApp.Application/     # DTO、服务接口/实现、UseCases、图片抽象
├── ClosetApp.Infrastructure/  # EF Core、SQLite、文件系统、图片、天气、AI、备份
├── ClosetApp.UI/              # WPF 视图、组件、主题、服务、ViewModel
├── ClosetApp.UI.Logic/        # UI 纯逻辑共享工程（State / Engine / Import / 错误提示）
├── ClosetApp.Tests/           # xUnit 测试工程
├── docs/
│   └── ARCHITECTURE_CONVENTIONS.md
├── README.md
├── PROJECT_DOCUMENTATION.md
└── AGENTS.md
```

---

## 4. 架构与依赖方向

### 4.1 分层关系

```text
Domain <- Application <- Infrastructure
                      <- UI
                      <- UI.Logic
                      <- Tests
```

约束：

- `Domain` 不依赖其他层
- `Application` 不依赖 `Infrastructure` 或 `UI`
- `Infrastructure` 实现 Application 定义的接口
- `UI` 负责页面、弹层、动画和用户交互编排
- `UI.Logic` 放纯逻辑，供 UI 与 Tests 复用

### 4.2 运行时数据流

```text
View / Component
  -> ViewModel / State
    -> Service / UseCase
      -> Repository
        -> EF Core / SQLite / File System / HTTP
```

---

## 5. 启动链路与 DI

### 5.1 DI 注册位置

依赖注入集中在 [`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.UI\App.xaml.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.UI/App.xaml.cs)。

当前已注册的关键内容包括：

- `DbContextFactory<ClosetDbContext>`
- 仓储：
  - `IClothingRepository`
  - `IOutfitRepository`
  - `ITagRepository`
  - `IFavoriteRepository`
  - `IOutfitWornRecordRepository`
  - `ILocalUserRepository`
  - `IPersonalProfileRepository`
  - `IOutfitGeneratedImageRepository`
- 业务服务：
  - `IClothingService`
  - `IOutfitService`
  - `ITagService`
  - `IOutfitRecommendationService`
  - `IPersonalProfileService`
  - `ILocalUserService`
  - `IAiGenerationPreferencesService`
  - `IAiImageGenerationService`
  - `IImageStorageService`
  - `IAiAssetStorageService`
  - `IImageMaintenanceService`
  - `IBackupService`
  - `IWeatherService`
  - `IWeatherPreferencesService`
  - `IRecommendationPreferencesService`
- UseCase：
  - `GetWardrobeOverview`
  - `ImportClothesFromImages`
  - `CompleteClothingMetadataBatch`
  - `ClearWardrobeByTypes`
  - `GetOutfitHistorySummary`
  - `GetWardrobeInsights`
  - `GetAnnualOutfitReport`
  - `RecordOutfitWorn`
  - `GetRecommendationReadinessSummary`
  - `GetTodayRecommendations`
  - `GetTagsForSelection`
  - `GetAiGenerationReadiness`
  - `GetOutfitGeneratedImages`
  - `SetPrimaryOutfitGeneratedImage`
  - `DeleteOutfitGeneratedImage`
  - `GenerateOutfitEffectImage`
  - `SaveUploadedOutfitGeneratedImage`

### 5.2 当前启动设计

启动行为已经从“启动时同步迁移数据库再开窗”调整为“优先出首屏，再后台准备数据库”。

当前真实链路：

1. 初始化日志
2. 注册全局异常处理
3. 构建 DI 容器
4. `ThemeService.InitializeAsync()` 在主窗口前完成，保证首屏主题稳定
5. 主窗口显示
6. `AppStartupCoordinator` 在后台触发数据库初始化
7. 各页面在真正读取数据前调用 `WaitUntilReadyAsync()`
8. `ILocalUserService.EnsureInitializedAsync()` 创建或修复本地超级管理员，并把旧数据归属到该用户
9. 登录窗口在数据库 ready 后显示；首次升级时若超级管理员无凭证，先设置管理员密码，再进入主窗口

`AppStartupCoordinator` 位于 [`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.UI\Services\AppStartupCoordinator.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.UI/Services/AppStartupCoordinator.cs)。

这条设计的目标是：

- 缩短“点开应用到看到窗口”的体感时间
- 避免首屏被数据库迁移和多个 Tab 首刷拖慢
- 用延迟 readiness 保证数据读取仍然安全

---

## 6. 领域模型

### 6.1 核心实体

#### LocalUser

本地用户工作区实体：

- `DisplayName`
- `AccountName`
- `AvatarPhotoPath`
- `Role`：`SuperAdmin` / `Member`
- `IsActive`
- `LinkedAccountId`

超级管理员账号不可删除，默认登录账号为 `admin`。本地密码/PIN 使用 PBKDF2 + 随机 salt 存储；旧数据升级后仍归属超级管理员，首次启动需要设置管理员账号密码。

#### Clothing

- `Id`
- `LocalUserId`
- `Name`
- `Type`
- `GarmentType`
- `ImagePath`
- `Color`
- `Brand`
- `Season`
- `FavoriteLevel`

#### Outfit

- `Id`
- `LocalUserId`
- `Name`
- `Scene`
- `Season`
- `Rating`
- `WearCount`
- `WornDate`
- `OriginalClothingCount`
- `OutfitClothes`
- `Favorites`
- `GeneratedImages`

#### Tag

- `Id`
- `LocalUserId`
- `Name`
- `Color`
- `Category`

#### Favorite

- 绑定到 `Outfit`

#### OutfitWornRecord

用于保留历史快照：

- `OutfitId` 可空
- `WornDate`
- `OutfitNameSnapshot`
- `OutfitClothingIdsSnapshot`
- `ClothingCountSnapshot`
- `ClothingDetailsSnapshot`
- `PreviewSnapshotPath`
- `IsSnapshotComplete`

#### PersonalProfile

当前为按本地用户隔离的个人档案，主要字段：

- `DisplayName`
- `HeightCm`
- `BodyShape`
- `SkinTone`
- `HairLength`
- `HairColor`
- `FaceFeaturesSummary`
- `StyleKeywords`
- `AvoidKeywords`
- `AvatarPhotoPath`
- `FullBodyPhotoPath`
- `CloudUploadConsentAcceptedAt`

#### OutfitGeneratedImage

归属到 `Outfit` 的效果图记录，主要字段：

- `OutfitId`
- `ProviderKind`
- `Model`
- `PromptSnapshot`
- `ProfileSnapshotJson`
- `OutfitSnapshotJson`
- `OptionSnapshotJson`
- `ResultImagePath`
- `IsPrimary`
- `Status`
- `FailureReason`
- `CreatedAt`

其中 `IsPrimary` 当前在产品层统一解释为“首选效果图”：

- 在“效果图优先”模式下优先显示
- 没有成功效果图时自动回退到原始搭配预览
- 不再等同于“永久覆盖搭配卡片封面”

### 6.2 枚举

- `ClothingType`
- `Season`
- `OutfitScene`
- `TagCategory`
- `RecommendationRotationStrategy`

### 6.3 衣物分类体系

`ClosetApp.Domain/Clothing/` 维护细粒度衣物分类：

- `GarmentType`
- `DisplayCategory`
- `LayerRole`
- `ClothingMappings`
- `ClothingTaxonomy`

这套模型支撑搭配预览与选择规则，不再只靠旧的 `ClothingType`。

---

## 7. 本地目录与资产结构

由 [`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.Infrastructure\AppPaths.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.Infrastructure/AppPaths.cs) 统一定义。

### 7.1 常规目录

- 数据根目录：`%LocalAppData%\ClosetApp\`
- 数据库：`%LocalAppData%\ClosetApp\closet.db`
- 图片根目录：`%LocalAppData%\ClosetApp\images\`
- 原图：`images/originals`
- 显示图：`images/display`
- 缩略图：`images/thumbnails`
- 日志：`logs`
- 备份：`backups`

### 7.2 AI 目录

- `ai/profile`
- `ai/renders/originals`
- `ai/renders/display`
- `ai/renders/thumbnails`

设计原则：

- 个人参考图与普通衣物图片分目录保存
- AI 渲染结果与普通图片缓存分离
- 备份时同时考虑元数据与图片资产

---

## 8. UI 架构

### 8.1 主导航

主界面由左侧 `NavigationSidebar` 和右侧内容区组成，一级导航维持 4 个：

- `ClothesTab`
- `OutfitsTab`
- `TagsTab`
- `SettingsTab`

左侧头像区是当前账号入口：悬停有高亮反馈，点击打开带头像的用户菜单，可编辑当前档案、退出登录；超级管理员额外显示用户管理入口。用户管理以管理台形式呈现，包含用户统计、搜索列表、选中用户详情和新增用户面板，只负责创建、编辑、重置凭证和删除用户，不切换当前会话；更换账号必须先退出登录后重新输入账号密码。

### 8.2 ClothesTab

职责：

- 瀑布流浏览衣物
- 搜索与筛选
- 批量导入
- 打开衣物编辑器

当前稳定子组件：

- `WardrobeSummaryPanel`
- `WardrobeFilterPanel`
- `WardrobeCollectionHeaderPanel`

### 8.3 OutfitsTab

当前真实结构已经收敛为：

- 顶部推荐总览卡
- 筛选、展示模式切换与排序
- 搭配列表
- 点击 `OutfitCard` 打开 `OutfitWorkspaceDialog`

不再使用常驻右侧详情区，也不再把 AI 管理直接铺在卡片正面。

[`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.UI\Views\OutfitsTab.xaml.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.UI/Views/OutfitsTab.xaml.cs) 当前清楚表明：

- 刷新前会等待 `AppStartupCoordinator`
- 卡片点击打开 `OutfitWorkspaceDialog`
- 编辑、删除、收藏仍从卡片事件路由进入
- 展示模式切换会立即生效并保存为全局默认值

### 8.4 OutfitCard

当前产品定位是“轻浏览卡”：

- 展示原始搭配预览，或在效果图优先模式下展示首选效果图
- 展示标题和轻量元信息
- 展示 AI 状态 badge
- 提供收藏按钮与更多菜单

不再承担：

- 大图浏览
- AI 主图展示
- 常驻底部操作条
- 右侧详情联动选中态

新增展示规则：

- `搭配优先`：保持原始搭配预览为主视觉
- `效果图优先`：若存在成功效果图，则展示首选效果图；否则自动回退到原始搭配预览
- 失败记录不会被拿来当卡片封面

### 8.5 OutfitWorkspaceDialog

当前是正式的“AI 效果图工作台浮窗”，不是 quick view。

职责：

- 展示当前首选效果图或最近效果图
- 展示效果图历史缩略图
- 打开大图浏览
- 打开生成 / 上传 / 管理弹层
- 保留收藏、编辑、删除、今天穿了等搭配动作

当前浮窗已经进一步纯化：

- 不再混入原始搭配主视觉
- 一打开就直接进入效果图工作区
- 无效果图时展示空状态与生成 / 上传入口

### 8.6 SettingsTab

当前 Settings 页已经按稳定分区收敛：

- `StorageLocationsSettingsPanel`
- `LogMaintenanceSettingsPanel`
- `ImageMaintenanceSettingsPanel`
- `AiImageGenerationSettingsPanel`
- `WeatherPreferencesSettingsPanel`
- `AppearanceSettingsPanel`
- `BackupSettingsPanel`

其中 `AppearanceSettingsPanel` 现在同时承接：

- 主题切换
- 搭配卡片展示模式默认值设置

---

## 9. AI 图片生成能力

### 9.1 当前定位

V1 的定位是“搭配效果图生成”，不是像素级虚拟试衣。

强调：

- 人物一致性
- 搭配层次和颜色贴近
- 场景、季节、天气语义一致

不承诺：

- 精确版型还原
- 局部重绘换装
- 高级 prompt 手工编辑

### 9.2 主要接口与 UseCase

当前已经接入并在代码中存在：

- `IPersonalProfileService`
- `IAiGenerationPreferencesService`
- `IAiImageGenerationService`

UseCase：

- `GetAiGenerationReadiness`
- `GenerateOutfitEffectImage`
- `GetOutfitGeneratedImages`
- `SetPrimaryOutfitGeneratedImage`
- `DeleteOutfitGeneratedImage`
- `SaveUploadedOutfitGeneratedImage`

### 9.3 生成前置条件

当前首版 readiness 规则以“能稳定生成”为目标，关键条件包括：

- 已填写个人档案
- 已同意云端上传
- 已配置 provider / model / API Key
- 搭配包含足够的有效衣物

其中：

- `gpt-image-2` 文生图接入不强制要求头像照
- 参考图工作流模型仍至少要求头像照
- 全身照现在是可选增强参考图，不再是强必需项

### 9.4 请求组装

应用层统一组装生成请求，不暴露自由 prompt 编辑。

请求来源包括：

- 个人档案摘要
- 参考图：头像照，必要时加全身照
- 搭配衣物摘要
- 场景参数
- 天气 / 季节上下文

### 9.5 当前 OpenAI 兼容实现

实现位于 [`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.Infrastructure\Services\OpenAiCompatibleImageGenerationService.cs`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.Infrastructure/Services/OpenAiCompatibleImageGenerationService.cs)。

关键规则：

- `gpt-image-2` 走 `images/generations`
- 其他 `gpt-image-*` 走 `images/edits`
- 非 `gpt-image-*` 走 `responses`
- `BaseUrl` 已兼容用户输入是否自带 `/v1`
- 请求超时取 `Math.Clamp(preferences.TimeoutSeconds, 30, 300)`
- 对超时、限流和网关波动做一次自动重试
- `gpt-5.5` 这类非 `gpt-image-*` 模型会走 `responses + image_generation tool`

### 9.6 结果与状态

生成链路当前采用“先落记录，再更新结果”的方式，避免用户感知为空白：

- 发起生成时先写入 `Pending`
- 成功后更新为 `Succeeded`
- 失败后更新为 `Failed`
- 失败原因会保留到记录中

这意味着：

- 即使 provider 失败，历史里也能看到失败痕迹
- UI 可以展示“最近一次失败”“生成中”等状态
- 用户可以按原条件重试

### 9.7 复用策略

当前已实现“避免重复生成”的缓存复用逻辑：

- 同档案快照
- 同搭配快照
- 同生成选项

命中时优先返回已保存结果，而不是每次都重新请求远端。

### 9.8 手动上传

AI 工作流不完全依赖远端生成。

用户可以：

- 手动上传本地效果图
- 保存到当前搭配历史
- 参与同一套首选图、缩略图、删除和预览流程

---

## 10. AI 相关 UI

### 10.1 PersonalProfileEditorPanel

个人档案编辑入口来自左侧头像区。

头像展示统一使用 `Components/Shared/LocalUserAvatar`：优先显示当前用户头像照，没有头像时回退到首字母，并在当前用户头像上显示状态点。侧边栏、登录页和用户管理弹窗都复用这一套头像壳。

当前分区包括：

- 基础信息
- 外形特征
- 参考照片
- 风格偏好
- 隐私同意

### 10.2 AiImageGenerationSettingsPanel

当前负责：

- `Base URL`
- `Model`
- `API Key`
- 超时秒数
- 测试连接
- 隐私说明
- 个人档案入口

最近几轮迭代还补了：

- API Key 持久化
- 眼睛按钮切换显示 / 隐藏
- 模型切换与手动编辑共存

### 10.3 GenerateOutfitImagePanel

这是生成 / 上传 / 历史管理的主弹层。

当前 UI 已支持：

- 忙碌态
- 进度条
- 已等待秒数
- 当前超时秒数
- 最近生成状态
- 最近失败尝试
- 按原条件重试

### 10.4 GeneratedImagePreviewDialog

专门负责大图浏览。

`OutfitWorkspaceDialog` 本身会先直接展示主效果图，避免“点击查看效果图后还要再点一次按钮才看到图”的断裂体验；需要沉浸式查看时再进入这个预览弹层。

---

## 11. 推荐与历史系统

### 11.1 今日推荐

推荐会综合：

- 天气
- 温度
- 场景
- 季节
- 收藏
- 穿着频次
- 最近穿着
- 标签
- 颜色偏好
- 推荐偏好

当前页面上推荐区已经收敛为左主右辅的单张总览卡。

### 11.2 穿着记录快照

快照系统是当前项目一个重要的稳定性基础：

- 删除 outfit 不删除历史
- 删除 clothing 前先刷新快照
- 缺图时仍尽量保留文字信息
- 历史展示优先使用 snapshot，而不是 live 导航属性

### 11.3 历史图片健康检查

当前支持：

- 扫描历史记录中的缺图
- 返回可导航的缺图摘要
- 单张修复历史引用
- 修复失败时清理本次新落地图片

---

## 12. 图片与缓存体系

### 12.1 三层图片结构

- `Original`
- `Display`
- `Thumbnail`

### 12.2 关键服务

- `ImageStorageService`
- `ImageMaintenanceService`
- `ImageAssetResolver`
- `AiAssetStorageService`

### 12.3 当前图片治理能力

- 缺失图片统计
- Display / Thumbnail 缓存重建
- 孤儿原图扫描和清理
- 历史图片健康检查
- 旧目录修复
- 缓存清理

### 12.4 性能侧约束

当前代码已包含一些图片加载稳定策略：

- 可见后再异步加载
- 并发请求去重
- 尺寸探测请求去重
- 弱引用缓存
- 失败负缓存

这些策略主要用于减少瀑布流滚动和首屏布局压力。

---

## 13. 备份与恢复

备份能力由 `IBackupService` / `BackupService` 提供。

当前支持：

- 导出前校验
- ZIP 导出
- JSON 核心数据导出
- ZIP / JSON 导入
- 备份历史

AI 相关备份范围已扩展为：

- `PersonalProfile`
- 个人参考图
- `OutfitGeneratedImage` 元数据
- AI 效果图图片

说明：

- ZIP 备份包含图片
- 纯 JSON 备份仍以核心结构化数据为主，不包含图片

---

## 14. 测试现状

测试工程：[`D:\03_Projects\Personal\GirlfriendClosetApp\ClosetApp.Tests\ClosetApp.Tests.csproj`](D:/03_Projects/Personal/GirlfriendClosetApp/ClosetApp.Tests/ClosetApp.Tests.csproj)

当前覆盖重点包括：

- 备份与恢复
- 图片治理
- 搭配预览引擎
- 衣柜批量导入
- 搭配推荐
- 页面状态
- ViewModel
- 数据层
- AI 生成链路

最近已补充的 AI 相关验证之一：

- provider 失败后会保留 `Failed` 记录，而不是完全不落库

最近验证结果：

- `rtk dotnet build ClosetApp.slnx /m:1`
- `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1`
- **203 tests passed**

---

## 15. 开发命令

仓库要求优先使用 `rtk`：

```powershell
rtk dotnet build ClosetApp.slnx /m:1
rtk dotnet run --project ClosetApp.UI
rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1
rtk pwsh -Command "Get-ChildItem -Force"
```

---

## 16. 当前实现边界与已知说明

### 16.1 AI 能力边界

当前版本是“效果图生成”，不是：

- 精确虚拟试衣
- 局部重绘换装
- 多人设系统
- 社区分享平台

### 16.2 页面结构边界

Outfits 页当前方向明确为：

- 列表页负责浏览
- AI 管理进入工作台浮窗
- 卡片不再堆叠过多按钮和大图

### 16.3 运行时体验优化方向

当前已经做了：

- 首屏延后数据库初始化
- 图片异步加载与缓存去重
- AI 请求重试与状态回写

后续仍可继续关注：

- 调试启动速度
- 首次页面加载过程中的重排感
- 大量卡片下 Masonry 的布局成本

---

## 17. 文档索引

- 快速入口：[`README.md`](D:/03_Projects/Personal/GirlfriendClosetApp/README.md)
- 架构约定：[`docs/ARCHITECTURE_CONVENTIONS.md`](D:/03_Projects/Personal/GirlfriendClosetApp/docs/ARCHITECTURE_CONVENTIONS.md)
- 协作约束：[`AGENTS.md`](D:/03_Projects/Personal/GirlfriendClosetApp/AGENTS.md)
