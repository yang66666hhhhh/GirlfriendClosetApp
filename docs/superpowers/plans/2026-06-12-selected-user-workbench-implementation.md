# Selected User Workbench Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把用户管理弹窗重构为“薄状态栏 + 左侧成员导航 + 右侧选中用户工作台”，让当前选中的用户成为唯一主视觉中心。

**Architecture:** 保持现有 `LocalUserManagementDialog` 的业务行为与 code-behind 事件处理不变，主要通过重排 XAML 结构与更新布局测试实现这次重设计。顶部当前用户区域降级为状态栏；左侧继续承接搜索、列表与新增用户；右侧整理为单用户 Hero、账号资料、安全与危险操作的连续工作台。

**Tech Stack:** WPF XAML、code-behind、xUnit 布局测试、`rtk dotnet test`、`rtk dotnet build`

---

## File Map

- Modify: `ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml`
  - 负责弹窗整体布局、状态栏、成员导航区、右侧工作台结构。
- Modify: `ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml.cs`
  - 如布局命名变化影响到现有控件引用，补齐命名同步；避免破坏现有事件行为。
- Modify: `ClosetApp.Tests/LocalUserManagementDialogLayoutTests.cs`
  - 负责为新布局写失败测试并保护结构回归。
- Modify: `ClosetApp.Tests/LoginWindowLayoutTests.cs`
  - 如用户管理弹窗关键命名或主结构断言变化，补充同步。
- Modify: `PROJECT_DOCUMENTATION.md`
  - 更新用户管理弹窗的信息架构说明。
- Modify: `README.md`
  - 更新高频入口说明，保持和当前 UI 一致。

### Task 1: 锁定新布局测试

**Files:**
- Modify: `ClosetApp.Tests/LocalUserManagementDialogLayoutTests.cs`
- Test: `ClosetApp.Tests/LocalUserManagementDialogLayoutTests.cs`

- [ ] **Step 1: 写失败测试，约束“薄状态栏 + 选中用户工作台”结构**

```csharp
[Fact]
public void UserManagementDialog_UsesSelectedUserWorkbenchLayout()
{
    var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

    Assert.Contains("x:Name=\"CurrentSessionBar\"", xaml);
    Assert.Contains("x:Name=\"SelectedUserWorkbench\"", xaml);
    Assert.Contains("x:Name=\"SelectedUserHeroCard\"", xaml);
    Assert.Contains("x:Name=\"SelectedUserAccountSection\"", xaml);
    Assert.Contains("x:Name=\"SelectedUserSecuritySection\"", xaml);
    Assert.Contains("x:Name=\"SelectedUserDangerSection\"", xaml);
    Assert.DoesNotContain("x:Name=\"CurrentUserWorkbenchCard\"", xaml);
}
```

- [ ] **Step 2: 运行测试，确认它先失败**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter "FullyQualifiedName~UserManagementDialog_UsesSelectedUserWorkbenchLayout" /m:1 /p:UseSharedCompilation=false`

Expected: FAIL，提示找不到新的状态栏或工作台命名。

- [ ] **Step 3: 再写一条测试，约束左侧只做导航，右侧拿到更多空间**

```csharp
[Fact]
public void UserManagementDialog_GivesMoreSpaceToSelectedUserDetail()
{
    var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

    Assert.Contains("<ColumnDefinition Width=\"300\"/>", xaml);
    Assert.Contains("<ColumnDefinition Width=\"32\"/>", xaml);
    Assert.Contains("<ColumnDefinition Width=\"*\"/>", xaml);
    Assert.Contains("Padding=\"26,26,26,32\"", xaml);
}
```

- [ ] **Step 4: 运行测试，确认它也失败**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter "FullyQualifiedName~UserManagementDialog_GivesMoreSpaceToSelectedUserDetail" /m:1 /p:UseSharedCompilation=false`

Expected: FAIL，提示列宽或 padding 还停留在旧值。

- [ ] **Step 5: 提交这一轮测试改动**

```bash
rtk git add ClosetApp.Tests/LocalUserManagementDialogLayoutTests.cs
rtk git commit -m "test: lock selected user workbench layout"
```

### Task 2: 重排 XAML 为“薄状态栏 + 左侧导航 + 右侧工作台”

**Files:**
- Modify: `ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml`
- Test: `ClosetApp.Tests/LocalUserManagementDialogLayoutTests.cs`

