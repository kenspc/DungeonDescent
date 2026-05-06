# 计划：色板重构 —— Brogue 风采样 + 首版装饰地形变种

## Objective

兑现 `docs/briefs/palette-brogue-pass.md` 第一切片：将 `src/Core/Palette.cs` 从 9 色 IBM CGA 子集重构为按 **Brogue vanilla 1.7.5** 采样的 **20 色语义化色板**（target 20，A1 阶段允许实战收缩到 16-22），同时新增 **2 种纯装饰 floor variant**（`FloorMossy` `,` + `FloorCracked` `'`，房间内组合 10%、走廊内组合 1%），让色板扩展真正在地图上看得到。

**显式不在范围**：自定义像素字体 / 动画 / 颜色抖动 / tileset / wall variant / variant 的 gameplay 效果 / 地形机制丰富化。

## Background

- 延续 `docs/briefs/pixel-ui-rewrite.md` deferred 段标识的"视觉打磨"工作；该 brief 显式锁定视觉锚点 (ii) Brogue 风（cell-based + 像素字体 + 精调色板），tileset 路径已永久搁置
- 上游 brief：`docs/briefs/palette-brogue-pass.md`（含完整 Failure Modes / Hard Part / Discovery Notes）
- **来自代码核查的关键非显见事实**（执行阶段不要重新质疑）：
  - **`Tile.IsWalkable` 已存在**于 `src/Map/Tile.cs:9`，实现为 `Type != TileType.Wall`。新 floor variant 自动继承 walkable + spawn 资格，**无需新 helper**
  - **`Room.Contains(Point)` 已存在**于 `src/Map/Room.cs:11-13`，可直接用于 in-room 判定
  - `Palette.X` 引用 72 处中 61 处集中在 `SadConsoleRenderer.cs`，剩余 11 处分布在 `Player.cs`、`Entity.cs`、`MonsterTemplate.cs`、`Item.cs` 共 5 个文件
  - **`SadConsoleRenderer` 的 tile rendering 是两个并行 switch 表达式**（`SadConsoleRenderer.cs:66-72` 是 remembered，`:77-83` 是 visible），且**均使用 `_ =>` 默认 arm 处理 Wall 走 `#` glyph**——意味着加新 `TileType` 不会触发 CS8509，但会让新 type 静默按 wall 字符渲染（潜在 bug 源，B2 必须修掉）

## Technical Approach

### 1. 色板结构（target 20 槽位，A1 实战可收缩 16-22）

```
Architecture: WallStone, FloorBase, FloorMossy, FloorCracked
Entity:       EntityPlayer, EntityHumanoid, EntityBeast, EntityMagical
Item:         ItemConsumable, ItemEquipment, ItemTreasure, ItemStaff
Effect:       EffectHealth, EffectPoison, EffectFire, EffectIce
UiChrome:     UiTitle, UiText, UiAccent, UiDim
```

`EntityPlayer` 锁定为 white（authentic Brogue choice，dungeon-and-heroes 主题贴合）。

### 2. 采样源

**Brogue vanilla 1.7.5**（Pender 原版仓库；`tmewett/BrogueCE` 是社区分支，仅作 fallback）。源码 `src/brogue/Globals.c` 内的 `color` 结构体定义（如 `wallForeColor`、`playerInLightColor`、`magicGlyphColor` 等）。Fallback：通过 wiki / 实机截图取色。

### 3. Variant 注入策略

- **位置**：`Map` 构造函数中，`GenerateRooms()` 之后、`PlaceStairs()` 之前调用新方法 `ScatterFloorVariants()`
- **算法**：遍历整个 `Width × Height` 格子；对每个 `TileType.Floor` cell：
  - 若 `Rooms.Any(r => r.Contains(p))`（房间内）：`_rng.NextDouble()` < 0.05 → mossy；< 0.10 → cracked
  - 否则（走廊内）：`_rng.NextDouble()` < 0.005 → mossy；< 0.010 → cracked
- **行为约束**：variant 的 walkable / FOV / spawn / pathing 行为完全继承 base floor（通过 `Tile.IsWalkable` 现有实现自动满足）

### 4. Remembered tile 着色策略

- 探索过但当前不可见的 tile 用**当前 visible 色的 dim 版本**（保留色相），而非单一 `UiDim`
- 实现：在 `Palette.cs` 添加静态 helper `Color Dim(Color c, float factor = 0.4f)`，按通道乘以 factor
- factor 起点 0.4（即 60% 暗化）；C1 阶段可调

