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

# other1：DebtHarvest + ApiExtensions + CaptureService

三个中型文件的合并审查。1783 行合计。

---

## PrisonDebtHarvestService.cs (585 行)

### 功能：器官扺债系统

**核心逻辑**：囚犯欠债超过阈值 → 自动摘器官 → 卖钱抵债。

**可摘部位**（token 字符串列表）：
- 器官：左肾、左肺、心脏
- 手指脚趾：左右各 5 根手指 + 5 根脚趾
- 手脚：左右手掌、左右脚掌
- 鼠族扩展(Ratkin)：额外有左右耳朵、尾巴

**左右判定**：遍历 BodyPartRecord 的父级链，在标签/defName 里搜索 "左/left" 或 "右/right"。

**自动流程**：`TryProcessAutomaticDebtHarvests`
- 检查欠债 >= 阈值 (默认100)
- 冷却时间满足
- 遍历 token 列表找第一个可摘的部位
- 直接添加 `Hediff_MissingPart`（不流血不麻醉）
- 减债 100 + 发奖励 + 记心情

**手动流程**：`TryHandleManualDebtSurgeryReward`
- 手术摘除匹配 token 的部位后触发
- 手动奖励 > 自动奖励（默认 200 > 150）

**鼠族兼容**：检测 pawn 的 def/kind/xenotype 中是否有 "Ratkin" 字符串 → 额外生成 RatEgg_Meat/RatEgg_Ear/RatEgg_Tail

### 评价

功能本身是 mod 的一大卖点（器官扺债），但 token 字符串匹配左右部位的方式很 hack。`PartMatchesToken` 里的巨型 switch（60+ 行）和 `HasSideToken` 的字符串搜索性能不好但只在欠债时触发一次。

移植时这个功能可以独立成 `GameComponent_DebtHarvest`。

---

## RimPrisonApiExtensions.cs (570 行)

### 功能：公开 API + 子 mod 扩展系统

这是这个 mod 最有远见的设计——**为其他 mod 提供注册回调的扩展点**。

**8 个扩展接口**：

| 接口 | 用途 | 注册方法 |
|------|------|---------|
| `IRimPrisonWorkEligibilityRule` | 判断 pawn 能否做某工作 | `RegisterWorkEligibilityRule` |
| `IRimPrisonWorkEfficiencyRule` | 返回工作效率倍率 | `RegisterWorkEfficiencyRule` |
| `IRimPrisonLaborJobProvider` | 提供自定义劳动 Job | `RegisterLaborJobProvider` |
| `IRimPrisonBabyFoodRule` | 判断食物能否喂婴儿 | `RegisterBabyFoodRule` |
| `IRimPrisonBabySpecialFoodProvider` | 婴儿特殊食物 Job | `RegisterBabySpecialFoodProvider` |
| `IRimPrisonFoodEffectRule` | 食物消费副作用 | `RegisterFoodEffectRule` |
| `IRimPrisonMoodRule` | 心情修正 | `RegisterMoodRule` |
| `IRimPrisonPreceptInterpreter` | 意识形态 Precept 解释 | `RegisterPreceptInterpreter` |

**决策模型**：`RimPrisonApiRuleDecision` — 三态（Unhandled/Allow/Deny），Deny 优先于 Allow。

**包装 API 类**（给调用者用的简洁接口）：
- `RimPrisonWorkApi` — 工作类型查询、效率计算
- `RimPrisonFoodApi` — 婴儿食物判断
- `RimPrisonCultureApi` — 文化/制度快照
- `RimPrisonFinanceApi` — 余额/债务/扣费
- `RimPrisonStateApi` — 全局状态快照

### 评价

这个 API 系统的设计是**合理的、干净的**。接口清晰、注册/注销机制标准。我们移植时可以完整保留这个扩展体系——甚至可以直接复用这些接口定义（改命名空间）。

---

## PrisonResetCaptureService.cs (628 行)

### 功能：批量逮捕/拘捕系统

**逮捕目标选择**：
- `GetCapturableTargets(map)` — 返回地图上所有可逮捕 pawn（180 tick 缓存）
- 过滤：非死、非囚犯、非殖民者、非奴隶、Humanlike 且可被拘捕

**锚点优先级**：
1. 婴儿囚犯 → 监狱婴儿床 （priority 0）
2. 成人囚犯 → 原版囚犯床 （priority 0）
3. 任何床 → 次优选择 （priority 1）
4. 无床 → 无效 （int.MaxValue）

**Job 构建** (`BuildCaptureJob`)：
- 婴儿/儿童 → `RPR_CaptureYoungToBed` → 监狱婴儿床
- 成人 → `RPR_CaptureToPrisonCell` → 监狱区域内的可用格子
- 兜底 → 原版 `Capture` Job → 囚犯床

**监狱内部格子缓存**：600 tick 刷新一次，从 `PrisonAreaAccessService` 的监狱区域提取可站立、非门、无 pawn 的格子。

**批量分配**：`TryAssignPendingCapture` 支持 excludedWorkerThingIds 防止一个殖民者被分配到多个逮捕任务。

### 评价

功能是批量逮捕 + 自动把囚犯送到监狱区域内的正确位置。缓存机制虽然又是定时刷新，但 180/600 tick 的间隔在大多数情况下够用。

移植时可以作为 `GameComponent_CaptureService` 独立实现。

---

## 三文件总结

| 文件 | 核心功能 | 移植方式 |
|------|---------|---------|
| PrisonDebtHarvestService | 器官扺债（自动+手动），鼠族兼容 | 独立 GameComponent |
| RimPrisonApiExtensions | 8 种扩展接口，5 个包装 API 类 | 直接复用接口定义 |
| PrisonResetCaptureService | 批量逮捕 + 锚点优先级 + 缓存 | 独立 GameComponent |

# other2：rest1 六文件合并审查

---

## PrisonAreaAccessService.cs (614 行)

### 功能：监狱区域管理

**核心职责**：
- **监狱区域选区** (`Area_PrisonReset`)：自定义 Area 类型，标识地图上的监狱区域
- **区域刷新** (`RefreshLegacyPrisonArea`)：根据囚犯床、监狱设施、手动标记的 cell 重新计算哪些格子属于监狱区域
- **细胞隶属判断** (`IsCellInPrisonArea`)：给定 cell 是否在监狱区域
- **封闭牢房判定** (`IsEnclosedPrisonRoom`)：房间必须是封闭的、有屋顶的、不靠地图边缘
- **监狱设施判定** (`IsPrisonFacilityBuilding`, `IsSupportedPrisonFacilityDef`)：桌子、可坐物品、娱乐设施、工作台都算监狱设施
- **门禁权限判断** (`CanPawnOpenControlledDoor`)：9 种分组（殖民者/狱卒/奴隶/囚犯/商队/访客/敌队/儿童/机械族）的门禁检查
- **殖民者监狱区工作限制** (`CanPawnWorkInPrisonArea`, `CanColonistUsePrisonAreaWorkGiver`)：殖民者在监狱区域内只能做特定工作（搬运、医疗、建筑、守卫职责）

