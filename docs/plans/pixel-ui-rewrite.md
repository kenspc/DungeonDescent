# Plan: 像素风 UI 重写 (Walking Skeleton via SadConsole)

> **来源 brief**: [`docs/briefs/pixel-ui-rewrite.md`](../briefs/pixel-ui-rewrite.md)
>
> **核心决策**: 用 SadConsole 10.x + MonoGame DesktopGL 替换当前 `System.Console` 渲染层；以 Walking Skeleton 模式分 6 个里程碑递进，每个里程碑独立可停手交付。

## Objective

把 DungeonDescent 的渲染层从 `System.Console` 直接绘图替换成 **SadConsole 10.x + MonoGame DesktopGL**，使游戏在 GUI 窗口内跑通完整一局；首版不做视觉打磨，目标是新架构落地并保持游戏功能 100% parity。

**显式不在 Scope 内**（继承自 brief）：

- 自定义像素字体烘焙、色板精调、动画、tileset、粒子效果
- 跨平台单文件 exe 打包 / 发布
- 任何游戏机制变更（`Game.cs` / `Map.cs` / 实体类 / Item 行为）
- 窗口动态 resize / 全屏切换（首版固定窗口尺寸，M1 用 80×30 占位、M3 起切换到 60×26 与游戏 UI 等大；运行时不响应用户拖拽 resize）

## Context

- **平台目标**：单一代码路径双端兼容（**P3**）。开发与首要验证位置：WSL2 + WSLg（已确认本机 `/mnt/wslg` + `WAYLAND_DISPLAY=wayland-0` + `DISPLAY=:0` 全部就位）；Windows native 通过共享 mount 路径作为副验证。
- **失败模式**：唯一不可接受的失败是 (β) 半路弃坑 / 跑不起来。其余 (α 学得不深 / γ 视觉差异不大 / δ 失去 ASCII 想象力) 显式接受。
- **学习哲学**："过程就是一种学习"——SadConsole 内容管线 / 字体烘焙踩坑时间是产出而非损耗。
- **当前代码基线**：`Map` 60×20，UI 总占用 60×26（标题 1 + 地图 20 + 状态 2 + 日志 3）；`Program.cs` 使用经典 `while + ReadKey` 阻塞循环；`GameColors` 使用 ANSI escape 字符串。

## Technical Approach

### 技术栈

| 组件 | 选择 | 理由 |
|---|---|---|
| 渲染框架 | **SadConsole 10.x** (latest stable) | cell-grid 渲染天然映射当前 `Renderer` + `Map` 模型；Brogue 风视觉目标的最佳匹配 |
| Host backend | **`SadConsole.Host.MonoGame`** + DesktopGL | 单一代码路径覆盖 Windows native 与 WSL2/WSLg；OpenGL via SDL2 |
| 默认字体 | **SadConsole 内置 IBM CP437 8×16** | 首版不引入 content pipeline (.mgcb)；自定义像素字体推到下个迭代 |
| .NET | **net8.0**（不变）| 与现有 `.csproj` 一致 |

### 架构决策

1. **改动严格隔离在 UI 层与主入口** — M0~M4 期间，所有改动发生在 `src/UI/`、`Program.cs`、`src/Core/GameColors.cs`。`Game.cs` / `Map.cs` / `src/Entities/*` / `src/Items/*` 不动。这是契约：任何在 M0~M4 阶段要求修改这些文件的提议都触发回到 Discovery 阶段。**M5 例外**：M5 会把实体类 `Color` 字段从 `string` 改为 `SadRogue.Primitives.Color`（同时 `Palette` 从 `src/UI/` 移到 `src/Core/`），这是显式技术债清理步骤，且 `Game.cs` 与 `Map.cs` 仍然不动。
2. **`Game.cs` 不认识 SadConsole** — 输入适配层在独立类 `src/UI/SadConsoleKeyAdapter.cs` 内把 SadConsole `AsciiKey` 翻译成 `ConsoleKeyInfo`，然后由 `GameSurface.ProcessKeyboard` 调用 `Game.HandleKey(ConsoleKeyInfo)`。`Game.cs` 的接口签名零变更。
3. **从阻塞模型切换到事件驱动** — 删除 `Program.cs` 顶层 `while + ReadKey` 循环；改用 SadConsole 主循环 + `ScreenObject.ProcessKeyboard` / `ScreenObject.Update(TimeSpan)` 钩子。游戏状态变化后由 `GameSurface.Refresh()` 重绘。
4. **ANSI escape → `Color` 结构体** — `GameColors`（10 个 ANSI 字符串）整体替换为 `SadRogue.Primitives.Color` 常量映射（SadConsole 10.x 使用 `SadRogue.Primitives.Color`，不是 `Microsoft.Xna.Framework.Color`）；M5 阶段彻底删除 `GameColors.cs`。
5. **Em-dash 兼容性处理** — 当前 `DrawTitle` 使用 Unicode `—`，CP437 默认字体无此字符。M2 阶段替换为 ASCII `-`；自定义字体迭代时可恢复。

