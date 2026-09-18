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

## 📊 Estado del Progreso — 17 de Septiembre 2026

### Progreso General: **~98%** Completado

```
Backend:       ████████████████████ 100%
Frontend:      ████████████████████ 100%
Integración:   ████████████████████ 100%
Documentación: ████░░░░░░░░░░░░░░░░  20%
```

### ✅ Completado

#### Backend (100%)
- [x] **Base de datos SQLite**: `Microsoft.Data.Sqlite` configurado
- [x] **Inicializador de BD**: `SqliteDatabaseInitializer.cs` crea tablas automáticamente + migración `payment_method`
- [x] **Modelos**: `Product.cs`, `Category.cs`, `Sale.cs` (con `PaymentMethod`), `SaleItem.cs`, `CartItem.cs`, `User.cs`
- [x] **Repositorios CRUD**: `SqliteProductRepository.cs` (con `GetById`, `GetByBarcode`), `SqliteCategoryRepository.cs`, `SqliteSaleRepository.cs` (con `payment_method`), `SqliteUserRepository.cs`
- [x] **Compilación**: `dotnet build` → 0 errores
- [x] **Estructura MVVM**: Separación de concerns implementada
- [x] **Autenticación**: Sistema de login con SHA256 + salt, usuario admin por defecto
- [x] **Eliminación segura**: DELETE con cascade en `sale_items` (no crashea al eliminar productos con ventas)

#### Frontend - Integración Completa (100%)
- [x] **Navegación**: Sidebar con 4 secciones (Dashboard, Inventario, Ventas, Registros)
- [x] **MainWindow**: Barra lateral con navegación MVVM, estilos globales para ListBox/ListBoxItem
- [x] **DashboardView**: Tarjetas KPI modernas con iconos, sombras, colores diferenciados, lista de ventas recientes con diseño de tarjetas
- [x] **InventoryView**: CRUD completo conectado a SQLite, búsqueda, estadísticas, escáner de cámara PC, escáner móvil QR
- [x] **SalesView**: Carrito de compras funcional, búsqueda por código de barras, escáner de cámara PC, escáner móvil QR, selector de método de pago (Efectivo/Tarjeta/Transferencia), cobro con actualización de stock
- [x] **RecordsView**: Filtros modernos, estadísticas con iconos, tabla de registros con badges
- [x] **LoginWindow**: Diseño moderno con fondo decorativo, logo con fondo morado, inputs redondeados, credenciales por defecto en tarjeta
- [x] **EditProductDialog**: Diálogo de edición con Barcode/Name/Price/Stock/MinStock, validación
- [x] **Estilos**: Paleta lavanda/lila consistente, tema forzado a Light
- [x] **Logout**: Botón cerrar sesión en sidebar

#### Escáner Móvil (HTTP) (100%)
- [x] **Servidor HTTP embebido**: `MobileScannerService.cs` sirve HTML + QR + endpoints REST
- [x] **QR Code**: Generado con `QRCoder`, muestra URL con IP local y puerto
- [x] **Página móvil**: HTML/CSS/JS optimizada para teléfono con dos pestañas:
  - **Carrito**: Escanea código → si existe se agrega al carrito; búsqueda de productos existentes con botón "Agregar"
  - **Nuevo**: Escanea código → si es nuevo, formulario con Código/Nombre/Precio/Stock/StockMínimo
- [x] **Detección de código**: ZXing-js + BarcodeDetector API + foto del código
- [x] **Endpoint `/check`**: Verifica si un producto existe en la BD
- [x] **Endpoint `/products`**: Busca productos por nombre/código
- [x] **Endpoint `/scan`**: Recibe códigos y los enruta según modo (inventory/sales)
- [x] **Modo dual**: El QR cambia entre modo "inventory" y "sales" según la pantalla activa
- [x] **Polling centralizado**: Un solo timer en `MainViewModel` procesa todos los barcodes
- [x] **Carrito desde móvil**: Botón "Agregar" en lista de productos → envía `CART:productId` → se agrega al carrito de la PC

#### Escáner de Cámara PC (100%)
- [x] **CameraService**: Captura frames de la cámara USB/webcam
- [x] **BarcodeScannerService**: Decodifica códigos de barras con ZXing
- [x] **En Inventario**: Escanea → si existe muestra info, si no abre diálogo de agregar
- [x] **En Ventas**: Escanea → si existe agrega al carrito, si no muestra error
- [x] **Preview en vivo**: Muestra video de cámara con marco de escaneo overlay

