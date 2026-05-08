# Requirement Brief: 像素字体首版 —— 向 Brogue 风采样

## Outcome

把 SadConsole 当前使用的默认 IBM 风 8×16 内置字体替换为**采样自 Brogue 实际外观**的自定义像素字体（PNG 字形表 + `.font` JSON 描述符），让上一步落地的 20 色语义色板真正与字体形态合奏，呈现 Brogue 风的"色板 + 字形"二合一视觉指纹。这是 pixel-ui-rewrite 之后视觉打磨的**第二步**——做完后视觉打磨依然 incomplete（动画仍在 deferred），这是预期行为，不是失败。

## Scope

**In scope:**

- 选定字体来源：Brogue vanilla 1.7.5 的实际字形表采样 / 第三方公开像素字体 / 自制 CP437 改型 三选一（plan 阶段决策）。
- 在 `DungeonDescent.csproj` / SadConsole 内容管线中加入字体资产（PNG 字形表 + 同名 `.font` JSON），并通过 `SadFont` 加载到全 4 个 surface（title / map / status / log）+ overlays。
- `Program.cs` / `src/UI/RootScreen.cs` 接入字体：`SadConsole.Game.Instance.LoadFont(...)` 或等价路径，把默认字体替换为新字体作为全局 default。
- **Cell 像素渲染尺寸目标 32×32**（路径 B：源字体 16×16，SadConsole 全局 `SizeMultiple = 2` 做 nearest-neighbor 整数倍放大；与 Brogue CE HD 模式同姿态）。`src/Core/Layout.cs` 的 60×26 grid 保持不变；物理窗口随之变为 1920×832，与显示器适配关系见 Constraints。
- 关键字形可读性 audit：`,` 与 `'`（FloorMossy / FloorCracked 装饰区分依赖单像素差异）、`.` 与 `,`、`<` 与 `>`、`@` 与 `&`、数字与字母 `O`/`0`、`I`/`l`/`1` 在新字体下都必须仍然瞬识。

**Out of scope:**

- 任何动画 / 颜色抖动 / 攻击 flash / 平滑滚动（动画是第三步，与字体合并做触发 F1）。
- 字体的从零像素设计（非画师工作；本 brief 只做"挑选 + 接入 + 微调"，不做"画字体"）。
- 多字体 / 字体切换 / 主题（运行期一律单字体）。
- 任何 `Game.cs` / `Map.cs` / 实体 / Item / FOV / BFS 行为变更——继续严格沿用 pixel-ui-rewrite 的"机制层不动"约束。
- 新增 floor variant / wall variant / 新装饰 glyph（color slot 已锁，本步只换字形载体不扩字符表）。
- 国际化 / 中日韩 glyph（CP437 / Latin-1 子集足够 Brogue 风游戏使用）。

**Deferred（建议顺序，不是承诺）：**

- 动画（颜色抖动 per-tile / 攻击 flash / FOV 透视淡入 / 上下楼过渡）—— 视觉打磨第三步；做之前必须有稳定的字体 baseline。
- tileset —— 永久搁置（与 (ii) Brogue 锚点冲突，跨两份 brief 一致结论）。
- 过场 / 标题画面美化 —— 字体落地后再判断是否需要。

## Failure Modes

按风险高到低：

