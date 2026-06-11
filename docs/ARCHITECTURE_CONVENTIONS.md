# Architecture Conventions

> 最后更新时间：2026-06-11
> 本文档记录当前项目的高价值架构约束，重点是分层、页面职责、AI 工作流边界、历史快照和图片资产规则。

## 1. 分层与依赖

- 依赖方向固定为：`Domain <- Application <- Infrastructure`，UI / UI.Logic / Tests 依赖这些层。
- `Domain` 不引用其他层。
- `Application` 只依赖 `Domain`，不依赖 `Infrastructure` 或 `UI`。
- `Infrastructure` 实现 Application 定义的接口。
- `UI.Logic` 只放纯逻辑，避免引入文件系统、数据库和 WPF 视觉树依赖。

## 2. DI 与启动

- DI 注册集中在 `ClosetApp.UI/App.xaml.cs`。
- 新业务流程优先注册为 UseCase，再由 UI 组合调用。
- 启动遵循“先首屏、后准备”的策略：
  - 主题初始化在主窗口前完成
  - 数据库初始化由 `AppStartupCoordinator` 后台执行
  - 页面真正读取数据前统一等待 readiness
- 不要把数据库迁移重新挪回主窗口显示前。
- 数据库 ready 后必须初始化 `LocalUser` 工作区，确保当前用户上下文存在后再刷新页面数据。

## 2.1 本地多用户工作区

- `LocalUser` 是本地数据隔离边界；本地登录认证绑定到该实体，后续远端账号只绑定 `LinkedAccountId`。
- `Clothing`、`Outfit`、`Tag`、`Favorite`、`OutfitWornRecord`、`PersonalProfile`、`OutfitGeneratedImage` 都必须带 `LocalUserId`。
- 仓储默认按 `ICurrentUserContext` 过滤当前用户数据；新增实体要自动写入当前用户 ID。
- 旧数据升级后归属超级管理员；超级管理员不可删除。
- 本地登录使用 `AccountName` + 密码/PIN 凭证；账号名作为本地登录标识，密码和 PIN 必须使用 PBKDF2 + 随机 salt 存储，禁止保存明文。
- 普通用户不能看到或打开用户管理入口；超级管理员负责创建、重置和删除普通用户。
- 登录后不提供无验证用户切换；更换本地用户必须退出登录并重新输入账号密码或 PIN。
- 用户管理弹窗不承担会话切换职责，只负责用户资料、头像、凭证和删除管理。
- 当前用户自助维护统一进入 `PersonalCenterDialog`，账号资料、个人档案和安全设置在同一个入口内分区，不再分散成多个“编辑档案”弹窗。

## 3. UseCase 与服务边界

- CRUD 和仓储型操作保留在现有 service。
- 新的业务编排优先落到 `ClosetApp.Application/UseCases`。
- 命名使用产品语言，例如：
  - `GetTodayRecommendations`
  - `RecordOutfitWorn`
  - `GenerateOutfitEffectImage`
- UseCase 负责业务编排，不负责 WPF 交互。

## 4. 页面状态

- 页面轻状态放在 `ClosetApp.UI.Logic/States`。
- 纯展示文案、摘要拼装、badge 文案等 helper 放在 `ClosetApp.UI.Logic/Services`。
- State / ViewModel 管状态与流程，helper 只做无副作用的文本和展示推导。
- Code-behind 负责：
  - 点击事件
  - 弹层编排
  - 动画
  - 页面级协调

## 5. Outfits 页职责

- `OutfitCard` 优先保持轻浏览卡，不再承载重管理逻辑。
- 卡片正面只保留：
  - 原始搭配预览或效果图优先主视觉
  - 标题与轻量元信息
  - AI 状态
  - 收藏
  - 更多菜单
- 卡片展示模式是 UI-only 偏好，支持：
  - `搭配优先`
  - `效果图优先`
- `效果图优先` 只消费成功且有结果图路径的效果图记录；没有成功图时必须自动回退到原始搭配预览。
- 点击卡片打开 `Components/Shared/Modal/OutfitWorkspaceDialog`。
- AI 效果图查看、生成、上传、设首选图、删除历史图统一进入工作台浮窗。
- 不要恢复“卡片大面积 overlay 操作区”或“页内常驻右侧详情栏”。

## 6. AI 图片生成边界

- 当前产品定位是“搭配效果图”，不是精确虚拟试衣。
- Prompt 由应用层模板组装，不对用户暴露自由 prompt 编辑。
- 当前默认 provider 是 OpenAI 兼容接口。
- 效果图归属到 `OutfitGeneratedImage`，挂到 `Outfit`。
- 历史图、主图、手动上传图共用同一套管理模型。

### 6.1 模型与接口规则

- `gpt-image-2` 走 `images/generations`
- 其他 `gpt-image-*` 走 `images/edits`
- 其他模型走 `responses`
- `BaseUrl` 需兼容是否自带 `/v1`
- 生成请求允许一次自动重试，用于处理超时和临时网关错误

### 6.2 生成前置条件

- `gpt-image-2` 文生图不强制要求头像照
- 参考图工作流至少需要个人头像照
- 全身照为可选增强参考图
- 必须完成云端上传同意
- 必须已保存 API Key 和基础配置

### 6.3 状态落库规则

- 发起生成先创建 `Pending`
- 成功写回 `Succeeded`
- 失败写回 `Failed`
- `FailureReason` 必须保留，方便 UI 展示和重试
- 不要把 provider 失败做成“完全无记录”

### 6.4 复用规则

- 相同档案快照 + 搭配快照 + 选项快照时优先复用历史结果
- 不要在命中相同条件时强制重复请求远端

## 7. PersonalProfile 与设置页分区

