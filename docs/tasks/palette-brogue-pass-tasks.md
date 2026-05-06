# Palette Brogue Pass — Task Document

## Context

把 DungeonDescent 的色板从 9 色 IBM CGA 子集重构为按 **Brogue vanilla 1.7.5** 采样的 16-22 色（target 20）语义化色板，并新增 2 种纯装饰 floor variant（`FloorMossy` `,` + `FloorCracked` `'`，房间 10% / 走廊 1%），是 brief 视觉打磨四切片中的**第一切片**。

- **来源 plan**: [`docs/plans/palette-brogue-pass.md`](../plans/palette-brogue-pass.md)
- **来源 brief**: [`docs/briefs/palette-brogue-pass.md`](../briefs/palette-brogue-pass.md)
- **上游 brief**: [`docs/briefs/pixel-ui-rewrite.md`](../briefs/pixel-ui-rewrite.md)（视觉打磨整段 deferred 段从此开始消化）

> 所有任务带有 `Depends on` 注释。**必须按编号顺序执行**——Task 1 故意 break build，Task 2 修复，Task 3-5 每个都有独立 build-clean 边界。乱序执行（如先做 Task 3）会让渲染器 `_ =>` 默认 arm 静默把 variant 当 wall 渲染（plan Risks 表明确警告的 bug）。
>
> **Task 6 是人工任务**——Claude 实现者在 Task 5 完成后**必须停手交人**，不可自动声称通过 GUI 目视核查。

## Cross-Phase Dependency Note

Task 3 起的代码引用 Task 1 引入的新 `Palette` 槽位名（`Palette.WallStone`、`Palette.FloorMossy`、`Palette.UiAccent`、`Palette.Dim()` 等）。这些槽位在 plan Technical Approach 第 1 节中规定，但在 Task 1 落地之前不存在于代码里。每个 Task 的代码示例假定前序 Task 已 DONE。

## Tasks

### Task 1: 重写 `src/Core/Palette.cs` 为 20 槽语义化色板 + `Dim()` helper

**Status:** DONE

删除现有 9 个 `Color` 静态字段（`White`, `Yellow`, `Green`, `Red`, `Cyan`, `Magenta`, `Blue`, `Gray`, `DarkRed`），替换为按 Brogue vanilla 1.7.5 采样的 16-22 个语义化槽位（target 20），按 5 组（Architecture / Entity / Item / Effect / UiChrome）分组。每个 RGB 值附 Brogue 1.7.5 来源注释（如 `// Brogue 1.7.5 src/brogue/Globals.c:wallForeColor`）；fallback 截图取色亦标注。同时添加静态 helper `Color Dim(Color c, float factor = 0.4f)`，按通道乘以 factor 得到暗化版本（B2/B4 用于记忆色）。

文件头注释包含：采样源链接（Pender/brogue 1.7.5 GitHub repo）、采样日期（YYYY-MM-DD）、A1 阶段是否实战收缩了 N 及理由。

**槽位规范（target 20）：**

```
Architecture: WallStone, FloorBase, FloorMossy, FloorCracked
Entity:       EntityPlayer, EntityHumanoid, EntityBeast, EntityMagical
Item:         ItemConsumable, ItemEquipment, ItemTreasure, ItemStaff
Effect:       EffectHealth, EffectPoison, EffectFire, EffectIce
UiChrome:     UiTitle, UiText, UiAccent, UiDim
```

`EntityPlayer` 锁定为 white（authentic Brogue）。若 Brogue 实际配色结构不能整齐塞进 5×4，允许收缩到 16-19 或 expand 到 21-22；任何偏离 20 的决定必须在文件头注释说明。

**颜色护栏（在槽位 RGB 决定时主动遵守）：**

- Variant (`FloorMossy` / `FloorCracked`) vs `FloorBase`：每通道 RGB 差值 ≤ 30
- 关键语义对建议距离 ≥ 80：`EntityPlayer` vs 任意 `Entity*`、`EffectHealth` vs `EffectPoison`、`FloorMossy/Cracked` vs 任意 `Entity*`

**Files:**

- Modify: `src/Core/Palette.cs`

**Acceptance criteria:**

- `dotnet build` 在 Task 1 完成时**预期失败**；目视确认错误集合**不含** `src/Core/Palette.cs:*`（即新 `Palette.cs` 自身语法 / 类型正确，错误全来自旧 `Palette.White / Yellow / ...` 引用，留待 Task 2 修复）
- 槽位数量在 16-22 区间内，全部命名遵循 5 组语义层级
- 每个 RGB 值的来源行可追溯（通过注释，源码引用或截图取色都接受）
- `Palette.Dim()` 方法存在，签名为 `public static Color Dim(Color c, float factor = 0.4f)`
- 文件头注释含采样源 URL + 采样日期

