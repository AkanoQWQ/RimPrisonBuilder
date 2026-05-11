# 重构 TODO List — RimPrisonBuilder ↔ RimPrison.Reset 功能对比

已做完 ✅ | 需新建 🔴 | 需补全 🟡

---

## 1. 基础设施

| 功能 | 状态 | 说明 |
|------|------|------|
| Harmony 入口 + PatchAll | ✅ | `RimPrisonBuilderMod.cs`，已用 |
| DefOf | 🟡 | `RP_DefOf.cs` 只有 2 个 DefOf 类，需补全 50+ def 引用 |
| 翻译键 | 🟡 | 基础已有，需要大量新增（制度/压制度/门禁/赎身/工资...） |
| 设置系统 | ✅ | `RimPrisonBuilderSettings` 已有 |
| 语言支持 | 🟡 | 中/英都有，需补大量翻译 |
| 扩展 API | 🔴 | 完全没有。需要 8 个扩展接口 + RimPrisonApi |
| RimTalk 兼容 | 🔴 | 需要 `Compat/RimTalkCompat.cs` |

---

## 2. 基层 Patch（事件驱动钩子）

| 功能 | 状态 | 说明 |
|------|------|------|
| IL Faction 注入 | ✅ | `Patch_FactionInjection.cs` — 全 WorkGiver 覆盖 |
| PawnCanUseWorkGiver | ✅ | `Patch_PawnCanUseWorkGiver.cs` |
| WorkTick 追踪 | ✅ | `Patch_WorkTickTracker.cs` |
| AutoForbidFix | ✅ | `Patch_AutoForbidFix.cs` |
| WorkSettingsInit | ✅ | `Patch_WorkSettingsInit.cs` |
| TimetableFix | ✅ | `Patch_TimetableFix.cs` |
| BlueprintFaction | ✅ | `Patch_BlueprintFaction.cs` |
| RespectAreaRestrictions | ✅ | 已有 |
| ForbiddenWorkTypes | ✅ | 已有 |
| AllowedAreaUI | ✅ | 已有 |
| 食物限制 | 🔴 | 需要 Patch `FoodUtility.FoodIsSuitable`——我们餐标在 PrisonerGroup，但没有 Hook 到真实进食判定 |
| 服装强制 | 🟡 | 我们有 `JobGiver_OptimizeApparel` 在 ThinkTree，但没有**服装白名单强制脱/穿**的 Patch |
| 物品自动解禁 | 🔴 | 需要 Patch `ForbidUtility.IsForbidden`——囚犯工作生产的物品不能自禁 |
| 门禁 | 🔴 | 需要 Patch `Building_Door.PawnCanOpen` |
| 压制度→越狱/崩溃 | 🔴 | 需要 Patch `PrisonBreakUtility.InitiatePrisonBreakMtbDays` + `MentalBreaker.CanHaveMentalBreak` |
| 进食扣费 | 🔴 | 需要 Patch `Thing.Ingested`——囚犯吃东西时自动扣余额 |
| 婴儿保育路由 | 🔴 | 需要重新路由婴儿喂食/安置/穿衣 Job 到囚犯保育员 |
| 日程覆写 | 🔴 | 需要 Patch `Pawn_TimetableTracker.get_CurrentAssignment`——或者完全依赖 ThinkTree |
| 工作类型解锁 | 🟡 | 囚犯年龄/背景不能禁用工作类型——需要 Patch `Pawn.WorkTypeIsDisabled` |
| PrisonerDied 心情 | 🔴 | 需要 Patch `Pawn.Kill` 或在需求系统注入制度相关心情 |

---

## 3. 数据模型 (ThingComp / GameComponent)

