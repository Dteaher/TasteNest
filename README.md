# TasteNest

TasteNest — настольное WPF-приложение на C# для рецептов, продуктов, избранного, планирования питания, списка покупок и статистики пользователя.

## Стек

- C# / WPF / .NET 8
- SQL Server / SQL Server Express
- SQL Server Management Studio
- Microsoft.Data.SqlClient
- T-SQL
- Git / GitHub

## Структура проекта

```text
RecipeKeeper.Wpf/
├─ Assets/          изображения, иконки и фото рецептов
├─ Configuration/   пример локальных настроек подключения
├─ Data/            модели, авторизация, работа с SQL Server
├─ Database/        SQL Server скрипты и инструкции для базы
├─ Deployment/      файлы для настройки серверной и клиентской версии
├─ Pages/           основные страницы приложения
├─ Ui/              общие UI-поведения
└─ Views/           пользовательские экраны и карточки
```

## Запуск из Visual Studio

1. Открой `RecipeKeeper.Wpf.slnx`.
2. Выбери проект `RecipeKeeper.Wpf`.
3. Убедись, что установлен workload Visual Studio `.NET desktop development`.
4. Запусти проект кнопкой `Start`.

## Запуск через терминал

```powershell
dotnet build .\RecipeKeeper.Wpf\RecipeKeeper.Wpf.csproj
dotnet run --project .\RecipeKeeper.Wpf\RecipeKeeper.Wpf.csproj
```

## База данных

Текущая версия использует SQL Server базу:

```text
RecipeKeeperDb
```

Основной скрипт для SQL Server Management Studio:

```text
RecipeKeeper.Wpf\Database\RecipeKeeper.sqlserver.sql
```

Настройки подключения лежат рядом с exe в файлах:

```text
TasteNest.settings.json
TasteNest.client.settings.json
```

Примеры настроек:

```text
RecipeKeeper.Wpf\Configuration\TasteNest.settings.example.json
RecipeKeeper.Wpf\Deployment\TasteNest.client.settings.example.json
```

## Стартовые аккаунты

При создании тестовой базы приложение добавляет:

- `admin / admin` — администратор
- `operator / operator` — оператор
- `user / user` — пользователь

## Что не хранится в репозитории

- `.vs`, `bin`, `obj`
- опубликованные сборки вроде `TasteNest_User_Version`
- реальные локальные `TasteNest.settings.json`
- временные файлы и локальные базы данных
