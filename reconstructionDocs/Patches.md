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
