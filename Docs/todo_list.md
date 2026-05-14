# 重构 TODO List — RimPrison 功能对比

---

## 1. 基础设施

✅ Harmony 入口 + PatchAll — `RimPrisonMod.cs`，已用
✅ 翻译键 — 中英双语完整，40+ 翻译键覆盖所有系统
✅ 设置系统 — `RimPrisonSettings` 已有
✅ 语言支持 — 中/英都有
✅ RimTalk 兼容 — 需要 `Compat/RimTalkCompat.cs`
🔴 扩展 API — 8 个扩展接口 + Register/Unregister + 5 个子 API 类（我们当前的API不兼容旧版扩展包）

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
✅ 日程覆写 — 通过 ApplyGroupSettings 直接写 timetable，无需 Patch
✅ 门禁 — `Patch_DoorAccess.cs`，PawnCanOpen 检查 Comp_DoorAccess
✅ 压制度→越狱/崩溃 — `Patch_SuppressionBreakThresholds.cs`

## 3. 数据模型 (ThingComp / GameComponent)

✅ 工作 tick 追踪 — `CompWorkTracker`
✅ 日程数据 — `PrisonerGroup.times` 组级 24h
✅ 门禁配置 — `Comp_DoorAccess`，单一 `allowPrisoners` bool
✅ 压制度快照 — `GameComponent_Suppression`，每 2000 tick 重算
✅ 活动日志 — `GameComponent_ActivityLog`，500 条环形缓冲
✅ 监狱区域 — `Area_Prison`（手动创建，无自动创建）
✅ 服装/成瘾品/食物策略 — `PrisonerGroup` 已集成原版 Policy + 策略页 UI

## 4. 日程系统

✅ 日程→行为路由 — ThinkTree 处理主路由，`ApplyGroupSettings` 直接写 `pawn.timetable`

## 5. 囚犯个人行为

✅ 买饭/成瘾品购买
✅ 服装强制 — `JobGiver_Prisoner_OptimizeApparel` 已处理：脱不允服装 + 储存区找策略服装

## 6. 经济系统

✅ 工资计算 — 每工作类型工资 × tick 比例
✅ 每日费用 — 床位与管理费用,日费扣除（）
✅ 赎身系统 — 余额达标→自动发信→接受扣钱释放 / 拒绝施加绝望(LetterDef — 赎身信，绝望HediffDef)
✅ 余额 Gizmo — 选中囚犯时显示余额 Gizmo
✅ 器官扺债
✅ 粮票系统 — CouponShop 体系（已有且独立）
✅ 粮票发放 — `Dialog_GrantCoupons` + `GameComponent_DailyAllowance` 余额/债务

## 7. 监狱制度系统

✅ 压制度公式 — `SuppressionCalculator`：基础 50 + guardFactor + turretFactor + prisonerFactor + moodFactor + healthFactor + regimeMod + difficultyMod
✅ 守卫/殖民者因子 — guardFactor: (guard×2+colonist)/effectivePrisoners
✅ 炮塔因子 — turretFactor: min(turrets×2, 20)
✅ 制度修正 — Harsh+10 / Deterrence+3 / Equality-5
✅ 难度修正 — 8 档难度 × 8 因子
✅ 精神崩溃阈值 — 压制度 < 阈值(50/55/40) → 允许
✅ 越狱阈值 — 压制度 < 阈值(30/35/20) → 允许
✅ 炮塔威胁判断 — 精神崩溃/越狱/低压制度 → 炮塔对囚犯视为威胁

## 8. 门禁

✅ 门控 Gizmo — 选中门时显示 Gizmo，单一允许/禁止按钮

## 9. UI

✅ 主窗口框架 — `Dialog_PrisonerManagement`，6 tab（日程/工作/囚犯管理/策略/总览/设置）
✅ UI 配色常量 — `RPR_UiStyle.cs`，颜色常量 + DrawPanel/DrawSubPanel 等
✅ FloatMenu 下拉 — 分组选择、策略选择均用 FloatMenu
✅ 囚犯管理页 — per-pawn 卡片：头像+年龄·阶段+分组+粮票+发放按钮
✅ 食物管理页 — 用原版 FoodPolicy 替代，集成到策略页
✅ 成瘾品页 — 用原版 DrugPolicy，集成到策略页
✅ 服装管理页 — 用原版 ApparelPolicy，集成到策略页
✅ 工作安排页 — 组级工作矩阵
✅ 设置页 — 货币名称/每日低保(真实) + 赎身/工资/器官
✅ 活动日志 — `GameComponent_ActivityLog` + 总览页实时显示最近 30 条
✅ 日程编辑器 — 组级 24h 颜色块编辑
✅ 总览页 — 人口统计(真实) + 压制度(可用) + 活动日志(真实)

## 10. XML Def

✅ ThinkTreeDef（劳动）
✅ PrisonerInteractionModeDef — AllowLabor
✅ MainButtonDef — Prisoners 标签
✅ WorkGiverDef — CouponShopStore
✅ JobDef（2个） — TakeToCouponShop / BuyFromCouponShop

## 11. 扩展 API

✅ 接口定义 — 8 个扩展接口（WorkEligibility/WorkEfficiency/LaborJob/BabyFood/BabySpecialFood/FoodEffect/Mood/Precept）
✅ 注册/注销 — Register/Unregister 模式
✅ RimPrisonWorkApi
✅ RimPrisonFoodApi
✅ RimPrisonCultureApi
✅ RimPrisonFinanceApi
✅ RimPrisonStateApi

## 12.婴儿相关

✅ 囚犯婴儿喂食 + 囚犯婴儿床管理（自动或手动设置监狱婴儿床）
✅ 婴儿保育 — 婴儿喂食由囚犯保育员完成（可选择隔离囚犯婴儿的保育）（注意，囚犯保育员无法保育殖民者婴儿——这可能是大部分人乐意看到的）

## 13. 新测试版即将加入的内容

✅ 监狱区限制殖民者（与机械族）特定的工作类型
🔴 制度设置（3 个，RPR_Regime_Harsh/Deterrence/Equality）
🔴 囚犯心情 ThoughtWorker（被割除器官）
🔴 文化相关（典狱长 Precept/Meme 支持）
🔴 守卫/典狱长

## 14. 删除的内容

🗑️ 进食扣费（我们使用商店购物机制，不需要自动扣费）
🗑️ 囚犯核心数据 — `CompPrisonerPolicy`：balance, debt, laborEnabled, workPriorities, payroll（我们已经有很多数据了，不是很清楚AI列的什么）
🗑️ 紧急需求恢复 — 食物<5%强制进食 / 休息<5%强制睡觉（原版没有这种保底吗？我们发现需要了再加）
🗑️ 互动锁 — 防止多个殖民者同时处理同一囚犯（需要再加，目前没发现相关bug）
🗑️ 医疗关注 — 自动 Tend + 转运到床（不懂说的是什么）
🗑️ 入监心情
🗑️ 批量逮捕设计器/逮捕 JobDriver/DesignationCategoryDef — 批量逮捕/JobDef（逮捕）/商队囚犯入监（已被其它 mod 实现，解耦）
🗑️ 每日发放（先抵债再入账—）（目前实现为直接进账/付款）
🗑️ PrisonerDied 心情