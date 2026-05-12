# RimPrison API 兼容分析

> 现有拓展包使用旧 API（`RimPrisonReset` 命名空间），我们必须提供向后兼容。

---

## 一、命名空间对比

| | 旧 API | 我们的 API |
|------|--------|------------|
| 命名空间 | `RimPrisonReset` | `RimPrison.API` |
| 接口前缀 | `IRimPrison*` | `IExtension_*` |
| 命名风格 | 完整描述 | 短前缀 |

拓展包引用的是旧命名空间 + 旧类型名，我们的新名称对外部不可见 → **必须添加兼容层**。

---

## 二、缺失的类型（拓展包依赖）

### 2.1 核心值类型（必须补）

```
RimPrisonApiRuleDecision (enum)     — Unhandled, Allow, Deny
RimPrisonApiRuleResult (struct)     — Decision, Value, Reason, Handled
```

这是旧 API 的核心设计：三态决策。扩展可以返回：
- `Unhandled` — 我不关心此情况，交给其他扩展判断
- `Allow` — 明确允许
- `Deny` — 明确拒绝（附带原因字符串）

我们的 `bool` 无法表达"不参与决策"，多个扩展时一个 false 就否决所有。

### 2.2 快照类型（必须补）

```
RimPrisonCultureSnapshot (struct)   — CultureApi.GetCultureSnapshot 的返回值
RimPrisonStateSnapshot (struct)     — StateApi.GetStateSnapshot 的返回值
```

拓展包可能缓存这些快照；不补的话 CultureApi/StateApi 的公开方法签名会断。

### 2.3 枚举依赖（必须补）

```
PrisonCultureRegime          — 被 CultureSnapshot 引用
PrisonResetTimeAssignment    — 被 StateSnapshot / StateApi 引用
PrisonSuppressionSnapshot    — 被 StateSnapshot 引用
PrisonResetScheduleAxisKind  — 被 StateApi.GetScheduleAxisForPawn 引用
PrisonMealTier               — 被 FoodEffectRule.NotifyFoodConsumed 引用
PrisonApparelAgeBand         — 被 WorkApi.SetDefaultWorkAgeAllowed 引用
```

### 2.4 引用类型

```
RimPrisonBabySpecialFoodOption (class) — 有 Id/Label/Description 属性
PrisonResetMapComponent               — 被 BabySpecialFood.TryGiveJob 引用
```

---

## 三、缺失的接口（拓展包实现）

旧接口 + 相应注册方法：

| 接口 | 关键签名 | 我们对应接口 |
|------|---------|------------|
| `IRimPrisonWorkEligibilityRule` | `CanWork(pawn, wt) → RuleResult` | `IExtension_WorkEligibility` (返回 bool) |
| `IRimPrisonWorkEfficiencyRule` | `GetEfficiency(pawn, wt) → RuleResult` | `IExtension_WorkEfficiency` (返回 float) |
| `IRimPrisonLaborJobProvider` | `TryGiveLaborJob(pawn, out Job) → bool` | `IExtension_LaborJob` (返回 Job?) |
| `IRimPrisonBabyFoodRule` | `CanUseAsBabyFood(baby, food) → RuleResult` | `IExtension_BabyFood` (返回 bool) |
| `IRimPrisonBabySpecialFoodProvider` | `Option` + `TryGiveJob(baby, component, out Job)` | `IExtension_BabySpecialFood` (只有属性) |
| `IRimPrisonFoodEffectRule` | `NotifyFoodConsumed(pawn, food, tier)` | `IExtension_FoodEffect` (查询乘数) |
| `IRimPrisonMoodRule` | `GetMoodOffset(pawn) → RuleResult` | `IExtension_Mood` (void) |
| `IRimPrisonPreceptInterpreter` | `Interpret(map, precept) → RuleResult` | `IExtension_Precept` (void ref) |
| `IRimPrisonUiExtension` | `Label` 属性 | **缺失** |

---

## 四、注册机制差异

| 维度 | 旧 API | 我们的 API |
|------|--------|------------|
| 存储 | `Dictionary<string, T>` | `List<T>` |
| 注册 | `RegisterXxx(string id, T rule)` → `bool` | `RegisterXxx(T ext)` → `void` |
| 注销 | `UnregisterXxx(string id)` → `bool` | `UnregisterXxx(T ext)` → `void` |
| 查询 | `IReadOnlyDictionary` 公开 | `internal` List |
| 重复检测 | `ContainsKey` 拒绝 | 无 |

拓展包使用 `RegisterWorkEligibilityRule("my_mod_id", rule)` 注册——我们需要接受字符串 ID。

---

## 五、LaborJob 注解系统（完全缺失）

旧 API 有完整的 Job → WorkType 关联机制：

```csharp
ConditionalWeakTable<Job, AnnotatedLaborWorkTypeHolder>
AnnotateLaborJobWorkType(Job job, string workTypeDefName)
ConsumeAnnotatedLaborJobWorkType(Job job) → string
PeekAnnotatedLaborJobWorkType(Job job) → string
```

