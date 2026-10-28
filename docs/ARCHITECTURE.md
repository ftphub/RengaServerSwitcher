# Архитектура

```text
Renga Professional
  └─ RengaServerSwitcher.Plugin.dll (WPF/.NET 8)
       ├─ UI: профили, статусы, параметры, подтверждение
       ├─ ProfileRepository / DPAPI: локальные профили
       ├─ ServerProbeService: TCP-проверка
       └─ RestartCoordinator: подготовка запроса и закрытие Renga
             └─ RengaServerSwitcher.RestartHelper.exe
                  ├─ ждёт выхода старой Renga
                  ├─ делает резервную копию и обновляет settings.ini
                  ├─ обновляет marker локального режима
                  └─ запускает новую Renga через explorer.exe parent
```

## Компоненты

- **Plugin:** Renga API, WPF-окна, валидация и сохранение профилей.
- **RestartHelper:** внешний `.exe`, который работает после выгрузки DLL Renga.
- **FreshProcessLauncher:** Win32-стратегия перезапуска с `explorer.exe` в качестве родительского процесса. Схема была подобрана после воспроизводимых падений нативного Collaboration Core при обычном запуске из helper.
- **IniFileService:** меняет параметры `[Collaboration]` после завершения старой Renga, создаёт копию `settings.ini.rss.bak`.

## Состояния

| Состояние | Отображение |
|---|---|
| Renga сообщает об установленном соединении | Подключено |
| Соединения нет; выбран автономный режим | Автономная работа |
| TCP-проверка прошла/не прошла | Доступен/Недоступен (только сетевой уровень) |

Для локальной работы используется недоступный loopback-порт и marker-файл. Это практический обход для проверенной конфигурации, а не официальный механизм отключения Collaboration через SDK.
