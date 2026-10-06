# 简体中文 i18n 首期 GUI 实施 Plan

日期：2026-10-05。依据：`docs/specs/i18n-zh-CN.md`。计划编写时产品代码基线：`e2499ac`。

状态：run `i18n-zh-cn-gui-20261005` 执行完毕（2026-10-05→2026-10-06）：TASK-01..07 全部完成并登记证据，最终 Gate 执行完毕（全量 354 测试通过、Release 构建 0 警告 0 错误、累计审查 0 findings）。spec 首期完成条件中依赖人工真机验证的行保持未完成（托盘交互、多显示器、100%/150%/200% DPI、编辑/文件对话框实时结果），详见 run report。执行模式按用户指令自 TASK-02 起为单审查模式。

## 范围与执行约定

- 覆盖 spec 的全部首期 GUI 汉化点：英文/简体中文、跟随系统和显式选择、重启生效、工作区管理、窗口详情、任务栏、托盘、文件对话框、状态与已知业务错误。
- CLI 留在独立计划 `docs/plans/i18n-zh-CN-cli.md`；执行本文件不包含第二阶段，不存在可默默跳过的可选任务。
- 按垂直功能切片组织：每项交付对应场景的资源、显示接入、必要数据兼容和行为验证；不先一次性翻译所有文案再集中接入界面。TASK-01 交付实际可操作的语言设置与启动路径，而非仅建立资源目录。
- 执行顺序：TASK-01 → TASK-02 → TASK-03 → TASK-04 → TASK-05 → TASK-06 → TASK-07。所有任务严格串行；同一文件可被不同任务修改，但仅限当前任务场景。
- 默认未来执行模式为 `strong-review`：每项使用新的实现子代理，调用 `$implement` 并遵循 `$tdd`；该代理负责本项全部修复。审查代理只读，可在整个 run 复用，每轮调用 `$code-review`，分别输出 Standards 和 Spec 结果。
- 每项依次完成实现、针对性测试与编译、双轴审查、修复与复验、checkpoint commit，再进入下一项。实现/修复提交可早于审查，但只有审查通过和证据登记后才算任务完成。
- 默认所有 P0/P1/P2 问题阻塞；本 plan 不授权跳过、替换实现代理、降级为 single-review 或创建 worktree。相同问题连续三轮修复未解决时按技能暂停并报告证据。
- spec 是需求来源；plan 不复制整套翻译表，不更改术语、语言映射、兼容要求或首期边界。规范文本、验收条件、依赖与任务边界在 run 中不可变。

## 实施前检查与证据约定

未来启动必须由用户提供明确的 `spec=<path>` 和 `plan=<path>`。执行前确认 spec/plan 已定稿，并按技能检查：

1. 读取明确给定的 spec/plan，读取存在的领域上下文及 ADR，计算原始 SHA-256 和排除允许状态字段后的不可变合同 SHA-256。
2. 校验每个任务块、唯一 ID、五个必需字段和无环依赖图；不得把本文说明标题识别为任务。
3. 获取实际 HEAD/当前分支和工作区状态。编写计划时 spec 是已有未跟踪文件；实施前必须重新检查。脏文件默认阻塞，不自动暂存、提交或清理规划材料；只有用户明确确认并登记快照后才能继续。
4. 默认当前工作区/分支；只有明确要求才创建隔离 worktree。
5. 编排者创建 `.scratch/subagent-driven-development/<run-id>/state.json` 和 `report.md`，使用技能 ledger schema；本次规划不创建运行记录。
6. 每轮记录任务 baseline、代理、实现/修复提交、验证命令及输出、审查 finding ID/优先级、豁免（如有）。review diff 为该任务 baseline 到 HEAD；最终 review 使用 run baseline 到 HEAD。

每项 Checklist 的证据行允许在未来 run 中更新：`Verification` 记录实际结果，`Review` 记录两轴报告引用，`Commits` 记录完整 SHA。不得预填 PASS 或用命令计划代替实际验证。

## 测试和变更边界