## Implementation Steps

每个里程碑设计为 **独立可停手交付**。任何里程碑之后停下来都不会留下"半残"的项目状态。

---

### M0 · 依赖接入

**预计耗时**：~30 min

**做什么**

1. 在项目根目录执行：
   ```bash
   dotnet add package SadConsole
   dotnet add package SadConsole.Host.MonoGame
   ```
2. 在 `DungeonDescent.csproj` 中确认 `<PackageReference>` 已加入。
3. `dotnet build` 验证。

**预期输出**

- `DungeonDescent.csproj` 新增两个 `<PackageReference>`
- `bin/Debug/net8.0/` 下出现 `SadConsole.dll`、`SadConsole.Host.MonoGame.dll`、`MonoGame.Framework.dll`、`SDL2.dll`、`libSDL2.so` 等

**验收**

- `dotnet build` 退出码 0，零警告（`<Nullable>enable</Nullable>` 启用情况下）
- 旧版游戏 `dotnet run` 仍可正常进入和游玩（旧 `Renderer` 未受影响）

**何时停手**：项目状态等于"拉了依赖但没用"，完全可逆，`git checkout DungeonDescent.csproj` 即可回滚。

---

### M1 · Walking Skeleton（最关键里程碑）

**预计耗时**：~1-2 hour

**目标**：验证整条 GUI 工具链（build → MonoGame init → 窗口创建 → 字体加载 → cell 渲染 → 输入响应 → 退出）端到端可用。**不接入 Game 逻辑。**

**做什么**

1. 创建一个临时分支（推荐：`feat/sadconsole-skeleton`）以隔离实验。
2. **完全替换** `Program.cs` 内容为最小 SadConsole host（暂时丢弃旧的 `while + Game` 逻辑，M2 再恢复）：

   ```csharp
   using SadConsole;
   using SadRogue.Primitives;
   using Console = SadConsole.Console;
   using SadGame = SadConsole.Game;  // 避免与 DungeonDescent.Game 冲突

   Settings.WindowTitle = "Dungeon Descent";

   SadGame.Create(80, 30);
   SadGame.Instance.OnStart = () =>
   {
       var screen = new Console(80, 30);
       screen.Print(38, 14, "@", Color.Yellow);
       screen.Print(30, 16, "Press any key to exit", Color.Gray);
       SadGame.Instance.Screen = screen;
       SadGame.Instance.DestroyDefaultStartingConsole();
   };
   SadGame.Instance.Run();
   SadGame.Instance.Dispose();
   ```

   **注**：`Program.cs` 当前 `using DungeonDescent;`，与 `DungeonDescent.Game` 类同名冲突，因此必须用 `SadGame` 别名（或全限定 `SadConsole.Game.Create(...)`）来消歧。

3. 运行 `dotnet run`，验证窗口出现。
4. 如果 WSL2 内启动失败（黑屏或异常退出），按以下顺序排查：
   - 执行 `glxinfo | head -10`（先 `sudo apt install mesa-utils` 若未安装）确认 OpenGL 渲染器
   - 尝试 `SDL_VIDEODRIVER=x11 dotnet run` 强制走 X11 而非 Wayland
   - `dotnet --info` 确认运行时是 net8.0

**预期输出**

- 一个全新的精简 `Program.cs`（约 15 行）
- WSL2 中 `dotnet run` 弹出 GUI 窗口

**验收**