**数据结构**：`Area_PrisonReset` 继承原版 `Area`，用 bool 矩阵标记每个 cell。

### 评价

这个功能是 **"原版 Locks mod 替代品"**——独立的门禁系统 + 监狱区域识别。逻辑相对独立，已经有 `Area_PrisonReset` 子类。移植时作为 `GameComponent_PrisonArea` 是合理的。门禁的 9 种分组和权限判断可以保留。

---

## PrisonResetDrugPurchaseService.cs (329 行)

### 功能：囚犯自动买药

**流程**：`TryQueueDrugPurchase`
1. 检查是否在娱乐时间 + 不是死亡/倒地/精神崩溃/已在进食
2. 在监狱区域内找最近的可购买成瘾品
3. 根据余额、心情、成瘾状态、赎身价判断是否应该买
4. 检查当前娱乐窗口内的用药次数限制（成人 3、儿童 2，成瘾渴望翻倍）
5. 强制开始 `Ingest` Job
6. 标记待扣费 + 追踪

**找药逻辑**：遍历地图 `ThingsInGroup(Drug)`，距离排序，有成瘾匹配的药优先。

**婴儿成瘾品**：婴儿如果能自行移动 + 能自己吃东西（反射调用 Toddlers mod 的 `CanFeedSelf`），也能买药。

**取消用药**：`CancelPendingDrugConsumptions` 遍历所有待用药 pawn，中断 Job。

### 评价

逻辑干净，职责单一。移植时可以作为 `CompDrugConsumption` 或整合到我们的成瘾品管理 tab 逻辑中。

---

## PrisonResetMealPurchaseService.cs (299 行)

### 功能：囚犯自动买饭

**流程**：`TryQueueMealPurchase`
1. 确定主餐标（婴儿→婴儿餐，余额高→精致餐，默认→普通餐）
2. 婴儿：先尝试特殊食物 Job（扩展 API），否则找婴儿餐
3. 非婴儿：按餐标降级搜索（精致→普通→福利），找最近的可购食物
4. 找到后强制开始 `Ingest` Job

**找饭逻辑**（`FindPurchasableMeal`）：用 `nutritionDiff * 10000 + distSq` 评分，优先营养匹配度高的 + 距离近的。
- 有白名单时：逐个 def 的 `ThingsOfDef` 查询
- 无白名单时：遍历 `ThingsInGroup(FoodSource)`

**福利餐心情**：吃福利餐时加 `RPR_WelfareMeal` 或 `RPR_WelfareMealBaby` 记忆。

### 评价

逻辑清晰。食物搜索的两条路径（白名单/全食物）是合理的优化。移植时作为独立服务类，对接我们的餐标配置。

---

## PrisonResetRimTalkCompat.cs (312 行)

### 功能：RimTalk 集成

向 RimTalk 注册囚犯的财务上下文变量：
- `rimprison_balance` — 余额
- `rimprison_debt` — 债务
- `rimprison_effective_balance` — 净值
- `rimprison_context` — 完整财务上下文（余额/债务/工资/赎身价的自然语言描述）

通过反射调用 RimTalk 的 API（`RimTalk.API.RimTalkPromptAPI`），因为 RimTalk 不是硬依赖，所以全部用反射。每 2500 tick 重试一次初始化。

`TryCreateCaptiveStatusCategory` 用反射构造 `ContextCategory`（尝试 3 种构造方式 + fallback 字段/属性搜索）。

### 评价

这个我们**可以直接复用**。RimTalk 集成是我们 design.md 里提到的重要联动，这段反射代码已经验证过能跑。改名后放到 `Source/Compat/RimTalkCompat.cs` 即可。

---

## PrisonResetScheduleEditorUtility.cs (458 行)

### 功能：日程编辑器数据结构与操作

**数据结构**：
- `PrisonResetScheduleSegment` — 单段日程（Assignment + DurationHours）
- `PrisonResetScheduleDurations` — 四种分配的小时统计

**核心算法**：
- `BuildSegmentsFromSchedule`：48 半小时间隔 → 连续段列表
- `BuildScheduleFromSegments`：连续段列表 → 48 半小时间隔
- `NormalizeSegments`：合并相邻同类型段，修正总时长为 24h
- `ResizeSegment`：拖拽边界 → 重新分配前后两段的时长
- `MoveSegment`：拖拽段 → 改变段顺序
- `AppendSegment` / `InsertSegment` / `RemoveSegment`：增删段（最小 0.5h）
- `Convert24HourScheduleToHalfHourSlots`：24 小时值 → 48 半小时间隔
- `BuildScheduleFromBoundaries`：3 个边界时间点 → 48 格日程

**约束**：最小段长 0.5h，总时长始终强制 = 24h，中间操作有溢出调节。

### 评价

这是**最值得直接复用的代码**之一。日程编辑器的纯数据结构算法，不依赖 UI、不依赖 MapComponent、零副作用。移植时可以直接搬，只改命名空间。

---

## ThoughtWorkers.cs (261 行)

### 功能：囚犯心情系统

**7 个 ThoughtWorker + 1 个 Thought**：

| 类 | 作用 |
|----|------|
| `ThoughtWorker_PrisonResetRegimeMood` | 制度心情：高压-1/威慑-2/平等-3（stage 0/1/2） |
| `ThoughtWorker_PrisonResetWardenPresence` | 典狱长在场：典狱长自己+3、殖民者+2、婴儿+1、囚犯+0 |
| `ThoughtWorker_PrisonResetBabyExpectations` | 被管理的婴儿有特殊期望 |
| `ThoughtWorker_PrisonResetWealthyStatus` | 余额影响心情：>=200（婴儿+2、成人+0）、<-50（婴儿+3、成人+1） |
| `ThoughtWorker_PrisonResetScheduleLack` | 无娱乐+无睡眠=-2、无娱乐=-0、无睡眠=-1 |
| `ThoughtWorker_PrisonResetNoLabor` | 无劳动安排=-0 |
| `ThoughtWorker_PrisonResetYardTime` | 放风时间在监狱区域或放风房间=+0 |
| `Thought_PrisonResetDebtOverload` | 债务每超 100=-1 心情 |

### 评价

心情系统在 design.md 里是 TODO，但他的实现已经跑通了。移植时可以直接用，但数值可能需要根据我们的改造值系统重新平衡（比如 `WealthyStatus` 应该关联改造值而非原版心情）。

---

## 六文件总结