- 主要自动化入口为无 UI 依赖的本地化门面，配合现有设置/配置服务和快捷键公共接口。复用现有 xUnit 临时文件模式，不为每个标签新增一套测试接口。
- 本地化门面与资源可放在 Core 的独立 Localization 区域，供现有 Core 测试项目直接引用；Core 窗口控制和配置标识保持语言无关。WPF 绑定适配器只负责消费门面，不持有第二份翻译表。
- 采用进程内固定的有效 UI 语言。保存的下次启动偏好与本次有效语言分开；不将设置选择器绑定到会立即改变所有文案的全局状态。
- 每项只对真实逻辑行为使用测试先行。静态标签和布局通过资源审查、Release 编译与人工场景验证；不制造一项标签对应一项测试的镜像断言。
- GUI/托盘实际操作使用专门测试数据。自动测试不恢复或关闭真实用户窗口，不重启 Explorer。未具备多显示器或对应 DPI 条件时记录未验证，不标记通过。
- 推荐针对性命令：`dotnet test tests/WorkspaceSwitcher.Tests/WorkspaceSwitcher.Tests.csproj -c Release --filter "<本项测试类>"`，随后 `dotnet build WorkspaceSwitcher.sln -c Release`。测试类名称是拟定名，以实际新增测试替换；必须记录实际命令。

## Task TASK-01: 语言设置保存与下次启动生效

### Goal

用户可以选择跟随系统、English 或简体中文并保存，下一次启动按该选择加载实际界面资源；语言机制具有英文回退。

预计 3–4 人时。主要触点：AppSettings、SettingsService、App 启动、MainWindow、MainViewModel 的设置保存路径，以及新的 Localization 资源/门面和 WPF 取值适配器。

### Acceptance

- language 持久化值为 system/en/zh-CN；旧设置缺字段和无效值均按 system 处理，保存语言不丢失已有恢复设置、全局固定项和最近工作区。
- 显式选择优先于系统 UI 语言；自动模式识别简体中文及其区域变体，繁体和其他系统语言使用英文。不能只凭 `zh` 前缀选择中文。
- 首次创建主窗口、弹窗或托盘前确定本次有效语言；检查现有 StartupUri 和主窗口构造路径，避免 InitializeComponent 早于资源初始化。
- 设置区实际出现语言选项及重启生效提示。选择新语言只改变保存偏好，当前会话原有文案和托盘不部分切换，也不自动重启。
- 英文中性资源与 zh-CN 资源能通过统一门面返回实际文本；中文资源缺项会取得英文内容，而非资源键或空字符串。
- 本项只翻译新增语言设置控件及必要的重启说明；后续场景的英文在本任务期间属于尚未实施的任务范围。

### Dependencies

- 无。

### Test seam

- 本地化门面：显式提供偏好和系统 UI culture，断言有效语言和代表性文本。覆盖 en、zh-CN、zh-Hans、zh-SG、zh-TW、zh-HK、缺失及无效偏好。
- SettingsService 公共 Load/Save：复用 SettingsServiceTests，旧 JSON 加载、语言往返、与已有选项连续保存后仍保留新偏好。
- 人工启动/退出/重启：验证启动顺序和本次/下次语言分离。不要把 WPF 正确加载资源当作 Core 单元测试已经证明的行为。

### Checklist

- [x] 针对语言决策、资源回退和设置兼容先写失败的行为测试，完成最小实现并使其通过。
- [x] 接入启动资源与设置选择器，检查全部 SaveCurrentSettings 路径不会重置 language。
- [x] 运行本项测试及 Release 编译，实际验证语言保存和下次启动生效。
- [x] 完成 Standards/Spec 双轴审查，由本项原实现代理修复并复验至无未豁免问题。（双轴审查 0 findings，无修复轮。）
- [x] 登记本项 checkpoint commit、验证证据和审查结果后标记完成。

Verification: `dotnet test tests/WorkspaceSwitcher.Tests/WorkspaceSwitcher.Tests.csproj -c Release --filter "FullyQualifiedName~LocalizationTests|FullyQualifiedName~SettingsServiceTests"` → 48 通过/0 失败；`dotnet build WorkspaceSwitcher.sln -c Release` → 0 警告 0 错误。人工项未验证：真实首次启动/保存/重启生效、会话内切换不部分生效、设置卡渲染。
Review: 双轴审查 Task01Review（Standards 0、Spec 0，overall correct，confidence 0.9）。
Commits: 644364e。

