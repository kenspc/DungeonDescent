# Requirement Brief: 像素风 UI 重写

## Outcome

把 DungeonDescent 的渲染层从 `System.Console` 直绘替换成 SadConsole（MonoGame 之上的 cell-grid 渲染框架），目标是**新架构落地并能跑通完整一局游戏**；视觉打磨不是首版交付物，留待事后迭代。

## Scope

**In scope:**
- `src/UI/Renderer.cs` 整体替换为 SadConsole 渲染
- 主循环从阻塞 `Console.ReadKey` 改成 SadConsole 的事件驱动模型
- 输入系统改造（按键映射保留当前键位）
- 首版能在窗口内玩完整一局：移动、战斗、捡物、上下楼、胜负结算
- 接受 SadConsole + MonoGame 进入 `.csproj`（打破"零第三方依赖"约束，是已接受的代价）

**Out of scope:**
- 视觉打磨：自定义像素字体烘焙、色板精调、动画、tileset、粒子效果
- 跨平台打包 / 单文件 exe 发布
- 任何游戏机制变更（`Game.cs` / `Map.cs` / 实体类 / Item 行为修改）

**Deferred:**
- 像素字体的最终选择与替换（首版用 SadConsole 默认字体）
- 色板精调
- 分享/发行相关准备（动机 (b) 的兑现）

## Failure Modes

唯一真正不可接受的失败模式：

- **半路弃坑 / 跑不起来**——Walking Skeleton（首次"开窗 + 渲染一帧 + 响应输入"端到端通路）卡住超过合理时长，是最大风险。

明确**可接受**的失败模式（用户在 Discovery 中显式宣布）：

- 学得不够深（动机 (c) 学习目标只兑现部分）
- 视觉差异不够惊艳（首版本来就只用默认字体）
- 失去 ASCII 的"想象空间"（Brogue 风的固有代价，已接受）

## The Hard Part

**最难的不是写代码，而是走通端到端最小路径（Walking Skeleton）**：第一次成功开 SadConsole 窗口 → 渲染一帧地图 → 响应一次键盘输入 → 玩家在地图上动一格。这条路径走通后，剩余工作绝大部分是把现有 `Renderer.cs` 中的 `Console.Write` 调用映射成 SadConsole 的 `cell.Glyph / cell.Foreground` 赋值——已经是熟悉的代码搬运。

**首期路径锁定为 Path B（SadConsole）**，候选与拒绝理由：

- **Path A（留在终端，Unicode 块字符 + 终端颜色）被拒绝**：(c) 学习目标产出过低（仅是更花哨的 `Console.Write`），且终端字体跨平台不可控，无法达成 (ii) Brogue 风需要的"自带像素字体"一致性。
- **Path C（裸 MonoGame / Raylib-cs）被拒绝**：工作量约 5× 于 Path B；(ii) Brogue 风需要的第一件事就是"在网格里渲染字符 cell"，而那正是 SadConsole 已经写好的部分，自己重写性价比低。
- **Path B（SadConsole）选定**：MonoGame 之上的"伪终端"框架，cell-based 渲染天然映射当前 `Map`/`Renderer` 模型；同时仍是真正的图形开发（接触 SpriteBatch、纹理、字体烘焙、内容管线），(c) 学习目标可兑现。

## Constraints

- **技术栈**：.NET 8，`<RootNamespace>DungeonDescent</RootNamespace>`，所有代码在单一 `DungeonDescent` namespace（`<ImplicitUsings>` 启用，`<Nullable>` 启用）。
- **依赖约束**：当前 `.csproj` 零第三方依赖；本项目显式打破此约束（添加 SadConsole + MonoGame），需在新文档/CLAUDE.md 中更新此前提。
- **核心兼容**：`Game.cs`（中心权威，持有 Map/Player/Monsters/Items/Floor/Status）、`src/Map/Map.cs`、`src/Entities/*`、`src/Items/*` 应保持基本不动；改动集中在 `src/UI/` 和 `Program.cs` 主循环。
- **时间盒**：未定，hobby 节奏。Plan 阶段需做成"每个里程碑独立可停手交付"的形式以适配不确定时间盒。
- **终端尺寸假设**：`Program.cs:9` 当前的 62×27 最小终端尺寸检查，迁移到窗口程序后需重新表达为窗口最小分辨率（或直接移除）。

## Context

- **动机配比**：主驱动是 (a) 自用审美 + (c) 学习驱动；(b) 可分享性占小份额。三者共同把候选路径从"留在终端"锁定到"窗口程序"。
- **视觉锚点**：(ii) **Brogue 风**——cell-based 字符 + 像素字体 + 精调色板。**明确不是** (i) Caves of Qud 那种 tileset 像素，**也不是** (iii) Stardew 那种 sprite 世界。后者会颠覆当前回合制网格游戏类型，本 brief 不进入这种 scope。
- **学习哲学**："过程就是一种学习"。SadConsole 内容管线 / 字体烘焙等操作中的踩坑时间是**产出而非损耗**，不应作为风险计入计划。
- **降期待姿态**：用户在 Discovery 中说"只要可以运行就好"，是主动把成功定义降到容易达成的位置（Walking Skeleton 模式的天然契合点），并非真的不在乎结果。

## Discovery Notes

- **路径三选一的关键决策**：(a) + (c) 动机配比下 Path A 首先被淘汰（学习产出过低、字体不可控）；剩 B vs C 之间，(ii) Brogue 风的视觉选择进一步把 Path C 推开——裸 MonoGame 第一件要写的就是"如何渲染字符网格"，而那正是 SadConsole 已经写好的部分。最终锁定 Path B。
- **视觉锚点 (ii) 的隐含意义**：保留 turn-based 网格 roguelike 游戏类型不变。若选 (iii) Stardew 风，则整个 `Program.cs` → `HandleKey` → `EndPlayerTurn` 回合循环要重写，已显式被排除。
- **"只要可以运行就好"的解读**：被解读为 (B) 降期待保护自己（fear is 弃坑）而非 (A) 真佛。用户随后用"主要是能把架构建立出来，能运行就成功一大步了。美不美可以事后修改它。过程就是一种学习。"显式确认这一解读。这是把 success criterion 锁定到"骨架能跑"而非"视觉完美"的关键证据。
- **时间盒未明确**：用户没在"周末/一两周/几个月/说不准"中显式选择，而是用更高层的"过程即学习"回答替代。Plan 阶段应将工作切成"每个里程碑独立可交付"的形式，规避时间盒缺失带来的 scope 风险。
