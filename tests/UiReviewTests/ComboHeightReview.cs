// superintelligentmeter contributors, 2026-10-30. Native combo height and interaction tests. GPL-2.0-only.
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Reflection;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    [StructLayout(LayoutKind.Sequential)] struct ComboRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct ComboInfo { public int Size; public ComboRect Item, Button; public uint State; public IntPtr Combo, Edit, List; }
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] static extern IntPtr ComboTestMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool GetComboBoxInfo(IntPtr hwnd, ref ComboInfo info);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out ComboRect rect);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] static extern int GetComboWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] static extern int SetComboWindowLong(IntPtr hwnd, int index, int value);
    [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint color, byte alpha, uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();

    static void CheckNativeComboHeight(ComboBox combo, Action<string, bool, string> check)
    {
        bool hosted = combo.Parent is GXValueField || combo.Parent?.GetType().Name == "NativeComboHost";
        if (combo.DropDownStyle == ComboBoxStyle.Simple) return;
        int expected = hosted ? Math.Max(combo.PreferredHeight, Math.Min((int)Math.Round(combo.DeviceDpi * 32d / 96), combo.Parent!.ClientSize.Height)) : combo.PreferredHeight;
        GetWindowRect(combo.Handle, out var native);
        check(hosted ? "standard-slot-native-height" : "compact-native-height", Math.Abs(native.Bottom - native.Top - expected) <= 2, $"Native={native.Bottom-native.Top}; managed={combo.Height}; expected={expected}; parent={combo.Parent?.ClientSize}");
        check("normal-native-drawing", combo.DrawMode == DrawMode.Normal && combo.FlatStyle == FlatStyle.Standard, combo.DrawMode + "/" + combo.FlatStyle);
    }

    static void CheckNativePopupAndKeyboard(ComboBox combo, Action<string, bool, string> check)
    {
        if (!combo.Enabled || combo.Items.Count < 2) return;
        var info = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() };
        bool found = GetComboBoxInfo(combo.Handle, ref info) && info.List != IntPtr.Zero;
        check("popup-native-handle", found, "Native ComboLBox obtained from the test combo."); if (!found) return;
        var foreground = GetForegroundWindow(); int style = GetComboWindowLong(info.List, -20);
        int initial = combo.SelectedIndex, listHeight = (int)ComboTestMessage(combo.Handle, 0x154, IntPtr.Zero, IntPtr.Zero);
        var items = combo.Items.Cast<object>().ToArray(); var font = combo.Font; var colors = (combo.ForeColor, combo.BackColor);
        // Suppress only this worker's popup before it is opened; never activate the
        // shell, move the pointer, send global keys or touch another application's HWND.
        SetComboWindowLong(info.List, -20, style | 0x80000);
        bool hidden = SetLayeredWindowAttributes(info.List, 0, 0, 2);
        check("popup-visibility-suppressed", hidden, "Layered alpha zero on the owned list window."); if (!hidden) { SetComboWindowLong(info.List, -20, style); return; }
        try
        {
            combo.DroppedDown = true; Pump(); GetWindowRect(info.List, out var bounds);
            check("native-popup-opens", combo.DroppedDown && bounds.Bottom > bounds.Top && bounds.Right > bounds.Left, $"Popup={bounds.Right-bounds.Left}x{bounds.Bottom-bounds.Top}");
            check("native-list-row-readable", listHeight >= combo.Font.Height, $"Item={listHeight}; font={combo.Font.Height}");
            combo.DroppedDown = false; Pump();
            int expected = initial < combo.Items.Count - 1 ? initial + 1 : initial - 1;
            int key = initial < combo.Items.Count - 1 ? 0x28 : 0x26;
            ComboTestMessage(combo.Handle, 0x100, (IntPtr)key, IntPtr.Zero); ComboTestMessage(combo.Handle, 0x101, (IntPtr)key, IntPtr.Zero); Pump();
            check("targeted-keyboard-selection", combo.SelectedIndex == expected, $"Before={initial}; expected={expected}; actual={combo.SelectedIndex}");
            check("list-items-and-style-retained", combo.Items.Cast<object>().SequenceEqual(items) && combo.Font.Equals(font) && combo.ForeColor == colors.Item1 && combo.BackColor == colors.Item2 && (int)ComboTestMessage(combo.Handle, 0x154, IntPtr.Zero, IntPtr.Zero) == listHeight, "Items, font, colors and popup item height unchanged.");
        }
        finally { combo.DroppedDown = false; SetComboWindowLong(info.List, -20, style); }
        check("no-foreground-focus-change", GetForegroundWindow() == foreground, "Only HWND-targeted messages were used.");
    }

    static void CheckComboHeightStates(Form editor, IGXDLMSView view, GXDLMSClient client, CaseResult result, string output, Size size, string prefix)
    {
        var model = view.Target;
        foreach (var combo in Descendants(editor).OfType<ComboBox>().Where(c => c.Visible).ToArray())
        {
            var field = combo.Parent as GXValueField; string name = field?.Name ?? combo.Name;
            int index = field?.Index ?? (combo.Name == "UnitTB" ? model is GXDLMSDemandRegister ? 4 : 3 : 0);
            if (index <= 0) continue;
            void Check(string rule, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = result.View.Split('.').Last() + "/" + prefix + "/" + name + "/" + rule, Passed = pass, Detail = detail });
            var access = model.GetAccess(index); var readTime = model.GetLastReadTime(index); int selected = combo.SelectedIndex;
            void Bind(bool connected) { GXDlmsUi.ObjectChanged(view, client, model, connected); editor.PerformLayout(); Pump(); }
            void Observe(string state) { CheckNativeComboHeight(combo, Check); CaptureAtPosition(editor, result, output, size, prefix + "-" + name + "-" + state, false); }
            try
            {
                model.SetAccess(index, AccessMode.ReadWrite); model.SetLastReadTime(index, SampleTime); Bind(true);
                model.ClearDirty(index);
                object? SemanticValue() => field != null ? model.GetValues()[index - 1] : model.GetType().GetProperty("Unit")?.GetValue(model);
                object? beforeValue = SemanticValue(); CheckNativePopupAndKeyboard(combo, Check); object? afterValue = SemanticValue();
                // HWND arrow navigation is deliberately unfocused. Register's
                // native notification does not commit in that artificial route
                // on either the old34E31D4D or new1411E260 assembly. Record it
                // without claiming full focused keyboard-to-model coverage;
                // CheckComboBinding separately verifies real selection events.
                result.FixtureNotes.Add($"Unfocused HWND keyboard observation {name}/{prefix}: before={beforeValue}; after={afterValue}; selected={combo.SelectedItem}; dirty={model.GetDirty(index, out _)}. Focused keyboard binding is not claimed.");
                if (selected >= 0) combo.SelectedIndex = selected; model.ClearDirty(index); Bind(true); Observe("editable");
                int changes = 0; EventHandler changed = (_, _) => changes++; combo.SizeChanged += changed;
                var before = combo.Bounds; for (int i = 0; i < 6; i++) { combo.Parent!.PerformLayout(); Pump(); } combo.SizeChanged -= changed;
                Check("repeated-layout-no-size-oscillation", changes == 0 && combo.Bounds == before, $"SizeChanged={changes}; before={before}; after={combo.Bounds}");
                if (field != null) { model.SetLastReadTime(index, DateTime.MinValue); Bind(true); Check("empty-unread-selection", combo.SelectedIndex == -1 && combo.Text.Length == 0); Observe("unread"); }
                model.SetLastReadTime(index, SampleTime); model.SetAccess(index, AccessMode.Read); Bind(true); Check("readonly-disabled", !combo.Enabled); Observe("readonly");
                model.SetAccess(index, AccessMode.ReadWrite); Bind(false); Check("disconnected-disabled", !combo.Enabled); Observe("disconnected");
            }
            finally { model.SetAccess(index, access); model.SetLastReadTime(index, readTime); model.ClearDirty(index); Bind(true); }
        }
    }

    static int ComboHeightFixture(string output)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); ApplicationTypography.Install();
        var result = new CaseResult { View = "Shared native ComboBox fixture", State = "editable-and-compact", AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Ui.Location))) };
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = name, Passed = pass, Detail = detail });
        try
        {
            using var shell = new AuditShell { ShowInTaskbar = false, Opacity = 0, StartPosition = FormStartPosition.Manual, Font = ApplicationTypography.BodyFont };
            var area = Screen.AllScreens[0].WorkingArea; shell.Location = new(area.X + 24, area.Y + 24); shell.Show(); for (int i = 0; i < 6; i++) Pump();
            using var editor = new Form { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill, Font = ApplicationTypography.BodyFont }; shell.Controls.Add(editor); editor.Show();
            var table = ModernPresentation.CompactSurface(editor);
            editor.Controls.Add(table);
            var editable = new ComboBox { Name = "EditableStandard", DropDownStyle = ComboBoxStyle.DropDown, Font = editor.Font }; editable.Items.AddRange(new object[] { "Automatic", "Manual", "Disabled" }); editable.SelectedIndex = 0;
            ModernPresentation.CompactRow(table, "Editable choice", editable);
            var scalar = new TextBox { Name = "ComparisonText", Text = "Standard input", Font = editor.Font }; ModernPresentation.CompactRow(table, "Text value", scalar);
            var compact = new ComboBox { Name = "UnhostedCompact", DropDownStyle = ComboBoxStyle.DropDownList, Font = editor.Font, Location = new(24, 180), Width = 150 }; compact.Items.AddRange(new object[] { "One", "Two" }); compact.SelectedIndex = 0; editor.Controls.Add(compact); compact.BringToFront();
            foreach (int monitor in Enumerable.Range(0, Screen.AllScreens.Length))
            {
                area = Screen.AllScreens[monitor].WorkingArea; shell.Location = new(area.X + 24, area.Y + 24); for (int i = 0; i < 6; i++) Pump();
                int phase = 0;
                foreach (var size in new[] { new Size(1100, 760), new Size(620, 560), new Size(1100, 760) })
                {
                    shell.ClientSize = size; editor.PerformLayout(); for (int i = 0; i < 5; i++) Pump(); string prefix = "monitor" + monitor + "-" + phase++;
                    void Record(string name, bool pass, string detail) => Check(prefix + "/" + name, pass, detail);
                    editable.SelectedIndex = 0; editable.Text = editable.GetItemText(editable.Items[0]);
                    Check(prefix + "/editable-is-rendered-child", editor.Contains(editable) && editable.Visible && editable.DeviceDpi == editor.DeviceDpi, $"Visible={editable.Visible}; combo DPI={editable.DeviceDpi}; editor DPI={editor.DeviceDpi}");
                    Check(prefix + "/editable-matches-text-frame", Math.Abs(editable.Height - scalar.Parent!.Height) <= 2, $"Combo={editable.Height}; text frame={scalar.Parent.Height}");
                    CheckNativeComboHeight(editable, Record); CheckNativeComboHeight(compact, Record); CheckNativePopupAndKeyboard(editable, Record);
                    // A custom edit value is unselected. Retaining an old selected
                    // item while replacing Text reverts to that item on handle
                    // recreation in the unchanged native baseline as well.
                    editable.SelectedIndex = -1; editable.Text = "Local typed value"; Pump(); Check(prefix + "/editable-text-retained", editable.Text == "Local typed value" && editable.SelectedIndex == -1 && editable.DropDownStyle == ComboBoxStyle.DropDown);
                    string retainedText = editable.Text; int retainedSelection = editable.SelectedIndex; Font originalFont = editable.Font;
                    using (var changedFont = new Font(originalFont.FontFamily, originalFont.SizeInPoints + 1, originalFont.Style))
                    {
                        editable.Font = changedFont; for (int i = 0; i < 5; i++) Pump(); CheckNativeComboHeight(editable, Record);
                        Check(prefix + "/font-change-preserves-value", editable.Text == retainedText && editable.SelectedIndex == retainedSelection);
                        editable.Font = originalFont; for (int i = 0; i < 5; i++) Pump();
                    }
                    typeof(Control).GetMethod("RecreateHandle", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editable, null); for (int i = 0; i < 5; i++) Pump(); CheckNativeComboHeight(editable, Record);
                    Check(prefix + "/handle-recreation-preserves-value", editable.IsHandleCreated && editable.Text == retainedText && editable.SelectedIndex == retainedSelection && editable.Font.Equals(originalFont));
                    int count = 0; EventHandler handler = (_, _) => count++; editable.SizeChanged += handler; for (int i = 0; i < 6; i++) { editable.Parent!.PerformLayout(); Pump(); } editable.SizeChanged -= handler; Check(prefix + "/no-layout-size-oscillation", count == 0, "SizeChanged=" + count);
                    CaptureAtPosition(editor, result, output, size, prefix, false);
                }
            }
            result.Status = "Passed";
        }
        catch (Exception ex) { result.Status = "Failed"; result.Errors.Add(ex.ToString()); }
        Write(Path.Combine(output, "result.json"), result); Console.WriteLine($"Shared fixture: {result.Captures.Count} captures; {result.BehaviorChecks.Count} checks; {result.BehaviorChecks.Count(c => !c.Passed)} failures.");
        return result.Status == "Passed" && result.BehaviorChecks.All(c => c.Passed) ? 0 : 1;
    }
}
