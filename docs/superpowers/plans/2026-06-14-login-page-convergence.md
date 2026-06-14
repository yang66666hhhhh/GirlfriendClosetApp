# 登录页收敛优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 收敛登录页为"单一密码登录 + 最近使用账号 + 主题切换 + 首次使用说明"的正式入口页，去掉 PIN 主流程。

**Architecture:** LoginWindow XAML + code-behind 直接修改，移除 PIN 相关控件和逻辑，新增主题切换（复用 ThemeService）、增强最近账号区块、首次使用说明区。保留底层 PIN 数据结构和服务接口。

**Tech Stack:** WPF, CommunityToolkit.Mvvm, ThemeService, ILocalAuthService

---

## File Map

| File | Action | Responsibility |
|------|--------|---------------|
| `ClosetApp.UI/Views/LoginWindow.xaml` | Modify | 移除 PIN 控件、凭证模式切换；新增主题切换入口、增强最近账号区块、首次使用说明区 |
| `ClosetApp.UI/Views/LoginWindow.xaml.cs` | Modify | 移除 PIN 逻辑；新增主题切换逻辑、最近账号点击处理、首次使用说明弹窗 |
| `ClosetApp.Tests/LoginWindowLayoutTests.cs` | Modify | 更新断言匹配新布局 |
| `ClosetApp.Tests/LoginRecentAccountsBuilderTests.cs` | Modify | 保留现有测试，可能微调 |

---

### Task 1: 移除 Setup 模式下的 PIN 输入区

**Files:**
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml:378-386` (SetupPanel 内的 PIN 区域)
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml.cs:136` (CompleteSetupAsync 中的 SetPinAsync 调用)
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml.cs:283` (HookInputErrorClearing 中的 SetupPinBox)

- [ ] **Step 1: 从 XAML SetupPanel 移除 PIN 控件**

移除以下 XAML 行（LoginWindow.xaml SetupPanel 内）：
```xml
<!-- 移除这3行 -->
<TextBlock Text="快捷 PIN（可选）" Style="{StaticResource LoginInputLabel}"/>
<PasswordBox x:Name="SetupPinBox"
             Margin="0,6,0,0"
             Style="{StaticResource LoginPasswordBox}"/>
<TextBlock Text="PIN 只用于本机快捷登录，可以之后再设置。"
           Margin="2,6,0,0"
           FontSize="{DynamicResource FontSize.Meta}"
           Foreground="{DynamicResource TextPlaceholderBrush}"/>
```

- [ ] **Step 2: 从 code-behind 移除 SetupPinBox 引用**

在 `LoginWindow.xaml.cs` 中：
1. `CompleteSetupAsync()` 方法：移除 `await _localAuthService.SetPinAsync(_superAdmin.Id, SetupPinBox.Password);`
2. `HookInputErrorClearing()` 方法：移除 `SetupPinBox.PasswordChanged += ClearErrorIfUserEditing;`

- [ ] **Step 3: 编译验证**

Run: `rtk dotnet build ClosetApp.slnx /m:1`
Expected: 0 errors

---

### Task 2: 移除登录模式下的 PIN 流程（凭证模式切换 + PIN 输入面板）

**Files:**
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml:399-440` (CredentialModePanel + PinCredentialPanel)
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml.cs:24,146-148,196,230-274,280,316-323` (PIN 相关逻辑)

- [ ] **Step 1: 从 XAML 移除凭证模式切换和 PIN 面板**

移除 LoginPanel 内的以下区域：
1. `CredentialModePanel` 整个 Grid（含密码/PIN 分段切换，行 399-427）
2. `PinCredentialPanel` 整个 StackPanel（行 433-438）

保留 `PasswordCredentialPanel`（含 LoginPasswordBox），但将其外层 StackPanel 去掉，直接把 LoginPasswordBox 放在 LoginPanel 下。

- [ ] **Step 2: 从 code-behind 移除 PIN 相关逻辑**

1. 移除 `LoginCredentialMode` 枚举（行 403-407）
2. 移除 `SelectedCredentialMode` 属性（行 24）
3. 移除 `PasswordMode_Checked`、`PinMode_Checked`、`SetLoginCredentialMode` 方法
4. 简化 `LoginAsync()`：只走密码，不再判断 `SelectedCredentialMode`
5. 简化 `ValidateLoginInputs()`：只校验密码
6. 简化 `FocusCredentialInput()`：只 focus LoginPasswordBox，移除 `preferPin` 参数
7. 简化 `RecentAccountSelector_SelectionChanged()`：选择后只清空密码、focus 密码框，不再调用 `SetLoginCredentialMode`
8. 移除 `HookInputErrorClearing()` 中 `LoginPinBox.PasswordChanged` 引用
9. 移除 `ClearFieldErrors()` 中对已删除控件的引用（如有）

- [ ] **Step 3: 编译验证**

Run: `rtk dotnet build ClosetApp.slnx /m:1`
Expected: 0 errors

---

### Task 3: 添加登录页右上角主题切换入口

**Files:**
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml` (Grid 根布局内，添加右上角主题切换)
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml.cs` (主题切换逻辑)

- [ ] **Step 1: 在 XAML 根 Grid 添加右上角主题切换**

在 `<Grid Background="#F8FAFF">` 内、`LoginWorkspaceShell` 之前，添加一个右上角定位的主题切换区域：

```xml
<StackPanel x:Name="LoginThemeToggle"
            HorizontalAlignment="Right"
            VerticalAlignment="Top"
            Margin="0,16,16,0"
            Orientation="Horizontal">
    <Border Style="{StaticResource AppSegmentedTabShell}">
        <UniformGrid Columns="2">
            <RadioButton x:Name="BtnThemeRose"
                         Content="柔粉"
                         GroupName="LoginTheme"
                         Style="{StaticResource AppSegmentedTabButton}"
                         Checked="LoginThemeRose_Checked"/>
            <RadioButton x:Name="BtnThemeBlue"
                         Content="清蓝"
                         GroupName="LoginTheme"
                         Style="{StaticResource AppSegmentedTabButton}"
                         Checked="LoginThemeBlue_Checked"/>
        </UniformGrid>
    </Border>
