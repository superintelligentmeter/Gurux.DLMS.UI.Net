// superintelligentmeter contributors, 2026-10-30. Offline Schedule footer alignment. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Objects.Enums;
using Gurux.DLMS.UI;

internal static partial class Program
{
    static readonly string[] ScheduleFooterControls = ["LogicalNameTB", "EntriesView", "gxButton1", "AddObjectBtn", "RemoveObjectBtn"];
    static readonly string[] ScheduleFooterButtons = ["gxButton1", "AddObjectBtn", "RemoveObjectBtn"];
    static readonly Dictionary<Form, Dictionary<string, Control>> ScheduleFooterOriginals = new();
    static readonly Dictionary<Form, Dictionary<Control, (string Family, float Points, int Fore, int Back)>> ScheduleFooterAppearance = new();
    static readonly Dictionary<Form, Dictionary<string, Rectangle>> ScheduleFooterGeometry = new();

    static void RememberScheduleFooter(Form editor)
    {
        ScheduleFooterOriginals[editor] = Descendants(editor).Where(c => ScheduleFooterControls.Contains(c.Name)).ToDictionary(c => c.Name);
        ScheduleFooterAppearance[editor] = new();
    }

    static void PopulateScheduleFooterFixture(GXDLMSSchedule model, string state)
    {
        model.Entries.Clear();
        if (state == "blank") return;
        var script = model.Parent.OfType<GXDLMSScriptTable>().Single();
        int count = state == "long-schedule" ? 40 : 3;
        for (int i = 0; i < count; i++)
            model.Entries.Add(new GXScheduleEntry
            {
                Index = (ushort)(i + 1), Enable = i % 2 == 0,
                Script = i % 3 == 2 ? null : script, ScriptSelector = (ushort)(i + 1),
                SwitchTime = new GXTime(SampleTime.AddMinutes(i * 15)), ValidityWindow = (ushort)(10 + i),
                ExecWeekdays = Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday,
                ExecSpecDays = state == "long-schedule" ? string.Concat(Enumerable.Repeat("10100101", 8)) : "1010010",
                BeginDate = new GXDate(SampleTime.Date), EndDate = new GXDate(SampleTime.Date.AddYears(1))
            });
    }

    static string[] ScheduleEntryText(GXScheduleEntry item) =>
        [item.Index.ToString(), item.Enable.ToString(), item.Script?.LogicalName ?? "", item.ScriptSelector.ToString(),
         item.SwitchTime.ToString(), item.ValidityWindow.ToString(), item.ExecWeekdays.ToString(), item.ExecSpecDays,
         item.BeginDate.ToString(), item.EndDate.ToString()];

