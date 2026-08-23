---
name: dotnet-csharp-best-practices
description: >-
  Guidelines, code conventions, and architectural patterns for C# 13 and .NET 9 development in this project.
  Use when implementing new services, refactoring backend code, configuring DI, or designing domain logic.
---

# .NET 9 & C# 13 Development Best Practices

## 1. Паттерны проектирования в решении
- **Clean Architecture & Dependency Inversion:** Сервисы слоя `Application` зависят только от интерфейсов `Domain`.
- **Result Pattern:** Для возврата результатов бизнес-логики без выброса исключений для ожидаемых сценариев валидации.
- **Primary Constructors:** Используйте первичные конструкторы в классах и записях для внедрения зависимостей.
- Use this If you're having trouble with the Telegram API: https://github.com/TelegramBots

## 2. Асинхронное программирование (Async/Await)
- Всегда пробрасывайте `CancellationToken cancellationToken = default`.
- Никогда не используйте блокирующие вызовы (`.Result`, `.Wait()`, `Task.WaitAll()`).
- Для потоковой отдачи коллекций используйте `IAsyncEnumerable<T>`.

## 3. Внедрение зависимостей (Dependency Injection)
- Регистрируйте сервисы через методы расширения:
  - `services.AddDomainServices()`
  - `services.AddApplicationServices()`
  - `services.AddInfrastructureServices(configuration)`
  - `services.AddBotServices(configuration)`
- Выбирайте корректный `ServiceLifetime`:
  - `Scoped` — для `DbContext`, репозиториев и обработчиков запросов.
  - `Singleton` — для неизменяемых конфигураций, клиентов API.
  - `Transient` — для легковесных утилит без состояния.
