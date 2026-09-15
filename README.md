# Safe-Sale - Sistema de Gestión de Supermercado

## 📱 Aplicación Multiplataforma de Mostrador
Sistema punto de venta y gestión de supermercado desarrollado con **Avalonia UI**, **C#** y **SQLite** para Windows, macOS, Linux y Android.

---

## 👥 Equipo de Desarrollo

| Integrante | Rol | Responsabilidades |
|------------|-----|-------------------|
| **Lucas** | **Backend** | Base de datos SQLite, repositorios, lógica de negocio, conexión a datos |
| **Jandir** | **Frontend** | Interfaces Avalonia UI, Views XAML, componentes, diseño UI/UX |
| **Dante** | **Documentación** | Diagramas E-R, manual de usuario, documentación técnica, diapositivas |

---

## 📊 Estado del Progreso — 14 de Septiembre 2026

### Progreso General: **~90%** Completado

```
Backend:       ████████████████████ 100%
Frontend:      ████████████████████ 100%
Integración:   ████████████████████ 100%
Documentación: ████░░░░░░░░░░░░░░░░  20%
```

### ✅ Completado

#### Backend (100%)
- [x] **Base de datos SQLite**: `Microsoft.Data.Sqlite` configurado
- [x] **Inicializador de BD**: `SqliteDatabaseInitializer.cs` crea 5 tablas automáticamente
- [x] **Modelos**: `Product.cs`, `Category.cs`, `Sale.cs`, `SaleItem.cs`, `CartItem.cs`, `User.cs`
- [x] **Repositorios CRUD**: `SqliteProductRepository.cs`, `SqliteCategoryRepository.cs`, `SqliteSaleRepository.cs`, `SqliteUserRepository.cs`
- [x] **Compilación**: `dotnet build` → 0 errores, 4 advertencias menores
- [x] **Estructura MVVM**: Separación de concerns implementada
- [x] **Autenticación**: Sistema de login con SHA256 + salt, usuario admin por defecto

#### Frontend - Integración Completa (100%)
- [x] **Navegación**: Sidebar con 4 secciones (Dashboard, Inventario, Ventas, Registros)
- [x] **MainWindow**: Barra lateral con navegación MVVM
- [x] **DashboardView**: KPIs reales desde BD (ventas día, ingresos mes, productos activos, total ventas)
- [x] **InventoryView**: CRUD completo conectado a SQLite, búsqueda, estadísticas
- [x] **SalesView**: Carrito de compras funcional, agregar por código de barras, +/- cantidad, cobro con actualización de stock
- [x] **RecordsView**: Historial de ventas con filtros por rango de fechas
- [x] **Integración Frontend-Backend**: Todas las vistas conectadas a repositorios SQLite
- [x] **Estilos**: Paleta de colores lavanda/lila consistente en todas las vistas
- [x] **Login**: Ventana de autenticación con usuario/contraseña, mensaje de error, diseño consistente
- [x] **Logout**: Botón cerrar sesión en sidebar, vuelve a ventana de login

### ⏳ Pendiente

- [ ] **QR Scanner**: Integración del escáner de código de barras
- [ ] **Testing**: Pruebas en Windows, macOS y Linux
- [ ] **Documentación**: Diagrama E-R, manual de usuario (Dante)
- [ ] **Diapositivas**: Presentación para defensa

---

## 🖥️ Diseño de Interfaz (UI/UX)

### Paleta de Colores
- **Primario**: Lavanda/Lila (`#E8DAEF`, `#BB8FCE`)
- **Acento**: Púrpura oscuro (`#4A148C`, `#7B1FA2`)
- **Fondo**: Rosa pálido (`#FAF3F7`)
- **Advertencia**: Rojo (`#EF5350`)

### Pantallas Principales

#### 1. **MainWindow** - Ventana Principal
- Barra lateral (sidebar) con navegación
- Logo "SS" en la parte superior
- 4 botones de sección con iconos
- ContentControl con ViewLocator para cambiar entre vistas

#### 2. **DashboardView** - Panel de Control
- KPI Cards con datos reales: Ventas del día, Ingresos del mes, Productos activos, Total ventas
- Lista de últimas ventas desde la BD

#### 3. **InventoryView** - Gestión de Inventario
- Barra de búsqueda por nombre o código
- Estadísticas: Total, Stock bajo, Categorías, Valor total
- Lista de productos desde SQLite con ID, Nombre, Código, Precio, Stock, Mínimo
- Botones: Agregar, Editar, Eliminar, Refrescar

#### 4. **SalesView** - Punto de Venta
- Lista de productos disponibles con selección
- Carrito de compras con cantidades (+/-)
- Resumen de venta (total, items)
- Cobro que actualiza stock en la BD
- Búsqueda por código de barras

#### 5. **RecordsView** - Historial
- Filtros por fecha (Desde/Hasta) con DatePicker
- Lista de ventas desde la BD
- Resumen: Total Ventas, Total Transacciones

---

## 📅 Cronograma (Hasta el 22 de septiembre de 2026)

| Fecha | Hito | Estado |
|-------|------|--------|
| **8 sep 2026** | Inicio de fase de ejecución | ✅ Completado |
| **10 sep** | Backend SQLite funcional | ✅ Completado |
| **11 sep** | Integración Frontend-Backend + Navegación + Ventas | ✅ Completado |
| **15 sep** | Login de usuarios | ✅ Completado (14 sep) |
| **18 sep** | Testing en 3 plataformas | ⏳ Pendiente |
| **20 sep** | Ajustes y bugs | ⏳ Pendiente |
| **22 sep** | **Entrega y defensa** | ⏳ Pendiente |

### Tareas por Día (Próximos días)