- WSL2 内 `dotnet run` 弹出窗口，显示居中黄色 `@` 与灰色 "Press any key to exit"
- 按任意键关闭窗口，进程正常退出（exit code 0）
- 拷贝/挂载到 Windows 端，PowerShell 内 `dotnet run` 表现一致（**Windows 验证可推迟到 M5**，但若条件允许尽早做能更早发现跨端差异）

**何时停手**：M1 通过 = β 失败模式（弃坑）的最大风险已经规避。整条工具链验证通过后，余下工作均为"在已经活着的项目上做小改进"。

---

### M2 · 渲染地图（无输入版）

**预计耗时**：~2-3 hour

**目标**：把 `Renderer.DrawMap()` 的全部渲染逻辑迁移到 SadConsole；游戏被一次性渲染到屏幕上（静态快照），无输入交互。

**做什么**

1. **新建** `src/UI/SadConsoleRenderer.cs`，包含静态方法：
   - `RenderAll(Game game, IScreenSurface surface)` — 等价旧 `Renderer.DrawAll`；入口先调用 `surface.Surface.Clear()` 再分别绘制 title/map/status/log，避免上一帧残留（旧 `Console.Clear()` 的等价）
   - `RenderMap(Game game, IScreenSurface surface)` — 等价旧 `Renderer.DrawMap`
   - 私有 `Draw*` 方法对应旧 `Draw*`
2. **新建** `src/UI/Palette.cs` 静态类，把 `GameColors` 的 ANSI 字符串映射为 `Color` 常量：
   ```csharp
   static class Palette
   {
       public static readonly Color White   = new(255, 255, 255);
       public static readonly Color Yellow  = new(255, 255, 85);
       public static readonly Color Green   = new(85, 255, 85);
       public static readonly Color Red     = new(255, 85, 85);
       public static readonly Color Cyan    = new(85, 255, 255);
       public static readonly Color Magenta = new(255, 85, 255);
       public static readonly Color Blue    = new(85, 85, 255);
       public static readonly Color Gray    = new(170, 170, 170);
       public static readonly Color DarkRed = new(170, 0, 0);
   }
   ```
   （取值参考标准 16 色 IBM CGA 调色板，未来可在自定义字体迭代时替换）
3. **不改实体类签名**——而是 `SadConsoleRenderer` 内部把现有 `string Color`（ANSI 字符串）的字段反向映射回 `Color`：
   - 在 `SadConsoleRenderer.cs` 内部添加私有方法 `Color AnsiToColor(string ansi)`，对 `GameColors` 中实际被实体使用的 9 个前景色字符串做 switch（White / Yellow / Green / Red / Cyan / Magenta / Blue / Gray / DarkRed）。`Reset` / `Bold` / `BgBlack` 不参与映射（renderer 内部不再需要它们）。
   - **风险点**：实体类的 `Color` 字段类型是 `string`，存储 ANSI escape sequence——这条耦合在 M5 处理（届时 `Color` 字段直接改成 `SadRogue.Primitives.Color` 类型）
4. **修改** `Program.cs`：在 M1 host 基础上构造一个 `DungeonDescent.Game` 实例（注意命名空间消歧——用 `var game = new DungeonDescent.Game();`），在 `OnStart` 闭包内调用 `SadConsoleRenderer.RenderAll(game, screen);`。`screen` 仍是 M1 中创建的 `Console(80, 30)` 局部变量；保持窗口存活直到用户按任意键关闭（M1 行为）。仍无输入处理逻辑。
5. **修改** title 字符串里的 `—` 为 `-`（CP437 兼容）。注意：要修改的是新建 `SadConsoleRenderer.cs` 内复制过来的 title 字符串（旧 `Renderer.cs` 仍保留至 M5 才删，旧文件不必改动）。

**预期输出**

- 新文件：`src/UI/SadConsoleRenderer.cs`、`src/UI/Palette.cs`
- 修改：`Program.cs`（构造 Game + 调用渲染）
- 旧 `src/UI/Renderer.cs` 与 `src/Core/GameColors.cs` **不动**（M5 才删）

**验收**

