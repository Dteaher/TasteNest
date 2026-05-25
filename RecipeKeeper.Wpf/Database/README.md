# База данных RecipeKeeper

Приложение использует SQLite как SQL-базу без установки отдельного сервера.

Файл базы создаётся при запуске:

```text
RecipeKeeper.Wpf\bin\Debug\net8.0-windows\recipes_wpf.db
```

Строка подключения вынесена в:

```text
Data\DbConnectionFactory.cs
```

Класс подключения по аналогии с методичкой:

```text
Data\ConnectObject.cs
```

SQL-структура базы отдельно описана в:

```text
Database\RecipeKeeper.sqlite.sql
```
