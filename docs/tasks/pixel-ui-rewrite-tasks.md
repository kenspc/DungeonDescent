# Pixel UI Rewrite — Task Document

## Context

把 DungeonDescent 的渲染层从 `System.Console` 直绘替换成 SadConsole 10.x + MonoGame DesktopGL，分 M0~M5 共 6 个里程碑递进。本任务文档把每个里程碑分解为可独立执行、独立验证的任务。

- **来源 plan**: [`docs/plans/pixel-ui-rewrite.md`](../plans/pixel-ui-rewrite.md)
- **来源 brief**: [`docs/briefs/pixel-ui-rewrite.md`](../briefs/pixel-ui-rewrite.md)

> 所有任务带有 `Depends on` 注释。**必须按编号顺序执行**——尤其是 M5（Task 12-17），打乱顺序会破坏 build（例如 Task 14 删除 `GameColors.cs` 必须发生在 Task 13 把所有 `GameColors.X` 引用替换为 `Palette.X` 之后）。

## Tasks

### Task 1: 接入 SadConsole NuGet 依赖

**Status:** DONE

在项目根目录执行 `dotnet add package SadConsole` 和 `dotnet add package SadConsole.Host.MonoGame`，确认 `DungeonDescent.csproj` 出现两个 `<PackageReference>`。运行 `dotnet build` 验证依赖链可解析。

**Implementation note:** SadConsole.Host.MonoGame 10.9.0 不再 transitively 引入 MonoGame；package readme 明确要求使用方自行添加 MonoGame 引用。已额外添加 `MonoGame.Framework.DesktopGL 3.8.4.1`，使 `MonoGame.Framework.dll` + 各平台 SDL2/OpenAL native libs 落地到 `bin/Debug/net8.0/runtimes/*`。

**Files:**
- Modify: `DungeonDescent.csproj`