| 功能 | 状态 | 说明 |
|------|------|------|
| 囚犯核心数据 | 🔴 | `CompPrisonerPolicy` — balance, debt, laborEnabled, workPriorities, payroll 等 |
| 改造值追踪 | 🟡 | `CompReformTracker` 刚写完，基础框架 OK，但需要接入 UI 和更多条件（心情等 TODO） |
| 工作 tick 追踪 | ✅ | `CompWorkTracker` |
| 日程数据 | 🔴 | 3 轴（成人/儿童/婴儿）× 48 半小时间隔，目前没有 |
| 门禁配置 | 🔴 | per-door 权限数据，目前没有 |
| 压制度快照 | 🔴 | `GameComponent_Suppression` — 计算公式已有，需要存储 + 事件驱动触发 |
| 活动日志 | 🔴 | `GameComponent_ActivityLog` — 环形缓冲 + per-pawn 检索 |
| 每日结算 | 🔴 | `GameComponent_DailyCycle` — 发薪 + 日费扣除 |
| 守卫/典狱长 | 🔴 | `GameComponent_Guards` — 分配 + 同步 |
| 监狱区域 | 🔴 | `GameComponent_PrisonArea` — Area 子类 + 设施标记 |
| 服装策略 | 🟡 | 已有 `PrisonerGroup.apparelPolicy`，但全局白名单和婴儿服装策略需要独立存储 |

---

## 4. 日程系统

| 功能 | 状态 | 说明 |
|------|------|------|
| 日程段数据结构 | 🔴 | `ScheduleSegment` + `ScheduleDurations` — 需要从零写 |
| 段操作算法 | 🔴 | BuildSegments / NormalizeSegments / ResizeSegment / MoveSegment |
| 3 轴日程存储 | 🔴 | Main / Child / Baby 三种 schedule |
| 日程编辑器 UI | 🔴 | Timeline blocks + 拖拽边界 + 段右键删除 + 当前时间指示线 |
| 日程→行为路由 | 🟡 | ThinkTree 已处理主路由（Sleep/Rec/Labor/Anything），但 Child/Baby 分离轴需要额外逻辑 |

---

## 5. 囚犯个人行为

| 功能 | 状态 | 说明 |
|------|------|------|
| 餐标系统 + 买饭 | 🔴 | 福利/婴儿/普通/精致 4 档 + 价格 + 食物搜索 + 自动消费 Job |
| 成瘾品购买 | 🔴 | 药品白名单 + 娱乐窗口内用药 + 用量限制 |
| 服装强制 | 🔴 | 脱不允服装 → 在监狱区域找允许服装 → 自动 Wear Job |
| 婴儿喂食 | 🔴 | 保育员喂食 + 乳房/奶瓶/特殊食物 + 安置 + 穿衣 |
| 婴儿床管理 | 🔴 | 自动分配/收回监狱婴儿床 |
| 医疗关注 | 🔴 | 自动 Tend + 转运到床 |
| 紧急需求恢复 | 🔴 | 食物<5%强制进食 / 休息<5%强制睡觉 |
| 互动锁 | 🔴 | 防止多个殖民者同时处理同一囚犯 |

---

## 6. 经济系统

| 功能 | 状态 | 说明 |
|------|------|------|
| 余额/债务 | 🔴 | `CompPrisonerPolicy` 存储，`ChargeBalanceOrAddDebt` |
| 工资计算 | 🔴 | 每工作类型工资率 × tick 比例 |
| 每日发放 | 🔴 | 自动/狱卒发放，先抵债再入账 |
| 每日费用 | 🔴 | 食物 8 + 床位 4/威慑 + 放风 3/高压 |
| 赎身系统 | 🔴 | 余额达标→自动发信→接受扣钱释放 / 拒绝两次施加绝望 |
| 器官扺债 | 🔴 | 自动/手动，token 部位匹配，抵债 100 + 发奖励 |
| 粮票系统 | ✅ | CouponShop 体系（已有且独立） |
| 粮票发放 | ✅ | `Dialog_GrantCoupons` + `GameComponent_DailyAllowance` |

---

## 7. 监狱制度系统

