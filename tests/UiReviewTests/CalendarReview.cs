// superintelligentmeter contributors, 2026-10-30. Focused offline Calendar regressions. GPL-2.0-only.
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    static int CalendarReview(string output, InventoryResult inventory, string[] args)
    {
        int timeoutIndex = Array.IndexOf(args, "--timeout");
        int timeout = timeoutIndex < 0 ? 90 : int.Parse(args[timeoutIndex + 1]);
        var cases = new List<CaseResult>();
        for (int monitor = 0; monitor < Screen.AllScreens.Length; monitor++)
        foreach (string state in new[] { "empty", "populated", "long-names" })
        {
            string folder = Path.Combine(output, "cases", state + "--monitor" + monitor); Directory.CreateDirectory(folder);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = folder };
            if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            foreach (string value in new[] { "calendar-worker", state, folder, monitor.ToString() }) start.ArgumentList.Add(value);
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            bool done = process.WaitForExit(timeout * 1000);
            if (!done) { process.Kill(true); process.WaitForExit(5000); }
            File.WriteAllText(Path.Combine(folder, "worker.log"), stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
            CaseResult result;
            try { result = JsonSerializer.Deserialize<CaseResult>(File.ReadAllText(Path.Combine(folder, "result.json")))!; }
            catch { result = new() { View = "Gurux.DLMS.UI.GXDLMSActivityCalendarView", State = state }; }
            if (!done) { result.Status = "Timeout"; result.Errors.Add("Calendar worker exceeded timeout."); }
            else if (process.ExitCode != 0 && result.Status == "Passed") result.Status = "WorkerError";
            cases.Add(result); Write(Path.Combine(output, "results.json"), cases);
            Console.WriteLine($"Calendar/{state}/monitor{monitor}: {result.Status}; {result.Captures.Count} captures; {result.BehaviorChecks.Count(x => !x.Passed)} failed checks.");
        }
        var checks = cases.SelectMany(c => c.BehaviorChecks).ToList(); Write(Path.Combine(output, "regressions.json"), checks);
        Write(Path.Combine(output, "calendar-summary.json"), new { inventory.AssemblySha256, Cases = cases.Count, Captures = cases.Sum(c => c.Captures.Count), Checks = checks.Count, FailedChecks = checks.Count(c => !c.Passed), FailedCases = cases.Count(c => c.Status != "Passed"), ActualDpi = cases.SelectMany(c => c.Captures).Select(c => c.Dpi).Distinct().Order(), Monitors = cases.Select(c => c.Monitor).Distinct() });
        return cases.Any(c => c.Status != "Passed") || checks.Any(c => !c.Passed) ? 1 : 0;
    }

    static void PopulateCalendar(GXDLMSActivityCalendar model, string state)
    {
        bool empty = state == "empty", longName = state == "long-names";
        string Name(string prefix) => longName ? prefix + " north distribution building commissioning calendar 2026 retained configuration" : prefix;
        model.CalendarNameActive = empty ? "" : Name("Active"); model.CalendarNamePassive = empty ? "" : Name("Passive");
        model.Time = new GXDateTime(SampleTime);
        GXDLMSDayProfile[] Days(int offset) => empty ? [] : Enumerable.Range(1, 2).Select(n => new GXDLMSDayProfile { DayId = (byte)(offset + n), DaySchedules = Enumerable.Range(1, 2).Select(a => new GXDLMSDayProfileAction { StartTime = new GXTime(SampleTime.AddHours(n + a)), ScriptLogicalName = "0.0.10.0.0.255", ScriptSelector = (ushort)(offset + n * 10 + a) }).ToArray() }).ToArray();
        GXDLMSWeekProfile[] Weeks(string side, int offset) => empty ? [] : Enumerable.Range(1, 2).Select(n => new GXDLMSWeekProfile { Name = Encoding.ASCII.GetBytes(Name(side + " week " + n)), Monday = (byte)(offset + 1), Tuesday = (byte)(offset + 1), Wednesday = (byte)(offset + 1), Thursday = (byte)(offset + 2), Friday = (byte)(offset + 2), Saturday = (byte)(offset + 2), Sunday = (byte)(offset + 1) }).ToArray();
        GXDLMSSeasonProfile[] Seasons(string side, GXDLMSWeekProfile[] weeks) => empty ? [] : Enumerable.Range(0, 2).Select(n => new GXDLMSSeasonProfile { Name = Encoding.ASCII.GetBytes(Name(side + " season " + n)), Start = new GXDateTime(SampleTime.AddMonths(n)), WeekName = weeks[n].Name.ToArray() }).ToArray();
        model.DayProfileTableActive = Days(0); model.DayProfileTablePassive = Days(10);
        model.WeekProfileTableActive = Weeks("Active", 0); model.WeekProfileTablePassive = Weeks("Passive", 10);
        model.SeasonProfileActive = Seasons("Active", model.WeekProfileTableActive); model.SeasonProfilePassive = Seasons("Passive", model.WeekProfileTablePassive);
        for (int index = 1; index <= 10; index++) { model.SetAccess(index, AccessMode.ReadWrite); model.SetLastReadTime(index, SampleTime); model.ClearDirty(index); }
        model.SetMethodAccess(1, MethodAccessMode.Access);
    }

    static int CalendarWorker(string state, string output, int monitor)
    {
        var result = new CaseResult { View = "Gurux.DLMS.UI.GXDLMSActivityCalendarView", State = state, LayoutAudit = true, Status = "Started", AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Ui.Location))) };
        void Save() => Write(Path.Combine(output, "result.json"), result);
        void Check(string name, bool passed, string detail = "") => result.BehaviorChecks.Add(new() { Name = state + "/" + name, Passed = passed, Detail = detail });
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); ApplicationTypography.Install(); Save();
        try
        {
            Type type = Ui.GetType(result.View, true)!; var attr = type.GetCustomAttribute<GXDLMSViewAttribute>()!;
            var model = (GXDLMSActivityCalendar)CreateModel(attr, type.Name, "blank", result.FixtureNotes); PopulateCalendar(model, state);
            var area = Screen.AllScreens[monitor].WorkingArea;
            using var shell = new AuditShell { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new(area.X + 24, area.Y + 24), Opacity = 0, ClientSize = new(1100, 760), AutoValidate = AutoValidate.Disable, Font = ApplicationTypography.BodyFont };
            shell.Show(); for (int i = 0; i < 6; i++) Pump();
            using var editor = (Form)Activator.CreateInstance(type)!;
            editor.TopLevel = false; editor.FormBorderStyle = FormBorderStyle.None; editor.Dock = DockStyle.Fill; editor.AutoValidate = AutoValidate.Disable;
            Control Find(string name) => Descendants(editor).First(c => c.Name == name);
            // Capture identity before presentation reparents or styles any control.
            var originals = new[] { "LogicalNameTB", "ACalendarNameTB", "PCalendarNameTB", "ActivatePassiveCalendarTb", "ActivatePassiveCalendarBtn" }.ToDictionary(n => n, Find);
            var view = (IGXDLMSView)editor; view.Target = model; shell.Controls.Add(editor); editor.Show(); for (int i = 0; i < 6; i++) Pump();
            ModernPresentation.Apply(editor); var client = new GXDLMSClient(); int meterActions = 0;
            GXDlmsUi.Init(view, (_, _) => { meterActions++; throw new InvalidOperationException("Calendar audit cannot invoke meter actions."); });
            void Bind(bool connected = true) { GXDlmsUi.ObjectChanged(view, client, model, connected); shell.PerformLayout(); editor.PerformLayout(); for (int i = 0; i < 4; i++) Pump(); }
            void Select(bool passive, int index) { ((TabControl)Find("tabControl1")).SelectedIndex = passive ? 1 : 0; ((TabControl)Find(passive ? "tabControl3" : "tabControl2")).SelectedIndex = index; Pump(); }
            Bind(); result.BindingSucceeded = true; result.Monitor = Screen.FromControl(shell).DeviceName;
            Check("body-font-inherited", editor.Font.Equals(shell.Font), $"Editor={editor.Font}; shell={shell.Font}");
            var normalGeometry = new Dictionary<string, Dictionary<string, Rectangle>>();
            result.FixtureNotes.Add("Distinct active/passive season/week/day objects; two days and two actions per day. Long ASCII names exercise horizontal text/list navigation. No dialogs, meter commands or user settings are invoked.");
            foreach (var step in new[] { (Name: "normal", Size: new Size(1100, 760)), (Name: "narrow", Size: new Size(620, 560)), (Name: "restored", Size: new Size(1100, 760)) })
            {
                shell.ClientSize = step.Size; Pump(); shell.ClientSize = step.Size; editor.Bounds = shell.ClientRectangle; shell.PerformLayout(); editor.PerformLayout(); for (int i = 0; i < 4; i++) Pump();
                foreach (bool passive in new[] { false, true }) foreach (int index in Enumerable.Range(0, 3))
                {
                    Select(passive, index); string variant = step.Name + "-" + (passive ? "passive" : "active") + "-" + new[] { "season", "week", "day" }[index];
                    string nameField = passive ? "PCalendarNameTB" : "ACalendarNameTB";
                    var root = (TableLayoutPanel)Find("groupBox1");
                    Check(variant + "/table-row-allocation", Math.Abs(root.GetRowHeights().Sum() - (root.ClientSize.Height - root.Padding.Vertical)) <= 2, $"Actual rows={string.Join(',', root.GetRowHeights())}; client={root.ClientSize.Height}; styles={string.Join(',', root.RowStyles.Cast<RowStyle>().Select(r => r.SizeType + ":" + r.Height))}");
                    var reference = (GXValueField)Find("LogicalNameTB");
                    foreach (string fieldName in new[] { "LogicalNameTB", "ActivatePassiveCalendarTb", nameField })
                    {
                        var field = (GXValueField)Find(fieldName); var native = Descendants(field).OfType<TextBox>().Single(c => c.Visible); double scale = field.DeviceDpi / 96d;
                        Check(variant + "/" + fieldName + "/padded-field-envelope", field.Height + 2 >= 32 * scale, $"Field={field.Height}px; font={field.Font.Height}px; padding={field.Padding}");
                        Check(variant + "/" + fieldName + "/native-text-viewport", native.ClientSize.Height + 2 >= native.Font.Height && native.Width > 40 * scale && native.Left >= 0 && native.Top >= 0 && native.Right <= field.ClientSize.Width + 2 && native.Bottom <= field.ClientSize.Height + 2, $"Native={native.Bounds}; parent={field.ClientSize}");
                        Check(variant + "/" + fieldName + "/retained-binding", ReferenceEquals(field, originals[fieldName]) && ReferenceEquals(field.Target, model));
                        Check(variant + "/" + fieldName + "/shared-field-style", Math.Abs(field.Height - reference.Height) <= 2 && Math.Abs(field.PointToScreen(Point.Empty).X - reference.PointToScreen(Point.Empty).X) <= 2 && field.Font.Equals(reference.Font) && field.BackColor == reference.BackColor && field.ForeColor == reference.ForeColor, $"Field={field.Size}, left={field.PointToScreen(Point.Empty).X}; reference={reference.Size}, left={reference.PointToScreen(Point.Empty).X}");
                    }
                    var text = Descendants(Find(nameField)).OfType<TextBox>().Single(c => c.Visible);
                    Check(variant + "/calendar-name-value", text.Text == (passive ? model.CalendarNamePassive : model.CalendarNameActive), text.Text);
                    string pageKey = (passive ? "passive" : "active") + index;
                    var geometry = Descendants(editor).Where(c => c.Visible && c.Name.Length != 0).ToDictionary(ControlPath, c => new Rectangle(editor.PointToClient(c.PointToScreen(Point.Empty)), c.Size));
                    if (step.Name == "normal") normalGeometry[pageKey] = geometry;
                    if (step.Name == "restored") Check(variant + "/restored-control-geometry", normalGeometry[pageKey].All(pair => geometry.TryGetValue(pair.Key, out var actual) && Math.Abs(actual.X - pair.Value.X) <= 2 && Math.Abs(actual.Y - pair.Value.Y) <= 2 && Math.Abs(actual.Width - pair.Value.Width) <= 2 && Math.Abs(actual.Height - pair.Value.Height) <= 2), "Named visible controls return within 2px of their original bounds.");
                    Capture(editor, result, output, step.Size, variant); Save();
                }
            }
            // Real binding refresh must clear and refill each list without sharing state.
            foreach (string refresh in new[] { "empty", "populated", "empty", state })
            {
                PopulateCalendar(model, refresh); Bind(); int count = refresh == "empty" ? 0 : 2;
                foreach (string list in new[] { "SeasonProfileActiveLV", "SeasonProfilePassiveLV", "AWeekProfileLV", "PWeekProfileLV", "ActiveDaysList", "PassiveDaysList" })
                    Check("refresh-" + refresh + "/" + list, ((ListView)Find(list)).Items.Count == count);
            }
            PopulateCalendar(model, "populated"); Bind();
            foreach (bool passive in new[] { false, true })
            {
                Select(passive, 2); var days = (ListView)Find(passive ? "PassiveDaysList" : "ActiveDaysList"); var actions = (ListView)Find(passive ? "PassiveActionList" : "ActiveActionList");
                foreach (ListViewItem item in days.Items) item.Selected = false; days.Items[1].Selected = true; Pump();
                var selected = (passive ? model.DayProfileTablePassive : model.DayProfileTableActive)[1];
                Check((passive ? "passive" : "active") + "/day-selection-binds-actions", actions.Items.Count == 2 && actions.Items.Cast<ListViewItem>().Select(i => i.Tag).SequenceEqual(selected.DaySchedules.Cast<object>()));
            }
            (int Index, bool Passive, int Tab, string List, string Button)[] groups = [(3, false, 0, "SeasonProfileActiveLV", "SeasonProfileActiveRemoveBtn"), (7, true, 0, "SeasonProfilePassiveLV", "SeasonProfilePassiveRemoveBtn"), (4, false, 1, "AWeekProfileLV", "WeekProfileActiveRemoveBtn"), (8, true, 1, "PWeekProfileLV", "WeekProfilePassiveRemoveBtn"), (5, false, 2, "ActiveDaysList", "DayProfileActiveRemoveBtn"), (9, true, 2, "PassiveDaysList", "DayProfilePassiveRemoveBtn")];
            foreach (var group in groups)
            {
                PopulateCalendar(model, "populated"); Bind(); Select(group.Passive, group.Tab);
                var list = (ListView)Find(group.List); var button = (Button)Find(group.Button);
                model.SetAccess(group.Index, AccessMode.Read); Bind(); Check(group.Button + "/readonly-disabled", !button.Enabled);
                model.SetAccess(group.Index, AccessMode.ReadWrite); Bind(false); Check(group.Button + "/disconnected-disabled", !button.Enabled);
                Bind(); Check(group.Button + "/writable-enabled", button.Enabled);
                foreach (ListViewItem item in list.Items) item.Selected = false; list.Items[0].Selected = true; Pump();
                button.PerformClick(); Pump();
                int count = group.Index switch { 3 => model.SeasonProfileActive.Length, 7 => model.SeasonProfilePassive.Length, 4 => model.WeekProfileTableActive.Length, 8 => model.WeekProfileTablePassive.Length, 5 => model.DayProfileTableActive.Length, _ => model.DayProfileTablePassive.Length };
                int opposite = group.Index switch { 3 => model.SeasonProfilePassive.Length, 7 => model.SeasonProfileActive.Length, 4 => model.WeekProfileTablePassive.Length, 8 => model.WeekProfileTableActive.Length, 5 => model.DayProfileTablePassive.Length, _ => model.DayProfileTableActive.Length };
                Check(group.Button + "/local-remove-correct-model", count == 1 && opposite == 2 && list.Items.Count == 1, $"Target={count}; opposite={opposite}; list={list.Items.Count}");
                Check(group.Button + "/dirty-then-clean", model.GetDirty(group.Index, out _) && !string.IsNullOrEmpty(view.ErrorProvider.GetError(list)));
                model.ClearDirty(group.Index); view.OnDirtyChange(group.Index, false); Check(group.Button + "/clean-indicator", string.IsNullOrEmpty(view.ErrorProvider.GetError(list)));
                Save();
            }
            var activate = (GXButton)Find("ActivatePassiveCalendarBtn");
            model.SetMethodAccess(1, MethodAccessMode.NoAccess); Bind(); Check("activate-no-access-disabled", !activate.Enabled);
            model.SetMethodAccess(1, MethodAccessMode.Access); Bind(false); Check("activate-disconnected-disabled", !activate.Enabled);
            Bind(); Check("activate-writable-enabled", activate.Enabled);
            Check("no-meter-actions", meterActions == 0); Check("activate-method-binding", activate.Index == 1 && ReferenceEquals(activate, originals["ActivatePassiveCalendarBtn"]));
            result.Status = "Passed"; shell.Hide();
        }
        catch (Exception ex) { result.Status = "Failed"; result.Errors.Add(ex.ToString()); }
        Save(); return result.Status == "Passed" ? 0 : 1;
    }
}
