# 计划书：像素字体首版 —— Brogue 风采样

## Objective

把 SadConsole 当前使用的内置默认字体替换为 Brogue-adjacent 风格的 16×16 像素字体（brief Hard Part #1 路径 B：第三方 OFL/MIT/BSD/ISC 公开像素字体；brief Hard Part #2 路径 B2：源 16×16 × cell 渲染尺寸 32×32），让 60×26 cell grid 在 1920×832 物理窗口下渲染，作为视觉打磨的第二步落地。范围严格按 `docs/briefs/pixel-font-pass.md` Scope 段执行；本 plan 不重新议 Scope / Failure Modes / Hard Part。

**Not in scope of this plan**:
- Scope / Failure Modes / Hard Part 的重新议——已在 brief 中固化。
- 动画 / 颜色抖动 / 多 floor variant / 标题画面美化——是后续步骤的工作。

## Background

- **上游 brief**：`docs/briefs/pixel-font-pass.md`（Cell 尺寸 32×32 / 路径 B2 / OFL-MIT-BSD-ISC license / Brogue anchor）。
- **更上游 brief**：`docs/briefs/palette-brogue-pass.md`（视觉打磨 step 1 — 已落地，commits f95ae05 → e0119f5）；`docs/briefs/pixel-ui-rewrite.md`（视觉打磨 deferred 起源）。
- **显示器前置（plan 阶段已确认）**：用户日常 2560×1440 + 可选外接 4K——1920×832 物理窗口在两种分辨率下均充裕，无 fallback 分支需要，brief Constraints 中的 24×24 退路本 plan 不启用。**例外触发条件**：若 M2 acceptance 实测窗口在 WSLg / 主机环境下因 DPI scaling 或窗口装饰开销实际不可用（标题栏被裁、内容超出屏幕、SDL 报错拒绝创建窗口），按 brief Constraints 的 24×24 fallback（路径 B3）重写本 plan 后再继续，不在本 plan 范围内偷偷换尺寸。
- **F1 narrative discipline 跨 brief 继承**：本 plan 的所有 commit message / PR / 进度叙事必须显式说"step 2 of 3"——视觉打磨整体仍 incomplete（动画是 step 3）。tileset 永久搁置不计入步数。

## Technical Approach

### Tech stack

- SadConsole 10.9.0 + SadConsole.Host.MonoGame 10.9.0 + MonoGame.Framework.DesktopGL 3.8.4.1（`.csproj` 现状，本 plan 不动版本）。
- .NET 8，单一 `DungeonDescent` namespace，`<ImplicitUsings>` + `<Nullable>` 启用。
- 不引入新的 NuGet 包。字体资产纯静态文件（PNG + JSON + LICENSE.txt）。

### Asset 路径与目录结构

```
assets/
└── fonts/
    ├── README.md                       # 字体来源 + license 总表
    └── <chosen-font-name>/
        ├── <name>.font                  # SadConsole JSON descriptor
        ├── <name>.png                   # 字形表（CP437/Latin-1 grid）
        └── LICENSE.txt                  # 字体本体 license 全文
```

`.csproj` 添加：

```xml
<ItemGroup>
  <Content Include="assets/fonts/**/*.font;assets/fonts/**/*.png"
           CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

`.txt` 类（`LICENSE.txt`、`README.md`、`seed.txt`）刻意不进 `<Content>`——是仓库内审计记录而非运行时资产。

**不引入 MGCB**——brief Hard Part #4 已对齐"直读姿态"。

### SadConsole 10.9 字体加载与 32×32 cell 渲染

API 表面**已经 Task 2 通过 Context7 + 本地 NuGet XML 文档验证**（详见 `docs/screenshots/font-pass/sadconsole-api-notes.md`）。SadConsole 10.x 走 **Builder 模式**，没有 `Game.Create(int, int, string fontPath, ...)` 重载——这是 plan v1 的 Technical Approach 假设错误，本节已据 Task 2 findings 重写。

**自定义字体加载 + 全局 2× 缩放**（路径 B2 的实际实现）：

```csharp
// Program.cs（重写）
using SadConsole;
using SadConsole.Configuration;
using SadGame = SadConsole.Game;