- `dotnet run` 弹窗显示完整一帧游戏画面：标题栏、地图（含玩家 `@`、怪物、物品、楼梯、墙壁）、状态栏、消息日志
- 视觉等价测试：截图与旧版 console 输出对比，所有 glyph 字符与坐标位置一致；颜色允许因 RGB ↔ ANSI 终端 palette 差异略有偏移（不要求像素级一致，但每种颜色必须能与其他颜色在视觉上明确区分，例如 Goblin 绿与 Cyan 楼梯不可混淆）
- FOV 正确：未探索区域空白，已探索未可见区域为灰色，可见区域为亮色
- 关闭窗口后进程正常退出

**何时停手**：M2 通过 = 渲染层完整替换成功。这是个能截图分享的产物（满足 brief (b) 动机的最小形态）。

---

### M3 · 接入输入 + 重绘循环

**预计耗时**：~1-2 hour

**目标**：把现在的"渲染一次就停"模式改成完整的事件驱动游戏循环。

**做什么**

1. **新建子类** `src/UI/GameSurface.cs`（位于 `DungeonDescent` namespace，因此裸写 `Game` 解析为 `DungeonDescent.Game`，无需别名）：
   ```csharp
   using SadConsole;
   using SadConsole.Input;

   namespace DungeonDescent;

   class GameSurface : SadConsole.Console
   {
       private readonly Game _game;
       public GameSurface(Game game) : base(60, 26) { _game = game; Refresh(); }

       public override bool ProcessKeyboard(Keyboard keyboard)
       {
           foreach (var key in keyboard.KeysPressed)
           {
               // 特殊键拦截（不进入 Game.HandleKey）
               if (key.Key == Microsoft.Xna.Framework.Input.Keys.Q)
               {
                   SadConsole.Game.Instance.MonoGameInstance.Exit();
                   return true;
               }
               // M4 之前 i / ? 暂不处理，直接吞掉避免误传

               var info = SadConsoleKeyAdapter.ToConsoleKeyInfo(key);
               if (info.HasValue)
               {
                   _game.HandleKey(info.Value);
                   Refresh();
                   return true;
               }
           }
           return false;
       }

       private void Refresh() => SadConsoleRenderer.RenderAll(_game, this);
   }
   ```
2. **新建** `src/UI/SadConsoleKeyAdapter.cs`：把 SadConsole `AsciiKey`（`keyboard.KeysPressed` 元素类型）转成 `ConsoleKeyInfo`。重点映射：
   - 字母 / 方向：W/A/S/D、ArrowUp/Down/Left/Right、`q`、`i`、Escape
   - **shift 修饰**：`>`（Shift+`.`，`Keys.OemPeriod` + Shift）、`<`（Shift+`,`，`Keys.OemComma` + Shift）、`?`（Shift+`/`，`Keys.OemQuestion` + Shift）；adapter 需读取 `keyboard.IsKeyDown(Keys.LeftShift)` / `IsKeyDown(Keys.RightShift)` 才能区分
   - 字符键：`.`（`Keys.OemPeriod` 不带 shift）、数字 1-9
   - **关键产出契约**：`Game.HandleKey` 同时使用 `ConsoleKeyInfo.Key`（识别 ArrowUp 等）与 `ConsoleKeyInfo.KeyChar`（识别 `>`、`<`、`.`），adapter 必须为每条映射同时生成两个字段。例如：`new ConsoleKeyInfo('>', ConsoleKey.OemPeriod, shift:true, alt:false, control:false)`。
   - 单元自测建议：在 adapter 内部加一个静态字典或 switch，映射用 `(Keys, bool shift)` 元组当 key，`ConsoleKeyInfo` 当 value。
3. **修改** `Program.cs`：移除"渲染一次就停"的逻辑，改为构造 `GameSurface` 并塞给 `SadGame.Instance.Screen`。SadConsole 主循环自动调度 `ProcessKeyboard`。
   - **窗口尺寸过渡**：M1/M2 用 `SadGame.Create(80, 30)`（占位），M3 起把 `Create` 调用改为 `SadGame.Create(60, 26)`，与 `GameSurface(60, 26)` 等大；M4 拆分多 surface 后整体仍保持 60×26。如果窗口尺寸超出实际渲染区域，会出现黑色边带——视觉可接受但应在 M3 同步调整。
