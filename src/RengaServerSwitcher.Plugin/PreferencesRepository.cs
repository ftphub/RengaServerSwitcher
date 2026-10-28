namespace RengaServerSwitcher;

internal sealed class PreferencesRepository
{
    public PluginPreferences Load() => JsonStore.Load(AppPaths.PreferencesFile, new PluginPreferences());
    public void Save(PluginPreferences preferences) => JsonStore.Save(AppPaths.PreferencesFile, preferences);
}
