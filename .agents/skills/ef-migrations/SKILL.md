---
name: ef-migrations
description: >-
  Standard operating procedures for creating, applying, and rolling back Entity Framework Core migrations with MS SQL Server.
  Use when modifying database entities, adding columns, tables, indexes, or updating the database schema.
---

# Entity Framework Core & MS SQL Migrations

## 1. Добавление новой миграции
При изменении сущностей или конфигураций `IEntityTypeConfiguration<T>` выполните команду в корне решения:

```powershell
dotnet ef migrations add <MigrationName> --project src/DatingBot.Infrastructure --startup-project src/DatingBot.Bot --output-dir Data/Migrations
```

## 2. Применение миграций к базе данных
Для применения всех непримененных миграций к локальной или целевой БД MS SQL:

```powershell
dotnet ef database update --project src/DatingBot.Infrastructure --startup-project src/DatingBot.Bot
```

## 3. Откат миграции
Для отката базы до определенной миграции:
```powershell
dotnet ef database update <PreviousMigrationName> --project src/DatingBot.Infrastructure --startup-project src/DatingBot.Bot
```

Для удаления последней еще не примененной миграции:
```powershell
dotnet ef migrations remove --project src/DatingBot.Infrastructure --startup-project src/DatingBot.Bot
```

## 4. Генерация SQL-скрипта (для Production)
```powershell
dotnet ef migrations script --project src/DatingBot.Infrastructure --startup-project src/DatingBot.Bot --idempotent --output migrations.sql
```