- [ ] **Step 1: 把顶部当前用户大卡改为薄状态栏**

```xml
<Border x:Name="CurrentSessionBar"
        Grid.Row="0"
        Style="{StaticResource UserManager.SectionCard}"
        Padding="14,10">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="12"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <shared:LocalUserAvatar x:Name="CurrentSessionAvatar"
                                Width="34"
                                Height="34"
                                ShowStatus="False"/>
        <StackPanel Grid.Column="2"
                    VerticalAlignment="Center">
            <TextBlock x:Name="TxtCurrentSessionUser"
                       FontSize="12"
                       FontWeight="SemiBold"/>
            <TextBlock x:Name="TxtCurrentSessionContext"
                       FontSize="11"/>
        </StackPanel>
    </Grid>
</Border>
```

- [ ] **Step 2: 让主区域左右分栏，右侧成为真正主工作区**

```xml
<Grid Grid.Row="2">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="300"/>
        <ColumnDefinition Width="32"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
</Grid>
```

- [ ] **Step 3: 左侧保留成员导航，不再承载详情语义**

```xml
<Border x:Name="MemberNavigatorCard"
        Grid.Column="0"
        Style="{StaticResource UserManager.SectionCard}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="14"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="16"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
    </Grid>
</Border>
```

- [ ] **Step 4: 右侧改成连续的选中用户工作台**

```xml
<ScrollViewer Grid.Column="2"
              VerticalScrollBarVisibility="Auto"
              HorizontalScrollBarVisibility="Disabled">
    <StackPanel x:Name="SelectedUserWorkbench">
        <Border x:Name="SelectedUserHeroCard" />
        <Border x:Name="SelectedUserAccountSection" />
        <Border x:Name="SelectedUserSecuritySection" />
        <Border x:Name="SelectedUserDangerSection" />
    </StackPanel>
</ScrollViewer>
```

- [ ] **Step 5: 运行两条新测试，确认结构改动通过**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter "FullyQualifiedName~UserManagementDialog_UsesSelectedUserWorkbenchLayout|FullyQualifiedName~UserManagementDialog_GivesMoreSpaceToSelectedUserDetail" /m:1 /p:UseSharedCompilation=false`

Expected: PASS

- [ ] **Step 6: 提交 XAML 主结构重排**

```bash
rtk git add ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml ClosetApp.Tests/LocalUserManagementDialogLayoutTests.cs
rtk git commit -m "feat: redesign user management around selected user"
```

### Task 3: 接通现有 code-behind 到新的顶部状态栏

**Files:**
- Modify: `ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml.cs`
- Test: `ClosetApp.Tests/LoginWindowLayoutTests.cs`

- [ ] **Step 1: 写测试，确认旧的当前用户 Hero 命名不再被依赖**

```csharp
[Fact]
public void UserManagementDialog_CodeBehindSupportsSessionBarLayout()
{
    var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml.cs"));

    Assert.Contains("ApplyCurrentUserHero", code);
    Assert.Contains("TxtCurrentSessionUser", code);
    Assert.Contains("TxtCurrentSessionContext", code);
}
```

- [ ] **Step 2: 运行测试，确认先失败**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter "FullyQualifiedName~UserManagementDialog_CodeBehindSupportsSessionBarLayout" /m:1 /p:UseSharedCompilation=false`

Expected: FAIL，因为 code-behind 还没写入新控件名。

- [ ] **Step 3: 更新 `ApplyCurrentUserHero`，让它只服务状态栏**

```csharp
private void ApplyCurrentUserHero(LocalUserRow row)
{
    CurrentSessionAvatar.AvatarPath = row.AvatarPath;
    CurrentSessionAvatar.Initial = row.AvatarInitial;
    TxtCurrentSessionUser.Text = $"{row.EditableName} · {row.RoleText}";
    TxtCurrentSessionContext.Text = row.IsCurrent ? "当前登录用户" : row.SessionText;
}
```

- [ ] **Step 4: 若 XAML 命名有调整，同步 Selected 用户区域 DataContext 和事件引用**

```csharp
private LocalUserRow? ResolveTargetRow(object sender, bool preferCurrentUser)
{
    if (!preferCurrentUser && (sender as FrameworkElement)?.DataContext is LocalUserRow boundRow)
        return boundRow;

    if (_currentUserId == null)
        return null;

    return _allRows.FirstOrDefault(row => row.Id == _currentUserId.Value);
}
```