### 5. 颜色护栏

- **Variant vs FloorBase**：每通道 RGB 差值 ≤ 30（防 F2，看得见但不像传递语义）
- **关键语义对建议距离 ≥ 80**（防 F3 / F4）：
  - `EntityPlayer` vs 任意 `Entity*`
  - `EffectHealth` vs `EffectPoison`
  - `FloorMossy` / `FloorCracked` vs 任意 `Entity*`
  - 此条作为 C1 人工核查的指引，**非硬测试**

### 6. Compile-error-driven audit

A1 重构 `Palette.cs` 后整个工程编译失败（约 60-72 errors）。利用编译器列出每个 `Palette.OldName` 的 call site，逐个分配新槽位。这保证零遗漏 + 零重复。

## Implementation Steps

每个 Phase 建议**单独 commit**，便于 `git revert` 回滚。

### Phase A: Palette refactored to 20 semantic slots

#### Step A1: 重写 `src/Core/Palette.cs`

**做什么**：

1. 删除现有 9 个 `Color` 静态字段（`White`, `Yellow`, `Green`, `Red`, `Cyan`, `Magenta`, `Blue`, `Gray`, `DarkRed`）
2. 添加 20 个语义槽位，按 5 组分组并加 `// ── Architecture ──` 等分隔注释
3. 每个 RGB 值附来源注释（如 `// Brogue 1.7.5 src/brogue/Globals.c:wallForeColor`）；fallback 截图取色亦标注
4. 添加静态 helper：

   ```csharp
   public static Color Dim(Color c, float factor = 0.4f) =>
       new((byte)(c.R * factor), (byte)(c.G * factor), (byte)(c.B * factor));
   ```

5. 文件头注释包含：采样源链接、采样日期、A1 阶段是否实战收缩了 N 及理由

**N 槽位灵活性**：若 Brogue 实际配色结构不能整齐塞进 5×4，允许收缩到 16-19 或 expand 到 21-22；任何偏离 20 的决定必须在文件头注释说明。

**Acceptance criteria**：

- `dotnet build` 在 A1 完成时**预期失败**，但**所有错误必须来自非 `Palette.cs` 文件**（即新 `Palette.cs` 自身语法 / 类型正确，其他文件因引用旧名 `Palette.White / Yellow / ...` 失败——这正是 A2 要修的）；执行者目视确认错误集合不含 `src/Core/Palette.cs:*`
- 槽位数量在 16-22 区间内，全部命名遵循语义层级（Architecture / Entity / Item / Effect / UiChrome 五组）
- 每个 RGB 值的来源行可追溯（通过注释）
- `Palette.Dim()` 方法存在且签名正确

#### Step A2: Audit + reassign all 72 `Palette.X` references

**做什么**：

1. 运行 `dotnet build`，得到全量编译错误列表
2. 对每个 `Palette.OldName` call site，按下面映射决定新语义槽位：

| 文件 | 引用数 | 默认映射规则 |
|---|---|---|
| `src/UI/SadConsoleRenderer.cs` | 61 | 见下方分类细则 |
| `src/Entities/MonsterTemplate.cs` | 4 | 按怪种类型决定：Rat=`EntityBeast`、Goblin=`EntityHumanoid`、Troll=`EntityHumanoid`（更大体型但同语义层）、Dragon=`EntityMagical` |
| `src/Items/Item.cs` | 5 | 默认色 (line 9) → `UiText`；Potion=`ItemConsumable`；Sword=`ItemEquipment`；Armor=`ItemEquipment`；GoldPile=`ItemTreasure`（注：当前 4 个 ItemType 暂不需用 `ItemStaff`，留给后续物品扩展）|
| `src/Entities/Player.cs` | 1 | → `EntityPlayer`（即 white；plan 第 1 节决定）|
| `src/Entities/Entity.cs` | 1 | 默认 fallback 色 → `UiText`（与 Item 默认一致；实际所有 `Entity` 子类都覆盖此值，故仅作 fallback）|

**`SadConsoleRenderer.cs` 61 处分类细则**（按当前行号粗分；A1 阶段实际槽名以最终落地的 16-22 槽为准，下表给出语义意图）：

