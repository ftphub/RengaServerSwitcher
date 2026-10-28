# Публикация на GitHub: пошагово

Подготовлено по тому же принципу, что и публикация Pilot BCF Exchange: чистый исходный код, README RU/EN, документация, CHANGELOG, issue templates, отдельный Release.

## Рекомендуемые данные репозитория

- **Repository name:** `RengaServerSwitcher`
- **Description:** `Server profiles, safe switching and offline mode for Renga Professional 9.3 Collaboration Server.`
- **Visibility:** `Public` (если вы решили публиковать исходники)
- **Topics:** `renga`, `renga-bim`, `renga-professional`, `collaboration`, `server-switcher`, `wpf`, `csharp`, `plugin`
- **License:** не выбирайте автоматически MIT/GPL; в проекте уже лежит `LICENSE` с `All rights reserved`.

## Публикация файлов

1. Создайте **пустой** репозиторий на GitHub без дополнительных README, .gitignore и лицензии (они уже есть).
2. Распакуйте `RengaServerSwitcher_GitHub_Repository_v1.1.2.zip` на компьютере.
3. Загрузите **содержимое папки репозитория**, а не ZIP как один файл:
   - GitHub → **Add file → Upload files**, перетащите папки/файлы с сохранением структуры, затем подтвердите commit;
   - либо используйте **GitHub Desktop**, что удобнее для публикации всех вложенных каталогов.
4. Убедитесь, что в корне отображаются `README.md`, `README.en.md`, `LICENSE`, `CHANGELOG.md`, `src/`, `docs/` и `.github/`.
5. Проверьте, что в репозитории **нет** `lib/*.dll`, `lib/*.tlb`, `profiles.json`, `settings.ini`, логов и дампов.

## Пример публикации через Git CLI (для пустого репозитория)

```powershell
git init
git branch -M main
git add .
git commit -m "Initial release: Renga Server Switcher v1.1.2"
git remote add origin https://github.com/YOUR_ACCOUNT/RengaServerSwitcher.git
git push -u origin main
```

`YOUR_ACCOUNT` замените на свой GitHub username. Не копируйте пример как реальный URL без замены.

## Release

1. Откройте **Releases → Draft a new release**.
2. Создайте tag: `v1.1.2`.
3. Title: `Renga Server Switcher v1.1.2`.
4. Скопируйте содержимое `docs/RELEASE_NOTES_v1.1.2.md` в описание.
5. Если есть **скомпилированный и проверенный бинарный пакет**, приложите его как Release asset. Он не входит в исходный ZIP; проверьте права на распространение SDK runtime DLL.
6. Опубликуйте Release.

> GitHub автоматически создаёт исходные архивы из тегов. Они **не являются готовыми DLL**.

## Что НЕ загружать

- `lib/Renga.NET8.PluginUtility.dll`, `lib/RengaCOMAPI.tlb` и другие SDK-бинарники без отдельного разрешения.
- `_build/`, `bin/`, `obj/`, `.vs/`, `*.pdb`.
- `%LOCALAPPDATA%/RengaServerSwitcher/` и `settings.ini` Renga.
- Реальные профили, IP/DNS, логины, ключи, журналы, crashdump.
- Скриншоты с внутренними IP, пользователями, ключами или проектами заказчиков без очистки.

## Картинки и презентация

Отдельные скриншоты можно добавить в `docs/screenshots/` **после удаления** конфиденциальных данных. Исходный DOCX с реальными тестовыми профилями и скриншотами не включён намеренно.
