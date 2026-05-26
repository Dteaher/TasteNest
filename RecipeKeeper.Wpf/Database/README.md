# База данных TasteNest

Проект использует SQL Server / SQL Server Express. База открывается и просматривается через SQL Server Management Studio.

Основная база:

```text
RecipeKeeperDb
```

Основной SQL-скрипт для SSMS:

```text
RecipeKeeper.Wpf\Database\RecipeKeeper.sqlserver.sql
```

Подключение из WPF-приложения выполняется через:

```text
RecipeKeeper.Wpf\Data\DbConnectionFactory.cs
```

Пакет подключения:

```text
Microsoft.Data.SqlClient
```

Основные таблицы:

- `dbo.Users`
- `dbo.Categories`
- `dbo.Recipes`
- `dbo.Ingredients`
- `dbo.Products`
- `dbo.Favorites`
- `dbo.RecipeViews`
- `dbo.MealPlan`
- `dbo.ShoppingItems`
- `dbo.RecipeStats`
- `dbo.UserRecipeStats`
- `dbo.OperatorUserNotes`
- `dbo.OperatorActionLog`