| 文件 | 功能 | 移植优先级 |
|------|------|----------|
| PrisonAreaAccessService | 监狱区域选区 + 门禁 | **高** — 区域管理是基础设施 |
| PrisonResetScheduleEditorUtility | 日程段算法 | **高** — 纯算法，零依赖 |
| PrisonResetRimTalkCompat | RimTalk 集成 | **中** — 直接复用 |
| PrisonResetMealPurchaseService | 自动买饭 | **中** — 依赖餐标配置 |
| PrisonResetDrugPurchaseService | 自动买药 | **中** — 依赖药品配置 |
| ThoughtWorkers | 囚犯心情 | **低** — 需要跟改造值系统协调 |

# other3：rest2 全部文件合并审查（27 文件）

---

## 基础设施 (3 文件)

### Bootstrap.cs (43 行)
Harmony 启动器。反射遍历 Assembly 中所有带 `[HarmonyPatch]` 的类，逐一 Patch。捕获异常打印错误。Harmony ID: `aaa.rimprison.reset`。

### PrisonResetDefs.cs (112 行)
`DefOf` 类。定义 57 个 def 引用：
- 1 MainButtonDef, 1 MemeDef, 3 PreceptDef（狱卒/高压/平等制度）
- 27 个 ThoughtDef（三制度×多种事件+赎身/绝望/工资/福利餐/债务）
- 4 个 HediffDef（绝望/余额/债务工作/待抵扣）
- 1 LetterDef, 1 RoomRoleDef, 1 JobDef, 1 WorkTypeDef
- 附带 5 个 helper：按制度解析对应 ThoughtDef（`ResolvePrisonerDied`、`ResolveNewPrisoner` 等）

### RimPrisonApi.cs (75 行)
对外 API 的稳定接口。提供了 `IsManagedPrisoner`、`IsPrisonAreaCell`、`GetBalance` 等 14 个封装方法。标注"不要直接 patch 内部细节，用这些方法"。带有 `Supports(feature)` 特性查询。

---

## 监狱区域系统 (4 文件)

### Area_PrisonReset.cs (52 行)
继承原版 `Area`，自定义颜色（暗红色）、自定义标签"监狱区域"、`ListPriority=900`。重写了 `ExposeData`、`GetUniqueLoadID`。

### Designator_AreaPrisonResetExpand.cs + Designator_AreaPrisonResetClear.cs
两个区域扩展/清除设计器。左键在监狱区域上画画。操作后调用 `QueueLegacyPrisonAreaRefresh()` 触发区域重建。

### RoomRoleWorker_PrisonYard.cs (40 行)
房间角色判定：如果房间内某个 cell 在监狱区域内且没有人类床 → 判定为"监狱放风区"（score 120000）。

---

## 逮捕系统 (1 文件)

### Designator_PrisonResetBulkCapture.cs (123 行)
批量逮捕设计器。像采矿一样框选目标 pawn。自动注册到 Orders 类别。单个 pawn 调用 `PrisonResetCaptureService.TryQueueCapture`。集成到建筑菜单（用反射调用 `ResolveDesignators`）。

### JobDriver_PrisonResetCaptureToCell.cs (187 行)
两个自定义 JobDriver：
- `JobDriver_PrisonResetCaptureToCell`：成人逮捕→押送到指定 prison cell
- `JobDriver_PrisonResetCaptureYoungToBed`：婴儿/儿童逮捕→押送到监狱婴儿床，包含 `TryMakeTakeePrisoner()` 处理

---

## 婴儿保育 (2 文件)

### PrisonBabyApparelService.cs (125 行)
婴儿服装服务。判断能否穿某件服装（检查白名单、冲突、生物编码），强制穿戴（`ForceWear`→脱掉冲突服装→`baby.apparel.Wear`→设为强制）。

### JobDriver_PrisonResetDressPrisonBaby.cs (102 行)
给婴儿穿衣服的自定义 JobDriver。流程：走到衣服→拾取→走到婴儿→穿衣服。`defaultDuration=60` tick。

---

## 工作系统 (2 文件)

### PrisonAreaWorkFallbackService.cs (220 行)
**核心补丁后备**。当原版 `JobGiver_Work` 找不到工作时，走监狱区域内的 WorkGiver 扫描。手动调用 `PotentialWorkThingsGlobal`→`HasJobOnThing`→`JobOnThing`（thing 和 cell 版本），过滤非监狱区域。还通过**反射调用**原版 `JobGiver_Work.PawnCanUseWorkGiver` 来检查权限。这是 `PrisonResetRuntimeService.TryIssueLaborJob` 的备用路径。

### PrisonGuardDutyService.cs (28 行)
守卫工作类型判定。`IsGuardDutyWorkType`：原版 Warden 或 4 个遗留守卫工作类型。

---

## UI 组件 (4 文件)

### PrisonResetUiStyle.cs (75 行)
UI 颜色常量 + 基础绘制方法：`DrawPanel`、`DrawSubPanel`、`DrawNavigationSelection`、`DrawSoftBorder`、`DrawMutedLabel`。全静态颜色定义。

### PrisonResetWindowLayoutUtility.cs (83 行)
窗口布局计算工具。列宽/列数/滑动宽度的纯数学方法。

### PrisonActivityLogDrawer.cs (69 行)
活动日志绘制组件。`SplitLogTimestamp` 分割时间戳和消息，`Draw` 方法自动计算行高并绘制 Tiny 字体的日志列表。

### Gizmo_PrisonResetBalance.cs (72 行)
囚犯选择时的自定义 Gizmo：显示余额数量，点击跳转到监狱管理主标签页选中该 pawn。

---

## 数据缓存 (1 文件)

### PrisonRuntimeCacheState.cs (210 行)
MapComponent 的运行时缓存状态。7 个脏 HashSet（Prisoner/Baby/Work/Medical/Apparel/Capture/Ransom）+ 2 个 spawn 索引（Pawn/Thing by ThingID）。`ConsumeDirtyPawns(budget)` 从多个 set 中消费 ID 去重。

---

## 服务类 (4 文件)

### PrisonResetFinanceUtility.cs (117 行)
日费计算：食物 8 + 床位 4（威慑/高压）+ 放风 3（高压）。工资摘要格式化。

### PrisonResetFloatingTextService.cs (28 行)
金币变化浮动文字。`MoteMaker.ThrowText` 显示 ±金额。

### PrisonInteractionLockService.cs (96 行)
**互斥锁**。防止多个殖民者同时处理同一个囚犯。全局 Dictionary（target pawn → actor + job）。`TryAcquireOrRefreshLock` + 6000 tick 超时自动释放。

### PrisonAreaDesignationRegistration.cs (38 行)
在游戏启动时将 `Designator_PrisonResetBulkCapture` 注册到 Orders 设计器类别。用反射注入。