Settings.WindowTitle = "Dungeon Descent";

var fontPath = Path.Combine(AppContext.BaseDirectory,
    "assets/fonts/<name>/<name>.font");

var startup = new Builder()
    .SetScreenSize(DungeonDescent.Layout.WindowWidth,
                   DungeonDescent.Layout.WindowHeight)
    .ConfigureFonts((cfg, _) => cfg.UseCustomFont(fontPath))
    .SetDefaultFontSize(IFont.Sizes.Two)        // ← 16×16 源 × 2 = 32×32 cell
    .OnStart((_, _) =>
    {
        var game = new DungeonDescent.Game();
        var root = new DungeonDescent.RootScreen(game);
        SadGame.Instance.Screen = root;
        SadGame.Instance.DestroyDefaultStartingConsole();
    });

try
{
    SadGame.Create(startup);
    SadGame.Instance.Run();
}
finally
{
    SadGame.Instance.Dispose();
}
```

物理窗口尺寸：`60 cell × (16×2) = 1920 wide`、`26 cell × (16×2) = 832 tall`。

**为什么 Builder 模式简化了一切**：`.SetDefaultFontSize(IFont.Sizes.Two)` 设置全局默认字号；任何在 `OnStart` 之后构造的 `ScreenSurface`（含 game surfaces 与所有 overlay）都会在构造瞬间从 `GameHost.DefaultFont` + `DefaultFontSize` 继承——**无需在 `RootScreen.cs` / overlay 类里逐个设 `FontSize`**。这把 plan v1 的 M2 step 8 (per-surface FontSize) 与 M2 step 9 (overlay conditional FontSize) 全部消化掉。

**Overlay 字体继承（已验证）**：在 `OnStart` 之后构造的 `ScreenSurface(int, int)` 会从 `GameHost.DefaultFont` 继承字体——本 plan 的所有 overlay (`InventoryScreen` / `HelpScreen` / `GameOverScreen` / `VictoryScreen`) 都是用户操作期触发，构造时机晚于 `OnStart`，自动继承。Plan v1 的 OQ2 在此已闭合。

### License 落地

- 每个 candidate / 选定字体目录放 `LICENSE.txt`（字体作者原始 license 全文，逐字复制）。
- `assets/fonts/README.md` 单文件聚合表，每行格式：
  ```
  | font name | source URL | license | designer |
  ```
- 即使本项目目前不发行也照办——日后想 share 时不必返工（brief F5）。

### Variant audit 截图姿态

- M1 先截 baseline（默认字体 + 当前 palette + 装饰 variant）— 作为对比基准。
- M3 每个 candidate 加载后截相同场景（同 floor seed、同玩家位置、同怪物物品分布）。
- 对比窗口：`docs/screenshots/font-pass/before.png` vs `cand-<name>.png`。
- **Variant audit 用 fixed map seed**：`Map` 已支持 seed 构造函数；M3 临时**只改 `Game.cs:19`**（`Game()` ctor 的初次 Map 创建）为固定常量，让 floor 1 audit 截图可复现。其余 3 处（line 27 retry loop / line 209 NextFloor / line 222 PrevFloor）必须保持 `_rng.Next()`——尤其是 line 27，pin 成常量会让 retry 循环无限重出同图直到 `MaxMapAttempts` 抛异常。Line 19 选定的 seed 必须事先验证为 "known-good"（单次调用就能产出 ≥ 2 房间，不依赖 retry）。Seed 数值文档化在 `docs/screenshots/font-pass/seed.txt`，**M5 清理时保留 seed.txt 文件**（不保留 `Game.cs` 代码改动），作为未来重做 audit 的复现凭据。
- **截图工具**：Windows 主机直接跑 `dotnet run` 时用 Snipping Tool / Win+Shift+S 框选游戏窗口，导出 PNG 不缩放。WSL2 + WSLg 时 Win+Shift+S 同样能识别 WSLg 窗口；命令行替代为 `wsl.exe -e bash -c "..."` 不适用（无 X 截屏在 WSL 内部）。Linux 桌面用 `gnome-screenshot -w` 或 `flameshot gui`。无论何种工具：截图必须保存为 PNG，**不允许 JPEG**（lossy 压缩破坏像素 audit）。

## Implementation Steps

每个 milestone 独立可停手——任何 milestone 完成后都可以暂停项目而不留半成品。

### M1 · Baseline 截图（pre-change 对比基准）

**做什么**：
1. 在当前 git HEAD（palette-brogue-pass 已落地）状态启动游戏。
2. 进入 floor 1，玩到能同时看到 player + 至少 1 monster + 1 item + FloorMossy + FloorCracked + StairsDown + status 行 + log 行的场景。
3. 截图保存为 `docs/screenshots/font-pass/before.png`。

**输入**：当前 git HEAD（commit e0119f5 或更新）。  
**输出**：1 张 PNG 截图。  
**Acceptance**：
- 截图文件存在于指定路径
- 包含上述 8 类可视元素（player / monster / item / FloorMossy / FloorCracked / StairsDown / status text / log text）
- 截图分辨率约 480×416（默认字体 8×16 cell × 60×26 grid）
- 文件 commit 进 git（与 plan 同步——审计材料）

### M2 · Pipeline 接入 + 32×32 cell（用 placeholder 字体）

**做什么**：
1. **API 验证（已完成于 Task 2，2026-05-08）**：findings 见 `docs/screenshots/font-pass/sadconsole-api-notes.md`。要点：
   - `Game.Create(int, int, string fontPath, ...)` 重载 **不存在** —— 走 `Builder` 模式 + `.ConfigureFonts((cfg, _) => cfg.UseCustomFont(path))`。
   - `ScreenSurface.FontSize` 是 `Point` 可设——但 plan 现采用全局 `.SetDefaultFontSize(IFont.Sizes.Two)`，**不再需要逐 surface 设**。
   - 子 surface 在构造时自动继承 `GameHost.DefaultFont` + `DefaultFontSize`——overlay 也自动继承，原 step 9 conditional **已废**。
   - `.font` schema 字段名：`IsSadExtended`（不是早期 plan template 写错的 `IsSadFontFormat`）。
   - **本步已闭合**——后续步骤按本节修订后的 Technical Approach 执行即可。
2. 选 placeholder 字体：**GNU Unifont 16×16**（OFL/GPL dual-licensed，刻意选风格不对的——避免心理上把 placeholder 当真 candidate）。
3. **生成 Unifont 16×16 PNG 字形表**：从 https://unifoundry.com/unifont/index.html 下载 `unifont-<version>.bdf`（或 `.hex`），用 `bdf2psf` / `bdftopcf` / 自写脚本提取 Basic Latin + Latin-1 + CP437 子集排成 16×16 cell 网格 PNG（恰好 256 字符 = 16 列 × 16 行 → 256×256 px PNG）。或直接用 SadConsole 社区已发布的 Unifont 16×16 改良 PNG（若可找到 OFL 兼容版本，记录来源 URL 到 README）。**接受失败**：若 1 小时内拼装不出可读 PNG，placeholder 改用 SadConsole 内置 `IBM_8x16_NoPadding`（直接复制其 `.font` + `.png` 出来作为 placeholder 资产路径），M2 目的"验证 pipeline"不变。
4. 创建 `assets/fonts/unifont/` 目录，放置：
   - `unifont.font`（SadConsole JSON descriptor，模板见下文 4a）
   - `unifont.png`（16×16 字形表，CP437/Latin-1 子集即可）
   - `LICENSE.txt`（GNU Unifont 完整 license 文本）

   **4a · `.font` JSON descriptor 模板**（Task 4b runtime 修订版，2026-05-08）：
   ```json
   {
     "$type": "SadConsole.SadFont, SadConsole",
     "Name": "unifont",
     "FilePath": "unifont.png",
     "GlyphHeight": 16,
     "GlyphWidth": 16,
     "GlyphPadding": 0,
     "Columns": 16,
     "Rows": 16,
     "SolidGlyphIndex": 219,
     "UnsupportedGlyphIndex": 0,
     "IsSadExtended": false
   }
   ```
   **`$type` 鉴别字段必填**——Newtonsoft.Json 反序列化 `IFont` 接口需要它指向具体类（`SadConsole.SadFont, SadConsole`）。缺它运行时抛 `JsonSerializationException: Could not create an instance of type SadConsole.IFont. Type is an interface or abstract class and cannot be instantiated.`——Task 2 schema 验证靠 XML doc 漏掉了这个 JSON-level 元数据，Task 4b 运行时补上。详见 `docs/screenshots/font-pass/sadconsole-api-notes.md` Assumption D。M3/M6 candidate 字体的 `.font` 文件全部要含 `$type` 字段。
5. 创建 `assets/fonts/README.md`，含 Unifont 条目。
6. 修改 `DungeonDescent.csproj` 加 `<Content Include="assets/fonts/**/*.font;assets/fonts/**/*.png" CopyToOutputDirectory="PreserveNewest" />`。
7. **重写 `Program.cs` 为 Builder 模式**——见 Technical Approach 段的代码块为准：用 `new Builder().SetScreenSize(...).ConfigureFonts((cfg, _) => cfg.UseCustomFont(fontPath)).SetDefaultFontSize(IFont.Sizes.Two).OnStart((_, _) => { ... }).` 然后 `SadGame.Create(startup)`。原有 `try/finally Dispose` 结构保留。
8. **`RootScreen.cs` 不需修改**——子 surface 构造时自动从 `GameHost.DefaultFont` + `DefaultFontSize` 继承字体与字号；不再需要逐 surface 设 `FontSize`。
9. **Overlay 也不需修改**——同样在 OnStart 之后构造，自动继承。原 plan v1 step 9 (conditional overlay FontSize) **已废**。
10. `dotnet build && dotnet run`，玩到 floor 2，开 inventory + help overlay。**不要求触发 game-over / victory overlay**——M2 是 placeholder pipeline smoke test，目的是验证字体加载 + 32×32 cell 渲染不崩；game-over / victory 的视觉验证留到 M5 final session（项目无 dev cheat，触发 victory 需打通 floor 5 击败 Dragon，对 placeholder 而言成本不匹配）。

**输入**：M1 完成。  
**输出**：游戏可在 placeholder 字体 + 32×32 cell 下端到端运行。  
**Acceptance**：
- 物理窗口实测 1920×832（用截图工具像素测量）。**WSLg HiDPI 放宽规则**：若 Windows 主机缩放（如 125% / 150%）导致 WSLg 报告窗口为 2400×1040 / 2880×1248 等非原生值，验证标准改为：单个 glyph 在屏幕上必须由整数 × 整数像素方块构成（无亚像素混合 / 无灰阶 anti-alias 漏出），用截图工具放大 4× 比对（任意单个 `@` 字形的边缘像素必须保持纯色边界，无中间灰阶）。见 R3。
- 4 个 game surface（title / map / status / log）+ 4 个 overlay（inventory / help / game over / victory）全部以 32×32 cell 渲染，无 cell 尺寸不一致
- Status 行 60 列文字未被裁剪：`HP:NN/NN ATK:NN DEF:NN LV:N EXP:NN/NN G:NNN Sc:NNNN` 完整可见
- 完整玩到 floor 2 不崩；inventory + help overlay 各自打开 + 关闭不崩。Game-over / victory overlay **不在 M2 验证范围**（见 M5）
- `git status` clean 后再 commit；commit message 含"step 2 of 3 (M2: pipeline only, placeholder font)"

### M3 · Candidate 调研 + 截图比对（3-5 候选）

**做什么**：
1. 从以下来源出 short list（criteria：OFL/MIT/BSD/ISC + **16×16 source** + Brogue-adjacent 厚实风格 + CP437/Latin-1 glyph 覆盖完整）：
   - **int10h.org Oldschool PC Fonts**（VileR，CC BY-SA 4.0 + 部分 OFL）— 含 PS/55、SVGA、Cordata 等 16×16 变体（注：CC BY-SA 4.0 不是 brief 列出的允许 license——需在 M3 决策时**显式审议**是否扩允许列表，或仅用其 OFL 子集）
   - **GNU Unifont**（OFL + GPL dual）— 16×16，覆盖广但视觉朴素（已在 M2 装为 placeholder，M3 无需重装）
   - **MxPlus IBM 系列**（int10h，OFL）— 部分有 16×16 hi-density 变体
   - **其他**：Press Start 2P 16×16 变体、Cherry / Curses 系（如能找到清晰 OFL/MIT 来源）
2. 选定 3-5 个 candidate（含 Unifont 不重复算）。每个安装到 `assets/fonts/<name>/`，含 `.font` + `.png` + `LICENSE.txt`，更新 `assets/fonts/README.md` 加条目。
3. **固化 map seed（floor 1 audit only，仅 pin line 19）**：`Map` 已支持 `public Map(int seed)` 构造函数。`Game.cs` 有 4 处 `new Map(_rng.Next())`，但**只有 line 19 是初次 Map 创建**——M3 audit 截图只在 floor 1 取，因此**只需 pin line 19**：

   - **Line 19**（`Game()` ctor 的初次 Map 创建）：临时改为 `new Map(42)`（或其他 known-good 常量）。
   - **Line 27**（在 `while (Map.Rooms.Count < 2 && attempts < MaxMapAttempts)` retry loop 体内）：**必须保持 `_rng.Next()`**。pin 成常量会让 retry loop 无限重出同图，触达 `MaxMapAttempts` 后抛 `InvalidOperationException`。
   - **Line 209/222**（`NextFloor` / `PrevFloor` 切楼层时的 Map 重建）：保持 `_rng.Next()`，M3 audit 不参与。

   **Known-good seed 验证流程**：选定 seed 前先验证它在 line 19 单次调用就能产出 `Rooms.Count >= 2`（避免 line 27 retry loop 被触发——retry 仍然 random 但增加截图与原 baseline 的不确定性）。验证方式：临时在 `Program.cs` 顶部加一行 `Console.WriteLine($"seed=42, rooms={new DungeonDescent.Map(42).Rooms.Count}");` 跑一次确认，不达标换 seed（43、44、…）直到 known-good，再正式 pin line 19。

   Seed 数值写入 `docs/screenshots/font-pass/seed.txt`（含 known-good 验证结果），commit 这个 .txt 文件。`Game.cs` 的 dev-only 改动用 `git stash` 保管不向 main 推。
4. 临时改 `Program.cs` 字体路径，逐个 candidate 加载、运行游戏、走到与 M1 baseline 相同场景、截图存 `docs/screenshots/font-pass/cand-<name>.png`。
5. 全部截完后 `git stash pop` 撤回 `Game.cs:19` 的 seed 改动（保留 seed.txt + 截图）。

**输入**：M2 完成。  
**输出**：3-5 个 candidate font 安装在 `assets/fonts/`；同等数量的 candidate 截图；`seed.txt` 含使用的 seed 值。  
**Acceptance**：
- short list 含至少 3、至多 5 个 candidate
- 每个 candidate 都通过 M2 的 acceptance（32×32 渲染无破损）
- 每个 candidate 截图完成且可见 baseline 中所有元素（player / monster / item / FloorMossy / FloorCracked / stairs / status / log）
- `seed.txt` 含明文 seed 数值
- `git diff src/Game.cs` 中无 seed 常量痕迹（line 19 临时改动已撤回，4 处 `new Map(_rng.Next())` 全部保持原状）
- `assets/fonts/README.md` 列出所有 candidate
- License 全部明确——若有 candidate license 不在 brief 允许列表（OFL/MIT/BSD/ISC）则在本 milestone 显式记录是否扩展允许列表，扩则 plan 必须更新

### M4 · 可读性 audit + 字体选定

**做什么**：对每个 M3 candidate 跑下面的 checklist：

```
Variant 辨识（继承 palette brief F2）：
  [ ] FloorMossy `,` 与 FloorBase `.` 可瞬识区分
  [ ] FloorCracked `'` 与 FloorMossy `,` 可瞬识区分
  [ ] FloorMossy / FloorCracked vs Entity 不撞色不撞形

