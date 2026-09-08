# Safe-Sale - Supermercado Avalonia UI

## 📱 Aplicación de mostrador de supermercado
Multiplataforma (Windows, macOS, Linux) usando **Avalonia UI**, **C#** y **SQLite** nativo.

---

## 📅 Estado Actual del Proyecto

| Fecha | Hito |
|-------|------|
| **Hoy**: 8 de septiembre de 2026 | Inicio de la fase de ejecución |
| **Entrega**: 22 de septiembre de 2026 | **9 días hábiles** |
| **Duración restante**: 2 semanas | Objetivo: Backend completo y operable |

---

## ✅ Qué ya está concluido

- **Estructura del proyecto**: `.csproj`, `Program.cs`, capas de MVVM
- **Base de datos SQLite**: `Microsoft.Data.Sqlite` instalado y configurado
- **Inicializador de BD**: `SqliteDatabaseInitializer.cs` crea 6 tablas automáticamente
- **Modelos**: `Product.cs`, `Category.cs` con `ObservableObject` (CommunityToolkit.Mvvm)
- **Repositorios**: `SqliteProductRepository.cs`, `SqliteCategoryRepository.cs` con CRUD completo
- **Conexión en Program.cs**: Base de datos se crea en el primer arranque en `~/supermarket.db` (ruta nativa por SO)

---

## 📦 Estructura de la Base de Datos (creada automáticamente)

```sql
-- Tablas creadas en la primera ejecución:
-- categories, products, users, sales, sale_items
```

---

## 🎯 Objetivos Restantes (9 días)

| Tarea | Responsable | Prioridad |
|-------|-------------|-----------|
| **Probar operaciones CRUD** (agregar/editar/borrar productos) | Lucas | Alta |
| **Conectar ViewModels** con repositorios SQLite | Lucas | Alta |
| **Pantalla de Login** para roles admin/cashier | Lucas | Alta |
| **Módulo de ventas** (carrito, cálculo de total, registro de venta) | Lucas | Alta |
| **Actualizar UI** (DataGrid productos, formulario de ingreso) | Lucas | Media |
| **Probar en Linux/macOS** (multiplatform) | Lucas | Media |
| **Preparar demo/defensa** (capturas, README actualizado) | Lucas | Baja |

---

## 🗓️ Cronograma Ajustado (9 días)

```
Día 1-2 (lun-mar): Probar CRUD y conexión BD → Already done ✅
Día 3-4 (miér-jue): ViewModels + Login + Pantallas básicas
Día 5-6 (vie-lun): Módulo de ventas y carrito
Día 7-8 (mar-mi): Testing en las 3 plataformas + bugs
Día 9 (mié): Ajustes finos + README + Preparación demo
```

---

## 👨‍💻 Rol de Lucas (Único desarrollador activo)

Dado el plazo ajustado, las responsabilidades se concentran en:

1. **Backend completo**: BD, repositorios, conexión, lógica de negocio
2. **Frontend UI**: Views XAML y ViewModels para Avalonia
3- **Testing**: Probar en Windows/macOS/Linux
4. **Documentación**: README, manuales, demo

---

## 📦 Cómo correr el proyecto

```bash
# 1. Restaurar paquetes
dotnet restore

# 2. Compilar (crea supermarket.db automáticamente en primer arranque)
dotnet build

# 3. Ejecutar
dotnet run --project src/SS/SS.csproj
```

La base de datos se creará automáticamente en la carpeta nativa de cada SO:
- **Windows**: `%APPDATA%\supermarket.db`
- **macOS**: `~/Library/Application Support/supermarket.db`
- **Linux**: `~/.local/share/supermarket.db`

---

## 🛠️ Tecnologías usadas

- **Avalonia UI** - Framework multiplataforma
- **C# .NET 10** - Backend/lógica
- **Microsoft.Data.Sqlite** - SQLite nativo (sin configuraciones extras)
- **SQLitePCLRaw** - Provedores nativos para Windows/macOS/Linux (incluidos en el proyecto)
- **CommunityToolkit.Mvvm** - Patrón MVVM
- **SQLite** - Base de datos local embebida

---

*Proyecto: Supermercado Safe-Sale*  
*Fecha de entrega: 22 de septiembre de 2026*  
*Desarrollado por Lucas*