---

## 其他 (3 文件)

### ChoiceLetter_PrisonerRansom.cs (189 行)
赎身申请信。两个选项：允许（扣余额→释放→离开地图）和拒绝（第一次拒绝 3 天后可再申请，第二次拒绝施加绝望 hediff + thought）。`ShouldAutomaticallyOpenLetter = true`。

### PrisonStatusHediffs.cs (82 行)
3 个状态 Hediff（显示在健康面板）：
- `Hediff_PrisonBalanceStatus` — 显示"边缘币余额：xxx"
- `Hediff_PrisonDebtWorkStatus` — 显示"工作还债（+20%）"
- `Hediff_PrisonDebtHarvestPendingStatus` — 显示"即将抵扣：xxx部件"

### AssemblyInfo.cs
标准 AssemblyInfo，无特殊内容。

---

## rest2 总结

这 27 个文件是 mod 的"零件箱"——基础设施、小工具、简单服务类。大部分代码干净且职责单一（除了 PrisonAreaWorkFallbackService 里的反射调用）。

**移植优先级最高**：
- `PrisonResetDefs.cs` — DefOf，必须参考来创建我们的 def
- `PrisonResetUiStyle.cs` — 配色常量，直接搬
- `PrisonInteractionLockService.cs` — 互斥锁，独立无依赖
- `ChoiceLetter_PrisonerRansom.cs` — 赎身信，完整工作流
- `Bootstrap.cs` — 参照写我们的 Harmony 入口

**需要改造**：
- `PrisonAreaWorkFallbackService.cs` — 去掉反射，改用我们的事件驱动
- `PrisonRuntimeCacheState.cs` — 脏标记系统可以简化，去掉轮询消费模式

# Patches.cs

**大小**: 87KB / 2679 行 | **补丁数量**: ~70 个
**路径**: `RIMPRISON/Source/RimPrison.Reset/Patches.cs`

---

## 补丁清单（按功能分组）

### 工作/日程/技能 (8 个)

| 补丁 | 目标方法 | 作用 |
|------|---------|------|
| `Patch_Pawn_TimetableTracker_CurrentAssignment` | `Pawn_TimetableTracker.get_CurrentAssignment` | 囚犯/婴儿返回自定义日程（Sleep/Rec/Labor/Anything） |
| `Patch_JobGiver_Work_PawnCanUseWorkGiver` | `JobGiver_Work.PawnCanUseWorkGiver` | Perfix: 殖民者禁入监狱区工作；囚犯允许使用 WorkGiver |
| `Patch_JobGiver_Work_TryIssueJobPackage` | `JobGiver_Work.TryIssueJobPackage` | 囚犯的工作 Job 由自定义扫描逻辑接管 |
| `Patch_WorkGiver_GrowerSow_JobOnCell_PrisonSkillBypass` | `WorkGiver_GrowerSow.JobOnCell` | 临时伪造种植技能等级，让低技能囚犯也能播种 |
| `Patch_Pawn_WorkTypeIsDisabled_PrisonIdentityUnlock` | `Pawn.WorkTypeIsDisabled` | 囚犯身份解锁的工作类型不检查 disabled |
| `Patch_Pawn_WorkTagIsDisabled_PrisonIdentityUnlock` | `Pawn.WorkTagIsDisabled` | 同上，按 WorkTag |
| `Patch_Pawn_GetDisabledWorkTypes_PrisonIdentityUnlock` | `Pawn.GetDisabledWorkTypes` | 同上，批量返回 |
| `Patch_Pawn_CombinedDisabledWorkTags_PrisonIdentityUnlock` | `Pawn.CombinedDisabledWorkTags` | 同上，合并标签 |

### 脏标记通知 (22 个)

这是一组"事件 → 标记脏 → 触发轮询"的补丁。每个补丁只是调用 `MarkPawnDirty`/`MarkThingDirty`/`MarkAreaDirty`。

| 补丁目标 | 触发事件 |
|----------|---------|
| `Pawn.SpawnSetup` / `Pawn.DeSpawn` | Pawn 生成/消失 |
| `Thing.SpawnSetup` / `Thing.DeSpawn` | 物品/建筑生成/消失 |
| `Pawn_JobTracker.EndCurrentJob` | Job 结束 → 检查是否需要重新路由 |
| `Pawn_WorkSettings.SetPriority` | 工作优先级变更 |
| `Pawn.Notify_DisabledWorkTypesChanged` | 能力变更（受伤/恢复） |
| `LifeStageWorker_HumanlikeChild` / `Adult` | 生命阶段变更 |
| `Building_Bed.SetBedOwnerTypeByInterface` | 婴儿床归属变更 |
| `CompAssignableToPawn_Bed.TryAssignPawn` / `TryUnassignPawn` | 床分配/取消 |
| **自 Patch 的 7 个**: `AddBalance`, `SetBalance`, `SetDebt`, `ChargeBalanceOrAddDebt`, `AddDebtHarvestReward`, `ProcessDailyConsumptionIfNeeded`, `TrySendRansomApplication` | 余额/赎身变化 |
| `ChoiceLetter_PrisonerRansom.AcceptRansom` / `DenyRansom` | 赎身信接受/拒绝 |

> 最后 9 个是**补丁自己的类的方法**。正常做法是在方法体内直接调用 `MarkPawnDirty`。这是 AI 堆代码的标志性行为——不会修改现有方法，只会从外部 Patch。

### 门禁 (3 个)

| 补丁 | 作用 |
|------|------|
| `Patch_Building_Door_PawnCanOpen` | 根据门禁配置决定 pawn 能否通过门 |
| `Patch_Building_Door_GetInspectString` | 门的信息面板显示门禁权限摘要 |
| `Patch_Building_Door_GetGizmos` | 门的 Gizmo 菜单：设置门禁权限（成人/儿童/婴儿分龄） |

### 婴儿保育 (15 个)

引导殖民者/囚犯执行婴儿保育任务：