## Task TASK-02: 中文创建/编辑工作区与快捷键兼容

### Goal

用户能通过完整中文弹窗创建或编辑工作区、选择快捷键，保存后业务仍使用原有内部值。

预计 3–4 人时。主要触点：WorkspaceDialog、ProfileItemViewModel 的快捷键显示与编辑绑定、HotkeyHelper 消费端和配置往返测试。

### Acceptance

- 创建/编辑两种模式的标题、说明、名称/描述/图标/快捷键标签、任务栏保存说明、主按钮、取消和空名称校验使用本次语言。
- Auto (1-5)、None (Disabled)、None 分别显示自动分配（1–5）、禁用快捷键、无；Ctrl/Alt/Shift/Win、字母、数字及 F 键保持技术名称。
- 下拉选项用稳定值和显示名分离的绑定；编辑旧配置时可正确选中，保存后内部值不变，中文标签不会传给解析器。
- No Hotkey 在 UI 显示“未设置快捷键”，保持现有公共解析/格式化接口的业务兼容，首期 CLI 不因此意外改成中文。
- 工作区名称和描述原样保存，包括中文、emoji、大括号；旧名称与描述不批量翻译。
- 创建/编辑成功与失败的对应状态模板一并本地化，形成可验收的完整操作切片；任务栏数量采用完整模板而非英文片段拼接。

### Dependencies

- TASK-01。

### Test seam

- 复用 HotkeyHelperTests 的公共解析断言，以及 ProfileServiceTests 的临时配置往返模式：中文显示选项绑定保存后仍得到相同虚拟键和修饰键。
- 经本地化门面验证创建/编辑结果模板，包括 0/1/多个窗口、带括号和大括号的参数。
- 人工执行创建、编辑旧配置、空名称校验、禁用/自动/显式快捷键选择；注册实际热键仅在受控环境验证。

### Checklist

- [x] 对内部值保存、旧选项解析及动态结果模板先写行为测试并完成实现。
- [x] 提取弹窗全量文案，接入选项显示/稳定值绑定和创建/编辑状态输出。
- [x] 运行相关 HotkeyHelper、ProfileService 和本地化测试及 Release 编译，记录弹窗场景验证。
- [x] 完成双轴审查，原实现代理修复并复验所有未豁免问题。（按用户指令自本任务起为单审查模式；审查 0 findings，无修复轮。）
- [x] 登记 checkpoint commit、验证和审查证据后标记完成。

Verification: `dotnet test tests/WorkspaceSwitcher.Tests/WorkspaceSwitcher.Tests.csproj -c Release --filter "FullyQualifiedName~HotkeyHelperTests|FullyQualifiedName~ProfileServiceTests|FullyQualifiedName~LocalizationTests"` → 121 通过/0 失败；`dotnet build WorkspaceSwitcher.sln -c Release` → 0 警告 0 错误（编排者复验）。人工项未验证：弹窗双模式渲染/DPI、创建与旧配置编辑全流程、空名称校验弹窗、真实热键注册、英文视觉回归。
Review: 单审查模式 Task02Review（0 findings，overall correct，confidence 0.92）。
Commits: 05c28fd。

## Task TASK-03: 中文工作区概览、统计和恢复设置

### Goal

用户可以看懂主界面工作区列表、空选中状态、数量、保存时间和各恢复选项。

预计 2–3 人时。主要触点：MainWindow 的头部/列表/详情摘要/恢复设置、ProfileItemViewModel 的回退描述与时间显示、相关本地化模板。

### Acceptance

- spec 主窗口/工作区管理表中的标题、说明、运行状态、当前使用标记、按钮及工具提示完成双语接入；品牌与按键保留。
- 无选中工作区、空描述、当前工作区为无等回退文本使用本次语言，不能遗漏 XAML FallbackValue。
- 窗口/显示器/固定项数量和保存时间完整格式化，不留下 `{0}w` 或英文 StringFormat。
- 今天/昨天/近期/较早日期按 spec 显示；日期判定使用本地时间，中文较早日期为 yyyy年M月d日 HH:mm，英文保持原显示规则。
- 所有恢复设置标题、开关说明和全局固定项说明中文一致；修改开关仍保留语言选择和其他设置，原功能语义不变。
- 此任务仅替换恢复/导出/更新等入口标签；这些操作的结果、对话框及错误分别由 TASK-06 覆盖，任务边界明确。