### 📱 Funcionalidades Móvil Detalladas

#### Pestaña "Carrito" (modo ventas)
- Botón "Escanear en Vivo" → usa cámara trasera del teléfono
- Botón "Tomar Foto del Código" → selecciona imagen de galería
- Campo código de barras (solo lectura, se llena automáticamente)
- **Buscar producto existente**: Campo de búsqueda que muestra lista de productos con nombre, código, stock, precio y botón "Agregar"
- Al tocar "Agregar" → producto se añade al carrito de la PC en tiempo real

#### Pestaña "Nuevo" (modo inventario)
- Botón "Escanear en Vivo" / "Tomar Foto del Código"
- Campo código de barras (se llena automáticamente)
- Campos: Nombre, Precio, Stock Inicial, Stock Mínimo (alerta)
- Botón "Guardar Nuevo" → crea el producto en la BD

### ⏳ Pendiente

- [ ] **Testing**: Pruebas en Windows, macOS y Linux
- [ ] **Documentación**: Diagrama E-R, manual de usuario (Dante)
- [ ] **Diapositivas**: Presentación para defensa

---

## 🖥️ Diseño de Interfaz (UI/UX)

### Paleta de Colores
- **Primario**: Lavanda/Lila (`#E8DAEF`, `#BB8FCE`)
- **Acento**: Púrpura oscuro (`#4A148C`, `#7B1FA2`)
- **Fondo**: Rosa pálido (`#FAF3F7`)
- **Éxito**: Verde (`#4CAF50`)
- **Advertencia**: Naranja (`#FF9800`)
- **Error**: Rojo (`#EF5350`)

### Pantallas Principales

#### 1. **LoginWindow** - Inicio de Sesión
- Fondo decorativo con formas orgánicas
- Logo "SS" con fondo morado redondeado
- Inputs con bordes redondeados y focus highlight
- Botón "Iniciar Sesión" con spinner de carga
- Credenciales por defecto: admin / admin123

#### 2. **MainWindow** - Ventana Principal
- Barra lateral (sidebar) con navegación
- Logo "SS" en la parte superior
- 4 botones de sección con iconos
- Estilos globales para ListBox transparentes con hover lavanda

#### 3. **DashboardView** - Panel de Control
- Tarjetas KPI con iconos Unicode, sombras, valores grandes
- Colores diferenciados por métrica (púrpura, verde, naranja)
- Lista de últimas ventas con diseño de tarjetas y badges

#### 4. **InventoryView** - Gestión de Inventario
- Barra de búsqueda por nombre o código
- Estadísticas: Total, Stock bajo, Categorías, Valor total
- Botones de escáner: Cámara PC + Móvil QR
- Vista previa de cámara con marco de escaneo
- QR inline cuando el escáner móvil está activo
- Tabla de productos con ID, Nombre, Código, Precio, Stock, Mínimo
- Botones: Agregar, Editar, Eliminar, Agregar al Carrito

#### 5. **SalesView** - Punto de Venta
- **Escáner de Cámara**: Botón toggle, preview en vivo con marco
- **Escáner Móvil**: Botón toggle, QR inline, solo productos existentes
- **Búsqueda manual**: Campo de código de barras + botón Buscar
- **Carrito**: Lista con cantidades (+/-), subtotal, botón eliminar
- **Resumen**: Items, Total, Método de pago (Efectivo/Tarjeta/Transferencia)
- **Botones**: COBRAR, Vaciar Carrito

#### 6. **RecordsView** - Historial
- Filtros modernos con DatePicker
- Estadísticas con iconos: Total Ventas, Transacciones
- Tabla de registros con badges de tipo y totales destacados

#### 7. **EditProductDialog** - Editar Producto
- Campos: Código, Nombre, Precio, Stock, Stock Mínimo
- Validación de campos obligatorios
- Guardar con actualización en BD

---

## 📅 Cronograma (Hasta el 22 de septiembre de 2026)

