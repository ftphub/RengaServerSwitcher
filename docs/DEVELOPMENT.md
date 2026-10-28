# Разработка и сборка

## Исходные файлы

- `src/RengaServerSwitcher.Plugin/` — UI, модели, профили, интеграция с Renga.
- `src/RengaServerSwitcher.RestartHelper/` — запись настроек и перезапуск.
- `RengaServerSwitcher.sln` — Visual Studio solution.
- `lib/` — **только локальные** зависимости официального SDK; не коммитить.

## Подготовка SDK

1. Получите официальный SDK 9.3.
2. Выполните:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\prepare-sdk.ps1 -SdkZip "C:\SDK\93 RengaSDK.zip"
   ```

3. Проверьте файлы `lib/RengaCOMAPI.tlb` и `lib/Renga.NET8.PluginUtility.dll`.

## Сборка

Откройте `RengaServerSwitcher.sln` в VS 2022, выберите `Release | x64`, нажмите **Build Solution**. На некоторых машинах `dotnet build` не заменяет Visual Studio/MSBuild при обработке COM-reference.

Командный сценарий `build.cmd` ищет MSBuild установленной Visual Studio.

## Контроль качества перед выпуском

1. Проверить, что выходят оба проекта (`Plugin`, `RestartHelper`).
2. Убедиться, что `.rndesc`, `.png`, `.exe`, `.deps.json` и необходимые зависимости скопированы в `_build/`.
3. Установить в отдельную Renga 9.3.
4. Проверить старт, UI, создание профиля, TCP, подключение, переход в автономный режим и обратно.
5. Проверить запрос сохранения при несохранённом проекте.
6. Не публиковать `settings.ini`, профили, журналы, дампы и SDK DLL без разрешения правообладателя.

## Публикация сборки

Скрипт `scripts/package-release.ps1` упаковывает результат сборки для GitHub Releases, но **требует явного подтверждения** наличия прав на распространение сторонних runtime DLL. Скрипт не заменяет проверки лицензирования и тестирования на целевой Renga.

## Важно для изменений

Механизм запуска через `explorer.exe` и автономного loopback endpoint прошёл ручные испытания. Менять `FreshProcessLauncher.cs`, `RestartCoordinator.cs`, `IniFileService.cs` или `RestartHelper/Program.cs` рекомендуется только с отдельным регрессионным тестированием.
