# TasteNest: сервер SQL на одном компьютере

## Идея

- Компьютер администратора хранит SQL Server Express и базу `RecipeKeeperDb`.
- Пользователи запускают `TasteNest.exe` и подключаются к серверу администратора.
- Пользователям не нужно устанавливать SQL Server или SSMS.

## Настройка сервера

Запустите PowerShell от имени администратора:

```powershell
.\Setup-TasteNestServer.ps1
```

Скрипт спросит пароль для SQL-логина `tastenest_app`.

Для автоматического запуска можно передать пароль параметром:

```powershell
.\Setup-TasteNestServer.ps1 -LoginPassword "CHANGE_ME"
```

## Клиентские настройки

Скопируйте пример:

```text
TasteNest.client.settings.example.json
```

рядом с `TasteNest.exe` и переименуйте в:

```text
TasteNest.settings.json
```

Затем укажите:

- `ServerName`: адрес сервера, например `192.168.0.10,1433`
- `SqlUser`: `tastenest_app`
- `SqlPassword`: пароль, который был указан при настройке сервера

Реальный файл `TasteNest.settings.json` не должен храниться в Git.
