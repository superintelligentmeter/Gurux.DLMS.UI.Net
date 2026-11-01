// superintelligentmeter contributors, 2026-10-30. Offline settings safeguard. GPL-2.0-only.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;

internal sealed class SettingsWriteGuard : IDisposable
{
    readonly List<(ApplicationSettingsBase Settings, Dictionary<string, object> Values)> originals = new();
    internal int SaveAttempts { get; private set; }
    internal SettingsWriteGuard(params Assembly[] assemblies)
    {
        foreach (var type in assemblies.Distinct().SelectMany(assembly => assembly.GetTypes()).Where(type =>
            typeof(ApplicationSettingsBase).IsAssignableFrom(type) && type.FullName.EndsWith(".Properties.Settings", StringComparison.Ordinal)))
        {
            var settings = (ApplicationSettingsBase)type.GetProperty("Default", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var values = settings.Properties.Cast<SettingsProperty>().ToDictionary(property => property.Name, property => settings[property.Name]);
            settings.SettingsSaving += CancelSave;
            originals.Add((settings, values));
        }
    }
    void CancelSave(object sender, CancelEventArgs args) { args.Cancel = true; ++SaveAttempts; }
    public void Dispose()
    {
        foreach (var (settings, values) in originals)
        {
            foreach (var value in values) settings[value.Key] = value.Value;
            settings.SettingsSaving -= CancelSave;
        }
        originals.Clear();
    }
}
