# Как открыть базу TasteNest в SQL Server Management Studio

1. Открой `SQL Server Management Studio`.
2. Подключись к своему SQL Server, например:

```text
localhost\SQLEXPRESS
```

3. В `Object Explorer` открой:

```text
Databases -> RecipeKeeperDb -> Tables
```

4. Чтобы посмотреть данные таблицы:
   - нажми правой кнопкой по таблице;
   - выбери `Select Top 1000 Rows` / `Выбрать первые 1000 строк`.

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

Если базу нужно создать заново через SSMS:

1. Нажми `File -> Open -> File`.
2. Выбери файл:

```text
D:\Users\Admin\Documents\New project\RecipeKeeper.Wpf\Database\RecipeKeeper.sqlserver.sql
```

3. Нажми `Execute` / `Выполнить`.
4. Обнови список баз данных.

WPF-приложение работает с этой же SQL Server базой через `Microsoft.Data.SqlClient`.
