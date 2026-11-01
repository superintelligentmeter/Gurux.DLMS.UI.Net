// superintelligentmeter contributors, 2026-10-30. Offline Profile numeric/date regressions. GPL-2.0-only.
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;
using System.Security.Cryptography;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    // Install before any Profile settings getter. The original singleton is only
    // retained by identity, never queried; the replacement cannot read/write files.
    sealed class MemoryProfileSettings : IDisposable
    {
        readonly FieldInfo field;
        readonly object original;
        public readonly ApplicationSettingsBase Instance;
        public readonly MemoryProfileProvider Provider = new();
        public bool Restored { get; private set; }
        public MemoryProfileSettings()
        {
            Type type = Ui.GetType("Gurux.DLMS.UI.Properties.Settings", true)!;
            field = type.GetField("defaultInstance", BindingFlags.Static | BindingFlags.NonPublic)!;
            original = field.GetValue(null)!;
            Instance = (ApplicationSettingsBase)Activator.CreateInstance(type, true)!;
            Provider.Initialize("OfflineProfileSettings", new NameValueCollection());
            foreach (SettingsProperty property in Instance.Properties) property.Provider = Provider;
            Instance.Providers.Clear(); Instance.Providers.Add(Provider);
            field.SetValue(null, Instance);
        }
        public void Dispose() { if (!Restored) { field.SetValue(null, original); Restored = ReferenceEquals(field.GetValue(null), original); } }
    }
    sealed class MemoryProfileProvider : SettingsProvider
    {
        public int Reads { get; private set; }
        public int SaveAttempts { get; private set; }
        public override string ApplicationName { get; set; } = "OfflineProfileTests";
        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties)
        {
            Reads++; var values = new SettingsPropertyValueCollection();
            foreach (SettingsProperty property in properties) values.Add(new SettingsPropertyValue(property) { SerializedValue = property.DefaultValue, IsDirty = false });
            return values;
        }
        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection values)
        { SaveAttempts++; throw new InvalidOperationException("Offline UI tests must not save settings."); }
    }

    static int ProfileInputs(string output)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); ApplicationTypography.Install();
        var result = new CaseResult { View = "Gurux.DLMS.UI.GXDLMSProfileGenericView", State = "numeric-and-date", LayoutAudit = true, AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Ui.Location))) };
        void Save() => Write(Path.Combine(output, "result.json"), result);
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = name, Passed = pass, Detail = detail });
        var settings = new MemoryProfileSettings(); var foreground = GetForegroundWindow(); int activations = 0;
        try
        {
            var type = Ui.GetType(result.View, true)!; var attr = type.GetCustomAttribute<GXDLMSViewAttribute>()!;
            var model = (GXDLMSProfileGeneric)CreateModel(attr, type.Name, "representative", result.FixtureNotes);
            var screens = Screen.AllScreens; var firstArea = screens[0].WorkingArea;
            using var shell = new AuditShell { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new(firstArea.X + 24, firstArea.Y + 24), Opacity = 0, ClientSize = new(1100, 760), AutoValidate = AutoValidate.Disable, Font = ApplicationTypography.BodyFont };
            shell.Activated += (_, _) => activations++;
            shell.Show(); for (int i = 0; i < 6; i++) Pump();
            using var editor = (Form)Activator.CreateInstance(type)!;
            editor.TopLevel = false; editor.FormBorderStyle = FormBorderStyle.None; editor.Dock = DockStyle.Fill; editor.AutoValidate = AutoValidate.Disable;
            Control Find(string name) => Descendants(editor).First(c => c.Name == name);
            string[] inputNames = ["StartEntry", "EndEntry", "ReadLastTB", "StartPick", "ToPick"];
            var originals = inputNames.ToDictionary(n => n, Find);
            var view = (IGXDLMSView)editor; view.Target = model; shell.Controls.Add(editor); editor.Show(); for (int i = 0; i < 6; i++) Pump();
            ApplicationTypography.Apply(editor);
            var appearance = originals.ToDictionary(p => p.Key, p => (p.Value.Font.FontFamily.Name, p.Value.Font.SizeInPoints, p.Value.Font.Style, p.Value.ForeColor, p.Value.BackColor, Dpi: p.Value.DeviceDpi));
            var nativeColors = new Dictionary<(string Name, bool ReadOnly), Color>();
            foreach (var number in originals.Values.OfType<NumericUpDown>())
            {
                bool readOnly = number.ReadOnly;
                foreach (bool state in new[] { false, true }) { number.ReadOnly = state; nativeColors[(number.Name, state)] = number.BackColor; }
                number.ReadOnly = readOnly;
            }
            ModernPresentation.Apply(editor); int commands = 0;
            GXDlmsUi.Init(view, (_, _) => { commands++; throw new InvalidOperationException("Profile input audit cannot issue meter commands."); });
            var client = new GXDLMSClient(); GXDlmsUi.ObjectChanged(view, client, model, true); Pump(); result.BindingSucceeded = true;
            var start = (NumericUpDown)Find("StartEntry"); var count = (NumericUpDown)Find("EndEntry"); var last = (NumericUpDown)Find("ReadLastTB");
            var from = (DateTimePicker)Find("StartPick"); var to = (DateTimePicker)Find("ToPick");
            var entryMode = (RadioButton)Find("ReadEntryBtn"); var lastMode = (RadioButton)Find("ReadLastRB"); var dateMode = (RadioButton)Find("ReadFromRB"); var allMode = (RadioButton)Find("ReadAllRB");
            RadioButton[] modes = [entryMode, lastMode, dateMode, allMode];
            var geometry = new Dictionary<string, Rectangle>();
            foreach (int monitor in Enumerable.Range(0, screens.Length))
            {
                var area = screens[monitor].WorkingArea; shell.Location = new(area.X + 24, area.Y + 24); for (int i = 0; i < 6; i++) Pump();
                foreach (var step in new[] { (Name: "normal", Size: new Size(1100, 760)), (Name: "narrow", Size: new Size(620, 560)), (Name: "restored", Size: new Size(1100, 760)) })
                {
                    shell.ClientSize = step.Size; Pump(); shell.ClientSize = step.Size; editor.Bounds = shell.ClientRectangle; shell.PerformLayout(); editor.PerformLayout(); for (int i = 0; i < 5; i++) Pump();
                    var settled = inputNames.ToDictionary(n => n, n => Find(n).Bounds); int sizeChanges = 0;
                    EventHandler resized = (_, _) => sizeChanges++;
                    foreach (string name in inputNames) Find(name).SizeChanged += resized;
                    for (int repeat = 0; repeat < 8; repeat++) { shell.PerformLayout(); editor.PerformLayout(); Find("StartEntry").Parent!.PerformLayout(); Pump(); }
                    foreach (string name in inputNames) Find(name).SizeChanged -= resized;
                    Check("monitor" + monitor + "/" + step.Name + "/settled-layout", sizeChanges == 0 && inputNames.All(n => Find(n).Bounds == settled[n]), "Native input SizeChanged events=" + sizeChanges);
                    foreach (string mode in new[] { "entry", "last", "date", "all" })
                    {
                        string prefix = "monitor" + monitor + "-" + step.Name + "-" + mode;
                        if (mode == "entry") { entryMode.Checked = true; start.Value = 2; count.Value = 20; Pump(); Check(prefix + "/model-range", model.AccessSelector.ToString() == "Entry" && Convert.ToDecimal(model.From) == 2 && Convert.ToDecimal(model.To) == 20); }
                        if (mode == "last") { lastMode.Checked = true; last.Value = 3; Pump(); Check(prefix + "/model-range", model.AccessSelector.ToString() == "Last" && (DateTime)model.From == DateTime.Today.AddDays(-3) && (DateTime)model.To == DateTime.Today.AddDays(1)); }
                        if (mode == "date")
                        {
                            dateMode.Checked = true; from.Value = SampleTime.Date.AddHours(8); to.Value = SampleTime.Date.AddDays(1).AddHours(8); from.Checked = to.Checked = true; Pump();
                            Check(prefix + "/model-range", model.AccessSelector.ToString() == "Range" && (DateTime)model.From == from.Value && (DateTime)model.To == to.Value);
                        }
                        if (mode == "all") { object beforeFrom = model.From, beforeTo = model.To; allMode.Checked = true; Pump(); Check(prefix + "/model-range", model.AccessSelector.ToString() == "All" && Equals(model.From, beforeFrom) && Equals(model.To, beforeTo)); }
                        Check(prefix + "/exclusive-mode", modes.Count(m => m.Checked) == 1);
                        Check(prefix + "/editable-state", start.ReadOnly == !entryMode.Checked && count.ReadOnly == !entryMode.Checked && last.ReadOnly == !lastMode.Checked && from.Enabled == dateMode.Checked && to.Enabled == dateMode.Checked);
                        foreach (string name in inputNames)
                        {
                            var control = Find(name); GetWindowRect(control.Handle, out var native); int expected = control is DateTimePicker picker ? picker.PreferredHeight : (int)Math.Round(control.DeviceDpi * 32d / 96);
                            Check(prefix + "/" + name + "/native-height", Math.Abs(native.Bottom - native.Top - expected) <= 2, $"Native={native.Bottom-native.Top}; managed={control.Height}; expected={expected}");
                            Check(prefix + "/" + name + "/retained-instance", ReferenceEquals(control, originals[name]));
                            Check(prefix + "/" + name + "/real-parented-control", editor.Contains(control) && control.Visible && control.IsHandleCreated);
                            var before = appearance[name]; var expectedBackColor = control is NumericUpDown numeric ? nativeColors[(name, numeric.ReadOnly)] : before.BackColor;
                            Check(prefix + "/" + name + "/font-colors", control.Font.FontFamily.Name == before.Name && Math.Abs(control.Font.SizeInPoints - before.SizeInPoints * control.DeviceDpi / before.Dpi) < .1 && control.Font.Style == before.Style && control.ForeColor == before.ForeColor && control.BackColor == expectedBackColor,
                                $"Font={control.Font.SizeInPoints}; expected={before.SizeInPoints * control.DeviceDpi / before.Dpi}; initialDPI={before.Dpi}; currentDPI={control.DeviceDpi}");
                            Check(prefix + "/" + name + "/native-contained", control.Left >= 0 && control.Top >= 0 && control.Right <= control.Parent!.ClientSize.Width + 2 && control.Bottom <= control.Parent.ClientSize.Height + 2 && control.Width >= control.Font.Height * 2);
                            if (control is DateTimePicker date)
                            {
                                var table = (TableLayoutPanel)date.Parent!; int row = table.GetRow(date); var rowHeights = table.GetRowHeights();
                                double center = rowHeights.Take(row).Sum() + rowHeights[row] / 2d;
                                Check(prefix + "/" + name + "/native-date-centered", Math.Abs(date.Top + date.Height / 2d - center) <= 2);
                                int content = TextRenderer.MeasureText(date.Text, date.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
                                Check(prefix + "/" + name + "/date-text-width", date.Width >= content + (int)Math.Round(date.DeviceDpi * 36d / 96), $"Width={date.Width}; text={content}; native checkbox/calendar allowance=36logical");
                            }
                            var bounds = new Rectangle(editor.PointToClient(control.PointToScreen(Point.Empty)), control.Size); string key = monitor + "/" + mode + "/" + name;
                            if (step.Name == "normal") geometry[key] = bounds;
                            if (step.Name == "restored") { var original = geometry[key]; Check(prefix + "/" + name + "/restored-geometry", Math.Abs(bounds.X-original.X)<=2 && Math.Abs(bounds.Y-original.Y)<=2 && Math.Abs(bounds.Width-original.Width)<=2 && Math.Abs(bounds.Height-original.Height)<=2); }
                        }
                        var reading = (TableLayoutPanel)start.Parent!;
                        Check(prefix + "/entry-peers-aligned", Math.Abs(start.Height-count.Height)<=2 && (reading.GetRow(start)==reading.GetRow(count) ? Math.Abs(start.Top-count.Top)<=2 : Math.Abs(start.Left-count.Left)<=2));
                        Check(prefix + "/date-peers-aligned", Math.Abs(from.Height-to.Height)<=2 && (reading.GetRow(from)==reading.GetRow(to) ? Math.Abs(from.Top-to.Top)<=2 : Math.Abs(from.Left-to.Left)<=2));
                        var tabs = Descendants(editor).OfType<TabControl>().First();
                        for (int tab = 0; tab < tabs.TabPages.Count; tab++) { tabs.SelectedIndex = tab; Pump(); Capture(editor, result, output, step.Size, prefix + "-tab" + tab); Save(); }
                    }
                }
            }
            entryMode.Checked = true; start.Value = 5; count.Value = 20; start.UpButton(); Check("spinner/start-up-model", start.Value == 6 && Convert.ToDecimal(model.From) == 6); start.DownButton(); Check("spinner/start-down-model", start.Value == 5 && Convert.ToDecimal(model.From) == 5);
            count.UpButton(); Check("spinner/count-up-model", count.Value == 21 && Convert.ToDecimal(model.To) == 21); count.DownButton(); Check("spinner/count-down-model", count.Value == 20 && Convert.ToDecimal(model.To) == 20);
            lastMode.Checked = true; last.Value = 3; last.UpButton(); Check("spinner/last-up-model", last.Value == 4 && (DateTime)model.From == DateTime.Today.AddDays(-4)); last.DownButton(); Check("spinner/last-down-model", last.Value == 3 && (DateTime)model.From == DateTime.Today.AddDays(-3));
            allMode.Checked = true;
            foreach (var number in new[] { start, count, last }) { decimal value = number.Value; number.Value = number.Minimum; number.DownButton(); Check(number.Name + "/minimum-clamped", number.Value == number.Minimum); number.Value = number.Maximum; number.UpButton(); Check(number.Name + "/maximum-clamped", number.Value == number.Maximum); number.Value = value; }
            dateMode.Checked = true; from.Checked = to.Checked = false; allMode.Checked = true; dateMode.Checked = true; Pump();
            Check("dates/unchecked-open-bounds-on-mode-refresh", !from.Checked && !to.Checked && (DateTime)model.From == DateTime.MinValue && (DateTime)model.To == DateTime.MaxValue);
            from.Checked = to.Checked = true; from.Value = SampleTime.Date; to.Value = SampleTime.Date.AddDays(2); Pump(); Check("dates/checked-exact-bounds", (DateTime)model.From == from.Value && (DateTime)model.To == to.Value);
            foreach (int width in new[] { 1100, 620 })
            {
                var metrics = result.Captures.Where(c => c.Width == width && c.Status == "Captured").SelectMany(c => c.LayoutMetrics).ToArray();
                foreach (string name in inputNames.Concat(new[] { "ReadEntryBtn", "ReadLastRB", "ReadFromRB", "ReadAllRB", "label2", "DaysLbl", "ToLbl", "ResetBtn", "CaptureBtn" }))
                {
                    var measured = metrics.Where(c => c.Name == name).ToArray();
                    Check(width + "/" + name + "/reachable", measured.Any(c => c.VisibleWidth >= c.Width - 4 && c.VisibleHeight >= c.Height - 4 && !c.CaptionClip), "Full input/caption/action available in at least one captured scroll position.");
                }
            }
            Check("settings/in-memory-provider-used", settings.Provider.Reads > 0 && settings.Provider.SaveAttempts == 0); Check("no-meter-actions", commands == 0);
            Check("optional-italy-actions-hidden-for-general-profile", new[] { "AddWhiteBtn", "AddBlackBtn", "RemoveBtn" }.All(n => !Find(n).Visible));
            Check("no-test-window-activation", activations == 0 && GetForegroundWindow() != shell.Handle, "PID=" + Environment.ProcessId + "; Activations=" + activations + "; initial foreground=" + foreground + "; final foreground=" + GetForegroundWindow() + "; test shell=" + shell.Handle);
            Write(Path.Combine(output, "focus-observation.json"), new { ProcessId = Environment.ProcessId, InitialForeground = foreground.ToInt64(), FinalForeground = GetForegroundWindow().ToInt64(), TestShell = shell.Handle.ToInt64(), Activations = activations, ActualDpi = editor.DeviceDpi });
            result.FixtureNotes.Add("Broad foreground identity may change due to the user or concurrent restore workers; the activation assertion attributes focus changes to this test shell only.");
            result.FixtureNotes.Add("Date Checked property setters do not emulate the native checkbox notification. Open-bound mapping is verified through the real read-mode refresh; focused checkbox clicking is not claimed. Numeric read-only backgrounds are compared with the same original control's pre-presentation native states.");
            result.FixtureNotes.Add("Settings.Default replaced before Profile construction with a file-free provider; original singleton restored. No Save/Reload or user configuration reads. Numeric extreme bounds are checked in All mode because the pre-existing active settings handler converts entries to Int32.");
            result.Monitor = string.Join(",", screens.Select(s => s.DeviceName)); result.Status = "Passed";
        }
        catch (Exception ex) { result.Status = "Failed"; result.Errors.Add(ex.ToString()); }
        finally { settings.Dispose(); Check("settings/original-instance-restored", settings.Restored); }
        if (result.BehaviorChecks.Any(c => !c.Passed)) result.Status = "Failed";
        Save(); Write(Path.Combine(output, "profile-input-summary.json"), new { result.AssemblySha256, result.Status, Captures = result.Captures.Count, Checks = result.BehaviorChecks.Count, FailedChecks = result.BehaviorChecks.Count(c => !c.Passed), ActualDpi = result.Captures.Select(c => c.Dpi).Distinct() });
        Console.WriteLine($"Profile inputs: {result.Captures.Count} captures; {result.BehaviorChecks.Count} checks; {result.BehaviorChecks.Count(c => !c.Passed)} failures.");
        return result.Status == "Passed" && result.BehaviorChecks.All(c => c.Passed) ? 0 : 1;
    }
}