---

### Task 2: Audit + 重新分配全部 72 处 `Palette.X` 引用

**Status:** TODO

**Depends on:** Task 1

运行 `dotnet build` 得到全量编译错误列表（约 60-72 errors），逐个 `Palette.OldName` call site 按下方映射决定新槽位。**Compile-error-driven audit**——编译器会列出每处需要修复的位置，零遗漏。

**审计映射表：**

| 文件 | 引用数 | 映射规则 |
|---|---|---|
| `src/UI/SadConsoleRenderer.cs` | 61 | 见下方分类细则 |
| `src/Entities/MonsterTemplate.cs` | 4 | Rat=`EntityBeast`、Goblin=`EntityHumanoid`、Troll=`EntityHumanoid`、Dragon=`EntityMagical` |
| `src/Items/Item.cs` | 5 | 默认色 (line 9) → `UiText`；Potion=`ItemConsumable`；Sword=`ItemEquipment`；Armor=`ItemEquipment`；GoldPile=`ItemTreasure` |
| `src/Entities/Player.cs` | 1 | → `EntityPlayer`（**注意：Player 颜色由 Yellow 改为 white**——这是 plan 第 1 节明确决定，不要保留 Yellow）|
| `src/Entities/Entity.cs` | 1 | 默认 fallback → `UiText`（实际所有子类都覆盖此值，仅作 fallback） |

**`SadConsoleRenderer.cs` 61 处分类细则：**

| 分类 | 当前色 | 行号示例 | 新槽位 |
|---|---|---|---|
| 标题栏 (`Dungeon Descent` 横条) | `Yellow` | 20 | `UiTitle` |
| Remembered tile（exploration 但 invisible） | `Gray` | 68-71 | **暂用 `UiDim` 占位**（Task 4 重写为 `Palette.Dim(...)`）|
| Visible tile：floor / wall | `White` | 79, 82 | `FloorBase` / `WallStone` |
| Visible tile：stairs | `Cyan` | 80-81 | `UiAccent` |
| 状态栏 HP 数值（健康） | `Green` | 99, 140 | `EffectHealth` |
| 状态栏 HP 数值（< 1/3，警示色） | `Red` | 99, 140 | `EffectPoison`（暂代"低 HP / 危险"语义；plan Open Questions 已记录命名偏差，C1 决定是否升级为独立 `EffectDanger` 槽）|
| 状态栏 ATK 标签 | `Cyan` | 102 | `UiAccent` |
| 状态栏 DEF 标签 | `Blue` | 104 | `UiAccent` |
| 状态栏 LV 标签 | `Magenta` | 106 | `UiAccent` |
| 状态栏 EXP 标签 | `Yellow` | 108 | `UiAccent` |
| 状态栏 G (Gold) 标签 | `Yellow` | 110 | `UiAccent` |
| 状态栏 Sc (Score) 标签 | `White` | 112 | `UiText` |
| 状态栏分隔空格 | `White` | 101/103/105/... | `UiText` |
| Hint 行（status row 1） | `Gray` | 118 | `UiDim` |
| Message log 文本 | `White` | 128 | `UiText` |
| Inventory 标题 | `Yellow` | 137 | `UiTitle` |
| Inventory body / prompt | `White` | 142, 144, 150, 160, 162, 172 | `UiText` |
| Help 标题 | `Cyan` | 178 | `UiTitle` |
| Help body | `White` | 179-190 | `UiText` |
| Help footer | `Gray` | 191 | `UiDim` |
| GameOver banner | `Red` | 202-204 | `EffectPoison` |
| GameOver stat lines | `White` | 205-208 | `UiText` |
| GameOver footer | `Gray` | 209 | `UiDim` |
| Victory banner | `Yellow` | 218-220 | `UiTitle` |
| Victory stat lines | `White` | 221-223 | `UiText` |
| Victory footer | `Gray` | 224 | `UiDim` |

**注**：状态栏 ATK/DEF/LV/EXP/G 全部映射到 `UiAccent` 让原先彩色的 stat 行变成单色 accent，是有意权衡（plan A2 audit 明确）。若 Task 6 视觉核查发现状态栏失去可读层次，回归 Task 1 增设 `UiAccent2` 槽。

**禁止保留 backward-compat alias**（如 `White → UiTitle` 别名一律不留）。

**Files:**