| 补丁 | 作用 |
|------|------|
| `Patch_ChildcareUtility_CanFeedBaby_CrossGroup` | 允许跨派系喂食（囚犯喂婴儿） |
| `Patch_Pawn_MindState_*` (2 个) | 自动喂食设置 |
| `Patch_Pawn_JobTracker_StartJob_BlockColonistPrisonBabyCare` | 阻止殖民者接手已被管理的婴儿保育 |
| `Patch_JobGiver_Autofeed_TryGiveJob_PrisonBabyCareGate` | 婴儿喂食路由到囚犯保育员 |
| `Patch_JobGiver_BringBabyToSafety_TryGiveJob_PrisonBabyCareGate` | 婴儿安置路由到囚犯保育员 |
| `Patch_WorkGiver_BringBabyToSafety_NonScanJob_PrisonBabyCareGate` | 同上 WorkGiver 版本 |
| `Patch_WorkGiver_Breastfeed_JobOnThing_PrisonBabyCareGate` | 哺乳路由 |
| `Patch_WorkGiver_BottleFeedBaby_JobOnThing_PrisonBabyCareGate` | 奶瓶喂食路由 |
| `Patch_WorkGiver_PlayWithBaby_JobOnThing_PrisonBabyCareGate` | 陪玩路由 |
| `Patch_ChildcareUtility_FindAutofeedBaby_*` | 自动喂食目标选择 |
| `Patch_ChildcareUtility_FindUnsafeBaby_*` | 不安全婴儿检测 |
| `Patch_ITab_Pawn_Feeding_*` (3 个) | 喂食标签页 UI 扩展 |

### 食物/禁物 (5 个)

| 补丁 | 作用 |
|------|------|
| `Patch_FoodUtility_FoodIsSuitable` | 囚犯按餐标白名单判断食物是否可食用 |
| `Patch_ForbidUtility_IsForbidden` (Thing 版) | 监狱区内物品对囚犯自动解禁 |
| `Patch_ForbidUtility_IsForbidden` (Cell 版) | 同上，格子版 |
| `Patch_WorkGiver_Warden_DeliverFood_PrisonToggle` | 典狱长送餐开关 |
| `Patch_WorkGiver_Warden_Feed_PrisonToggle` | 同上 |

### 心情/越狱/精神崩溃 (4 个)

| 补丁 | 作用 |
|------|------|
| `Patch_MentalBreaker_CanHaveMentalBreak` | 压制度影响精神崩溃阈值 |
| `Patch_PrisonBreakUtility_InitiatePrisonBreakMtbDays` | 压制度影响越狱概率 |
| `Patch_MentalBreaker_TryDoMentalBreak` | 精神崩溃时记录日志 |
| `Patch_MentalStateHandler_TryStartMentalState` | 精神崩溃状态记录日志 |

### 房间评分 (3 个)

| 补丁 | 作用 |
|------|------|
| `Patch_RoomRoleWorker_PrisonCell_GetScore_PrisonBabyBed` | 有婴儿床的房间仍可评为单人囚室 |
| `Patch_RoomRoleWorker_PrisonBarracks_GetScore_PrisonBabyBed` | 同上，营房 |
| `Patch_RoomRoleWorker_Nursery_GetScore_PrisonBabyBed` | 有婴儿床的房间仍可评为育婴室 |
| `Patch_Room_IsPrisonCell_PrisonYard` | 包含监狱放风区的囚室判定 |

### 其他 (10 个)

| 补丁 | 作用 |
|------|------|
| `Patch_Pawn_GuestTracker_*` (3 个) | 囚犯状态变化时触发同步 |
| `Patch_Building_TurretGun_IsValidTarget` | 炮塔威胁注册 |
| `Patch_Pawn_AgeTracker_BirthdayBiological` | 生日时重新检查身份/工作策略 |
| `Patch_Pawn_NeedsTracker_NeedsTrackerTickInterval` | 囚犯需求 tick 频率 |
| `Patch_Pawn_NeedsTracker_ShouldHaveNeed_PrisonJoy` | 成年囚犯应该有娱乐需求 |
| `Patch_Thing_Ingested_PrisonResetMealCharge` | 囚犯吃东西时自动扣费 |
| `Patch_Recipe_RemoveBodyPart_ApplyOnPawn` | 器官抵扣手术记录 |
| `Patch_Pawn_GetGizmos_PrisonResetBalance` | 囚犯 Gizmo 显示余额信息 |
| `Patch_RecordsUtility_Notify_BillDone` / `Patch_QuestManager_Notify_ThingsProduced` | 囚犯生产的物品自动解禁 |
| `Patch_PawnNeedsUIUtility_GetThoughtGroupsInDisplayOrder` | 心情 UI 排序 |
| `Patch_Alert_HitchedAnimalHungryNoFood` / `Patch_GoodwillSituationManager` | 空指针保护（对 null map 的防御） |
| `Patch_Building_GetGizmos_PrisonFacility` | 建筑的"设为监狱设施"Gizmo |

---

## 与我们架构的关系

我们的 `Source/Patches/` 已有：

| 我们的 | 对应他哪个功能 |
|--------|--------------|
| `Patch_PawnCanUseWorkGiver.cs` | 等同于他的 `Patch_JobGiver_Work_PawnCanUseWorkGiver` |
| `Patch_FactionInjection.cs` (IL 织入) | 我们没有独立的，我们的 IL 织入是全 WorkGiver 覆盖的 |
| `Patch_AutoForbidFix.cs` | 等同于他的 `Patch_ForbidUtility_IsForbidden` |
| `Patch_WorkTickTracker.cs` | 跟踪工作 tick |
| `Patch_WorkSettingsInit.cs` | WorkSettings 初始化 |

需要新增移植的：

1. **日程注入** (`Patch_Pawn_TimetableTracker`) — 我们的 ThinkTree 直接处理了日程→行为路由，可能不需要
2. **工作解锁** (`Patch_Pawn_WorkTypeIsDisabled`) — 比较重要，囚犯的工作类型不能被年龄/背景禁用
3. **食物白名单** (`Patch_FoodUtility_FoodIsSuitable`) — 我们目前的餐标在 PrisonerGroup，需要补这个 Patch 来做食物过滤
4. **婴儿保育** — 整个婴儿保育体系（15 个补丁）需要后续独立模块
5. **压制度/越狱** — 4 个补丁，需要 GameComponent

**这个文件最大的问题**：22 个补丁只做了一件事——调用 `MarkPawnDirty` → 触发 MapComponent 轮询。在我们的架构里，这些应该直接调用 GameComponent/Comp 的方法，不需要中间层。

# PrisonResetMapComponent.cs

**大小**: 135KB / 3910 行
**路径**: `RIMPRISON/Source/RimPrison.Reset/PrisonResetMapComponent.cs`
**类型**: `MapComponent` — 整个 Mod 的**唯一数据中心**

---

## 作用

这个类是整个 Mod 的**大脑 + 数据库 + 调度中心**。它管理：

