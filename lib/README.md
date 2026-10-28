# Локальные библиотеки SDK (не публикуются)

Для сборки проекта здесь должны находиться:

```text
lib/RengaCOMAPI.tlb
lib/Renga.NET8.PluginUtility.dll
```

Эти файлы не включены в репозиторий. Возьмите их из официального Renga SDK 9.3 (архив разработчика) и выполните:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\prepare-sdk.ps1 -SdkZip "C:\path\to\93 RengaSDK.zip"
```

Скрипт извлечёт **только** `tlb/RengaCOMAPI.tlb` и `Net/Renga.NET8.PluginUtility.dll`. Не добавляйте их в Git.