- Modify: `src/UI/SadConsoleRenderer.cs`
- Modify: `src/Entities/MonsterTemplate.cs`
- Modify: `src/Items/Item.cs`
- Modify: `src/Entities/Player.cs`
- Modify: `src/Entities/Entity.cs`

**Acceptance criteria:**

- `dotnet build` 零错误零警告
- `grep -rn "Palette\." --include="*.cs" /home/kenspc/projects/DungeonDescent/ | wc -l` 仍约 72（数量级，允许 ±5）
- `dotnet run` 跑通：游戏可玩、无渲染异常
- `Player.cs:18` 的 `Color = Palette.Yellow` 已改为 `Color = Palette.EntityPlayer`（即 white）
- 无任何 `Palette.White / Yellow / Green / Red / Cyan / Magenta / Blue / Gray / DarkRed` 旧名残留（`grep "Palette\.White\|Palette\.Yellow\|..."` 返回 0）
- Commit message 归档每个旧名 → 新名映射（grep summary 即可）
- **🚧 Independent stop point — Milestone A complete**：色板重构完成；视觉打磨整体仍 incomplete

---

### Task 3: `TileType` enum 加入 `FloorMossy` 和 `FloorCracked`

**Status:** TODO

**Depends on:** Task 2

修改 `src/Map/TileType.cs`，把 `enum TileType { Wall, Floor, StairsDown, StairsUp }` 扩为 `enum TileType { Wall, Floor, FloorMossy, FloorCracked, StairsDown, StairsUp }`。

**注意**：因 `SadConsoleRenderer.cs:71` 和 `:82` 仍保留 `_ =>` 默认 arm，新增 enum 值不会触发 CS8509 编译错误——但意味着如果在 Task 4 之前误跑了 Task 5，新 variant 会**静默地**渲染为 `#`（wall）字符。**必须严格按 Task 3 → 4 → 5 顺序执行。**

**Files:**

- Modify: `src/Map/TileType.cs`

**Acceptance criteria:**

- `enum TileType` 含 6 个值：`Wall, Floor, FloorMossy, FloorCracked, StairsDown, StairsUp`
- `dotnet build` 零错误零警告
- `Tile.IsWalkable` 表达式（`Type != TileType.Wall`）无需修改——新 variant 自动继承 walkable
- `dotnet run` 跑通；地图视觉无变化（Task 5 才让 variant 出现，Task 4 才让 variant 显示对的字符）

---

### Task 4: 渲染器两个 switch 改为 exhaustive + 用 `Palette.Dim()` 改记忆色

**Status:** TODO

**Depends on:** Task 3

修改 `src/UI/SadConsoleRenderer.cs` 的两个 tile rendering switch 表达式（line 66-72 remembered，line 77-83 visible）。**移除 `_ =>` 默认 arm**，改为显式列出所有 6 个 `TileType` case，让编译器在以后新增 enum 值时强制提示（CS8509）。同时在 remembered 分支用新增的 `Palette.Dim(...)` helper 让记忆色保留色相但暗化（不再是单一 `UiDim` 灰）。

**Visible 分支**（line 77-83）：

```csharp
(Color color, char glyph) = tile.Type switch
{
    TileType.Wall          => (Palette.WallStone,    '#'),
    TileType.Floor         => (Palette.FloorBase,    '.'),
    TileType.FloorMossy    => (Palette.FloorMossy,   ','),
    TileType.FloorCracked  => (Palette.FloorCracked, '\''),
    TileType.StairsDown    => (Palette.UiAccent,     '>'),
    TileType.StairsUp      => (Palette.UiAccent,     '<'),
};
```

**Remembered 分支**（line 66-72）：

```csharp
(Color color, char glyph) = tile.Type switch
{
    TileType.Wall          => (Palette.Dim(Palette.WallStone),    '#'),
    TileType.Floor         => (Palette.Dim(Palette.FloorBase),    '.'),
    TileType.FloorMossy    => (Palette.Dim(Palette.FloorMossy),   ','),
    TileType.FloorCracked  => (Palette.Dim(Palette.FloorCracked), '\''),
    TileType.StairsDown    => (Palette.Dim(Palette.UiAccent),     '>'),
    TileType.StairsUp      => (Palette.Dim(Palette.UiAccent),     '<'),
};
```

楼梯统一改用 `UiAccent`（替代 Task 2 中临时使用的 `Cyan` 之前的版本——Task 2 已将 `Cyan` 映射到 `UiAccent`，本 Task 只是确保 switch 内一致使用）。

**Files:**

- Modify: `src/UI/SadConsoleRenderer.cs`

**Acceptance criteria:**

