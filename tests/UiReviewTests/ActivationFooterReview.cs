// superintelligentmeter contributors, 2026-10-30. Offline native footer alignment. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS;
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    static readonly string[] ActivationControls = ["LogicalNameTB", "DescriptionTB", "Assigments", "AssigmentAddBtn", "Masks", "MaskAddBtn", "MaskRemoveBtn", "ActiveMasksCb"];
    static readonly Dictionary<Form, Dictionary<string,Control>> ActivationOriginals = new();
    static readonly Dictionary<Form, Dictionary<Control,(string Family,float Points,int Fore,int Back)>> ActivationAppearance = new();
    static void RememberActivationFooter(Form editor)
    {
        ActivationOriginals[editor]=Descendants(editor).Where(c=>ActivationControls.Contains(c.Name)).ToDictionary(c=>c.Name);
        ActivationAppearance[editor]=new();
    }
    static void PopulateActivationFooterFixture(GXDLMSRegisterActivation model,string state)
    {
        if(state!="long-activation")return;
        model.Description="Register activation commissioning fixture with long mask names and enough assignment rows for native scrolling";
        model.RegisterAssignment=Enumerable.Range(1,30).Select(i=>new GXDLMSObjectDefinition { ObjectType=Gurux.DLMS.Enums.ObjectType.Register, LogicalName="1.0."+i+".8.0.255" }).ToArray();
        model.MaskList.Clear();
        for(int i=0;i<16;i++)model.MaskList.Add(new(System.Text.Encoding.ASCII.GetBytes("Regional commissioning activation mask with long name "+i),Enumerable.Range(1,Math.Min(i+1,30)).Select(x=>(byte)x).ToArray()));
        model.ActiveMask=model.MaskList[7].Key;
    }
    static string ActivationMaskText(byte[]? value) => value==null || value.Length==0 ? "" : value.All(b=>b>=0x21&&b<=0x7e) ? System.Text.Encoding.ASCII.GetString(value) : GXDLMSTranslator.ToHex(value);
    static void CheckActivationFooter(Form editor,GXDLMSRegisterActivation model,CaseResult result,string state)
    {
        void Check(string name,bool pass,string detail="")=>result.BehaviorChecks.Add(new(){Name="ActivationFooter/"+result.State+"/"+state+"/"+name,Passed=pass,Detail=detail});
        Control Find(string name)=>Descendants(editor).Single(c=>c.Name==name);
        int S(int value)=>(int)Math.Round(value*editor.DeviceDpi/96d);
        int X(Control c)=>editor.PointToClient(c.PointToScreen(Point.Empty)).X;
        int Y(Control c)=>editor.PointToClient(c.PointToScreen(Point.Empty)).Y;
        bool Full(Control c)
        {
            var visible=c.RectangleToScreen(c.ClientRectangle);
            for(Control? parent=c.Parent;parent!=null;parent=parent.Parent)visible=Rectangle.Intersect(visible,parent.RectangleToScreen(parent.ClientRectangle));
            return visible.Width+2>=c.ClientSize.Width&&visible.Height+2>=c.ClientSize.Height;
        }
        var current=Descendants(editor).ToHashSet();
        Check("original-controls-retained",ActivationOriginals[editor].All(p=>current.Contains(p.Value)&&ReferenceEquals(Find(p.Key),p.Value)));
        Check("method-indices",((GXButton)Find("AssigmentAddBtn")).Index==1&&((GXButton)Find("MaskAddBtn")).Index==2&&((GXButton)Find("MaskRemoveBtn")).Index==3);
        var assignment=(ListView)Find("Assigments");var masks=(ListView)Find("Masks");var active=(ComboBox)Find("ActiveMasksCb");
        Check("assignment-binding",assignment.Items.Count==(model.RegisterAssignment?.Length??0));
        Check("mask-binding",masks.Items.Count==model.MaskList.Count&&active.Items.Count==model.MaskList.Count);
        string expected=ActivationMaskText(model.ActiveMask);
        Check("active-mask-binding",active.Text==expected,"Actual="+active.Text+"; expected="+expected);
        foreach(var entry in new[]{(List:assignment,Names:new[]{"AssigmentAddBtn"}),(List:masks,Names:new[]{"MaskAddBtn","MaskRemoveBtn"})})
        {
            if(!entry.List.Visible)continue;
            var buttons=entry.Names.Select(n=>(GXButton)Find(n)).ToArray();var footer=buttons[0].Parent!;var table=(TableLayoutPanel)footer.Parent!;
            int row=table.GetRow(footer);var heights=table.GetRowHeights();
            Check(entry.List.Name+"/list-visible",Full(entry.List));
            Check(entry.List.Name+"/right-edge",Math.Abs(X(buttons[^1])+buttons[^1].Width-X(entry.List)-entry.List.Width)<=1,"ButtonRight="+(X(buttons[^1])+buttons[^1].Width)+"; listRight="+(X(entry.List)+entry.List.Width));
            Check(entry.List.Name+"/footer40logical",Math.Abs(heights[row]-S(40))<=2,"Height="+heights[row]+"; dpi="+editor.DeviceDpi);
            foreach(var button in buttons)
            {
                Check(button.Name+"/button32logical",Math.Abs(button.Height-S(32))<=2,"Height="+button.Height+"; dpi="+editor.DeviceDpi);
                Check(button.Name+"/footer-centered",Math.Abs(Y(button)+button.Height/2d-Y(footer)-footer.Height/2d)<=2);
                Check(button.Name+"/visible",Full(button));
                Check(button.Name+"/below-list-gap",Math.Abs(Y(button)-Y(entry.List)-entry.List.Height-S(4))<=2);
            }
            if(buttons.Length==2)
            {
                Check("mask-buttons-same-row",buttons[0].Top==buttons[1].Top&&buttons[0].Height==buttons[1].Height);
                Check("mask-buttons8logical-gap",Math.Abs(X(buttons[1])-X(buttons[0])-buttons[0].Width-S(8))<=1);
            }
        }
        foreach(var control in ActivationOriginals[editor].Values.Where(c=>c.Visible))
        {
            var now=(control.Font.FontFamily.Name,control.Font.SizeInPoints,control.ForeColor.ToArgb(),control.BackColor.ToArgb());
            if(ActivationAppearance[editor].TryGetValue(control,out var before))Check(control.Name+"/font-colors-stable",before==now);
            else ActivationAppearance[editor][control]=now;
        }
    }
    static void CheckActivationRefresh(Form editor,GXDLMSRegisterActivation model,GXDLMSClient client,CaseResult result)
    {
        var before=(model.ActiveMask??[]).ToArray();
        GXDlmsUi.ObjectChanged((IGXDLMSView)editor,client,model,true);Pump();
        var active=Descendants(editor).OfType<ComboBox>().Single(c=>c.Name=="ActiveMasksCb");
        result.BehaviorChecks.Add(new(){Name="ActivationFooter/active-mask-full-refresh-preserved",Passed=(model.ActiveMask??[]).SequenceEqual(before)&&active.Text==ActivationMaskText(before)});
        result.FixtureNotes.Add("Original Assignments/Mask action buttons were not invoked; no meter command, modal Add/Remove dialog or settings write performed. Both native tab footers and active-mask refresh were checked.");
    }
}