### Dependencies

- TASK-02。

### Test seam

- 本地化门面提供实际统计与日期格式化结果，用固定参考时间覆盖跨日、昨天、近期和较早日期，不依赖测试当天。
- 现有设置公共读写验证语言偏好与恢复选项共存。
- 人工查看无选中/有选中/空描述/长中文名称，以及英文回归；静态标签无需逐控件测试。

### Checklist

- [x] 对时间与统计实际输出先写行为测试，再实现资源模板和回退取值。
- [x] 接入主窗口概览及恢复设置，检查静态文本、绑定格式和 FallbackValue。
- [x] 运行针对性格式化/设置测试及 Release 编译，记录概览和设置人工验收。
- [x] 完成双轴审查，原实现代理修复并复验所有未豁免问题。（单审查模式；审查 0 findings，两个判断点均裁定合规，无修复轮。）
- [x] 登记 checkpoint commit、验证和审查证据后标记完成。

Verification: `dotnet test tests/WorkspaceSwitcher.Tests/WorkspaceSwitcher.Tests.csproj -c Release --filter "FullyQualifiedName~DisplayFormattingTests|FullyQualifiedName~SettingsServiceTests"` → 40 通过/0 失败；`dotnet build WorkspaceSwitcher.sln -c Release` → 0 警告 0 错误（编排者复验）。人工项未验证：无选中/选中/空描述/长中文名称视图、英文视觉回归、{0} 个窗口宽度自适应与各 DPI。
Review: 单审查模式 Task03Review（0 findings，overall correct，confidence 0.9；{0}w 英文模板值与 hero 元数据行隐藏均裁定合规）。
Commits: a4b3371。

## Task TASK-04: 中文窗口详情与显示器选择

### Goal

用户能使用中文查看及编辑窗口状态、目标显示器、坐标和尺寸，保存的布局保持原数据语义。

预计 2–3 人时。主要触点：WindowItemViewModel、MainWindow 窗口详情区、MonitorService 返回数据的显示消费端。

### Acceptance

- 窗口详情标题、目标状态/显示器、坐标/尺寸标签、自动保存和移除说明覆盖 spec 全表。
- 状态标签与下拉选项都显示正常/最大化/最小化，绑定和保存继续使用原枚举，禁止翻译枚举名或改变其值。
- 主/非主显示器名称由结构化索引、标识、分辨率和坐标格式化；不解析英文 MonitorOption.Name，也不改变 Core 枚举显示器行为。
- 缺失可执行路径的系统/后台进程说明中文可读；真实窗口标题、应用名、路径、数字坐标不翻译。
- 改状态/坐标/显示器后保存并重载，底层值与原行为一致；移除只处理当前工作区配置。

### Dependencies

- TASK-03。

### Test seam

- 通过同一本地化门面验证状态/显示器实际显示文本，输入模拟主/非主显示器数据，不依赖机器硬件枚举结果。
- 复用 WindowPlacementTests/配置往返验证状态枚举和负坐标等数据不受语言影响。
- 人工验证状态下拉绑定、自动保存及显示器选择，真实多显示器环境不足时如实记录。

### Checklist

- [x] 先写状态/显示器输出与布局数据兼容行为测试，再完成显示层接入。
- [x] 提取窗口详情文案，分离枚举/显示名及显示器结构数据/名称。
- [x] 运行针对性格式化、WindowPlacement 和配置测试及 Release 编译，记录窗口编辑验收。
- [x] 完成双轴审查，原实现代理修复并复验所有未豁免问题。（单审查模式；审查 0 findings，无修复轮。）
- [x] 登记 checkpoint commit、验证和审查证据后标记完成。

Verification: `dotnet test tests/WorkspaceSwitcher.Tests/WorkspaceSwitcher.Tests.csproj -c Release --filter "FullyQualifiedName~WindowDetailsDisplayTests|FullyQualifiedName~ProfileServiceTests|FullyQualifiedName~WindowPlacementTests"` → 35 通过/0 失败；`dotnet build WorkspaceSwitcher.sln -c Release` → 0 警告 0 错误（编排者复验）。人工项未验证：状态下拉绑定、自动保存、显示器选择与真实多显示器环境。
Review: 单审查模式 Task04Review（0 findings，overall correct，confidence 0.93；仅记录非 finding：零显示器回退名格式随结构化格式化略有差异）。
Commits: 6d962f1。