**Acceptance criteria:**
- `DungeonDescent.csproj` 内出现 `<PackageReference Include="SadConsole" Version="..." />` 与 `<PackageReference Include="SadConsole.Host.MonoGame" Version="..." />`
- `dotnet build` 退出码 0，零警告（项目 `<Nullable>enable</Nullable> 已启用，与 plan M0 验收一致）
- `bin/Debug/net8.0/` 下出现 `SadConsole.dll`、`SadConsole.Host.MonoGame.dll`、`MonoGame.Framework.dll`、`SDL2.dll`、`libSDL2.so`（或 Windows 端 `SDL2.dll`）
- 旧版游戏 `dotnet run` 仍能正常进入并游玩（旧 `Renderer` 未受影响）
- `dotnet list package` 显示真实拉取的 SadConsole 版本号并写回 `.csproj` 的 `PackageReference Version`（用于在 Risks 中追踪、防止后续未审升级）

---

### Task 2: Walking Skeleton — 最小 SadConsole host

**Status:** DONE

**Implementation note:** SadConsole 10.9.0 不再提供 `Game.Instance.OnStart` 属性赋值的旧 API；改用 `Game.Create(width, height, EventHandler<GameHost> startingEventHandler)` 重载。键盘退出 hook 使用 `SadConsole.Quick.Keyboard.WithKeyboard` 扩展方法。`screen.IsFocused = true` 确保 hook 接收到按键。诊断运行显示 STARTUP → Before Run → OnStart fired → After Run → After Dispose 流程完整，进程 exit code 0、无 stderr。视觉部分（GUI 窗口出现 + 居中黄色 `@` + 灰色提示）DEFERRED — needs human visual verification（per custom instruction #3）。

**Depends on:** Task 1

**完全替换** `Program.cs` 内容为最小 SadConsole host，开窗显示一个居中的黄色 `@` 字符 + 灰色提示文字 "Press any key to exit"。**不接入 Game 逻辑**——本任务的目的仅是验证整条 GUI 工具链端到端可用。代码模板见 plan M1 步骤 2。注意 `using SadGame = SadConsole.Game` 别名以避免与 `DungeonDescent.Game` 冲突。

**Files:**
- Modify: `Program.cs`（整体替换为约 15 行）

**Acceptance criteria:**
- 在 WSL2 内执行 `dotnet run`，弹出 GUI 窗口，标题为 "Dungeon Descent"
- 窗口中显示居中黄色 `@` 字符与灰色 "Press any key to exit" 提示
- 按任意键关闭窗口，进程正常退出（exit code 0）
- 若启动失败，按 plan M1 步骤 4 的排查清单（`glxinfo`、`SDL_VIDEODRIVER=x11`、`dotnet --info`）定位并解决

---

### Task 3: 创建 `Palette` + `SadConsoleRenderer` (含 AnsiToColor 适配)

**Status:** DONE

**Depends on:** Task 2

新建两个文件：(a) `src/UI/Palette.cs` 静态类，把 9 个前景色按 IBM CGA 16 色调色板映射为 `SadRogue.Primitives.Color` 常量（具体值见 plan M2 步骤 2 的代码块）；(b) `src/UI/SadConsoleRenderer.cs` 静态类，包含 `RenderAll(Game game, IScreenSurface surface)`、`RenderMap`、`DrawTitle`、`DrawStatusBar`、`DrawMessageLog`、`DrawInventory`、`DrawHelp`、`DrawGameOver`、`DrawVictory` 等方法（结构对照旧 `src/UI/Renderer.cs`）。`RenderAll` 入口必须先调用 `surface.Surface.Clear()` 再分别绘制各区域，模拟旧 `Console.Clear()`。新建私有方法 `Color AnsiToColor(string ansi)` 把 9 个前景 ANSI 字符串（White / Yellow / Green / Red / Cyan / Magenta / Blue / Gray / DarkRed）反向映射成 `Palette` 常量；`Reset` / `Bold` / `BgBlack` 不参与映射。复制旧 title 字符串 "Dungeon Descent — Floor X/5" 时把 `—` 改为 ASCII `-`（CP437 兼容）。**不修改** 实体类 `Color` 字段（仍是 string）。

**Files:**
- Create: `src/UI/Palette.cs`
- Create: `src/UI/SadConsoleRenderer.cs`

**Acceptance criteria:**
- `dotnet build` 通过，零警告
- `Palette` 包含 9 个 `static readonly Color` 字段，命名与值与 plan M2 步骤 2 完全一致
- `SadConsoleRenderer.RenderAll` 第一行调用 `surface.Surface.Clear()`
- `SadConsoleRenderer.AnsiToColor` 对 9 个前景色字符串各有对应分支返回 `Palette` 常量
- `SadConsoleRenderer` 内 title 字符串使用 ASCII `-` 而非 Unicode `—`
- 旧 `src/UI/Renderer.cs` 与 `src/Core/GameColors.cs` 未改动

---

### Task 4: 把 `Program.cs` 接通 `SadConsoleRenderer`

**Status:** DONE

**Note:** Visual acceptance (full game frame visible, FOV correct, color separation between Goblin green / stairs cyan, etc.) DEFERRED — needs human visual verification (per custom instruction #3).

**Depends on:** Task 3

在 Task 2 的 host 基础上，于 `OnStart` 闭包内构造 `var game = new DungeonDescent.Game();`（注意命名空间消歧，避免与 `SadConsole.Game` 冲突），并调用 `SadConsoleRenderer.RenderAll(game, screen)`。`screen` 仍是 `Console(80, 30)` 局部变量（M3 才换成 60×26）。**仍不处理输入**——保留 Task 2 的"按任意键退出"行为。

**Files:**
- Modify: `Program.cs`

**Acceptance criteria:**
- `dotnet run` 弹出 80×30 窗口，**一帧渲染** 完整游戏画面：标题栏 + 60×20 地图（玩家 `@`、墙 `#`、地板 `.`、楼梯 `>` `<`、怪物 `r` `g` `T` `D`、物品 `!` `+` `[` `$`）+ 状态栏（HP/ATK/DEF/LV/EXP/Gold/Score 一行 + 按键提示一行）+ 消息日志（3 行）
- FOV 正确：未探索区域空白；可见区域亮色（floor 白、stairs cyan、player 黄等）；已探索未可见区域灰色
- 视觉与旧 console 版对比：所有 glyph 字符与坐标位置一致；颜色允许 RGB ↔ ANSI palette 略有偏移，但每种颜色必须能与其他颜色明确区分（例如 Goblin 绿与 stairs cyan 不可视觉混淆）
- 按任意键正常关闭窗口

---

### Task 5: 创建 `SadConsoleKeyAdapter`（按键映射表）

**Status:** DONE

**Implementation note:** SadConsole 10.9.0 exposes its own `SadConsole.Input.Keys` enum (mirroring `Microsoft.Xna.Framework.Input.Keys`); `AsciiKey.Key` is `SadConsole.Input.Keys`, so the switch is keyed on that type. WASD letter keys map to `KeyChar='w'..'d'` (lowercase) so `char.ToLower(...)` in `Game.HandleKey` is a no-op. Arrow keys deliberately use `KeyChar='\0'` since `Game.HandleKey` matches them on `Key`, not `KeyChar`.