| 分类 | 当前色 | 行号示例 | 新槽位 |
|---|---|---|---|
| 标题栏 (`Dungeon Descent` 横条) | `Yellow` | 20 | `UiTitle` |
| Remembered tile（exploration 但 invisible） | `Gray` | 68-71 | A2 阶段保留 `Gray` 占位；B2 阶段重写为 `Palette.Dim(Palette.WallStone/FloorBase/UiAccent)` |
| Visible tile：floor / wall | `White` | 79, 82 | `FloorBase` / `WallStone` |
| Visible tile：stairs | `Cyan` | 80-81 | `UiAccent`（plan 第 4 / B2 节统一） |
| 状态栏 HP 数值（健康） | `Green` | 99, 140 | `EffectHealth` |
| 状态栏 HP 数值（< 1/3，警示色） | `Red` | 99, 140 | `EffectHealth` 的"低 HP"对位色——优先用 `EffectPoison`（命名与语义有偏差但是当前可用槽中辨识度最高的红色；若 A1 阶段决定加独立 `EffectDanger` 槽则改用之）|
| 状态栏 ATK 标签 | `Cyan` | 102 | `UiAccent`（与 stair 同槽，可接受——状态栏 accent 不与地图 tile 同屏争夺辨识）|
| 状态栏 DEF 标签 | `Blue` | 104 | `UiAccent`（同上；如 A1 决定保留两个不同 accent 则可分开）|
| 状态栏 LV 标签 | `Magenta` | 106 | `UiAccent` |
| 状态栏 EXP 标签 | `Yellow` | 108 | `UiAccent` |
| 状态栏 G (Gold) 标签 | `Yellow` | 110 | `UiAccent` |
| 状态栏 Sc (Score) 标签 | `White` | 112 | `UiText` |
| 状态栏分隔空格 | `White` | 101/103/105/... | `UiText` |
| Hint 行（status row 1） | `Gray` | 118 | `UiDim` |
| Message log 文本 | `White` | 128 | `UiText` |
| Inventory 标题 | `Yellow` | 137 | `UiTitle` |
| Inventory body / prompt | `White` | 142, 144, 150, 160, 162, 172 | `UiText` |
| Help 标题 | `Cyan` | 178 | `UiTitle`（与 INVENTORY 标题一致；若 A1 保留差异可分开）|
| Help body | `White` | 179-190 | `UiText` |
| Help footer ("Press any key...") | `Gray` | 191 | `UiDim` |
| GameOver banner | `Red` | 202-204 | `EffectPoison`（同上 HP 警示色用法；表示"危险/失败"语义）|
| GameOver stat lines | `White` | 205-208 | `UiText` |
| GameOver footer | `Gray` | 209 | `UiDim` |
| Victory banner | `Yellow` | 218-220 | `UiTitle` |
| Victory stat lines | `White` | 221-223 | `UiText` |
| Victory footer | `Gray` | 224 | `UiDim` |

**说明**：以上"状态栏 ATK/DEF/LV/EXP/G 全部映射到 `UiAccent`"会让原先彩色的 stat 行变成单色 accent。这是有意权衡——A1 阶段把"五种 stat 各自一种颜色"视为旧 IBM CGA 风的彩虹噪声；新 Brogue 风偏好少数 accent 色。若 C1 视觉核查发现状态栏失去可读层次，A2 阶段可临时回退（在 plan Open Questions 记一笔）。

3. 替换并重新编译，迭代直至 clean
4. **不留 backward-compat alias**（如 `White → UiTitle` 别名一律不留）

**Acceptance criteria**：

- `dotnet build` 零错误零警告
- `grep -rn "Palette\." --include="*.cs" /home/kenspc/projects/DungeonDescent/ | wc -l` 仍约 72（数量级，允许 ±5）
- `dotnet run` 跑通：游戏可玩、无渲染异常；A2 后整体感觉应是"Brogue 调"，但还没 variants
- 每个旧名 → 新名映射在 commit message 中归档
- **🚧 Independent stop point — Milestone A complete**（视觉打磨整体仍 incomplete）

### Phase B: Decorative floor variants

#### Step B1: 扩 `src/Map/TileType.cs` 加入 variant

**做什么**：

修改 `enum TileType { Wall, Floor, StairsDown, StairsUp }` 为 `enum TileType { Wall, Floor, FloorMossy, FloorCracked, StairsDown, StairsUp }`。

**Acceptance criteria**：

- `dotnet build` 零错误零警告（**注意**：因渲染器有 `_ =>` 默认 arm，新 enum 值不触发 CS8509——这是 B2 要修的潜在 bug 源）
- `Tile.IsWalkable` 表达式无需修改（`Type != TileType.Wall` 自动满足）
- `dotnet run` 跑通；地图视觉无变化（B3 才让 variant 出现，B2 才让 variant 显示对的字符）

