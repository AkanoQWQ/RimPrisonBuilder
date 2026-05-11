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
