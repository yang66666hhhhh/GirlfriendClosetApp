# 设置页密度优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不改变设置页核心功能和信息结构的前提下，压缩首屏空白，提升设置概览与外观区的产品级完成度。

**Architecture:** 继续沿用 `SettingsTab` 的“概览 + 外观 + 双列设置区”结构，不新增窗口或独立预览系统。重点通过 `SettingsTab.xaml` 和 `AppearanceSettingsPanel.xaml` 的布局重排、共享设置样式微调，以及现有 ViewModel 摘要字段复用来提升信息密度。

**Tech Stack:** WPF, XAML shared styles, CommunityToolkit.Mvvm, existing `SettingsViewModel` summaries

---

## File Map

| File | Action | Responsibility |
|------|--------|---------------|
| `ClosetApp.UI/Views/SettingsTab.xaml` | Modify | 收紧顶部概览布局、减少首屏纵向空白、让概览统计更紧凑 |
| `ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml` | Modify | 将左侧大预览改为紧凑预览摘要卡，重排主题卡与右侧设置卡 |
| `ClosetApp.UI/Themes/Controls/Settings.xaml` | Modify | 新增或收敛设置页紧凑卡片/摘要样式，保证视觉统一 |
| `ClosetApp.Tests/SettingsLayoutTests.cs` | Modify | 更新设置页与外观区布局断言，保护密度优化结果 |

---

### Task 1: 先写布局测试，锁定新的密度目标

**Files:**
- Modify: `ClosetApp.Tests/SettingsLayoutTests.cs`

- [ ] **Step 1: 为设置概览的紧凑结构补充失败断言**

在 `SettingsLayoutTests.cs` 的 `SettingsTab_UsesWorkbenchOverviewStructure` 和 `SettingsTab_UsesTactileOverviewSurfaces` 附近补充断言，目标是锁定：

```csharp
Assert.Contains("x:Name=\"SettingsOverviewSummaryGrid\"", xaml);
Assert.Contains("x:Name=\"SettingsOverviewMetricMatrix\"", xaml);
Assert.DoesNotContain("x:Name=\"SettingsOverviewCompactSummaryGrid\"", xaml);
Assert.DoesNotContain("x:Name=\"SettingsOverviewMetricsGrid\"", xaml);
```

- [ ] **Step 2: 为外观区“小预览摘要卡”补充失败断言**

在 `AppearanceSettingsPanel_UsesWorkbenchCards` 里补充断言，目标是锁定：

```csharp
Assert.Contains("x:Name=\"AppearancePreviewSummaryCard\"", xaml);
Assert.Contains("x:Name=\"AppearancePreviewSummaryHeader\"", xaml);
Assert.Contains("x:Name=\"AppearancePreviewSummarySurface\"", xaml);
Assert.DoesNotContain("x:Name=\"AppearanceWorkbenchHeader\"", xaml); // 若最终移除旧命名则保留此断言
```

如果决定保留 `AppearanceWorkbenchHeader`，则不要写最后一条，改为断言新的预览节点必须存在。

- [ ] **Step 3: 为主题卡区和右侧设置区的新节奏补充断言**

补充断言，防止外观区继续保留大面积空白：

```csharp
Assert.Contains("x:Name=\"AppearanceLeftRail\"", xaml);
Assert.Contains("x:Name=\"AppearanceRightRail\"", xaml);
Assert.Contains("x:Name=\"AppearanceThemeSelectionCard\"", xaml);
Assert.Contains("x:Name=\"AppearanceControlsStack\"", xaml);
```

- [ ] **Step 4: 先跑设置布局测试，确认失败**

Run:

```powershell
rtk pwsh -Command "dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter 'FullyQualifiedName~SettingsLayoutTests' /m:1"
```

Expected: 至少 1 个失败，报缺少新的布局节点或仍存在旧节点。

- [ ] **Step 5: Commit**

```bash
git add ClosetApp.Tests/SettingsLayoutTests.cs
git commit -m "test: lock settings density layout targets"
```

---

### Task 2: 收紧设置概览首屏，压缩顶部空白

**Files:**
- Modify: `ClosetApp.UI/Views/SettingsTab.xaml`
- Test: `ClosetApp.Tests/SettingsLayoutTests.cs`

- [ ] **Step 1: 将概览摘要条改成更紧凑的两行摘要区**

把当前的 `SettingsOverviewHeroSurface` 内部从“1 行 4 列超长摘要带”改成“2 行 2 列摘要网格”，避免信息横向摊平造成空旷感。目标结构：

```xml
<Border x:Name="SettingsOverviewHeroSurface"
        Margin="0,10,0,0"
        Style="{StaticResource SettingsWorkbenchHeroNoteCard}">
    <Grid x:Name="SettingsOverviewSummaryGrid">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="10"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="16"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- 主题 / 卡片 -->
        <!-- AI / 城市 -->
    </Grid>
</Border>
```

- [ ] **Step 2: 将四张统计卡改成 2x2 矩阵**

把当前 `SettingsOverviewMetricsGrid` 的 1 行 4 卡改成 2 行 2 卡矩阵，减少横向拉伸感，提升首屏利用率：

