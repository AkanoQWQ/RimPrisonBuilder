# 原 UI 重构方案 — 保留外观，铲除史山

## 核心原则

1. **外观不变** — 作者和玩家看到的东西一模一样
2. **逻辑替换** — 所有数据源改成我们的 Comp/GameComponent，不用他的 MapComponent
3. **按 Tab 拆文件** — 3446 行 1 文件 → 8 个文件
4. **翻译键** — 硬编码 Unicode 转义全部 → `"Key".Translate()`

## 原 UI 结构

```
MainTabWindow_RimPrisonReset (3446 行，7 个 Tab)
├── Overview (总览)      — 环形图+饼图+日程+日志+守卫
├── Prisoners (囚犯)     — 卡片网格+搜索+筛选
├── Work (工作)          — 工作矩阵+侧栏
├── Food (食物)          — 4 档餐标
├── Drug (成瘾品)        — 药品目录
├── Apparel (服装)       — 3 年龄带
└── Settings (设置)      — 杂项配置
```

## 重构后文件结构

```
Source/UI/
├── MainTabWindow_Prisoners.cs     ← 已有，保留（我们的 prisoner management）
│
├── RPR_OverviewTab.cs             ← 【新】总览页
├── RPR_PrisonersTab.cs            ← 【新】囚犯管理页
├── RPR_WorkTab.cs                 ← 【新】工作安排页
├── RPR_FoodTab.cs                 ← 【新】食物管理页
├── RPR_DrugTab.cs                 ← 【新】成瘾品页
├── RPR_ApparelTab.cs              ← 【新】服装管理页
├── RPR_SettingsTab.cs             ← 【新】设置页
│
├── RPR_UiStyle.cs                 ← 颜色常量 + DrawPanel/DrawBorder（直接搬）
├── RPR_ScheduleEditor.cs          ← 日程编辑器控件（拖拽+timeline）
├── RPR_ActivityLogDrawer.cs       ← 活动日志绘制
├── RPR_PolarChartRenderer.cs      ← 环形图/饼图渲染（纹理缓存修复版）
│
├── Dialog_PrisonerManagement.cs   ← 已有，保持
├── Dialog_GrantCoupons.cs         ← 已有，保持
└── PrisonerBalanceGizmo.cs        ← 【新】选中囚犯时的余额 Gizmo
```

## 每个 Tab 的重构模式

每个 Tab 遵循统一的模式：

```csharp
public class RPR_XxxTab
{
    // 缓存字段（窗口生命周期）
    private List<Pawn> cachedPawns;
    private int cachedPawnsTick = -1;
    private string searchText = "";
    private Vector2 scrollPos;

    // DrawXxxTab(Rect rect, ParentWindowData data)
    //   1. 从 data 取预计算的 pawn 列表/配置（不自己调 MapComponent）
    //   2. 检查缓存是否 stale → 刷新
    //   3. 搜索过滤 → 仅在 searchText 变化时重算
    //   4. BeginScrollView → 绘制行 → EndScrollView
    //   5. FloatMenu 在按钮点击时创建，不在 OnGUI 里预建
    public void DrawXxxTab(Rect rect, ParentWindowData data);
}
```

**关键**：每个 Draw 方法从 ParentWindowData 取值，不自己调用 `PrisonResetUtility.GetComponent(CurrentMap)`。

## 视觉不变性检查清单

重构时必须保持的视觉元素：

| Tab | 必须保持的视觉 |
|-----|--------------|
| Overview | 环形图大小和位置、饼图颜色、卡片布局、日程预览的折叠效果 |
| Prisoners | 卡片宽度/间距、徽章颜色、筛选 chip 样式、详情面板宽度 |
| Work | 列头纵向文字、矩阵格子大小、Shift 批量变色、候选侧栏宽度 |
| Food | 4 栏布局、价格输入框位置、搜索框、婴儿特殊食物勾选框 |
| Drug | 已选面板+目录网格、搜索框、允许/禁用 Toggle |
| Apparel | 3 列（婴儿/儿童/成人）、跨年龄开关、武器搜索框 |
| Settings | 参数名左对齐、输入框右对齐、各分区标题 |

## 数据源替换映射

| 原数据源 | 新数据源 |
|---------|---------|
| `component.GetEffectiveBalance(pawn)` | `pawn.GetPrisonerPolicy()?.GetEffectiveBalance() ?? 0f` |
| `component.prisonerPolicies` | `pawn.GetComp<CompPrisonerPolicy>()` |
| `component.mainHourAssignments` | `GameComponent_Schedule.MainSchedule` |
| `component.suppressionSnapshot` | `pawn.GetComp<CompSuppression>()?.Snapshot` |
| `component.GetCurrencyName()` | `RimPrisonSettings.CurrencyName` |
| `PrisonResetUtility.GetManagedPopulationForDisplay(map)` | `ParentWindowData.ManagedPawns`（预缓存） |

## 环形图纹理缓存的修复

原缓存是静态 Dictionary 永不清理。修复版：

```csharp
private static readonly Dictionary<string, Texture2D> PolarTextureCache = new();
private const int MaxCacheEntries = 8;

// 每次 Get 时：
//   如果缓存命中 → 返回
//   如果超过上限 → 删除最老的条目
//   生成新纹理 → 存入缓存
// 窗口关闭时 → 全部 Dispose + 清空
```

## 移植 UI 的工作量

| Tab | 原行数 | 估计新行数 | 主要工作 |
|-----|-------|-----------|---------|
| Overview | ~700 | ~400 | 环形图保留，数据源换 Comp |
| Prisoners | ~700 | ~350 | 卡片逻辑复用，换数据源 |
| Work | ~600 | ~300 | 简化，我们已有 WorkTab |
| Food | ~300 | ~200 | 从零写（原代码能参考布局） |
| Drug | ~200 | ~150 | 同上 |
| Apparel | ~200 | ~150 | 同上 |
| Settings | ~200 | ~200 | 几乎重写（硬编码最多） |
| MainWindow 骨架 | ~200 | ~100 | 只做 Tab 切换 |
| **合计** | **~3400** | **~1850** | |

视觉 100% 保留，代码量减半。

## 实现顺序

1. `RPR_UiStyle.cs` — 颜色常量 + DrawPanel（直接搬，工作量 0）
2. `MainWindow 骨架` — 7 Tab 导航（新建，100 行）
3. `PolarChartRenderer.cs` — 环形图控件（原逻辑搬过来 + 缓存修复）
4. `RPR_ScheduleEditor.cs` — 日程编辑器控件（搬 ScheduleUtility 的算法）
5. 逐个 Tab 实现，从简单的开始：Settings → Drug → Food → Apparel → Work → Prisoners → Overview
