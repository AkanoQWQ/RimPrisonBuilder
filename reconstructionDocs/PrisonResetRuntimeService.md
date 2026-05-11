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