| 子系统 | 行号范围 | 说明 |
|--------|---------|------|
| 数据字段 | 13–104 | **100+ 个私有字段**，包括 40+ 个 List/Dictionary |
| 运行时缓存 | 106–148 | `[Unsaved]` 标记的非持久化缓存 Dictionary |
| 属性访问器 | 155–431 | 大量 get/set 属性，部分有副作用 |
| 序列化 | 446–553 | **60+ 行 Scribe 调用** |
| MapComponentTick | 570–602 | 每 tick 主循环：hydrate → dirty pawns → dirty things → guard sync → work sync → area refresh → facility mark → suppression → runtime service |
| 缓存水合 | 694–807 | `EnsureRuntimeCacheStateHydrated()` + Dirty/Mark 系统 |
| 压制度计算 | 816–919 | 每 250 tick 重新计算（遍历所有囚犯 + 炮塔 + 守卫） |
| 囚犯策略 | 921–949 | `GetOrCreatePolicy()` — per-pawn 数据模型 |
| 日程系统 | 951–1088 | 3 轴（成人/儿童/婴儿）× 48 半小时间隔 = 144 个 int 存储 |
| 工作分配 | 1090–1566 | 策略应用、可见工作类型、优先级矩阵、默认策略 |
| 服装/食物/药品 | 1568–1993 | 餐标白名单、药品白名单、消费扣费 |
| 余额/债务 | 1995–2110 | 余额管理、发薪、欠债、赎身申请 |
| 商队/逮捕 | 2160–2321 | 入监登记、押送分配 |
| 每日结算 | 2377–2466 | 发薪 + 日费扣除 |
| 日志系统 | 2468–2497 | 2000 条上限的环形日志缓冲 |
| 文化/意识形态 | 2499–2564 | 从意识形态同步制度/狱卒系统 |
| 守卫/狱卒 | 2566–2888 | 守卫分配、典狱长任命、Warden 工作同步 |
| 门禁 | 2890–2948 | 门禁权限配置 |
| 监狱设施 | 2950–3054 | 建筑设施标记 + 每 2500 tick 自动标记 |
| 婴儿床 | 3056–3291 | 婴儿床 OwnerType 切换（用**反射**） |
| 成瘾品使用追踪 | 1696–1777 | 娱乐窗口计数 + 并行 List 索引重建 |
| 债务抵扣/器官 | 3420–3494 | 器官扺债、心情记忆 |
| 清理/缓存重建 | 3581–3909 | RebuildCaches、SanitizeNames、工资费率 |

---

## 致命问题

### 1. 上帝对象反模式 — 这是整个 Mod 唯一的"数据库"

这个类做了 **所有事情**。数据、逻辑、缓存在一个 3910 行的文件里。没有任何职责分离。

### 2. 并行 List 地狱（至少 12 组）

```
prisonerPolicies              List<PrisonerPolicyData>   ← 唯一正常建模的数据
activityLog                   List<string>               ← 三条并行：log + pawnId + personalActivity
balanceRecordPawnThingIds     List<string>               ← 两条并行：pawnId + record
personalActivityPawnThingIds  List<string>               ← 两条并行
activeWorkSessionPawnThingIds List<string>               ← 三条并行
customPayrollRateWorkTypeDefNames + customPayrollRateValues ← 两条并行
recreationDrugUsePawnThingIds + recreationDrugUseCounts + recreationDrugUseWindowIds ← 三条并行
pendingTradeIntakePawnThingIds + pendingTradeIntakeEscortPawnThingIds ← 两条并行
pendingCapturePawnThingIds + pendingCaptureEscortPawnThingIds ← 两条并行
```

每组都是一次 RemoveAt/Add 时索引错位的风险。已经看到了 `EnsureDrugUseStateCollections()` 里 `while (counts.Count < pawnIds.Count)` 这种防御性补齐代码——作者自己也知道会错位。

### 3. PERF: `MapComponentTick()` 每 tick 做太多事

```csharp
public override void MapComponentTick()  // 每 tick (60次/秒@1x)
{
    EnsureRuntimeCacheStateHydrated();   // 条件性重建整个 pawn+thing 缓存
    ProcessDirtyThingBudget();           // 消费32个 dirty thing ID
    ProcessDirtyPawnBudget(ticks);       // 消费64个 dirty pawn ID
    // guard sync (250 tick间隔)
    // work session sync (600 tick间隔)
    // prison facility auto-mark (2500 tick间隔)
    // suppression recalc (250 tick间隔) ← 遍历所有囚犯+炮塔
    // runtime service tick (每 tick!)
}
```

250 tick=10秒（1x速度）。压制度每 10 秒重算一次，即使没有任何变化。`DirtyPawnBudgetPerTick=8` 但这里消费 64 个。

### 4. PERF: `RecalculateSuppression()` 每 250 tick 全量扫描

```csharp
// 遍历所有被管理的囚犯 (line 832)
// 遍历所有炮塔建筑 (line 867-876)
// 调用 IsCellInPrisonArea 检查每个炮塔
// 8 个 SuppressionCalculator 方法调用
// 分配新的 PrisonSuppressionSnapshot (line 897)
```

没有任何缓存、没有任何增量计算。即使只有一个囚犯的心情变了 0.1%，也是全量重算。

### 5. 反射访问原版私有字段

```csharp
typeof(Building_Bed).GetField("forOwnerType", 
    BindingFlags.Instance | BindingFlags.NonPublic);
// ...
BedForOwnerTypeField?.SetValue(bed, ownerType);  // line 3150
```

每次设置婴儿床 OwnerType 都用反射。跨版本必炸。应该用 Harmony `___forOwnerType` 注入。

### 6. PERF: `EnsureDefaults()` 创建 40+ 个新 List

`EnsureDefaults()` 被构造函数和 `FinalizeInit()` 各调用一次。每次检查 ~40 个字段是否为 null 并 `new List<>()`。在 FinalizeInit 里还会立即被 `RebuildCaches()` 重建一批 Dictionary。

### 7. 数据一致性问题：List ↔ Dictionary 双重维护

很多数据同时以 `List<T>`（持久化）+ `Dictionary<string, T>`（`[Unsaved]` 缓存）存储。缓存在 `PostLoadInit` 时通过 `RebuildCaches()` 重建，但运行时修改必须同时更新两边。`GetOrCreatePolicy`、`GetOrCreateDefaultWorkPolicy` 等方法里到处可见 `if (dict.Count == 0 && list.Count > 0) RebuildCaches()` 的防御代码。

### 8. Scribe 序列化过于脆弱

`ExposeData()` 里有 60+ 个 `Scribe_Values.Look` + `Scribe_Collections.Look` 调用。`SuppressionSnapshot` 的 15 个字段是逐个 Scribe 的（line 488-505），而不是整体 Deep Look。改一个字段名就要同时改序列化键名，容易导致数据静默丢失。

### 9. 成瘾品使用计数器有内存泄露风险

```csharp
recreationDrugUsePawnThingIds   // 三并行 List
recreationDrugUseCounts
recreationDrugUseWindowIds
recreationDrugUseIndexByPawnId  // [Unsaved] Dictionary 缓存
```

死掉的 pawn、被释放的 pawn 不会从这里清理。只增不减。