- [ ] **Step 5: 运行针对性测试，确认 code-behind 接通**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter "FullyQualifiedName~UserManagementDialog_CodeBehindSupportsSessionBarLayout|FullyQualifiedName~UserManagementDialog_UsesSelectedUserWorkbenchLayout" /m:1 /p:UseSharedCompilation=false`

Expected: PASS

- [ ] **Step 6: 提交 code-behind 同步**

```bash
rtk git add ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml.cs ClosetApp.Tests/LoginWindowLayoutTests.cs
rtk git commit -m "refactor: sync user management code-behind with session bar"
```

### Task 4: 收紧说明文案并统一右侧工作台节奏

**Files:**
- Modify: `ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml`
- Test: `ClosetApp.Tests/LocalUserManagementDialogLayoutTests.cs`

- [ ] **Step 1: 写测试，防止无意义说明文案重新回流**

```csharp
[Fact]
public void UserManagementDialog_RemovesRedundantExplanatoryCopy()
{
    var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

    Assert.DoesNotContain("先维护当前账号", xaml);
    Assert.DoesNotContain("新增用户只放在这里", xaml);
    Assert.DoesNotContain("这里先处理自己的头像", xaml);
}
```

- [ ] **Step 2: 运行测试，确认先失败或覆盖现状**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter "FullyQualifiedName~UserManagementDialog_RemovesRedundantExplanatoryCopy" /m:1 /p:UseSharedCompilation=false`

Expected: 若仍有旧文案则 FAIL；若现状已满足则 PASS，可继续下一步。

- [ ] **Step 3: 把右侧 Hero、账号资料、安全、危险区块的说明文案统一压短**

```xml
<TextBlock Text="账号资料"
           FontSize="14"
           FontWeight="SemiBold"/>
<TextBlock Text="登录账号与展示名称"
           Margin="0,4,0,0"
           FontSize="12"/>
```

- [ ] **Step 4: 统一右侧区块的 padding 和 section gap**

```xml
<StackPanel x:Name="SelectedUserWorkbench">
    <Border x:Name="SelectedUserHeroCard"
            Padding="26,26,26,24"
            Margin="0,0,0,16"/>
    <Border x:Name="SelectedUserAccountSection"
            Padding="26,22,26,24"
            Margin="0,0,0,16"/>
</StackPanel>
```

- [ ] **Step 5: 运行布局测试，确认仍通过**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter "FullyQualifiedName~LocalUserManagementDialogLayoutTests" /m:1 /p:UseSharedCompilation=false`

Expected: PASS

- [ ] **Step 6: 提交文案和节奏收口**

```bash
rtk git add ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml ClosetApp.Tests/LocalUserManagementDialogLayoutTests.cs
rtk git commit -m "style: tighten user management workbench copy"
```

### Task 5: 文档同步与总验证

**Files:**
- Modify: `PROJECT_DOCUMENTATION.md`
- Modify: `README.md`
- Test: `ClosetApp.Tests/LocalUserManagementDialogLayoutTests.cs`

- [ ] **Step 1: 更新 README 中对用户管理弹窗的描述**

```md
- 超级管理员可进入“薄状态栏 + 左侧成员导航 + 右侧选中用户工作台”的用户管理弹窗，维护用户新增、编辑、头像上传/移除、重置凭证和删除。
```

- [ ] **Step 2: 更新项目文档中的导航与用户管理说明**

```md
用户管理已重排为“顶部状态栏 + 左侧成员导航 + 右侧选中用户工作台”：选中某个用户后，右侧连续展示该用户的头像、账号资料、安全区和危险操作。
```

- [ ] **Step 3: 运行用户管理相关测试**

Run: `rtk dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter "FullyQualifiedName~LocalUserManagementDialogLayoutTests|FullyQualifiedName~LoginWindowLayoutTests" /m:1 /p:UseSharedCompilation=false`

Expected: PASS

- [ ] **Step 4: 运行全量构建验证**

Run: `rtk dotnet build ClosetApp.slnx /m:1`

Expected: `0 errors, 0 warnings`

- [ ] **Step 5: 提交文档与最终收口**

```bash
rtk git add README.md PROJECT_DOCUMENTATION.md docs/superpowers/specs/2026-06-12-selected-user-workbench-design.md docs/superpowers/plans/2026-06-12-selected-user-workbench-implementation.md
rtk git commit -m "docs: document selected user workbench redesign"
```
