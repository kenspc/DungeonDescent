# Requirement Brief: 色板重构 —— 向 Brogue 风采样 + 首版装饰地形变种

## Outcome

把当前 `src/Core/Palette.cs` 的 9 色 IBM CGA 子集替换为按 Brogue vanilla 实际配色采样的 16-24 色**语义化色板**，并通过新增 1-2 种**纯装饰性 floor variant** 让扩出来的色板真正在地图上看得到。这是 pixel-ui-rewrite 之后视觉打磨的**第一步**——做完后视觉打磨依然 incomplete，这是预期行为，不是失败。

## Scope

**In scope:**

- `src/Core/Palette.cs` 重构：从 9 个名色（White / Yellow / Green / ...）改成 16-24 个**语义色**，按 Brogue vanilla 的实际类别分组（architecture / entity-by-species / item-by-type / effect-status / ui-chrome 五组各 3-4 色）。
- 全工程 72 处 `Palette.X` 引用 audit + 重新分配（85% 集中在 `src/UI/SadConsoleRenderer.cs`，11% 散落在 `src/Entities/*` + `src/Items/Item.cs`，集中作业不是考古）。
- `src/Map/TileType.cs` 增加 1-2 个 floor variant（建议 `FloorMossy` / `FloorCracked`），**纯装饰**——同 walkable、不影响 FOV / BFS / spawn 规则。
- `src/Map/Map.cs` 生成阶段按概率（建议 5-15%）把基础 `Floor` 替换为 variant。
- `src/UI/SadConsoleRenderer.cs` 渲染新 variant（替换 glyph + 新色板里的 variant 色）。

**Out of scope:**

- 自定义像素字体（保留 SadConsole 默认字体）。
- 任何动画 / 颜色抖动 / 氛围混色（一旦做就跨入"色板 + 动画"二合一，与"不一次过大动作"约束冲突）。
- Tileset 渲染（与 (ii) Brogue 风 anchor 冲突，永久搁置）。
- Wall variant（首版只做 floor，避免影响房间形状辨识）。
- Variant 的任何 gameplay 效果（陷阱 / 增益 / 减益 一律不做）。
- Brogue 风地形丰富化（水池、火焰、苔藓块等会改变 gameplay 的元素）。

**Deferred（建议顺序，不是承诺）：**

- 自定义像素字体 —— 视觉打磨第二步。
- 动画（颜色抖动 / 攻击 flash / 平滑滚动等）—— 第三步。
- Tileset —— 永久搁置，与 Brogue 锚点冲突。

## Failure Modes

按风险高到低：

- **F1 · 把这一步当视觉打磨完成**：commit message、PR 描述、进度汇报里把"色板做完"包装成"视觉打磨完成"。这是 Discovery 中用户**显式**标出的最不可接受的失败模式——它约束的不是技术工作而是工作叙事。本 brief 的根本前提是这件事**只是第一步**。
- **F2 · 装饰 variant 被误读为有意义**：玩家看到 `Floor` vs `FloorMossy` 时本能假设有 gameplay 差异（陷阱 / 增益）。Brogue 玩家习惯"纹理是噪声"，传统 roguelike 玩家未必。需要在 variant 视觉差异度上找平衡：明显到能看到（兑现 (C) 的本意），又不能明显到像在传递语义。
- **F3 · 辨识性退化**：扩到 16-24 色后两种怪物 / 物品颜色太相近，地图上分不清。Brogue 自己也踩过这个坑。可缓解：plan 阶段对关键语义对（player vs monster、health vs poison 等）做近似色对距离 audit，必须保持高对比。
- **F4 · 变种色 vs 怪物色撞色**：装饰 floor variant 的色调与某怪物 identity 色重合，玩家误把静态地砖当成静止怪物。Brogue 处理方式是 floor variant 一律低饱和 + 低亮度——可借鉴。

**显式接受的失败（不算 failure）：**

- 视觉效果不"惊艳"。色板单独工作不可能呈现 Brogue 的完整视觉感（缺字体 + 缺动画 + 缺地形丰富）——这是首版预期。
- 丢失 IBM CGA 9 色的 retro 怀旧感——选择 (ii) Brogue 风 anchor 时已经签字接受的代价。

## The Hard Part

最难的不是色板代码，而是这三个判断：