**Depends on:** Task 4

新建 `src/UI/SadConsoleKeyAdapter.cs`，包含静态方法 `static ConsoleKeyInfo? ToConsoleKeyInfo(AsciiKey key)`，把 SadConsole 的 `AsciiKey`（来自 `keyboard.KeysPressed`）转成 `System.ConsoleKeyInfo`。映射要点：

- 字母键：`W` `A` `S` `D` `Q` `I`
- 方向键：`Up` `Down` `Left` `Right`
- Shift 修饰键：`>` (Shift + `Keys.OemPeriod`)、`<` (Shift + `Keys.OemComma`)、`?` (Shift + `Keys.OemQuestion`)；adapter 需读取 `keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift)` 判定 shift 状态——因此签名应改为 `ToConsoleKeyInfo(AsciiKey key, Keyboard keyboard)`
- 字符键：`.` (`Keys.OemPeriod` 不带 shift)、`Escape`、数字 `1`-`9`

**关键契约**：返回的 `ConsoleKeyInfo` 必须同时填充 `Key` 字段（识别 `ConsoleKey.UpArrow`、`ConsoleKey.D1` 等）和 `KeyChar` 字段（识别 `>`、`<`、`.`、`1`-`9` 等字符）。`Game.HandleKey` 同时使用两者。例：`new ConsoleKeyInfo('>', ConsoleKey.OemPeriod, shift: true, alt: false, control: false)`。

**Files:**
- Create: `src/UI/SadConsoleKeyAdapter.cs`

**Acceptance criteria:**
- `dotnet build` 通过
- `ToConsoleKeyInfo` 覆盖以下按键：WASD、4 个方向键、`q`、`i`、`>`、`<`、`?`、`.`、Escape、`1`-`9`
- 每条返回值的 `Key` 与 `KeyChar` 都正确填充（例如 `>` 返回 `KeyChar='>', Key=ConsoleKey.OemPeriod, shift=true`）
- 未识别的按键返回 `null`
- Adapter 内部用静态字典或 switch 实现，便于后续扩展

---

### Task 6: 创建 `GameSurface`（事件驱动渲染封装）

**Status:** DONE

**Depends on:** Task 5

新建 `src/UI/GameSurface.cs` 位于 `DungeonDescent` namespace，继承 `SadConsole.Console`。构造函数接受 `Game game` 参数，调用 `: base(60, 26)`；构造时立即调用一次 `Refresh()`。覆盖 `ProcessKeyboard(Keyboard keyboard)`：

- 遍历 `keyboard.KeysPressed`
- 拦截 `Keys.Q` → 调 `SadConsole.Game.Instance.MonoGameInstance.Exit()` 并返回 `true`
- 其他键调 `SadConsoleKeyAdapter.ToConsoleKeyInfo(key, keyboard)`，若返回非 null 则调 `_game.HandleKey(info.Value)` + `Refresh()`，返回 `true`
- 未识别按键返回 `false`

私有 `Refresh()` 方法调用 `SadConsoleRenderer.RenderAll(_game, this)`。

**Files:**
- Create: `src/UI/GameSurface.cs`

**Acceptance criteria:**
- `dotnet build` 通过
- `GameSurface` 是 `DungeonDescent` namespace 下的 `SadConsole.Console` 子类
- `ProcessKeyboard` 显式拦截 `Keys.Q` 退出；其他键通过 adapter 转 `ConsoleKeyInfo`
- `Refresh` 调用 `SadConsoleRenderer.RenderAll(_game, this)`
- 构造函数立即调用 `Refresh()` 渲染初始帧
- 对 `Keys.I` `Keys.OemQuestion` 等 M4 才支持的键，可不做特殊处理（让 adapter 返回的 `ConsoleKeyInfo` 落入 `Game.HandleKey` 的默认分支被忽略）

---

### Task 7: 切 `Program.cs` 到事件驱动 + 窗口尺寸 60×26

**Status:** DONE

