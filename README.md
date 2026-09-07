# CrossApp

Наскрізний проєкт з крос-платформного програмування.
Предметна область: Склад.
Сутності: Product, StockBatch, Warehouse, Movement.
Призначення: облік залишків товарів по партіях.

## Запуск

dotnet build
dotnet run --project src/Cli

## Середовище

.NET SDK 10.0, Windows 11 x64 (win-x64)

## Додаткові завдання

### 1. Порівняння розмірів Self-contained збірок

- `win-x64` publish: **70.50 MB** (70.496 MB)
- `linux-x64` publish: **70.51 MB** (70.513 MB)

### 2. Запуск з прапорцем --json

dotnet run --project src/Cli -- --json