## Task TASK-05: 中文任务栏配置与操作结果

### Goal

用户能区分“全局固定”和“仅此工作区”，理解保存、应用及同步任务栏配置的入口与结果。

预计 2–3 人时。主要触点：TaskbarItemViewModel、MainWindow 任务栏区域、MainViewModel 任务栏操作状态输出。

### Acceptance

- 任务栏页标题、启用说明、保存/应用/同步按钮及工具提示、移除提示和无配置空状态完成双语接入。
- Static 统一显示全局固定，Workspace Only 显示仅此工作区；动态状态、设置区、富文本说明术语一致。
- 原先分段英文帮助文本按完整句子翻译，强调关键术语不破坏中文语序。
- 保存固定项、应用成功/失败、无配置、同步成功/失败及标记范围变化的状态模板采用本次语言。
- 切换固定范围并保存/重载后 IsStatic 等内部字段与原功能一致，未翻译快捷方式文件名、路径和真实应用名称。

### Dependencies

- TASK-04。

### Test seam

- 本地化门面验证全局/专用状态、0/1/多个固定项及成功/失败模板，复用配置往返测试验证 IsStatic 持久化。
- GUI 验证无配置/有配置两种状态。自动测试只验证数据与输出，不实际改用户任务栏或重启 Explorer；实际应用行为仅在受控环境记录。

### Checklist

- [x] 先写范围状态、计数输出和配置保存行为测试，再接入资源。
- [x] 完成任务栏显示、完整帮助说明和本场景操作结果的双语覆盖。
- [x] 运行针对性格式化/配置测试及 Release 编译，记录任务栏 GUI 验收及副作用测试条件。
- [x] 完成双轴审查，原实现代理修复并复验所有未豁免问题。（单审查模式；审查 0 findings，三个判断点均裁定合规，无修复轮。）
- [x] 登记 checkpoint commit、验证和审查证据后标记完成。

Verification: `dotnet test tests/WorkspaceSwitcher.Tests/WorkspaceSwitcher.Tests.csproj -c Release --filter "FullyQualifiedName~TaskbarDisplayTests|FullyQualifiedName~ProfileServiceTests"` → 80 通过/0 失败，另全量 273 通过/0 失败；`dotnet build WorkspaceSwitcher.sln -c Release` → 0 警告 0 错误（编排者复验）。人工项未验证：任务栏页双语渲染、无配置/有配置状态、状态栏实际结果、真实任务栏应用行为（副作用仅限受控环境，自动化未触碰任务栏/Explorer）。
Review: 单审查模式 Task05Review（0 findings，overall correct，confidence 0.93；英文两处快照入口统一为 Snapshot Current Taskbar、帮助文本句内分段、emoji 内嵌均裁定合规）。
Commits: c316e90。

## Task TASK-06: 中文恢复/文件操作、业务错误与托盘

### Goal

用户从主窗口、快捷键或托盘操作时能看到一致的中文结果，导入导出和已知失败也有明确说明。

预计 2–3 人时。主要触点：MainViewModel 尚未覆盖的恢复/切换/更新/删除/导入导出状态、MainWindow 的托盘通知、TrayIconService、App 异常入口及必要的 Core 已知错误原因标识。

### Acceptance

- 恢复、切换、更新当前布局、删除、导入、导出的成功/失败状态和初始后台状态完成双语覆盖；包含恢复数、关闭数、原工作区名及可选任务栏结果的完整模板。
- 文件对话框标题和文件类型说明本地化，*.json、*.*、扩展名及用户文件名不变。
- 已知名称为空、配置不存在、源文件不存在、格式无效、快捷键注册失败按稳定原因映射中文，不通过匹配英文异常正文判断。
- 已知原因标识只作最小必要补充，不重构整个 Core；未知/外部异常外层中文，原始消息与堆栈仍保留，现有异常处理行为不变。
- 托盘悬停、菜单、无工作区提示、后备恢复路径、切换/快照/关闭到托盘通知采用同一本次语言。Hotkey/Tray 内部来源标识不变，仅显示中文。
- 托盘菜单重建和保存下次语言偏好之后仍使用本次语言；快照名称保留 Quick_时间 标识，新描述可以本地化，旧描述不改写。
- TASK-02/05 已完成的结果模板仅为修复一致性而调整，不重复建立消息实现。