- **F1 · 把这一步当视觉打磨完成**：commit message / PR / 进度汇报里把"字体换好"包装成"视觉打磨完成"。从上一份 brief 继承的叙事约束——本 brief 的根本前提是这件事**只是第二步**。色板 + 字体仍未含动画，不构成 Brogue 完整视觉。
- **F2 · 字体走错风格 anchor**：选了 IBM PC / Apple II / Commodore 64 系的复古像素字体，复古感强但**不是 Brogue 风**。Brogue 字体偏厚实、衬线感弱、灰阶 anti-alias 极少（近乎纯像素）；选错风格会无声地把视觉锚点从 (ii) Brogue 滑向通用 retro。Plan 阶段必须把候选字体并排截图比对再选。
- **F3 · 装饰 variant 辨识失效**：FloorMossy 用 `,`、FloorCracked 用 `'`，区分依赖于 1-2 像素的形状差。新字体若把这两个标点画得太相似（或太装饰），玩家在地图上根本看不出 variant，整个 palette-brogue-pass 第三步（"色板要看得到"）的兑现被字体偷掉。Plan 阶段必须做 variant glyph 可读性 audit，不通过就调字体或换 glyph。
- **F4 · 32×32 物理窗口溢出显示器可视区**：60×26 cell × 32×32 = 1920×832 物理窗口。在 1080p 屏（1920×1080）上仅余 ~248px 给 OS 标题栏 + 任务栏 + 窗口边框——边界值，部分桌面环境（含 Windows 任务栏未隐藏 + 标题栏 + 边框）会超出可视区强迫缩放。WSLg 同等约束。1366×768 屏一律放不下。**Plan 阶段第一步必须以"用户日常显示器分辨率"为前置输入**做决策，不通过则按 Constraints 中的 fallback 退回 24×24。
- **F5 · 字体许可问题**：Brogue 是 AGPLv3，直接抄它的字体资产 redistribute 的合规边界在 hobby 项目里通常会被忽略。即便项目目前不发行（pixel-ui-rewrite 中 (b) 可分享性占小份额），plan 阶段仍需显式记录字体来源 + license，避免日后想发布时返工。

**显式接受的失败（不算 failure）：**

- 视觉效果仍未"惊艳"——色板 + 字体仍缺动画，Brogue 完整观感不可能在第二步呈现。
- 字体字号变更使窗口物理大小不再是任意尺寸，截图比例与上一版不同——是字体作业的固有代价，不算回归。
- 部分 glyph（如 `&` `%` `*`）在新字体下风格略显突兀——只要不损害可读性即可，纯字体审美调优放到后续微调。

## The Hard Part

最难的不是接字体的 SadConsole API（`Game.LoadFont(path)` 一行调用），而是**这四个判断**：

1. **字体来源三选一的取舍**——
   - **A · 直接采样 Brogue 字形表**：风格 100% 命中，但 license 边界模糊（AGPLv3 资产 redistribute 限制）。
   - **B · 第三方公开像素字体**（如 Press Start 2P / Cozette / Cherry / Spleen 等）：license 干净（多为 OFL/MIT），但风格各异，需要并排比对挑最接近 Brogue 的那一款。
   - **C · 自制 CP437 改型**：把现有 SadConsole IBM 风字体的关键 glyph 局部改造（厚化笔画、调标点、调装饰 variant 用的 `,` `'`）。工作量最大但可控且 license 干净。
   - **倾向**：**B 优先**——license 干净、可读性已被群体使用验证、成本最低；只有当 plan 阶段所有 B 候选都明显偏离 Brogue 时才回退到 A 或 C。
2. **32×32 cell 的实现路径选择**——用户偏好已锁定 32×32。两条实现路径：
   - **B1 · 源字体本身就是 32×32 像素**：候选稀少（32×32 像素字体本来就少，Brogue 风的更稀），多数情况要回落到自制改型（Hard Part #1 的 C 选项），工作量大且字体短名单被进一步收窄。
   - **B2 · 源字体 16×16，SadConsole 全局 `SizeMultiple = 2` 做 nearest-neighbor 2× 整数倍放大**：与 Brogue CE HD 模式同姿态；字体短名单与 16×16 case 完全一致，决定耦合最低；实现只是在 `Game.LoadFont` 之后多设一个全局尺寸倍率。
   - **倾向 B2**——决定耦合最低、anchor 不漂移、实现成本最低。Plan 阶段只在 B2 下所有候选字体均被否决时回落到 B1。