| Fecha | Hito | Estado |
|-------|------|--------|
| **8 sep 2026** | Inicio de fase de ejecución | ✅ Completado |
| **10 sep** | Backend SQLite funcional | ✅ Completado |
| **11 sep** | Integración Frontend-Backend + Navegación + Ventas | ✅ Completado |
| **14 sep** | Login de usuarios | ✅ Completado |
| **15 sep** | QR Scanner + Estilos UI | ✅ Completado |
| **16 sep** | Escáner móvil HTTP + Carrito desde móvil | ✅ Completado |
| **17 sep** | Escáner cámara PC + Dashboard/Login/Registros mejorados | ✅ Completado |
| **18 sep** | Testing en 3 plataformas | ⏳ Pendiente |
| **20 sep** | Ajustes y bugs | ⏳ Pendiente |
| **22 sep** | **Entrega y defensa** | ⏳ Pendiente |

---

## 📦 Base de Datos

### Tablas Creadas Automáticamente
```sql
categories     (id, name, description)
products       (id, name, barcode, price, stock, category_id, min_stock)
users          (id, username, password_hash, role, created_at)
sales          (id, user_id, total, payment_method, created_at)
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

### Escáner Móvil
1. En la app, ve a **Inventario** o **Ventas**
2. Activa **"Escanear con Móvil"** → aparece un código QR
3. Abre la cámara del teléfono → escanea el QR
4. Se abre la página del escáner en el teléfono
5. Usa la pestaña **"Carrito"** para agregar productos existentes
6. Usa la pestaña **"Nuevo"** para crear productos (solo desde Inventario)

> **Nota**: El teléfono y la PC deben estar en la misma red WiFi.

---

## 📦 Instalador y Actualizaciones

### Para Usuarios - Instalar la App
1. Ve a [Releases](https://github.com/MiguelBR4806Y/Safe-Sale/releases)
2. Descarga el instalador para tu plataforma:
   - **Windows**: `SafeSale-1.0.0-full.nupkg` o ejecuta el `Setup.exe`
   - **macOS Apple Silicon** (M1/M2/M3/M4): `SafeSale-1.0.0-arm64.pkg`
   - **macOS Intel**: `SafeSale-1.0.0-x64.pkg`
3. Ejecuta el instalador
4. La app se actualizará automáticamente cuando haya nuevas versiones

### Para Desarrolladores - Crear Instalador

#### Requisitos previos
```bash
# Instalar dotnet tool Velopack
dotnet tool install --global Velopack

# Para publicar en GitHub (opcional, para auto-updates)
winget install GitHub.cli
gh auth login
```

#### Publicar desde Windows
```powershell
# Ejecutar el script de publicación
.\publish-windows.ps1
```

#### Publicar desde macOS
```bash
# Ejecutar el script de publicación
chmod +x publish-macos.sh
./publish-macos.sh
```

#### Flujo de actualización
```
1. Haces cambios en el código
2. Actualizas la versión en SS.csproj (<Version>X.Y.Z</Version>)
3. Ejecutas el script de publicación (Windows o macOS)
4. Los usuarios reciben la actualización automáticamente al abrir la app
```

#### Comandos manuales
```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained -o ./publish/win-x64
vpk pack --packId SafeSale --packVersion 1.0.0 --packDir ./publish/win-x64 --mainExe SafeSale.exe
vpk publish --repoUrl "https://github.com/MiguelBR4806Y/Safe-Sale" --tag "v1.0.0"

