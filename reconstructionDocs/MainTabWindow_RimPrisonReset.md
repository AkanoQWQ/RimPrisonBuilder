# MainTabWindow_RimPrisonReset.cs

**大小**: 155KB / 3446 行
**路径**: `RIMPRISON/Source/RimPrison.Reset/MainTabWindow_RimPrisonReset.cs`

---

## 作用

整个 Mod 的**唯一主窗口**。包含 7 个子页面：

| Tab | 行号范围 | 功能 |
|-----|---------|------|
| 总览 (Overview) | 245–999 | 压制度环形图、人口饼图、因素影响、日程编辑器、活动日志、守卫分配 |
| 囚犯管理 (Prisoners) | 1762–2483 | 囚犯卡片网格（年龄/状态筛选）、详细信息面板、余额调整、医疗选择、器官抵扣状态 |
| 工作安排 (Work) | 1824–2193 | 候选工作侧栏（拖拽多选）、工作优先级矩阵、Shift批量调整、列头拖拽排序、时薪设置 |
| 食物管理 (Food) | 2657–2943 | 四档餐标（福利/婴儿/标准/精致）、价格设定、搜索筛选、婴儿特殊食物选项 |
| 成瘾品 (Drug) | 2676–2819 | 已选药品面板 + 目录网格、价格调整、允许/禁用切换 |
| 服装管理 (Apparel) | 2945–3039 | 婴儿/儿童/成人三列、跨生命阶段开关、武器+服装搜索选择 |
| 其它设置 (Settings) | 3041–3193 | 货币名称、赎身价格、工资发放模式、器官抵扣配置、殖民地工作禁区 |

---

## 致命问题 (必须改)

### 1. 硬编码中文字符串 — 零国际化
```csharp
private static readonly string TitleText = "边缘监狱"; // "边缘监狱"
```
全文件 3000+ 行**没有任何翻译键**。所有 UI 文本都是 Unicode 转义序列或直接中文字符串。如果要出多语言版本，必须全部重写。

### 2. PERF: `AllManagedPawns()` 每帧创建新 List
```csharp
private List<Pawn> AllManagedPawns()
{
    return PrisonResetUtility.GetManagedPopulationForDisplay(CurrentMap);
}
```
该方法在 `DoWindowContents` → 各个 Draw 方法中被调用**数十次**。每次返回一个新的 `List<Pawn>` 实例。在 60fps 下，这就是每秒数十次堆分配。应该缓存到 `PostOpen` + `SetDirty` 模式。

### 3. PERF: `Component` 属性每帧查 MapComponent
```csharp
private PrisonResetMapComponent Component => PrisonResetUtility.GetComponent(CurrentMap);
```
`DoWindowContents` 里每帧调用，且每个子方法都重复 `var component = Component`。应改为初始化时缓存。

### 4. PERF: 环形图纹理缓存永不清理
```csharp
private static readonly Dictionary<string, OverviewRingTextureCacheEntry> OverviewRingTextureCache = new();
```
- 静态 Dictionary，键是 cacheKey（页面名如 "overview.population"），值是 Texture2D + Color32[] 像素数据
- **永远不删除条目**，切地图、重新打开窗口时都累积
- 在有多个环形图的 Overview 页面上，每帧调用 `DrawRingChart` → `GetOverviewPolarTexture` 
- 每次 signature 变化时重新计算 256×256=65536 像素的双重循环（含 `Mathf.Sqrt`、`Mathf.Atan2`）
- size=256 时每次分配 `new Color32[65536]`
- **建议**：缓存 Texture2D 可以，但必须限制缓存大小（LRU），并在 OnDestroy/map change 时清理

### 5. PERF: OnGUI 里的大量堆分配
以下每帧都在 `Draw*` 中 `new` 出来：
```csharp
new List<(float fraction, Color color)>  // BuildPopulationSlices/BuildSuppressionSlices/BuildAgeBandSlices
new List<FloatMenuOption>                 // 各种下拉菜单（OnGUI中创建但只在点击时使用）
new List<(string, string)>                // DrawOverviewFactorsCard
new List<Pawn>                            // VisibleManagedPawns/GetDrugDefsForDisplay/ResolveVisibleWorkTypes
```
FloatMenu 的 List 创建尤其浪费——按钮点击时才需要，不应该在 OnGUI 中预创建。

### 6. PERF: `SearchCandidateMatches` 的 O(n*m) 模糊搜索
```csharp
private static bool SearchCandidateMatches(string value, string normalizedQuery)
{
    var candidateIndex = 0;
    for (var queryIndex = 0; queryIndex < normalizedQuery.Length && candidateIndex < candidate.Length; queryIndex++)
    {
        var found = candidate.IndexOf(normalizedQuery[queryIndex], candidateIndex);
        if (found < 0) return false;
        candidateIndex = found + 1;
    }
    return true;
}
```
这个 fuzzy matching 方法每次搜索都遍历所有候选项的所有字符。在有 100+ 个 ThingDef 的列表中，每帧执行数十次。应该只在实际输入变化时重新计算，并用缓存。

### 7. 日程编辑器动画用 `Time.realtimeSinceStartup`
```csharp
overviewScheduleTransitionStartedAt = Time.realtimeSinceStartup;
```
`GetOverviewScheduleTransitionProgress()` 每帧计算实时时间差。但在 RimWorld 暂停时（窗口打开），`Time.realtimeSinceStartup` 仍在流逝。这意味着用户切回来时动画可能已经跳过了。应该用 `Time.time` 或基于帧的计时。

### 8. 数据持久化缺失
大量 UI 状态（搜索文本、scrollbar 位置、选中的 tab、filter 状态）存在字段中但没有通过 `Scribe` 持久化。关闭再打开窗口会丢失所有状态。

---

## 重构建议

1. **按 tab 拆分文件**（最优先）：
   - `MainTabWindow_RimPrisonReset.cs` → 骨架 + 导航
   - `RPR_OverviewTab.cs` — 总览
   - `RPR_PrisonersTab.cs` — 囚犯管理
   - `RPR_WorkTab.cs` — 工作安排
   - `RPR_FoodTab.cs` — 食物管理
   - `RPR_DrugTab.cs` — 成瘾品
   - `RPR_ApparelTab.cs` — 服装
   - `RPR_SettingsTab.cs` — 设置

2. **UI 组件复用**：`DrawRingChart`、`DrawAgeBandRing`、`DrawSelectedDefList`、`DrawTextBlock`、`DrawToggleChip` 等是通用组件，应该抽取到独立的 UI 工具类。

3. **翻译系统**：全部硬编码字符串 → `"Key".Translate()`

4. **数据缓存**：`AllManagedPawns()` 的结果应该在窗口打开时缓存，数据变化时通过事件/标记刷新。

5. **环形图缓存**：限制最大缓存条目数（如 8），窗口关闭时清理全部纹理资源。

6. **FloatMenu 延迟创建**：按钮点击时再 `new List<FloatMenuOption>()`，不在 OnGUI 中创建。

7. **日程动画**：改用 `GenTicks.TicksGame` 或 `Find.TickManager.TicksGame` 做基于游戏的计时。