- 两个 switch 表达式均**不含** `_ =>` arm（exhaustive，将来加新 `TileType` 必触发 CS8509）
- Visible 分支为 6 个 explicit `TileType` case
- Remembered 分支为 6 个 explicit `TileType` case，每个色都通过 `Palette.Dim(...)` 包裹
- `dotnet build` 零错误零警告
- `dotnet run` 跑通；地图视觉**仍无 variants 出现**（Task 5 才注入），但 wall/floor 颜色应反映新 palette
- 离开探索过的房间，目视确认记忆色为 visible 色的 dim 版本（暗版 `WallStone` / `FloorBase` / `UiAccent`，不再是均匀灰）

---

### Task 5: 在 `Map.cs` 加入 `ScatterFloorVariants()` 后处理

**Status:** TODO

**Depends on:** Task 4

在 `src/Map/Map.cs` 加入私有方法 `ScatterFloorVariants()`，在 `Map(int seed)` 构造函数中 `GenerateRooms()` 之后、`PlaceStairs()` 之前调用。算法遍历整个 `Width × Height` 格子，对每个 `TileType.Floor` cell 用 `Map._rng.NextDouble()` 抽取概率：

- 若 `Rooms.Any(r => r.Contains(p))`（房间内）：< 0.05 → mossy；< 0.10 → cracked
- 否则（走廊内）：< 0.005 → mossy；< 0.010 → cracked

**实现：**

```csharp
private void ScatterFloorVariants()
{
    for (int y = 0; y < Height; y++)
    for (int x = 0; x < Width;  x++)
    {
        if (_tiles[x, y].Type != TileType.Floor) continue;
        var p = new Point(x, y);
        bool inRoom = Rooms.Any(r => r.Contains(p));
        double r = _rng.NextDouble();
        if (inRoom)
        {
            if (r < 0.05)      _tiles[x, y].Type = TileType.FloorMossy;
            else if (r < 0.10) _tiles[x, y].Type = TileType.FloorCracked;
        }
        else
        {
            if (r < 0.005)      _tiles[x, y].Type = TileType.FloorMossy;
            else if (r < 0.010) _tiles[x, y].Type = TileType.FloorCracked;
        }
    }
}
```

构造函数修改（`Map.cs:19`）：

```csharp
public Map(int seed)
{
    _rng = new Random(seed);
    Fill(TileType.Wall);
    GenerateRooms();
    ScatterFloorVariants();   // ← 新增
    PlaceStairs();
}
```

`Room.Contains(Point)` 已存在（`src/Map/Room.cs:11-13`），无需新 helper。`Tile.IsWalkable` 自动让 variant walkable（`Type != TileType.Wall`），无需新 helper。

**Files:**

- Modify: `src/Map/Map.cs`

**Acceptance criteria:**

- `Map.cs` 含私有方法 `ScatterFloorVariants()`
- `Map(int seed)` 构造函数在 `GenerateRooms()` 之后、`PlaceStairs()` 之前调用 `ScatterFloorVariants()`
- 仅替换 `TileType.Floor` 的 cell（不动 stairs / wall）
- 使用 `Map._rng` 共享种子（可重现）
- `dotnet build` 零错误零警告
- `dotnet run` 跑通；可启动到第一层游戏窗口（Task 6 才做完整目视核查）
- **🚧 Independent stop point — Milestone B complete**：brief 第一切片代码层面交付完成；待 Task 6 人工核查放行

---

### Task 6: 人工目视核查（manual, GUI required）

**Status:** TODO

**Depends on:** Task 5

> ⚠️ **Claude 实现者不能自动跑此任务。** 需要人工启动 GUI 游戏并观察。Claude 应在 Task 5 完成后停手，把控制权交给人工。

跑一局完整游戏（楼层 1 → 楼层 5+），逐项核查：

| 检查项 | 通过条件 |
|---|---|
| Palette 整体感 | 暗背景 + 微妙 Brogue 风；不是 IBM CGA 的高对比 |
| Variant 房间密度 | 每个房间约 10% floor 是 `,` / `'`；目测不会让人误以为是道具或怪 |
| Variant 走廊密度 | 走廊大部分 `.`；偶尔（每 ~100 cell 中约 1 颗）有 variant |
| 关键语义对辨识 (F3) | Player vs 同房间 monster 远看 ≥ 80 RGB 距离感；health 状态 vs poison 状态远看分得清 |
| Variant 撞 monster 色 (F4) | 巡视 floor 1-5 实际遭遇怪种类，确认无 variant 色与 monster 色"远看相似" |
| Walkable 一致性 | 走过 `.` `,` `'` 的速度 / 消息提示完全相同 |
| FOV 一致性 | Variants 进入 / 离开 FOV 时与 base floor 行为一致 |
| 记忆色分层 | 探索过但当前不可见的 tile 颜色为 visible 色的暗版本（不是单一灰）；可识别 mossy / cracked / wall / floor 区别 |
| 状态栏可读层次 | 单色 `UiAccent` 标签是否仍能清晰区分 ATK/DEF/LV/EXP/G——若失败，回归 Task 1 增设 `UiAccent2` 槽 |