4. **暂不处理** inventory / help / game over overlay（M4）。在 `GameSurface.ProcessKeyboard` 内对特殊键单独分支处理：`q` 直接调用 `SadConsole.Game.Instance.MonoGameInstance.Exit()` 退出窗口；`i` / `?` 暂时落入 no-op 分支（`Game.HandleKey` 不识别这些键会自动跳过，因此即使误传也不会触发 turn logic，但显式拦截更清晰）。
5. **保留** `Program.cs` 在 `SadGame.Instance.Run()` 后的进程退出语义（exit code 0），与 M1 一致。

**预期输出**

- 新文件：`src/UI/GameSurface.cs`、`src/UI/SadConsoleKeyAdapter.cs`
- 修改：`Program.cs`

**验收**

- 玩家按 WASD / 方向键能在地图上移动，地图实时刷新
- 撞怪触发战斗，怪物在玩家结束回合后移动
- 楼梯 `>` `<` 切换楼层正常工作
- 拾取物品自动入库存（即使 inventory UI 还没接，可观察 `Game.Player.Inventory` 状态）
- 按 `q` 关闭窗口
- Inventory / help 暂时不可用（按 `i` `?` 无反应或仅记录到 log，是已知 M4 工作）

**何时停手**：M3 通过 = 游戏可玩，但 inventory / help / 死亡屏等 overlay 缺失。

---

### M4 · 状态栏、消息日志、Overlay 屏幕

**预计耗时**：~2-3 hour

**目标**：完整 UI parity——旧 `Renderer` 能显示的一切，新版都能显示。

**做什么**

1. **拆分 surface 结构**：把 `GameSurface` 替换为顶层 `RootScreen : ScreenObject`，包含子节点：
   - `MapSurface`（60×20，坐标 (0,1)）
   - `TitleSurface`（60×1，坐标 (0,0)）
   - `StatusSurface`（60×2，坐标 (0,21)）—— 两行：HP/ATK/DEF/LV/EXP/Gold/Score 一行 + 按键提示一行
   - `LogSurface`（60×3，坐标 (0,23)）
2. **移植 overlay 屏幕**：
   - 创建 `InventoryScreen`、`HelpScreen`、`GameOverScreen`、`VictoryScreen`，每个为独立 `ScreenObject`
   - 在 `RootScreen` 维护 `_currentOverlay` 引用与 4 个 game 子 surface (`title`/`map`/`status`/`log`) 的引用列表
   - **切屏机制**：按 `i` 时把 4 个 game 子 surface 的 `IsVisible = false` 并把 inventory overlay 加入 `RootScreen.Children`；overlay 内部 `ProcessKeyboard` 处理 `Esc` 键，触发 `RootScreen.CloseOverlay()` 把 game 子 surface 的 `IsVisible = true` 并 `Children.Remove(overlay)`
   - **Help 同模式**：`?` 触发 `HelpScreen`，内部按任意键调 `CloseOverlay`
   - **Game over / Victory 触发**：在 `RootScreen.Update(TimeSpan)` 内检查 `_game.Status`，若变为 `Dead` 或 `Won`，调用 `OpenOverlay(GameOverScreen / VictoryScreen)`；overlay 内按任意键调 `SadConsole.Game.Instance.MonoGameInstance.Exit()` 退出进程（不返回主屏）
3. **细节迁移**：
   - HP < `MaxHp / 3` 显示红色：从 M2 复制过来的 `SadConsoleRenderer.DrawStatusBar` 已包含此逻辑；M4 拆分到 `StatusSurface` 时保留同样规则
   - Inventory 用 `1-9` 数字键选择物品（按 `1` 用 slot 0，按 `2` 用 slot 1...）+ Esc 退出（保留旧键位）；使用物品后调用 `_game.EndPlayerTurn()`（保留旧 `Program.cs` 的回合消耗语义）
   - 死亡 / 胜利屏后按任意键退出程序（不回主菜单——本版本无主菜单）

**预期输出**

- 新文件：`src/UI/RootScreen.cs`、`src/UI/InventoryScreen.cs`、`src/UI/HelpScreen.cs`、`src/UI/GameOverScreen.cs`、`src/UI/VictoryScreen.cs`（**默认每个 screen 一个文件**——便于独立调试；若任一 screen 实现少于 30 行可与同 namespace 的相邻 screen 合并，但 `RootScreen` 必须独立）
- 修改：`Program.cs`、`SadConsoleRenderer.cs`、`SadConsoleKeyAdapter.cs`

**验收**