用途：当 LaborJobProvider 创建了 Job 后，用注解标明这个 Job 属于哪个 WorkType。后续工资计算/统计需要知道 job 对应的工作类型来按正确费率发薪。

这是我们内部需要的功能（为 CompPrisonerPolicy 做准备），同时对外暴露。

---

## 六、子 API 方法缺口

### RimPrisonWorkApi（旧版有，我们缺的）

```
IsWorkTypeAllowed(pawn, workType) → bool
SetDefaultWorkAgeAllowed(map, workType, ageBand, allowed) → bool
GetConfiguredWorkTypesIgnoringAgeBand(pawn) → List<WorkTypeDef>
AnnotateLaborJobWorkType / ConsumeAnnotated / PeekAnnotated
```

### RimPrisonFoodApi（旧版有，我们缺的）

```
IsBabyFoodAllowed(baby, food) → bool        — 直接查询，非遍历扩展
IsFoodAllowed(pawn, food) → bool             — 同上
TryResolveBabySpecialFoodJob(baby, component, out Job) → bool
NotifyFoodConsumed(pawn, food, tier) → void  — 事件通知，非查询
```

### RimPrisonCultureApi（旧版有，我们缺的）

```
GetCultureSnapshot(map) → RimPrisonCultureSnapshot
PlayerIdeologyHasPrecept(precept) → bool
```

### RimPrisonFinanceApi（旧版有，我们缺的）

```
GetDebtBalance(pawn) → float
GetEffectiveBalance(pawn) → float
ChargeBalanceOrAddDebt(pawn, amount) → float
AddBalance(pawn, amount) → void
```

### RimPrisonStateApi（旧版有，我们缺的）

```
GetStateSnapshot(map) → RimPrisonStateSnapshot
GetCurrentAssignmentForPawn(pawn) → TimeAssignment
GetScheduleAxisForPawn(pawn) → ScheduleAxisKind
GetScheduleForPawn(pawn) → List<int>
IsPrisonAreaCell(map, cell) → bool
IsPrisonBabyBed(bed) → bool
IsWardenSystemEnabled(map) → bool
IsBabyForageFilthFoodPoisoningEnabled(map) → bool
LogEvent(map, entry) → void
```

---

## 七、兼容方案

### 方案：保留旧 API 类型 + 适配到新实现

1. **复制旧 API 类型**到 `Source/API/Compat/`（`RimPrisonReset` 命名空间）
   - `RimPrisonApiRuleDecision` / `RimPrisonApiRuleResult`
   - 所有 `IRimPrison*` 接口
   - 所有 snapshot struct
   - `RimPrisonBabySpecialFoodOption`
   - `RimPrisonExtensionApi`（注册中心 + 决策求值）
   - 5 个子 API 类

2. **内部桥接**：我们的内部逻辑（CompPrisonerPolicy、工资计算等）通过旧 API 注册中心遍历扩展、求值决策

3. **我们的新接口保留**但标记 `[Obsolete]` 或内部使用，公开 API 全部走旧类型

4. **逐步替换**：当所有拓展包迁移后，删除新接口

---

## 八、执行优先级

### P0 — 阻塞拓展包兼容（必须先做）

- [ ] 添加 `RimPrisonApiRuleDecision` + `RimPrisonApiRuleResult` 类型
- [ ] 添加所有 9 个 `IRimPrison*` 接口（旧命名空间 + 旧签名）
- [ ] 添加 `RimPrisonBabySpecialFoodOption` 类
- [ ] 将注册中心从 `List<T>` 改为 `Dictionary<string, T>`
- [ ] 补全 Register/Unregister 方法（含 ID 参数）
- [ ] 添加三态决策求值逻辑 `ResolveBooleanRules`

### P1 — 阻塞拓展包调用

- [ ] 补全 `RimPrisonWorkApi` 缺失方法（IsWorkTypeAllowed 等）
- [ ] 补全 `RimPrisonFoodApi` 缺失方法（IsBabyFoodAllowed 等）
- [ ] 补全 `RimPrisonCultureApi` 缺失方法（GetCultureSnapshot）
- [ ] 补全 `RimPrisonFinanceApi` 缺失方法（ChargeBalanceOrAddDebt 等——依赖 CompPrisonerPolicy）
- [ ] 补全 `RimPrisonStateApi` 缺失方法（GetStateSnapshot 等）
- [ ] 添加 LaborJob 注解系统

### P2 — 内部功能依赖

- [ ] `IRimPrisonUiExtension` 接口 + 注册
- [ ] `RimPrisonCultureSnapshot` / `RimPrisonStateSnapshot` struct
- [ ] 所需枚举的定义或引用

---

## 九、实现提示

- 旧 API 文件放在 `Source/API/Compat/`，保持 `RimPrisonReset` 命名空间
- 不要删除我们当前的 `RimPrisonApi.cs`（内部代码可能引用），先把兼容层叠加上去
- `RimPrisonFinanceApi` 的方法依赖于 CompPrisonerPolicy，先写占位返回（如 0f），等经济系统实现后再补逻辑
- `PrisonMealTier`、`PrisonApparelAgeBand` 等枚举可能定义在旧 mod 的其他文件中，需要找到或重新定义