| 功能 | 状态 | 说明 |
|------|------|------|
| 压制度公式 | 🔴 | 9 参数公式已有（`SuppressionCalculator`），需独立实现 |
| 守卫/殖民者因子 | 🔴 | guardFactor: (guard×2+colonist)/effectivePrisoners |
| 炮塔因子 | 🔴 | turretFactor: min(turrets×2, 20) |
| 制度修正 | 🔴 | Harsh+10 / Deterrence+3 / Equality-5 |
| 难度修正 | 🔴 | 8 档难度 × 8 因子 |
| 精神崩溃阈值 | 🔴 | 压制度 < 阈值(50/55/40) → 允许 |
| 越狱阈值 | 🔴 | 压制度 < 阈值(30/35/20) → 允许 |
| 炮塔威胁判断 | 🔴 | 精神崩溃/越狱/低压制度 → 炮塔对囚犯视为威胁 |

---

## 8. 意识形态

| 功能 | 状态 | 说明 |
|------|------|------|
| Meme 支持 | 🔴 | `RPR_PrisonMeme`，需要 XML + DefOf |
| 制度 Precept | 🔴 | 3 个 Precept（高压/威慑/平等） |
| 典狱长 Precept | 🔴 | `RPR_WardenSystem_Enabled` |
| 文化同步 | 🔴 | 从 Ideology 读取→覆盖配置 |
| 囚犯心情 ThoughtWorker | 🔴 | 7 个 ThoughtWorker + 1 个 ThoughtSituational |

---

## 9. 逮捕 / 入监

| 功能 | 状态 | 说明 |
|------|------|------|
| 批量逮捕设计器 | 🔴 | 框选逮捕 |
| 逮捕 JobDriver | 🔴 | 成人→押到 prison cell / 婴儿→押到婴儿床 |
| 商队囚犯入监 | 🔴 | 押送→监狱区域→完成登记 |
| 入监心情 | 🔴 | 殖民者看到新囚犯的心情 |

---

## 10. 门禁

| 功能 | 状态 | 说明 |
|------|------|------|
| 9 分组权限 | 🔴 | 殖民者/狱卒/奴隶/囚犯/商队/访客/敌队/儿童/机械体 |
| 门控 Gizmo | 🔴 | 右击门设置权限 |
| 门信息面板 | 🔴 | 显示当前门的权限摘要 |

---

## 11. UI（按原 UI 参考重写）

| 功能 | 状态 | 说明 |
|------|------|------|
| 主窗口框架 | 🟡 | 我们有 `Dialog_PrisonerManagement`，但只有 5 个 tab。需扩展为 7 tab（+ 安全/总览） |
| UI 配色常量 | 🔴 | 需要 `UiStyle.cs` 统一管理颜色 |
| 总览页 | 🔴 | 压制度弧形图 + 人口饼图 + 日程预览 + 活动日志 + 守卫列表 |
| 囚犯管理页 | 🟡 | 已有 per-pawn 卡片基础，缺年龄/状态筛选器和余额/改造值 Gizmo |
| 工作安排页 | 🟡 | 已有组级工作矩阵，缺每 pawn 的矩阵 + 列头拖拽 + 时薪设置 |
| 食物管理页 | 🔴 | 4 栏餐标配置，完全缺失 |
| 成瘾品页 | 🔴 | 药品目录 + 已选面板，完全缺失 |
| 服装管理页 | 🔴 | 3 年龄带服装选择 + 跨年龄开关，完全缺失 |
| 设置页 | 🔴 | 货币名称/赎身价格/工资模式/器官抵扣/殖民者工作禁区 |
| 活动日志 | 🔴 | 带时间戳的滚动日志列表 |
| 日程编辑器（环型+线型） | 🔴 | Timeline blocks + 拖拽 + 动画过渡 |
| 余额 Gizmo | 🔴 | 选中囚犯时显示余额 Gizmo，点击跳转到 Prisoners 页 |
| FloatMenu 下拉 | 🟡 | 已有部分 FloatMenu（分组选择/策略选择），需扩展 |
| 窗口布局工具 | 🔴 | 列宽/列数自适应计算的工具方法 |