战斗辨识：
  [ ] Player `@` 在战场上最显眼
  [ ] Monster 字母（'r' 'g' 'k' 'T' 'd' 'L' 等）相互可区分

UI 数字可读：
  [ ] Status 行 HP/ATK/DEF/EXP/G/Sc 数字完全清晰
  [ ] 字母 `O`/`0`、`I`/`l`/`1`、`B`/`8` 可区分

Brogue anchor（继承 brief F2）：
  [ ] 整体感不偏 IBM PC / Apple II / C64 retro
  [ ] 笔画厚实程度接近 Brogue
  [ ] 灰阶 anti-alias 极少 / 近无
```

任何 candidate 一项不通过 → 否决。把幸存 candidates 按 anchor 接近度排序，选 1 个，写 `docs/screenshots/font-pass/decision.md` 含：
- 选定字体 + 来源 URL + license + designer
- 每个被否决 candidate 的具体否决项（按 checklist 逐条标红）
- 留作未来回看的审计轨迹

**Escalation**（若 M3 选定的全部 candidates——3 至 5 个，视当时短名单大小——全否决）：
- **第一选择**：扩大 candidate 池，回 M3。最多扩至 8 candidates。
- **第二选择**：升级到 brief Hard Part #1 的 C 路径（自制 CP437 改型）— **要求重写本 plan**，不再走 M5。
- **第三选择**：升级到路径 B1（源 32×32 字体）— **要求重写本 plan**，不再走 M5。

**输入**：M3 完成。  
**输出**：`decision.md` + 选定字体名（写入 `assets/fonts/README.md` 顶部"Selected"段）。  
**Acceptance**：
- 1 个 candidate 通过全部 checklist（或触发 escalation 路径之一）
- `decision.md` 含完整否决理由表（每个被否决 candidate 一段）
- `assets/fonts/README.md` 顶部加"Selected: <font-name>"标记

### M5 · 清理 + 端到端验证 + commit

**做什么**：
1. 删除 `assets/fonts/` 下所有未选定的 candidate 目录（包括 M2 的 unifont placeholder，除非选定就是 Unifont）。
2. 更新 `assets/fonts/README.md`：仅保留选定字体的条目 + "Selected" 标记。
3. 更新 `Program.cs` 字体路径指向选定字体（如 M2 之后未改回，本步固化）。
4. 检查 `git diff src/Game.cs` 不含 M3 临时引入的固定 seed 常量（4 处 `new Map(_rng.Next())` 已恢复原状）。
5. 完整玩一局：floor 1 → 战斗 → 捡物品 → 上楼 → floor 2 → 让 HP 归零触发 GameOverScreen（站在怪物旁继续受伤至死即可——比 floor 5 击败 Dragon 触发 victory 成本低得多）。
6. 验证 overlay 视觉布局：inventory / help / game-over（runtime 必触发）。**Victory overlay 处理**：与 inventory / help / game-over 共用同一 `ScreenSurface` 渲染管线，结构同构——这 3 个全部通过即视为 victory 也通过，**不强制 runtime 触发 victory**。若本次 session 自然抵达 floor 5 击败 Dragon 则顺便验证；否则 victory overlay 的 runtime 触发推迟到下一次自然达成的 session，不阻塞本 commit。
7. 起 commit：

```
feat(font): adopt 16x16 brogue-adjacent pixel font