3. **变体 glyph 可读性 audit 怎么做**——不能等 build 出来再"瞄一眼"。建议 plan 阶段把字体候选直接渲染到一张包含装饰 floor variant + 玩家 + 怪物 + 物品 + UI 文字的 baseline 截图上，对比当前 palette-brogue-pass 落地版本，判断 variant 是否仍然可见、是否仍然不像 gameplay 信号（继承 palette brief 的 F2）。
4. **资产管线落地姿态**——SadConsole 的字体既可以走 `LoadFont(path)` 直接读 `.font` JSON + PNG（最简单），也可以走 MGCB content pipeline 烘焙。倾向 **直接 LoadFont**——hobby 节奏 + 单一字体 + 静态资产，MGCB 是 overengineering。Plan 阶段只在 `LoadFont` 跑不通时才升级到 MGCB。

## Constraints

- **技术栈**：.NET 8，单一 `DungeonDescent` namespace，`<ImplicitUsings>` + `<Nullable>` 启用——与前两份 brief 一致。
- **依赖**：不引入新的第三方依赖。SadConsole 已内建字体加载能力，字体资产仅是 PNG + JSON 文件。
- **核心兼容性**：`Game.cs` / 战斗 / 移动 / FOV / BFS / Map / 实体 / Item 一律不动。改动文件预期集中在：`Program.cs`、`src/UI/RootScreen.cs`、`DungeonDescent.csproj`（资产引用），加 `assets/fonts/` 新目录。
- **Layout 强约束**：`src/Core/Layout.cs` 的 60×26 cell grid **不变**。变化只允许发生在 cell 像素渲染尺寸（即窗口物理大小），不允许影响 cell 数量或 surface 划分（title 1 / map 20 / status 2 / log 3）。
- **Cell 渲染尺寸前置（取代 plan 自由决定项）**：用户偏好 **32×32 cell**（路径 B2：16×16 源字体 × SadConsole `SizeMultiple = 2`）。Plan 阶段第一步**必须以用户日常工作显示器分辨率为前置输入**做适配核查：
  - **≥ 1440p（2560×1440 / 4K / 5K）**：直接执行 32×32，1920×832 物理窗口在屏内充裕。
  - **1080p（1920×1080）**：临界。需用户显式签字接受"窗口几乎贴满宽度 + 高度仅余 ~248px"；不接受则**fallback 至 24×24**（1440×624 物理窗口，1080p 上舒适）。
  - **< 1080p（1366×768 等）**：**强制 fallback 至 24×24**——32×32 不放下，无谈判余地。
  - 24×24 走路径 **B3**（16×16 源 × `SizeMultiple = 1.5`，但 SadConsole 一般只支持整数倍——若如此则用 24×24 源字体或维持 16×16 cell；plan 阶段验证）。
- **可读性硬下限**：变体 floor `,` 与 `'` 必须仍可瞬识；玩家 `@` 在战斗中必须仍最显眼；数字字符在 status 行（HP / ATK / DEF / EXP / G / Sc）必须仍清晰可读。任何字体候选未通过这三关一律否决。
- **License 硬约束**：所选字体必须有明确公开 license；plan 阶段需在 `assets/fonts/` 目录里加 `LICENSE` 或 `README.md` 显式记录来源 + 许可证文本。即便 hobby 不发行也照办——日后想分享时不必返工。
- **时间盒**：与前两份 brief 对齐——hobby 节奏，无硬截止；plan 阶段切成"字体接入 → 候选比对 → 选定 → variant audit → cell 尺寸定型"的独立可停手里程碑。

## Context

