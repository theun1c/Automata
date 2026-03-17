# План самостоятельной разработки (18.03.2026)

Цель дня:
1. Довести UI до максимально точного совпадения с макетами.
2. Параллельно разобраться в текущей структуре проекта.
3. Подготовить и частично выполнить рефакторинг архитектуры под масштабирование.

## 0) Режим работы (рекомендация)

Работайте короткими итерациями:
- 45-60 минут работа
- 10 минут сверка с макетами + фиксация заметок

Каждую итерацию завершайте мини-логом:
- что поменяли,
- что сломалось,
- что дальше.

## 1) Блок A: Дизайн (приоритет P1)

### A1. Sidebar + Header
Файлы:
- `src/Automata.Desktop/Views/MainWindow.axaml`
- `src/Automata.Web/Pages/Shared/_Layout.cshtml`
- `src/Automata.Web/wwwroot/css/site.css`

Что дожать:
- точные отступы и размеры шрифтов;
- состояние hover/active пунктов;
- визуал раскрытия `Администрирование`;
- контраст/цвета/толщина границ;
- выравнивание верхней панели и breadcrumb.

### A2. Страницы Admin
Файлы:
- `src/Automata.Desktop/Views/MachinesView.axaml`
- `src/Automata.Desktop/Views/CompaniesView.axaml`
- `src/Automata.Desktop/Views/*Window.axaml`

Что дожать:
- высоты строк таблиц,
- ширины колонок,
- кнопки действий и их визуальный вес,
- spacing в формах,
- нижняя панель действий в модалках.

### A3. Главная + Монитор
Файлы:
- `src/Automata.Desktop/Views/DashboardView.axaml`
- `src/Automata.Desktop/Views/MonitorTaView.axaml`
- `src/Automata.Web/Pages/Monitor/Index.cshtml`
- `src/Automata.Web/wwwroot/css/site.css`

Что дожать:
- пропорции карточек,
- графические акценты (псевдо-диаграммы/таблица/текстовые блоки),
- плотность контента как в референсе.

## 2) Блок B: Разбор проекта (приоритет P1, параллельно A)

### B1. Карта проекта
Соберите для себя таблицу:
- слой/папка,
- назначение,
- зависимости.

Минимум:
- `Automata.Domain`
- `Automata.Application`
- `Automata.Infrastructure`
- `Automata.Desktop`
- `Automata.Web`

### B2. Где точка входа UI
Desktop:
- `App.axaml` / `App.axaml.cs`
- `MainWindow.axaml`
- `MainWindowViewModel.cs`
- `ViewLocator.cs`

Web:
- `Pages/Shared/_Layout.cshtml`
- `Pages/Monitor/Index.cshtml(.cs)`

### B3. Модалки
Разобрать шаблон:
- флаг в VM (`Is*Open`) ->
- событие `PropertyChanged` в View code-behind ->
- `new Window().ShowDialog(owner)` ->
- закрытие и синхронизация флага.

## 3) Блок C: Архитектурный рефакторинг (приоритет P2)

Важно: сначала согласовать целевую структуру, потом переносить.

### C1. Предложенная целевая структура Desktop

Пример:
- `Automata.Desktop/Shell/*`
- `Automata.Desktop/Modules/Auth/*`
- `Automata.Desktop/Modules/Dashboard/*`
- `Automata.Desktop/Modules/Monitor/*`
- `Automata.Desktop/Modules/Admin/Machines/*`
- `Automata.Desktop/Modules/Admin/Companies/*`
- `Automata.Desktop/Common/*`

### C2. Правила перемещения
- Переносить сначала `View + ViewModel + code-behind` едиными блоками.
- После каждого блока проверять:
  - namespace,
  - `x:Class` в `.axaml`,
  - соответствие в `ViewLocator`.
- Не делать массовый rename без промежуточной проверки запуска.

### C3. Рефакторинг пошагово
1. Вынести только один модуль (например `Admin/Machines`).
2. Убедиться, что запускается.
3. Вынести `Admin/Companies`.
4. Вынести `Monitor`.
5. Потом трогать shell.

## 4) Отдельное решение по web внутри desktop

Есть 2 варианта:

### Вариант 1 (быстрый): оставить текущий desktop MonitorTaView
- Плюс: просто и стабильно.
- Минус: web и desktop расходятся визуально/поведенчески.

### Вариант 2 (масштабируемый): встроить реальный Razor в desktop через WebView
- Desktop открывает встроенный браузер на `http://localhost:<port>/Monitor/Index`.
- `Automata.Web` стартует локально (в фоне) при открытии раздела.
- Плюс: единый монитор в web/desktop.
- Минус: инфраструктурно сложнее (жизненный цикл процесса, порт, shutdown).

Рекомендация: подготовить архитектурный каркас под Вариант 2, но внедрять после UI freeze.

## 5) На что обратить внимание (чтобы не потерять день)

- Не смешивать визуальные правки с большим переносом файлов в одном коммите.
- Не ломать русские подписи элементов.
- Не удалять mock-данные пока не начат backend-этап.
- В рефакторинге всегда держать рабочую точку отката.

## 6) Минимальный Definition of Done на завтра

- Визуал desktop shell + admin страниц доведен до целевого состояния.
- Есть согласованная и зафиксированная целевая архитектура модулей.
- Минимум 1-2 модуля реально перенесены в новую структуру без регрессий.
- Документация обновлена по факту переноса.