Visual polish step 2 of 3 (palette → font → animation).
Animation remains deferred; this commit does not complete visual polish.
```

（具体行文可调，但 "step 2 of 3" + "does not complete visual polish" 两个短语必须出现，兑现 brief F1。）

**输入**：M4 完成。  
**输出**：clean working tree + 1 commit。  
**Acceptance**：
- `assets/fonts/` 仅含 1 个 font 目录 + 1 个 README.md + `decision.md`（继续保留作为审计）
- `docs/screenshots/font-pass/` 内 baseline + cand-* + decision.md + seed.txt 全部保留（审计用）
- 全程无视觉回归（baseline 中所有元素仍可见、可读、可区分）
- 3 个 overlay（inventory / help / game-over）已 runtime 触发并验证 32×32 渲染；victory overlay 不强制 runtime 触发（见 step 6 说明）
- commit message 显式含 "step 2 of 3" 与 "does not complete visual polish"
- `git status` clean

## Risks and Mitigations

- **R1 · SadConsole 10.9 字体 API 与 plan 假设不一致**：~~plan 假定 `SadConsole.Game.Create(int, int, string, ...)` 重载存在 + `ScreenSurface.FontSize` 可直接赋 `Point`。Mitigation：M2 第一步显式用 Context7 MCP 查证。~~ **已闭合（Task 2，2026-05-08）**：plan v1 假设的重载不存在，10.x 走 Builder 模式；plan Technical Approach 段已据此重写。Findings 详见 `docs/screenshots/font-pass/sadconsole-api-notes.md`。
- **R2 · 16×16 source OFL 字体候选稀疏**：M3 可能找不到 5 个达标 candidate。Mitigation：M4 已含 escalation 路径（扩池 8 → C 自制 → B1 源 32×32）。Plan 接受最少 3 candidates 即可推进。Unifont 已在 M2 装妥，是 baseline candidate。
- **R3 · WSLg HiDPI scaling 让 1920×832 物理窗口实际不是 1920×832**：WSLg 会按 Windows 主机 DPI 拉伸窗口。Mitigation：M2 acceptance 用截图工具像素测量；若 WSLg 拉伸，验证 cell 内部 nearest-neighbor 仍是整数倍即可，物理像素值放宽。Windows 主机直接跑（非 WSLg）应该无此问题——若怀疑 WSLg 异常先在 Windows 验证。
- **R4 · 32×32 + status 行 60 列文字溢出**：当前 status 行最坏情况已经接近 60 列；新字体若个别字符更宽（虽源 16×16 应严格 monospace），SadConsole 会 silently truncate（`SadConsoleRenderer.cs:142-145` 注释提到这一点）。Mitigation：M2 acceptance 显式逐字段检查 status 行可见，全部 candidate 都过 M4 status 数字 checklist。
- **R5 · M3 fixed seed 临时改动忘清理**：M5 含显式 `git diff` 检查。Mitigation：M3 用 `git stash` 保管 `Game.cs:19` 改动（仅 1 行——因 retry loop / 楼层切换的另外 3 处必须保持 `_rng.Next()`），M3 末尾 `git stash pop` 撤回，M5 acceptance 强制 `git diff src/Game.cs` 中不再含 line 19 引入的固定 seed 常量。
- **R6 · License 边界审议拖累 M3**：若 candidate 来自 int10h.org 且其 license 是 CC BY-SA 4.0（不在 brief 允许列表），M3 步骤 5 要求显式审议是否扩展允许列表。Mitigation：plan 不预先批准扩列表——交 M3 阶段决策；若拒扩，则该 candidate 自动出局，回到剩余候选。

## Open Questions

- **OQ1 · ~~SadConsole 10.9 是否支持 `Game.Create` 的 fontPath 重载~~** ✅ **闭合（Task 2，2026-05-08）**：不支持。10.x 走 Builder 模式 + `.ConfigureFonts((cfg, _) => cfg.UseCustomFont(path))`。Plan Technical Approach 段已重写。
- **OQ2 · ~~ScreenSurface 与 Overlay 的字体继承~~** ✅ **闭合（Task 2，2026-05-08）**：构造时自动从 `GameHost.DefaultFont` + `DefaultFontSize` 继承。OnStart 后构造的所有 surface（含 overlay）自动继承。M2 step 8/9 conditional 已废。
- **OQ3 · 字体短名单的最终来源池**：M3 起点已列（int10h / Unifont / MxPlus），但若用户对某个 candidate 有先验偏好（"我想试试 Cherry"），plan 不阻止增加。
- **OQ4 · License 允许列表是否扩展**：brief 列 OFL/MIT/BSD/ISC；若 M3 出现 CC BY-SA 4.0 候选（int10h 多数），是否扩列表——M3 阶段决策。

## References

- 上游 brief：`docs/briefs/pixel-font-pass.md`
- 上游 brief（步骤 1）：`docs/briefs/palette-brogue-pass.md`
- 上游 brief（视觉打磨起源）：`docs/briefs/pixel-ui-rewrite.md`
- SadConsole 字体格式参考（M2 阶段查证）：`/thraka/sadconsole`（Context7 lib id 待 M2 验证）
- 字体 candidate 来源：[int10h.org Oldschool PC Fonts](https://int10h.org/oldschool-pc-fonts/)、[GNU Unifont](https://unifoundry.com/unifont/index.html)
