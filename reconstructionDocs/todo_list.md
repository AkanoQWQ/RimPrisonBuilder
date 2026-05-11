# 重构 TODO List — RimPrison 功能对比

---

## 1. 基础设施

✅ Harmony 入口 + PatchAll — `RimPrisonMod.cs`，已用
🟡 DefOf — `RP_DefOf.cs` 只有 2 个 DefOf 类，需补全 50+ def 引用
✅ 翻译键 — 中英双语完整，25+ 翻译键覆盖所有标签页
✅ 设置系统 — `RimPrisonSettings` 已有
✅ 语言支持 — 中/英都有
🔴 扩展 API — 需要 8 个扩展接口 + RimPrisonApi
🔴 RimTalk 兼容 — 需要 `Compat/RimTalkCompat.cs`

## 2. 基层 Patch

✅ IL Faction 注入 — `Patch_FactionInjection.cs`，全 WorkGiver 覆盖
✅ PawnCanUseWorkGiver — `Patch_PawnCanUseWorkGiver.cs`
✅ WorkTick 追踪 — `Patch_WorkTickTracker.cs`
✅ AutoForbidFix — `Patch_AutoForbidFix.cs`
✅ WorkSettingsInit — `Patch_WorkSettingsInit.cs`
✅ TimetableFix — `Patch_TimetableFix.cs`
✅ BlueprintFaction — `Patch_BlueprintFaction.cs`
✅ RespectAreaRestrictions
✅ ForbiddenWorkTypes
✅ AllowedAreaUI
✅ 食物限制 — `Patch_FoodRestrictionFix.cs`，囚犯自己进食遵守 FoodPolicy
✅ 物品自动解禁 — `Patch_AutoForbidFix.cs`
🔴 门禁 — 需要 Patch `Building_Door.PawnCanOpen`
🔴 压制度→越狱/崩溃 — 需要 Patch `PrisonBreakUtility` + `MentalBreaker`
🔴 婴儿保育路由 — 婴儿喂食/安置/穿衣 Job 路由到囚犯保育员
✅ 日程覆写 — 通过 ApplyGroupSettings 直接写 timetable，无需 Patch
🟡 工作类型解锁 — 囚犯年龄/背景不能禁用工作类型
🔴 PrisonerDied 心情 — 制度相关心情注入

## 3. 数据模型 (ThingComp / GameComponent)

🔴 囚犯核心数据 — `CompPrisonerPolicy`：balance, debt, laborEnabled, workPriorities, payroll
✅ 工作 tick 追踪 — `CompWorkTracker`
🟡 日程数据 — `PrisonerGroup.times` 组级 24h，无 3 轴×48 半小时间隔
🔴 门禁配置 — per-door 权限数据
🔴 压制度快照 — `GameComponent_Suppression`
🔴 活动日志 — `GameComponent_ActivityLog`，环形缓冲 + per-pawn 检索
🔴 每日结算 — `GameComponent_DailyCycle`，发薪 + 日费扣除（依赖 CompPrisonerPolicy）
🔴 守卫/典狱长 — `GameComponent_Guards`
🔴 监狱区域 — `GameComponent_PrisonArea`，Area 子类 + 设施标记
✅ 服装/成瘾品/食物策略 — `PrisonerGroup` 已集成原版 Policy + 策略页 UI

## 4. 日程系统

🗑️ 日程段数据结构 — 用原版 `TimeAssignmentDef[]` × 24h 替代
🗑️ 段操作算法 — 用原版 `TimeAssignmentSelector` 替代
🗑️ 3 轴日程存储 — 通过分组区分年龄，每组独立 24h 日程
🗑️ 日程编辑器 UI — 用原版颜色网格 + 鼠标涂色替代
✅ 日程→行为路由 — ThinkTree 处理主路由，`ApplyGroupSettings` 直接写 `pawn.timetable`

## 5. 囚犯个人行为

🔴 买饭
🔴 成瘾品购买 — 药品白名单 + 娱乐窗口内用药 + 用量限制
✅ 服装强制 — `JobGiver_OptimizeApparel` 已处理：脱不允服装 + 储存区找策略服装
🔴 婴儿喂食 — 保育员喂食 + 乳房/奶瓶/特殊食物 + 安置 + 穿衣
🔴 婴儿床管理 — 自动分配/收回监狱婴儿床
🔴 医疗关注 — 自动 Tend + 转运到床
🔴 紧急需求恢复 — 食物<5%强制进食 / 休息<5%强制睡觉
🔴 互动锁 — 防止多个殖民者同时处理同一囚犯

## 6. 经济系统

🔴 余额/债务 — `CompPrisonerPolicy` 存储，`ChargeBalanceOrAddDebt`
🔴 工资计算 — 每工作类型工资率 × tick 比例
🔴 每日发放 — 自动/狱卒发放，先抵债再入账
🔴 每日费用 — 食物 8 + 床位 4/威慑 + 放风 3/高压
🔴 赎身系统 — 余额达标→自动发信→接受扣钱释放 / 拒绝两次施加绝望
🔴 器官扺债 — 自动/手动，token 部位匹配，抵债 100 + 发奖励
✅ 粮票系统 — CouponShop 体系（已有且独立）
✅ 粮票发放 — `Dialog_GrantCoupons` + `GameComponent_DailyAllowance`

## 7. 监狱制度系统

