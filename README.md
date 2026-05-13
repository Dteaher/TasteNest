# TasteNest

TasteNest — WPF-приложение на C# для рецептов, продуктов, избранного, плана питания, списка покупок и администрирования пользователей.

## Стек

- C# / WPF / .NET 8
- SQL Server Express
- Microsoft.Data.SqlClient

## Запуск из Visual Studio

1. Откройте `RecipeKeeper.Wpf.slnx`.
2. Выберите проект `RecipeKeeper.Wpf`.
3. Убедитесь, что установлен workload Visual Studio: `.NET desktop development`.
4. Запустите проект кнопкой `Start`.

## Настройки базы данных

Реальные настройки подключения не хранятся в Git.

Для локального запуска можно скопировать:

```text
RecipeKeeper.Wpf/Configuration/TasteNest.settings.example.json
```

в выходную папку приложения как:

```text
TasteNest.settings.json
```

Для клиентской сборки используйте пример:

```text
RecipeKeeper.Wpf/Deployment/TasteNest.client.settings.example.json
```

и укажите адрес SQL Server, логин и пароль.

## Стартовые аккаунты

При создании базы приложение добавляет:

- `admin / admin` — администратор
- `operator / operator` — оператор
- `user / user` — пользователь

## Что не хранится в репозитории

- `.vs`, `bin`, `obj`
- опубликованная пользовательская папка `TasteNest_User_Version`
- реальные `TasteNest.settings.json`
- временные файлы и материалы генерации
