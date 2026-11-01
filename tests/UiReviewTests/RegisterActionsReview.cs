// superintelligentmeter contributors, 2026-10-30. Offline native Register action alignment. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS.UI;

internal static partial class Program
{
    static readonly string[] RegisterActionViews = ["GXDLMSRegisterView", "GXDLMSExtendedRegisterView", "GXDLMSDemandRegisterView"];
    sealed record RegisterActionsSnapshot(Dictionary<string, Control> Controls, Dictionary<Control, int> Indices);
    static readonly Dictionary<Form, RegisterActionsSnapshot> RegisterActionOriginals = new();
    static readonly Dictionary<Form, Dictionary<Control,(string Family,float Points,int Fore,int Back)>> RegisterActionAppearance = new();

    static void RememberRegisterActions(Form editor)
    {
        var controls=Descendants(editor).Where(c=>c is GXValueField or GXButton || c.Name=="UnitTB").ToDictionary(c=>c.Name);
        var indices=controls.Values.Where(c=>c is GXValueField or GXButton).ToDictionary(c=>c,c=>c is GXValueField f?f.Index:((GXButton)c).Index);
        RegisterActionOriginals[editor]=new(controls,indices);
    }

    static void CheckRegisterActions(Form editor, CaseResult result, int step)
    {
        void Check(string name,bool pass,string detail="")=>result.BehaviorChecks.Add(new(){Name="RegisterActions/"+editor.GetType().Name+"/"+result.State+"/step"+step+"/"+name,Passed=pass,Detail=detail});
        Control Find(string name)=>Descendants(editor).Single(c=>c.Name==name);
        int S(int value)=>(int)Math.Round(value*editor.DeviceDpi/96d);
        int Left(Control c)=>editor.PointToClient(c.PointToScreen(Point.Empty)).X;
        int Top(Control c)=>editor.PointToClient(c.PointToScreen(Point.Empty)).Y;
        var reset=(GXButton)Find("ResetBtn");
        var buttons=editor.GetType().Name=="GXDLMSDemandRegisterView"?new[]{(GXButton)Find("NextPeriodBtn"),reset}:new[]{reset};
        var first=buttons[0];var field=Find("LogicalNameTB");var panel=first.Parent!;
        var rows=(TableLayoutPanel)panel.Parent!;int row=rows.GetRow(panel);var heights=rows.GetRowHeights();
        var original=RegisterActionOriginals[editor];var current=Descendants(editor).ToHashSet();
        Check("original-control-instances-and-indices",original.Controls.All(p=>current.Contains(p.Value)&&ReferenceEquals(Find(p.Key),p.Value))&&original.Indices.All(p=>(p.Key is GXValueField f?f.Index:((GXButton)p.Key).Index)==p.Value));
        Check("method-indices-retained",reset.Index==1&&(buttons.Length==1||buttons[0].Index==2));
        Check("first-action-on-scalar-field-left-edge",Math.Abs(Left(first)-Left(field))<=1,"Action="+Left(first)+"; field="+Left(field));
        Check("action-row-current-dpi-height",Math.Abs(heights[row]-S(40))<=2,"Row="+heights[row]+"; dpi="+editor.DeviceDpi);
        int rowTop=Top(rows)+rows.Padding.Top+heights.Take(row).Sum();
        foreach(var button in buttons)
        {
            Check(button.Name+"/native32logical-height",Math.Abs(button.Height-S(32))<=2,"Height="+button.Height+"; expected="+S(32));
            Check(button.Name+"/centered-in-action-row",Math.Abs((Top(button)+button.Height/2d)-(rowTop+heights[row]/2d))<=2,"ButtonTop="+Top(button)+"; rowTop="+rowTop+"; rowHeight="+heights[row]);
            Check(button.Name+"/contained-by-field-cell",button.Left>=0&&button.Right<=panel.ClientSize.Width+1&&button.Bottom<=panel.ClientSize.Height+1);
        }
        if(buttons.Length==2)
        {
            Check("demand-actions-same-row-center-height",buttons[0].Top==reset.Top&&buttons[0].Height==reset.Height);
            Check("demand8logical-gap",Math.Abs(reset.Left-buttons[0].Right-S(8))<=1,"Gap="+(reset.Left-buttons[0].Right));
            Check("demand-reading-tab-order",buttons[0].TabIndex<reset.TabIndex);
        }
        var appearance=original.Controls.Values.ToDictionary(c=>c,c=>(c.Font.FontFamily.Name,c.Font.SizeInPoints,c.ForeColor.ToArgb(),c.BackColor.ToArgb()));
        if(step==0)RegisterActionAppearance[editor]=appearance;
        else Check("typography-colors-retained-on-resize",appearance.All(p=>RegisterActionAppearance[editor][p.Key]==p.Value));
        if(step==2)
        {
            var baseline=result.Captures.First(c=>c.Variant=="default");
            foreach(var button in buttons)
            {
                var before=baseline.Controls.First(c=>c.Name==button.Name);
                Check(button.Name+"/restored-bounds",Math.Abs(before.X-Left(button))<=2&&Math.Abs(before.Y-Top(button))<=2&&Math.Abs(before.Width-button.Width)<=2&&Math.Abs(before.Height-button.Height)<=2);
            }
        }
    }

    static void CheckRegisterActionReachability(CaseResult result)
    {
        var names=result.View.EndsWith("GXDLMSDemandRegisterView")?new[]{"NextPeriodBtn","ResetBtn"}:new[]{"ResetBtn"};
        foreach(int width in new[]{1100,620})foreach(string name in names)
        {
            var observations=result.Captures.Where(c=>c.Width==width).SelectMany(c=>c.LayoutMetrics).Where(c=>c.Name==name);
            result.BehaviorChecks.Add(new(){Name="RegisterActions/"+result.View+"/"+result.State+"/"+width+"/"+name+"/reachable",Passed=observations.Any(c=>c.VisibleWidth+2>=c.ClientWidth&&c.VisibleHeight+2>=c.ClientHeight),Detail="Normal/narrow native action remains fully visible in a captured top/bottom/reveal position."});
        }
        result.FixtureNotes.Add("Reset and Next Period native buttons retain original instances and method indices. No click, meter action, confirmation dialog or settings operation invoked. Cases run on each currently connected physical monitor.");
    }
}