# macOS (desde Mac)
dotnet publish -c Release -r osx-arm64 --self-contained -o ./publish/osx-arm64
dotnet publish -c Release -r osx-x64 --self-contained -o ./publish/osx-x64
vpk pack --packId SafeSale --packVersion 1.0.0 --packDir ./publish/osx-arm64 --mainExe SafeSale
vpk pack --packId SafeSale --packVersion 1.0.0 --packDir ./publish/osx-x64 --mainExe SafeSale
vpk publish --repoUrl "https://github.com/MiguelBR4806Y/Safe-Sale" --tag "v1.0.0"
```

---

## 📁 Estructura del Proyecto

```
Safe-Sale/
├── src/
│   └── SS/                              # Proyecto principal
│       ├── App.axaml                    # Aplicación Avalonia (tema Light)
│       ├── App.axaml.cs
│       ├── Program.cs                   # Punto de entrada (inicia BD)
│       ├── SS.csproj                    # Configuración del proyecto
│       ├── ViewLocator.cs               # Localizador de vistas
│       ├── Models/
│       │   ├── Product.cs               # Modelo de producto
│       │   ├── Category.cs              # Modelo de categoría
│       │   ├── Sale.cs                  # Modelo de venta (con PaymentMethod)
│       │   ├── SaleItem.cs              # Modelo de ítem de venta
│       │   ├── CartItem.cs              # Modelo de ítem del carrito
│       │   └── User.cs                  # Modelo de usuario
│       ├── ViewModels/
│       │   ├── ViewModelBase.cs         # Clase base MVVM
│       │   ├── MainViewModel.cs         # Navegación + polling centralizado
│       │   ├── DashboardViewModel.cs    # Panel de control
│       │   ├── InventoryViewModel.cs    # Gestión de inventario + escáner
│       │   ├── SalesViewModel.cs        # Punto de venta + escáner
│       │   ├── RecordsViewModel.cs      # Historial
│       │   └── LoginViewModel.cs        # Lógica de login
│       ├── Views/
│       │   ├── MainWindow.axaml         # Ventana principal (sidebar)
│       │   ├── MainWindow.axaml.cs
│       │   ├── LoginWindow.axaml        # Ventana de login (diseño moderno)
│       │   ├── LoginWindow.axaml.cs
│       │   ├── DashboardView.axaml      # Panel de control (tarjetas KPI)
│       │   ├── DashboardView.axaml.cs
│       │   ├── InventoryView.axaml      # Gestión de inventario + escáner
│       │   ├── InventoryView.axaml.cs
│       │   ├── SalesView.axaml          # Punto de venta + escáner
│       │   ├── SalesView.axaml.cs
│       │   ├── RecordsView.axaml        # Historial (diseño moderno)
│       │   └── RecordsView.axaml.cs
│       ├── Dialogs/
│       │   ├── EditProductDialog.axaml  # Editar producto
│       │   └── EditProductDialog.axaml.cs
│       ├── Data/
│       │   ├── AppDatabase.cs           # Ruta compartida de la BD
│       │   ├── SqliteDatabaseInitializer.cs  # Crea tablas + migra columnas
│       │   ├── SqliteProductRepository.cs    # CRUD productos + GetByBarcode
│       │   ├── SqliteCategoryRepository.cs   # CRUD categorías
│       │   ├── SqliteSaleRepository.cs       # CRUD ventas + payment_method
│       │   └── SqliteUserRepository.cs       # CRUD usuarios
│       ├── Services/
│       │   ├── MobileScannerService.cs  # Servidor HTTP + QR + escáner móvil
│       │   ├── CameraService.cs         # Captura de cámara PC
│       │   ├── BarcodeScannerService.cs # Decodificación de código de barras
│       │   ├── PasswordHasher.cs        # Hash SHA256 + salt
│       │   ├── UpdateService.cs         # Auto-updates con Velopack + GitHub
│       │   ├── ICameraService.cs        # Interfaz de cámara
│       │   └── IBarcodeScannerService.cs # Interfaz de escáner
│       ├── Converters/
│       │   └── CameraConverters.cs      # BoolToString, BoolToColor
│       ├── www/
│       │   └── zxing.min.js             # ZXing-js para decodificación
│       └── Assets/
│           └── avalonia-logo.ico
├── docs/                                # Documentación (Dante)
├── publish-windows.ps1                  # Script de publicación Windows
├── publish-macos.sh                     # Script de publicación macOS
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
- **Éxito**: `#4CAF50` (Verde)
- **Advertencia**: `#FF9800` (Naranja)
- **Error**: `#EF5350` (Rojo)
- **Tema**: Forzado a **Light** en `App.axaml`

---

## 🔧 Notas Técnicas

- **Smart App Control (SAC)**: En Windows 11 25H2 en modo enforcement, bloquea ejecutables no firmados en Documents/Desktop. Solución: ejecutar desde `C:\SS`
- **SQLite Vulnerabilidad**: NU1903 suprimida con `<NoWarn>NU19003</NoWarn>` (no hay versión parchada de SQLitePCLRaw)
- **Cámara en móvil**: `navigator.mediaDevices` no disponible en HTTP. Solo funciona con archivo/foto
- **ZXing-js**: `HTMLCanvasElementLuminanceSource` más confiable que `RGBLuminanceSource` para fotos

---

## 📞 Comunicación

- **Daily check-ins**: 15 min cada mañana
- **Integración**: Lunes y miércoles fusionar cambios
- **Demo final**: Miércoles 22 sep - los 3 presentes

---

*Proyecto: Safe-Sale - Sistema de Gestión de Supermercado*  
*Fecha de entrega: 22 de septiembre de 2026*  
*Integrantes: Lucas (Backend), Jandir (Frontend), Dante (Documentación)*  
*Última actualización: 17 de septiembre de 2026*