### Dependencies

- TASK-05。

### Test seam

- 本地化门面验证结果组合、来源和已知原因映射、中文/大括号名称参数；通过现有 ProfileService 公共接口与临时文件触发可控失败，断言原因与中文输出。
- 文件筛选实际模式、用户文件名和路径保持原值的行为检查。
- 人工验证文件对话框、托盘菜单/重建/通知/关闭到托盘；恢复与关闭真实窗口仅在受控测试工作区验证，不将硬件操作纳入无隔离自动测试。

### Checklist

- [x] 先写动态结果和已知错误映射的行为测试，再完成中文资源和边界映射。
- [x] 接入文件对话框、托盘全部入口及异常标题/外层说明，复查未覆盖状态分支。
- [x] 运行相关格式化、ProfileService、HotkeyHelper 测试及 Release 编译，记录托盘与文件操作验收。
- [x] 完成双轴审查，原实现代理修复并复验所有未豁免问题。（单审查模式；1 项 P3 发现 F1（NameEmpty 消息丢参数后缀，CLI 非字节一致）已由原代理修复 459ea00 并经复验 CLOSED；三个判断点裁定合规。）
- [x] 登记 checkpoint commit、验证和审查证据后标记完成。

Verification: `dotnet test tests/WorkspaceSwitcher.Tests/WorkspaceSwitcher.Tests.csproj -c Release --filter "FullyQualifiedName~OperationDisplayTests|FullyQualifiedName~LocalizationTests|FullyQualifiedName~ProfileServiceTests"` → 156 通过/0 失败，另全量 353 通过/0 失败；`dotnet build WorkspaceSwitcher.sln -c Release` → 0 警告 0 错误（编排者复验）。人工项未验证：文件对话框实际渲染、托盘悬停/菜单/重建/通知/关闭到托盘、真实恢复切换结果、异常弹窗、热键注册失败实际浮现。
Review: 单审查模式 Task06Review（1 findings：F1 P3 已修复并 CLOSED；判断点：切换结果按“有/无原工作区”组合矩阵合规、OperationFailureException 为最小必要补充合规、严重错误标题措辞有据）。
Commits: 0196e07（test）、89f04a9（feat）、459ea00（fix）。

## Task TASK-07: 中文可读布局与首期整体验收

### Goal

完成中文字体、长文本和高 DPI 的实际布局验证，使首期 GUI 在中英文下均达到 spec 的完成条件。

预计 2–4 人时。主要触点：App 全局样式、MainWindow/WorkspaceDialog 的字体及尺寸布局，以及必要的验收文档。

### Acceptance

- 中文字体有适当 Windows 字体回退，无缺字；说明可换行，主窗口最小 980×620、弹窗 500 宽的关键按钮/标签无重叠遮挡。
- 100%/150%/200% DPI 下检查主界面、弹窗、窗口详情、任务栏和语言设置；保留原主题及图标语义，必要时允许滚动。
- 按 spec 清单复查所有静态、绑定格式、回退和动态文案；残留英文仅为品牌、按键、技术标识、用户数据或外部系统信息等明确例外。
- 中英文、跟随系统、改语言后重启、旧配置和快捷键的完整用户路径通过验收；现有功能未因字体/布局修改退化。
- 保存逐场景结果和未验证条件，不用编译通过或截图代替功能验证。无法验证的必需场景保持未完成。
- 本项只负责字体/布局与覆盖验收。若发现前项逻辑缺陷，按技能路由回最早受影响任务，由原代理修复，不在本任务偷偷扩大边界。

### Dependencies

- TASK-06。

### Test seam

- spec 人工验收矩阵对应的实际应用场景和布局截图；对资源残留做静态辅助检查，人工判断例外，不能把字符串扫描当作唯一验收。
- 全量现有及新增公共行为测试；布局变化通常不新增镜像单元测试，发现行为退化时增加对应公共接口回归用例。