- **位置**：是 `docs/briefs/palette-brogue-pass.md` 的延续——那篇启动了视觉打磨第一步（色板 + 装饰 variant），本 brief 启动第二步（字体）。`docs/briefs/pixel-ui-rewrite.md` 是这两份的共同上游，明确把字体留在 deferred。
- **视觉锚点继承**：(ii) Brogue 风跨三份 brief 一致；tileset 永久排除。本 brief 不重新审议锚点。
- **学习目标对齐**：pixel-ui-rewrite 把"SadConsole 内容管线 / 字体烘焙"标为 (c) 学习目标的核心兑现节点。字体步骤恰是那条学习曲线的关键一段——内容管线 / 字体资产格式 / cell 尺寸与 surface 关系，本步骤一次集齐。
- **F1 跨 brief 继承**：上一份 brief 把"叙事失败"（"包装成视觉打磨完成"）elevated 到 Failure Modes 顶部，本 brief 同等保留。色板 + 字体一起仍未含动画，不构成 Brogue 完整视觉。
- **变体 glyph 强依赖字体形态**：palette-brogue-pass 让 FloorMossy / FloorCracked 用 `,` 与 `'` 区分；这两个 glyph 在不同字体下形态差异显著，本 brief 对字体的可读性 audit 是上一步落地有效性的隐式回归测试。

## Discovery Notes

- **本 brief 跳过了正式 Discovery 对话**：用户在 `/kenspc-brief` 调用中带的 `<system-reminder>` 显式指示"不要停下问问题"。这意味着本 brief 是 Claude **直接基于既有上下文推断**写出的，不是与用户结构化对谈的产物。下次若有时间，仍建议用一轮 Discovery 对以下推断点显式签字。
- **下一步选 "字体" 而非 "动画" 的推断依据**：① `palette-brogue-pass.md` 的 Deferred 段把字体列为 step 2、动画列为 step 3，用户在那次 Discovery 中"未反对 deferred 三项排序"——视为弱 commit。② 字体单点替换不会与 palette 改动产生视觉 race condition；动画放在字体定型之后做，jitter 的 baseline 才稳定。③ F1 跨 brief 继承——把"字体 + 动画"打包做触发 F1 的概率最高。
- **被 deprioritize 的备选下一步**：
  - **颜色抖动 / 动画首版**：被 push 到 step 3，理由如上。
  - **多 floor variant / wall variant**：palette-brogue-pass brief 显式排除（F2 风险 + 房间辨识性），不重新审议。
  - **HUD / status 行 / overlay 视觉打磨**：与 Brogue anchor 无强对齐——Brogue 的 HUD 风格本身朴素，与玩家屏幕主体的视觉语言强绑定（同字体同色板）。等字体落地后这一项基本"附赠"完成。
  - **过场 / 标题画面美化**：尚无标题画面，不是当前作业入口。
- **未明确决策项（plan 阶段处理）**：
  - 字体来源：A（直接采 Brogue）/ B（第三方 OFL/MIT 像素字体）/ C（自制 CP437 改型）三选一。倾向 B。
  - 资产管线：`Game.LoadFont` 直读 / MGCB 烘焙 二选一。倾向直读。
  - 候选 B 字体的 short list（plan 阶段并排比对，本 brief 不预先指定）。
- **本 brief 写完后的用户追加偏好（已合并）**：
  - **Cell 渲染尺寸**：用户在 brief 初稿写完后追加偏好 **32×32**——已固化进 Scope / Hard Part #2 / Constraints。原 brief 的 16×16 倾向被取代。
  - **实现路径**：32×32 走路径 B2（16×16 源 × `SizeMultiple = 2`，与 Brogue CE HD 模式同姿态），不走 B1（源 32×32 字体），以避免进一步收窄字体短名单。
  - **fallback 触发条件**：1080p 屏需用户签字 / 1366×768 强制退回 24×24（plan 阶段决策的硬性输入）。
  - 触发更新的对话原文："如果我 prefer 尺寸是 32×32 可以吗" → 经 Claude 确认 1920×832 物理窗口的代价后用户回 "好"。
- **管线（Content Pipeline）澄清**：用户在追加偏好同一轮提问"什么是管线"——这是 MonoGame Content Builder（MGCB）的简称，编译期把 PNG/WAV/TTF 等源资产打包成 `.xnb` 二进制再让运行时读。本 brief Hard Part #4 倾向直读不走管线——hobby 单字体规模无 ROI，已与用户对齐。
- **Anchor 一致性核查**：本 brief 不重审 (ii) Brogue 风 / tileset 永久排除——三份 brief 一致即可，再问一次是噪声。