</StackPanel>
```

- [ ] **Step 2: 在 code-behind 添加主题切换逻辑**

1. 注入 `ThemeService`（通过 `App.Services.GetRequiredService<ThemeService>()`）
2. 在 `LoginWindow_Loaded` 中，根据 `_themeService.CurrentTheme` 设置 `BtnThemeRose.IsChecked` / `BtnThemeBlue.IsChecked`
3. 添加 `LoginThemeRose_Checked` 和 `LoginThemeBlue_Checked` 事件处理：
   - 调用 `await _themeService.ApplyThemeAsync(AppThemeKind.Rose/Blue)`
   - 注意：主题切换不放进表单内部，不干扰主登录动作

- [ ] **Step 3: 编译验证**

Run: `rtk dotnet build ClosetApp.slnx /m:1`
Expected: 0 errors

---

### Task 4: 强化最近使用账号区块

**Files:**
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml:512-517` (HeroSessionHint 静态文案替换)
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml.cs` (ApplyRecentAccountsState 中更新 HeroSessionHint)

- [ ] **Step 1: 替换 HeroSessionHint 为增强最近账号区块**

将原来的：
```xml
<TextBlock x:Name="HeroSessionHint"
           Margin="0,10,0,0"
           HorizontalAlignment="Center"
           FontSize="{DynamicResource FontSize.Hint}"
           Foreground="{DynamicResource TextSecondaryBrush}"
           Text="最近使用：admin"/>
```

替换为：
```xml
<StackPanel x:Name="HeroRecentAccountBlock"
            Margin="0,16,0,0"
            HorizontalAlignment="Center"
            Cursor="Hand"
            MouseLeftButtonDown="RecentAccountBlock_Click">
    <TextBlock Text="最近使用"
               HorizontalAlignment="Center"
               FontSize="{DynamicResource FontSize.Meta}"
               Foreground="{DynamicResource TextPlaceholderBrush}"/>
    <TextBlock x:Name="TxtRecentAccountName"
               HorizontalAlignment="Center"
               FontSize="{DynamicResource FontSize.Label}"
               FontWeight="SemiBold"
               Foreground="{DynamicResource TextPrimaryBrush}"/>
    <TextBlock x:Name="TxtRecentAccountLastLogin"
               Margin="0,2,0,0"
               HorizontalAlignment="Center"
               FontSize="{DynamicResource FontSize.Meta}"
               Foreground="{DynamicResource TextSecondaryBrush}"/>
</StackPanel>
```

- [ ] **Step 2: 在 code-behind 实现最近账号区块更新和点击**

1. 在 `ApplyRecentAccountsState()` 中，更新 `TxtRecentAccountName` 和 `TxtRecentAccountLastLogin`：
   - 如果有最近账号：显示 accountName 和 lastLoginText
   - 如果没有：隐藏整个 HeroRecentAccountBlock
2. 添加 `RecentAccountBlock_Click` 事件处理：
   - 自动填充账号输入框（RecentAccountSelector.Text）
   - 聚焦密码框（LoginPasswordBox.Focus()）

- [ ] **Step 3: 编译验证**

Run: `rtk dotnet build ClosetApp.slnx /m:1`
Expected: 0 errors

---

### Task 5: 添加首次使用说明区

**Files:**
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml` (LoginFormSurface 底部)
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml.cs` (说明弹窗逻辑)

- [ ] **Step 1: 在 XAML LoginFormSurface 底部添加首次使用说明**

在 `LoginThemeToggle` 之后（或 `HeroRecentAccountBlock` 之后），添加：

```xml
<StackPanel x:Name="FirstTimeHintBlock"
            Margin="0,24,0,0"
            HorizontalAlignment="Center">
    <TextBlock HorizontalAlignment="Center"
               FontSize="{DynamicResource FontSize.Hint}"
               Foreground="{DynamicResource TextSecondaryBrush}">
        <Run Text="首次使用？"/>
        <Run Text="请先完成管理员初始化"/>
    </TextBlock>
    <Button x:Name="BtnLearnMultiUser"
            Content="了解本地多用户模式"
            HorizontalAlignment="Center"
            Margin="0,6,0,0"
            Background="Transparent"
            BorderThickness="0"
            Foreground="{DynamicResource PrimaryBrush}"
            FontSize="{DynamicResource FontSize.Meta}"
            Cursor="Hand"
            Click="LearnMultiUser_Click"/>