#### Step B2: 扩 `src/UI/SadConsoleRenderer.cs` 两个 switch 表达式

**做什么**：

1. **重构两个 switch 移除 `_ =>` 默认 arm**，改为显式列出所有 `TileType` case，让编译器在以后新增 enum 值时强制提示（CS8509）
2. **Visible 分支**（`SadConsoleRenderer.cs:77-83`）：

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

3. **Remembered 分支**（`SadConsoleRenderer.cs:66-72`）：用 `Palette.Dim(...)` 保留色相但暗化

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

4. **StairsDown/StairsUp 着色**：用 `UiAccent`（替代当前的 `Cyan`），保持楼梯仍醒目；A2 阶段如选择不同槽位需更新此处

**Acceptance criteria**：

- `dotnet build` 零错误零警告
- 两个 switch 表达式均不含 `_ =>` arm（exhaustive）
- `dotnet run` 跑通；地图视觉**仍无 variants 出现**（B3 才注入），但 wall/floor 颜色应反映新 palette
- 离开探索过的房间，目视确认记忆色为 visible 色的 dim 版本（不再是均匀灰）

#### Step B3: 在 `src/Map/Map.cs` 加入 `ScatterFloorVariants()`

**做什么**：

1. 在 `Map.cs` 私有部分添加方法：

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

2. 在 `Map(int seed)` 构造函数（`Map.cs:19`）插入调用：

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

**Acceptance criteria**：

- `dotnet build` 零错误零警告
- `dotnet run` 跑通；目视检查（人工，需 GUI）：
  - 房间内约 10% floor cell 显示 `,` 或 `'`（mossy 5%、cracked 5%，肉眼能看到散落但不密集）
  - 走廊内非常稀疏（约 1%），偶尔在长走廊见到 1 颗 variant
  - Variants 在 visible 区显示自身色调，在 remembered 区显示 `Dim()` 暗化版
  - 玩家可走过 variants（无消息、无停顿）
  - 怪物可走过 variants（BFS pathing 不绕开）
  - 物品可生成在 variants 上
  - 上下楼后新地图重新 scatter（不复用旧分布）
- **🚧 Independent stop point — Milestone B complete**：brief 第一切片交付完成

### Phase C: Verification + commit narrative

#### Step C1: 人工目视核查清单（manual, requires GUI display）

> ⚠️ Claude 实现者**不能自动跑此步骤**——需要人工启动游戏并观察。Claude 应在 B3 完成后停手交人。

跑一局完整游戏（楼层 1 → 楼层 5+），逐项核查：

| 检查项 | 通过条件 |
|---|---|
| Palette 整体感 | 暗背景 + 微妙 Brogue 风；不是 IBM CGA 的高对比 |
| Variant 房间密度 | 每个房间约 10% floor 是 `,` / `'`；目测不会让人误以为是道具或怪 |
| Variant 走廊密度 | 走廊大部分 `.`；偶尔（每 ~100 cell 中约 1 颗）有 variant |
| 关键语义对辨识 | Player vs 同房间 monster 远看 ≥ 80 RGB 距离感；health 状态 vs poison 状态远看分得清 |
| Variant 撞 monster 色 | 巡视 floor 1-5 实际遭遇怪种类，确认无 variant 色与 monster 色"远看相似" |
| Walkable 一致性 | 走过 `.` `,` `'` 的速度 / 消息提示完全相同 |
| FOV 一致性 | Variants 进入 / 离开 FOV 时与 base floor 行为一致 |
| 记忆色分层 | 探索过但当前不可见的 tile 颜色为 visible 色的暗版本（不是单一灰）；可识别 mossy / cracked / wall / floor 区别 |

**若任何检查项不过**：

- 视觉密度不对 → 调 B3 中 0.05 / 0.10 / 0.005 / 0.010 阈值
- Variant 撞色 → 调 A1 中 variant 槽位 RGB
- 记忆色太暗或太亮 → 调 `Palette.Dim()` 的 factor 默认值

#### Step C2: Commit / PR with F1-defensive narrative

**做什么**：

- Commit message 顶部明确包含："first slice of visual polish; font/animation/tileset pending"
- PR 描述（如走 PR 流程）链接到 brief，明确这是 brief 的第一切片
- **禁止使用**："完成视觉打磨" / "visual polish complete" / "finalized look" / "shipped Brogue style" 等措辞

**Acceptance criteria**：