---

## 12. XML Def

| 功能 | 状态 | 说明 |
|------|------|------|
| ThinkTreeDef（劳动） | ✅ | `RimPrisonBuilderDefs.xml` |
| PrisonerInteractionModeDef | ✅ | AllowLabor |
| MainButtonDef | ✅ | Prisoners 标签 |
| WorkGiverDef | ✅ | CouponShopStore |
| JobDef（2个） | ✅ | TakeToCouponShop / BuyFromCouponShop |
| JobDef（逮捕） | 🔴 | RPR_CaptureToPrisonCell / RPR_CaptureYoungToBed / RPR_DressPrisonBaby |
| HediffDef（4个） | 🔴 | 绝望/余额状态/债务工作/待抵扣 |
| ThoughtDef（27个） | 🔴 | 三制度 × 多事件 + 赎身 + 工资 + 福利餐 |
| MemeDef | 🔴 | RPR_PrisonMeme |
| PreceptDef（3个） | 🔴 | RPR_Regime_Harsh/Deterrence/Equality + RPR_WardenSystem_Enabled |
| RoomRoleDef | 🔴 | RPR_PrisonYard |
| LetterDef | 🔴 | 赎身信 |
| DesignationCategoryDef | 🔴 | 批量逮捕 |
| WorkTypeDef | 🔴 | RPR_Guardianship |
| AreaDef | 🔴 | Area_PrisonReset |

---

## 13. 扩展 API

| 功能 | 状态 | 说明 |
|------|------|------|
| 接口定义 | 🔴 | 8 个扩展接口（WorkEligibility/WorkEfficiency/LaborJob/BabyFood/BabySpecialFood/FoodEffect/Mood/Precept） |
| 注册/注销 | 🔴 | Register/Unregister 模式 |
| RimPrisonWorkApi | 🔴 | 外部调用入口 |
| RimPrisonFoodApi | 🔴 | 外部调用入口 |
| RimPrisonCultureApi | 🔴 | 外部调用入口 |
| RimPrisonFinanceApi | 🔴 | 外部调用入口 |
| RimPrisonStateApi | 🔴 | 外部调用入口 |

---

## 总体统计

| 类别 | ✅ 已有 | 🔴 缺 | 🟡 差一点 |
|------|---------|------|-----------|
| 基础设施 | 3 | 2 | 3 |
| Patch | 10 | 8 | 2 |
| 数据模型 | 1 | 7 | 2 |
| 日程 | 0 | 5 | 0 |
| 囚犯行为 | 0 | 8 | 0 |
| 经济 | 1 | 6 | 0 |
| 制度 | 0 | 7 | 0 |
| 意识形态 | 0 | 5 | 0 |
| 逮捕 | 0 | 4 | 0 |
| 门禁 | 0 | 3 | 0 |
| UI | 0 | 9 | 4 |
| XML Defs | 6 | 8 | 0 |
| API | 0 | 12 | 0 |
| **总计** | **21** | **84** | **11** |

---

## 推荐开发顺序

**Phase 1 — 地基**：Def / 基础设施 / UI 配色常量
**Phase 2 — 核心 Patch**：食物限制 / 服装强制 / 门禁 / 物品解禁
**Phase 3 — 囚犯行为**：消费 / 服装 / 医疗 / 婴儿
**Phase 4 — 日程**：数据结构 + 算法 + UI
**Phase 5 — 经济**：余额 / 工资 / 日费 / 赎身 / 器官扺债
**Phase 6 — 制度**：压制度 + 意识形态 + 心情
**Phase 7 — UI Tab**：总览 / 食物 / 药品 / 服装 / 设置
**Phase 8 — 逮捕 + 门禁**：逮捕设计器 + JobDriver + 门禁 UI
**Phase 9 — API + Compat**：扩展接口 + RimTalk
