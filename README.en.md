# Renga Server Switcher

**Server profile manager for Renga Professional Collaboration Server.** Store connection profiles and switch between servers or a local working mode without repeatedly entering the host, port, username and access key.

**Version:** `1.1.2` · **Author:** `kartashov_ma` · **Tested on:** Renga Professional 9.3.32980.0, API 2.50, Windows x64

[Русский README](README.md) · [Installation](docs/INSTALL.md) · [User guide (RU)](docs/USER_GUIDE.md) · [Changelog](CHANGELOG.md)

## Features

- Store multiple connection profiles (name, IP/DNS, port, username, server access key).
- Configure automatic change synchronization independently per profile.
- Add, edit and delete profiles; detect duplicates.
- Indicate the active profile and Renga's connection status.
- Test TCP reachability of a server endpoint.
- Switch to local/offline operation without losing saved profiles.
- Save changes before restarting Renga and automatically relaunch it using an external helper.
- Encrypt access keys **in the plugin's profile store** via Windows DPAPI.
- Optionally show the switcher window on Renga startup; keep operation logs.

## Requirements

Tested on **Renga Professional 9.3.32980.0**, **Renga API 2.50**, **Windows x64**, and **.NET 8**. Other versions have not been validated.

The source repository excludes Renga SDK binary libraries. To build, use **Visual Studio 2022**, **.NET 8 SDK**, and the official **Renga SDK 9.3**. See [installation](docs/INSTALL.md) and [development](docs/DEVELOPMENT.md).

## Installation

Copy the complete compiled plugin output into:

```text
C:\Program Files\Renga Professional\Plugins\RengaServerSwitcher\
```

Restart Renga and enable the plugin under **Plug-ins** if necessary.

## How to use

1. Open **Renga Server Switcher** from the Renga toolbar.
2. Select a saved server and click **Connect**, or choose **Work locally**.
3. Confirm the restart and save any unsaved project changes when prompted.

## Important notes

- **DPAPI only protects the plugin profile store.** The Renga application itself can write the active server access key to its `settings.ini` file and backup.
- Local mode is implemented by using an unreachable loopback endpoint, not an official Renga offline API switch.
- Successful TCP testing does not verify the access key or authorization.
- The automated relaunch uses a separate helper and a Windows Explorer-parent process strategy verified on the stated test environment.

## License

Copyright © 2026 **kartashov_ma**. **All rights reserved.** See [LICENSE](LICENSE) and [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md).
