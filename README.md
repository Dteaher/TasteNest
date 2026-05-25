# TasteNest

TasteNest — WPF-приложение на C# для поиска рецептов, ведения продуктов, избранного, плана питания, списка покупок и пользовательской статистики.

## Стек

- C# / WPF / .NET 8
- SQL Server Express / SQL Server
- Microsoft.Data.SqlClient

## Структура проекта

```text
RecipeKeeper.Wpf/
├─ Assets/          изображения, иконки и фото рецептов
├─ Configuration/   пример локальных настроек подключения
├─ Data/            модели, авторизация, работа с SQL Server
├─ Database/        SQL-скрипты и инструкции для БД
├─ Deployment/      материалы для серверной настройки
├─ Pages/           основные страницы приложения
├─ Ui/              общие UI-поведения
└─ Views/           пользовательские экраны и карточки
```

## Запуск из Visual Studio

1. Откройте `RecipeKeeper.Wpf.slnx`.
2. Выберите проект `RecipeKeeper.Wpf`.
3. Убедитесь, что установлен workload Visual Studio `.NET desktop development`.
4. Запустите проект кнопкой `Start`.

## Запуск через терминал

```powershell
dotnet build .\RecipeKeeper.Wpf\RecipeKeeper.Wpf.csproj
dotnet run --project .\RecipeKeeper.Wpf\RecipeKeeper.Wpf.csproj
```

## Настройки базы данных

Реальные настройки подключения не хранятся в репозитории.

Для локального запуска используйте пример:

```text
RecipeKeeper.Wpf/Configuration/TasteNest.settings.example.json
```

При необходимости скопируйте его в выходную папку приложения как:

```text
TasteNest.settings.json
```

Для клиентской сборки используйте пример:

```text
RecipeKeeper.Wpf/Deployment/TasteNest.client.settings.example.json
```

## Стартовые аккаунты

При создании базы приложение добавляет:

- `admin / admin` — администратор
- `operator / operator` — оператор
- `user / user` — пользователь

## Что не хранится в проекте

- `.vs`, `bin`, `obj`
- опубликованные сборки вроде `TasteNest_User_Version`
- реальные `TasteNest.settings.json`
- локальные базы данных и временные файлы
