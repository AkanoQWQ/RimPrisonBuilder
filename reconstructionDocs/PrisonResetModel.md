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
