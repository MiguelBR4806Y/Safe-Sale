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

## 🎯 Estado Actual del Progreso

### ✅ Completado (Backend 100% funcional)
- [x] **Base de datos SQLite**: `Microsoft.Data.Sqlite` configurado
- [x] **Inicializador de BD**: `SqliteDatabaseInitializer.cs` crea 6 tablas automáticamente
- [x] **Modelos**: `Product.cs`, `Category.cs` con CommunityToolkit.Mvvm
- [x] **Repositorios CRUD**: `SqliteProductRepository.cs`, `SqliteCategoryRepository.cs`
- [x] **Compilación**: `dotnet build` → 0 errores, 4 advertencias menores
- [x] **Estructura MVVM**: Separación de concerns implementada

### 🔧 En Progreso
- [ ] **Vistas XAML**: Estructura base creada, pendiente integración con Backend
- [ ] **Navegación**: Sidebar con 4 secciones implementado
- [ ] **QR Scanner**: Estructura lista en `Services/QrScannerService.cs`

### ⏳ Pendiente
- [ ] **Integración Frontend-Backend**: Conectar vistas con repositorios
- [ ] **Módulo de Ventas**: Carrito y cobro
- [ ] **Login**: Autenticación de usuarios
- [ ] **Testing**: Pruebas en 3 plataformas
- [ ] **Documentación**: Diagramas y manual

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

#### 2. **DashboardView** - Panel de Control
- KPI Cards: Ventas del día, Ingresos del mes, Productos activos, Clientes
- Gráfico de ventas por hora (placeholder)
- Últimas ventas

#### 3. **InventoryView** - Gestión de Inventario
- Barra de búsqueda por nombre o código
- Estadísticas: Total, Stock bajo, Categorías, Valor total
- Lista de productos con ID, Nombre, Código, Precio, Stock, Mínimo
- Botones: Agregar, Editar, Eliminar, Cargar por Barra

#### 4. **SalesView** - Punto de Venta
- Carrito de compras
- Resumen de venta (total, items)
- Formulario nueva venta
- Botones: Agregar al Carro, Cobrar

#### 5. **RecordsView** - Historial
- Filtros por fecha (Desde/Hasta)
- Lista de registros (ventas, devoluciones)
- Resumen: Total Ventas, Total Devoluciones

---

## 📅 Cronograma (Hasta el 22 de septiembre de 2026)

| Fecha | Hito | Estado |
|-------|------|--------|
| **8 sep 2026** | Inicio de fase de ejecución | ✅ Completado |
| **10 sep** | Backend SQLite funcional | ✅ Completado |
| **15 sep** | Vistas XAML base | ✅ Completado |
| **18 sep** | Integración Frontend-Backend | 🔧 En progreso |
| **20 sep** | Testing y ajustes | ⏳ Pendiente |
| **22 sep** | **Entrega y defensa** | ⏳ Pendiente |

### Tareas por Día (Próximos días)

| Día | Lucas (Backend) | Jandir (Frontend) | Dante (Doc) |
|-----|-----------------|-------------------|-------------|
| **Jue-Vie** | Conectar repositorios con Views | Completar diseño UI | Diagrama E-R actualizado |
| **Sáb-Dom** | Módulo ventas + carrito | Pantalla POS + cobro | Manual de usuario |
| **Lun** | Testing en 3 plataformas | Testing UI + bugs | Ajustes finales |
| **Mar** | Ajustes backend finalizados | Ajustes UI | Preparar defensa |
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
- **Windows**: `%APPDATA%\supermarket.db`
- **macOS**: `~/Library/Application Support/supermarket.db`
- **Linux**: `~/.local/share/supermarket.db`

---

## 🛠️ Cómo Ejecutar

```bash
# 1. Restaurar paquetes NuGet
dotnet restore

# 2. Compilar el proyecto
dotnet build src/SS/SS.csproj

# 3. Ejecutar la aplicación
dotnet run --project src/SS/SS.csproj
```

La base de datos se creará automáticamente en el primer arranque.

---

## 📁 Estructura del Proyecto

```
Safe-Sale/
├── src/
│   └── SS/                          # Proyecto principal
│       ├── App.axaml                # Aplicación Avalonia
│       ├── App.axaml.cs
│       ├── Program.cs               # Punto de entrada (inicia BD)
│       ├── SS.csproj                # Configuración del proyecto
│       ├── ViewLocator.cs           # Localizador de vistas
│       ├── Models/
│       │   ├── Product.cs           # Modelo de producto
│       │   └── Category.cs          # Modelo de categoría
│       ├── ViewModels/
│       │   ├── ViewModelBase.cs     # Clase base MVVM
│       │   ├── MainViewModel.cs     # ViewModel principal
│       │   └── InventoryViewModel.cs # ViewModel de inventario
│       ├── Views/
│       │   ├── MainWindow.axaml     # Ventana principal (sidebar)
│       │   ├── DashboardView.axaml  # Panel de control
│       │   ├── InventoryView.axaml  # Gestión de inventario
│       │   ├── SalesView.axaml      # Punto de venta
│       │   └── RecordsView.axaml    # Historial
│       ├── Data/
│       │   ├── SqliteDatabaseInitializer.cs  # Crea tablas
│       │   ├── SqliteProductRepository.cs    # CRUD productos
│       │   └── SqliteCategoryRepository.cs   # CRUD categorías
│       ├── Services/
│       │   └── QrScannerService.cs  # Escáner QR
│       └── Assets/
│           └── avalonia-logo.ico
├── docs/                            # Documentación (Dante)
├── README.md                        # Este archivo
└── requirements.txt                 # Configuración
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