### 10. 日志缓冲硬截断

```csharp
if (activityLog.Count > 2000)
    activityLog.RemoveRange(0, overflow);    // line 2489
// balanceRecordEntries 同上 (line 3395)
// personalActivityEntries 同上 (line 3416)
```

每次追加都要检查 + 批量 RemoveRange。用 `Queue<T>` 或环形缓冲会好得多。

---

## 值得保留的功能设计

以下功能设计是好的，应该移植到我们的架构：

1. **Per-pawn 策略模型** (`PrisonerPolicyData`) — 每个囚犯独立的工作/余额/赎身/债务配置
2. **三级日程轴** (成人/儿童/婴儿) — 分生命阶段的日程安排
3. **四档餐标 + 价格** — 福利/婴儿/标准/精致，带价格强制排序
4. **门禁权限系统** — 虽然是复杂了点，但概念好
5. **赎身申请** — 余额达标后自动发信，最多拒绝 2 次
6. **活动日志** — per-pawn 和全局双轨记录
7. **工资/债务抵扣** — 先抵债再发薪的逻辑是好的
8. **意识形态集成** — 从 Meme/Precept 读取监狱制度

---

## 重构方案

**不要在这个文件上修修补补。**

1. **拆分数据层**：每个子系统独立的 ThingComp/GameComponent
   - `CompPrisonerPolicy` → per-pawn 策略（替代 `PrisonerPolicyData` + `prisonerPolicies` 并行 List）
   - `CompPrisonerBalance` → per-pawn 余额
   - `GameComponent_PrisonSchedule` → 全局日程
   - `GameComponent_PrisonDoorAccess` → 门禁
   - `GameComponent_PrisonMealConfig` → 餐标配置
   - `GameComponent_PrisonGuards` → 守卫/典狱长

2. **消除所有并行 List**：用带 ThingID 索引的 Dictionary + Scribe_Deep 替代

3. **事件驱动替代轮询**：
   - 压制度 → 只在守卫/囚犯/炮塔变化时重算
   - 设施自动标记 → 只在建筑完成/拆除时触发
   - 工作会话同步 → 用 Harmony 钩子，不定时轮询

4. **反射 → Harmony 注入**：`Building_Bed.forOwnerType` 用 `___forOwnerType` 三下划线方案

5. **序列化**：每个 Comp 自己 `PostExposeData()`，不做集中式 60 行 Scribe

6. **日志**：独立 `GameComponent_ActivityLog`，用 `Queue<T>` + 自动清理

# PrisonResetModel.cs

**大小**: 55KB / 1688 行 | **类型**: 数据模型 + 枚举 + 静态工具类

---

## 内容

### 枚举定义 (12 个)

| 枚举 | 值 | 用途 |
|------|----|------|
| `PrisonResetTimeAssignment` | Sleep / Recreation / Labor / Anything | 日程安排类型 |
| `PrisonCultureRegime` | Harsh / Deterrence / Equality | 监狱制度（高压/威慑/平等） |
| `PrisonCultureSource` | Ideology | 文化来源 |
| `PrisonManagedIdentityKind` | None / Prisoner / Colonist / Slave | 被管理 pawn 的身份类型 |
| `PrisonDoorAccessGroup` | Colonist / Guard / Slave / Prisoner / Caravan / Visitor / Enemy / Child / Mechanoid | 门禁权限分组（9 种） |
| `PrisonMealTier` | Welfare / Baby / Standard / Fine | 餐标等级（福利/婴儿/普通/精致） |
| `PrisonPayrollDeliveryMode` | Automatic / WardenDelivery | 工资发放模式 |
| `ColonistPrisonAreaWorkMode` | ForbiddenByDefault / AllowedByDefault | 殖民者监狱区工作模式 |
| `PrisonApparelAgeBand` | Baby / Child / Adult | 服装年龄带 |
| `PrisonResetScheduleAxisKind` | Main / Child / Baby | 日程轴 |
| `PrisonResetCaptureJobKind` | Capture / Arrest / CaptureToCell | 逮捕工作类型 |

### 数据类 (6 个，全部 IExposable)

| 类 | 主要字段 | 用途 |
|----|---------|------|
| `PrisonerPolicyData` | 20+ 字段：laborEnabled, balance, debtBalance, 工作优先级, 赎身状态... | **per-prisoner 核心数据** |
| `PrisonDefaultWorkPolicyData` | workTypeDefName, allowBaby/Child/Adult | 各工作类型的默认年龄许可 |
| `PrisonGlobalApparelPolicyData` | 3 个年龄带的白名单 + allowCrossAgeApparel | 全局服装/武器策略 |
| `PrisonDoorAccessConfigData` | 9 个 bool 权限字段 | 单扇门的门禁配置 |
| `PrisonSuppressionSnapshot` | 16 个字段（抑制值、各因素、阈值） | 压制度计算结果快照 |
| `ActiveWorkSession` | pawnThingId, workTypeDefName, startTick | 活动工作会话 |

### 静态工具类 `PrisonResetModelUtility` — 核心公式

| 功能组 | 方法 | 逻辑 |
|--------|------|------|
| **默认日程** | `BuildDefaultSchedule()` | 0-5h 睡眠, 6-7h 娱乐, 8-17h 劳动, 18-19h 娱乐, 20-21h 自由, 22-23h 睡眠 |
| **门禁** | `IsDoorAccessAllowed`, `ResolveDoorAccessGroup`, `BuildDoorAccessSummary` | 9 种分组的权限判断 |
| **餐标** | `ResolvePrimaryMealTier`, `BuildMealTierSearchOrder`, `CanAffordMealTier` | 婴儿→婴儿餐，余额>20×普通餐价→精致餐，否则普通餐，降级兜底 |
| **成瘾品** | `ShouldTryDrugPurchaseByMoodAndBalance`, `GetDrugUseLimit` | 基于余额/心情/赎身价/成瘾/绝望的判断 |
| **工资** | `GetEightHourPayrollRateForWorkTypeDefName`, `CalculatePayrollAmount`, `AddPendingPayroll` | 默认 40/8h，按 tick 比例计算，按工作类型累计 |
| **余额/债务** | `ChargePolicyBalanceOrDebt`, `ApplyNetBalance`, `GetDebtOverloadMoodPenalty` | 先扣余额再累积债务，债务>200 每 100 加 1 心情惩罚 |
| **压制度** | `SuppressionCalculator` (内部类) | 9 参数公式：50 + guardFactor + turretFactor + prisonerFactor + moodFactor + healthFactor + regimeModifier + difficultyModifier |
| **工作类型** | `GetAllowedPrisonLaborWorkTypes`, `IsExcludedPrisonLaborWorkType`, `IsAnytimeCareWorkType` | 排除清洁和守卫类，医疗/育儿全天候可执行 |
| **工作会话** | `OpenActiveWorkSession`, `CloseActiveWorkSession`, `CleanupStaleWorkSessions` | 记录工作时长→计算工资→写日志 |
| **缓存** | `GetConfigurableMealThingDefs`, `GetConfigurableDrugThingDefs`, `GetConfigurableApparelThingDefs` | 静态懒加载，一次性 DefDatabase 筛选 |