**若任何检查项不过**，按下面调整路径回归对应 Task：

- 视觉密度不对 → 调 Task 5 中 `0.05 / 0.10 / 0.005 / 0.010` 阈值
- Variant 撞色 → 调 Task 1 中 variant 槽位 RGB
- 记忆色太暗或太亮 → 调 Task 1 中 `Palette.Dim()` 的 factor 默认值
- HP 警示与 GameOver banner 视觉混淆 → Task 1 增设 `EffectDanger` 独立槽（plan Open Questions 已预设）
- 状态栏单色失去层次 → Task 1 增设 `UiAccent2` 槽

**Files:**

无（人工目视，无代码改动）

**Acceptance criteria:**

- 9 项检查清单**全部通过**
- 任何不通过项**已回归对应 Task** 调整阈值/RGB 并重新执行下游 Task
- 人工签字：visual goal 达成
- 若有调整记录在 commit message 或 task 文档对应 Task 的 implementation note 中

---

### Task 7: F1-defensive commit + brief 引用

**Status:** TODO

**Depends on:** Task 6

把全部代码改动 commit（建议每 Phase 单独 commit 便于 revert：Task 1+2 = Phase A，Task 3+4+5 = Phase B，Task 6 调整 = 后置修补）。Commit message 必须包含 F1-defensive narrative，明确"这不是视觉打磨完成、只是第一切片"。

**Commit message 模板**（top-level commit 或 PR 描述）：

```
feat(palette): refactor Palette to Brogue 1.7.5 + decorative floor variants

This is the first slice of visual polish; font/animation/tileset pending.

- Palette.cs: 9-color CGA → 20-slot semantic (Architecture/Entity/Item/Effect/UiChrome)
- TileType: added FloorMossy + FloorCracked (purely decorative)
- Renderer: switches now exhaustive (no `_ =>` default); remembered tiles use Palette.Dim()
- Map: ScatterFloorVariants() — 10% in rooms, 1% in corridors

Refs: docs/briefs/palette-brogue-pass.md
Plan: docs/plans/palette-brogue-pass.md
```

**禁止使用**："完成视觉打磨" / "visual polish complete" / "finalized look" / "shipped Brogue style" 等终结性措辞。

**Files:**

无（仅 commit / PR 操作）

**Acceptance criteria:**

- Commit message 顶部明确包含字符串 "first slice of visual polish; font/animation/tileset pending"
- Commit body 引用 brief：`Refs: docs/briefs/palette-brogue-pass.md`
- 自检命令 `git log -1 --format=%B | grep -iE "polish|visual"` 应只匹配 "first slice" / "pending" 等限定语，**不**匹配 "complete" / "finalized" / "done" 等终结性词
- 若走 PR 流程，PR 描述含相同 F1 narrative

---

## Notes

- **Brogue 1.7.5 采样源**：GitHub Pender/brogue 仓库 1.7.5 标签；源码 `src/brogue/Globals.c` 内 `wallForeColor` / `playerInLightColor` / `magicGlyphColor` 等 `color` 结构体。Fallback：BrogueCE 仓库 / Brogue wiki / 实机截图取色
- **`Palette.Dim()` factor**：起点 0.4（即 60% 暗化）；Task 6 可调
- **每 Phase 单独 commit 建议**：Task 1+2 一个 commit（Phase A）→ Task 3+4+5 一个 commit（Phase B）→ Task 6 修补（如有）→ Task 7 是否单独 commit 取决于是否走 PR 流程。这样 `git revert` 可以按 milestone 回滚

## F1 Reminder

完成所有 7 个任务后，达成的是**视觉打磨第一切片**——不是"视觉打磨完成"。剩余切片：

- ❌ 自定义像素字体（第二切片，未来 brief）
- ❌ 动画 / 颜色抖动（第三切片，未来 brief）
- ❌ Tileset（永久搁置，与 Brogue 锚点冲突）

**不要**在 commit message / 进度汇报 / PR 描述中将本 task 文档的产出写成"视觉打磨完成"。
