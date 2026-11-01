// Added for the Intelmeter fork, 2026-10-29. GPL-2.0-only; see ../../LICENSE.
// SPDX-License-Identifier: GPL-2.0-only
// Profile Sort object regression matrix added 2026-10-30 under the same license.
// Clock native combo regression matrix added 2026-10-30 under the same license.
// All-object combo coverage added 2026-10-30 under the same license.
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.ManufacturerSettings;
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

// Offline visual/geometry audit. Workers have no transport. The focused Calendar
// mode also invokes nonmodal local Remove actions on disposable in-memory models.
internal static partial class Program
{
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    static readonly Assembly Ui = typeof(ModernPresentation).Assembly;
    static readonly Size[] Sizes = [new(1100, 760), new(620, 560)];
    static readonly DateTime SampleTime = new(2026, 10, 29, 14, 30, 0, DateTimeKind.Utc);
    static readonly string[] SimpleViews = ["GXDLMSDataView", "GXDLMSRegisterView", "GXDLMSExtendedRegisterView", "GXDLMSDemandRegisterView", "GXDLMSHdlcSetupView", "GXDLMSTcpUdpSetupView", "GXDLMSMacAddressSetupView"];
    static readonly string[] RoundtripViews = ["GXDLMSDataView", "GXDLMSRegisterView", "GXDLMSExtendedRegisterView", "GXDLMSDemandRegisterView", "GXDLMSClockView", "GXDLMSProfileGenericView", "GXDLMSAssociationLogicalNameView"];
    // Visible combos observed across the prior 73-view/147-fixture audit, plus
    // Data's metadata-driven enum editor. Generic hidden GXValueField children
    // do not count as visible combos. Security version 0 is an extra fixture.
    static readonly string[] ComboViews = ["GXDLMSAccountView", "GXDLMSActionScheduleView", "GXDLMSAssociationLogicalNameView", "GXDLMSAssociationShortNameView", "GXDLMSAutoAnswerView", "GXDLMSAutoConnectView", "GXDLMSChargeView", "GXDLMSClockView", "GXDLMSCommunicationPortProtectionView", "GXDLMSCompactDataView", "GXDLMSCreditView", "GXDLMSDataView", "GXDLMSDemandRegisterView", "GXDLMSDisconnectControlView", "GXDLMSExtendedRegisterView", "GXDLMSG3Plc6LoWPanView", "GXDLMSGSMDiagnosticView", "GXDLMSHdlcSetupView", "GXDLMSIECLocalPortSetupView", "GXDLMSIECOpticalPortSetupView", "GXDLMSIecTwistedPairSetupView", "GXDLMSIp6SetupView", "GXDLMSLimiterView", "GXDLMSMBusClientView", "GXDLMSMBusDiagnosticView", "GXDLMSMBusMasterPortSetupView", "GXDLMSMBusSlavePortSetupView", "GXDLMSModemConfigurationView", "GXDLMSNtpSetupView", "GXDLMSProfileGenericView", "GXDLMSPushSetupView", "GXDLMSRegisterActivationView", "GXDLMSRegisterMonitorView", "GXDLMSRegisterView", "GXDLMSSecuritySetupView", "GXDLMSSFSKPhyMacSetUpView", "GXDLMSTariffPlanView", "GXDLMSTokenGatewayView", "GXDLMSWirelessModeQchannelView"];