```xml
<Grid x:Name="SettingsOverviewMetricMatrix"
      Margin="0,10,0,0">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="12"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="12"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <!-- 原图 / 缓存 -->
    <!-- AI / 日志 -->
</Grid>
```

四张卡继续复用 `SettingsWorkbenchMetricCard`，不要新造视觉风格。

- [ ] **Step 3: 收顶部节奏**

同步微调以下间距，避免概览区继续显空：

```xml
<ScrollViewer Grid.Row="1"
              VerticalScrollBarVisibility="Auto"
              Padding="44,10,44,40">

<Border x:Name="SettingsOverviewHero"
        Style="{StaticResource SettingsWorkbenchHeroCard}">
    <!-- Padding 从 20,18 收到 18,16 或同量级 -->
</Border>

<Border x:Name="SettingsWorkbenchSectionDivider"
        Height="1"
        Margin="0,2,0,14"
        Background="{DynamicResource BorderDividerBrush}"/>
```

注意：不要改成营销式大标题，不要增加说明段落。

- [ ] **Step 4: 跑设置布局测试**

Run:

```powershell
rtk pwsh -Command "dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter 'FullyQualifiedName~SettingsLayoutTests' /m:1"
```

Expected: 概览区相关断言通过，外观区相关断言仍可能失败。

- [ ] **Step 5: Commit**

```bash
git add ClosetApp.UI/Views/SettingsTab.xaml ClosetApp.Tests/SettingsLayoutTests.cs
git commit -m "feat: tighten settings overview density"
```

---

### Task 3: 将外观区左侧大预览降级为“小预览摘要卡”

**Files:**
- Modify: `ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml`
- Modify: `ClosetApp.UI/Themes/Controls/Settings.xaml`
- Test: `ClosetApp.Tests/SettingsLayoutTests.cs`

- [ ] **Step 1: 重排外观区左右结构**

把当前 `AppearanceSettingsPanel` 的主 Grid 从“左大右小”改成更均衡的左右两栏，左侧承载主题选择 + 小预览，右侧承载控制项：

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="1.02*"/>
        <ColumnDefinition Width="16"/>
        <ColumnDefinition Width="0.98*"/>
    </Grid.ColumnDefinitions>

    <StackPanel x:Name="AppearanceLeftRail" Grid.Column="0"/>
    <StackPanel x:Name="AppearanceRightRail" Grid.Column="2"/>
</Grid>
```

- [ ] **Step 2: 左侧保留主题选择，但包进更明确的选择卡**

给当前 `AppearanceThemeGrid` 外层加一个 `AppearanceThemeSelectionCard`，这样左侧不再是裸露大块空白：

```xml
<Border x:Name="AppearanceThemeSelectionCard"
        Style="{StaticResource SettingsInsetCard}">
    <StackPanel>
        <TextBlock Text="主题"
                   Style="{StaticResource PageFieldLabelText}"/>
        <Grid x:Name="AppearanceThemeGrid"
              Margin="0,10,0,0">
            <!-- ThemeRoseCard / ThemeBlueCard -->
        </Grid>
    </StackPanel>
</Border>
```

ThemeCard 本身不新增复杂逻辑，优先复用现有 `ThemeCard`。

- [ ] **Step 3: 新增“小预览摘要卡”而不是大面积预览**

在左侧主题卡下方新增一张紧凑卡片，使用现有 ViewModel 摘要字段，不创建新的预览引擎：

```xml
<Border x:Name="AppearancePreviewSummaryCard"
        Style="{StaticResource SettingsInsetCard}"
        Margin="0,12,0,0">
    <StackPanel x:Name="AppearancePreviewSummaryHeader">
        <TextBlock Text="预览"
                   Style="{StaticResource PageFieldLabelText}"/>
        <Border x:Name="AppearancePreviewSummarySurface"
                Margin="0,10,0,0"
                Style="{StaticResource SettingsSurfacePanel}">
            <StackPanel>
                <TextBlock Text="{Binding ThemeSummary}"
                           Style="{StaticResource PageSummaryValueText}"/>
                <TextBlock Text="{Binding FontSizeSummary}"
                           Style="{StaticResource PageMutedBodyText}"
                           Margin="0,6,0,0"/>
                <TextBlock Text="{Binding OutfitCardDisplaySummary}"
                           Style="{StaticResource PageMutedBodyText}"
                           Margin="0,4,0,0"/>
            </StackPanel>
        </Border>
    </StackPanel>
</Border>
```

关键点：
- 保留预览，但只做“状态确认型小预览”
- 不再出现大面积示意空白
- 不新增新的 code-behind 逻辑

- [ ] **Step 4: 右侧控制卡收紧节奏**

把右侧 `AppearanceDisplayModeCard`、`AppearanceFontSizeCard`、`AppearanceAppInfoCard` 包进 `AppearanceControlsStack`，统一收紧外边距：

```xml
<StackPanel x:Name="AppearanceControlsStack" Grid.Column="2">
    <!-- AppearanceDisplayModeCard -->
    <!-- AppearanceFontSizeCard Margin="0,10,0,0" -->
    <!-- AppearanceAppInfoCard Margin="0,10,0,0" -->
