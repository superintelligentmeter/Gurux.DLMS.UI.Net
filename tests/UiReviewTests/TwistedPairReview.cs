// superintelligentmeter contributors, 2026-10-30. Focused offline viewport checks. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    static readonly string[] TwistedInputs = ["DescriptionTB", "LogicalNameTB", "ModeTb", "SpeedTb", "PrimaryAddressesList", "TabisList", "PrimaryAddressesAddBtn", "PrimaryAddressesEditBtn", "PrimaryAddressesRemoveBtn", "TabisAddBtn", "TabisEditBtn", "TabisRemoveBtn"];
    static readonly Dictionary<Form, Dictionary<Control, (string Family, float Points, int Fore, int Back)>> TwistedAppearance = new();

    static void CheckTwistedPair(Form editor, GXDLMSIecTwistedPairSetup model, Dictionary<string, Control> originals, CaseResult result, int step)
    {
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = "TwistedPair/" + result.State + "/step" + step + "/" + name, Passed = pass, Detail = detail });
        Control Find(string name) => Descendants(editor).Single(c => c.Name == name);
        Rectangle Visible(Control control)
        {
            var visible = control.RectangleToScreen(control.ClientRectangle);
            for (Control? parent = control.Parent; parent != null; parent = parent.Parent)
                visible = Rectangle.Intersect(visible, parent.RectangleToScreen(parent.ClientRectangle));
            return visible;
        }
        bool Full(Control control) { var visible = Visible(control); return visible.Width + 2 >= control.ClientSize.Width && visible.Height + 2 >= control.ClientSize.Height; }
        var current = TwistedInputs.Select(Find).ToArray();
        Check("original-instances-retained", current.All(c => ReferenceEquals(c, originals[c.Name])));
        Check("attribute-bindings", ((GXValueField)Find("LogicalNameTB")).Index == 1 && ((GXValueField)Find("ModeTb")).Index == 2 && ((GXValueField)Find("SpeedTb")).Index == 3);
        var first = (ListView)Find("PrimaryAddressesList"); var second = (ListView)Find("TabisList");
        Check("primary-address-values-bound", first.Items.Cast<ListViewItem>().Select(i => i.Text).SequenceEqual((model.PrimaryAddresses ?? []).Select(i => i.ToString())));
        Check("tabi-values-bound", second.Items.Cast<ListViewItem>().Select(i => i.Text).SequenceEqual((model.Tabis ?? []).Select(i => i.ToString())));
        foreach (var list in new[] { first, second })
        {
            Check(list.Name + "/useful-list-height", list.ClientSize.Height >= list.Font.Height * 3 + 4, "Client=" + list.ClientSize + "; font=" + list.Font.Height);
            Check(list.Name + "/native-list-scroll-retained", list.Scrollable && list.Columns.Count == 1 && list.Columns[0].Width >= list.Font.Height * 3);
        }
        Check("list-edges-aligned", Math.Abs(first.Left - second.Left) <= 2 && Math.Abs(first.Right - second.Right) <= 2);
        Check("list-actions-follow-their-list", first.Bottom <= Find("PrimaryAddressesAddBtn").Parent!.Top && second.Bottom <= Find("TabisAddBtn").Parent!.Top);
        if (editor.ClientSize.Height >= 650)
        {
            foreach (var control in current)
                Check(control.Name + "/fully-visible-without-outer-scroll", Full(control), "Bounds=" + control.Bounds + "; visible=" + Visible(control));
            Check("no-unneeded-outer-vertical-scroll", !editor.VerticalScroll.Visible);
        }
        var appearances = current.ToDictionary(c => c, c => (c.Font.FontFamily.Name, c.Font.SizeInPoints, c.ForeColor.ToArgb(), c.BackColor.ToArgb()));
        if (step == 0) TwistedAppearance[editor] = appearances;
        else Check("fonts-colors-stable-on-resize", appearances.All(p => TwistedAppearance[editor][p.Key] == p.Value));
        Check("description-remains-complete", Find("DescriptionTB").Text == (model.Description ?? ""));
        if (step == 3)
        {
            var initial = result.Captures.First(c => c.Width == 1450 && c.Variant == "default");
            foreach (var list in new[] { first, second })
            {
                var before = initial.Controls.First(c => c.Name == list.Name);
                Check(list.Name + "/restored-size", Math.Abs(before.Width - list.Width) <= 2 && Math.Abs(before.Height - list.Height) <= 2);
            }
        }
    }

    static void CheckTwistedPairReachability(CaseResult result)
    {
        foreach (int width in new[] { 1450, 1100, 620 })
        foreach (string name in TwistedInputs)
        {
            var metrics = result.Captures.Where(c => c.Width == width && c.Status == "Captured").SelectMany(c => c.LayoutMetrics).Where(c => c.Name == name).ToArray();
            result.BehaviorChecks.Add(new() { Name = "TwistedPair/" + result.State + "/" + width + "/" + name + "/reachable", Passed = metrics.Any(c => c.VisibleWidth + 2 >= c.ClientWidth && c.VisibleHeight + 2 >= c.ClientHeight), Detail = "Normal controls must fit at scroll-top; narrow controls may use existing outer scrolling. Only list scrolling is needed for long list data." });
        }
        result.FixtureNotes.Add("Focused IEC Twisted Pair checks use offline objects only. Actual Add/Edit use unchanged GXTextDlg constructors (byte Primary Address and sbyte TAB(i)); no modal action or transport invoked.");
    }
}
