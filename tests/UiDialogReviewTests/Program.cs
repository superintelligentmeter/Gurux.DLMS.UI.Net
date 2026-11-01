// superintelligentmeter contributors, 2026-10-29. Offline supporting-dialog review. GPL-2.0-only.
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Objects.Enums;
using Gurux.DLMS.UI;

internal static class Program
{
    static readonly Assembly Ui = typeof(ModernPresentation).Assembly;
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    static SettingsWriteGuard settingsGuard;
    static readonly DateTime SampleTime = new(2026, 10, 29, 14, 30, 0);
    static IEnumerable<Control> Descendants(Control c) { foreach (Control child in c.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
    static void Pump() { for (int i = 0; i < 5; ++i) { Application.DoEvents(); Thread.Sleep(15); } }
    static void Set(object o, string name, object value) => o.GetType().GetProperty(name)?.SetValue(o, value);
    static object Model(string name) => Activator.CreateInstance(typeof(GXDLMSObject).Assembly.GetType("Gurux.DLMS.Objects." + name, true)!);
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length < 2) return 64;
        if (args[0] == "buttons") return ButtonAudit(Path.GetFullPath(args[1]), args.Length > 2 ? args[2] : "");
        if (args[0] == "schedule-entry-modes") return ScheduleEntryModes(Path.GetFullPath(args[1]));
        if (args[0] == "worker") return Worker(args[1], args[2], args.Length > 3 && args[3] is "monitors" or "combos", args.Length > 3 && args[3] == "combos", args.Length > 3 && args[3] is "layout" or "buttons", args.Length > 4 ? int.Parse(args[4]) : -1, args.Length > 3 && args[3] == "single-date", args.Length > 3 && args[3] == "parameter-target-add", args.Length > 3 && args[3] == "buttons");
        string output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "ui-assembly.sha256"), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Ui.Location))) + "  " + Ui.Location);
        var results = new List<DialogResultRecord>();
        foreach (Type type in Ui.GetTypes().Where(t => !t.IsAbstract && typeof(Form).IsAssignableFrom(t) && !typeof(IGXDLMSView).IsAssignableFrom(t)).OrderBy(t => t.Name))
        {
            if (args.Length > 2 && !type.Name.Contains(args[2], StringComparison.OrdinalIgnoreCase)) continue;
            if (args[0] == "date-time-single" && type.Name != "GXDateTimeDlg") continue;
            if (args[0] == "parameter-target-add" && type.Name != "GXDLMSTargetObjectDlg") continue;
            string folder = Path.Combine(output, type.Name); Directory.CreateDirectory(folder);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(Environment.ProcessPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            foreach (string a in new[] { "worker", type.FullName!, folder }) start.ArgumentList.Add(a);
            if (args[0] is "monitors" or "combos") start.ArgumentList.Add(args[0]);
            if (args[0] == "date-time-single") start.ArgumentList.Add("single-date");
            if (args[0] == "parameter-target-add") start.ArgumentList.Add("parameter-target-add");
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            bool completed = process.WaitForExit(30000);
            if (!completed) { process.Kill(true); process.WaitForExit(3000); }
            File.WriteAllText(Path.Combine(folder, "worker.log"), stdout.Result + stderr.Result);
            var path = Path.Combine(folder, "result.json");
            var result = File.Exists(path) ? JsonSerializer.Deserialize<DialogResultRecord>(File.ReadAllText(path))! : new() { Type = type.FullName! };
            if (!completed) result.Error = "Worker timed out";
            results.Add(result);
            File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results, Json));
            Console.WriteLine(type.Name + ": " + (result.Error.Length == 0 ? result.Captures.Count + " captures" : result.Error.Split('\n')[0]));
        }
        ContactSheets(output, results);
        File.WriteAllText(Path.Combine(output, "coverage.txt"), string.Join(Environment.NewLine, results.Select(r => r.Type + ": " + (r.Error.Length == 0 ? "Rendered " + r.Captures.Count + " states; actions=" + r.Accept + "/" + r.Cancel : r.Error))) + Environment.NewLine + "Offline fixtures only. No meter, network, certificate request, real key store or user preferences are modified. Geometry warnings are review candidates. Native bitmap limitations apply; no claim of screen-reader or all protocol workflow coverage.");
        return results.Any(r => r.Error.Length != 0) ? 1 : 0;
    }
    // Isolated offline workers: preserve fixed-dialog supported sizes and audit every tab.
    static int ButtonAudit(string output, string filter)
    {
        Directory.CreateDirectory(output);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        var results = new List<DialogResultRecord>();
        foreach (Type type in Ui.GetTypes().Where(t => !t.IsAbstract && typeof(Form).IsAssignableFrom(t) && !typeof(IGXDLMSView).IsAssignableFrom(t)).OrderBy(t => t.Name))
        {
            if (filter.Length != 0 && !type.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
            for (int monitor = 0; monitor < Screen.AllScreens.Length; monitor++)
            {
                string folder = Path.Combine(output, type.Name + "--monitor" + monitor); Directory.CreateDirectory(folder);
                var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                if (Path.GetFileNameWithoutExtension(Environment.ProcessPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
                foreach (string a in new[] { "worker", type.FullName!, folder, "buttons", monitor.ToString() }) start.ArgumentList.Add(a);
                using var process = Process.Start(start)!;
                var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                bool completed = process.WaitForExit(45000);
                if (!completed) { process.Kill(true); process.WaitForExit(3000); }
                File.WriteAllText(Path.Combine(folder, "worker.log"), stdout.Result + stderr.Result);
                string path = Path.Combine(folder, "result.json");
                var result = File.Exists(path) ? JsonSerializer.Deserialize<DialogResultRecord>(File.ReadAllText(path))! : new() { Type = type.FullName!, Error = "Worker produced no result" };
                if (!completed) result.Error = "Worker timed out";
                results.Add(result);
                File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results, Json));
                Console.WriteLine(type.Name + " monitor" + monitor + ": " + (result.Error.Length == 0 ? result.Captures.Count + " captures" : result.Error.Split('\n')[0]));
            }
        }
        File.WriteAllText(Path.Combine(output, "coverage.txt"), "Button source/geometry audit; every compiled supporting Form, both actual monitors, all tabs and existing safe conditional fixtures. Fixed dialogs retain their supported dimensions. Ordinary buttons/GXButtons are measured, including hidden controls; toolbars and icon-only commands are source-classified exceptions. No actions, network, settings saves or user documents. Findings are candidates until reviewed, not automatic conformance claims.");
        return results.Any(r => r.Error.Length != 0) ? 1 : 0;
    }
    // Exact Schedule PreAction dialog constructors; no Settings.Default access or action execution.
    static int ScheduleEntryModes(string output)
    {
        Directory.CreateDirectory(output);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        ApplicationTypography.Install();
        var records = new List<DialogResultRecord>();
        foreach (int mode in new[] { 1, 2, 3 })
        {
            var record = new DialogResultRecord { Type = typeof(GXDLMScheduleDlg).FullName + "/mode" + mode,
                AssemblySha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Ui.Location))) };
            string folder = Path.Combine(output, "mode" + mode);
            Directory.CreateDirectory(folder);
            try
            {
                var script = new GXDLMSScriptTable("0.0.10.0.0.255");
                var objects = new GXDLMSObjectCollection { script };
                var time = new GXTime(6, 0, 0, 0);
                var begin = new GXDate(SampleTime);
                var end = new GXDate(SampleTime.AddYears(1));
                var entry = new GXScheduleEntry { Index = 7, Enable = true, Script = script, ScriptSelector = 2,
                    SwitchTime = time, BeginDate = begin, EndDate = end, ExecWeekdays = Weekdays.Monday | Weekdays.Friday,
                    ValidityWindow = 60, ExecSpecDays = "1" };
                using var form = new GXDLMScheduleDlg(entry, objects, mode);
                ApplicationTypography.Apply(form);
                form.AutoValidate = AutoValidate.Disable;
                form.ShowInTaskbar = false; form.Opacity = 0;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(Screen.PrimaryScreen!.WorkingArea.Left + 20, Screen.PrimaryScreen.WorkingArea.Top + 20);
                SetWindowLong(form.Handle, -20, GetWindowLong(form.Handle, -20) | 0x08000000 | 0x00000080);
                form.Show(); Pump();
                form.ClientSize = new Size(Math.Min(form.ClientSize.Width, 620), Math.Min(form.ClientSize.Height, 560));
                form.PerformLayout(); Pump();
                Control Find(string name) => Descendants(form).Single(c => c.Name == name);
                void Check(bool pass, string text) { if (!pass) throw new InvalidOperationException(text); record.Assertions.Add(text); }
                Check(form.Text == (mode == 1 ? "Enable/Disable schedule entry" : mode == 2 ? "Add schedule entry" : "Remove schedule entry"), "Exact Schedule mode title");
                Check(Find("ScriptTb").Enabled == (mode == 2), "Only Insert allows script selection");
                foreach (string name in new[] { "IndexTB", "SelectorTb", "TimeTb", "ValidityWindowTb", "ExecSpecDaysTb", "BeginTb", "EndTb" })
                    Check(((TextBox)Find(name)).ReadOnly == (mode != 2), name + " preserves mode-specific read-only state");
                Check(Find("EnabledCb").Enabled == (mode != 3), "Enable checkbox follows Enable/Insert/Remove mode");
                foreach (string name in new[] { "MondayCb", "TuesdayCb", "WednesdayCb", "ThursdayCb", "FridayCb", "SaturdayCb", "SundayCb" })
                    Check(Find(name).Enabled == (mode == 2), name + " preserves weekday editability");
                Check(ReferenceEquals(form.AcceptButton, Find("OkBtn")) && ReferenceEquals(form.CancelButton, Find("CancelBtn")), "Original Enter/Escape button identities retained");
                foreach (string name in new[] { "OkBtn", "CancelBtn" })
                {
                    var button = Find(name);
                    Check(form.RectangleToScreen(form.ClientRectangle).Contains(button.RectangleToScreen(button.ClientRectangle)), name + " remains fully visible at compact size");
                }
                Check(entry.Index == 7 && entry.Enable && ReferenceEquals(entry.Script, script) && entry.ScriptSelector == 2 &&
                    ReferenceEquals(entry.SwitchTime, time) && ReferenceEquals(entry.BeginDate, begin) && ReferenceEquals(entry.EndDate, end), "Construct/render does not publish caller entry changes");
                record.Accept = (form.AcceptButton as Control)?.Name ?? "";
                record.Cancel = (form.CancelButton as Control)?.Name ?? "";
                Capture(form, folder, "schedule-mode" + mode + "-compact", record);
            }
            catch (Exception ex) { record.Error = ex.ToString(); }
            records.Add(record);
            File.WriteAllText(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(record, Json));
            File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(records, Json));
        }
        ContactSheets(output, records);
        Console.WriteLine($"Schedule dialogs: {records.Count} modes, {records.Sum(r => r.Captures.Count)} captures, {records.Sum(r => r.Assertions.Count)} assertions, {records.Count(r => r.Error.Length != 0)} errors");
        return records.Any(r => r.Error.Length != 0) ? 1 : 0;
    }

    static int Worker(string typeName, string folder, bool monitors, bool combos, bool layout = false, int monitor = -1, bool singleDate = false, bool parameterAdd = false, bool buttonAudit = false)
    {
        var record = new DialogResultRecord { Type = typeName, ButtonAudit = buttonAudit };
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); ApplicationTypography.Install();
            using var guard = buttonAudit ? new SettingsWriteGuard(Ui) : null;
            settingsGuard = guard;
            // Date/time fixtures use no settings; leave even the process-local singleton untouched.
            if (!typeName.EndsWith(".GXDateTimeDlg", StringComparison.Ordinal) && !typeName.EndsWith(".GXDLMSTargetObjectDlg", StringComparison.Ordinal))
            {
                // Reset only the process-local settings object; never call Save/Reload.
                Type settings = Ui.GetType("Gurux.DLMS.UI.Properties.Settings")!;
                var defaults = settings.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
                foreach (PropertyInfo p in settings.GetProperties().Where(p => p.CanWrite && p.GetIndexParameters().Length == 0 && p.PropertyType == typeof(string))) p.SetValue(defaults, "");
            }
            Type type = Ui.GetType(typeName, true)!;
            using Form form = CreateDialog(type, folder, singleDate, parameterAdd, layout);
            if (buttonAudit) ButtonAuditMetrics.Remember(form);
            if (layout)
            {
                record.LayoutAudit = true;
                record.AssemblySha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Ui.Location)));
                LayoutDialog(form, folder, record, monitor);
                File.WriteAllText(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(record, Json));
                return 0;
            }
            // Production applies typography at Application.Idle; this bounded manual pump does not.
            ApplicationTypography.Apply(form);
            form.AutoValidate = AutoValidate.Disable; form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual; form.Location = new(-20000, -20000);
            form.Show(); Pump();
            record.ComboSites = Descendants(form).OfType<ComboBox>().Select(c => new ComboSite { Name = c.Name, ParentType = c.Parent?.GetType().Name ?? "", Style = c.DropDownStyle.ToString(), DrawMode = c.DrawMode.ToString(), FlatStyle = c.FlatStyle.ToString() }).ToList();
            record.Accept = (form.AcceptButton as Control)?.Text ?? ""; record.Cancel = (form.CancelButton as Control)?.Text ?? "";
            Control previous = null;
            for (int n = 0; n < 35 && form.SelectNextControl(previous, true, true, true, false); ++n)
            {
                var focused = Descendants(form).FirstOrDefault(c => c.Focused);
                if (focused == null) break;
                record.TabOrder.Add(focused.Name); previous = focused;
            }
            if (singleDate || parameterAdd)
            {
                form.ClientSize = new(Math.Min(form.ClientSize.Width, 620), form.ClientSize.Height); Pump();
                Capture(form, folder, parameterAdd ? "parameter-add-compact" : "single-date-compact", record);
            }
            else
            {
                Capture(form, folder, "default", record);
                foreach (var page in Descendants(form).OfType<TabPage>().ToArray())
                {
                    for (Control current = page; current != null; current = current.Parent) if (current is TabPage tp) ((TabControl)tp.Parent!).SelectedTab = tp;
                    Pump(); Capture(form, folder, "tab-" + page.Name, record);
                }
                if (form.FormBorderStyle is FormBorderStyle.Sizable or FormBorderStyle.SizableToolWindow)
                {
                    Size original = form.ClientSize;
                    form.ClientSize = new(Math.Min(original.Width, 620), Math.Min(original.Height, 560)); Pump(); Capture(form, folder, "compact", record);
                    form.ClientSize = original; Pump(); Capture(form, folder, "restored", record);
                }
            }
            void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); record.Assertions.Add(label); }
            if (singleDate)
            {
                Check(Descendants(form).Single(c => c.Name == "StartTimeTb").Visible, "Action Schedule single-date constructor shows StartTimeTb");
                Check(!Descendants(form).Single(c => c.Name == "EndTimeTb").Visible && !Descendants(form).Single(c => c.Name == "NowEndBtn").Visible, "Single-date constructor hides end-time controls");
            }
            var okButtons = Descendants(form).OfType<Button>().Where(b => b.DialogResult == DialogResult.OK).ToArray();
            var cancelButtons = Descendants(form).OfType<Button>().Where(b => b.DialogResult == DialogResult.Cancel).ToArray();
            if (okButtons.Length == 1 && cancelButtons.Length == 1)
            {
                Check(ReferenceEquals(form.AcceptButton, okButtons[0]), "Enter uses the original OK button");
                Check(ReferenceEquals(form.CancelButton, cancelButtons[0]), "Escape uses the original Cancel button");
            }
            if (parameterAdd)
            {
                var selector = Descendants(form).OfType<ComboBox>().Single(c => c.Name == "TargetCb");
                var index = Descendants(form).OfType<TextBox>().Single(c => c.Name == "IndexTB");
                Check(selector.Enabled && selector.Items.Count > 0 && selector.SelectedIndex == -1, "Parameter Monitor Add starts with an unselected enabled target list");
                Check(index.Visible && index.Enabled && index.Text == "0", "Four-argument Add constructor exposes initial attribute index zero");
                var selected = selector.Items.OfType<GXDLMSRegister>().First(); selector.SelectedItem = selected; index.Text = "2";
                ((Button)form.AcceptButton!).PerformClick();
                Check(ReferenceEquals(type.GetProperty("Target")!.GetValue(form), selected) && Convert.ToInt32(type.GetProperty("Index")!.GetValue(form)) == 2, "Valid local OK commits target and attribute index without invoking monitor PreAction");
            }
            if (type.Name is "GXCipheringSettings" or "GXDeviceCipheringSettings")
            {
                var suite = Descendants(form).OfType<ComboBox>().Single(c => c.Name == "SecuritySuiteCb");
                int count = Descendants(form).Count();
                foreach (int index in new[] { 0, 1, 2, 0 }) { suite.SelectedIndex = index; Pump(); Capture(form, folder, "suite-" + index, record); }
                var signing = Descendants(form).OfType<ComboBox>().FirstOrDefault(c => c.Name == "SigningCb");
                if (signing != null)
                    foreach (var mode in new[] { Signing.None, Signing.GeneralSigning, Signing.StaticUnifiedModel, Signing.None })
                    { signing.SelectedItem = mode; Pump(); Capture(form, folder, "signing-" + mode, record); }
                // 2026-11-01: the device dialog also has a Framing tab; target the key-action tab explicitly.
                var page = Descendants(form).OfType<TabPage>().Single(p => p.Name == "CipheringTab");
                ((TabControl)page.Parent!).SelectedTab = page;
                var modeSelector = signing ?? suite;
                modeSelector.SelectedItem = signing != null ? (object)Signing.GeneralSigning : SecuritySuite.Suite1;
                Pump();
                var last = Descendants(form).OfType<Button>().Last(b => b.Visible && b.Name == "UpdateBtn");
                page.ScrollControlIntoView(last); Pump();
                var buttonBounds = last.RectangleToScreen(last.ClientRectangle);
                Check(page.RectangleToScreen(page.ClientRectangle).Contains(buttonBounds), "Bottom key action is reachable through the native scrollbar");
                Capture(form, folder, "keys-bottom", record);
                modeSelector.SelectedIndex = 0; Pump();
                Check(Descendants(form).Count() == count, "Suite/signing cycles retain all native controls without duplicates");
            }
            if (type.Name == "GXDLMSTargetObjectDlg" && !parameterAdd)
            {
                var model = new GXDLMSRegister("1.0.1.8.0.255");
                using var indexed = (Form)Activator.CreateInstance(type, new object[] { "Read-only target", model, 3, null })!;
                ApplicationTypography.Apply(indexed);
                indexed.ShowInTaskbar = false; indexed.StartPosition = FormStartPosition.Manual; indexed.Location = new(-20000, -20000); indexed.Show(); Pump();
                Check(Convert.ToInt32(type.GetProperty("Index")!.GetValue(indexed)) == 3, "Read-only target retains the supplied attribute index");
                Check(Descendants(indexed).OfType<TextBox>().Single(c => c.Name == "IndexTB").Text == "3", "Read-only target displays the supplied attribute index");
                Capture(indexed, folder, "read-only-index", record);
            }
            if (combos)
            {
                AuditCombos(form, folder, record);
            }
            else if (monitors)
            {
                Size originalSize = form.Size;
                foreach (var screen in Screen.AllScreens.Concat(Screen.AllScreens.Take(1)))
                {
                    form.WindowState = FormWindowState.Normal;
                    form.Location = new Point(screen.WorkingArea.Left + 25, screen.WorkingArea.Top + 25);
                    Pump(); Pump();
                    Capture(form, folder, "monitor-" + form.DeviceDpi, record);
                    form.WindowState = FormWindowState.Minimized; Pump();
                    form.WindowState = FormWindowState.Normal; Pump();
                    Capture(form, folder, "restored-" + form.DeviceDpi, record);
                    Check(form.Visible && form.ClientSize.Width > 0, "Monitor restore " + form.DeviceDpi);
                }
            }
            // Disposal bypasses preference-saving FormClosing handlers.
        }
        catch (Exception ex) { record.Error = (ex is TargetInvocationException ? ex.InnerException ?? ex : ex).ToString(); }
        File.WriteAllText(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(record, Json));
        return record.Error.Length == 0 ? 0 : 1;
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    static extern int GetWindowLong(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    static extern int SetWindowLong(IntPtr window, int index, int value);

    static void LayoutDialog(Form form, string folder, DialogResultRecord record, int monitor)
    {
        var screen = Screen.AllScreens[Math.Max(0, monitor)];
        record.Monitor = screen.DeviceName;
        form.AutoValidate = AutoValidate.Disable; form.ShowInTaskbar = false; form.Opacity = 0;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new(screen.WorkingArea.Left + 20, screen.WorkingArea.Top + 20);
        SetWindowLong(form.Handle, -20, GetWindowLong(form.Handle, -20) | 0x08000000 | 0x00000080);
        form.Show(); Pump(); Pump(); ApplicationTypography.Apply(form); form.PerformLayout(); Pump();
        record.Accept = (form.AcceptButton as Control)?.Name ?? ""; record.Cancel = (form.CancelButton as Control)?.Name ?? "";
        record.Resizable = form.FormBorderStyle is FormBorderStyle.Sizable or FormBorderStyle.SizableToolWindow;
        var tabs = Descendants(form).OfType<TabControl>().ToArray();
        var selected = tabs.ToDictionary(t => t, t => t.SelectedIndex);
        void CapturePosition(string variant)
        {
            var scrolls = Descendants(form).Prepend(form).OfType<ScrollableControl>().Where(s => s.Visible && s.AutoScroll).ToArray();
            foreach (var s in scrolls) s.AutoScrollPosition = Point.Empty;
            form.PerformLayout(); Pump(); Capture(form, folder, variant, record);
            CheckSectionAlignment(form, record, variant);
            if (scrolls.Any(s => s.VerticalScroll.Visible))
            {
                foreach (var s in scrolls.Where(s => s.VerticalScroll.Visible)) s.AutoScrollPosition = new(0, int.MaxValue);
                Pump();
                if (form.GetType().Name == "GXDeviceCipheringSettings")
                {
                    // AutoSize rows can grow when the scrollbar reduces available
                    // width. Reach the final action after that native layout settles.
                    var last = Descendants(form).OfType<Button>().Where(c => c.Visible)
                        .OrderBy(c => c.PointToScreen(Point.Empty).Y + c.Height).Last();
                    foreach (var s in scrolls.Where(s => s.VerticalScroll.Visible)) s.ScrollControlIntoView(last);
                    Pump();
                    var bounds = last.RectangleToScreen(last.ClientRectangle);
                    if (!scrolls.All(s => s.RectangleToScreen(s.ClientRectangle).Contains(bounds)))
                        throw new InvalidOperationException(variant + ": final native action is not scroll reachable: " + last.Name);
                    record.Assertions.Add(variant + ": final native action is fully scroll reachable: " + last.Name);
                }
                Capture(form, folder, variant + "-bottom", record);
                foreach (var s in scrolls) s.AutoScrollPosition = Point.Empty;
            }
        }
        var auditSizes = record.ButtonAudit ? new[] { new Size(1500, 900), new Size(900, 700), new Size(480, 560), new Size(1500, 900) } : new[] { new Size(1500, 900), new Size(620, 560), new Size(480, 560), new Size(1500, 900) };
        foreach (var size in auditSizes.Select((value, index) => (value, index)))
        {
            record.RequestedWidth = size.value.Width;
            if (record.Resizable) { form.ClientSize = size.value; Pump(); form.ClientSize = size.value; form.PerformLayout(); Pump(); }
            if (record.ButtonAudit && size.index == 3) { form.WindowState = FormWindowState.Minimized; Pump(); form.WindowState = FormWindowState.Normal; Pump(); form.PerformLayout(); Pump(); }
            string suffix = size.index == 3 ? "-restored" : "";
            foreach (var initial in selected) initial.Key.SelectedIndex = initial.Value;
            CapturePosition(size.value.Width + "-default" + suffix);
            foreach (var tab in tabs)
            for (int index = 0; index < tab.TabPages.Count; index++)
            {
                foreach (var initial in selected) initial.Key.SelectedIndex = initial.Value;
                for (Control current = tab.Parent; current != null; current = current.Parent)
                    if (current is TabPage page) ((TabControl)page.Parent!).SelectedTab = page;
                tab.SelectedIndex = index; Pump();
                CapturePosition(size.value.Width + "-tab-" + tab.Name + "-" + index + suffix);
            }
            if (form.GetType().Name == "GXImageDlg")
            {
                var signature = Descendants(form).Single(c => c.Name == "SignatureGb");
                CapturePosition(size.value.Width + "-signature-visible" + suffix);
                signature.Visible = false; Pump();
                CapturePosition(size.value.Width + "-signature-hidden" + suffix);
                signature.Visible = true; Pump();
            }
            // Reach the state-dependent property groups without invoking any command.
            // The image fixture uses a disposable secure client, and ciphering choices
            // only select the existing presentation of empty local key collections.
            if (form.GetType().Name is "GXCipheringSettings" or "GXDeviceCipheringSettings")
            {
                var suite = Descendants(form).OfType<ComboBox>().Single(c => c.Name == "SecuritySuiteCb");
                for (Control current = suite.Parent; current != null; current = current.Parent)
                    if (current is TabPage page) ((TabControl)page.Parent!).SelectedTab = page;
                suite.SelectedItem = SecuritySuite.Suite1; Pump();
                CapturePosition(size.value.Width + "-suite1" + suffix);
                var signing = Descendants(form).OfType<ComboBox>().FirstOrDefault(c => c.Name == "SigningCb");
                if (signing != null)
                    foreach (var mode in new[] { Signing.GeneralSigning, Signing.StaticUnifiedModel })
                    {
                        signing.SelectedItem = mode; Pump();
                        CapturePosition(size.value.Width + "-" + mode + suffix);
                    }
                if (signing != null) signing.SelectedItem = Signing.None;
                suite.SelectedItem = SecuritySuite.Suite0; Pump();
            }
        }
        form.Hide();
    }

    static void CheckSectionAlignment(Form form, DialogResultRecord record, string state)
    {
        var all = Descendants(form).ToArray();
        Control Find(string name) => all.Single(c => c.Name == name);
        int Left(Control control) => control.Parent!.PointToScreen(control.Location).X;
        void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(state + ": " + message);
            record.Assertions.Add(state + ": " + message);
        }
        string name = form.GetType().Name;
        if (name == "GXImageDlg" && Find("SignatureGb").Visible)
        {
            var peers = new[] { "IdentificationTb", "FileNameTb", "SigningKeyTb", "SignatureTb" }.Select(Find).ToArray();
            Check(peers.Max(Left) - peers.Min(Left) <= 1, "Image identity/file/signing inputs share one left rail");
            var labels = new[] { "TextLbl", "label1", "SigningKeyLbl", "SignatureLbl" }.Select(Find).ToArray();
            Check(labels.Max(Left) - labels.Min(Left) <= 1, "Image section captions share one left rail");
            Check(Left(Find("SigningKeyTb")) + Find("SigningKeyTb").Width == Left(Find("SignatureTb")) + Find("SignatureTb").Width,
                "Signature field peers share their right edge while preserving adjacent actions");
        }
        if (name is "GXCipheringSettings" or "GXDeviceCipheringSettings")
        {
            if (name == "GXDeviceCipheringSettings")
            {
                var choice = (CheckBox)Find("UsePreEstablishedApplicationAssociations");
                var preferred = choice.GetPreferredSize(Size.Empty);
                Check(choice.ClientSize.Width >= preferred.Width && choice.ClientSize.Height >= preferred.Height,
                    "Pre-established checkbox reserves its complete native caption and glyph");
                Check(((TableLayoutPanel)choice.Parent!).GetColumnSpan(choice) == 3,
                    "Pre-established choice uses a full-width row outside the compact label column");
            }
            var keys = new[] { "ClientSigningKeysCb", "ClientAgreementKeysCb", "ServerSigningKeysCb", "ServerAgreementKeysCb" }.Select(Find).Where(c => c.Visible).ToArray();
            if (keys.Length == 0) return;
            int rail = Left(Find("SecuritySuiteCb"));
            Check(keys.All(c => Math.Abs(Left(c) - rail) <= 1), "Ciphering common/client/server inputs share one left rail");
            var labels = new[] { "label3", "label4", "label6", "label5" }.Select(Find).Where(c => c.Visible).ToArray();
            int labelRail = Left(Find("label1"));
            Check(labels.All(c => Math.Abs(Left(c) - labelRail) <= 1), "Ciphering section captions share one left rail");
            Check(keys.Select(c => Left(c) + c.Width).Distinct().Count() == 1, "Ciphering key selectors share their right edge");
            foreach (var key in keys)
            {
                var table = (TableLayoutPanel)key.Parent!;
                var action = table.GetControlFromPosition(2, table.GetRow(key))!;
                Check(key.Right <= action.Left && action.Right <= table.ClientSize.Width,
                    "Key selector and info action remain separate and inside their row: " + key.Name);
            }
        }
    }

    static Form CreateDialog(Type t, string folder, bool singleDate = false, bool parameterAdd = false, bool alignmentStates = false)
    {
        var objects = new GXDLMSObjectCollection();
        var data = new GXDLMSData("0.0.96.1.0.255") { Description = "Laboratory meter identification", Value = "SM-2026-001" }; data.SetDataType(2, DataType.String);
        var register = new GXDLMSRegister("1.0.1.8.0.255") { Description = "Total import energy", Value = 12846.72 };
        var script = new GXDLMSScriptTable("0.0.10.0.0.255");
        var monitor = new GXDLMSRegisterMonitor { Thresholds = new object[] { 10, 20, 30 } };
        var function = new GXDLMSFunctionControl();
        objects.Add(data); objects.Add(register); objects.Add(script); objects.Add(monitor); objects.Add(function);
        objects.Add(new GXDLMSCredit("0.0.19.10.0.255")); objects.Add(new GXDLMSCharge("0.0.19.20.0.255"));
        var day = new GXDLMSDayProfile { DayId = 1, DaySchedules = Array.Empty<GXDLMSDayProfileAction>() };
        var week = new GXDLMSWeekProfile { Name = Encoding.ASCII.GetBytes("Weekday"), Monday = 1, Tuesday = 1, Wednesday = 1, Thursday = 1, Friday = 1, Saturday = 1, Sunday = 1 };
        var season = new GXDLMSSeasonProfile { Name = Encoding.ASCII.GetBytes("Summer"), WeekName = week.Name, Start = new GXDateTime(SampleTime) };
        var action = new GXDLMSScriptAction { Type = ScriptActionType.Write, Target = data, Index = 2, ParameterDataType = DataType.String, Parameter = "Sample" };
        var scriptValue = new GXDLMSScript { Id = 1 }; scriptValue.Actions.Add(action);
        string keys = Path.Combine(folder, "keys"), certs = Path.Combine(folder, "certificates"); Directory.CreateDirectory(keys); Directory.CreateDirectory(certs);
        string image = Path.Combine(folder, "sample.bin"); File.WriteAllBytes(image, new byte[128]);
        object[] args = t.Name switch
        {
            "GXCertificateForm" => [null, "https://example.invalid", certs, "Offline certificate store", null],
            "GXCertificateSigningRequestDlg" => [Encoding.ASCII.GetBytes("SIM00001")],
            "GXCertifigateGenerationForm" => [Path.Combine(folder, "request.csr"), "https://example.invalid"],
            "GXCipheringSettings" => [new GXDLMSTranslator(), keys, certs, "", "", "", ""],
            "GXDeviceCipheringSettings" => ["https://example.invalid", "Offline meter", keys, certs],
            // Null systemTitle is essential: non-null requests key/certificate generation in these constructors.
            "GXEcdsaKeysDlg" => ["https://example.invalid", keys, certs, "Offline keys", SecuritySuite.Suite1, null],
            "GXKeyForm" => [null, "https://example.invalid", keys, certs, "Offline keys", SecuritySuite.Suite1, null],
            "GXPrivateKeyForm" or "GXClockPresetTimeDlg" => [],
            "GXAccountCreditChargeDlg" => [new GXCreditChargeConfiguration { CreditReference = "0.0.19.10.0.255", ChargeReference = "0.0.19.20.0.255" }, objects],
            "GXCertificateType" => ["Select certificate type"],
            "GXChargeTableDlg" => [Encoding.ASCII.GetBytes("Day"), (short)15, false],
            "GXDateTimeDlg" => singleDate ? [new GXDateTime(SampleTime)] : [new GXDateTime(SampleTime), new GXDateTime(SampleTime.AddHours(1))],
            "GXDLMSActivityCalendarActionDlg" => [new GXDLMSDayProfileAction { StartTime = new GXTime(6, 0, 0, 0), ScriptLogicalName = script.LogicalName, ScriptSelector = 1 }, objects.GetObjects(ObjectType.ScriptTable)],
            "GXDLMSActivityCalendarDayProfileDlg" => [day],
            "GXDLMSActivityCalendarSeasonProfileDlg" => [season, new[] { week }],
            "GXDLMSActivityCalendarWeekProfileDlg" => [week, new[] { day }],
            "GXDLMSAssociationViewDlg" => [new GXDLMSAssociationLogicalName(), register, true, false],
            "GXDLMScheduleDlg" => [new GXScheduleEntry { Index = 1, Script = script, ScriptSelector = 1, SwitchTime = new GXTime(6, 0, 0, 0), BeginDate = new GXDate(SampleTime), EndDate = new GXDate(SampleTime.AddYears(1)) }, objects, 2],
            "GXDLMSKeyTableDlg" => [new GXKeyValuePair<byte, byte[]>(1, new byte[16])],
            "GXDLMSMBusClientDataDlg" => [new GXMBusClientData()],
            "GXDLMSModemConfigurationDlg" => [new GXDLMSModemInitialisation { Request = "AT", Response = "OK", Delay = 100 }],
            "GXDLMSNeighbourTableDlg" => [new GXDLMSNeighbourTable()],
            "GXDLMSNtpDlg" => [1u, new byte[16], false],
            "GXDLMSProfileGenericColumnDlg" => [new GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject>(register, new GXDLMSCaptureObject(2, 0)), objects, "Profile capture column"],
            "GXDLMSPushTargetDlg" => [new GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject>(register, new GXDLMSCaptureObject(2, 0)), objects],
            "GXDLMSRegisterActivationDlg" => ["Activation mask", Encoding.ASCII.GetBytes("Normal"), new byte[] { 1, 2, 3 }],
            "GXDLMSRoutingTableDlg" => [new GXDLMSRoutingTable()],
            "GXDLMSScriptActionDlg" => [action, objects],
            "GXDLMSScriptDlg" => [scriptValue, objects, false],
            "GXDLMSSpecialDaysTableDlg" => [new GXDLMSSpecialDay { Index = 1, Date = new GXDate(SampleTime), DayId = 1 }, false],
            "GXDLMSTargetObjectDlg" => parameterAdd ? ["Add new target", null, 0, objects] : ["Choose object", register, objects],
            "GXDLMSTokenGatewayConfigurationDlg" => [new GXTokenGatewayConfiguration { CreditReference = "0.0.19.10.0.255", TokenProportion = 100 }, objects],
            "GXFindDlg" => [new GXFindParameters()],
            "GXFunctionControlFunctionDlg" => [function, "Tariff management", 2],
            "GXImageDlg" when alignmentStates => [Gurux.DLMS.ManufacturerSettings.ManufactureSettings.SignImageWithEcdsa,
                new Gurux.DLMS.Secure.GXDLMSSecureClient { Ciphering = { SecuritySuite = SecuritySuite.Suite1 } }],
            "GXImageDlg" => [(Gurux.DLMS.ManufacturerSettings.ManufactureSettings)0, new GXDLMSClient()],
            "GXImageInfoDlg" => [image],
            "GXRegisterMonitorActionsDlg" => [monitor, new GXDLMSActionSet { ActionUp = new GXDLMSActionItem { LogicalName = script.LogicalName, ScriptSelector = 1 }, ActionDown = new GXDLMSActionItem { LogicalName = script.LogicalName, ScriptSelector = 2 } }, false],
            "GXRegisterMonitorThresholdDlg" => [monitor, 0, false],
            "GXSapDlg" => [(ushort)1, "Laboratory meter", false],
            "GXSecuritySetupKeyDlg" => [1],
            "GXTextDlg" => ["Edit value", "Description", "Main incoming meter â€” laboratory reference"],
            "GXUserDlg" => [(byte)1, "Meter operator", false],
            _ => throw new InvalidOperationException("No safe fixture: " + t.Name)
        };
        return (Form)Activator.CreateInstance(t, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null)!;
    }
    // superintelligentmeter contributors, 2026-10-30. Native combo regression evidence.
    [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct ComboInfo
    {
        public int Size; public NativeRect Item, Button; public int ButtonState;
        public IntPtr Combo, Editor, List;
    }
    [DllImport("user32.dll")] static extern bool GetComboBoxInfo(IntPtr handle, ref ComboInfo info);
    static void AuditCombos(Form form, string folder, DialogResultRecord record)
    {
        Size original = form.ClientSize;
        float originalDpi = form.DeviceDpi;
        int visit = 0;
        foreach (var screen in Screen.AllScreens.Concat(Screen.AllScreens.Take(1)))
        {
            form.WindowState = FormWindowState.Normal;
            form.Location = new Point(screen.WorkingArea.Left + 20, screen.WorkingArea.Top + 20);
            Pump(); Pump();
            string monitor = "combo-" + visit++ + "-dpi-" + form.DeviceDpi;
            bool resizable = form.FormBorderStyle is FormBorderStyle.Sizable or FormBorderStyle.SizableToolWindow;
            foreach (string size in new[] { "wide", "narrow" })
            {
                float factor = form.DeviceDpi / 96f;
                form.ClientSize = resizable ? new Size((int)((size == "wide" ? 960 : 620) * factor), (int)((size == "wide" ? 720 : 560) * factor))
                    : new Size((int)(original.Width * form.DeviceDpi / originalDpi), (int)(original.Height * form.DeviceDpi / originalDpi));
                Pump();
                string variant = monitor + "-" + size;
                Capture(form, folder, variant, record);
                foreach (var page in Descendants(form).OfType<TabPage>().ToArray())
                {
                    for (Control current = page; current != null; current = current.Parent)
                        if (current is TabPage tp) ((TabControl)tp.Parent!).SelectedTab = tp;
                    Pump(); Capture(form, folder, variant + "-tab-" + page.Name, record);
                }
                if (form.GetType().Name is "GXCipheringSettings" or "GXDeviceCipheringSettings")
                {
                    var suite = Descendants(form).OfType<ComboBox>().Single(c => c.Name == "SecuritySuiteCb");
                    suite.SelectedIndex = 1; Pump(); Capture(form, folder, variant + "-suite1", record);
                    var signing = Descendants(form).OfType<ComboBox>().FirstOrDefault(c => c.Name == "SigningCb");
                    if (signing != null)
                    {
                        foreach (var mode in new[] { Signing.GeneralSigning, Signing.StaticUnifiedModel })
                        {
                            signing.SelectedItem = mode; Pump();
                            Capture(form, folder, variant + "-" + mode, record);
                            ExerciseComboDropdowns(form, record, variant + "-" + mode);
                        }
                        signing.SelectedItem = Signing.None;
                    }
                    else ExerciseComboDropdowns(form, record, variant + "-suite1");
                    suite.SelectedIndex = 0; Pump();
                }
                ExerciseComboDropdowns(form, record, variant);
            }
            form.WindowState = FormWindowState.Minimized; Pump();
            form.WindowState = FormWindowState.Normal; Pump();
            Capture(form, folder, monitor + "-min-restore", record);
        }
    }
    static void ExerciseComboDropdowns(Form form, DialogResultRecord record, string variant)
    {
        foreach (var combo in Descendants(form).OfType<ComboBox>().Where(c => c.Visible && c.Enabled).ToArray())
        {
            Rectangle visible = combo.RectangleToScreen(combo.ClientRectangle);
            for (Control parent = combo.Parent; parent != null; parent = parent.Parent)
                visible.Intersect(parent.RectangleToScreen(parent.ClientRectangle));
            if (visible.Size != combo.ClientSize || combo.DropDownStyle == ComboBoxStyle.Simple) continue;
            int selected = combo.SelectedIndex; string text = combo.Text;
            var style = combo.DropDownStyle; var mode = combo.DrawMode; var flat = combo.FlatStyle;
            combo.Focus(); combo.DroppedDown = true; Application.DoEvents();
            combo.DroppedDown = false; Application.DoEvents();
            if (combo.SelectedIndex != selected || combo.Text != text || combo.DropDownStyle != style || combo.DrawMode != mode || combo.FlatStyle != flat)
                throw new InvalidOperationException("Opening/closing changed native combo state: " + combo.Name);
            record.Assertions.Add(variant + ": native dropdown preserves selection, text and style: " + combo.Name);
        }
    }
    static ComboMetric MeasureCombo(ComboBox combo)
    {
        var info = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() };
        bool native = combo.IsHandleCreated && GetComboBoxInfo(combo.Handle, ref info);
        var parent = combo.Parent;
        var metric = new ComboMetric { Name = combo.Name, ParentType = parent?.GetType().Name ?? "", Bounds = combo.Bounds.ToString(),
            ParentBounds = parent?.ClientRectangle.ToString() ?? "", Height = combo.Height, PreferredHeight = combo.PreferredHeight,
            Width = combo.Width, Font = combo.Font.Name, FontPoints = combo.Font.SizeInPoints, Dpi = combo.DeviceDpi,
            Style = combo.DropDownStyle.ToString(), DrawMode = combo.DrawMode.ToString(), FlatStyle = combo.FlatStyle.ToString(), Enabled = combo.Enabled,
            NativeItemHeight = native ? info.Item.Bottom - info.Item.Top : 0,
            DecorativeFrame = parent?.GetType().Name.Contains("FieldFrame") == true };
        if (combo.DropDownStyle != ComboBoxStyle.Simple && combo.Height + 2 < combo.PreferredHeight)
            metric.Findings.Add("Native height differs beyond normal 2px DPI rounding");
        if (parent != null && !(parent is ScrollableControl sc && sc.AutoScroll) && !parent.ClientRectangle.Contains(combo.Bounds))
            metric.Findings.Add("Immediate parent clips combo");
        if (native && metric.NativeItemHeight < combo.Font.Height && combo.DropDownStyle != ComboBoxStyle.Simple)
            metric.Findings.Add("Native item rectangle is shorter than font height");
        if (metric.DecorativeFrame) metric.Findings.Add("Native combo inside decorative frame");
        return metric;
    }
    static void Capture(Form form, string folder, string variant, DialogResultRecord record)
    {
        string path = Path.Combine(folder, variant + ".png");
        using var image = new Bitmap(form.Width, form.Height); form.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(path);
        var capture = new CaptureRecord { Path = path, Variant = variant, Dpi = form.DeviceDpi, Width = form.ClientSize.Width, Height = form.ClientSize.Height };
        record.Captures.Add(capture);
        if (record.ButtonAudit)
        {
            capture.SaveAttempts = settingsGuard?.SaveAttempts ?? 0;
            capture.Buttons = ButtonAuditMetrics.Measure(form).Cast<object>().ToList();
            capture.OriginalButtonsRetained = ButtonAuditMetrics.OriginalsRetained(form);
            capture.FooterChecks = ButtonAuditMetrics.FooterChecks(form).Cast<object>().ToList();
            var footerFailure = ButtonAuditMetrics.FooterChecks(form).FirstOrDefault(c => !c.Passed);
            if (footerFailure != null) throw new InvalidOperationException(variant + ": " + footerFailure.Name + " " + footerFailure.Detail);
            if (!capture.OriginalButtonsRetained) throw new InvalidOperationException(variant + ": original button identity or protocol binding changed");
        }
        if (record.LayoutAudit)
        {
            capture.RequestedWidth = record.RequestedWidth;
            capture.LayoutMetrics = LayoutMetrics.Measure(form);
        }
        foreach (Control c in Descendants(form).Where(c => c.Visible && c.Width > 0 && c.Height > 0))
        {
            if (c is Button or TextBox or ComboBox or CheckBox or RadioButton or Label)
            {
                bool scroll = c.Parent is ScrollableControl sc && sc.AutoScroll;
                if (!scroll && !c.Parent!.ClientRectangle.Contains(c.Bounds)) capture.Warnings.Add($"Outside parent: {c.Name} {c.Bounds} parent={c.Parent.Name}/{c.Parent.ClientRectangle}");
                if (c is Button && TextRenderer.MeasureText(c.Text, c.Font).Width > c.ClientSize.Width) capture.Warnings.Add("Button caption: " + c.Name + "=" + c.Text);
            }
        }
        capture.Combos = Descendants(form).OfType<ComboBox>().Where(c => c.Visible).Select(MeasureCombo).ToList();
        var buttons = Descendants(form).OfType<Button>().Where(b => b.Visible).ToArray();
        foreach (Button b in buttons)
        {
            Rectangle visible = b.RectangleToScreen(b.ClientRectangle);
            for (Control p = b.Parent; p != null; p = p.Parent) visible.Intersect(p.RectangleToScreen(p.ClientRectangle));
            if (visible.Size != b.ClientSize) capture.Warnings.Add("Ancestor clips button: " + b.Name);
        }
        for (int a = 0; a < buttons.Length; ++a) for (int b = a + 1; b < buttons.Length; ++b)
            if (buttons[a].RectangleToScreen(buttons[a].ClientRectangle).IntersectsWith(buttons[b].RectangleToScreen(buttons[b].ClientRectangle)))
                capture.Warnings.Add("Buttons overlap: " + buttons[a].Name + "/" + buttons[b].Name);

        if (record.LayoutAudit) File.WriteAllText(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(record, Json));
    }
    static void ContactSheets(string output, List<DialogResultRecord> results)
    {
        var captures = results.SelectMany(r => r.Captures.Select(c => (Name: r.Type.Split('.').Last() + "/" + c.Variant, Capture: c))).ToArray();
        for (int start = 0; start < captures.Length; start += 12)
        {
            var page = captures.Skip(start).Take(12).ToArray();
            using var bitmap = new Bitmap(1200, 4 * 310); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.White);
            using var font = new Font("Segoe UI", 9);
            for (int n = 0; n < page.Length; ++n)
            {
                int x = n % 3 * 400, y = n / 3 * 310; graphics.DrawString(page[n].Name, font, Brushes.Black, new RectangleF(x + 4, y, 390, 30));
                using var source = Image.FromFile(page[n].Capture.Path); double scale = Math.Min(390d / source.Width, 270d / source.Height);
                graphics.DrawImage(source, x + 4, y + 32, (int)(source.Width * scale), (int)(source.Height * scale));
            }
            bitmap.Save(Path.Combine(output, "contact-" + start / 12 + ".png"));
        }
    }
    public sealed class ComboSite { public string Name { get; set; } = ""; public string ParentType { get; set; } = ""; public string Style { get; set; } = ""; public string DrawMode { get; set; } = ""; public string FlatStyle { get; set; } = ""; }
    public sealed class ComboMetric
    {
        public string Name { get; set; } = ""; public string ParentType { get; set; } = ""; public string Bounds { get; set; } = ""; public string ParentBounds { get; set; } = "";
        public int Height { get; set; } public int PreferredHeight { get; set; } public int Width { get; set; } public int NativeItemHeight { get; set; }
        public string Font { get; set; } = ""; public float FontPoints { get; set; } public int Dpi { get; set; }
        public string Style { get; set; } = ""; public string DrawMode { get; set; } = ""; public string FlatStyle { get; set; } = ""; public bool Enabled { get; set; } public bool DecorativeFrame { get; set; }
        public List<string> Findings { get; set; } = [];
    }
    public sealed class DialogResultRecord { public bool ButtonAudit { get; set; } public bool LayoutAudit { get; set; } public string AssemblySha256 { get; set; } = ""; public string Monitor { get; set; } = ""; public bool Resizable { get; set; } public int RequestedWidth { get; set; } public string Type { get; set; } = ""; public string Error { get; set; } = ""; public List<ComboSite> ComboSites { get; set; } = []; public List<string> TabOrder { get; set; } = []; public List<string> Assertions { get; set; } = []; public string Accept { get; set; } = ""; public string Cancel { get; set; } = ""; public List<CaptureRecord> Captures { get; set; } = []; }
    public sealed class CaptureRecord { public List<object> FooterChecks { get; set; } = []; public int SaveAttempts { get; set; } public List<object> Buttons { get; set; } = []; public bool OriginalButtonsRetained { get; set; } public int RequestedWidth { get; set; } public List<LayoutMetric> LayoutMetrics { get; set; } = []; public string Path { get; set; } = ""; public string Variant { get; set; } = ""; public int Dpi { get; set; } public int Width { get; set; } public int Height { get; set; } public List<ComboMetric> Combos { get; set; } = []; public List<string> Warnings { get; set; } = []; }
}