- **完整 UI parity**：拿一张旧版游戏截图与新版逐项对比，所有 UI 元素都能显示
- HP 低于 `MaxHp / 3` 时数字显示红色
- Inventory 流程：`i` 进入、看到物品列表、`1` 使用第一个物品、看到 log 更新、Esc 返回
- Help 流程：`?` 进入、显示按键说明、任意键返回
- Game Over / Victory 屏：触发时显示总结信息，按任意键退出

**何时停手**：M4 通过 = brief 中"In Scope" 项已全部实现，仅差清理。

---

### M5 · 旧代码清理 + 双端验证

**预计耗时**：~1 hour

**目标**：移除所有 `System.Console` 渲染遗留、统一类型、跨端验证。

**做什么**

1. **删除文件**（直接 `git rm`）：
   - `src/UI/Renderer.cs`
   - `src/Core/GameColors.cs`
2. **修改实体类的 `Color` 字段**：把 `Player`、`Monster`、`Item`、`MonsterTemplate` 中 `string Color` 字段类型改为 `SadRogue.Primitives.Color`。
   - **依赖方向问题**：实体类位于 `src/Entities/` 与 `src/Items/`，原本不引用 UI 层。M5 之前 `Palette` 放在 `src/UI/Palette.cs` 是正确的（UI 层内部使用）。M5 修改字段类型后，实体若直接用 `Palette.Yellow` 会让 entity 反向依赖 UI 层，破坏分层。**两种方案选其一**：
     - **方案 A（推荐）**：把 `Palette` 从 `src/UI/Palette.cs` 移到 `src/Core/Palette.cs`，让 entity / item 与 UI 都从 Core 引用，依赖方向干净。M5 步骤里同步执行此移动。
     - **方案 B**：实体类内联裸写 `new Color(255, 255, 85)` 等 RGB 值，不依赖 `Palette`。代价是颜色常量散落，未来调色板调整需要多文件修改。
   - 选定方案 A 并在执行 M5 时同步 `git mv src/UI/Palette.cs src/Core/Palette.cs`，更新所有引用。
   - 这一步会让 `SadConsoleRenderer` 内部的 `AnsiToColor` 适配方法失去作用，应同时删除
3. **删除** `Program.cs` 顶层的旧代码痕迹：
   - `Console.OutputEncoding = ...`、`Console.CursorVisible = false`、62×27 终端尺寸检查全部删除
4. **更新** `CLAUDE.md`：
   - 删除"Verification is done by building and running manually"以下提到的 62×27 终端检查段落
   - 新增运行环境说明："Requires GUI environment: Windows native, WSL2 with WSLg (Win11), or Linux/macOS with display server"
   - 在依赖部分注明 SadConsole + MonoGame DesktopGL（这两个是首批第三方依赖，brief 已显式接受这条约束突破）
   - 修正 Architecture 段落："no third-party dependencies" 改为"depends on SadConsole + MonoGame DesktopGL"
5. **更新** `README.md`：
   - 第一段 "No third-party libraries — pure System.Console with ANSI color rendering" 需修正为反映 SadConsole + MonoGame
   - "Requirements" 段去掉 "Terminal: minimum 62 columns × 27 rows" 与 "ANSI color support"，改为 GUI 环境要求
   - "Architecture" 段（"No third-party libraries. Rendering uses raw ANSI escape sequences"）需重写
   - 示例截图（ASCII art block）可保留作为视觉参考，并加注 "现已为 GUI 窗口渲染"
6. **正交清理验证**：
   ```bash
   grep -rn "Console\.Write\|Console\.SetCursorPosition\|Console\.Clear\|GameColors\|\\\\x1b\[" src/ Program.cs
   ```
   预期返回零结果。
7. **双端验证**：
   - **WSL2**：`dotnet run` 完整跑一局到死亡或胜利，确认无报错
   - **Windows native**：通过 `\\wsl$\Ubuntu\home\kenspc\projects\DungeonDescent` 在 PowerShell 7 内 `cd` 进去 `dotnet run`（共享同一份代码、同一份 obj/bin），完整跑一局确认窗口正常出现且行为与 WSL2 端一致

**预期输出**

- 删除：`src/UI/Renderer.cs`、`src/Core/GameColors.cs`
- 修改：`Program.cs`、`src/Entities/*`、`src/Items/*` 中持有 `Color` 字段的实体类、`CLAUDE.md`、`SadConsoleRenderer.cs`（移除适配层）