</StackPanel>
```

- [ ] **Step 2: 在 code-behind 实现说明弹窗**

添加 `LearnMultiUser_Click` 事件处理：
- 使用 `ConfirmModal` 或直接创建一个简洁的信息弹窗（复用共享 Modal 体系）
- 弹窗内容说明：
  - 这是本地多用户工作区
  - 成员由超级管理员在登录后创建
  - 登录页本身不开放公开注册

- [ ] **Step 3: 移除登录页中任何"注册/创建账号/新增成员"相关文案**

检查 XAML 和 code-behind，确认没有遗留的注册相关文案。

- [ ] **Step 4: 编译验证**

Run: `rtk dotnet build ClosetApp.slnx /m:1`
Expected: 0 errors

---

### Task 6: 简化登录主路径布局

**Files:**
- Modify: `ClosetApp.UI/Views/LoginWindow.xaml` (整体布局调整)

- [ ] **Step 1: 确认登录主路径只保留账号、密码、登录按钮**

检查 LoginPanel 最终结构应为：
1. RecentAccountSelector（可编辑 ComboBox，保留作为输入增强）
2. LoginPasswordBox
3. BtnSubmit

移除所有已删除的控件引用，确保 XAML 结构干净。

- [ ] **Step 2: 调整 SetupPanel 的模式描述文案**

`TxtModeDescription` 在 setup 模式下改为："先完成管理员凭证设置，再进入本地衣柜。"（保持现有文案即可，已正确）

- [ ] **Step 3: 编译验证**

Run: `rtk dotnet build ClosetApp.slnx /m:1`
Expected: 0 errors

---

### Task 7: 更新登录页布局测试

**Files:**
- Modify: `ClosetApp.Tests/LoginWindowLayoutTests.cs`

- [ ] **Step 1: 更新 `LoginWindow_UsesCenteredCardLayout` 测试**

移除对 `CredentialModeSurface` 的断言（已删除）。

- [ ] **Step 2: 更新 `LoginWindow_RendersRecentAccountDropdown` 测试**

1. 移除 `Text="最近使用：admin"` 断言（已替换为增强区块）
2. 添加对新最近账号区块的断言：`x:Name="HeroRecentAccountBlock"`, `TxtRecentAccountName`, `TxtRecentAccountLastLogin`

- [ ] **Step 3: 更新/替换 `LoginWindow_UsesExplicitCredentialModeSelector` 测试**

此测试断言凭证模式切换控件存在，需替换为新的断言：
- 不再包含 `CredentialModePanel`, `BtnPasswordMode`, `BtnPinMode`, `PinCredentialPanel`
- 不再包含 `LoginCredentialMode`, `SetLoginCredentialMode` 等

- [ ] **Step 4: 更新 `LoginWindow_GuardsCredentialModeControlsDuringInitialization` 测试**

移除对 PIN 相关控件 null-check 的断言。

- [ ] **Step 5: 添加新测试：`LoginWindow_ContainsThemeToggle`**

验证 XAML 包含：
- `x:Name="LoginThemeToggle"`
- `Content="柔粉"`
- `Content="清蓝"`
- `Style="{StaticResource AppSegmentedTabShell}"`

- [ ] **Step 6: 添加新测试：`LoginWindow_ContainsFirstTimeHint`**

验证 XAML 包含：
- `首次使用`
- `管理员初始化`
- `了解本地多用户模式`
- 不包含 `注册`、`创建账号`、`新增成员`

- [ ] **Step 7: 添加新测试：`LoginWindow_DoesNotContainPinControls`**

验证 XAML 不包含：
- `BtnPinMode`
- `PinCredentialPanel`
- `SetupPinBox`
- `快捷 PIN`

- [ ] **Step 8: 运行所有测试**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1`
Expected: All PASS

---

### Task 8: 最终验证与回归

**Files:** None (verification only)

- [ ] **Step 1: 完整编译**

Run: `rtk dotnet build ClosetApp.slnx /m:1`
Expected: 0 errors, 0 warnings (或仅 pre-existing warnings)

- [ ] **Step 2: 运行全部测试**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj /m:1`
Expected: All PASS

- [ ] **Step 3: 代码审查检查清单**

- [ ] PIN 仅从登录主入口下线，底层 `PinHash`、`SetPinAsync`、`ResetMemberCredentialAsync` 中的 PIN 能力保留
- [ ] 登录只走密码校验
- [ ] 主题切换使用现有 `ThemeService`，不新增登录页专属主题状态
- [ ] 不包含"注册/新增成员/创建账号"入口文案
- [ ] 包含"首次使用/管理员初始化"说明区
- [ ] 最近账号区块展示最近一次使用账号的可感知信息
- [ ] 所有新增控件复用现有按钮、输入框、typography token 和主题资源