### Checklist

- [ ] 完成中文字体、长文案、最小尺寸和各 DPI 验证，并修复本项布局问题。（字体/长文/最小尺寸 980×620 与 500 宽弹窗已在 125% 真机验证；100%/150%/200% DPI 受系统缩放设置限制未验证，保持未完成。）
- [x] 整理 spec 首期汉化点到完成任务的覆盖记录，列明例外和实际未验证条件。
- [x] 运行全量 Release 构建及测试，完成中英文和旧配置人工回归。
- [x] 完成本项双轴审查，路由并复验此前任务的逻辑发现，清除所有未豁免问题。（单审查模式 Task07Review 0 findings；路由缺陷 8fb3156 已修复并复验；最终累计审查 0 findings。）
- [x] 登记本项 checkpoint commit、验证和审查证据后标记完成。

Verification: 全量 `dotnet test tests/WorkspaceSwitcher.Tests/WorkspaceSwitcher.Tests.csproj -c Release` → 354 通过/0 失败；`dotnet build WorkspaceSwitcher.sln -c Release` → 0 警告 0 错误。真机冒烟（125% DPI、单显示器、系统 zh-CN）：跟随系统/显式 English/显式 简体中文 三路径、旧设置（无 language 字段）兼容且既有字段保留、profile 文件跨语言运行字节一致（sha256）、980×620 最小尺寸与 500 宽弹窗长文换行无遮挡、中文字形无缺字、无乱码与占位符残留；TASK-03 hero 绑定缺陷复验通过（0 错误弹窗、统计非空、随选择原子更新）。人工矩阵未验证项：托盘菜单/通知/关闭到托盘、多显示器、100%/150%/200% DPI、编辑弹窗与文件对话框实时操作结果（逐行结果与原因见 run report）。
Review: 单审查模式 Task07Review（0 findings，overall correct，confidence 0.92）。
Commits: b8a6a77（占位符一致性测试）、83f37ba（字体回退/换行/状态栏）、8a9c3dd（弹窗换行修复）；路由缺陷修复 8fb3156（TASK-03，经冒烟复验）。

## Spec 覆盖索引与工时

| Spec 范围 | 主责任任务 | 说明 |
|---|---|---|
| 语言选择/回退/保存/启动 | TASK-01 | 后续均消费同一机制 |
| 创建/编辑、快捷键、用户数据保留 | TASK-02 | 完整弹窗到保存结果 |
| 主界面、回退、统计、日期、恢复设置 | TASK-03 | 入口按钮本地化；操作结果由 TASK-06 交付 |
| 窗口状态、显示器、坐标、尺寸 | TASK-04 | 显示与持久化值分离 |
| 任务栏、固定范围、帮助与结果 | TASK-05 | 实际副作用需受控验证 |
| 恢复/文件操作、已知错误、托盘 | TASK-06 | 包含关闭通知和后备分支 |
| 中英布局、DPI、全量覆盖与兼容验收 | TASK-07 | 首期最终用户可读性 |
| CLI | 独立第二阶段 plan | 不由本 run 执行 |

首期预计 16–24 人时，与 spec 一致；包括针对性测试、审查修复与验收，实际审查轮数或设备条件可能影响用时。

## 最终 Gate（不新增任务块）

所有七项完成后，编排者依技能执行：

- 全量 `dotnet build WorkspaceSwitcher.sln -c Release` 和 `dotnet test WorkspaceSwitcher.sln -c Release --no-build`，保留实际输出。
- 由只读审查代理对 run baseline 到 HEAD 做累计 `$code-review`，保留 Standards/Spec 两轴。任何未豁免发现路由至最早受影响任务，重开原代理修复；必要时重跑下游循环。
- 确认每项 checkpoint commit、针对性验证、累计审查和必需人工验收均有证据，工作区无无法解释的产品变更。执行前已登记脏文件和 ledger 只在记录明确时允许保留。
- 输出 run ID、spec/plan 合同与原始 hash、按顺序的任务提交、实现/审查/修复轮次、所有豁免、命令结果及最终工作区状态。若任何必要验证缺失，不报告首期完成。
