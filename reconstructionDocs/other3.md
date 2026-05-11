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
