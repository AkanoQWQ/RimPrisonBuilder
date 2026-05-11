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
