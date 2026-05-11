# PrisonResetMapComponent.cs

**大小**: 135KB / 3910 行
**路径**: `RIMPRISON/Source/RimPrison.Reset/PrisonResetMapComponent.cs`
**类型**: `MapComponent` — 整个 Mod 的**唯一数据中心**

---

## 作用

这个类是整个 Mod 的**大脑 + 数据库 + 调度中心**。它管理：

| 子系统 | 行号范围 | 说明 |
|--------|---------|------|
| 数据字段 | 13–104 | **100+ 个私有字段**，包括 40+ 个 List/Dictionary |
| 运行时缓存 | 106–148 | `[Unsaved]` 标记的非持久化缓存 Dictionary |
| 属性访问器 | 155–431 | 大量 get/set 属性，部分有副作用 |
| 序列化 | 446–553 | **60+ 行 Scribe 调用** |
| MapComponentTick | 570–602 | 每 tick 主循环：hydrate → dirty pawns → dirty things → guard sync → work sync → area refresh → facility mark → suppression → runtime service |
| 缓存水合 | 694–807 | `EnsureRuntimeCacheStateHydrated()` + Dirty/Mark 系统 |
| 压制度计算 | 816–919 | 每 250 tick 重新计算（遍历所有囚犯 + 炮塔 + 守卫） |
| 囚犯策略 | 921–949 | `GetOrCreatePolicy()` — per-pawn 数据模型 |
| 日程系统 | 951–1088 | 3 轴（成人/儿童/婴儿）× 48 半小时间隔 = 144 个 int 存储 |
| 工作分配 | 1090–1566 | 策略应用、可见工作类型、优先级矩阵、默认策略 |
| 服装/食物/药品 | 1568–1993 | 餐标白名单、药品白名单、消费扣费 |
| 余额/债务 | 1995–2110 | 余额管理、发薪、欠债、赎身申请 |
| 商队/逮捕 | 2160–2321 | 入监登记、押送分配 |
| 每日结算 | 2377–2466 | 发薪 + 日费扣除 |
| 日志系统 | 2468–2497 | 2000 条上限的环形日志缓冲 |
| 文化/意识形态 | 2499–2564 | 从意识形态同步制度/狱卒系统 |
| 守卫/狱卒 | 2566–2888 | 守卫分配、典狱长任命、Warden 工作同步 |
| 门禁 | 2890–2948 | 门禁权限配置 |
| 监狱设施 | 2950–3054 | 建筑设施标记 + 每 2500 tick 自动标记 |
| 婴儿床 | 3056–3291 | 婴儿床 OwnerType 切换（用**反射**） |
| 成瘾品使用追踪 | 1696–1777 | 娱乐窗口计数 + 并行 List 索引重建 |
| 债务抵扣/器官 | 3420–3494 | 器官扺债、心情记忆 |
| 清理/缓存重建 | 3581–3909 | RebuildCaches、SanitizeNames、工资费率 |

---

## 致命问题

### 1. 上帝对象反模式 — 这是整个 Mod 唯一的"数据库"

这个类做了 **所有事情**。数据、逻辑、缓存在一个 3910 行的文件里。没有任何职责分离。

### 2. 并行 List 地狱（至少 12 组）

```
prisonerPolicies              List<PrisonerPolicyData>   ← 唯一正常建模的数据
activityLog                   List<string>               ← 三条并行：log + pawnId + personalActivity
balanceRecordPawnThingIds     List<string>               ← 两条并行：pawnId + record
personalActivityPawnThingIds  List<string>               ← 两条并行
activeWorkSessionPawnThingIds List<string>               ← 三条并行
customPayrollRateWorkTypeDefNames + customPayrollRateValues ← 两条并行
recreationDrugUsePawnThingIds + recreationDrugUseCounts + recreationDrugUseWindowIds ← 三条并行
pendingTradeIntakePawnThingIds + pendingTradeIntakeEscortPawnThingIds ← 两条并行
pendingCapturePawnThingIds + pendingCaptureEscortPawnThingIds ← 两条并行
```

每组都是一次 RemoveAt/Add 时索引错位的风险。已经看到了 `EnsureDrugUseStateCollections()` 里 `while (counts.Count < pawnIds.Count)` 这种防御性补齐代码——作者自己也知道会错位。

### 3. PERF: `MapComponentTick()` 每 tick 做太多事

```csharp
public override void MapComponentTick()  // 每 tick (60次/秒@1x)
{
    EnsureRuntimeCacheStateHydrated();   // 条件性重建整个 pawn+thing 缓存
    ProcessDirtyThingBudget();           // 消费32个 dirty thing ID
    ProcessDirtyPawnBudget(ticks);       // 消费64个 dirty pawn ID
    // guard sync (250 tick间隔)
    // work session sync (600 tick间隔)
    // prison facility auto-mark (2500 tick间隔)
    // suppression recalc (250 tick间隔) ← 遍历所有囚犯+炮塔
    // runtime service tick (每 tick!)
}
```

250 tick=10秒（1x速度）。压制度每 10 秒重算一次，即使没有任何变化。`DirtyPawnBudgetPerTick=8` 但这里消费 64 个。