- Commit message 自检：`grep -i "polish\|visual" <commit-message>` 应只匹配 "first slice" / "pending" 等限定语
- Brief 在 commit body 中被引用（如 `Refs: docs/briefs/palette-brogue-pass.md`）

## Risks and Mitigations

| Risk | Source | Mitigation |
|---|---|---|
| **F1** 把第一切片当视觉打磨完成 | Brief Failure Mode #1 | C2 commit narrative 强制；plan 末尾再 reminder |
| **F2** Variant 被误读为 gameplay 信号 | Brief Failure Mode #2 | 颜色护栏 ≤ 30 RGB delta vs FloorBase；低视觉重量 glyph (`,` `'`)；命名不暗示 gameplay (`FloorMossy` 而非 `FloorTrap`) |
| **F3** 16-24 色辨识性退化 | Brief Failure Mode #3 | C1 关键语义对人工核查（Player vs Entity\* 等） |
| **F4** Variant 撞 monster 色 | Brief Failure Mode #4 | C1 巡视实际遭遇怪核查；variant 色用低饱和低亮度（贴近 FloorBase） |
| **取色源不可达** | Plan-side risk | Brogue vanilla 1.7.5 源码可访问；fallback wiki 截图取色 |
| **20 槽位实战不合身** | Plan-side risk | A1 允许收缩到 16-19 或 expand 21-22，决定记入文件头注释 |
| **B1 后默认 arm 静默渲染 variant 为 wall** | Code review during planning | B2 重构移除 `_ =>` arm；执行顺序严格 B1 → B2 → B3，不可乱序 |
| **`Dim` factor 0.4 太暗或太亮** | Plan-side risk | C1 调试可改默认值；factor 是显式参数 |
| **走廊 1% variant 影响 F2** | Decision residual | 走廊 variant 比房间稀 10×，加上低饱和色，F2 实际风险更低 |
| **`EffectPoison` 兼任"低 HP / 死亡"警示色**（命名 vs 用途偏差） | Plan-side risk | 当前 20 槽位无独立 `EffectDanger`；A1 阶段如发现 Brogue 实际配色里有合适的"暗红/血红"语义色，可在 5×4 框架内将 `EffectPoison` 改名为更通用的 `EffectDanger`；C1 阶段如目视到混淆（玩家看到红 HP 误以为中毒），A2 阶段可加 `EffectDanger` 独立槽（总数 21）|
| **状态栏 ATK/DEF/LV/EXP/G 五色 → `UiAccent` 单色后失去可读层次** | A2 audit table 偏好简化 | C1 视觉核查项；如失败，A2 阶段允许在 UiChrome 组内增设 `UiAccent2` 等差异化 accent 槽（总数 21-22 仍在容差内）|

## Open Questions

无（三个原 open questions 已在 Discovery 收尾解决：走廊 1%、记忆色分层、`EntityPlayer` = white）。

执行阶段可能浮出的新疑点（不阻塞 plan）：

- **B2 中 `StairsDown` / `StairsUp` 用 `UiAccent`**：当前用 `Cyan`（已被弃名），plan 默认改 `UiAccent`。若 A2 阶段决定 stairs 应有独立槽位（如 Effect 组里加 `EffectStairs`），更新此处映射
- **`Palette.Dim()` factor**：起点 0.4，C1 调试
- **`EffectPoison` 槽位是否改名为 `EffectDanger`**：A2 audit table 把"低 HP 警示色"和"GameOver banner"也归到 `EffectPoison`，此用法与 poison status 语义略有偏差；A1 阶段确定槽位时如发现 Brogue 配色里区分"中毒色"和"血红色"，建议改名 `EffectDanger` 或新增独立槽位（详见 Risks 表"EffectPoison 兼任"行）
- **状态栏 stat 标签同色后是否需要 `UiAccent2`**：C1 视觉核查项；详见 Risks 表对应行

## Final Reminder（防 F1）

**这个 plan 完成的不是"视觉打磨"，而是"视觉打磨第一切片"。** 完成后：

- ✅ 色板从 9 → 16-22 (target 20) 槽位，按 Brogue 1.7.5 采样
- ✅ 2 个装饰 floor variant（房间 10% / 走廊 1%）
- ✅ Remembered tile 分层暗化
- ❌ 自定义像素字体（第二切片）
- ❌ 动画 / 颜色抖动（第三切片）
- ❌ Tileset（永久搁置）

不要在 commit message / 进度汇报 / PR 描述中将本 plan 的产出写成"视觉打磨完成"。