**Note:** Background run confirms the event-driven loop now stays alive past 4 seconds (exit code 143 from SIGTERM, no stderr) — previously M2/M4 builds returned `SadGame.Instance.Run()` immediately. The interactive acceptance points (WASD movement, combat, stair transitions, item pickup, `q` exits cleanly) DEFERRED — needs human visual verification (per custom instruction #3).

**Depends on:** Task 6

修改 `Program.cs`：移除 Task 4 的"渲染一次就停"代码；改为构造 `var game = new DungeonDescent.Game();` + `var surface = new GameSurface(game);` + `SadGame.Instance.Screen = surface;`。同步把 `SadGame.Create(80, 30)` 改为 `SadGame.Create(60, 26)`（与 `GameSurface(60, 26)` 等大）。`SadGame.Instance.Run()` 由 SadConsole 主循环驱动，自动调度 `ProcessKeyboard`。

**Files:**
- Modify: `Program.cs`

**Acceptance criteria:**
- `dotnet run` 弹出 60×26 cell 大小的窗口（无大块黑边）
- 玩家按 WASD / 方向键能在地图上移动，每次按键后地图实时刷新
- 撞怪触发战斗，怪物在玩家结束回合后正常移动
- 楼梯 `>` `<` 切换楼层正常工作（先后下降到 floor 5、再上升到 floor 1 触发胜利）
- 拾取物品自动入库存（按 `i` 此时无效，但 `Game.Player.Inventory` 内可观察到）
- 按 `q` 关闭窗口，进程正常退出
- 按 `i` `?` 暂时无可见反应（M4 处理）

---

### Task 8: `RootScreen` + 4-surface 拆分

**Status:** DONE

**Implementation note:** SadConsoleRenderer's old monolithic `RenderAll(game, surface)` method is replaced by four `Render{Title,Map,Status,Log}` methods that each target a dedicated 0,0-origin surface. Map rendering no longer applies a `+1` Y-offset because the map surface itself is positioned at (0, 1). `GameSurface.cs` is deleted (was unused after Program.cs switched to RootScreen, and would have a stale RenderAll dependency). Visual portion (4 regions correctly placed, layout identical to Task 7, gameplay regression-free) DEFERRED — needs human visual verification.

**Depends on:** Task 7

新建 `src/UI/RootScreen.cs`，继承 `ScreenObject`。在 `RootScreen` 内创建 4 个子 surface 并加入 `Children`：

- `_titleSurface`：60×1，position (0,0)
- `_mapSurface`：60×20，position (0,1)
- `_statusSurface`：60×2，position (0,21)
- `_logSurface`：60×3，position (0,23)

把 Task 7 中 `Program.cs` 对 `GameSurface` 的引用替换为 `RootScreen`。同时拆分 `SadConsoleRenderer.RenderAll(game, surface)` 为多个面向具体 surface 的方法：`RenderTitle(game, titleSurface)`、`RenderMap(game, mapSurface)`、`RenderStatus(game, statusSurface)`、`RenderLog(game, logSurface)`。`RootScreen` 维护对 `Game` 的引用，提供 `Refresh()` 方法把 4 个 surface 都重绘一遍。`ProcessKeyboard` 逻辑（含 `q` 退出、key adapter 调用）从 `GameSurface` 迁移到 `RootScreen`，并在每次按键后调 `Refresh()`。

`GameSurface` 在本任务后可保留也可删除——由实施时决定，但 `Program.cs` 不再使用它。

**Files:**
- Create: `src/UI/RootScreen.cs`
- Modify: `src/UI/SadConsoleRenderer.cs`（拆分渲染方法）
- Modify: `Program.cs`（用 RootScreen 替换 GameSurface）

**Acceptance criteria:**
- `dotnet run` 显示 60×26 窗口，4 个区域正确就位（标题在最上、地图占中间 20 行、状态 2 行、日志 3 行在最下）
- 视觉上与 Task 7 的输出无差别（仅内部 surface 结构不同）
- 所有 Task 7 验收过的玩法（移动、战斗、楼梯、拾取、`q` 退出）继续通过
- HP < `MaxHp / 3` 时数字仍显示红色

---

### Task 9: `InventoryScreen` + Overlay 切屏机制

**Status:** DONE

**Implementation note:** `RootScreen.ProcessKeyboard` short-circuits to `false` when an overlay is up, letting SadConsole route the input to the focused overlay child. `Inventory.Count > 0 && index < Inventory.Count` gates digit presses so empty / out-of-bounds slots don't burn a turn. Visual portion (overlay swap, HP red color when low, "(empty)" message, etc.) DEFERRED — needs human visual verification.

**Depends on:** Task 8

新建 `src/UI/InventoryScreen.cs` 作为 `ScreenObject` 子类，60×26 大小，渲染逻辑参考旧 `Renderer.DrawInventory`（标题 "=== INVENTORY ===" + HP 行 + 物品列表 `[1] {glyph} {Name}` + 提示）。在 `RootScreen` 中实现切屏机制：

- 私有字段 `_currentOverlay : ScreenObject?`、`_gameSurfaces : List<ScreenObject>`（持有 4 个 game 子 surface）
- `OpenOverlay(ScreenObject overlay)`：把 4 个 game surface 的 `IsVisible = false`，把 `overlay` 加入 `Children`，设 `_currentOverlay = overlay`
- `CloseOverlay()`：把 4 个 game surface 的 `IsVisible = true`，从 `Children` 移除 `_currentOverlay`，设 `_currentOverlay = null`
- `ProcessKeyboard` 中：若 `_currentOverlay == null` 且按下 `i`，调 `OpenOverlay(new InventoryScreen(_game, this))`；inventory 屏自身的 `ProcessKeyboard` 处理 `Esc`（调 `_root.CloseOverlay()`）和 `1`-`9`（调 `_game.UseInventoryItem(index)` + `_game.Log.Add(msg)` + `_game.EndPlayerTurn()` + `_root.Refresh()`）

**Files:**
- Create: `src/UI/InventoryScreen.cs`
- Modify: `src/UI/RootScreen.cs`（添加 OpenOverlay/CloseOverlay/_currentOverlay 字段）

**Acceptance criteria:**
- 在主屏按 `i` 进入 inventory 屏：4 个 game surface 隐藏，inventory 屏可见
- Inventory 屏正确显示当前 HP（含低于 1/3 时红色规则）、carry 计数、物品列表
- 按 `1`-`9` 使用对应槽位物品：触发 `Game.UseInventoryItem` + log 更新 + `EndPlayerTurn`，画面返回主屏
- 按 `Esc` 直接返回主屏不消耗回合
- 物品列表为空时显示 "(empty)" 且按任意键不进入 use 流程
- inventory 行为与旧 console 版完全一致

---

### Task 10: `HelpScreen` overlay

**Status:** DONE

**Note:** Visual portion (help screen content correct, returns on any key, doesn't burn turn) DEFERRED — needs human visual verification.

**Depends on:** Task 9

新建 `src/UI/HelpScreen.cs` 作为 `ScreenObject` 子类，60×26 大小，渲染逻辑参考旧 `Renderer.DrawHelp`（标题 "=== HELP ===" + 移动键/楼梯/库存/退出/地图符号说明 + "Press any key to return..."）。在 `RootScreen.ProcessKeyboard` 中：若按下 `?` 且 `_currentOverlay == null`，调 `OpenOverlay(new HelpScreen(this))`。HelpScreen 自身 `ProcessKeyboard` 处理任意按键 → 调 `_root.CloseOverlay()`。

**Files:**
- Create: `src/UI/HelpScreen.cs`
- Modify: `src/UI/RootScreen.cs`（在 ProcessKeyboard 中添加 `?` 处理分支）

**Acceptance criteria:**
- 在主屏按 `?` 进入 help 屏，4 个 game surface 隐藏
- Help 屏显示完整按键说明与地图符号说明（与旧 `DrawHelp` 内容一致）
- 按任意键返回主屏，不消耗回合
- Help 屏内不会触发任何游戏行为

---

### Task 11: `GameOverScreen` + `VictoryScreen` + Status 检测

**Status:** DONE

**Implementation note:** `RootScreen.Update(TimeSpan)` polls `_game.Status` each frame; when it transitions to `Dead` or `Won`, the matching end overlay is opened (replacing any current overlay). End screens treat any keypress as exit-process. Visual portion (death triggers GameOver with correct numbers, win triggers Victory, exit code 0 on key) DEFERRED — needs human visual verification.

**Depends on:** Task 9

新建两个 ScreenObject 子类：

- `src/UI/GameOverScreen.cs`：渲染 "YOU DIED" 框 + Floor/Level/Gold/Final Score 行（参考旧 `DrawGameOver`）
- `src/UI/VictoryScreen.cs`：渲染 "YOU ESCAPED THE DUNGEON!" 框 + Level/Gold/Final Score 行（参考旧 `DrawVictory`）

在 `RootScreen` 中覆盖 `Update(TimeSpan delta)`：检查 `_game.Status`：

- 若 `Dead` 且 `_currentOverlay` 不是 GameOverScreen → 调 `OpenOverlay(new GameOverScreen(_game, this))`
- 若 `Won` 且 `_currentOverlay` 不是 VictoryScreen → 调 `OpenOverlay(new VictoryScreen(_game, this))`

GameOverScreen / VictoryScreen 的 `ProcessKeyboard` 收到任意按键 → 调 `SadConsole.Game.Instance.MonoGameInstance.Exit()` 直接退出进程（不返回主屏，与旧版语义一致）。

**Files:**
- Create: `src/UI/GameOverScreen.cs`
- Create: `src/UI/VictoryScreen.cs`
- Modify: `src/UI/RootScreen.cs`（添加 Update 方法）

**Acceptance criteria:**
- 玩家死亡（HP 归零，`Game.Status == Dead`）：自动显示 GameOver overlay 含正确的 Floor/Level/Gold/Final Score
- 玩家从 floor 1 上楼（或杀 Dragon）触发胜利（`Game.Status == Won`）：显示 Victory overlay 含正确的 Level/Gold/Final Score
- 任一结算屏按任意键退出进程，exit code 0
- 触发结算后即使再按其他键也不会回到游戏（status 不可逆）
- M4 的所有验收（移动、战斗、楼梯、拾取、inventory、help、game over、victory）通过 = brief In Scope 全部交付

---

### Task 12: 移动 `Palette` 到 `src/Core/`

**Status:** DONE

**Depends on:** Task 11

把 `src/UI/Palette.cs` 移到 `src/Core/Palette.cs`（内容不变）。更新所有引用 `Palette` 的代码——目前仅 `src/UI/SadConsoleRenderer.cs`。所有命名空间仍是 `DungeonDescent`，因此 import 不需要 using 语句改动；但物理路径变了，需要 `git mv`。

此移动是为 Task 13 做准备：实体类（位于 `src/Entities/`、`src/Items/`）不应反向依赖 UI 层，所以 `Palette` 必须在 `src/Core/`（已被 entity 隐式可用，因为同 namespace）。

**Files:**
- Move: `src/UI/Palette.cs` → `src/Core/Palette.cs`
- Modify: 无（namespace 一致，引用代码无需改动）

**Acceptance criteria:**
- `src/Core/Palette.cs` 存在，内容与原 `src/UI/Palette.cs` 完全一致
- `src/UI/Palette.cs` 不存在
- `dotnet build` 通过
- `git log --follow src/Core/Palette.cs` 应显示原文件历史（用 `git mv` 而非删 + 新建）

---

### Task 13: 实体 `Color` 字段改 `SadRogue.Primitives.Color` 类型 + 删 `AnsiToColor`

**Status:** DONE

**Note:** Old `src/UI/Renderer.cs` and `src/Core/GameColors.cs` still compile after this change (Renderer.cs only uses `entity.Color` inside string interpolation, which falls back to `Color.ToString()`; the ANSI escape constants are still valid `string` literals in GameColors.cs). They are dead code at this point - Task 14 deletes both.

**Depends on:** Task 12

把以下字段的类型从 `string` 改为 `SadRogue.Primitives.Color`，并把所有初始化点的 `GameColors.X` 引用替换为 `Palette.X`：

- `src/Entities/Entity.cs`：`public string Color { get; init; } = GameColors.White;` → `public Color Color { get; init; } = Palette.White;`
- `src/Entities/Player.cs`：构造函数 `Color = GameColors.Yellow` → `Color = Palette.Yellow`
- `src/Entities/MonsterTemplate.cs`：record 第三参数 `string Color` → `Color Color`；`MonsterTemplates.Rat/Goblin/Troll/Dragon` 4 处 `GameColors.X` 替换为 `Palette.X`
- `src/Items/Item.cs`：`public string Color` → `public Color Color`；`Item.Potion/Sword/Armor/GoldPile` 4 处 `GameColors.X` 替换为 `Palette.X`
- `src/UI/SadConsoleRenderer.cs`：移除私有 `AnsiToColor` 方法；所有调用点（如 `AnsiToColor(player.Color)`）直接使用 `player.Color`、`monster.Color`、`item.Color`、`tile.Color` 等

`PositionedItem` 构造函数中 `Color = source.Color` 不需修改（类型已经统一）。

**Files:**
- Modify: `src/Entities/Entity.cs`
- Modify: `src/Entities/Player.cs`
- Modify: `src/Entities/MonsterTemplate.cs`
- Modify: `src/Items/Item.cs`
- Modify: `src/UI/SadConsoleRenderer.cs`

**Acceptance criteria:**
- `dotnet build` 通过，零警告
- `Entity.Color`、`Player.Color`、`MonsterTemplate.Color`、`Item.Color` 类型为 `SadRogue.Primitives.Color`
- 实体类初始化全部使用 `Palette.X` 引用，不再有 `GameColors.X`
- `SadConsoleRenderer.AnsiToColor` 方法被删除
- `SadConsoleRenderer` 渲染时直接读取 `entity.Color` / `item.Color` / `tile.Color` 当作 `Color` 类型使用
- `dotnet run` 完整跑一局（移动、战斗、楼梯、拾取、inventory、help、死亡或胜利）功能不变

---

### Task 14: 删除 `Renderer.cs` 与 `GameColors.cs`

**Status:** DONE

**Note:** Also removed the now-stale "GameColors.cs" reference from the comment in `src/Core/Palette.cs` so that the grep contract truly returns zero hits.

**Depends on:** Task 13

执行 `git rm src/UI/Renderer.cs` 与 `git rm src/Core/GameColors.cs`。两个文件在 Task 13 之后已经无引用——`Renderer.cs` 在 Task 4 之后就未被调用、`GameColors.cs` 在 Task 13 之后无任何引用。

**Files:**
- Delete: `src/UI/Renderer.cs`
- Delete: `src/Core/GameColors.cs`

**Acceptance criteria:**
- 两个文件不在仓库中
- `dotnet build` 通过，零警告
- 验证 grep 命令零命中：
  ```bash
  grep -rn "Console\.Write\|Console\.SetCursorPosition\|Console\.Clear\|GameColors\|\\\\x1b\[" src/ Program.cs
  ```
- `git status` 显示两个文件 staged for deletion
- `dotnet run` 仍然能完整跑一局

---

### Task 15: 清理 `Program.cs` 旧 Console 初始化代码

**Status:** TODO

**Depends on:** Task 14

`Program.cs` 顶部仍有从原始 console 应用残留的初始化代码（在 Task 2 替换 Program.cs 时被丢弃，但若其他任务过程中有意外恢复，需在此清理）。检查并确保删除：

- `Console.OutputEncoding = System.Text.Encoding.UTF8;`
- `Console.CursorVisible = false;`
- `Console.Title = "Dungeon Descent";`（已被 SadConsole 的 `Settings.WindowTitle` 取代）
- 62×27 终端尺寸检查（`if (Console.WindowWidth < 62 || Console.WindowHeight < 27)`）
- 进程退出处的 `Console.CursorVisible = true`（GUI 模式无意义）

**Files:**
- Modify: `Program.cs`

**Acceptance criteria:**
- `Program.cs` 内不出现 `Console.OutputEncoding`、`Console.CursorVisible`、`Console.WindowWidth`、`Console.WindowHeight`、`Console.Title` 任一引用
- `Program.cs` 不超过约 20 行
- `dotnet build` 通过
- `dotnet run` 行为不变

---

### Task 16: 更新 `CLAUDE.md` 与 `README.md`

**Status:** TODO

**Depends on:** Task 15

按 plan M5 步骤 4 与 5 修改两份文档：

`CLAUDE.md`:
- 移除 `Program.cs` 第 9 行 62×27 终端尺寸检查相关说明
- 在 Commands 段落或新增运行环境段落注明："Requires GUI environment: Windows native, WSL2 with WSLg (Win11), or Linux/macOS with display server"
- Architecture 段落："no third-party dependencies" → "depends on SadConsole 10.x + MonoGame DesktopGL"
- Architecture 中提到 `Renderer.cs` 与 `GameColors` 的部分需更新为 `SadConsoleRenderer.cs` + `Palette.cs`
- 若有提到 ANSI / 终端 / `Console.Write` 的细节，全部修正

`README.md`:
- 第一段 "No third-party libraries — pure System.Console with ANSI color rendering" → 反映 SadConsole + MonoGame DesktopGL
- "Requirements" 段：去掉 "Terminal: minimum 62 columns × 27 rows" 与 "ANSI color support"，改为 "GUI environment (Windows native / WSL2+WSLg / Linux/macOS with display server)"
- "Architecture" 段中 "No third-party libraries. Rendering uses raw ANSI escape sequences" → 重写为新事实
- 示例截图（ASCII art block）可保留作为视觉参考，但加注 "Now rendered as a GUI window"

**Files:**
- Modify: `CLAUDE.md`
- Modify: `README.md`

**Acceptance criteria:**
- `CLAUDE.md` 与 `README.md` 内不出现 "62×27"、"No third-party libraries" / "no third-party dependencies"、"ANSI escape sequences" / "ANSI color support" / "raw ANSI" 等已过期描述
- 两份文档明确说明项目现已为 GUI 应用，需要 display server
- 两份文档明确列出 SadConsole 与 MonoGame DesktopGL 为依赖
- `git diff CLAUDE.md README.md` 内容合理、无误删功能性段落

---

### Task 17: 双端运行验证

**Status:** TODO

**Depends on:** Task 16

最终回归验证（不修改代码）。

**WSL2 端验证**：在当前 WSL2 shell 内 `dotnet run`，完整玩一局直到死亡或胜利（建议两种结局都各跑一次）。观察：

- 窗口正常出现，无报错
- 全部 UI 元素显示正常
- Inventory / help overlay 正常切换
- 结算屏显示正确

**Windows native 端验证**：通过 WSL mount 路径在 Windows PowerShell 7 内执行：

```powershell
cd \\wsl$\Ubuntu\home\kenspc\projects\DungeonDescent
dotnet run
```

完整跑一局，确认窗口正常、行为与 WSL2 端一致。

**Files:** 无（纯验证步骤）

**Acceptance criteria:**
- WSL2 内至少完成一局完整游戏（从 floor 1 直到死亡或胜利），无 crash、无渲染异常
- Windows native 内至少完成一局完整游戏，无 crash、无渲染异常、行为与 WSL2 端一致
- `dotnet build` 在两端均零警告
- 至此 brief 中所有 In Scope 项目交付完成；视觉打磨进入下一迭代

---

## Notes

- **M5 顺序刚性约束**：Task 12 → 13 → 14 顺序不可打乱。Task 13 在前面任务删除 `GameColors.cs` 之前必须把所有 `GameColors.X` 引用替换为 `Palette.X`，否则 build 会失败。
- **Task 13 是最大块（5 文件 type cascade）**，原则上仍可拆但任意中间状态都会破坏 build——保持原子操作，在一个 commit 内完成。
- **Task 10 与 Task 11 互不依赖**（仅都依赖 Task 9），实施时可串行也可并行；本文档按编号排列以保持线性可读。
- **何时停手契约**：每个 milestone 末尾的 task（Task 1 / 2 / 4 / 7 / 11 / 17）通过即可"停手交付"——项目处于一致可玩状态。其他任务之间停手会留下半完成状态，但 build 仍应保持绿色（除非违反顺序约束）。

---

## Plan-Level Concerns

记录在分解任务时发现、但属于 plan 文档自身的问题。这些问题已在任务层面被正确处理，但用户在审阅 plan 时可能会困惑，建议在下次更新 plan 时一并修订。

### 1. M5 步骤 2 的实体类清单不精确

**Plan 影响位置**：`docs/plans/pixel-ui-rewrite.md` M5 → "做什么" → 步骤 2

**Plan 当前文字**：
> 把 `Player`、`Monster`、`Item`、`MonsterTemplate` 中 `string Color` 字段类型改为 `SadRogue.Primitives.Color`。

**问题**：
- `Player` 与 `Monster` 都不直接持有 `Color` 字段——该字段定义在它们的基类 `Entity.cs:8`：`public string Color { get; init; } = GameColors.White;`。
- 真正需要修改字段类型声明的是 `Entity.cs`，而 `Player.cs` / `Monster.cs` 仅修改其构造时的赋值表达式（`Player` 构造函数 `Color = GameColors.Yellow`；`Monster.From` 内 `Color = t.Color`，后者在 `MonsterTemplate.Color` 类型升级后自动跟随，无需独立改动）。
- 真正持有独立 `string Color` 声明的实体类是：`Entity`、`MonsterTemplate`、`Item`（共 3 处类型声明 + 多处赋值点）。

**任务层处理**：Task 13 已经按真实代码结构列出修改清单（`Entity.cs`、`Player.cs`、`MonsterTemplate.cs`、`Item.cs`、`SadConsoleRenderer.cs`），并明确说明 `Monster.cs` 与 `PositionedItem` 都不需修改，无歧义。

**建议**：下次更新 plan 时把 M5 步骤 2 第一句改为
> 把 `Entity` (基类) 与 `MonsterTemplate`、`Item` 中 `string Color` 字段类型改为 `SadRogue.Primitives.Color`，并把 `Player` 构造函数与 `Monster.From` / `Item.X()` 工厂方法中所有 `GameColors.X` 赋值替换为 `Palette.X`。
以此与代码结构对齐。
