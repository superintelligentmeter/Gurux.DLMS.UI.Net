# Gurux.DLMS.UI.Net — superintelligentmeter fork

An unofficial community fork of [Gurux.DLMS.UI.Net](https://github.com/Gurux/Gurux.DLMS.UI.Net), maintained by [superintelligentmeter](https://github.com/superintelligentmeter). This repository provides the Windows Forms COSEM object views and shared UI components used by the companion [GXDLMSDirector fork](https://github.com/superintelligentmeter/GXDLMSDirector).

This is **not an official Gurux release** and does not imply Gurux endorsement. Original Gurux UI components and the DLMS/COSEM libraries are developed by Gurux Ltd; their copyright, attribution and license notices remain applicable.

License: **GPL-2.0-only**. The fork's contributions are distributed under the same license; original Gurux and third-party notices remain applicable. See [LICENSE](LICENSE), [MODIFICATIONS.txt](MODIFICATIONS.txt), [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

## Companion application preview

![GXDLMSDirector companion application with multi-meter measurement tiles and a voltage trend](ui.png)

*The GXDLMSDirector companion application, built from the paired forks, showing simulated readings. No physical meters are connected. The dashboard module belongs to Director; this UI library is not a separate dashboard application.*

## Build with GXDLMSDirector

Install the **.NET 10 SDK** on Windows, then clone the two forks into sibling directories:

```powershell
git clone https://github.com/superintelligentmeter/GXDLMSDirector.git
git clone https://github.com/superintelligentmeter/Gurux.DLMS.UI.Net.git
dotnet build GXDLMSDirector/Development/GXDLMSDirector46.csproj -c Release -t:Rebuild
```

Keep the directory names `GXDLMSDirector` and `Gurux.DLMS.UI.Net`; Director references this project. See the [Director dashboard guide](https://github.com/superintelligentmeter/GXDLMSDirector#use-dashboards) for dashboard usage, autosave and optional feature switches.

The protocol library remains the official Gurux.DLMS 9.0.2609.2201 NuGet package. This GPL dependency is not exempt from redistribution obligations merely because it is unmodified.

## Original Gurux resources and attribution

Gurux provides the original DLMS/COSEM components and object-view framework. The components support compatible electricity, gas and water meters; the UI library supplies views for displaying and editing COSEM objects.

- [Original Gurux.DLMS.UI.Net repository](https://github.com/Gurux/Gurux.DLMS.UI.Net)
- [Original GXDLMSDirector repository](https://github.com/Gurux/GXDLMSDirector)
- [Gurux website](https://www.gurux.fi/) and [Gurux.DLMS documentation](https://www.gurux.fi/index.php?q=Gurux.DLMS)
- [Gurux community updates](https://twitter.com/guruxorg)

Preserve the original Gurux and third-party notices. See [MODIFICATIONS.txt](MODIFICATIONS.txt) for fork changes. Binary distributions must provide complete corresponding source for the exact versions of both forks and covered dependencies, including unchanged GPL dependency DLLs, with build instructions and the applicable license notices.

## Original Gurux UI integration example

The following upstream usage example is retained for reference.

Gurux.DLMS.UI
=========================== 
Purpose of Gurux.DLMS.UI is help to create user interface for DLMS applications.
First you need to create Panel where UI is shown. In this example we name it as UIPanel.
 You can create DLMS views at the run time,
but usually it's better to create them all and update panel when user selects next COSEM object.
You can create all UI objects like this.

```csharp
//All UI objects are save on views dictionary.
Dictionary<Type, IGXDLMSView> views = new Dictionary<Type, IGXDLMSView>();

foreach (Type type in typeof(GXDLMSDataView).Assembly.GetTypes())
{
    GXDLMSViewAttribute[] att = (GXDLMSViewAttribute[])type.GetCustomAttributes(typeof(GXDLMSViewAttribute), true);
    if (!type.IsInterface && typeof(IGXDLMSView).IsAssignableFrom(type))
    {
        IGXDLMSView view = Activator.CreateInstance(type) as IGXDLMSView;
        Form f = view as Form;
        f.TopLevel = false;
        f.TopMost = false;
        f.FormBorderStyle = FormBorderStyle.None;
        f.Dock = DockStyle.Fill;
        f.Width = UIPanel.Width;
        f.Height = UIPanel.Height;
        views.Add(att[0].DLMSType, view);
    }
}

``` 
When we want to show UI for selected COSEM object we can do it like this:
```csharp
//Clear old view.
foreach (Control it in UIPanel.Controls)
{
    it.Hide();
}
UIPanel.Controls.Clear();
//Select new COSEM object. In this example we select clock.
GXDLMSClock clock = new GXDLMSClock();
IGXDLMSView selectedView = views[clock];
selectedView.Target = clock;
GXDlmsUi.UpdateProperties(clock, selectedView);
//Add DLMS COSEM view to UI panel.
UIPanel.Controls.Add((Form)selectedView);
Show UI.
((Form)selectedView).Show();

``` 