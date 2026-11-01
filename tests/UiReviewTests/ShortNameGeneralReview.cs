// Superintelligent Meter contributors, 2026-10-30. Short Name General layout checks. GPL-2.0-only.
using Gurux.DLMS;
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    static void SeedShortNameGeneral(GXDLMSAssociationShortName model, string state)
    {
        if (!state.StartsWith("short-name-general")) return;
        model.Version = (byte)((IGXDLMSBase)model).GetMaxSupportedVersion();
        var security = new GXDLMSSecuritySetup("0.0.43.0.0.255");
        model.Parent.Add(security); model.ObjectList.Add(security);
        int index = 1;
        foreach (var item in model.ObjectList) item.ShortName = (ushort)(index++ * 0x100);
        model.SecuritySetupReference = security.LogicalName;
    }
    static void CheckShortNameGeneral(Form editor, CaseResult result, CaptureResult capture)
    {
        if (editor.GetType().Name != "GXDLMSAssociationShortNameView") return;
        Control Find(string name) => Descendants(editor).Single(c => c.Name == name);
        Rectangle Outer(Control c) => new(c.Parent!.PointToScreen(c.Location), c.Size);
        var general = Find("tabPage2"); if (!general.Visible) return;
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = "ShortNameGeneral/" + capture.Width + "/" + capture.Variant + "/" + name, Passed = pass, Detail = detail });
        var secret = (TextBox)Find("SecretTB"); var frame = secret.Parent!;
        var security = (ComboBox)Find("SecuritySetupCb"); var host = security.Parent!;
        var ascii = (CheckBox)Find("SecretAsciiCb"); var update = (GXButton)Find("UpdateTimeBtn");
        var logical = Outer(Find("LogicalNameTB")); var field = Outer(frame); var combo = Outer(host);
        var check = Outer(ascii); var button = Outer(update);
        int S(int value) => (int)Math.Round(value * editor.DeviceDpi / 96d);
        Check("absolute-input-left", logical.Left == field.Left && field.Left == combo.Left, $"Logical={logical}; secret={field}; security={combo}");
        Check("absolute-input-right", logical.Right == combo.Right, $"Logical={logical}; security={combo}");
        bool actionsBelow = check.Top >= field.Bottom + S(4);
        Check("action-rail", actionsBelow ? check.Left == logical.Left : button.Right == combo.Right, $"Stacked={actionsBelow}; logical={logical}; ascii={check}; update={button}");
        Check("scalar-height", Math.Abs(field.Height-S(32))<=1 && Math.Abs(host.Height-S(32))<=1);
        Check("update-method-index", update.Index == 5 && update.Action == ActionType.Action);
        Check("secret-format-gap", check.Left >= field.Right + S(8) || check.Top >= field.Bottom + S(4));
        Check("ascii-update-gap", button.Left >= check.Right + S(8) || button.Top >= check.Bottom + S(8));
        Check("secret-security-order", combo.Top >= Math.Max(field.Bottom, button.Bottom) + S(4));
        foreach (var control in new Control[] { frame, host, ascii, update })
        {
            var visible = control.RectangleToScreen(control.ClientRectangle);
            bool fullyVisible = true;
            for (Control? parent = control.Parent; parent != null && parent != editor; parent = parent.Parent)
                fullyVisible &= parent.RectangleToScreen(parent.ClientRectangle).Contains(visible);
            Check(control.Name + "/contained", fullyVisible, $"Bounds={Outer(control)}; page={Outer(general)}");
        }
        if (result.State.StartsWith("short-name-general"))
        {
            Check("security-object-bound", security.SelectedItem is GXDLMSSecuritySetup selected && selected.LogicalName == "0.0.43.0.0.255");
            string expected = result.State.Contains("long") ? string.Join(" ", Enumerable.Repeat("A1B2C3D4", 32)) : "41 42 43 44";
            Check("complete-secret-retained", secret.Text == expected);
        }
    }
}