---

## 压制度计算公式

```
suppression = 50
  + guardFactor      (guardCount*2 + colonistCount) / effectivePrisoners → ratio→factor [-12, 20]
  + turretFactor      min(turrets*2, 20)
  + prisonerFactor    -min(effectivePrisoners*1.5, 25)
  + moodFactor        (mood²*12 - 3) * 1.25
  + healthFactor      (0.5 - health) * 8
  + regimeModifier    Harsh+10, Deterrence+3, Equality-5
  + difficultyModifier (1 - difficultyValue) * 8

effectivePrisoners = adults + children*0.25 + babies*0.1  (上限20)
difficultyValues: Peaceful=0.1, LosingIsFun=2.0
```

---

## 评价

这个文件相对干净。它是 12 个枚举 + 6 个数据类 + 1 个公式工具类的集合。主要在同一个文件里没有拆开。

**需要移植的关键模型**：

1. `PrisonerPolicyData` → 我们的 `CompPrisonerPolicy`（per-pawn ThingComp）
2. `PrisonSuppressionSnapshot` → 移植到我们的压制 GameComponent
3. `SuppressionCalculator` — 公式可以直接复用，改成事件驱动触发
4. 工资计算公式 — 独立工具类，和我们的 `CompWorkTracker` 对接
5. 门禁数据模型 — 独立 GameComponent

**不需要的**：日程、餐标、服装策略模型已经被我们的 `PrisonerGroup` 体系覆盖。

# PrisonResetRuntimeService.cs

**大小**: 115KB / 3400 行 | **类型**: `static class` — 全局运行时调度

---

## 提供的功能

### 1. 囚犯行为路由（核心功能）

根据日程安排将囚犯路由到正确行为：

| 日程安排 | 路由行为 |
|---------|---------|
| Sleep | 去睡觉 (`JobGiver_GetRest`) |
| Recreation | 娱乐 (`JobGiver_GetJoy`) |
| Labor | 工作 (按优先级扫描 WorkGiver) |
| Anything | 自由（娱乐/进食/睡觉任选） |

- 如果当前行为匹配日程 → 保持不动
- 如果不匹配 → 中断当前 Job，路由到目标行为
- "AnytimeCare" 工作类型（医疗/消防/病人休息）无论什么日程都可以执行
- 睡眠满足（>=1.0）后允许离开床去娱乐（>=0.9 释放）

### 2. 紧急需求恢复

- **食物 < 5%** → 强制进入"食物恢复"模式，自动触发餐点购买（`PrisonResetMealPurchaseService`）
- **休息 < 5%** → 强制进入"休息恢复"模式，自动分配睡觉 Job
- 恢复到 35% 后退出恢复模式
- 恢复模式下锁定当前行为，不允许日程切换

### 3. 服装/武器强制

- 检查囚犯身上每件服装 → 不允许的脱掉（`TryDrop`，forbid=false）
- 检查主武器 → 不允许的卸下
- 在监狱区域内寻找允许的服装 → 自动分配 `Wear` Job
- 婴儿：寻找监狱区域的婴儿服装 → 分配守卫/保育员帮忙穿
- 每 250 tick 缓存一次监狱区域内的所有服装列表

### 4. 婴儿保育系统

- 喂食：检查是否需要哺乳/喂食 → 找保育员 → 分配喂食 Job
- 安置：婴儿不能动/倒地 → 找保育员 → 分配 `BringBabyToSafety` Job
- 穿衣：找最近的允许婴儿服装 → 分配保育员穿衣服
- 婴儿床管理：自动分配/收回非监狱婴儿床，优先使用监狱婴儿床
- 保育员来自：殖民者（优先）或成年囚犯

### 5. 囚犯医疗

- 需要医疗的囚犯 → 分配医生 Tend Job
- 需要卧床的囚犯 → 分配守卫/殖民者 `TakeToBed` Job
- 殖民者医疗优先 → 如果有殖民者需要医疗，囚犯等待
- 医生选择：最近的有效殖民者医生

### 6. 商队囚犯入监

- 商队带来的囚犯 → 自动分配押送员 → 带入监狱区域
- 如果囚犯自己在监狱区域外 → 分配 Goto Job 走到监狱区域
- 到达后自动完成入监登记

### 7. 待逮捕目标处理

- 待逮捕 pawn → 分配殖民者执行逮捕
- 如果被捕后是囚犯 → 转入商队入监流程
- 如果已在监狱区域内 → 自动完成逮捕登记

### 8. 工作分配扫描

- 按玩家的工作优先级 + 排序遍历所有 WorkGiver
- 对每个 WorkGiver 调用 `PotentialWorkThingsGlobal` / `HasJobOnThing` / `JobOnThing`
- 工作必须在监狱区域内（`PrisonAreaAccessService.CanPawnWorkInPrisonArea`）
- 支持扩展 API（`RimPrisonExtensionApi`）注入额外工作 Job

### 9. 状态追踪

- 全局人口快照（受管理囚犯列表、pawn 索引）
- 脏标记系统：DirtyManaged、DirtyGuardianship、DirtyMedical、DirtyMeal、DirtyDrug、DirtyRansom
- 睡眠满足 hysteresis（>=1.0 满足, >=0.9 保持, <0.9 释放）
- 定期清理死/移除的 pawn 引用（60000 tick）

---

## 移植要点

这些功能中，对我们最有价值的是：

1. **行为路由** — 我们的 ThinkTree 里已经有这套逻辑（`JobGiver_Work` etc），不需要移植
2. **紧急需求恢复** — 低饱食度触发强制进食是好的设计，可以在 `CompReformTracker` 旁边加一个 `CompCriticalNeed` 实现
3. **服装强制** — 我们的 `JobGiver_OptimizeApparel` 已经在 ThinkTree 里了，服装白名单/黑名单需要从 MapComponent 移植到 `PrisonerGroup` 或全局设置
4. **婴儿保育** — 完整且复杂的功能，应该在重构日程/工作分配后就绪后再移植
5. **商队入监/逮捕** — 独立功能，可以后期作为 GameComponent 加入
6. **工作扫描** — 原版 `JobGiver_Work` + 我们的 IL Faction 注入已经处理了，不需要这层额外的手动扫描

不需要从这个文件直接搬代码。了解它做了什么，然后用我们的事件驱动架构重新实现等价功能。
