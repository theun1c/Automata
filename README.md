# Automata

Информационная система для сети вендинговых аппаратов.

## Состав решения
- `src/Automata.Desktop` — desktop-клиент (Avalonia).
- `src/Automata.Web` — web-модуль на Razor Pages (только мониторинг).
- `src/Automata.Application` — прикладные контракты и read/write модели.
- `src/Automata.Infrastructure` — EF Core `DbContext`, сущности и сервисы.
- `tests/Automata.Tests` — unit-тесты для ключевых модулей.
- `database` — SQL-схема, recreate и seed-данные.

## Рабочие модули desktop
- Главная (dashboard).
- Монитор ТА.
- Учет ТМЦ.
- Администрирование / Торговые автоматы.
- Администрирование / Пользователи.

## Web-модуль
- Единственная рабочая страница: `Монитор ТА`.

## База данных
Актуальные ограничения описаны в:
- `AGENTS.md`
- `docs/CODEX_SPEC.md`

## Быстрый запуск
1. Поднять PostgreSQL и создать БД `automata`.
2. Применить SQL из `database/recreate_schema.sql`.
3. При необходимости задать `AUTOMATA_CONNECTION_STRING`.
4. Запустить desktop:
   - `dotnet run --project src/Automata.Desktop/Automata.Desktop.csproj`
5. Запустить web:
   - `dotnet run --project src/Automata.Web/Automata.Web.csproj`

## Проверка качества
- Сборка решения:
  - `dotnet build Automata.sln -v minimal`
- Тесты:
  - `dotnet test tests/Automata.Tests/Automata.Tests.csproj -v minimal`