    [STAThread]
    static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        if (args.Length == 0) { Console.Error.WriteLine("Usage: UiReviewTests inventory|audit|regressions|behavior|roundtrip|sort-reference|clock-combo|all-combos|calendar OUTPUT [--filter NAME[,NAME]] [--timeout 30]"); return 64; }
        try
        {
            if (args[0] == "worker") return Worker(args[1], args[2], args[3], args.Length > 4 ? int.Parse(args[4]) : -1, args.Length > 5 && args[5] is "combos" or "combo-height", args.Length > 5 && args[5] is "layout" or "action-schedule" or "parameter-monitor" or "twisted-pair" or "image-transfer" or "register-actions" or "activation-footer" or "profile-header" or "schedule-footer" or "buttons", args.Length > 5 && args[5] == "combo-height", args.Length > 5 && args[5] == "action-schedule", args.Length > 5 && args[5] == "parameter-monitor", args.Length > 5 && args[5] == "twisted-pair", args.Length > 5 && args[5] == "image-transfer", args.Length > 5 && args[5] == "register-actions", args.Length > 5 && args[5] == "activation-footer", args.Length > 5 && args[5] == "profile-header", args.Length > 5 && args[5] == "schedule-footer", args.Length > 5 && args[5] == "buttons");
            if (args[0] == "sort-reference-worker") return SortReferenceWorker(args[1], args[2]);
            if (args[0] == "clock-combo-worker") return ClockComboWorker(args[1], args[2]);
            if (args[0] == "calendar-worker") return CalendarWorker(args[1], args[2], int.Parse(args[3]));
            var output = Path.GetFullPath(args.Length > 1 ? args[1] : "ui-review-artifacts");
            Directory.CreateDirectory(output);
            var inventory = Inventory();
            Write(Path.Combine(output, "inventory.json"), inventory);
            File.WriteAllText(Path.Combine(output, "inventory.csv"), "Kind,Type,ModelType,Constructors\n" + string.Join("\n", inventory.Screens.Select(s => string.Join(",", Csv(s.Kind), Csv(s.Type), Csv(s.ModelType), Csv(string.Join(" | ", s.Constructors))))));
            Console.WriteLine($"Compiled inventory: {inventory.ObjectViewCount} object views; {inventory.DialogCount} additional dialogs; {inventory.UnattributedViewCount} unattributed views.");
            if (args[0] == "inventory") return 0;
            if (args[0] == "sort-reference") return SortReferenceReview(output, inventory, args);
            if (args[0] == "clock-combo") return ClockComboReview(output, inventory, args);
            if (args[0] == "calendar") return CalendarReview(output, inventory, args);
            if (args[0] == "combo-height-fixture") return ComboHeightFixture(output);
            if (args[0] == "profile-inputs") return ProfileInputs(output);
            if (!new[] { "audit", "regressions", "roundtrip", "behavior", "all-combos", "action-schedule", "parameter-monitor", "twisted-pair", "image-transfer", "register-actions", "activation-footer", "profile-header", "schedule-footer" }.Contains(args[0])) return 64;
            var filterIndex = Array.IndexOf(args, "--filter");
            var timeoutIndex = Array.IndexOf(args, "--timeout");
            int seconds = timeoutIndex >= 0 ? int.Parse(args[timeoutIndex + 1]) : 30;
            var cases = new List<CaseResult>();
            foreach (var screen in inventory.Screens.Where(s => s.Kind == "ObjectView"))
            {
                if (filterIndex >= 0 && !args[filterIndex + 1].Split(',').Any(filter => screen.Type.Contains(filter, StringComparison.OrdinalIgnoreCase))) continue;
                if (args[0] == "regressions" && !SimpleViews.Contains(screen.ShortName)) continue;
                if (args[0] == "roundtrip" && !RoundtripViews.Contains(screen.ShortName)) continue;
                if (args[0] == "behavior" && !new[] { "GXDLMSClockView", "GXDLMSPushSetupView", "GXDLMSAssociationLogicalNameView", "GXDLMSMeterReadingParameterView", "GXDLMSUnixTimeView" }.Contains(screen.ShortName)) continue;
                if (args[0] == "all-combos" && !ComboViews.Contains(screen.ShortName)) continue;
                if (args[0] == "action-schedule" && screen.ShortName != "GXDLMSActionScheduleView") continue;
                if (args[0] == "parameter-monitor" && screen.ShortName != "GXDLMSParameterMonitorView") continue;
                if (args[0] == "twisted-pair" && screen.ShortName != "GXDLMSIecTwistedPairSetupView") continue;
                if (args[0] == "image-transfer" && screen.ShortName != "GXDLMSImageTransferView") continue;
                if (args[0] == "register-actions" && !RegisterActionViews.Contains(screen.ShortName)) continue;
                if (args[0] == "activation-footer" && screen.ShortName != "GXDLMSRegisterActivationView") continue;
                if (args[0] == "profile-header" && screen.ShortName != "GXDLMSProfileGenericView") continue;
                if (args[0] == "schedule-footer" && screen.ShortName != "GXDLMSScheduleView") continue;
                var states = args[0] == "all-combos" ? screen.ShortName == "GXDLMSDataView" ? new[] { "enum" } : screen.ShortName == "GXDLMSSecuritySetupView" ? new[] { "representative", "version0" } : new[] { "representative" } : args[0] == "roundtrip" ? new[] { screen.ShortName == "GXDLMSDataView" ? "structured" : "representative" } : new[] { "blank", "representative" }.Concat(screen.ShortName == "GXDLMSDataView" ? new[] { "structured" } : []);
                if (args[0] == "action-schedule") states = new[] { "blank", "representative", "long-script" };
                if (args[0] == "parameter-monitor") states = new[] { "blank", "representative", "long-target-value" };
                if (args[0] == "twisted-pair") states = new[] { "blank", "representative", "long-twisted" };
                if (args[0] == "image-transfer") states = new[] { "blank", "representative", "long-images" };
                if (args[0] == "activation-footer") states = new[] { "blank", "representative", "long-activation" };
                if (args[0] == "profile-header") states = new[] { "blank", "representative", "long-profile-reference", "italy-profile" };
                if (args[0] == "schedule-footer") states = new[] { "blank", "representative", "long-schedule" };
                foreach (int monitor in args[0] is "roundtrip" or "all-combos" or "action-schedule" or "parameter-monitor" or "twisted-pair" or "image-transfer" or "register-actions" or "activation-footer" or "profile-header" or "schedule-footer" ? Enumerable.Range(0, Screen.AllScreens.Length) : new[] { -1 })
                foreach (var state in states)
                {
                    string caseDir = Path.Combine(output, "cases", screen.ShortName + "--" + state + (monitor >= 0 ? "--monitor" + monitor : ""));
                    Directory.CreateDirectory(caseDir);
                    var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = caseDir };
                    // Supports both apphost execution and `dotnet UiReviewTests.dll`.
                    if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
                    foreach (string value in new[] { "worker", screen.Type, state, caseDir }) start.ArgumentList.Add(value);
                    start.ArgumentList.Add(monitor.ToString());
                    if (args[0] == "all-combos") start.ArgumentList.Add(args.Contains("--native-height") ? "combo-height" : "combos");
                    if (args[0] == "action-schedule") start.ArgumentList.Add("action-schedule");
                    if (args[0] == "parameter-monitor") start.ArgumentList.Add("parameter-monitor");
                    if (args[0] == "twisted-pair") start.ArgumentList.Add("twisted-pair");
                    if (args[0] == "image-transfer") start.ArgumentList.Add("image-transfer");
                    if (args[0] == "register-actions") start.ArgumentList.Add("register-actions");
                    if (args[0] == "activation-footer") start.ArgumentList.Add("activation-footer");
                    if (args[0] == "profile-header") start.ArgumentList.Add("profile-header");
                    if (args[0] == "schedule-footer") start.ArgumentList.Add("schedule-footer");
                    using var process = Process.Start(start)!;
                    var stdout = process.StandardOutput.ReadToEndAsync();
                    var stderr = process.StandardError.ReadToEndAsync();
                    bool finished = process.WaitForExit(seconds * 1000);
                    if (!finished) { process.Kill(true); process.WaitForExit(5000); }
                    File.WriteAllText(Path.Combine(caseDir, "worker.log"), stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
                    string resultPath = Path.Combine(caseDir, "result.json");
                    CaseResult result;
                    try { result = JsonSerializer.Deserialize<CaseResult>(File.ReadAllText(resultPath))!; }
                    catch { result = new() { View = screen.Type, State = state }; }
                    if (!finished) { result.Status = "Timeout"; result.Errors.Add($"Worker exceeded {seconds}s; process tree terminated."); }
                    else if (process.ExitCode != 0 && result!.Status == "Passed") { result.Status = "WorkerError"; result.Errors.Add($"Worker exit code {process.ExitCode}."); }
                    if (result.Captures.Count == 0) foreach (var size in Sizes) result.Captures.Add(new() { Width = size.Width, Height = size.Height, Status = "Unavailable", Error = result.Status });
                    Write(resultPath, result);
                    cases.Add(result);
                    Write(Path.Combine(output, "results.json"), cases);
                    Console.WriteLine($"{cases.Count}: {screen.ShortName}/{state}: {result.Status}; {result.Captures.Count(c => c.Status == "Captured")} captures");
                }
            }
            var regressions = args[0] is "twisted-pair" or "image-transfer" or "register-actions" or "activation-footer" or "profile-header" or "schedule-footer" ? cases.SelectMany(c => c.BehaviorChecks).ToList() : RegressionResults(cases);
            Write(Path.Combine(output, "regressions.json"), regressions);
            WriteLedger(output, inventory, cases, regressions);
            MakeContactSheets(output, cases);
            if (args[0] == "all-combos") WriteComboLedger(output, inventory, cases, regressions);
            Console.WriteLine($"Completed: {cases.Count} fixture cases, {cases.Sum(c => c.Captures.Count(x => x.Status == "Captured"))} captures, {regressions.Count(r => !r.Passed)} regression failures.");
            return cases.Any(c => c.Status != "Passed") || regressions.Any(r => !r.Passed) ? 1 : 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 2; }
    }

    // This matrix deliberately supplies attribute metadata and actual sort references;
    // scalar-only fixtures do not exercise the former multiline-scrollbar failure.
    static int SortReferenceReview(string output, InventoryResult inventory, string[] args)
    {
        int timeoutIndex = Array.IndexOf(args, "--timeout");
        int seconds = timeoutIndex >= 0 ? int.Parse(args[timeoutIndex + 1]) : 90;
        var cases = new List<CaseResult>();
        foreach (string state in new[] { "empty", "clock", "register", "long-name" })
        {
            string caseDir = Path.Combine(output, "cases", "GXDLMSProfileGenericView--" + state);
            Directory.CreateDirectory(caseDir);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = caseDir };
            if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            foreach (string value in new[] { "sort-reference-worker", state, caseDir }) start.ArgumentList.Add(value);
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            bool finished = process.WaitForExit(seconds * 1000);
            if (!finished) { process.Kill(true); process.WaitForExit(5000); }
            File.WriteAllText(Path.Combine(caseDir, "worker.log"), stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
            CaseResult result;
            try { result = JsonSerializer.Deserialize<CaseResult>(File.ReadAllText(Path.Combine(caseDir, "result.json")))!; }
            catch { result = new() { View = typeof(GXDLMSProfileGenericView).FullName!, State = state }; }
            if (!finished) { result.Status = "Timeout"; result.Errors.Add($"Worker exceeded {seconds}s."); }
            else if (process.ExitCode != 0 && result.Status == "Passed") { result.Status = "WorkerError"; result.Errors.Add("Exit code " + process.ExitCode); }
            Write(Path.Combine(caseDir, "result.json"), result); cases.Add(result); Write(Path.Combine(output, "results.json"), cases);
            Console.WriteLine($"Sort reference/{state}: {result.Status}; {result.Captures.Count} captures; {result.BehaviorChecks.Count(x => !x.Passed)} behavior failures.");
        }
        var checks = RegressionResults(cases);
        Write(Path.Combine(output, "regressions.json"), checks);
        WriteLedger(output, inventory, cases, checks);
        Write(Path.Combine(output, "sort-reference-summary.json"), new { inventory.AssemblySha256, WorkerCases = cases.Count, States = cases.Select(c => c.State), MonitorRoutes = cases.Select(c => c.Monitor).Distinct(), ObservedDpi = cases.SelectMany(c => c.Captures).Select(c => c.Dpi).Distinct().Order(), Captures = cases.Sum(c => c.Captures.Count), Checks = checks.Count, FailedChecks = checks.Count(c => !c.Passed), FailedCases = cases.Count(c => c.Status != "Passed") });
        File.AppendAllText(Path.Combine(output, "coverage-ledger.txt"), "\nSort reference focused matrix: explicit attribute 6 Structure metadata; empty, Clock, Register and long-description references. Each same-view worker traverses every actual Screen.AllScreens monitor then returns to its origin; wide/narrow/wide, minimize/restore, and null/populated/null binding refreshes are captured. Native selection is tested without writing the clipboard. No protocol actions or user settings are invoked.\n");
        Console.WriteLine($"Completed sort reference: {cases.Count} workers; {cases.Sum(c => c.Captures.Count)} captures; {checks.Count} checks; {checks.Count(c => !c.Passed)} failures.");
        return cases.Any(c => c.Status != "Passed") || checks.Any(c => !c.Passed) ? 1 : 0;
    }

    static int SortReferenceWorker(string state, string output)
    {
        var result = new CaseResult { View = typeof(GXDLMSProfileGenericView).FullName!, State = state, Status = "Started", AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Ui.Location))) };
        void Save() => Write(Path.Combine(output, "result.json"), result);
        void Check(string name, bool passed, string detail = "") => result.BehaviorChecks.Add(new() { Name = "SortReference/" + state + "/" + name, Passed = passed, Detail = detail });
        Save();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); ApplicationTypography.Install();
        string stage = "fixture";
        try
        {
            using var isolatedSettings = new MemoryProfileSettings();
            var attr = typeof(GXDLMSProfileGenericView).GetCustomAttribute<GXDLMSViewAttribute>()!;
            var profile = (GXDLMSProfileGeneric)CreateModel(attr, nameof(GXDLMSProfileGenericView), "representative", result.FixtureNotes);
            profile.CapturePeriod = 900; profile.ProfileEntries = 1000;
            var clock = new GXDLMSClock("0.0.1.0.0.255") { Description = "Meter clock", Time = new GXDateTime(SampleTime) };
            profile.Parent.Add(clock);
            profile.CaptureObjects.Add(new GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject>(clock, new GXDLMSCaptureObject(2, 0)));
            // Keep both capture columns and buffer rows consistent.
            for (int i = 0; i < profile.Buffer.Count; i++) profile.Buffer[i] = new object[] { 12846.72 + i, new GXDateTime(SampleTime.AddMinutes(i)) };
            var register = profile.Parent.OfType<GXDLMSRegister>().Single(); register.Description = "Imported active energy";
            GXDLMSObject populated = state is "empty" or "clock" ? clock : register;
            if (state == "long-name") populated.Description = "North building incoming supply, laboratory reference meter, total imported active energy for the main distribution board and its independently monitored downstream circuits; retained descriptive name from the saved meter configuration for commissioning and periodic inspection.";
            GXDLMSObject? initial = state == "empty" ? null : populated;
            profile.SetDataType(6, DataType.Structure); profile.SetAccess(6, AccessMode.Read);
            void Assign(GXDLMSObject? value) { profile.SortObject = value; profile.SortAttributeIndex = value == null ? 0 : 2; profile.SortDataIndex = 0; profile.SetLastReadTime(6, SampleTime); }
            Assign(initial);
            result.FixtureNotes.Add("Attribute 6 explicitly Structure, read-only; populated references use attribute 2/data index 0. Clock and Register are present in CaptureObjects and the parent collection.");
            var screens = Screen.AllScreens;
            var origin = screens[0].WorkingArea;
            stage = "construction";
            using var shell = new AuditShell { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new(origin.X + 24, origin.Y + 24), Opacity = 0, ClientSize = Sizes[0], AutoValidate = AutoValidate.Disable, Font = ApplicationTypography.BodyFont, Text = "Offline Sort object review" };
            shell.Show(); for (int i = 0; i < 6; i++) Pump();
            using var editor = new GXDLMSProfileGenericView { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill, AutoValidate = AutoValidate.Disable };
            var view = (IGXDLMSView)editor; view.Target = profile; shell.Controls.Add(editor); editor.Show(); for (int i = 0; i < 6; i++) Pump();
            ModernPresentation.Apply(editor);
            GXDlmsUi.Init(view, (_, _) => throw new InvalidOperationException("Sort reference audit must never invoke a meter action."));
            var client = new GXDLMSClient();
            void Bind(GXDLMSObject? value) { Assign(value); GXDlmsUi.ObjectChanged(view, client, profile, true); shell.PerformLayout(); editor.PerformLayout(); for (int i = 0; i < 4; i++) Pump(); }
            void Resize(Size size) { shell.ClientSize = size; Pump(); shell.ClientSize = size; editor.Bounds = shell.ClientRectangle; shell.PerformLayout(); editor.PerformLayout(); for (int i = 0; i < 4; i++) Pump(); }
            string Geometry() => string.Join(";", Descendants(editor).Where(c => c.Visible && new[] { "SortObjectReference", "ReferenceIdentity", "ReferenceDetails" }.Contains(c.Name)).Select(c =>
            {
                var bounds = c.Bounds;
                // Capture/reveal may scroll the bounded reference to its first
                // native textbox. Compare content geometry, not that viewport offset.
                if (c.Parent is ScrollableControl parent && parent.Name == "SortObjectReference")
                    bounds.Offset(-parent.AutoScrollPosition.X, -parent.AutoScrollPosition.Y);
                return $"{c.Name}:{bounds}:{c.Font.SizeInPoints:0.00}";
            }));
            void Observe(string variant, GXDLMSObject? expected)
            {
                stage = variant;
                foreach (var s in Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(c => c.AutoScroll)) s.AutoScrollPosition = Point.Empty;
                editor.PerformLayout(); Pump();
                CheckSortReference(editor, shell, profile, expected, variant, Check);
                CaptureAtPosition(editor, result, output, shell.ClientSize, variant); Save();
            }
            Bind(initial); result.BindingSucceeded = true;
            Check("explicit-structure-metadata", GXDLMSClient.GetAttributeInfo(profile, 6)?.Type == DataType.Structure);
            Check("legacy-field-removed", !Descendants(editor).Any(c => c.Name == "SortObjectTB"));
            string? firstGeometry = null; int firstDpi = 0;
            var monitorRoute = new List<string>();
            for (int monitor = 0; monitor < screens.Length; monitor++)
            {
                var area = screens[monitor].WorkingArea;
                shell.Location = new(area.X + 24, area.Y + 24); for (int i = 0; i < 8; i++) Pump();
                Resize(Sizes[0]); Bind(initial);
                string prefix = "monitor" + monitor + "-dpi" + editor.DeviceDpi;
                monitorRoute.Add(screens[monitor].DeviceName + "=" + editor.DeviceDpi + "dpi");
                Check(prefix + "/actual-monitor", Screen.FromControl(shell).DeviceName == screens[monitor].DeviceName, Screen.FromControl(shell).DeviceName);
                Observe(prefix + "-wide", initial); string wideGeometry = Geometry();
                if (monitor == 0) { firstGeometry = wideGeometry; firstDpi = editor.DeviceDpi; }
                Resize(Sizes[1]); Observe(prefix + "-narrow", initial);
                if (state == "long-name")
                {
                    string description = populated.Description;
                    populated.Description = "Energy"; Bind(populated); Observe(prefix + "-refresh-short-defined", populated);
                    var reference = (ScrollableControl)Descendants(editor).Single(c => c.Name == "SortObjectReference");
                    int preferred = reference.GetPreferredSize(new Size(reference.Width, 0)).Height;
                    Check(prefix + "/short-defined-reference-removes-unneeded-bar", preferred > reference.Height + 2 || !reference.VerticalScroll.Visible, "Fullwidth preferred=" + preferred + "; height=" + reference.Height + "; Vbar=" + reference.VerticalScroll.Visible);
                    populated.Description = description; Bind(initial);
                }
                Bind(null); Observe(prefix + "-refresh-empty-before", null);
                Bind(populated); Observe(prefix + "-refresh-populated", populated);
                Bind(null); Observe(prefix + "-refresh-empty-after", null);
                Bind(initial); Resize(Sizes[0]); Observe(prefix + "-wide-restored", initial);
                Check(prefix + "/wide-narrow-wide-restores", Geometry() == wideGeometry, "Before=" + wideGeometry + "; After=" + Geometry());
                shell.WindowState = FormWindowState.Minimized; for (int i = 0; i < 4; i++) Pump();
                Check(prefix + "/minimized", shell.WindowState == FormWindowState.Minimized);
                shell.WindowState = FormWindowState.Normal; for (int i = 0; i < 8; i++) Pump();
                Observe(prefix + "-minimize-restored", initial);
                Check(prefix + "/minimize-restore-preserves-layout", shell.ClientSize == Sizes[0] && Geometry() == wideGeometry, "After=" + Geometry());
                shell.WindowState = FormWindowState.Maximized; for (int i = 0; i < 8; i++) Pump();
                Check(prefix + "/maximized", shell.WindowState == FormWindowState.Maximized);
                shell.WindowState = FormWindowState.Normal; for (int i = 0; i < 8; i++) Pump();
                Observe(prefix + "-maximize-restored", initial);
                Check(prefix + "/maximize-restore-preserves-layout", shell.ClientSize == Sizes[0] && Geometry() == wideGeometry, "After=" + Geometry());
            }
            shell.Location = new(origin.X + 24, origin.Y + 24); for (int i = 0; i < 8; i++) Pump();
            Resize(Sizes[0]); Bind(initial); Observe("physical-monitor-return", initial);
            Check("physical-monitor-roundtrip-restores", editor.DeviceDpi == firstDpi && Geometry() == firstGeometry, "Before=" + firstGeometry + "; After=" + Geometry());
            result.Monitor = string.Join(" -> ", monitorRoute) + " -> " + screens[0].DeviceName;
            result.Status = "Passed"; shell.Hide();
        }
        catch (Exception ex) { result.Status = stage + "Error"; result.Errors.Add(ex.ToString()); }
        Save(); return result.Status == "Passed" ? 0 : 1;
    }

    static void CheckSortReference(Form editor, Form shell, GXDLMSProfileGeneric profile, GXDLMSObject? expected, string variant, Action<string, bool, string> record)
    {
        void Check(string name, bool pass, string detail = "") => record(variant + "/" + name, pass, detail);
        var reference = Descendants(editor).Single(c => c.Name == "SortObjectReference");
        var identity = Descendants(reference).OfType<TextBox>().Single(c => c.Name == "ReferenceIdentity");
        var details = Descendants(reference).OfType<TextBox>().Single(c => c.Name == "ReferenceDetails");
        Check("reference-visible", reference.Visible && identity.Visible);
        Check("identity-content", expected == null ? identity.Text == "No sort object" : identity.Text.Contains(expected.ObjectType.ToString()) && identity.Text.Contains(expected.LogicalName) && identity.Text.Contains(expected.Description ?? ""), identity.Text);
        Check("details-content", expected == null ? !details.Visible : details.Visible && details.Text == $"Attribute: {profile.SortAttributeIndex} · Data index: {profile.SortDataIndex}", details.Text);
        Check("model-reference-and-indices-preserved", ReferenceEquals(profile.SortObject, expected) && profile.SortAttributeIndex == (expected == null ? 0 : 2) && profile.SortDataIndex == 0);
        foreach (var box in new[] { identity, details }.Where(c => c.Visible))
        {
            string name = box.Name + "/";
            Check(name + "native-readonly-copyable", box.ReadOnly && box.Enabled && box.ShortcutsEnabled && box.TabStop && box.Multiline);
            Check(name + "no-inner-scrollbars", box.ScrollBars == ScrollBars.None && box.WordWrap, "ScrollBars=" + box.ScrollBars + "; WordWrap=" + box.WordWrap);
            box.Select(0, 0);
            Point first = box.GetPositionFromCharIndex(0), last = box.GetPositionFromCharIndex(Math.Max(0, box.TextLength - 1));
            int lines = box.GetLineFromCharIndex(Math.Max(0, box.TextLength - 1)) + 1;
            Check(name + "all-lines-readable", box.ClientSize.Width >= 80 && first.Y >= 0 && last.Y + box.Font.Height <= box.ClientSize.Height + 2, $"Client={box.ClientSize}; NativeLines={lines}; First={first}; Last={last}; FontHeight={box.Font.Height}");
            Check(name + "font-matches-shell", Math.Abs(box.Font.SizeInPoints - shell.Font.SizeInPoints) < .1, $"Text={box.Font.SizeInPoints}; shell={shell.Font.SizeInPoints}; DPI={box.DeviceDpi}");
            box.SelectAll(); Check(name + "text-selectable", box.SelectionLength == box.TextLength && box.SelectedText == box.Text); box.Select(0, 0);
            var viewport = (ScrollableControl)reference; int contentHeight = viewport.AutoScroll ? Math.Max(viewport.ClientSize.Height, viewport.AutoScrollMinSize.Height) : viewport.ClientSize.Height;
            Check(name + "inside-reference-content", box.Left >= 0 && box.Top - viewport.AutoScrollPosition.Y >= 0 && box.Right <= reference.ClientSize.Width && box.Bottom - viewport.AutoScrollPosition.Y <= contentHeight + 2 && !viewport.HorizontalScroll.Visible, $"Text={box.Bounds}; Reference={reference.ClientSize}; content={contentHeight}");
        }
        var ancestors = new List<Control>(); for (Control? c = reference.Parent; c != null; c = c.Parent) ancestors.Add(c);
        Check("no-parent-horizontal-scroll", !ancestors.OfType<ScrollableControl>().Any(c => c.HorizontalScroll.Visible));
        Check("text-selection-does-not-mutate-reference", ReferenceEquals(profile.SortObject, expected));
    }

    static int ClockComboReview(string output, InventoryResult inventory, string[] args)
    {
        int timeoutIndex = Array.IndexOf(args, "--timeout");
        int seconds = timeoutIndex >= 0 ? int.Parse(args[timeoutIndex + 1]) : 90;
        var cases = new List<CaseResult>();
        foreach (string state in new[] { "editable", "unread", "read-only", "disconnected" })
        {
            string caseDir = Path.Combine(output, "cases", "GXDLMSClockView--" + state);
            Directory.CreateDirectory(caseDir);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = caseDir };
            if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            foreach (string value in new[] { "clock-combo-worker", state, caseDir }) start.ArgumentList.Add(value);
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            bool finished = process.WaitForExit(seconds * 1000);
            if (!finished) { process.Kill(true); process.WaitForExit(5000); }
            File.WriteAllText(Path.Combine(caseDir, "worker.log"), stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
            CaseResult result;
            try { result = JsonSerializer.Deserialize<CaseResult>(File.ReadAllText(Path.Combine(caseDir, "result.json")))!; }
            catch { result = new() { View = typeof(GXDLMSClockView).FullName!, State = state }; }
            if (!finished) { result.Status = "Timeout"; result.Errors.Add($"Worker exceeded {seconds}s."); }
            else if (process.ExitCode != 0 && result.Status == "Passed") { result.Status = "WorkerError"; result.Errors.Add("Exit code " + process.ExitCode); }
            Write(Path.Combine(caseDir, "result.json"), result); cases.Add(result); Write(Path.Combine(output, "results.json"), cases);
            Console.WriteLine($"Clock combo/{state}: {result.Status}; {result.Captures.Count} captures; {result.BehaviorChecks.Count(x => !x.Passed)} behavior failures.");
        }
        var checks = RegressionResults(cases);
        Write(Path.Combine(output, "regressions.json"), checks); WriteLedger(output, inventory, cases, checks);
        Write(Path.Combine(output, "clock-combo-summary.json"), new { inventory.AssemblySha256, WorkerCases = cases.Count, States = cases.Select(c => c.State), MonitorRoutes = cases.Select(c => c.Monitor).Distinct(), ObservedDpi = cases.SelectMany(c => c.Captures).Select(c => c.Dpi).Distinct().Order(), Captures = cases.Sum(c => c.Captures.Count), Checks = checks.Count, FailedChecks = checks.Count(c => !c.Passed), FailedCases = cases.Count(c => c.Status != "Passed") });
        File.AppendAllText(Path.Combine(output, "coverage-ledger.txt"), "\nClock combo focused matrix: original attribute 9 GXValueField/native ComboBox identity retained; six typed enum options, editable/unread/read-only/disconnected states. Same-view physical monitor roundtrip, wide/narrow/wide, minimize and maximize restore. Native selection changes only disposable model values; no meter command or transport. Wrapper background, padding and rendered border strips are checked alongside native preferred height/full width/centering.\n");
        Console.WriteLine($"Completed clock combo: {cases.Count} workers; {cases.Sum(c => c.Captures.Count)} captures; {checks.Count} checks; {checks.Count(c => !c.Passed)} failures.");
        return cases.Any(c => c.Status != "Passed") || checks.Any(c => !c.Passed) ? 1 : 0;
    }

    static int ClockComboWorker(string state, string output)
    {
        var result = new CaseResult { View = typeof(GXDLMSClockView).FullName!, State = state, Status = "Started", AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Ui.Location))) };
        void Save() => Write(Path.Combine(output, "result.json"), result);
        void Check(string name, bool passed, string detail = "") => result.BehaviorChecks.Add(new() { Name = "ClockCombo/" + state + "/" + name, Passed = passed, Detail = detail });
        Save(); Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); ApplicationTypography.Install();
        string stage = "fixture";
        try
        {
            var attr = typeof(GXDLMSClockView).GetCustomAttribute<GXDLMSViewAttribute>()!;
            var model = (GXDLMSClock)CreateModel(attr, nameof(GXDLMSClockView), "representative", result.FixtureNotes);
            var values = Enum.GetValues<Gurux.DLMS.Objects.Enums.ClockBase>();
            model.ClockBase = Gurux.DLMS.Objects.Enums.ClockBase.Crystal; model.SetDataType(9, DataType.Enum);
            bool connected = state != "disconnected", hasRead = state != "unread", writable = connected && state != "read-only";
            var screens = Screen.AllScreens; var origin = screens[0].WorkingArea;
            using var shell = new AuditShell { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new(origin.X + 24, origin.Y + 24), Opacity = 0, ClientSize = Sizes[0], AutoValidate = AutoValidate.Disable, Font = ApplicationTypography.BodyFont, Text = "Offline Clock combo review" };
            shell.Show(); for (int i = 0; i < 6; i++) Pump();
            stage = "construction";
            using var editor = new GXDLMSClockView { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill, AutoValidate = AutoValidate.Disable };
            var originalField = Descendants(editor).OfType<GXValueField>().Single(c => c.Name == "ClockBaseTB");
            var originalCombo = Descendants(originalField).OfType<ComboBox>().Single();
            var view = (IGXDLMSView)editor; view.Target = model; shell.Controls.Add(editor); editor.Show(); for (int i = 0; i < 6; i++) Pump();
            ModernPresentation.Apply(editor); GXDlmsUi.Init(view, (_, _) => throw new InvalidOperationException("Clock combo review must never invoke a meter action."));
            var client = new GXDLMSClient();
            void Bind()
            {
                model.SetAccess(9, state == "read-only" ? AccessMode.Read : AccessMode.ReadWrite); model.SetLastReadTime(9, hasRead ? SampleTime : DateTime.MinValue);
                model.ClearDirty(9); GXDlmsUi.ObjectChanged(view, client, model, connected);
                shell.PerformLayout(); editor.PerformLayout(); for (int i = 0; i < 4; i++) Pump();
            }
            void Resize(Size size) { shell.ClientSize = size; Pump(); shell.ClientSize = size; editor.Bounds = shell.ClientRectangle; shell.PerformLayout(); editor.PerformLayout(); for (int i = 0; i < 4; i++) Pump(); }
            string Geometry() => $"Field={originalField.Bounds}; Combo={originalCombo.Bounds}; Font={originalCombo.Font.SizeInPoints:0.00}";
            void Observe(string variant)
            {
                stage = variant;
                foreach (var s in Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(c => c.AutoScroll)) s.AutoScrollPosition = Point.Empty;
                editor.PerformLayout(); Pump();
                CheckClockCombo(editor, shell, model, originalField, originalCombo, values, writable, hasRead, variant, Check);
                Capture(editor, result, output, shell.ClientSize, variant); Save();
            }
            Bind(); result.BindingSucceeded = true;
            Check("attribute9-enum-metadata", GXDLMSClient.GetAttributeInfo(model, 9)?.Type == DataType.Enum);
            result.FixtureNotes.Add("ClockBaseTB attribute 9; original native combo retained. Six actual ClockBase enum values are tested; no meter transport exists.");
            var route = new List<string>(); string? firstGeometry = null; int firstDpi = 0;
            Rectangle firstField = Rectangle.Empty, firstCombo = Rectangle.Empty;
            float firstFont = 0;
            bool WithinTwoPixels(Rectangle first, Rectangle second) => Math.Abs(first.X - second.X) <= 2 && Math.Abs(first.Y - second.Y) <= 2 && Math.Abs(first.Width - second.Width) <= 2 && Math.Abs(first.Height - second.Height) <= 2;
            for (int monitor = 0; monitor < screens.Length; monitor++)
            {
                var area = screens[monitor].WorkingArea; shell.Location = new(area.X + 24, area.Y + 24); for (int i = 0; i < 8; i++) Pump();
                Resize(Sizes[0]); Bind(); string prefix = "monitor" + monitor + "-dpi" + editor.DeviceDpi;
                route.Add(screens[monitor].DeviceName + "=" + editor.DeviceDpi + "dpi");
                Check(prefix + "/actual-monitor", Screen.FromControl(shell).DeviceName == screens[monitor].DeviceName);
                Observe(prefix + "-wide"); string wideGeometry = Geometry();
                if (monitor == 0) { firstGeometry = wideGeometry; firstDpi = editor.DeviceDpi; firstField = originalField.Bounds; firstCombo = originalCombo.Bounds; firstFont = originalCombo.Font.SizeInPoints; }
                Resize(Sizes[1]); Observe(prefix + "-narrow");
                if (state == "editable")
                {
                    foreach (var value in values)
                    {
                        model.ClearDirty(9); originalCombo.SelectedItem = value; Pump();
                        bool dirty = model.GetDirty(9, out object dirtyValue);
                        Check(prefix + "/select-" + value + "/commits-model", model.ClockBase == value && dirty && Convert.ToInt32(dirtyValue) == Convert.ToInt32(value), "Model=" + model.ClockBase + "; dirty=" + dirty);
                        Check(prefix + "/select-" + value + "/dirty-indicator", !string.IsNullOrEmpty(view.ErrorProvider.GetError(originalField)));
                        Observe(prefix + "-selected-" + value);
                        Bind(); Check(prefix + "/select-" + value + "/clean-refresh", !model.GetDirty(9, out _) && !originalField.Dirty && string.IsNullOrEmpty(view.ErrorProvider.GetError(originalField)) && Equals(originalCombo.SelectedItem, value));
                    }
                }
                else if (state == "unread")
                {
                    hasRead = true; Bind(); Observe(prefix + "-read-arrived");
                    hasRead = false; Bind(); Observe(prefix + "-read-cleared");
                }
                else
                {
                    foreach (var value in values)
                    {
                        model.ClockBase = value; Bind();
                        Check(prefix + "/readonly-refresh-" + value, Equals(originalCombo.SelectedItem, value) && !originalCombo.Enabled && !model.GetDirty(9, out _));
                    }
                }
                model.ClockBase = Gurux.DLMS.Objects.Enums.ClockBase.Crystal; Bind(); Resize(Sizes[0]); Observe(prefix + "-wide-restored");
                Check(prefix + "/wide-narrow-wide-restores", Geometry() == wideGeometry, "Before=" + wideGeometry + "; After=" + Geometry());
                shell.WindowState = FormWindowState.Minimized; for (int i = 0; i < 4; i++) Pump(); Check(prefix + "/minimized", shell.WindowState == FormWindowState.Minimized);
                shell.WindowState = FormWindowState.Normal; for (int i = 0; i < 8; i++) Pump(); Observe(prefix + "-minimize-restored");
                Check(prefix + "/minimize-restores", shell.ClientSize == Sizes[0] && Geometry() == wideGeometry, Geometry());
                shell.WindowState = FormWindowState.Maximized; for (int i = 0; i < 8; i++) Pump(); Check(prefix + "/maximized", shell.WindowState == FormWindowState.Maximized);
                shell.WindowState = FormWindowState.Normal; for (int i = 0; i < 8; i++) Pump(); Observe(prefix + "-maximize-restored");
                Check(prefix + "/maximize-restores", shell.ClientSize == Sizes[0] && Geometry() == wideGeometry, Geometry());
            }
            shell.Location = new(origin.X + 24, origin.Y + 24); for (int i = 0; i < 8; i++) Pump(); Resize(Sizes[0]); Bind(); Observe("physical-monitor-return");
            Check("physical-monitor-roundtrip-restores", editor.DeviceDpi == firstDpi && WithinTwoPixels(firstField, originalField.Bounds) && WithinTwoPixels(firstCombo, originalCombo.Bounds) && Math.Abs(originalCombo.Font.SizeInPoints - firstFont) < .1, "Before=" + firstGeometry + "; After=" + Geometry() + "; native DPI rounding tolerance=2px");
            Rectangle returnedField = originalField.Bounds, returnedCombo = originalCombo.Bounds;
            foreach (int monitor in Enumerable.Range(1, screens.Length - 1).Concat(new[] { 0 }))
            {
                var area = screens[monitor].WorkingArea; shell.Location = new(area.X + 24, area.Y + 24); for (int i = 0; i < 8; i++) Pump();
                Resize(Sizes[0]); Bind(); Observe("repeat-monitor" + monitor + "-dpi" + editor.DeviceDpi);
            }
            Check("repeat-monitor-roundtrip-no-size-drift", originalField.Size == returnedField.Size && originalCombo.Size == returnedCombo.Size && WithinTwoPixels(firstField, originalField.Bounds) && WithinTwoPixels(firstCombo, originalCombo.Bounds) && Math.Abs(originalCombo.Font.SizeInPoints - firstFont) < .1 && editor.DeviceDpi == firstDpi, "First return field=" + returnedField + "; combo=" + returnedCombo + "; repeated return=" + Geometry());
            // Existing Clock access/toggle regressions cover timezone and independent DST permissions.
            CheckBehavior(editor, view, model, client, result.BehaviorChecks);
            foreach (int index in Enumerable.Range(2, 8))
            {
                view.ErrorProvider.Clear(); view.OnDirtyChange(index, true); bool marked = Descendants(editor).Any(c => !string.IsNullOrEmpty(view.ErrorProvider.GetError(c)));
                view.OnDirtyChange(index, false); Check("dirty-clean/attribute" + index, marked && Descendants(editor).All(c => string.IsNullOrEmpty(view.ErrorProvider.GetError(c))));
            }
            result.Monitor = string.Join(" -> ", route) + " -> " + screens[0].DeviceName; result.Status = "Passed"; shell.Hide();
        }
        catch (Exception ex) { result.Status = stage + "Error"; result.Errors.Add(ex.ToString()); }
        Save(); return result.Status == "Passed" ? 0 : 1;
    }

    static void CheckClockCombo(Form editor, Form shell, GXDLMSClock model, GXValueField originalField, ComboBox originalCombo, Gurux.DLMS.Objects.Enums.ClockBase[] values, bool writable, bool hasRead, string variant, Action<string, bool, string> record)
    {
        void Check(string name, bool pass, string detail = "") => record(variant + "/" + name, pass, detail);
        var field = Descendants(editor).OfType<GXValueField>().Single(c => c.Name == "ClockBaseTB");
        var combo = Descendants(field).OfType<ComboBox>().Single();
        Check("original-bound-controls-reused", ReferenceEquals(field, originalField) && ReferenceEquals(combo, originalCombo) && ReferenceEquals(field.Target, model));
        Check("attribute9-combo-binding", field.Index == 9 && field.Type == ValueFieldType.CompoBox);
        Check("six-typed-enum-items", combo.Items.Count == values.Length && combo.Items.Cast<object>().SequenceEqual(values.Cast<object>()), string.Join(",", combo.Items.Cast<object>().Select(c => c.GetType().Name + ":" + c)));
        Check("selected-value", hasRead ? Equals(combo.SelectedItem, model.ClockBase) : combo.SelectedIndex == -1 && combo.Text == "", "Selected=" + combo.SelectedItem + "; model=" + model.ClockBase + "; read=" + hasRead);
        Check("access-state", combo.Enabled == writable && field.ReadOnly == !writable);
        Check("wrapper-background-blends", field.Parent != null && field.BackColor.ToArgb() == field.Parent.BackColor.ToArgb(), "Field=" + field.BackColor + "; Parent=" + field.Parent?.BackColor);
        Check("wrapper-zero-padding", field.Padding == Padding.Empty, field.Padding.ToString());
        CheckNativeComboHeight(combo, Check);
        Check("native-text-viewport-readable", combo.ClientSize.Height >= combo.Font.Height, "Client height=" + combo.ClientSize.Height + "; font height=" + combo.Font.Height);
        Check("native-full-width", combo.Left == 0 && combo.Width == field.ClientSize.Width, "Combo=" + combo.Bounds + "; wrapper=" + field.ClientSize);
        Check("native-vertically-centered", Math.Abs(combo.Top - (field.ClientSize.Height - combo.Height) / 2) <= 1, "Combo=" + combo.Bounds + "; wrapper=" + field.ClientSize);
        Check("native-bounds-fit", combo.Top >= 0 && combo.Bottom <= field.ClientSize.Height && combo.Width >= 100, "Combo=" + combo.Bounds + "; wrapper=" + field.ClientSize);
        Check("native-style-and-font-retained", combo.FlatStyle == FlatStyle.Standard && Math.Abs(combo.Font.SizeInPoints - field.Font.SizeInPoints) < .1 && Math.Abs(combo.Font.SizeInPoints - shell.Font.SizeInPoints) < .1, "Font=" + combo.Font.SizeInPoints + "; shell=" + shell.Font.SizeInPoints);
        if (writable)
        {
            combo.Select(); Check("native-keyboard-focus-target", combo.CanFocus && combo.CanSelect && combo.TabStop && field.ActiveControl == combo);
        }
        else Check("readonly-not-keyboard-editable", !combo.Enabled && !combo.CanSelect);
        using var image = new Bitmap(Math.Max(1, field.Width), Math.Max(1, field.Height)); field.DrawToBitmap(image, new Rectangle(Point.Empty, field.Size));
        var samples = new List<Point>();
        if (combo.Top > 0) samples.AddRange(new[] { new Point(0, 0), new Point(field.Width / 2, 0), new Point(field.Width - 1, 0) });
        if (combo.Bottom < field.Height) samples.AddRange(new[] { new Point(0, field.Height - 1), new Point(field.Width / 2, field.Height - 1), new Point(field.Width - 1, field.Height - 1) });
        Check("no-second-visible-wrapper-border", samples.All(p => image.GetPixel(p.X, p.Y).ToArgb() == field.Parent!.BackColor.ToArgb()), "Border strip samples=" + samples.Count);
    }

    static void SetDataEnumFixture(GXDLMSData data)
    {
        data.SetDataType(2, DataType.Enum);
        var metadata = data.Attributes.Find(2);
        metadata.UIValueType = ValueFieldType.CompoBox; metadata.Values.Clear();
        metadata.Values.Add(new GXObisValueItem(0, "Disabled"));
        metadata.Values.Add(new GXObisValueItem(1, "Automatic"));
        metadata.Values.Add(new GXObisValueItem(2, "Manual"));
        data.Value = 1; data.SetAccess(2, AccessMode.ReadWrite); data.SetLastReadTime(2, SampleTime);
    }

    static void CheckVisibleCombos(Form editor, CaseResult result, string variant)
    {
        var view = (IGXDLMSView)editor;
        foreach (var combo in Descendants(editor).OfType<ComboBox>().Where(c => c.Visible).ToArray())
        {
            string path = ControlPath(combo);
            var field = combo.Parent as GXValueField;
            var entry = result.Combos.FirstOrDefault(c => c.Path == path);
            if (entry == null)
            {
                entry = new() { Path = path, Name = field?.Name ?? combo.Name, NativeName = combo.Name, Kind = field == null ? "Direct" : "GXValueField", AttributeIndex = field?.Index ?? 0, HostType = combo.Parent?.GetType().Name ?? "", NativeInstanceId = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(combo) };
                result.Combos.Add(entry);
            }
            entry.VisibleObserved = true; entry.ItemTypes = combo.Items.Cast<object>().Select(c => c.GetType().FullName ?? "").Distinct().ToArray(); entry.Items = combo.Items.Cast<object>().Select(c => c.ToString() ?? "").ToArray(); entry.Variants.Add(variant);
            void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = result.View.Split('.').Last() + "/" + result.State + "/" + entry.Name + "/" + variant + "/" + name, Passed = pass, Detail = detail });
            Check("native-instance-retained", entry.NativeInstanceId == System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(combo));
            var ancestors = new List<Control>(); for (Control? parent = combo.Parent; parent != null && parent != editor; parent = parent.Parent) ancestors.Add(parent);
            Check("no-painted-wrapper", !ancestors.Any(c => c.GetType().Name == "ModernFieldFrame"), string.Join("/", ancestors.Select(c => c.GetType().Name)));
            if (combo.Name == "ActiveMasksCb") Check("active-mask-native-standard-frame", combo.FlatStyle == FlatStyle.Standard && combo.Parent?.GetType().Name == "NativeComboHost", combo.FlatStyle.ToString());
            CheckNativeComboHeight(combo, Check);
            Check("text-viewport-readable", combo.ClientSize.Height >= combo.Font.Height, $"Client={combo.ClientSize}; font height={combo.Font.Height}");
            Check("inside-parent", combo.Left >= 0 && combo.Top >= 0 && combo.Right <= combo.Parent!.ClientSize.Width && combo.Bottom <= combo.Parent.ClientSize.Height, $"Combo={combo.Bounds}; parent={combo.Parent?.ClientSize}");
            var viewportLocation = editor.PointToClient(combo.PointToScreen(Point.Empty));
            Check("horizontal-viewport-reachable", (viewportLocation.X >= 0 && viewportLocation.X + combo.Width <= editor.ClientSize.Width) || ancestors.OfType<ScrollableControl>().Any(c => c.HorizontalScroll.Visible), $"Editor={editor.ClientSize}; combo x={viewportLocation.X}, width={combo.Width}; ancestor horizontal scroll={ancestors.OfType<ScrollableControl>().Any(c => c.HorizontalScroll.Visible)}");
            Check("native-keyboard-state", combo.Enabled ? combo.CanSelect && combo.CanFocus && combo.TabStop : !combo.CanSelect, "Enabled=" + combo.Enabled + "; TabStop=" + combo.TabStop);
            bool hosted = field != null || combo.Parent?.GetType().Name == "NativeComboHost";
            if (hosted)
            {
                var host = combo.Parent!;
                Check("full-host-width", combo.Left == 0 && combo.Width == host.ClientSize.Width, $"Combo={combo.Bounds}; host={host.ClientSize}");
                Check("centered-native-height", Math.Abs(combo.Top - Math.Max(0, (host.ClientSize.Height - combo.Height) / 2)) <= 1, $"Combo={combo.Bounds}; host={host.ClientSize}");
                Check("host-zero-padding", host.Padding == Padding.Empty, host.Padding.ToString());
                Check("host-parent-background", host.Parent != null && host.BackColor.ToArgb() == host.Parent.BackColor.ToArgb(), "Host=" + host.BackColor + "; parent=" + host.Parent?.BackColor);
                using var bitmap = new Bitmap(Math.Max(1, host.Width), Math.Max(1, host.Height)); host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.Size));
                var pixels = new List<Point>();
                if (combo.Top > 0) pixels.AddRange(new[] { new Point(0, 0), new Point(host.Width / 2, 0), new Point(host.Width - 1, 0) });
                if (combo.Bottom < host.Height) pixels.AddRange(new[] { new Point(0, host.Height - 1), new Point(host.Width / 2, host.Height - 1), new Point(host.Width - 1, host.Height - 1) });
                Check("no-secondary-border-pixels", pixels.All(p => bitmap.GetPixel(p.X, p.Y).ToArgb() == host.Parent!.BackColor.ToArgb()), "Outside-native strip samples=" + pixels.Count);
            }
            if (field != null) Check("bound-combo-type", field.Type == ValueFieldType.CompoBox && ReferenceEquals(field.Target, view.Target), "Index=" + field.Index);
            if (!entry.BehaviorChecked)
            {
                entry.BehaviorChecked = true;
                CheckComboBinding(editor, view, combo, field, entry, Check);
            }
        }
    }

    static void CheckComboBinding(Form editor, IGXDLMSView view, ComboBox combo, GXValueField? field, ComboEntry entry, Action<string, bool, string> check)
    {
        var model = view.Target; var client = new GXDLMSClient();
        void Check(string name, bool pass, string detail = "") => check(name, pass, detail);
        void Refresh(bool connected = true) { GXDlmsUi.ObjectChanged(view, client, model, connected); editor.PerformLayout(); Pump(); }
        if (field != null && field.Index > 0 && combo.Items.Count > 1 && combo.Items.Cast<object>().All(c => c is Enum) && model.GetValues()[field.Index - 1] is Enum originalValue)
        {
            int index = field.Index; var access = model.GetAccess(index); var readTime = model.GetLastReadTime(index);
            var originalItems = combo.Items.Cast<object>().ToArray();
            try
            {
                model.SetAccess(index, AccessMode.ReadWrite); model.SetLastReadTime(index, SampleTime); Refresh();
                var alternate = combo.Items.Cast<object>().First(c => !Equals(c, originalValue));
                combo.SelectedItem = alternate; Pump();
                Check("typed-selection-commits", Equals(model.GetValues()[index - 1], alternate) && model.GetDirty(index, out _), "Selected=" + alternate + "; model=" + model.GetValues()[index - 1]);
                combo.SelectedItem = originalValue; model.ClearDirty(index); Refresh();
                Check("clean-binding-restores-selection", Equals(combo.SelectedItem, originalValue) && !model.GetDirty(index, out _) && combo.Items.Cast<object>().SequenceEqual(originalItems));
                model.SetLastReadTime(index, DateTime.MinValue); Refresh();
                Check("unread-value-blank", combo.SelectedIndex == -1 && combo.Text.Length == 0, "Text=" + combo.Text);
                model.SetLastReadTime(index, SampleTime); model.SetAccess(index, AccessMode.Read); Refresh();
                Check("read-only-disabled", !combo.Enabled && field.ReadOnly);
                model.SetAccess(index, AccessMode.ReadWrite); Refresh(false);
                Check("disconnected-disabled", !combo.Enabled && field.ReadOnly);
                entry.BehaviorCoverage = "Typed enum selection commit, clean refresh, unread, read-only and disconnected binding.";
            }
            finally
            {
                model.SetAccess(index, access); model.SetLastReadTime(index, readTime); model.ClearDirty(index); Refresh();
            }
        }
        else if (field == null && combo.Name == "UnitTB" && (model is GXDLMSRegister || model is GXDLMSDemandRegister))
        {
            object original = combo.SelectedItem!; int index = model is GXDLMSDemandRegister ? 4 : 3;
            var property = model.GetType().GetProperty("Unit")!;
            var alternate = combo.Items.Cast<object>().First(c => c is Unit && !Equals(c, original));
            combo.SelectedItem = alternate; Pump(); Check("unit-selection-commits", Equals(property.GetValue(model), alternate) && model.GetDirty(index, out _));
            combo.SelectedItem = original; model.ClearDirty(index); Refresh();
            Check("unit-selection-refresh", Equals(combo.SelectedItem, original) && !model.GetDirty(index, out _));
            entry.BehaviorCoverage = "Direct Unit enum selection and clean refresh.";
        }
        else if (field == null && model is GXDLMSPushSetup push && (combo.Name == "ServiceCB" || combo.Name == "MessageCB"))
        {
            var property = model.GetType().GetProperty(combo.Name == "ServiceCB" ? "Service" : "Message")!;
            object original = property.GetValue(model)!; var alternate = combo.Items.Cast<object>().First(c => !Equals(c, original));
            combo.SelectedItem = alternate; Pump(); Check("push-selection-commits", Equals(property.GetValue(push), alternate) && model.GetDirty(3, out _));
            combo.SelectedItem = original; model.ClearDirty(3); Refresh(); Check("push-selection-refresh", Equals(combo.SelectedItem, original));
            entry.BehaviorCoverage = "Direct Push enum selection and clean refresh; access checks also run in existing Push behavior suite.";
        }
        else if (field == null && model is GXDLMSRegisterActivation activation && combo.Name == "ActiveMasksCb")
        {
            var original = activation.ActiveMask.ToArray();
            int originalIndex = combo.SelectedIndex;
            int alternateIndex = originalIndex == 0 ? 1 : 0;
            combo.SelectedIndex = alternateIndex; Pump();
            Check("active-mask-selection-commits", activation.ActiveMask.SequenceEqual(activation.MaskList[alternateIndex].Key) && model.GetDirty(4, out _));
            combo.SelectedIndex = originalIndex; model.ClearDirty(4);
            Refresh();
            Check("active-mask-full-refresh-preserves-selection", activation.ActiveMask.SequenceEqual(original) && combo.SelectedIndex == originalIndex && combo.Items.Count == activation.MaskList.Count && !model.GetDirty(4, out _));
            entry.BehaviorCoverage = "Direct active-mask selection mapping and full clear/repopulate binding refresh with two populated masks.";
        }
        else entry.BehaviorCoverage = "Layout/items/keyboard inspected; specialized direct or metadata mapping not generically mutated.";
    }

    static void CheckDataEnumTransitions(Form editor, IGXDLMSView view, GXDLMSData data, GXDLMSClient client, CaseResult result, string output, Size size, string variant)
    {
        var field = Descendants(editor).OfType<GXValueField>().Single(c => c.Name == "ValueTB");
        var native = Descendants(field).OfType<ComboBox>().Single(); var text = Descendants(field).OfType<TextBox>().Single();
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = "DataEnum/" + size.Width + "/" + variant + "/" + name, Passed = pass, Detail = detail });
        void Refresh() { data.ClearDirty(2); GXDlmsUi.ObjectChanged(view, client, data, true); editor.PerformLayout(); for (int i = 0; i < 4; i++) Pump(); }
        void Scalar() { data.SetDataType(2, DataType.String); var metadata = data.Attributes.Find(2); metadata.Values.Clear(); metadata.UIValueType = ValueFieldType.TextBox; data.Value = "Plain scalar value"; data.SetAccess(2, AccessMode.ReadWrite); data.SetLastReadTime(2, SampleTime); Refresh(); }
        Scalar(); var padding = field.Padding; var background = field.BackColor; int height = field.Height;
        Check("initial-scalar", field.Type == ValueFieldType.TextBox && text.Visible && !native.Visible && text.Text == "Plain scalar value");
        Capture(editor, result, output, size, variant + "-scalar-before");
        SetDataEnumFixture(data); Refresh();
        Check("metadata-switch-to-combo", field.Type == ValueFieldType.CompoBox && native.Visible && !text.Visible && native.Items.Cast<object>().Select(c => c.ToString()).SequenceEqual(new[] { "Disabled", "Automatic", "Manual" }) && native.Text == "Automatic");
        native.SelectedIndex = 2; Pump(); Check("metadata-numeric-mapping", Convert.ToInt32(data.Value) == 2 && data.GetDirty(2, out _)); Refresh();
        Capture(editor, result, output, size, variant + "-enum-selected");
        data.SetAccess(2, AccessMode.Read); Refresh(); Check("metadata-readonly", !native.Enabled && field.ReadOnly);
        Capture(editor, result, output, size, variant + "-enum-readonly");
        data.SetAccess(2, AccessMode.ReadWrite); data.SetLastReadTime(2, DateTime.MinValue); Refresh(); Check("metadata-unread", native.SelectedIndex == -1 && native.Text == "");
        Capture(editor, result, output, size, variant + "-enum-unread");
        Scalar();
        Check("scalar-presentation-restored", field.Type == ValueFieldType.TextBox && text.Visible && !native.Visible && field.Padding == padding && field.BackColor == background && field.Height == height, "Padding=" + field.Padding + "; background=" + field.BackColor + "; height=" + field.Height);
        using (var bitmap = new Bitmap(field.Width, field.Height)) { field.DrawToBitmap(bitmap, new Rectangle(Point.Empty, field.Size)); Check("scalar-border-restored", bitmap.GetPixel(0, 0).ToArgb() != field.BackColor.ToArgb()); }
        Capture(editor, result, output, size, variant + "-scalar-after");
        SetDataEnumFixture(data); Refresh();
    }

    static void RecordInactiveCombos(Form editor, CaseResult result)
    {
        foreach (var combo in Descendants(editor).OfType<ComboBox>())
        {
            string path = ControlPath(combo); if (result.Combos.Any(c => c.Path == path)) continue;
            var field = combo.Parent as GXValueField;
            result.Combos.Add(new() { Path = path, Name = field?.Name ?? combo.Name, NativeName = combo.Name, Kind = field == null ? "Direct" : "GXValueField", AttributeIndex = field?.Index ?? 0, HostType = combo.Parent?.GetType().Name ?? "", VisibleObserved = false, BehaviorCoverage = field != null && field.Type != ValueFieldType.CompoBox ? "Inactive generic child: current field type=" + field.Type : "Not visible in exercised tabs/model version." });
        }
    }

    static void WriteComboLedger(string output, InventoryResult inventory, List<CaseResult> cases, List<Regression> checks)
    {
        var combos = cases.SelectMany(c => c.Combos.Select(combo => new { c.View, c.State, c.Monitor, Combo = combo })).ToArray();
        Write(Path.Combine(output, "combo-inventory.json"), combos);
        Write(Path.Combine(output, "combo-summary.json"), new { inventory.AssemblySha256, CompiledViews = inventory.ObjectViewCount, HistoricalInventoryViews = 73, HistoricalFixtureCases = 147, SelectedViews = cases.Select(c => c.View).Distinct().Count(), Cases = cases.Count, Captures = cases.Sum(c => c.Captures.Count(x => x.Status == "Captured")), VisibleComboInstances = combos.Count(c => c.Combo.VisibleObserved), InactiveGenericChildren = combos.Count(c => !c.Combo.VisibleObserved && c.Combo.Kind == "GXValueField"), Checks = checks.Count, FailedChecks = checks.Count(c => !c.Passed), FailedCases = cases.Count(c => c.Status != "Passed"), Dpi = cases.SelectMany(c => c.Captures).Select(c => c.Dpi).Distinct().Order() });
        File.WriteAllText(Path.Combine(output, "combo-inventory.csv"), "View,State,Monitor,Name,Kind,Attribute,Host,VisibleObserved,Items,BehaviorCoverage\n" + string.Join("\n", combos.Select(c => string.Join(",", Csv(c.View), Csv(c.State), Csv(c.Monitor), Csv(c.Combo.Name), Csv(c.Combo.Kind), c.Combo.AttributeIndex, Csv(c.Combo.HostType), c.Combo.VisibleObserved, Csv(string.Join(" | ", c.Combo.Items)), Csv(c.Combo.BehaviorCoverage)))));
        File.AppendAllText(Path.Combine(output, "coverage-ledger.txt"), "\nAll-combo scope: selection derives from all147 prior fixtures across73 compiled object views, with38 views/96 visible combos identified. Fresh runs target those38 plus Data enum transitions and Security Setup version0. Inactive generic GXValueField combo children are explicitly distinguished. Fresh negative coverage for the remaining34 views is not claimed. Generic typed enums and explicit Unit/Push/Data mapping fixtures are mutated only in disposable local models; other direct protocol mappings are recorded as layout/items coverage.\n");
    }

    static InventoryResult Inventory()
    {
        var result = new InventoryResult { Assembly = Ui.Location, AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Ui.Location))), AssemblyVersion = Ui.GetName().Version?.ToString() ?? "", CreatedUtc = DateTime.UtcNow };
        foreach (var t in Ui.GetTypes().Where(t => !t.IsAbstract && typeof(Form).IsAssignableFrom(t)).OrderBy(t => t.FullName))
        {
            var attr = t.GetCustomAttribute<GXDLMSViewAttribute>();
            bool view = typeof(IGXDLMSView).IsAssignableFrom(t);
            result.Screens.Add(new() { Type = t.FullName!, ShortName = t.Name, Kind = view ? attr != null ? "ObjectView" : "UnattributedView" : "Dialog", ModelType = attr?.DLMSType.FullName ?? "", Versions = attr?.Versions ?? [], LogicalNames = attr?.LogicalNames ?? [], Standards = attr?.Standards.Select(s => s.ToString()).ToArray() ?? [], ButtonFields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f => typeof(Button).IsAssignableFrom(f.FieldType)).Select(f => f.Name).ToArray(), Constructors = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(c => "(" + string.Join(", ", c.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name + (p.IsOptional ? " = " + p.DefaultValue : ""))) + ")").ToArray(), Coverage = view && attr != null ? "Constructed/bound in isolated workers" : "Inventory only; requires explicit safe fixture before execution" });
        }
        result.ObjectViewCount = result.Screens.Count(s => s.Kind == "ObjectView");
        result.DialogCount = result.Screens.Count(s => s.Kind == "Dialog");
        result.UnattributedViewCount = result.Screens.Count(s => s.Kind == "UnattributedView");
        return result;
    }

    static int Worker(string typeName, string state, string output, int monitor = -1, bool comboAudit = false, bool layoutAudit = false, bool comboHeightAudit = false, bool scheduleAudit = false, bool parameterAudit = false, bool twistedAudit = false, bool imageAudit = false, bool registerActionsAudit = false, bool activationFooterAudit = false, bool profileHeaderAudit = false, bool scheduleFooterAudit = false, bool buttonAudit = false)
    {
        if (layoutAudit && !scheduleAudit && !parameterAudit && !twistedAudit && !imageAudit && !registerActionsAudit && !activationFooterAudit && !profileHeaderAudit && !scheduleFooterAudit && !buttonAudit) Sizes[0] = new Size(1500, 900);
        var result = new CaseResult { View = typeName, State = state, Status = "Started", ComboAudit = comboAudit, LayoutAudit = layoutAudit, ButtonAudit = buttonAudit, AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Ui.Location))) };
        void Save() => Write(Path.Combine(output, "result.json"), result);
        Save();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        ApplicationTypography.Install();
        string stage = "fixture";
        try
        {
            // General object sweeps must be as file-free as the focused Profile tests.
            using var isolatedSettings = new MemoryProfileSettings();
            var t = Ui.GetType(typeName, true)!;
            var attr = t.GetCustomAttribute<GXDLMSViewAttribute>();
            var model = attr == null ? null : CreateModel(attr, t.Name, state, result.FixtureNotes, comboAudit || buttonAudit);
            if (buttonAudit && model is GXDLMSAssociationShortName shortName) SeedShortNameGeneral(shortName, state);
            if (profileHeaderAudit) { PopulateProfileHeaderFixture((GXDLMSProfileGeneric)model!, state); result.BehaviorChecks.Add(new() { Name="ProfileHeader/fixture-owner", Passed=state!="italy-profile" || model!.LogicalName=="0.0.21.0.1.255" && model.Parent.Parent is GXDLMSClient owner && owner.Standard==Standard.Italy, Detail="Owner="+model!.Parent.Parent?.GetType().Name+"; LN="+model.LogicalName }); }
            if (scheduleFooterAudit || buttonAudit && model is GXDLMSSchedule) PopulateScheduleFooterFixture((GXDLMSSchedule)model!, state);
            if (buttonAudit && model is GXDLMSProfileGeneric buttonProfile && state.StartsWith("italy-"))
            {
                PopulateProfileHeaderFixture(buttonProfile, "italy-profile");
                if (state == "italy-enrolled") buttonProfile.LogicalName = "0.0.21.0.2.255";
            }
            stage = "construction"; Save();
            var monitorBounds = monitor >= 0 ? Screen.AllScreens[monitor].WorkingArea : Rectangle.Empty;
            using var shell = new AuditShell { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = monitor >= 0 ? new(monitorBounds.X + 24, monitorBounds.Y + 24) : new(-20000, -20000), Opacity = monitor >= 0 ? 0 : 1, ClientSize = Sizes[1], AutoValidate = AutoValidate.Disable, Font = ApplicationTypography.BodyFont, Text = "Offline UI audit" };
            // Establish the actual monitor/DPI before constructing hosted controls.
            shell.Show(); for (int settle = 0; settle < 6; settle++) Pump();
            using var editor = (Form)Activator.CreateInstance(t, true)!;
            if (layoutAudit && !buttonAudit) RememberInputRails(editor);
            if (buttonAudit) ButtonAuditMetrics.Remember(editor);
            if (registerActionsAudit) RememberRegisterActions(editor);
            if (activationFooterAudit) RememberActivationFooter(editor);
            if (scheduleFooterAudit) RememberScheduleFooter(editor);
            var originalScheduleInputs = scheduleAudit ? Descendants(editor).Where(c => ActionScheduleInputs.Contains(c.Name)).ToDictionary(c => c.Name) : null;
            var originalParameterInputs = parameterAudit ? Descendants(editor).Where(c => ParameterMonitorInputs.Contains(c.Name)).ToDictionary(c => c.Name) : null;
            var originalTwistedInputs = twistedAudit ? Descendants(editor).Where(c => TwistedInputs.Contains(c.Name)).ToDictionary(c => c.Name) : null;
            var originalImageInputs = imageAudit ? Descendants(editor).Where(c => ImageTransferInputs.Contains(c.Name)).ToDictionary(c => c.Name) : null;
            var originalProfileHeaderInputs = profileHeaderAudit ? Descendants(editor).Where(c => ProfileHeaderInputs.Contains(c.Name)).ToDictionary(c => c.Name) : null;
            editor.TopLevel = false; editor.FormBorderStyle = FormBorderStyle.None; editor.Dock = DockStyle.Fill; editor.AutoValidate = AutoValidate.Disable;
            var view = editor as IGXDLMSView;
            stage = "presentation"; Save();
            if (view != null) view.Target = model;
            shell.Controls.Add(editor); editor.Show(); for (int settle = 0; settle < 6; settle++) Pump();
            ModernPresentation.Apply(editor);
            if (view != null) GXDlmsUi.Init(view, (_, _) => { throw new InvalidOperationException("Audit must never invoke a meter action."); });
            shell.PerformLayout(); editor.PerformLayout(); Pump();
            stage = "binding"; Save();
            var client = new GXDLMSClient { Standard = attr?.Standards.FirstOrDefault() ?? Standard.DLMS };
            if ((profileHeaderAudit || buttonAudit) && state.StartsWith("italy-")) client.Standard = Standard.Italy;
            // Connected here controls local field availability only. There is no transport;
            // timestamps permit fixture values to take the same binding path as a read meter.
            if (t.Name == "GXDLMSMeterReadingParameterView") view!.OnValueChanged(new GXDLMSViewArguments { Client = client, Connected = true, Index = 2, Value = ((GXDLMSData)model!).Value });
            if (view != null) GXDlmsUi.ObjectChanged(view, client, model, !(buttonAudit && state == "disconnected"));
            result.BindingSucceeded = true;
            if (buttonAudit && model is GXDLMSAssociationShortName && state.StartsWith("short-name-general"))
                Descendants(editor).OfType<TextBox>().Single(c => c.Name == "SecretTB").Text = state.Contains("long")
                    ? string.Join(" ", Enumerable.Repeat("A1B2C3D4", 32)) : "41 42 43 44";
            if (imageAudit) PopulateImageTransferLog(editor, state);
            Pump();
            stage = "capture"; Save();
            int sizeIndex = 0;
            var workerSizes = buttonAudit ? new[] { new Size(1100,760), new Size(900,650), new Size(620,560), new Size(1100,760) } : profileHeaderAudit ? new[] { new Size(1500,900), new Size(1100,760), new Size(620,560), new Size(1500,900) } : imageAudit ? ImageFixtureSizes(shell, monitor) : twistedAudit ? new[] { new Size(1450, 690), new Size(1100, 650), new Size(620, 560), new Size(1450, 690) } : (monitor >= 0 ? Sizes.Concat(new[] { Sizes[0] }) : Sizes);
            foreach (var size in workerSizes)
            {
                shell.ClientSize = size; Pump(); shell.ClientSize = size; editor.Bounds = shell.ClientRectangle; shell.PerformLayout(); editor.PerformLayout(); for(int settle=0;settle<4;settle++)Pump();
                var tabs = Descendants(editor).OfType<TabControl>().ToArray();
                if (model is GXDLMSAssociationLogicalName && state.StartsWith("association-version")) { foreach (var focusedTab in tabs.Where(t => t.Name == "tabControl1")) focusedTab.SelectedIndex = 1; Pump(); }
                if (model is GXDLMSAssociationShortName && state.StartsWith("short-name-general")) { foreach (var tab in tabs.Where(t => t.Name == "tabControl1")) tab.SelectedIndex = 1; Pump(); }
                var selections = tabs.ToDictionary(tab => tab, tab => tab.SelectedIndex);
                if (twistedAudit)
                {
                    foreach (var scroller in Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(c => c.AutoScroll)) scroller.AutoScrollPosition = Point.Empty;
                    Pump();
                    CheckTwistedPair(editor, (GXDLMSIecTwistedPairSetup)model!, originalTwistedInputs!, result, sizeIndex);
                }
                if (imageAudit)
                {
                    foreach (var scroller in Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(c => c.AutoScroll)) scroller.AutoScrollPosition = Point.Empty;
                    Pump();
                    CheckImageTransfer(editor, (GXDLMSImageTransfer)model!, originalImageInputs!, result, sizeIndex, sizeIndex is 0 or 1 or 4);
                }
                if (registerActionsAudit)
                {
                    foreach (var scroller in Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(c => c.AutoScroll)) scroller.AutoScrollPosition = Point.Empty;
                    Pump();
                    CheckRegisterActions(editor, result, sizeIndex);
                }
                if (profileHeaderAudit)
                {
                    foreach (var scroller in Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(c => c.AutoScroll)) scroller.AutoScrollPosition = Point.Empty;
                    Pump(); CheckProfileHeader(editor, (GXDLMSProfileGeneric)model!, originalProfileHeaderInputs!, result, sizeIndex, output, size);
                }
                if (activationFooterAudit) CheckActivationFooter(editor, (GXDLMSRegisterActivation)model!, result, "size"+sizeIndex+"-initial");
                if (scheduleFooterAudit)
                {
                    foreach (var scroller in Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(c => c.AutoScroll)) scroller.AutoScrollPosition = Point.Empty;
                    Pump(); CheckScheduleFooter(editor, (GXDLMSSchedule)model!, result, sizeIndex);
                }
                if (sizeIndex == 0 && model is GXDLMSAssociationLogicalName && state.Contains("-body9"))
                {
                    foreach (var button in Descendants(editor).OfType<Button>().Where(b => b.Name is "UpdatePwBtn" or "UpdateHighPwBtn"))
                        button.Font = new Font("Segoe UI", 9f, button.Font.Style, GraphicsUnit.Point);
                    result.FixtureNotes.Add("Controlled initial settled-font regression: password buttons receive intended9pt Segoe UI once; body font is not altered and no per-capture font reapplication occurs.");
                    editor.PerformLayout(); for (int settle = 0; settle < 4; settle++) Pump();
                }
                string sizeVariant = ((profileHeaderAudit || buttonAudit) ? sizeIndex >= 3 : imageAudit ? sizeIndex >= 4 : twistedAudit ? sizeIndex >= 3 : sizeIndex >= 2) ? "restored" : "default";
                Capture(editor, result, output, size, sizeVariant); Save();
                if (imageAudit) CaptureImageTransferReveals(editor, result, output, size, sizeVariant);
                if (scheduleAudit) CheckActionSchedule(editor, (GXDLMSActionSchedule)model!, originalScheduleInputs!, result, sizeIndex);
                if (parameterAudit) CheckParameterMonitor(editor, (GXDLMSParameterMonitor)model!, originalParameterInputs!, result, sizeIndex);
                sizeIndex++;
                foreach (var tab in tabs)
                {
                    if (model is GXDLMSAssociationLogicalName && state.StartsWith("association-version")) continue;
                    int initial = selections[tab];
                    for (int index = 0; index < tab.TabPages.Count; index++)
                    {
                        foreach (var selection in selections) selection.Key.SelectedIndex = selection.Value;
                        // Nested controls require their ancestor tabs to be selected too.
                        // Selecting an invisible nested page alone would duplicate the
                        // active parent's screenshot and falsely claim passive-page coverage.
                        for (Control? ancestor = tab.Parent; ancestor != null; ancestor = ancestor.Parent)
                            if (ancestor is TabPage page && page.Parent is TabControl owner) owner.SelectedTab = page;
                        tab.SelectedIndex = index; Pump();
                        if (activationFooterAudit) CheckActivationFooter(editor, (GXDLMSRegisterActivation)model!, result, "size"+sizeIndex+"-tab"+index);
                        Capture(editor, result, output, size, "tab-" + Array.IndexOf(tabs, tab) + "-" + Safe(tab.Name) + "-" + index + (sizeIndex >= 3 ? "-restored" : "")); Save();
                    }
                    tab.SelectedIndex = initial;
                }
                foreach (var selection in selections) selection.Key.SelectedIndex = selection.Value;
                if (comboHeightAudit) CheckComboHeightStates(editor, view!, client, result, output, size, "size" + sizeIndex);
                if (comboAudit && model is GXDLMSData enumData && t.Name == "GXDLMSDataView") CheckDataEnumTransitions(editor, view!, enumData, client, result, output, size, "size" + sizeIndex);
            }
            if (view != null && !layoutAudit)
            {
                var provider = view.ErrorProvider ?? t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(f => typeof(ErrorProvider).IsAssignableFrom(f.FieldType)).Select(f => f.GetValue(editor)).OfType<ErrorProvider>().FirstOrDefault();
                if (view.ErrorProvider == null && provider != null) result.FixtureNotes.Add("Dirty indicator inspection uses the actual ErrorProvider component because the interface getter returns null.");
                foreach (int index in Enumerable.Range(1, ((IGXDLMSBase)model!).GetAttributeCount()))
                {
                    try {
                        provider?.Clear(); view.OnDirtyChange(index, true);
                        bool indicated = provider != null && Descendants(editor).Any(c => !string.IsNullOrEmpty(provider.GetError(c)));
                        view.OnDirtyChange(index, false);
                        bool clear = provider == null || Descendants(editor).All(c => string.IsNullOrEmpty(provider.GetError(c)));
                        if (indicated) result.BehaviorChecks.Add(new() { Name = t.Name + "/dirty-clean/attribute" + index, Passed = clear, Detail = "Dirty indicator appeared; clean cleared=" + clear });
                        if (t.Name == "GXDLMSDemandRegisterView" && index == 2) { result.DirtyIndicatorAppeared = indicated; result.CleanIndicatorCleared = clear; }
                    } catch (Exception ex) { result.BehaviorChecks.Add(new() { Name = t.Name + "/dirty-clean/attribute" + index, Passed = false, Detail = ex.Message }); }
                }
            }
            if (t.Name == "GXFunctionControlFunctionDlg") result.BehaviorChecks.Add(new() { Name = "FunctionControlFunction/accept-cancel", Passed = editor.AcceptButton != null && editor.CancelButton != null && editor.AcceptButton != editor.CancelButton, Detail = "Accept=" + (editor.AcceptButton as Control)?.Name + "; Cancel=" + (editor.CancelButton as Control)?.Name });
            if (!layoutAudit && view != null && !(comboAudit && model is GXDLMSData)) CheckBehavior(editor, view, model!, client, result.BehaviorChecks);
            if (comboAudit) RecordInactiveCombos(editor, result);
            if (scheduleAudit) CheckActionScheduleReachability(result);
            if (parameterAudit) CheckParameterMonitorReachability(result);
            if (twistedAudit) CheckTwistedPairReachability(result);
            if (registerActionsAudit) CheckRegisterActionReachability(result);
            if (activationFooterAudit) CheckActivationRefresh(editor, (GXDLMSRegisterActivation)model!, client, result);
            if (scheduleFooterAudit) CheckScheduleFooterRefresh(editor, (GXDLMSSchedule)model!, client, result);
            if (profileHeaderAudit) { CheckProfileHeaderInteractions(editor, (GXDLMSProfileGeneric)model!, client, result); CheckProfileHeaderReachability(result); }
            if (imageAudit)
            {
                ImageTransferMonitorRoundtrip(shell, editor, (GXDLMSImageTransfer)model!, originalImageInputs!, result, output, monitor);
                CheckImageTransferReachability(result);
            }
            if (buttonAudit && (state == "roundtrip" || state.EndsWith("-roundtrip"))) ReviewButtonRoundtrip(shell, editor, result, output);
            if (layoutAudit && !buttonAudit && model != null) CheckRailInteractions(editor, model, result);
            if (buttonAudit) result.BehaviorChecks.Add(new() { Name = "Buttons/original-instances-method-indices-actions-retained", Passed = ButtonAuditMetrics.OriginalsRetained(editor) });
            isolatedSettings.Dispose();
            result.BehaviorChecks.Add(new() { Name = t.Name + "/offline-settings-isolated", Passed = isolatedSettings.Provider.SaveAttempts == 0 && isolatedSettings.Restored, Detail = "File-free settings provider; Save attempts=" + isolatedSettings.Provider.SaveAttempts + "; original singleton restored=" + isolatedSettings.Restored });
            result.Monitor = monitor >= 0 ? Screen.AllScreens[monitor].DeviceName : "Offscreen default monitor";
            result.Status = "Passed";
            shell.Hide();
        }
        catch (Exception ex)
        {
            result.Status = stage + "Error";
            result.Errors.Add(ex.ToString());
        }
        Save();
        return result.Status == "Passed" ? 0 : 1;
    }

    static GXDLMSObject CreateModel(GXDLMSViewAttribute attr, string viewName, string state, List<string> notes, bool comboFixture = false)
    {
        // The generic fallback requires an actual IGXDLMSBase model.
        var model = attr.DLMSType == typeof(GXDLMSObject) ? new GXDLMSData() : (GXDLMSObject)Activator.CreateInstance(attr.DLMSType)!;
        if (attr.Versions.Length > 0) model.Version = attr.Versions[0];
        if (state == "version0") model.Version = 0;
        if (comboFixture && (model is GXDLMSG3Plc6LoWPan || model is GXDLMSMBusClient))
        {
            model.Version = (byte)((IGXDLMSBase)model).GetMaxSupportedVersion();
            notes.Add("Combo fixture uses supported model version " + model.Version + " so the visible device-type/encryption-status enum attribute is available. Version 0 leaves this later attribute unbound in the existing view.");
        }
        model.LogicalName = attr.LogicalNames?.FirstOrDefault() ?? (string.IsNullOrEmpty(model.LogicalName) ? "0.0.96.1.0.255" : model.LogicalName);
        if (state != "blank")
        {
            model.Description = "Laboratory reference meter / representative saved configuration";
            // Fill deterministic scalar fields only. Do not invent protocol collection rows.
            // Complex collection coverage is explicitly recorded for each case below.
            foreach (var property in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0 && p.DeclaringType != typeof(GXDLMSObject)))
            {
                try
                {
                    object? value = null; var pt = property.PropertyType;
                    if (pt == typeof(GXDateTime)) value = new GXDateTime(SampleTime);
                    else if (pt == typeof(GXDate)) value = new GXDate(SampleTime);
                    else if (pt == typeof(GXTime)) value = new GXTime(SampleTime);
                    else if (pt == typeof(DateTime)) value = SampleTime;
                    else if (pt.IsEnum && !pt.IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(pt, property.GetValue(model)!) && Enum.GetValues(pt).Length > 0) value = Enum.GetValues(pt).GetValue(0);
                    else if (pt == typeof(string) && string.IsNullOrEmpty((string?)property.GetValue(model)))
                    {
                        value = property.Name.Contains("Address") ? "192.0.2.10" : property.Name.Contains("LogicalName") || property.Name.EndsWith("Reference") ? "0.0.96.1.0.255" : property.Name.Contains("Password") ? "test-fixture" : "Example " + property.Name;
                    }
                    // Opaque protocol bytes (compact templates, certificates, keys) need
                    // valid schema-specific fixtures; arbitrary bytes create false failures.
                    else if (pt.IsPrimitive && pt != typeof(bool) && pt != typeof(char) && Convert.ToDouble(property.GetValue(model)) == 0) value = Convert.ChangeType(1, pt);
                    if (value != null) { property.SetValue(model, value); notes.Add("Populated " + property.Name); }
                }
                catch (Exception ex) { notes.Add("Kept default " + property.Name + ": " + ex.GetBaseException().Message); }
            }
            Set(model, "Value", 12846.72); Set(model, "CurrentAverageValue", 12.45); Set(model, "LastAverageValue", 11.82); Set(model, "Scaler", 0.01d); Set(model, "Unit", model is GXDLMSDemandRegister ? Unit.ActivePower : Unit.ActiveEnergy); Set(model, "Status", 0); Set(model, "Period", 900u); Set(model, "NumberOfPeriods", (ushort)1);
        }
        if (model is GXDLMSData data)
        {
            if (viewName == "GXDLMSUnixTimeView") { data.SetDataType(2, DataType.UInt32); data.Value = state == "blank" ? 0u : GXDateTime.ToUnixTime(SampleTime); }
            else if (viewName == "GXDLMSMeterReadingParameterView") { data.SetDataType(2, DataType.OctetString); data.Value = state == "blank" ? new byte[6] : new byte[] { 0, 2, 0, 0, 3, 132 }; }
            else if (state == "structured") { data.SetDataType(2, DataType.Array); data.Value = new GXArray { new GXStructure { "Phase L1", 230.15, 12.4 }, new GXStructure { "Phase L2", 229.8, 11.7 }, new GXStructure { "Phase L3", 231.2, 12.1 } }; }
            else if (state == "enum") SetDataEnumFixture(data);
            else { data.SetDataType(2, DataType.String); data.Value = state == "blank" ? null : "SM-LAB-2026-000182 / Firmware 3.2.17 / Manufacturer test unit"; }
        }
        if (state == "structured" && model is GXDLMSPppSetup ppp)
        {
            ppp.SetDataType(3, DataType.Array); ppp.SetDataType(4, DataType.Array);
            ppp.LCPOptions = new[]
            {
                new GXDLMSPppSetupLcpOption { Type = Gurux.DLMS.Objects.Enums.PppSetupLcpOptionType.MaxRecUnit, Length = 4, Data = (ushort)1500 },
                new GXDLMSPppSetupLcpOption { Type = Gurux.DLMS.Objects.Enums.PppSetupLcpOptionType.MagicNumber, Length = 6, Data = 0x12345678u }
            };
            ppp.IPCPOptions = new[]
            {
                new GXDLMSPppSetupIPCPOption { Type = Gurux.DLMS.Objects.Enums.PppSetupIPCPOptionType.PrefLocalIP, Length = 6, Data = 0xC000020Au },
                new GXDLMSPppSetupIPCPOption { Type = Gurux.DLMS.Objects.Enums.PppSetupIPCPOptionType.PrefPeerIP, Length = 6, Data = 0xC0000201u }
            };
            notes.Add("Explicit populated LCP/IPCP option arrays exercise dynamic structured row height and width.");
        }
        if (state == "structured" && model is GXDLMSIp4Setup ip4)
        {
            ip4.SetDataType(4, DataType.Array); ip4.SetDataType(5, DataType.Array);
            ip4.MulticastIPAddress = new[] { System.Net.IPAddress.Parse("239.192.0.1"), System.Net.IPAddress.Parse("239.192.0.2") };
            ip4.IPOptions = new[] { new GXDLMSIp4SetupIpOption { Type = Gurux.DLMS.Objects.Enums.Ip4SetupIpOptionType.RecordRoute, Length = 7, Data = new byte[] { 4, 192, 0, 2, 10 } } };
            notes.Add("Explicit multicast addresses and record-route option exercise structured arrays beside bounded scalar fields.");
        }
        if (model is GXDLMSIecTwistedPairSetup twisted && state != "blank")
        {
            twisted.PrimaryAddresses = state == "long-twisted" ? Enumerable.Range(0, 256).Select(i => (byte)i).ToArray() : new byte[] { 1, 12, 127, 255 };
            twisted.Tabis = state == "long-twisted" ? Enumerable.Range(0, 128).Select(i => (sbyte)i).ToArray() : new sbyte[] { 0, 1, 12, 127 };
            if (state == "long-twisted") twisted.Description = "IEC twisted pair commissioning configuration with a deliberately long descriptive name; complete byte and signed-byte address ranges";
            notes.Add("Explicit legal byte/sbyte list values; the long fixture fills all256 addresses and128 nonnegative TAB values and uses a long description. List Add/Edit dialogs are not invoked.");
        }
        if (model is GXDLMSImageTransfer image) PopulateImageTransferFixture(image, state);
        var peers = new GXDLMSObjectCollection { model };
        foreach (var peer in new GXDLMSObject[] { new GXDLMSData("0.0.96.1.1.255"), new GXDLMSRegister("1.0.1.8.0.255"), new GXDLMSScriptTable("0.0.10.0.0.255"), new GXDLMSCredit("0.0.19.10.0.255"), new GXDLMSCharge("0.0.19.20.0.255") }) if (peer.GetType() != model.GetType()) peers.Add(peer);
        if (state != "blank") PopulateCollections(model, peers, notes);
        if (state == "long-script" && model is GXDLMSActionSchedule schedule && schedule.Target != null)
        {
            schedule.Target.Description = "Regional tariff transition and scheduled demand reset - laboratory commissioning script with a deliberately long descriptive name";
            notes.Add("Long script display name exercises native combo width without changing object identity.");
        }
        if (state == "long-target-value" && model is GXDLMSParameterMonitor parameters)
        {
            parameters.ChangedParameter.Target.Description = "Regional total import energy register - laboratory meter with a deliberately long descriptive target name";
            parameters.ChangedParameter.Value = "A long changed parameter value retained in the native read-only field without widening the shared property rail or clipping the execution actions";
            notes.Add("Long target description and scalar value retain their complete bound strings.");
        }
        if (model is GXDLMSRegisterActivation footerActivation) PopulateActivationFooterFixture(footerActivation, state);
        notes.Add("Protocol-specific collection branches not seeded by PopulateCollections retain model defaults.");
        if (model is GXDLMSAssociationLogicalName && state.StartsWith("association-version"))
        {
            model.Version = byte.Parse(new string(state.Substring("association-version".Length).TakeWhile(char.IsDigit).ToArray()));
            notes.Add("Explicit Association LN version fixture: " + model.Version);
        }
        for (int i = 1; i <= ((IGXDLMSBase)model).GetAttributeCount(); i++) model.SetLastReadTime(i, SampleTime);
        return model;
    }

    static void Set(object model, string propertyName, object value)
    {
        var property = model.GetType().GetProperty(propertyName);
        if (property?.CanWrite != true) return;
        try { property.SetValue(model, property.PropertyType.IsInstanceOfType(value) ? value : Convert.ChangeType(value, property.PropertyType)); } catch { }
    }

    static void PopulateCollections(GXDLMSObject model, GXDLMSObjectCollection peers, List<string> notes)
    {
        var register = peers.OfType<GXDLMSRegister>().FirstOrDefault() ?? new GXDLMSRegister("1.0.1.8.0.255");
        register.Value = 12846.72; register.Scaler = .01; register.Unit = Unit.ActiveEnergy;
        var scriptTable = peers.OfType<GXDLMSScriptTable>().FirstOrDefault() ?? new GXDLMSScriptTable("0.0.10.0.0.255");
        var script = new GXDLMSScript { Id = 1 };
        script.Actions.Add(new GXDLMSScriptAction { Target = register, Index = 2, Type = Gurux.DLMS.Objects.Enums.ScriptActionType.Write, Parameter = 1, ParameterDataType = DataType.Int32 });
        scriptTable.Scripts.Add(script);
        var capture = new GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject>(register, new GXDLMSCaptureObject(2, 0));
        switch (model)
        {
            case GXDLMSAssociationLogicalName association: association.ObjectList.AddRange(peers.Where(x => x != model)); notes.Add("Populated association objects/access rows."); break;
            case GXDLMSAssociationShortName association: association.ObjectList.AddRange(peers.Where(x => x != model)); notes.Add("Populated short-name association objects."); break;
            case GXDLMSScriptTable: notes.Add("Populated script and write-action table."); break;
            case GXDLMSProfileGeneric profile: profile.CaptureObjects.Add(capture); profile.Buffer.Add(new object[] { 12846.72 }); profile.Buffer.Add(new object[] { 12847.15 }); profile.Buffer.Add(new object[] { 12848.03 }); profile.EntriesInUse = 3; notes.Add("Populated profile capture column and 3 buffer rows."); break;
            case GXDLMSPushSetup push: push.PushObjectList.Add(capture); notes.Add("Populated push target object."); break;
            case GXDLMSActionSchedule schedule: schedule.Target = scriptTable; schedule.ExecutedScriptSelector = 1; schedule.ExecutionTime = new[] { new GXDateTime(SampleTime), new GXDateTime(SampleTime.AddDays(1)) }; notes.Add("Populated action schedule entries."); break;
            case GXDLMSParameterMonitor parameters:
                parameters.ChangedParameter = new GXDLMSTarget(register, 2) { Value = 12846.72 };
                parameters.CaptureTime = SampleTime;
                parameters.Parameters.Add(new GXDLMSTarget(register, 2));
                parameters.Parameters.Add(new GXDLMSTarget(peers.OfType<GXDLMSData>().First(), 2));
                notes.Add("Populated changed register/attribute/value, capture time and two monitored parameter rows."); break;
            case GXDLMSRegisterActivation activation:
                activation.RegisterAssignment = new[] { new GXDLMSObjectDefinition { ObjectType = register.ObjectType, LogicalName = register.LogicalName } };
                activation.MaskList.Add(new KeyValuePair<byte[], byte[]>(Encoding.ASCII.GetBytes("Day"), new byte[] { 1 }));
                activation.MaskList.Add(new KeyValuePair<byte[], byte[]>(Encoding.ASCII.GetBytes("Night"), new byte[] { 1 }));
                activation.ActiveMask = activation.MaskList[0].Key;
                notes.Add("Populated register assignment and two named masks with a selected active mask."); break;
            case GXDLMSRegisterMonitor monitor:
                monitor.Thresholds = new object[] { 10d, 20d }; monitor.MonitoredValue.ObjectType = ObjectType.Register; monitor.MonitoredValue.LogicalName = register.LogicalName; monitor.MonitoredValue.AttributeIndex = 2;
                monitor.Actions = new[] { new GXDLMSActionSet(), new GXDLMSActionSet() };
                foreach (var action in monitor.Actions) { action.ActionUp.LogicalName = action.ActionDown.LogicalName = scriptTable.LogicalName; action.ActionUp.ScriptSelector = action.ActionDown.ScriptSelector = 1; }
                notes.Add("Populated thresholds, monitored value, and action rows."); break;
            case GXDLMSActivityCalendar calendar:
                var day = new GXDLMSDayProfile { DayId = 1, DaySchedules = new[] { new GXDLMSDayProfileAction { StartTime = new GXTime(SampleTime), ScriptLogicalName = scriptTable.LogicalName, ScriptSelector = 1 } } };
                var week = new GXDLMSWeekProfile { Name = Encoding.ASCII.GetBytes("week1"), Monday = 1, Tuesday = 1, Wednesday = 1, Thursday = 1, Friday = 1, Saturday = 1, Sunday = 1 };
                var season = new GXDLMSSeasonProfile { Name = Encoding.ASCII.GetBytes("summer"), Start = new GXDateTime(SampleTime), WeekName = week.Name };
                calendar.CalendarNameActive = "Active calendar"; calendar.CalendarNamePassive = "Passive calendar";
                calendar.DayProfileTableActive = calendar.DayProfileTablePassive = new[] { day }; calendar.WeekProfileTableActive = calendar.WeekProfileTablePassive = new[] { week }; calendar.SeasonProfileActive = calendar.SeasonProfilePassive = new[] { season };
                notes.Add("Populated active/passive season, week, day and scheduled action tables."); break;
            case GXDLMSSecuritySetup security:
                security.Certificates.Add(new Gurux.DLMS.Objects.GXDLMSCertificateInfo { Entity = Gurux.DLMS.Objects.Enums.CertificateEntity.Server, Type = Gurux.DLMS.Objects.Enums.CertificateType.DigitalSignature, SerialNumber = 1, Issuer = "CN=Offline fixture issuer", Subject = "CN=Offline fixture meter", SubjectAltName = "DNS:fixture.invalid" });
                notes.Add("Populated certificate metadata only; no private keys, certificate generation, files or network."); break;
        }
    }

    static void CheckBehavior(Form editor, IGXDLMSView view, GXDLMSObject model, GXDLMSClient client, List<Regression> checks)
    {
        Control Find(string name) => Descendants(editor).First(c => c.Name == name);
        void Check(string name, bool pass, string detail = "") => checks.Add(new() { Name = model.GetType().Name + "/" + name, Passed = pass, Detail = detail });
        void Refresh(bool connected) => GXDlmsUi.ObjectChanged(view, client, model, connected);
        if (editor.GetType().Name == "GXDLMSDataView" && model is GXDLMSData scalarData)
        {
            var field = (GXValueField)Find("ValueTB");
            scalarData.SetDataType(2, DataType.String); scalarData.Value = "Scalar before transition"; Refresh(true); Pump();
            int scalarHeight = field.Height;
            scalarData.SetDataType(2, DataType.Array); scalarData.Value = new GXArray { new GXStructure { "Phase L1", 230.15 }, new GXStructure { "Phase L2", 229.8 }, new GXStructure { "Phase L3", 231.2 } }; Refresh(true); Pump();
            int structuredHeight = field.Height;
            Check("same-view-scalar-to-array-grows", structuredHeight >= 96 * editor.DeviceDpi / 96 && structuredHeight > scalarHeight, $"Scalar={scalarHeight}px; array={structuredHeight}px");
            scalarData.SetDataType(2, DataType.String); scalarData.Value = "Scalar after transition"; Refresh(true); Pump();
            Check("same-view-array-to-scalar-shrinks", Math.Abs(field.Height - scalarHeight) <= 2 && field.Height < structuredHeight, $"Before={scalarHeight}px; array={structuredHeight}px; after={field.Height}px");
            scalarData.SetDataType(2, DataType.Structure); scalarData.Value = new GXStructure { "Phase L1", 230.15, 12.4 }; Refresh(true); Pump();
            int structureHeight = field.Height;
            Check("same-view-scalar-to-structure-grows", structureHeight >= 96 * editor.DeviceDpi / 96 && structureHeight > scalarHeight, $"Scalar={scalarHeight}px; structure={structureHeight}px");
            scalarData.SetDataType(2, DataType.String); scalarData.Value = "Scalar after structure"; Refresh(true); Pump();
            Check("same-view-structure-to-scalar-shrinks", Math.Abs(field.Height - scalarHeight) <= 2 && field.Height < structureHeight, $"Before={scalarHeight}px; structure={structureHeight}px; after={field.Height}px");
        }
        if (editor.GetType().Name == "GXDLMSMeterReadingParameterView")
        {
            var values = (CheckedListBox)Find("ValuesLb");
            var data = (GXDLMSData)model;
            var original = (byte[])data.Value;
            string bits = new GXBitString(original.Take(2).ToArray(), 0).ToString();
            Check("six-byte-flags-bind", values.Items.Count == 16 && Enumerable.Range(0, 16).All(i => values.GetItemChecked(i) == (bits[i] == '1')));
            bool changed = !values.GetItemChecked(0); values.SetItemChecked(0, changed);
            view.OnValueChanged(new GXDLMSViewArguments { Client = client, Connected = true, Index = 2, Value = data.Value });
            Check("flag-edit-roundtrip", values.GetItemChecked(0) == changed && ((byte[])data.Value).Length == 6);
        }
        if (model is GXDLMSAssociationLogicalName association)
        {
            association.XDLMSContextInfo.Conformance = Conformance.Get | Conformance.DeltaValueEncoding;
            view.OnValueChanged(new GXDLMSViewArguments { Client = client, Connected = true, Index = 5 });
            var delta = (CheckBox)Find("DeltaValueEncodingCb");
            Check("conformance-live-indicator", delta.Checked && !delta.IsDisposed && delta.Parent != null, "Delta value encoding set via attribute5 refresh");
            association.XDLMSContextInfo.Conformance = Conformance.Get;
            view.OnValueChanged(new GXDLMSViewArguments { Client = client, Connected = true, Index = 5 });
            Check("conformance-clear-indicator", !delta.Checked);
        }
        if (model is GXDLMSPushSetup)
        {
            model.SetAccess(3, AccessMode.ReadWrite); Refresh(false);
            Check("destination-disabled-disconnected", !Find("ServiceCB").Enabled && !Find("MessageCB").Enabled);
            model.SetAccess(3, AccessMode.Read); Refresh(true);
            Check("destination-disabled-readonly", !Find("ServiceCB").Enabled && !Find("MessageCB").Enabled);
            model.SetAccess(3, AccessMode.ReadWrite); Refresh(true);
            Check("destination-enabled-writable", Find("ServiceCB").Enabled && Find("MessageCB").Enabled);
        }
        if (model is GXDLMSClock clock)
        {
            clock.TimeZone = -1; clock.Enabled = true;
            model.SetAccess(3, AccessMode.ReadWrite); Refresh(false);
            var zone = (CheckBox)Find("TimeZoneCb"); var zoneValue = (GXValueField)Find("TimeZoneTB");
            Check("timezone-disabled-disconnected", !zone.Enabled && !Find("CurrentTimeZoneBtn").Enabled && zoneValue.ReadOnly);
            model.SetAccess(3, AccessMode.Read); Refresh(true);
            Check("timezone-disabled-readonly", !zone.Enabled && !Find("CurrentTimeZoneBtn").Enabled && zoneValue.ReadOnly);
            zone.Checked = false; zone.Checked = true;
            Check("timezone-toggle-cannot-bypass-access", !Find("CurrentTimeZoneBtn").Enabled && zoneValue.ReadOnly);
            model.SetAccess(3, AccessMode.ReadWrite); clock.TimeZone = -1; Refresh(true);
            Check("timezone-minus-one-is-valid", zone.Checked && !zoneValue.ReadOnly && Find("CurrentTimeZoneBtn").Enabled);
            zone.Checked = false; Refresh(true);
            Check("timezone-disabled-survives-refresh", !zone.Checked && clock.TimeZone == short.MinValue && zoneValue.ReadOnly && !Find("CurrentTimeZoneBtn").Enabled, "Stored offset=" + clock.TimeZone);
            model.SetAccess(5, AccessMode.Read); model.SetAccess(6, AccessMode.ReadWrite); model.SetAccess(7, AccessMode.Read); model.SetAccess(8, AccessMode.ReadWrite); clock.Enabled = true; Refresh(true);
            Check("dst-mixed-access", ((GXValueField)Find("BeginTB")).ReadOnly && !((GXValueField)Find("EndTB")).ReadOnly && ((GXValueField)Find("DeviationTB")).ReadOnly);
            ((CheckBox)Find("EnabledCB")).Checked = false; ((CheckBox)Find("EnabledCB")).Checked = true;
            Check("dst-toggle-preserves-access", ((GXValueField)Find("BeginTB")).ReadOnly && !((GXValueField)Find("EndTB")).ReadOnly && ((GXValueField)Find("DeviationTB")).ReadOnly);
        }
    }

    static void Capture(Form editor, CaseResult result, string output, Size requested, string variant)
    {
        var scrollables = Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(s => s.Visible && s.AutoScroll).ToArray();
        foreach (var surface in scrollables) surface.AutoScrollPosition = Point.Empty;
        editor.PerformLayout(); Pump();
        CaptureAtPosition(editor, result, output, requested, variant);
        if (scrollables.Any(s => s.VerticalScroll.Visible))
        {
            foreach (var surface in scrollables.Where(s => s.VerticalScroll.Visible)) surface.AutoScrollPosition = new Point(0, int.MaxValue);
            Pump(); CaptureAtPosition(editor, result, output, requested, variant + "-bottom");
            foreach (var surface in scrollables) surface.AutoScrollPosition = Point.Empty;
            Pump();
        }
        if ((result.ComboAudit || result.LayoutAudit) && requested.Width == 620)
        {
            // Nested tabs can need an intermediate scroll position: scrolling
            // every ancestor to its bottom does not expose every native child.
            IEnumerable<Control> reveal = result.ButtonAudit ? Descendants(editor).OfType<Button>() : result.LayoutAudit
                ? Descendants(editor).Where(c => LayoutMetrics.Role(c) is "Scalar" or "Action" or "Choice")
                : Descendants(editor).OfType<ComboBox>();
            foreach (var combo in reveal.Where(c => c.Visible).ToArray())
            {
                string path = ControlPath(combo);
                bool captured = result.Captures.Where(c => c.Width == requested.Width && c.Status == "Captured").Any(c =>
                {
                    var item = c.Controls.FirstOrDefault(v => v.Path == path);
                    if (item == null || item.X < 0 || item.Y < 0 || item.X + item.Width > c.ActualWidth || item.Y + item.Height > c.ActualHeight) return false;
                    return c.Controls.Where(a => path.StartsWith(a.Path + "/", StringComparison.Ordinal)).All(a => item.X >= a.X && item.Y >= a.Y && item.X + item.Width <= a.X + a.Width && item.Y + item.Height <= a.Y + a.Height);
                });
                if (captured) continue;
                for (Control? parent = combo.Parent; parent != null; parent = parent.Parent)
                    if (parent is ScrollableControl surface && surface.AutoScroll) { surface.ScrollControlIntoView(combo); Pump(); }
                CaptureAtPosition(editor, result, output, requested, variant + "-reveal-" + (combo.Parent is GXValueField field ? field.Name : combo.Name));
            }
            foreach (var surface in scrollables) surface.AutoScrollPosition = Point.Empty;
            Pump();
        }
    }

    static void CaptureAtPosition(Form editor, CaseResult result, string output, Size requested, string variant, bool inspectCombos = true)
    {
        if (result.LayoutAudit && !result.ButtonAudit) CheckInputRails(editor, result, requested.Width + "/" + variant);
        if (result.ComboAudit && inspectCombos)
        {
            // Binding/measurement can trigger native layout and scroll-to-focus.
            // Restore the requested capture position before drawing the viewport.
            var positions = Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(c => c.AutoScroll).ToDictionary(c => c, c => c.AutoScrollPosition);
            CheckVisibleCombos(editor, result, requested.Width + "/" + variant);
            foreach (var position in positions) position.Key.AutoScrollPosition = new Point(-position.Value.X, -position.Value.Y);
            Pump();
        }
        var capture = new CaptureResult { Width = requested.Width, Height = requested.Height, ActualWidth = editor.ClientSize.Width, ActualHeight = editor.ClientSize.Height, Dpi = editor.DeviceDpi, Variant = variant };
        result.Captures.Add(capture);
        try
        {
            string file = requested.Width + "x" + requested.Height + "--" + variant + ".png";
            if (result.ButtonAudit) capture.PrePaintButtons = ButtonAuditMetrics.Measure(editor);
            using (var image = new Bitmap(Math.Max(editor.Width, 1), Math.Max(editor.Height, 1))) { editor.DrawToBitmap(image, new Rectangle(Point.Empty, editor.Size)); image.Save(Path.Combine(output, file)); }
            capture.Path = Path.Combine(output, file);
            if (result.LayoutAudit) capture.LayoutMetrics = LayoutMetrics.Measure(editor);
            if (result.ButtonAudit) capture.Buttons = ButtonAuditMetrics.Measure(editor);
            if (result.ButtonAudit) CheckCaptionAlignment(editor, result, capture);
            if (result.ButtonAudit) CheckShortNameGeneral(editor, result, capture);
            if (result.ButtonAudit) CheckReportedLayouts(editor, result, capture);
            if (result.ButtonAudit) foreach (var check in ButtonAuditMetrics.FooterChecks(editor)) result.BehaviorChecks.Add(new() { Name = "Buttons/" + variant + "/" + check.Name, Passed = check.Passed, Detail = check.Detail });
            foreach (var c in Descendants(editor).Where(c => c.Visible)) capture.Controls.Add(MeasureControl(editor, c));
            foreach (var parent in Descendants(editor).Prepend(editor))
            {
                var controls = parent.Controls.Cast<Control>().Where(c => c.Visible && c.Width > 0 && c.Height > 0).ToArray();
                for (int i = 0; i < controls.Length; i++) for (int j = i + 1; j < controls.Length; j++)
                {
                    var a = controls[i]; var b = controls[j];
                    if (a is TabPage || b is TabPage || a is Splitter || b is Splitter || a.Bounds.Contains(b.Bounds) || b.Bounds.Contains(a.Bounds)) continue;
                    var intersection = Rectangle.Intersect(a.Bounds, b.Bounds);
                    if (intersection.Width > 3 && intersection.Height > 3) capture.Overlaps.Add(new() { First = ControlPath(a), Second = ControlPath(b), Width = intersection.Width, Height = intersection.Height });
                }
                if (parent is ScrollableControl sc && (sc.AutoScroll || sc.HorizontalScroll.Visible || sc.VerticalScroll.Visible)) capture.ScrollSurfaces.Add(new() { Path = ControlPath(parent), Horizontal = sc.HorizontalScroll.Visible, Vertical = sc.VerticalScroll.Visible, ClientWidth = sc.ClientSize.Width, ClientHeight = sc.ClientSize.Height, DisplayWidth = sc.DisplayRectangle.Width, DisplayHeight = sc.DisplayRectangle.Height });
            }
            capture.Status = "Captured";
        }
        catch (Exception ex) { capture.Status = "CaptureError"; capture.Error = ex.ToString(); result.Errors.Add(capture.Error); }
    }

    static ControlResult MeasureControl(Form root, Control c)
    {
        var origin = root.PointToClient(c.PointToScreen(Point.Empty));
        var accessibleName = c.AccessibleName ?? "";
        Control? ancestor = c.Parent;
        while (string.IsNullOrWhiteSpace(accessibleName) && ancestor != null && ancestor != root) { accessibleName = ancestor.AccessibleName ?? ""; ancestor = ancestor.Parent; }
        bool input = c is TextBoxBase || c is ComboBox || c is NumericUpDown || c is DateTimePicker || c is ListView || c is DataGridView || c is CheckedListBox;
        bool action = c is ButtonBase;
        bool labelClipped = false;
        if (c is Label label && label.Text.Length > 0 && !label.AutoEllipsis)
        {
            var measured = TextRenderer.MeasureText(label.Text, label.Font, new Size(Math.Max(1, label.ClientSize.Width), 32767), TextFormatFlags.WordBreak);
            labelClipped = measured.Height > label.ClientSize.Height + 3;
        }
        return new() { Name = c.Name, Path = ControlPath(c), Type = c.GetType().Name, Text = c.Text, X = origin.X, Y = origin.Y, Width = c.Width, Height = c.Height, ParentWidth = c.Parent?.ClientSize.Width ?? 0, ParentHeight = c.Parent?.ClientSize.Height ?? 0, Left = c.Left, Top = c.Top, FontPoints = c.Font.SizeInPoints, Enabled = c.Enabled, TabIndex = c.TabIndex, TabStop = c.TabStop, AccessibleName = c.AccessibleName ?? "", EffectiveAccessibleName = accessibleName, AccessibleRole = c.AccessibleRole.ToString(), AccessibilityCandidate = (input || action) && string.IsNullOrWhiteSpace(accessibleName) && !(action && c.Text.Length > 0), LabelClipCandidate = labelClipped, OutsideParent = c.Left < -1 || c.Top < -1 || c.Right > (c.Parent?.ClientSize.Width ?? int.MaxValue) + 1 || c.Bottom > (c.Parent?.ClientSize.Height ?? int.MaxValue) + 1, FieldType = c is GXValueField f ? f.Type.ToString() : "" };
    }

    static List<Regression> RegressionResults(List<CaseResult> cases)
    {
        var checks = new List<Regression>();
        foreach (var c in cases.Where(c => SimpleViews.Any(v => c.View.EndsWith("." + v))))
        {
            foreach (var capture in c.Captures.Where(x => x.Variant == "default" && x.Status == "Captured"))
            {
                checks.Add(new() { Name = c.View + "/" + c.State + "/" + capture.Width + "/no-form-horizontal-scroll", Passed = !capture.ScrollSurfaces.Any(s => s.Horizontal && !s.Path.Contains("DataGridView")), Detail = string.Join("; ", capture.ScrollSurfaces.Where(s => s.Horizontal).Select(s => s.Path)) });
                var fields = capture.Controls.Where(x => x.Type == "GXValueField" && x.FieldType != "ListBox" && x.FieldType != "CheckedListBox").ToArray();
                if (fields.Length > 0) checks.Add(new() { Name = c.View + "/" + c.State + "/" + capture.Width + "/shared-fields-readable", Passed = fields.All(f => f.Height >= 24 * capture.Dpi / 96 && f.Width >= 100 * capture.Dpi / 96), Detail = string.Join("; ", fields.Where(f => f.Height < 24 * capture.Dpi / 96 || f.Width < 100 * capture.Dpi / 96).Select(f => $"{f.Name}={f.Width}x{f.Height}")) });
            }
            if (c.View.EndsWith(".GXDLMSDemandRegisterView")) checks.Add(new() { Name = "DemandRegister/" + c.State + "/dirty-then-clean-clears-error", Passed = c.DirtyIndicatorAppeared && c.CleanIndicatorCleared, Detail = $"Dirty indicated: {c.DirtyIndicatorAppeared}; clean cleared: {c.CleanIndicatorCleared}" });
            checks.AddRange(c.BehaviorChecks);
            if (c.State == "structured") foreach (var capture in c.Captures.Where(x => x.Status == "Captured"))
            {
                var data = capture.Controls.Where(x => x.Type == "ListView" || x.Type == "DataGridView" || (x.Type == "TextBox" && x.Path.Contains("ValueTB"))).ToArray();
                checks.Add(new() { Name = "Data/structured/" + capture.Width + "/multirow-visible", Passed = data.Any(x => x.Height >= 96 * capture.Dpi / 96 && x.Width >= 180 * capture.Dpi / 96), Detail = data.Length == 0 ? "No visible structured list/grid" : string.Join("; ", data.Select(x => $"{x.Name}={x.Width}x{x.Height}")) });
            }
        }
        checks.AddRange(cases.Where(c => !SimpleViews.Any(v => c.View.EndsWith("." + v))).SelectMany(c => c.BehaviorChecks));
        foreach (var c in cases) foreach (var capture in c.Captures.Where(x => x.Status == "Captured"))
        {
            var collapsed = capture.Controls.Where(x => (x.Type == "TextBox" || x.Type == "ComboBox" || x.Type == "NumericUpDown") && x.Width < 20).ToArray();
            checks.Add(new() { Name = c.View + "/" + c.State + "/" + capture.Width + "/" + capture.Variant + "/native-inputs-not-collapsed", Passed = collapsed.Length == 0, Detail = string.Join("; ", collapsed.Select(x => x.Name + "=" + x.Width + "px")) });
            if (c.View.EndsWith(".GXDLMSProfileGenericView"))
            {
                var tables = capture.Controls.Where(x => x.Name == "CaptureObjectsLv" || x.Name == "ProfileGenericView").ToArray();
                checks.Add(new() { Name = "ProfileGeneric/" + c.State + "/" + capture.Width + "/" + capture.Variant + "/table-multiple-rows-visible", Passed = tables.Length > 0 && tables.All(x => x.Height >= 96 * capture.Dpi / 96), Detail = string.Join("; ", tables.Select(x => x.Name + "=" + x.Width + "x" + x.Height)) });
            }
        }
        foreach (var c in cases.Where(c => c.Captures.Any(x => x.Variant == "restored")))
        {
            var first = c.Captures.First(x => x.Width == 1100 && x.Variant == "default"); var last = c.Captures.First(x => x.Variant == "restored");
            var changed = first.Controls.Where(x => last.Controls.Any(y => x.Path == y.Path && (Math.Abs(x.Width-y.Width)>2 || Math.Abs(x.Height-y.Height)>2 || Math.Abs(x.FontPoints-y.FontPoints)>0.1))).ToArray();
            checks.Add(new() { Name = c.View + "/" + c.Monitor + "/wide-narrow-wide-restores", Passed = changed.Length == 0 && first.Dpi == last.Dpi, Detail = string.Join("; ", changed.Select(x=>x.Path)) });
        }
        return checks;
    }

    static void WriteLedger(string output, InventoryResult inventory, List<CaseResult> cases, List<Regression> regressions)
    {
        var text = new StringBuilder();
        text.AppendLine("Offline WinForms UI coverage ledger").AppendLine("Assembly SHA256: " + inventory.AssemblySha256).AppendLine($"Compiled inventory: {inventory.ObjectViewCount} attributed object views, {inventory.DialogCount} additional dialogs, {inventory.UnattributedViewCount} unattributed views.");
        text.AppendLine($"Executed: {cases.Select(c => c.View).Distinct().Count()} object views; {cases.Count} model cases; {cases.Sum(c => c.Captures.Count(x => x.Status == "Captured"))} captures.");
        text.AppendLine($"Model case failures/timeouts: {cases.Count(c => c.Status != "Passed")}; regression checks: {regressions.Count - regressions.Count(r => !r.Passed)}/{regressions.Count} passed.");
        text.AppendLine("Viewport targets: 1100x760 and 620x560. Blank, representative scalar and selected populated-table fixtures; structured Data additionally. Every tab is selected with its ancestors; not every tab combination. Each scrollable capture includes top/bottom states. Rendered through WinForms DrawToBitmap in offscreen, non-activating windows.");
        text.AppendLine("Scope limits: dialogs are inventoried, not instantiated; network/meter actions, writes, modal workflows and user settings are never invoked. No transport exists. No high-contrast, screen-reader, every-DPI, exhaustive protocol collection, or interactive navigation claim. Supporting dialogs are covered by the companion UiDialogReviewTests suite. Geometry/accessibility flags are review candidates, not automatic defect verdicts. DrawToBitmap has native-control rendering limits.");
        text.AppendLine("\nView | state | status | captured | geometry candidates | label candidates | accessible-name candidates");
        foreach (var c in cases) text.AppendLine($"{c.View.Split('.').Last()} | {c.State} | {c.Status} | {c.Captures.Count(x => x.Status == "Captured")} | {c.Captures.Sum(x => x.Overlaps.Count + x.Controls.Count(y => y.OutsideParent))} | {c.Captures.Sum(x => x.Controls.Count(y => y.LabelClipCandidate))} | {c.Captures.Sum(x => x.Controls.Count(y => y.AccessibilityCandidate))}");
        text.AppendLine("\nFailed regression checks:"); foreach (var check in regressions.Where(r => !r.Passed)) text.AppendLine(check.Name + ": " + check.Detail);
        File.WriteAllText(Path.Combine(output, "coverage-ledger.txt"), text.ToString());
        File.WriteAllText(Path.Combine(output, "coverage.csv"), "View,State,Status,Captures,Errors\n" + string.Join("\n", cases.Select(c => string.Join(",", Csv(c.View), Csv(c.State), Csv(c.Status), c.Captures.Count(x => x.Status == "Captured"), Csv(string.Join(" | ", c.Errors))))));
    }

    static void MakeContactSheets(string output, List<CaseResult> cases)
    {
        var images = cases.Where(c => c.State == "representative").SelectMany(c => c.Captures.Where(x => x.Status == "Captured" && x.Width == 620).Select(x => (Title: c.View.Split('.').Last() + "/" + x.Variant, x.Path))).ToArray();
        for (int page = 0; page * 12 < images.Length; page++)
        {
            using var sheet = new Bitmap(1280, 1070); using var graphics = Graphics.FromImage(sheet); graphics.Clear(Color.FromArgb(232, 237, 242));
            using var font = new Font("Segoe UI", 9); using var brush = new SolidBrush(Color.FromArgb(20, 30, 40));
            for (int cell = 0; cell < 12 && page * 12 + cell < images.Length; cell++)
            {
                var item = images[page * 12 + cell]; int x = cell % 4 * 320; int y = cell / 4 * 356;
                graphics.DrawString(item.Title.Replace("GXDLMS", ""), font, brush, new RectangleF(x + 6, y + 5, 310, 34));
                using var source = Image.FromFile(item.Path); graphics.DrawImage(source, new Rectangle(x + 5, y + 38, 310, 280));
            }
            sheet.Save(Path.Combine(output, $"contact-sheet-{page + 1:00}.png"));
        }
    }

    static IEnumerable<Control> Descendants(Control control) { foreach (Control child in control.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
    static string ControlPath(Control c) => (c.Parent == null ? "" : ControlPath(c.Parent) + "/") + (string.IsNullOrWhiteSpace(c.Name) ? c.GetType().Name + "[" + (c.Parent?.Controls.GetChildIndex(c) ?? 0) + "]" : c.Name);
    static void Pump() { for (int i = 0; i < 3; i++) { Application.DoEvents(); Thread.Sleep(10); } }
    static string Safe(string name) => string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_'));
    static string Csv(string text) => "\"" + text.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
    static void Write<T>(string path, T value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json));
    sealed class AuditShell : Form { protected override bool ShowWithoutActivation => true; }

    sealed class InventoryResult { public string Assembly { get; set; } = ""; public string AssemblySha256 { get; set; } = ""; public string AssemblyVersion { get; set; } = ""; public DateTime CreatedUtc { get; set; } public int ObjectViewCount { get; set; } public int DialogCount { get; set; } public int UnattributedViewCount { get; set; } public List<ScreenEntry> Screens { get; set; } = []; }
    sealed class ScreenEntry { public string[] ButtonFields { get; set; } = []; public string Type { get; set; } = ""; public string ShortName { get; set; } = ""; public string Kind { get; set; } = ""; public string ModelType { get; set; } = ""; public byte[] Versions { get; set; } = []; public string[] LogicalNames { get; set; } = []; public string[] Standards { get; set; } = []; public string[] Constructors { get; set; } = []; public string Coverage { get; set; } = ""; }
    sealed class ComboEntry { public string Path { get; set; } = ""; public string Name { get; set; } = ""; public string NativeName { get; set; } = ""; public string Kind { get; set; } = ""; public int AttributeIndex { get; set; } public string HostType { get; set; } = ""; public int NativeInstanceId { get; set; } public bool VisibleObserved { get; set; } public bool BehaviorChecked { get; set; } public string BehaviorCoverage { get; set; } = ""; public string[] Items { get; set; } = []; public string[] ItemTypes { get; set; } = []; public List<string> Variants { get; set; } = []; }
    sealed class CaseResult { public bool ButtonAudit { get; set; } public bool LayoutAudit { get; set; } public bool ComboAudit { get; set; } public List<ComboEntry> Combos { get; set; } = []; public string View { get; set; } = ""; public string State { get; set; } = ""; public string Status { get; set; } = "Failed"; public string AssemblySha256 { get; set; } = ""; public string Monitor { get; set; } = ""; public bool BindingSucceeded { get; set; } public bool DirtyIndicatorAppeared { get; set; } public bool CleanIndicatorCleared { get; set; } public List<string> FixtureNotes { get; set; } = []; public List<string> Errors { get; set; } = []; public List<CaptureResult> Captures { get; set; } = []; public List<Regression> BehaviorChecks { get; set; } = []; }
    sealed class CaptureResult { public List<ButtonAuditMetric> PrePaintButtons { get; set; } = []; public List<ButtonAuditMetric> Buttons { get; set; } = []; public List<LayoutMetric> LayoutMetrics { get; set; } = []; public int Width { get; set; } public int Height { get; set; } public int ActualWidth { get; set; } public int ActualHeight { get; set; } public int Dpi { get; set; } public string Variant { get; set; } = "default"; public string Status { get; set; } = ""; public string Error { get; set; } = ""; public string Path { get; set; } = ""; public List<ControlResult> Controls { get; set; } = []; public List<Overlap> Overlaps { get; set; } = []; public List<ScrollSurface> ScrollSurfaces { get; set; } = []; }
    sealed class ControlResult { public string Name { get; set; } = ""; public string Path { get; set; } = ""; public string Type { get; set; } = ""; public string Text { get; set; } = ""; public int X { get; set; } public int Y { get; set; } public int Width { get; set; } public int Height { get; set; } public int ParentWidth { get; set; } public int ParentHeight { get; set; } public int Left { get; set; } public int Top { get; set; } public float FontPoints { get; set; } public bool Enabled { get; set; } public int TabIndex { get; set; } public bool TabStop { get; set; } public string AccessibleName { get; set; } = ""; public string EffectiveAccessibleName { get; set; } = ""; public string AccessibleRole { get; set; } = ""; public bool AccessibilityCandidate { get; set; } public bool LabelClipCandidate { get; set; } public bool OutsideParent { get; set; } public string FieldType { get; set; } = ""; }
    sealed class Overlap { public string First { get; set; } = ""; public string Second { get; set; } = ""; public int Width { get; set; } public int Height { get; set; } }
    sealed class ScrollSurface { public string Path { get; set; } = ""; public bool Horizontal { get; set; } public bool Vertical { get; set; } public int ClientWidth { get; set; } public int ClientHeight { get; set; } public int DisplayWidth { get; set; } public int DisplayHeight { get; set; } }
    sealed class Regression { public string Name { get; set; } = ""; public bool Passed { get; set; } public string Detail { get; set; } = ""; }
}
