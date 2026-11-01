// superintelligentmeter contributors, 2026-10-30. Offline Profile header-layout checks. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    static readonly string[] ProfileHeaderInputs = ["LogicalNameTB", "CapturePeriodTB", "EntriesInUseTB", "EntriesTB", "SortModeTB", "SortObjectReference", "ResetBtn", "CaptureBtn", "StartEntry", "EndEntry", "ReadLastTB", "StartPick", "ToPick", "ReadEntryBtn", "ReadLastRB", "ReadFromRB", "ReadAllRB", "AddWhiteBtn", "AddBlackBtn", "RemoveBtn"];
    static readonly Dictionary<Form, Dictionary<Control, (string Family,float Points,int Fore,int Back)>> ProfileHeaderAppearance = new();
    static readonly Dictionary<Form, Dictionary<Control,Rectangle>> ProfileHeaderGeometry = new();
    static void PopulateProfileHeaderFixture(GXDLMSProfileGeneric profile, string state)
    {
        var register=profile.Parent.OfType<GXDLMSRegister>().First();
        register.Description=state=="long-profile-reference" ? "North building incoming supply, laboratory reference meter, total imported active energy for the main distribution board and its independently monitored downstream circuits; retained descriptive name from the saved meter configuration for commissioning and periodic inspection." : "Imported active energy";
        profile.SortObject=state=="blank"?null:register;
        profile.SortAttributeIndex=state=="blank"?0:2;profile.SortDataIndex=0;
        profile.SetDataType(6,DataType.Structure);profile.SetAccess(6,AccessMode.Read);profile.SetLastReadTime(6,SampleTime);
        if(state=="italy-profile")
        {
            profile.LogicalName="0.0.21.0.1.255";
            var owner=new GXDLMSClient { Standard=Standard.Italy }; var objects=profile.Parent.ToArray(); owner.Objects.AddRange(objects);
            var setParent=typeof(GXDLMSObject).GetProperty("Parent")!.GetSetMethod(true)!;
            foreach(var obj in objects)setParent.Invoke(obj,new object[]{owner.Objects});
        }
    }
    static (int X,int Y,int Right,int Bottom) ProfileBounds(Form editor,Control control)
    {var p=editor.PointToClient(control.PointToScreen(Point.Empty));return(p.X,p.Y,p.X+control.Width,p.Y+control.Height);}
    static bool ProfileFull(Control control)
    {
        var visible=control.RectangleToScreen(control.ClientRectangle);
        for(Control? parent=control.Parent;parent!=null;parent=parent.Parent)visible=Rectangle.Intersect(visible,parent.RectangleToScreen(parent.ClientRectangle));
        return visible.Width+2>=control.ClientSize.Width&&visible.Height+2>=control.ClientSize.Height;
    }
    static void CheckProfileHeader(Form editor,GXDLMSProfileGeneric profile,Dictionary<string,Control> originals,CaseResult result,int step,string output,Size requested)
    {
        void Check(string name,bool pass,string detail="")=>result.BehaviorChecks.Add(new(){Name="ProfileHeader/"+result.State+"/step"+step+"/"+name,Passed=pass,Detail=detail});
        Control Find(string name)=>Descendants(editor).Single(c=>c.Name==name);
        int S(int n)=>(int)Math.Round(n*editor.DeviceDpi/96d);
        var all=Descendants(editor).ToHashSet();
        Check("original-controls-retained",originals.All(p=>all.Contains(p.Value)&&ReferenceEquals(Find(p.Key),p.Value)));
        Check("original-attribute-bindings",((GXValueField)Find("LogicalNameTB")).Index==1&&((GXValueField)Find("CapturePeriodTB")).Index==4&&((GXValueField)Find("EntriesInUseTB")).Index==7&&((GXValueField)Find("EntriesTB")).Index==8&&((GXValueField)Find("SortModeTB")).Index==5);
        foreach(string name in new[]{"LogicalNameTB","CapturePeriodTB","EntriesInUseTB","EntriesTB","SortModeTB"})
        {
            var field=Find(name);GetWindowRect(field.Handle,out var native);
            Check(name+"/standard-metadata-height",Math.Abs(field.Height-S(32))<=1&&Math.Abs(native.Bottom-native.Top-S(32))<=1,
                $"Managed={field.Height}; native={native.Bottom-native.Top}; expected={S(32)}; maximum={field.MaximumSize.Height}");
        }
        foreach(string name in new[]{"StartEntry","EndEntry","ReadLastTB"})
        {
            var number=(NumericUpDown)Find(name);GetWindowRect(number.Handle,out var native);
            Check(name+"/native-height-preserved",Math.Abs(native.Bottom-native.Top-S(32))<=2,"Native="+(native.Bottom-native.Top)+"; expected="+S(32));
        }
        foreach(string name in new[]{"StartPick","ToPick"})
        {
            var date=(DateTimePicker)Find(name);GetWindowRect(date.Handle,out var native);
            Check(name+"/native-date-height-preserved",Math.Abs(native.Bottom-native.Top-date.PreferredHeight)<=2);
        }
        var sort=Find("SortModeTB");var reference=Find("SortObjectReference");
        var mode=ProfileBounds(editor,sort);var refer=ProfileBounds(editor,reference);
        var left=ProfileBounds(editor,Find("LogicalNameTB"));var right=ProfileBounds(editor,Find("CapturePeriodTB"));
        Check("sort-mode-left-rail",Math.Abs(mode.X-left.X)<=2);
        bool paired=Math.Abs(left.Y-right.Y)<=2;
        if(paired)
        {
            Check("sort-object-right-rail",Math.Abs(refer.X-right.X)<=2,"Reference="+refer+"; right metadata="+right);
            Check("sort-object-normal-field-width",Math.Abs(reference.Width-Find("CapturePeriodTB").Width)<=2,"Reference="+reference.Width+"; metadata="+Find("CapturePeriodTB").Width);
            Check("sort-mode-and-object-one-row",Math.Abs(mode.Y-refer.Y)<=2,"Mode="+mode+"; reference="+refer);
            Check("sort-controls-not-overlapping",mode.Right<=refer.X);
        }
        else
        {
            Check("narrow-sort-object-bounded",reference.Width<=S(EditorLayoutPolicy.FieldWidth)&&reference.Width>=S(100));
            Check("narrow-sort-controls-not-overlapping",mode.Right<=refer.X||refer.Right<=mode.X||mode.Bottom<=refer.Y||refer.Bottom<=mode.Y);
        }
        var reading=(TableLayoutPanel)Find("StartEntry").Parent!;
        var startBounds=ProfileBounds(editor,Find("StartEntry"));var countBounds=ProfileBounds(editor,Find("EndEntry"));
        if(reading.GetRow(Find("StartEntry"))==reading.GetRow(Find("EndEntry")))
        {
            Check("reading-left-peer-rail",Math.Abs(startBounds.X-left.X)<=2&&Math.Abs(Find("StartEntry").Width-Find("LogicalNameTB").Width)<=2);
            Check("reading-right-peer-rail",Math.Abs(countBounds.X-right.X)<=2&&Math.Abs(Find("EndEntry").Width-Find("CapturePeriodTB").Width)<=2,"Reading="+countBounds+"; metadata="+right);
            var periodCaption=Find("CapturePeriodTB").Parent!.Controls.OfType<Label>().Single(c=>c.Text=="Period");
            Check("reading-count-caption-rail",Math.Abs(ProfileBounds(editor,Find("label2")).X-ProfileBounds(editor,periodCaption).X)<=2);
            Check("reading-to-caption-rail",Math.Abs(ProfileBounds(editor,Find("ToLbl")).X-ProfileBounds(editor,periodCaption).X)<=2);
        }
        else Check("narrow-reading-stacks-consistently",Math.Abs(startBounds.X-countBounds.X)<=2&&Math.Abs(Find("StartEntry").Width-Find("EndEntry").Width)<=2);
        var identity=Descendants(reference).OfType<TextBox>().Single(c=>c.Name=="ReferenceIdentity");
        var details=Descendants(reference).OfType<TextBox>().Single(c=>c.Name=="ReferenceDetails");
        var expected=profile.SortObject;
        Check("sort-reference-content",expected==null?identity.Text=="No sort object":identity.Text.Contains(expected.LogicalName)&&identity.Text.Contains(expected.Description??""));
        Check("sort-reference-details",expected==null?!details.Visible:details.Visible&&details.Text==$"Attribute: {profile.SortAttributeIndex} · Data index: {profile.SortDataIndex}");
        foreach(var box in new[]{identity,details}.Where(c=>c.Visible))
        {
            Check(box.Name+"/native-readonly-copyable",box.ReadOnly&&box.Enabled&&box.ShortcutsEnabled&&box.TabStop);
            box.SelectAll();Check(box.Name+"/complete-text-selectable",box.SelectedText==box.Text);box.Select(0,0);
            Point last=box.GetPositionFromCharIndex(Math.Max(0,box.TextLength-1));
            Check(box.Name+"/wrapped-text-readable",last.Y+box.Font.Height<=box.ClientSize.Height+2,"Last="+last+"; client="+box.ClientSize+"; font="+box.Font.Height);
        }
        var viewport=(ScrollableControl)reference;
        Check("reference-viewport-bounded",reference.Height<=S(120)+2);
        Check("reference-has-no-horizontal-scrollbar",!viewport.HorizontalScroll.Visible);
        if(result.State=="long-profile-reference")
        {
            viewport.AutoScrollPosition=Point.Empty;Pump();
            if(step is 0 or 2)CaptureAtPosition(editor,result,output,requested,"profile-reference-top-step"+step);
            viewport.PerformLayout();Pump(); viewport.AutoScrollPosition=new Point(0,viewport.VerticalScroll.Maximum);viewport.PerformLayout();Pump();
            Check("reference-details-scroll-reachable",ProfileFull(details),"Position="+viewport.AutoScrollPosition+"; detail="+details.Bounds+"; viewport="+viewport.ClientSize);
            Check("reference-only-vertical-scroll",!viewport.HorizontalScroll.Visible&&viewport.VerticalScroll.Visible);
            if(step is 0 or 2)CaptureAtPosition(editor,result,output,requested,"profile-reference-bottom-step"+step);
            viewport.AutoScrollPosition=Point.Empty;Pump();
        }
        var reset=(GXButton)Find("ResetBtn");var capture=(GXButton)Find("CaptureBtn");
        var a=ProfileBounds(editor,reset);var b=ProfileBounds(editor,capture);
        Check("original-method-indices",reset.Index==1&&capture.Index==2);
        Check("actions-vertical",Math.Abs(a.X-b.X)<=1&&Math.Abs(a.Right-b.Right)<=1&&a.Bottom<=b.Y,"Reset="+a+"; Capture="+b);
        Check("actions-equal-size",reset.Size==capture.Size);
        Check("actions120logical-width",Math.Abs(reset.Width-S(120))<=2);
        Check("actions-standard-height",Math.Abs(reset.Height-S(32))<=2&&Math.Abs(capture.Height-S(32))<=2);
        Check("actions8logical-gap",Math.Abs(b.Y-a.Bottom-S(8))<=2);
        Check("actions-on-right",a.X>left.X&&a.Right<=editor.ClientSize.Width-S(8)+2&&editor.ClientSize.Width-a.Right<=S(32),"Reset="+a+"; editor="+editor.ClientSize);
        var tabs=Descendants(editor).OfType<TabControl>().First();
        Check("actions-above-data",b.Bottom<=ProfileBounds(editor,tabs).Y+2);
        Check("actions-visible-at-scroll-top",ProfileFull(reset)&&ProfileFull(capture));
        foreach(var control in originals.Values.Where(c=>c.Visible))
        {
            var now=(control.Font.FontFamily.Name,control.Font.SizeInPoints,control.ForeColor.ToArgb(),control.BackColor.ToArgb());
            if(!ProfileHeaderAppearance.TryGetValue(editor,out var appearance))ProfileHeaderAppearance[editor]=appearance=new();
            if(appearance.TryGetValue(control,out var before))Check(control.Name+"/font-colors-stable",before==now);
            else appearance[control]=now;
        }
        Rectangle Rect(Control c){var b=ProfileBounds(editor,c);return new(b.X,b.Y,c.Width,c.Height);}
        if(step==0)ProfileHeaderGeometry[editor]=originals.Values.Where(c=>c.Visible).ToDictionary(c=>c,Rect);
        if(step==3)Check("wide-narrow-wide-restores",ProfileHeaderGeometry[editor].All(p=>Math.Abs(Rect(p.Key).X-p.Value.X)<=2&&Math.Abs(Rect(p.Key).Y-p.Value.Y)<=2&&Math.Abs(Rect(p.Key).Width-p.Value.Width)<=2&&Math.Abs(Rect(p.Key).Height-p.Value.Height)<=2));
        bool italy=result.State=="italy-profile";
        Check("italy-action-visibility",Find("AddWhiteBtn").Visible==italy&&Find("AddBlackBtn").Visible==italy&&!Find("RemoveBtn").Visible);
        var settled=originals.Values.ToDictionary(c=>c,c=>c.Bounds);
        for(int repeat=0;repeat<4;repeat++){editor.PerformLayout();reference.Parent!.PerformLayout();Pump();}
        Check("layout-settles-without-accumulation",settled.All(p=>p.Key.Bounds==p.Value));
    }
    static void CheckProfileHeaderInteractions(Form editor,GXDLMSProfileGeneric profile,GXDLMSClient client,CaseResult result)
    {
        void Check(string name,bool pass,string detail="")=>result.BehaviorChecks.Add(new(){Name="ProfileHeader/"+result.State+"/"+name,Passed=pass,Detail=detail});
        Control Find(string name)=>Descendants(editor).Single(c=>c.Name==name);
        var entry=(RadioButton)Find("ReadEntryBtn");var last=(RadioButton)Find("ReadLastRB");var date=(RadioButton)Find("ReadFromRB");var all=(RadioButton)Find("ReadAllRB");
        var start=(NumericUpDown)Find("StartEntry");var count=(NumericUpDown)Find("EndEntry");var days=(NumericUpDown)Find("ReadLastTB");var from=(DateTimePicker)Find("StartPick");var to=(DateTimePicker)Find("ToPick");
        entry.Checked=true;start.Value=2;count.Value=20;Pump();Check("entry-read-mode",profile.AccessSelector.ToString()=="Entry"&&Convert.ToDecimal(profile.From)==2&&Convert.ToDecimal(profile.To)==20&&!start.ReadOnly&&!count.ReadOnly);
        last.Checked=true;days.Value=3;Pump();Check("last-read-mode",profile.AccessSelector.ToString()=="Last"&&!days.ReadOnly&&start.ReadOnly&&count.ReadOnly);
        date.Checked=true;from.Checked=to.Checked=true;from.Value=SampleTime.Date;to.Value=SampleTime.Date.AddDays(1);Pump();Check("date-read-mode",profile.AccessSelector.ToString()=="Range"&&from.Enabled&&to.Enabled&&(DateTime)profile.From==from.Value&&(DateTime)profile.To==to.Value);
        all.Checked=true;Pump();Check("all-read-mode",profile.AccessSelector.ToString()=="All"&&new[]{entry,last,date,all}.Count(c=>c.Checked)==1);
        var original=profile.SortObject;int attr=profile.SortAttributeIndex,index=profile.SortDataIndex;
        profile.SortObject=null;((IGXDLMSView)editor).OnValueChanged(new(){Client=client,Connected=true,Index=6,Value=null});Pump();
        Check("empty-reference-refresh",Descendants(editor).OfType<TextBox>().Single(c=>c.Name=="ReferenceIdentity").Text=="No sort object");
        var viewport=(ScrollableControl)Find("SortObjectReference");
        Check("empty-reference-resets-scroll",viewport.AutoScrollPosition==Point.Empty&&!viewport.VerticalScroll.Visible&&!viewport.HorizontalScroll.Visible);
        profile.SortObject=original;profile.SortAttributeIndex=attr;profile.SortDataIndex=index;
        ((IGXDLMSView)editor).OnValueChanged(new(){Client=client,Connected=true,Index=6,Value=original});Pump();
        Check("reference-identity-preserved",ReferenceEquals(profile.SortObject,original)&&profile.SortAttributeIndex==attr&&profile.SortDataIndex==index);
        result.FixtureNotes.Add("Profile header checks do not invoke Reset/Capture, Capture Object editing, Italy actions or transport. In-memory settings remain active for constructor/read-mode handlers.");
    }
    static void CheckProfileHeaderReachability(CaseResult result)
    {
        foreach(int width in result.Captures.Select(c=>c.Width).Distinct())
        foreach(string name in ProfileHeaderInputs.Where(n=>!new[]{"AddWhiteBtn","AddBlackBtn","RemoveBtn"}.Contains(n)))
        {
            var m=result.Captures.Where(c=>c.Width==width).SelectMany(c=>c.LayoutMetrics).Where(c=>c.Name==name).ToArray();
            result.BehaviorChecks.Add(new(){Name="ProfileHeader/"+result.State+"/"+width+"/"+name+"/reachable",Passed=m.Any(c=>c.VisibleWidth+4>=c.ClientWidth&&c.VisibleHeight+4>=c.ClientHeight),Detail="Normal top header/actions fit; tall references and data pages may use existing outer scrolling in short viewports."});
        }
    }
}