1. **9 色 → N 色的语义分组怎么切**。分太细（30+ 色）维护成本高 + visual chaos；分太粗（12 色以下）兑现不了 Brogue 的"信息密度感"。建议 plan 阶段以 Brogue vanilla 的实际语义类别为骨架（architecture / entity-by-species / item-by-type / effect-status / ui-chrome），落地到 **16-24 色之间**。
2. **Variant 视觉差异度的旋钮位置**。要让 (C) 真正"看得到"又不能触发 F2。建议第一次实现时**偏弱**——low-contrast 色 + 相近 glyph（`.` → `,`），观察玩家直觉，不够再调强。这是个 UX 旋钮，不是工程问题；plan 阶段会演变成"做出来 → 试玩 → 调 → 再试"的反馈循环。
3. **Brogue 实际色板的采样源**。vanilla 1.7.5 / Brogue CE / Lighting Mod 三个版本配色不同。**留作 plan 阶段决策点**——hobby 节奏，可以先实验性各取一组色样本对比再选。

## Constraints

- **技术栈**：.NET 8，单一 `DungeonDescent` namespace，`<ImplicitUsings>` + `<Nullable>` 启用——与 pixel-ui-rewrite 一致。
- **依赖**：不引入新的第三方依赖。所有改动在现有 SadConsole + MonoGame 栈内完成。
- **核心兼容性**：`Game.cs` / 战斗 / 移动 / FOV / BFS 一律不动。改动集中在 `src/Core/Palette.cs`、`src/Map/TileType.cs`、`src/Map/Map.cs` 的生成逻辑、`src/UI/SadConsoleRenderer.cs`，以及 entity/item 文件里散落的 11 处 `Palette` 引用。
- **Variant 行为强约束**：`FloorMossy` 等必须与 `Floor` 在 walkable / FOV / pathfinding / spawn 资格上完全等价——通过单一 helper（如 `IsWalkable(TileType)`）保证语义统一，避免 `Type == TileType.Floor` 类型的硬比较散落。
- **时间盒**：与 pixel-ui-rewrite 对齐——hobby 节奏，无硬截止；plan 阶段切成"每个里程碑独立可停手交付"的形式。

## Context

- **位置**：是 `docs/briefs/pixel-ui-rewrite.md` 的延续——那篇把"视觉打磨"整段 deferred，本 brief 启动其中第一步。
- **视觉锚点**：(ii) Brogue 风继续作数（Discovery 中显式确认）。Tileset 路径已永久排除。
- **架构限制**：当前 `Map` 是二元 `floor / wall`（只有 stairs 是第三类）。Brogue 色板的丰富感在很大程度上靠地形丰富兑现；本 brief 不动机制层，只通过装饰 variant 打开"色板能看到"的最小入口——这是首版的姿态而非长期局限。
- **Audit scope 友好**：72 处 `Palette.X` 引用集中在 5 个文件，单文件 `SadConsoleRenderer.cs` 占 61 处——集中作业不是散落各处的考古。

## Discovery Notes

- **三档抉择**：色板任务从档 1（9-slot 等价替换）/ 档 2（扩 slot + 语义化）/ 档 3（+ 颜色抖动）三档中用户选档 2，明确放弃"先小步走"的更保守姿态，同时拒绝档 3（已踩入动画领域）。
- **(A)/(B)/(C) 抉择转向**：用户初判 (A)（色板做完先停），随后主动改为 (C)（顺手加 1-2 装饰 variant 让色板"看得到"），理由是"单单改色板不是完整的视觉打磨"。这次反转把 Outcome 从"色板落地"扩展为"色板落地且视觉上可感知"，并把 F2 引入 Failure Modes。
- **F1 的 elevated 排序**：用户对 (i) 辨识性退化 / (ii) 丢 retro 感 / (iii) 与字体不搭 三个候选 failure mode 全部否决。真正的 failure mode 是叙事层 F1——"别把色板做完包装成视觉打磨完成"。这一条 elevated 到 Failure Modes 顶部，因为它约束的不是技术工作而是工作叙事。
- **未明确决策项（plan 阶段处理）**：Brogue 版本（vanilla / CE / Lighting Mod）、variant 的具体命名与 glyph 选择、N 色的具体数字（16 / 20 / 24）、概率参数（5% / 10% / 15%）。
- **后续顺序建议**：用户未反对 deferred 三项排序为 字体 → 动画 → tileset(永久搁置)，按建议写入 Deferred。
- **anchor 一致性核查**：开头质疑 tileset 是否推翻 (ii) Brogue 锚点；用户回答"当然作数"。本 brief 据此把 tileset 列为永久搁置而非延后。