- 个人档案使用 `PersonalProfile`，按 `LocalUserId` 隔离，走 SQLite，不走普通 JSON 偏好。
- 主题、天气、推荐、AI 图片生成和搭配卡片展示设置按当前本地用户隔离。
- API Key 不明文存普通设置 JSON。
- 设置页稳定分区组件放在 `Components/Settings`：
  - `StorageLocationsSettingsPanel`
  - `LogMaintenanceSettingsPanel`
  - `ImageMaintenanceSettingsPanel`
  - `AiImageGenerationSettingsPanel`
- `WeatherPreferencesSettingsPanel`
- `AppearanceSettingsPanel`
- `BackupSettingsPanel`
- `AppearanceSettingsPanel` 同时承接主题切换和搭配卡片展示模式默认值设置
- `SettingsTab` 负责初始化和跨分区协调，不重新吞回所有局部按钮逻辑。
- `SettingsTab` 顶部保持总览工作台，下方稳定分成“日常偏好 / 维护治理”两列；不要再回到大段说明文字 + 多层卡片嵌套的旧布局。

## 8. 历史快照与删除规则

- `OutfitWornRecord.OutfitId` 必须允许为空，历史记录不能因为删除 live 搭配而消失。
- 记录穿着时必须保存：
  - 搭配名称快照
  - 衣物 ID 列表
  - 衣物数量
  - 衣物详情
  - 预览图路径
  - 完整性标记
- 删除衣物或搭配前，必须先刷新相关快照。
- 历史展示优先使用 snapshot，而不是 live 导航属性。
- live `Outfit.OutfitClothes` 读取必须容忍 `Clothing` 导航为空。

## 9. 图片资产与保留规则

- 衣物图片使用：
  - `images/originals`
  - `images/display`
  - `images/thumbnails`
- AI 图片使用：
  - `ai/profile`
  - `ai/renders/originals`
  - `ai/renders/display`
  - `ai/renders/thumbnails`

### 9.1 不可破坏规则

- 被 `Clothing` 引用的图片是有效资产
- 只被 `OutfitWornRecord` 快照引用的图片仍是有效历史资产
- 单件删除、批量清空和孤儿原图清理都不能删除历史快照仍在引用的图片
- 缓存清理可以删 display / thumbnails，但不能删 originals

### 9.2 健康检查与修复

- 历史缺图判断统一复用 `IImageAssetResolver`
- 修复失败时要清理本次新保存图片，避免制造孤儿文件
- 健康检查结果要包含可导航摘要，不能只给聚合数字

## 10. 备份与恢复

- ZIP 备份包含图片资产
- JSON 备份只导核心结构化数据
- AI 相关备份范围应包含：
  - `LocalUser`
  - 每个用户的 `PersonalProfile`
  - 个人参考图
  - `OutfitGeneratedImage` 元数据
  - 生成结果图
- 备份失败反馈要尽量说明数据库是否回滚、图片恢复走到哪一步

## 11. 标签与系统元数据

- `TagCategory.Season` 是系统管理标签，不进入普通标签整理视图。
- `TagsTab` 只展示 `Style` / `Scene`。
- 标签筛选和排序逻辑保持在 `TagsTabState`，不要散落到多个 panel 的 code-behind。

## 12. 共享组件与样式

- 设计 token 放在 `ClosetApp.UI/Themes/Tokens`
- 控件样式放在 `ClosetApp.UI/Themes/Controls`
- 新 UI 优先复用 token，不要硬编码颜色、阴影、圆角和间距
- 共享控件优先放 `Components/Shared`
- 自定义控件优先用 DependencyProperty，不要用大量命令式视觉同步
- 弹窗内高频操作优先复用共享样式：
  - 关闭按钮：`ModalCloseButton`
  - 页脚取消 / 保存：`ModalCancelButton` / `ModalSaveButton`
  - 次级工具按钮：`SecondaryButton` / `GhostButton`
  - 模式切换：`AppSegmentedTabShell` + `AppSegmentedTabButton`
- 本地用户头像统一使用 `Components/Shared/LocalUserAvatar`；侧边栏、登录页、用户管理弹窗不要各自手写头像壳。
- 本地用户头像和个人档案参考图都必须按用户 ID 生成独立文件槽名，禁止继续使用全局固定 `avatar` / `full-body` 文件名覆盖不同用户资源。
- 对象型 `ComboBox` 必须显式声明显示映射：
  - 简单对象列表使用 `DisplayMemberPath`
  - 需要自定义展示时使用 `ItemTemplate`
  - 不允许在同一个 `ComboBox` 上同时混用 `DisplayMemberPath` 和 `ItemTemplate`
- 登录页最近账号下拉、搭配页筛选下拉、设置页偏好下拉都应复用共享样式，并通过测试保护选中态显示，避免退回对象 `ToString()`

## 13. 命名空间安全

- 遇到 Domain 实体和 UI 命名空间重名时，使用显式 alias：
  - `ClothingEntity`
  - `OutfitEntity`
  - `TagEntity`
- 不要新增与 Domain 实体完全同名的 UI 命名空间层级，除非已经在该特性目录中。

## 14. 错误提示

- 用户可见错误统一优先走 `WardrobeActionErrorPresenter`
- 不要直接把原始异常消息抛给 UI
- Toast / Modal 应展示整理后的中文提示

## 15. 测试约定

- 新增业务编排优先补对应 UseCase 或 service 测试
- AI 相关至少覆盖：
  - readiness 判定
  - 成功写库
  - 失败保留 `Failed` 记录
  - 保存图片失败时清理临时文件
  - 删除主图后的回退逻辑
- 文档描述的行为、状态和入口变更后，要同步更新 `README.md`、`PROJECT_DOCUMENTATION.md` 和本文件