**验收**

- 上面 grep 命令零命中
- WSL2 + Windows 各自完整通关一局或战死一局，无 crash
- `CLAUDE.md` 已更新；`README.md`（若提到运行要求）相应更新
- `dotnet build` 零警告

**何时停手**：M5 通过 = brief 中所有 In Scope 项目交付完成。视觉打磨（自定义字体、色板精调、动画、tileset）从下个迭代开始，是新的 brief / plan。

---

## Risks and Mitigations

| 风险 | 概率 | 影响 | 缓解 |
|---|---|---|---|
| WSLg 在 Wayland 后端下显示异常 | 低（已确认本机 WSLg 配置完整） | 高（M1 卡住等于触发 β 失败） | M1 步骤已写入 `SDL_VIDEODRIVER=x11` 兜底命令；如仍异常，临时切到 Windows native 验证 M1 后再回 WSL2 |
| MonoGame DesktopGL 缺 SDL2 native lib | 极低（WSL2 + WSLg 自带 Mesa + libSDL2） | 中 | `sudo apt install libsdl2-2.0-0` 修复 |
| Em-dash `—` 在 CP437 默认字体不可显 | 高 | 极低（一个标题装饰字符） | M2 阶段 grep `—` 改 `-`；自定义字体迭代时可恢复 |
| 实体类 `Color` 字段类型耦合（当前为 `string` ANSI） | 高 | 中（影响 M2 → M5 的衔接） | M2 用 `AnsiToColor` 适配层避开类型修改；M5 一次性把字段类型改为 `Color`，删适配层。这是显式技术债，已在 M5 计划清偿 |
| SadConsole API 大版本变化（10.x vs 9.x） | 低（10.x 已稳定 1+ 年） | 中 | M0 在 `dotnet add package` 之后立即 `dotnet list package`（或直接 cat `DungeonDescent.csproj`）查看实际拉到的版本号，写到 `.csproj` 的 PackageReference Version；后续不允许在未 review 的情况下升级 |
| Inventory / overlay 切屏逻辑比预期复杂 | 中 | 低（仅延后 M4 完成时间） | M4 已显式拆出独立 ScreenObject，每个 overlay 独立可调试；万一时间紧可在 M4 内部分 sub-milestones（M4a 状态栏+日志、M4b inventory、M4c game over/victory），单独可停手 |
| 视觉差异不够明显（Brogue 风默认字体仍像 ASCII） | 高 | 已在 brief 显式接受 (γ) | 不缓解 — 视觉打磨属于下个迭代 |
| Windows native 验证发现行为差异 | 低 | 中（要在 M5 内回头修） | M5 列为正式验收步骤；M1/M2 阶段若条件允许可早期发现，降低 M5 时返工成本 |

## Build & Run

迁移过程中所有命令保持不变：

```bash
dotnet build          # 编译
dotnet run            # 编译 + 运行（开窗口）
dotnet run --no-build # 直接运行
dotnet watch run      # 改文件即重启（开发首选）
```

**WSL2 启动兜底**（仅 M1 阶段若 Wayland 路径异常时使用）：

```bash
SDL_VIDEODRIVER=x11 dotnet run
```

**Windows 端通过 WSL mount 验证**（M5）：

```powershell
cd \\wsl$\Ubuntu\home\kenspc\projects\DungeonDescent
dotnet run
```

## Definition of Done

整个 plan 达成的标志：

- [ ] M0 ~ M5 所有验收项通过
- [ ] `git grep "Console\\.Write\\|Console\\.SetCursorPosition\\|Console\\.Clear\\|GameColors\\|\\\\x1b\\["` 在 `src/` 与 `Program.cs` 内零命中（与 M5 步骤 5 grep 一致）
- [ ] `dotnet build` 零警告
- [ ] WSL2 与 Windows native 各完整跑过至少一局
- [ ] `CLAUDE.md` 更新到位（运行环境要求、依赖、终端尺寸说明）
- [ ] `README.md` 更新（"No third-party libraries" / 终端尺寸要求 / ANSI 渲染描述等过期内容已修正）
- [ ] 旧 `Renderer.cs` 与 `GameColors.cs` 已从仓库删除
