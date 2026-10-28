namespace RengaServerSwitcher;

internal sealed class ProfileRepository
{
    private readonly Logger _log;
    public ProfileRepository(Logger log) => _log = log;

    public List<ServerProfile> LoadAll()
    {
        var stored = JsonStore.Load(AppPaths.ProfilesFile, new List<StoredServerProfile>());
        var result = new List<ServerProfile>();
        foreach (var p in stored)
        {
            var key = string.Empty;
            try { key = DpapiProtector.Unprotect(p.AccessKeyProtected); }
            catch (Exception ex) { _log.Error($"Cannot decrypt access key for profile '{p.Name}'.", ex); }

            result.Add(new ServerProfile
            {
                Id = p.Id == Guid.Empty ? Guid.NewGuid() : p.Id,
                Name = p.Name,
                Host = p.Host,
                Port = p.Port <= 0 ? 8820 : p.Port,
                UserName = p.UserName,
                AccessKey = key,
                RealtimeCollaboration = p.RealtimeCollaboration
            });
        }
        return result.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public void Save(ServerProfile profile)
    {
        Validate(profile);
        var all = LoadAll();
        var duplicate = all.Any(x => x.Id != profile.Id &&
            (string.Equals(x.Name.Trim(), profile.Name.Trim(), StringComparison.OrdinalIgnoreCase) ||
             (string.Equals(x.Host.Trim(), profile.Host.Trim(), StringComparison.OrdinalIgnoreCase) &&
              x.Port == profile.Port &&
              string.Equals(x.UserName.Trim(), profile.UserName.Trim(), StringComparison.OrdinalIgnoreCase))));
        if (duplicate)
            throw new InvalidOperationException("Профиль с таким названием или той же комбинацией сервер/порт/пользователь уже существует.");

        var index = all.FindIndex(x => x.Id == profile.Id);
        if (index >= 0) all[index] = profile.Clone(); else all.Add(profile.Clone());
        SaveAll(all);
        _log.Info($"Profile saved: {profile.Name} ({profile.Host}:{profile.Port}, user={profile.UserName}).");
    }

    public void Delete(Guid id)
    {
        var all = LoadAll();
        var profile = all.FirstOrDefault(x => x.Id == id);
        all.RemoveAll(x => x.Id == id);
        SaveAll(all);
        if (profile != null) _log.Info($"Profile deleted: {profile.Name}.");
    }

    private static void Validate(ServerProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name)) throw new InvalidOperationException("Укажите название профиля.");
        if (string.IsNullOrWhiteSpace(profile.Host)) throw new InvalidOperationException("Укажите IP-адрес или DNS-имя сервера.");
        if (profile.Port is < 1 or > 65535) throw new InvalidOperationException("Порт должен быть от 1 до 65535.");
        if (string.IsNullOrWhiteSpace(profile.UserName)) throw new InvalidOperationException("Укажите имя пользователя.");
        foreach (var value in new[] { profile.Name, profile.Host, profile.UserName, profile.AccessKey })
            if (value.Contains('\r') || value.Contains('\n')) throw new InvalidOperationException("Поля не должны содержать перевод строки.");
    }

    private static void SaveAll(IEnumerable<ServerProfile> all)
    {
        var stored = all.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).Select(p => new StoredServerProfile
        {
            Id = p.Id,
            Name = p.Name.Trim(),
            Host = p.Host.Trim(),
            Port = p.Port,
            UserName = p.UserName.Trim(),
            AccessKeyProtected = DpapiProtector.Protect(p.AccessKey),
            RealtimeCollaboration = p.RealtimeCollaboration
        }).ToList();
        JsonStore.Save(AppPaths.ProfilesFile, stored);
    }
}
