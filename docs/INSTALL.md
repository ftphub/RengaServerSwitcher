# Установка Renga Server Switcher

## Требования

- Renga Professional 9.3 (проверено: `9.3.32980.0`).
- Windows x64, .NET 8.
- Для сборки: Visual Studio 2022, компоненты разработки .NET Desktop, официальный Renga SDK 9.3.

## Вариант A. Готовый бинарный комплект

1. Полностью завершите Renga.
2. Распакуйте **проверенный** архив выпуска, если он опубликован в GitHub Releases.
3. Скопируйте **все файлы** из папки сборки плагина в:

   ```text
   C:\Program Files\Renga Professional\Plugins\RengaServerSwitcher\
   ```

4. Запустите Renga, проверьте модуль в **Настройки → Подключаемые модули** и кнопку на панели.
5. Если установка производится в `Program Files`, могут потребоваться права администратора.

В папке должны присутствовать как минимум `RengaServerSwitcher.Plugin.dll`, `RengaServerSwitcher.rndesc`, `RengaServerSwitcher.RestartHelper.exe` и зависимости сборки.

## Вариант B. Сборка из исходников

1. Получите официальный архив **Renga SDK 9.3**.
2. Положите его вне репозитория и выполните в PowerShell из корня проекта:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\prepare-sdk.ps1 -SdkZip "C:\SDK\93 RengaSDK.zip"
   ```

   Скрипт положит в локальный `lib/` две библиотеки, необходимые проекту. Они не добавляются в Git.

3. Откройте `RengaServerSwitcher.sln` в Visual Studio 2022.
4. Выберите **Release | x64** и выполните **Build → Build Solution**.
5. Результат находится в `_build\RengaServerSwitcher\`.
6. Закройте Renga и скопируйте весь результат в каталог `Plugins\RengaServerSwitcher\`, либо запустите `install.cmd` с нужными правами.

**Замечание:** проект содержит COM-ссылку на `RengaCOMAPI.tlb`; для надёжной сборки используйте Visual Studio/MSBuild под Windows, а не рассчитывайте на универсальную сборку через `dotnet` в другой ОС.

## Первый запуск и проверка

1. Откройте Renga Server Switcher.
2. Создайте тестовый профиль на доступном сервере.
3. Проверьте TCP-доступность и выполните подключение.
4. Проверьте смену пользователя / сервера после перезапуска.
5. Выполните переход в автономную работу и обратно.

При ошибке загрузки смотрите [Troubleshooting](TROUBLESHOOTING.md).
