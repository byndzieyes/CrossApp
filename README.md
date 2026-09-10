# CrossApp

Наскрізний проєкт з крос-платформного програмування.  
Предметна область: Склад.  
Сутності: Product, StockBatch, Warehouse, Movement.  
Призначення: облік залишків товарів по партіях.

## Структура рішення

```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs
```

- **`Core`** (`classlib`): центральна бібліотека логіки, яка не залежить від інтерфейсу користувача чи платформи виводу.
- **`Cli`** (`exe`): консольна точка входу, яка посилається на `Core` (`Cli` → `Core`) та відповідає виключно за введення/виведення даних.

---

## Збірка та запуск

### Збірка всіх проєктів рішення

```bash
dotnet build
```

### Запуск консольного застосунку

```bash
dotnet run --project src/Cli
```

### Збірка окремо бібліотеки Core

```bash
dotnet build src/Core/Core.csproj
```

---

## Публікація

### 1. Framework-dependent (потребує встановленого .NET 10 Runtime)

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained false -o publish/win-x64-fd
```

### 2. Self-contained (включає .NET Runtime у вихідний каталог)

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -o publish/win-x64-sc
```

### Запуск опублікованого застосунку

```powershell
# Приклад для Windows:
.\publish\self-contained\Cli.exe
```

---

## Порівняння режимів публікації

| RID       | Режим               | Розмір publish | Потрібен встановлений runtime |
| :-------- | :------------------ | :------------- | :---------------------------- |
| `win-x64` | self-contained      | ~76.67 МБ      | Ні                            |
| `win-x64` | framework-dependent | ~0.19 МБ       | Так (.NET 10)                 |

### Різниця режимів

- **Framework-dependent** містить лише скомпільований код застосунку та залежності, тому має мінімальний розмір. Працює лише на машинах із попередньо встановленим сумісним середовищем .NET Runtime.
- **Self-contained** пакує застосунок разом із копією середовища виконання .NET Runtime для цільової платформи (RID). Не вимагає наявності встановленого .NET на машині клієнта, але має значно більший розмір і прив'язаний до конкретної ОС/архітектури.

---

## Середовище

.NET SDK 10.0, Windows 11 x64 (`win-x64`)

---

## Додаткові завдання (Лабораторна 1)

### 1. Порівняння розмірів Self-contained збірок

- `win-x64` publish: **70.50 MB** (70.496 MB)
- `linux-x64` publish: **70.51 MB** (70.513 MB)

### 2. Запуск з прапорцем --json

```bash
dotnet run --project src/Cli -- --json
```