| Día | Lucas (Backend) | Jandir (Frontend) | Dante (Doc) |
|-----|-----------------|-------------------|-------------|
| **Jue 12** | Login de usuarios | QR Scanner + ajustes UI | Diagrama E-R |
| **Vie 13** | Testing multiplataforma | Testing UI | Manual de usuario |
| **Sáb-Dom** | Ajustes finales | Ajustes finales | Diapositivas |
| **Lun** | Bug fixes | Bug fixes | Ajustes doc |
| **Mar** | Preparar entrega | Preparar entrega | Preparar defensa |
| **Mié 22** | **Entrega y defensa** | **Entrega y defensa** | **Entrega y defensa** |

---

## 📦 Base de Datos

### Tablas Creadas Automáticamente
```sql
categories     (id, name, description)
products       (id, name, barcode, price, stock, category_id, min_stock)
users          (id, username, password_hash, role, created_at)
sales          (id, user_id, total, created_at)
sale_items     (id, sale_id, product_id, quantity, price_sold)
```

### Ruta de la BD (según SO)
- **Windows**: `%LOCALAPPDATA%\supermarket.db`
- **macOS**: `~/Library/Application Support/supermarket.db`
- **Linux**: `~/.local/share/supermarket.db`

---

## 🛠️ Cómo Ejecutar

### Windows / macOS / Linux
```bash
# 1. Clonar el repositorio
git clone https://github.com/MiguelBR4806Y/Safe-Sale.git
cd Safe-Sale

# 2. Restaurar paquetes NuGet
dotnet restore

# 3. Compilar el proyecto
dotnet build src/SS/SS.csproj

# 4. Ejecutar la aplicación
dotnet run --project src/SS/SS.csproj
```

La base de datos se creará automáticamente en el primer arranque.

---

## 📁 Estructura del Proyecto

```
Safe-Sale/
├── src/
│   └── SS/                              # Proyecto principal
│       ├── App.axaml                    # Aplicación Avalonia
│       ├── App.axaml.cs
│       ├── Program.cs                   # Punto de entrada (inicia BD)
│       ├── SS.csproj                    # Configuración del proyecto
│       ├── ViewLocator.cs               # Localizador de vistas
│       ├── Models/
│       │   ├── Product.cs               # Modelo de producto
│       │   ├── Category.cs              # Modelo de categoría
│       │   ├── Sale.cs                  # Modelo de venta
│       │   ├── SaleItem.cs              # Modelo de ítem de venta
│       │   ├── CartItem.cs              # Modelo de ítem del carrito
│       │   └── User.cs                  # Modelo de usuario
│       ├── ViewModels/
│       │   ├── ViewModelBase.cs         # Clase base MVVM
│       │   ├── MainViewModel.cs         # Navegación principal
│       │   ├── DashboardViewModel.cs    # Panel de control
│       │   ├── InventoryViewModel.cs    # Gestión de inventario
│       │   ├── SalesViewModel.cs        # Punto de venta
│       │   ├── RecordsViewModel.cs      # Historial
│       │   └── LoginViewModel.cs        # Lógica de login
│       ├── Views/
│       │   ├── MainWindow.axaml         # Ventana principal (sidebar)
│       │   ├── MainWindow.axaml.cs
│       │   ├── LoginWindow.axaml        # Ventana de login
│       │   ├── LoginWindow.axaml.cs
│       │   ├── DashboardView.axaml      # Panel de control
│       │   ├── DashboardView.axaml.cs
│       │   ├── InventoryView.axaml      # Gestión de inventario
│       │   ├── InventoryView.axaml.cs
│       │   ├── SalesView.axaml          # Punto de venta
│       │   ├── SalesView.axaml.cs
│       │   ├── RecordsView.axaml        # Historial
│       │   └── RecordsView.axaml.cs
│       ├── Data/
│       │   ├── AppDatabase.cs           # Ruta compartida de la BD
│       │   ├── SqliteDatabaseInitializer.cs  # Crea tablas + admin default
│       │   ├── SqliteProductRepository.cs    # CRUD productos
│       │   ├── SqliteCategoryRepository.cs   # CRUD categorías
│       │   ├── SqliteSaleRepository.cs       # CRUD ventas
│       │   └── SqliteUserRepository.cs       # CRUD usuarios
│       ├── Services/
│       │   ├── QrScannerService.cs      # Escáner QR
│       │   └── PasswordHasher.cs        # Hash SHA256 + salt
│       └── Assets/
│           └── avalonia-logo.ico
├── docs/                                # Documentación (Dante)
├── README.md                            # Este archivo
└── requirements.txt                     # Configuración
```

---

## 🎨 Guía de Estilo UI

- **Fondo principal**: `#FAF3F7` (Rosa pálido)
- **Tarjetas/Paneles**: `#FFFFFF` (Blanco)
- **Bordes/Botones**: `#E8DAEF` (Lavanda claro)
- **Botones acento**: `#BB8FCE` (Lila)
- **Texto primario**: `#4A148C` (Púrpura oscuro)
- **Texto secundario**: `#6C3483` (Púrpura medio)
- **Número destacado**: `#7B1FA2` (Púrpura vibrante)
- **Advertencia/Error**: `#EF5350` (Rojo)

---

## 📞 Comunicación

- **Daily check-ins**: 15 min cada mañana
- **Integración**: Lunes y miércoles fusionar cambios
- **Demo final**: Miércoles 22 sep - los 3 presentes

---

*Proyecto: Safe-Sale - Sistema de Gestión de Supermercado*  
*Fecha de entrega: 22 de septiembre de 2026*  
*Integrantes: Lucas (Backend), Jandir (Frontend), Dante (Documentación)*  
*Última actualización: 14 de septiembre de 2026*