🔴 压制度公式 — 9 参数公式已有（`SuppressionCalculator`），需独立实现
🔴 守卫/殖民者因子 — guardFactor: (guard×2+colonist)/effectivePrisoners
🔴 炮塔因子 — turretFactor: min(turrets×2, 20)
🔴 制度修正 — Harsh+10 / Deterrence+3 / Equality-5
🔴 难度修正 — 8 档难度 × 8 因子
🔴 精神崩溃阈值 — 压制度 < 阈值(50/55/40) → 允许
🔴 越狱阈值 — 压制度 < 阈值(30/35/20) → 允许
🔴 炮塔威胁判断 — 精神崩溃/越狱/低压制度 → 炮塔对囚犯视为威胁

## 8. 意识形态

## 9. 逮捕 / 入监

🔴 批量逮捕设计器 — 框选逮捕
🔴 逮捕 JobDriver — 成人→押到 prison cell / 婴儿→押到婴儿床
🔴 商队囚犯入监 — 押送→监狱区域→完成登记
🔴 入监心情 — 殖民者看到新囚犯的心情

## 10. 门禁

🔴 9 分组权限 — 殖民者/狱卒/奴隶/囚犯/商队/访客/敌队/儿童/机械体
🔴 门控 Gizmo — 右击门设置权限
🔴 门信息面板 — 显示当前门的权限摘要

## 11. UI

✅ 主窗口框架 — `Dialog_PrisonerManagement`，6 tab（日程/工作/囚犯管理/策略/总览/设置）
✅ UI 配色常量 — `RPR_UiStyle.cs`，颜色常量 + DrawPanel/DrawSubPanel 等
🟡 总览页 — 人口统计(真实) + 压制度(占位) + 活动日志(占位)
✅ 囚犯管理页 — per-pawn 卡片：头像+年龄·阶段+分组+粮票+发放按钮
🟡 工作安排页 — 组级工作矩阵
✅ 食物管理页 — 用原版 FoodPolicy 替代，集成到策略页
✅ 成瘾品页 — 用原版 DrugPolicy，集成到策略页
✅ 服装管理页 — 用原版 ApparelPolicy，集成到策略页
🟡 设置页 — 货币名称/每日低保(真实) + 赎身/工资/器官(占位)
🔴 活动日志 — 带时间戳的滚动日志列表
🟡 日程编辑器 — 组级 24h 颜色块编辑，无拖拽/动画
🔴 余额 Gizmo — 选中囚犯时显示余额 Gizmo
✅ FloatMenu 下拉 — 分组选择、策略选择均用 FloatMenu
🔴 窗口布局工具 — 列宽/列数自适应计算

## 12. XML Def

✅ ThinkTreeDef（劳动）
✅ PrisonerInteractionModeDef — AllowLabor
✅ MainButtonDef — Prisoners 标签
✅ WorkGiverDef — CouponShopStore
✅ JobDef（2个） — TakeToCouponShop / BuyFromCouponShop
🔴 JobDef（逮捕） — RPR_CaptureToPrisonCell / RPR_CaptureYoungToBed / RPR_DressPrisonBaby
🔴 HediffDef（4个） — 绝望/余额状态/债务工作/待抵扣
🔴 ThoughtDef（27个） — 三制度 × 多事件 + 赎身 + 工资 + 福利餐
🔴 MemeDef — RPR_PrisonMeme
🔴 PreceptDef（3个） — RPR_Regime_Harsh/Deterrence/Equality + RPR_WardenSystem_Enabled
🔴 RoomRoleDef — RPR_PrisonYard
🔴 LetterDef — 赎身信
🔴 DesignationCategoryDef — 批量逮捕
🔴 WorkTypeDef — RPR_Guardianship
🔴 AreaDef — Area_PrisonReset

## 13. 扩展 API

🔴 接口定义 — 8 个扩展接口（WorkEligibility/WorkEfficiency/LaborJob/BabyFood/BabySpecialFood/FoodEffect/Mood/Precept）
🔴 注册/注销 — Register/Unregister 模式
🔴 RimPrisonWorkApi
🔴 RimPrisonFoodApi
🔴 RimPrisonCultureApi
🔴 RimPrisonFinanceApi
🔴 RimPrisonStateApi

## 14.非首个发布版内容的内容

🗑️ Meme 支持 — `RPR_PrisonMeme`，需要 XML + DefOf
🗑️ 制度 Precept — 3 个 Precept（高压/威慑/平等）
🗑️ 典狱长 Precept — `RPR_WardenSystem_Enabled`
🗑️ 文化同步 — 从 Ideology 读取→覆盖配置
🗑️ 囚犯心情 ThoughtWorker — 7 个 ThoughtWorker + 1 个 ThoughtSituational

## 15.与新机制不符，删掉的内容

🗑️ 进食扣费 — 需要 Patch `Thing.Ingested`，囚犯吃东西自动扣余额（我们使用商店购物机制，不需要自动扣费）

---

## 推荐开发顺序

1. **Phase 1 — 地基 ✅**：Def / 基础设施 / UI 配色常量 / 重命名 RimPrison
2. **Phase 2 — 核心 Patch 🟡**：门禁 / 工作类型解锁
3. **Phase 3 — 囚犯行为**：消费 / 服装 / 医疗 / 婴儿
4. **Phase 4 — 日程**：数据结构 + 算法 + UI（组级日程已有）
5. **Phase 5 — 经济**：余额 / 工资 / 日费 / 赎身 / 器官扺债
6. **Phase 6 — 制度**：压制度 + 意识形态 + 心情
7. **Phase 7 — UI 收尾**：活动日志 / 总览完善 / 设置页完善 / 余额 Gizmo
8. **Phase 8 — 逮捕 + 门禁**：逮捕设计器 + JobDriver + 门禁 UI
9. **Phase 9 — API + Compat**：扩展接口 + RimTalk