### 4. PERF: `RecalculateSuppression()` 每 250 tick 全量扫描

```csharp
// 遍历所有被管理的囚犯 (line 832)
// 遍历所有炮塔建筑 (line 867-876)
// 调用 IsCellInPrisonArea 检查每个炮塔
// 8 个 SuppressionCalculator 方法调用
// 分配新的 PrisonSuppressionSnapshot (line 897)
```

没有任何缓存、没有任何增量计算。即使只有一个囚犯的心情变了 0.1%，也是全量重算。

### 5. 反射访问原版私有字段

```csharp
typeof(Building_Bed).GetField("forOwnerType", 
    BindingFlags.Instance | BindingFlags.NonPublic);
// ...
BedForOwnerTypeField?.SetValue(bed, ownerType);  // line 3150
```

每次设置婴儿床 OwnerType 都用反射。跨版本必炸。应该用 Harmony `___forOwnerType` 注入。

### 6. PERF: `EnsureDefaults()` 创建 40+ 个新 List

`EnsureDefaults()` 被构造函数和 `FinalizeInit()` 各调用一次。每次检查 ~40 个字段是否为 null 并 `new List<>()`。在 FinalizeInit 里还会立即被 `RebuildCaches()` 重建一批 Dictionary。

### 7. 数据一致性问题：List ↔ Dictionary 双重维护

很多数据同时以 `List<T>`（持久化）+ `Dictionary<string, T>`（`[Unsaved]` 缓存）存储。缓存在 `PostLoadInit` 时通过 `RebuildCaches()` 重建，但运行时修改必须同时更新两边。`GetOrCreatePolicy`、`GetOrCreateDefaultWorkPolicy` 等方法里到处可见 `if (dict.Count == 0 && list.Count > 0) RebuildCaches()` 的防御代码。

### 8. Scribe 序列化过于脆弱

`ExposeData()` 里有 60+ 个 `Scribe_Values.Look` + `Scribe_Collections.Look` 调用。`SuppressionSnapshot` 的 15 个字段是逐个 Scribe 的（line 488-505），而不是整体 Deep Look。改一个字段名就要同时改序列化键名，容易导致数据静默丢失。

### 9. 成瘾品使用计数器有内存泄露风险

```csharp
recreationDrugUsePawnThingIds   // 三并行 List
recreationDrugUseCounts
recreationDrugUseWindowIds
recreationDrugUseIndexByPawnId  // [Unsaved] Dictionary 缓存
```

死掉的 pawn、被释放的 pawn 不会从这里清理。只增不减。

### 10. 日志缓冲硬截断

```csharp
if (activityLog.Count > 2000)
    activityLog.RemoveRange(0, overflow);    // line 2489
// balanceRecordEntries 同上 (line 3395)
// personalActivityEntries 同上 (line 3416)
```

每次追加都要检查 + 批量 RemoveRange。用 `Queue<T>` 或环形缓冲会好得多。

---

## 值得保留的功能设计

以下功能设计是好的，应该移植到我们的架构：

1. **Per-pawn 策略模型** (`PrisonerPolicyData`) — 每个囚犯独立的工作/余额/赎身/债务配置
2. **三级日程轴** (成人/儿童/婴儿) — 分生命阶段的日程安排
3. **四档餐标 + 价格** — 福利/婴儿/标准/精致，带价格强制排序
4. **门禁权限系统** — 虽然是复杂了点，但概念好
5. **赎身申请** — 余额达标后自动发信，最多拒绝 2 次
6. **活动日志** — per-pawn 和全局双轨记录
7. **工资/债务抵扣** — 先抵债再发薪的逻辑是好的
8. **意识形态集成** — 从 Meme/Precept 读取监狱制度

---

## 重构方案

**不要在这个文件上修修补补。**

1. **拆分数据层**：每个子系统独立的 ThingComp/GameComponent
   - `CompPrisonerPolicy` → per-pawn 策略（替代 `PrisonerPolicyData` + `prisonerPolicies` 并行 List）
   - `CompPrisonerBalance` → per-pawn 余额
   - `GameComponent_PrisonSchedule` → 全局日程
   - `GameComponent_PrisonDoorAccess` → 门禁
   - `GameComponent_PrisonMealConfig` → 餐标配置
   - `GameComponent_PrisonGuards` → 守卫/典狱长

2. **消除所有并行 List**：用带 ThingID 索引的 Dictionary + Scribe_Deep 替代

3. **事件驱动替代轮询**：
   - 压制度 → 只在守卫/囚犯/炮塔变化时重算
   - 设施自动标记 → 只在建筑完成/拆除时触发
   - 工作会话同步 → 用 Harmony 钩子，不定时轮询

4. **反射 → Harmony 注入**：`Building_Bed.forOwnerType` 用 `___forOwnerType` 三下划线方案

5. **序列化**：每个 Comp 自己 `PostExposeData()`，不做集中式 60 行 Scribe

6. **日志**：独立 `GameComponent_ActivityLog`，用 `Queue<T>` + 自动清理