</StackPanel>
```

同时将这些卡片内部标题与正文间距从 `8/10/12` 级别收至 `6/8/10` 级别，避免白块感过重。

- [ ] **Step 5: 如需样式支持，在 Settings.xaml 增加紧凑摘要样式**

如果 `SettingsInsetCard` 和 `SettingsSurfacePanel` 还不够，可以只补充一组轻量样式，不要引入新体系：

```xml
<Style x:Key="SettingsPreviewSummaryValue" TargetType="TextBlock">
    <Setter Property="FontSize" Value="{DynamicResource FontSize.Label}"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}"/>
</Style>
```

仅在确实需要时新增；优先复用 `PageSummaryValueText` 和 `PageMutedBodyText`。

- [ ] **Step 6: 跑设置布局测试**

Run:

```powershell
rtk pwsh -Command "dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter 'FullyQualifiedName~SettingsLayoutTests' /m:1"
```

Expected: 外观区断言全部通过。

- [ ] **Step 7: Commit**

```bash
git add ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml ClosetApp.UI/Themes/Controls/Settings.xaml ClosetApp.Tests/SettingsLayoutTests.cs
git commit -m "feat: replace oversized appearance preview with compact summary"
```

---

### Task 4: 顺手统一下方区块节奏，避免上面收紧后下面显得松散

**Files:**
- Modify: `ClosetApp.UI/Views/SettingsTab.xaml`
- Modify: `ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml`
- Modify: `ClosetApp.UI/Components/Settings/StorageLocationsSettingsPanel.xaml`
- Test: `ClosetApp.Tests/SettingsLayoutTests.cs`

- [ ] **Step 1: 收双列区的纵向节奏**

在 `SettingsTab.xaml` 中把双列容器的间距与段间距统一到更紧凑的节奏：

```xml
<Grid x:Name="SettingsWorkbenchColumns"
      Margin="0,0,0,8">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="16"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
</Grid>
```

- [ ] **Step 2: 收天气和文件位置的标题节奏**

对 `WeatherPreferencesSettingsPanel.xaml` 与 `StorageLocationsSettingsPanel.xaml` 做最小节奏修正：

```xml
<Border x:Name="WeatherCityCard"
        Style="{StaticResource SettingsInsetCard}"
        Margin="0,0,0,12">

<Border x:Name="WeatherRecommendationCard"
        Style="{StaticResource SettingsInsetCard}"
        Margin="0,12,0,0">

<Grid x:Name="StorageHeaderGrid"
      Margin="0,0,0,12">
```

只调整节奏，不改变控件组合。

- [ ] **Step 3: 跑设置布局测试**

Run:

```powershell
rtk pwsh -Command "dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter 'FullyQualifiedName~SettingsLayoutTests' /m:1"
```

Expected: 所有 SettingsLayoutTests 通过。

- [ ] **Step 4: Commit**

```bash
git add ClosetApp.UI/Views/SettingsTab.xaml ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml ClosetApp.UI/Components/Settings/StorageLocationsSettingsPanel.xaml ClosetApp.Tests/SettingsLayoutTests.cs
git commit -m "style: unify settings section rhythm"
```

---

### Task 5: 最终验证

**Files:**
- No code changes expected

- [ ] **Step 1: 跑设置相关测试**

Run:

```powershell
rtk pwsh -Command "dotnet test ClosetApp.Tests\ClosetApp.Tests.csproj --filter 'FullyQualifiedName~Settings' /m:1"
```

Expected: PASS

- [ ] **Step 2: 跑全量构建**

Run:

```powershell
rtk pwsh -Command "dotnet build ClosetApp.slnx /m:1"
```

Expected: 0 errors

- [ ] **Step 3: 手动验收设置页首屏**

检查点：

```text
1. 设置概览首屏不再出现一整条过长摘要带 + 一排横向摊平卡片
2. 外观区左侧不再被大面积预览占据
3. 小预览卡能快速表达“当前主题 / 字体 / 卡片默认”
4. 右侧三张卡的高度和节奏更整齐
5. 下方天气与文件位置在上半区收紧后不会显得过松
6. 全页仍然完全复用现有设置卡片、共享按钮、Typography token 和 ThemeCard 风格
```

- [ ] **Step 4: Commit**

```bash
git add ClosetApp.UI/Views/SettingsTab.xaml ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml ClosetApp.UI/Themes/Controls/Settings.xaml ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml ClosetApp.UI/Components/Settings/StorageLocationsSettingsPanel.xaml ClosetApp.Tests/SettingsLayoutTests.cs
git commit -m "feat: polish settings page density"
```

---

## Notes for Implementers

- 不要新增新的设置页面或弹窗体系。
- 不要引入营销式 hero、插画、渐变背景或无信息装饰块。
- 小预览必须复用现有摘要字段，不要新增 ViewModel 复杂状态。
- 优先改布局与节奏，避免为了“填满”而增加解释性文案。
- 如 ThemeCard 仍显得过高，优先通过宿主布局控制占比；只有在确实无法收紧时，才单独为 ThemeCard 增加“compact”模式，并同步补测试。