    static bool ScheduleEntriesBound(ListView list, GXDLMSSchedule model) => list.Items.Count == model.Entries.Count &&
        list.Items.Cast<ListViewItem>().Select((row, i) => ReferenceEquals(row.Tag, model.Entries[i]) &&
            row.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text).SequenceEqual(ScheduleEntryText(model.Entries[i]))).All(x => x);

    static void CheckScheduleFooter(Form editor, GXDLMSSchedule model, CaseResult result, int step)
    {
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = "ScheduleFooter/" + result.State + "/step" + step + "/" + name, Passed = pass, Detail = detail });
        Control Find(string name) => Descendants(editor).Single(c => c.Name == name);
        int S(int n) => (int)Math.Round(n * editor.DeviceDpi / 96d);
        Rectangle Bounds(Control c) { var p = editor.PointToClient(c.Parent!.PointToScreen(c.Location)); return new(p.X, p.Y, c.Width, c.Height); }
        bool Full(Control c)
        {
            var visible = c.RectangleToScreen(c.ClientRectangle);
            for (Control? parent = c.Parent; parent != null; parent = parent.Parent) visible = Rectangle.Intersect(visible, parent.RectangleToScreen(parent.ClientRectangle));
            return visible.Width + 2 >= c.ClientSize.Width && visible.Height + 2 >= c.ClientSize.Height;
        }
        var current = Descendants(editor).ToHashSet();
        Check("original-controls-retained", ScheduleFooterOriginals[editor].All(p => current.Contains(p.Value) && ReferenceEquals(Find(p.Key), p.Value)));
        Check("logical-name-attribute", ((GXValueField)Find("LogicalNameTB")).Index == 1);
        var list = (ListView)Find("EntriesView");
        Check("entries-complete-binding", ScheduleEntriesBound(list, model), "Rows=" + list.Items.Count + "; model=" + model.Entries.Count);
        Check("all10columns-retained", list.Columns.Count == 10);
        var buttons = ScheduleFooterButtons.Select(n => (GXButton)Find(n)).ToArray();
        Check("shared-button-width", buttons.Select(b => b.Width).Distinct().Count() == 1);
        Check("original-method-indices", buttons.Select(b => b.Index).SequenceEqual(new[] { 1, 2, 3 }));
        Check("original-button-labels", buttons.Select(b => b.Text).SequenceEqual(new[] { "Enable/Disable", "Insert", "Remove" }));
        Check("button-tab-order", buttons[0].TabIndex < buttons[1].TabIndex && buttons[1].TabIndex < buttons[2].TabIndex);
        var footer = buttons[0].Parent!;
        var table = (TableLayoutPanel)footer.Parent!;
        int row = table.GetRow(footer), height = table.GetRowHeights()[row];
        Check("footer40logical", Math.Abs(height - S(40)) <= 2, "Height=" + height + "; DPI=" + editor.DeviceDpi);
        Check("right-edge", Math.Abs(Bounds(buttons[^1]).Right - Bounds(list).Right) <= 1, "Button=" + Bounds(buttons[^1]) + "; list=" + Bounds(list));
        GetWindowRect(buttons[^1].Handle, out var nativeButton); GetWindowRect(list.Handle, out var nativeList);
        Check("native-right-edge", Math.Abs(nativeButton.Right - nativeList.Right) <= 1);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i]; var bounds = Bounds(button);
            GetWindowRect(button.Handle, out var native);
            Check(button.Name + "/native32logical", Math.Abs(native.Bottom - native.Top - S(32)) <= 2);
            Check(button.Name + "/width128logical", Math.Abs(button.Width - S(128)) <= 2);
            Check(button.Name + "/centered", Math.Abs(bounds.Top + bounds.Height / 2d - Bounds(footer).Top - footer.Height / 2d) <= 2);
            Check(button.Name + "/list-gap4logical", Math.Abs(bounds.Top - Bounds(list).Bottom - S(4)) <= 2);
            int textWidth = TextRenderer.MeasureText(button.Text, button.Font).Width;
            int requiredWidth = textWidth + button.Padding.Horizontal + 2 * button.FlatAppearance.BorderSize;
            using var graphics = button.CreateGraphics();
            int nativeTextWidth = TextRenderer.MeasureText(graphics, button.Text, button.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine).Width;
            var preferred = button.GetPreferredSize(Size.Empty);
            Check(button.Name + "/label-fits", requiredWidth <= button.ClientSize.Width,
                "Text=" + textWidth + "; nativeHdcText=" + nativeTextWidth + "; nativeHdcDpi=" + graphics.DpiX + "; preferred=" + preferred + "; padding=" + button.Padding.Horizontal + "; border=" + button.FlatAppearance.BorderSize + "; required=" + requiredWidth + "; client=" + button.ClientSize.Width + "; font=" + button.Font);
            if (step != 1) Check(button.Name + "/normal-visible", Full(button));
            if (i > 0)
                Check(button.Name + "/same-row-and-gap8logical", bounds.Top == Bounds(buttons[i - 1]).Top && Math.Abs(bounds.Left - Bounds(buttons[i - 1]).Right - S(8)) <= 2);
        }
        if (step != 1) Check("normal-list-visible", Full(list));
        foreach (var control in ScheduleFooterOriginals[editor].Values)
        {
            var now = (control.Font.FontFamily.Name, control.Font.SizeInPoints, control.ForeColor.ToArgb(), control.BackColor.ToArgb());
            if (ScheduleFooterAppearance[editor].TryGetValue(control, out var before)) Check(control.Name + "/fonts-colors-stable", before == now);
            else ScheduleFooterAppearance[editor][control] = now;
        }
        var geometry = ScheduleFooterOriginals[editor].ToDictionary(p => p.Key, p => Bounds(p.Value));
        if (step == 0) ScheduleFooterGeometry[editor] = geometry;
        if (step == 2) Check("normal-narrow-normal-restores", ScheduleFooterGeometry[editor].All(p => geometry[p.Key] == p.Value));
        for (int repeat = 0; repeat < 4; repeat++) { editor.PerformLayout(); footer.PerformLayout(); Pump(); }
        Check("layout-settles", geometry.All(p => Bounds(Find(p.Key)) == p.Value));
    }

    static void CheckScheduleFooterRefresh(Form editor, GXDLMSSchedule model, GXDLMSClient client, CaseResult result)
    {
        var before = model.Entries.ToArray();
        GXDlmsUi.ObjectChanged((IGXDLMSView)editor, client, model, true); Pump();
        var list = Descendants(editor).OfType<ListView>().Single(c => c.Name == "EntriesView");
        result.BehaviorChecks.Add(new() { Name = "ScheduleFooter/whole-object-refresh-preserves-entries", Passed = model.Entries.SequenceEqual(before) && ScheduleEntriesBound(list, model) });
        foreach (int width in result.Captures.Select(c => c.Width).Distinct())
        foreach (string name in ScheduleFooterControls)
        {
            var metrics = result.Captures.Where(c => c.Width == width).SelectMany(c => c.LayoutMetrics).Where(c => c.Name == name);
            result.BehaviorChecks.Add(new() { Name = "ScheduleFooter/" + width + "/" + name + "/reachable", Passed = metrics.Any(c => c.VisibleWidth + 2 >= c.ClientWidth && c.VisibleHeight + 2 >= c.ClientHeight) });
        }
        result.FixtureNotes.Add("Schedule footer tests preserve all entry tags/10 column values and original method indices. Native lists retain their own scrolling; short viewports may use outer scrolling. No Enable/Disable, Insert, Remove, PreAction, modal dialog, transport or persistent setting action was invoked.");
    }
}
