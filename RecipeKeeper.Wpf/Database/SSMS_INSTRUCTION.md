# Как открыть базу RecipeKeeper в SQL Server Management Studio

1. Открой `SQL Server Management Studio`.
2. Подключись к своему SQL Server.
3. Нажми `File -> Open -> File`.
4. Выбери файл:

```text
D:\Users\Admin\Documents\New project\RecipeKeeper.Wpf\Database\RecipeKeeper.sqlserver.sql
```

5. Нажми `Execute` / `Выполнить`.
6. В Object Explorer появится база:

```text
RecipeKeeperDb
```

7. Открой:

```text
Databases -> RecipeKeeperDb -> Tables
```

Там будут таблицы:

- `Users`
- `Categories`
- `Recipes`
- `Ingredients`
- `Products`
- `Favorites`
- `RecipeViews`
- `MealPlan`
- `ShoppingItems`
- `RecipeStats`

Важно: WPF-приложение пока продолжает работать через SQLite, чтобы не ломать текущий запуск.
Этот SQL Server скрипт нужен для просмотра и демонстрации базы в SSMS.
