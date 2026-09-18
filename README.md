# Safe-Sale - Sistema de Gestión de Supermercado

## Aplicación Multiplataforma de Mostrador
Sistema punto de venta y gestión de supermercado desarrollado con **Avalonia UI**, **C#** y **SQLite** para Windows, macOS, Linux y Android.

---

## Equipo de Desarrollo

| Integrante | Rol | Responsabilidades |
|------------|-----|-------------------|
| **Lucas** | **Backend** | Base de datos SQLite, repositorios, lógica de negocio, conexión a datos |
| **Jandir** | **Frontend** | Interfaces Avalonia UI, Views XAML, componentes, diseño UI/UX |
| **Dante** | **Documentación** | Diagramas E-R, manual de usuario, documentación técnica, diapositivas |

---

## Estado del Progreso — 18 de Septiembre 2026

### Progreso General: **~99%** Completado

```
Backend:       ████████████████████ 100%
Frontend:      ████████████████████ 100%
Integración:   ████████████████████ 100%
Documentación: ████░░░░░░░░░░░░░░░░  20%
```

### Completado

#### Backend (100%)
- [x] **Base de datos SQLite**: `Microsoft.Data.Sqlite` configurado
- [x] **Inicializador de BD**: `SqliteDatabaseInitializer.cs` crea tablas automaticamente + migracion `payment_method`
- [x] **Modelos**: `Product.cs`, `Category.cs`, `Sale.cs` (con `PaymentMethod`), `SaleItem.cs`, `CartItem.cs`, `User.cs`
- [x] **Repositorios CRUD**: `SqliteProductRepository.cs` (con `GetById`, `GetByBarcode`), `SqliteCategoryRepository.cs`, `SqliteSaleRepository.cs` (con `payment_method`), `SqliteUserRepository.cs`
- [x] **Compilacion**: `dotnet build` → 0 errores, 0 warnings
- [x] **Estructura MVVM**: Separacion de concerns implementada
- [x] **Autenticacion**: Sistema de login con SHA256 + salt, usuario admin por defecto
- [x] **Eliminacion segura**: DELETE con cascade en `sale_items` (no crashea al eliminar productos con ventas)

#### Frontend - Integracion Completa (100%)
- [x] **Navegacion**: Sidebar con 4 secciones (Dashboard, Inventario, Ventas, Registros)
- [x] **MainWindow**: Barra lateral con navegacion MVVM, estilos globales para ListBox/ListBoxItem
- [x] **DashboardView**: Tarjetas KPI modernas con iconos, sombras, colores diferenciados, lista de ventas recientes con diseno de tarjetas
- [x] **InventoryView**: CRUD completo conectado a SQLite, busqueda, estadisticas, escaner de camara PC, escaner movil QR
- [x] **SalesView**: Carrito de compras funcional, busqueda por codigo de barras, escaner de camara PC, escaner movil QR, selector de metodo de pago (Efectivo/Tarjeta/Transferencia), cobro con actualizacion de stock
- [x] **RecordsView**: Filtros modernos con DatePicker, estadisticas con iconos, tabla de registros con badges
- [x] **LoginWindow**: Diseno moderno con fondo decorativo, logo con fondo morado, inputs redondeados, credenciales por defecto en tarjeta
- [x] **EditProductDialog**: Dialogo de edicion con Barcode/Name/Price/Stock/MinStock, validacion
- [x] **Estilos**: Paleta lavanda/lila consistente, tema forzado a Light
- [x] **Logout**: Boton cerrar sesion en sidebar

#### Sistema de Estilos Compartido (100%)
- [x] **Styles.axaml**: Estilos reutilizables para TextBlock, Border, Button, TextBox, ListBox, ComboBox, DatePicker
- [x] **App.axaml**: Recursos centralizados (brushes, colores) en `Application.Resources`
- [x] **Classes consistentes**: `page-title`, `page-subtitle`, `section-title`, `kpi-value`, `kpi-label`, `card-value`, `card-label`, `body`, `caption`, `card`, `card-flat`, `stat-card`, `primary`, `secondary`, `danger`, `success`, `ghost`, `icon`, `separator`
- [x] **StringFormat corregido**: `${0:F2}` → `'{}${0:F2}'` en todas las vistas (RecordsView, InventoryView, SalesView, DashboardView)
- [x] **DatePicker corregido**: `DateTime?` → `DateTimeOffset?` en RecordsViewModel para compatibilidad con `DatePicker.SelectedDate`

#### Escaner Movil (HTTP) (100%)
- [x] **Servidor HTTP embebido**: `MobileScannerService.cs` sirve HTML + QR + endpoints REST
- [x] **QR Code**: Generado con `QRCoder`, muestra URL con IP local y puerto
- [x] **Pagina movil**: HTML/CSS/JS optimizada para telefono con dos pestanas:
  - **Carrito**: Escanea codigo → si existe se agrega al carrito; busqueda de productos existentes con boton "Agregar"
  - **Nuevo**: Escanea codigo → si es nuevo, formulario con Codigo/Nombre/Precio/Stock/StockMinimo
- [x] **Deteccion de codigo**: ZXing-js + BarcodeDetector API + foto del codigo
- [x] **Endpoint `/check`**: Verifica si un producto existe en la BD
- [x] **Endpoint `/products`**: Busca productos por nombre/codigo
- [x] **Endpoint `/scan`**: Recibe codigos y los enruta segun modo (inventory/sales)
- [x] **Modo dual**: El QR cambia entre modo "inventory" y "sales" segun la pantalla activa
- [x] **Polling centralizado**: Un solo timer en `MainViewModel` procesa todos los barcodes
- [x] **Carrito desde movil**: Boton "Agregar" en lista de productos → envia `CART:productId` → se agrega al carrito de la PC

#### Escaner de Camara PC (100%)
- [x] **CameraService**: Captura frames de la camara USB/webcam con hilo seguro
- [x] **Thread-safe StopCapture**: `ManualResetEventSlim` espera a que el loop termine antes de liberar el `VideoCapture` (evita SIGSEGV)
- [x] **Camera enumeration**: `GetAvailableCameras()` detecta hasta 10 dispositivos
- [x] **Camera selection**: ComboBox para seleccionar camara en Inventario y Ventas
- [x] **BarcodeScannerService**: Decodifica codigos de barras con ZXing
- [x] **En Inventario**: Escanea → si existe muestra info, si no abre dialogo de agregar
- [x] **En Ventas**: Escanea → si existe agrega al carrito, si no muestra error
- [x] **Preview en vivo**: Muestra video de camara con marco de escaneo overlay

#### macOS - Permisos de Camara (100%)
- [x] **Info.plist**: `NSCameraUsageDescription` y `NSMicrophoneUsageDescription`
- [x] **Entitlements.plist**: `com.apple.security.device.camera`
- [x] **SS.csproj**: Configuracion macOS con `AppInfoPlistFile`, `CodeSignEntitlements`, `CFBundleIdentifier`

#### Auto-Actualizacion (100%)
- [x] **Velopack**: `UpdateService.cs` busca actualizaciones en GitHub Releases
- [x] **Actualizacion automatica**: Al iniciar sesion, verifica y descarga nueva version
- [x] **Scripts sincronizados**: Ambos scripts (Windows/macOS) leen la version del `.csproj` y generan tags unicos

### Funcionalidades Movil Detalladas

#### Pestana "Carrito" (modo ventas)
- Boton "Escanear en Vivo" → usa camara trasera del telefono
- Boton "Tomar Foto del Codigo" → selecciona imagen de galeria
- Campo codigo de barras (solo lectura, se llena automaticamente)
- **Buscar producto existente**: Campo de busqueda que muestra lista de productos con nombre, codigo, stock, precio y boton "Agregar"
- Al tocar "Agregar" → producto se anade al carrito de la PC en tiempo real

#### Pestana "Nuevo" (modo inventario)
- Boton "Escanear en Vivo" / "Tomar Foto del Codigo"
- Campo codigo de barras (se llena automaticamente)
- Campos: Nombre, Precio, Stock Inicial, Stock Minimo (alerta)
- Boton "Guardar Nuevo" → crea el producto en la BD

### Pendiente

- [ ] **Testing**: Pruebas en Windows, macOS y Linux
- [ ] **Documentacion**: Diagrama E-R, manual de usuario (Dante)
- [ ] **Diapositivas**: Presentacion para defensa

---

## Diseno de Interfaz (UI/UX)

### Paleta de Colores
- **Primario**: Lavanda/Lila (`#E8DAEF`, `#BB8FCE`)
- **Acento**: Púrpura oscuro (`#4A148C`, `#7B1FA2`)
- **Fondo**: Rosa palido (`#FAF3F7`)
- **Exito**: Verde (`#4CAF50`)
- **Advertencia**: Naranja (`#FF9800`)
- **Error**: Rojo (`#EF5350`)

### Pantallas Principales

#### 1. **LoginWindow** - Inicio de Sesion
- Fondo decorativo con formas organicas
- Logo "SS" con fondo morado redondeado
- Inputs con bordes redondeados y focus highlight
- Boton "Iniciar Sesion" con spinner de carga
- Credenciales por defecto: admin / admin123

#### 2. **MainWindow** - Ventana Principal
- Barra lateral (sidebar) con navegacion
- Logo "SS" en la parte superior
- 4 botones de seccion con iconos
- Estilos globales para ListBox transparentes con hover lavanda

#### 3. **DashboardView** - Panel de Control
- Tarjetas KPI con iconos Unicode, sombras, valores grandes
- Colores diferenciados por metrica (purpura, verde, naranja)
- Lista de ultimas ventas con diseno de tarjetas y badges

#### 4. **InventoryView** - Gestion de Inventario
- Barra de busqueda por nombre o codigo
- Estadisticas: Total, Stock bajo, Categorias, Valor total
- Botones de escaner: Camara PC + Movil QR (tamano normal)
- **Selector de camara**: ComboBox para elegir entre camaras disponibles
- Vista previa de camara con marco de escaneo
- QR inline cuando el escaner movil esta activo
- Tabla de productos con ID, Nombre, Codigo, Precio, Stock, Minimo
- Botones: Agregar, Editar, Eliminar, Agregar al Carrito

#### 5. **SalesView** - Punto de Venta
- **Escaner de Camara**: Boton toggle, preview en vivo con marco
- **Selector de camara**: ComboBox para elegir entre camaras disponibles
- **Escaner Movil**: Boton toggle, QR inline, solo productos existentes
- **Busqueda manual**: Campo de codigo de barras + boton Buscar
- **Carrito**: Lista con cantidades (+/-), subtotal, boton eliminar
- **Resumen**: Items, Total, Metodo de pago (Efectivo/Tarjeta/Transferencia)
- **Botones**: COBRAR, Vaciar Carrito

#### 6. **RecordsView** - Historial
- Filtros modernos con DatePicker (DateTimeOffset)
- Estadisticas con iconos: Total Ventas, Transacciones
- Tabla de registros con badges de tipo y totales destacados

#### 7. **EditProductDialog** - Editar Producto
- Campos: Codigo, Nombre, Precio, Stock, Stock Minimo
- Validacion de campos obligatorios
- Guardar con actualizacion en BD

---

## Cronograma (Hasta el 22 de septiembre de 2026)

| Fecha | Hito | Estado |
|-------|------|--------|
| **8 sep 2026** | Inicio de fase de ejecucion | Completado |
| **10 sep** | Backend SQLite funcional | Completado |
| **11 sep** | Integracion Frontend-Backend + Navegacion + Ventas | Completado |
| **14 sep** | Login de usuarios | Completado |
| **15 sep** | QR Scanner + Estilos UI | Completado |
| **16 sep** | Escaner movil HTTP + Carrito desde movil | Completado |
| **17 sep** | Escaner camara PC + Dashboard/Login/Registros mejorados | Completado |
| **18 sep** | Estilos compartidos, camara selection, macOS permissions, bug fixes | Completado |
| **20 sep** | Testing en 3 plataformas | Pendiente |
| **22 sep** | **Entrega y defensa** | Pendiente |

---

## Base de Datos

### Tablas Creadas Automaticamente
```sql
categories     (id, name, description)
products       (id, name, barcode, price, stock, category_id, min_stock)
users          (id, username, password_hash, role, created_at)
sales          (id, user_id, total, payment_method, created_at)
sale_items     (id, sale_id, product_id, quantity, price_sold)
```

### Ruta de la BD (segun SO)
- **Windows**: `%LOCALAPPDATA%\supermarket.db`
- **macOS**: `~/Library/Application Support/supermarket.db`
- **Linux**: `~/.local/share/supermarket.db`

---

## Como Ejecutar

### Windows / macOS / Linux
```bash
# 1. Clonar el repositorio
git clone https://github.com/MiguelBR4806Y/Safe-Sale.git
cd Safe-Sale

# 2. Restaurar paquetes NuGet
dotnet restore

# 3. Compilar el proyecto
dotnet build src/SS/SS.csproj

# 4. Ejecutar la aplicacion
dotnet run --project src/SS/SS.csproj
```

La base de datos se creara automaticamente en el primer arranque.

### Escaner Movil
1. En la app, ve a **Inventario** o **Ventas**
2. Activa **"Escanear con Movil"** → aparece un codigo QR
3. Abre la camara del telefono → escanea el QR
4. Se abre la pagina del escaner en el telefono
5. Usa la pestana **"Carrito"** para agregar productos existentes
6. Usa la pestana **"Nuevo"** para crear productos (solo desde Inventario)

> **Nota**: El telefono y la PC deben estar en la misma red WiFi.

---

## Instalador y Actualizaciones

### Para Usuarios - Instalar la App
1. Ve a [Releases](https://github.com/MiguelBR4806Y/Safe-Sale/releases)
2. Descarga el instalador para tu plataforma:
   - **Windows**: `SS-Installer.exe`
   - **macOS Apple Silicon** (M1/M2/M3/M4): `SS-Installer.pkg`
   - **macOS Intel**: `SS-Installer.pkg`
3. Ejecuta el instalador
4. La app se actualizara automaticamente cuando haya nuevas versiones

### Para Desarrolladores - Crear Instalador

#### Requisitos previos
```bash
# Instalar dotnet tool Velopack
dotnet tool install --global Velopack

# Para publicar en GitHub (opcional, para auto-updates)
# Windows:
winget install GitHub.cli
gh auth login

# macOS:
brew install gh
gh auth login
```

#### Publicar desde Windows
```powershell
# Ejecutar el script de publicacion
.\publish-windows.ps1
```

#### Publicar desde macOS
```bash
# Ejecutar el script de publicacion
chmod +x publish-macos.sh
./publish-macos.sh
```

#### Flujo de actualizacion
```
1. Haces cambios en el codigo
2. Actualizas la version en SS.csproj (<Version>X.Y.Z</Version>)
3. Ejecutas el script de publicacion (Windows o macOS)
4. Los usuarios reciben la actualizacion automaticamente al abrir la app
```

#### Comandos manuales
```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained -o ./publish/win-x64
vpk pack --packId SafeSale --packVersion 1.0.0 --packDir ./publish/win-x64 --mainExe SafeSale.exe

# macOS (desde Mac)
dotnet publish -c Release -r osx-arm64 --self-contained -o ./publish/osx-arm64
dotnet publish -c Release -r osx-x64 --self-contained -o ./publish/osx-x64
vpk pack --packId SafeSale --packVersion 1.0.0 --packDir ./publish/osx-arm64 --mainExe SafeSale
vpk pack --packId SafeSale --packVersion 1.0.0 --packDir ./publish/osx-x64 --mainExe SafeSale
```

---

## Estructura del Proyecto

```
Safe-Sale/
├── src/
│   └── SS/                              # Proyecto principal
│       ├── App.axaml                    # Aplicacion Avalonia (tema Light + recursos)
│       ├── App.axaml.cs
│       ├── Program.cs                   # Punto de entrada (inicia BD)
│       ├── SS.csproj                    # Configuracion del proyecto
│       ├── Styles.axaml                 # Estilos compartidos reutilizables
│       ├── ViewLocator.cs               # Localizador de vistas
│       ├── Models/
│       │   ├── Product.cs               # Modelo de producto
│       │   ├── Category.cs              # Modelo de categoria
│       │   ├── Sale.cs                  # Modelo de venta (con PaymentMethod)
│       │   ├── SaleItem.cs              # Modelo de item de venta
│       │   ├── CartItem.cs              # Modelo de item del carrito
│       │   └── User.cs                  # Modelo de usuario
│       ├── ViewModels/
│       │   ├── ViewModelBase.cs         # Clase base MVVM
│       │   ├── MainViewModel.cs         # Navegacion + polling centralizado
│       │   ├── DashboardViewModel.cs    # Panel de control
│       │   ├── InventoryViewModel.cs    # Gestion de inventario + escaner + camara
│       │   ├── SalesViewModel.cs        # Punto de venta + escaner + camara
│       │   ├── RecordsViewModel.cs      # Historial (con DateTimeOffset)
│       │   ├── LoginViewModel.cs        # Logica de login + auto-update
│       │   └── AboutViewModel.cs        # Acerca de + manual update check
│       ├── Views/
│       │   ├── MainWindow.axaml         # Ventana principal (sidebar)
│       │   ├── MainWindow.axaml.cs
│       │   ├── LoginWindow.axaml        # Ventana de login (diseno moderno)
│       │   ├── LoginWindow.axaml.cs
│       │   ├── DashboardView.axaml      # Panel de control (tarjetas KPI)
│       │   ├── DashboardView.axaml.cs
│       │   ├── InventoryView.axaml      # Gestion de inventario + escaner + selector camara
│       │   ├── InventoryView.axaml.cs
│       │   ├── SalesView.axaml          # Punto de venta + escaner + selector camara
│       │   ├── SalesView.axaml.cs
│       │   ├── RecordsView.axaml        # Historial (diseno moderno)
│       │   ├── RecordsView.axaml.cs
│       │   └── AboutView.axaml          # Acerca de + check updates
│       ├── Dialogs/
│       │   ├── EditProductDialog.axaml  # Editar producto
│       │   └── EditProductDialog.axaml.cs
│       ├── Data/
│       │   ├── AppDatabase.cs           # Ruta compartida de la BD
│       │   ├── SqliteDatabaseInitializer.cs  # Crea tablas + migra columnas
│       │   ├── SqliteProductRepository.cs    # CRUD productos + GetByBarcode
│       │   ├── SqliteCategoryRepository.cs   # CRUD categorias
│       │   ├── SqliteSaleRepository.cs       # CRUD ventas + payment_method
│       │   └── SqliteUserRepository.cs       # CRUD usuarios
│       ├── Services/
│       │   ├── MobileScannerService.cs  # Servidor HTTP + QR + escaner movil
│       │   ├── CameraService.cs         # Captura de camara PC (thread-safe)
│       │   ├── BarcodeScannerService.cs # Decodificacion de codigo de barras
│       │   ├── PasswordHasher.cs        # Hash SHA256 + salt
│       │   ├── UpdateService.cs         # Auto-updates con Velopack + GitHub
│       │   ├── ICameraService.cs        # Interfaz de camara (con enumeracion)
│       │   └── IBarcodeScannerService.cs # Interfaz de escaner
│       ├── Converters/
│       │   └── CameraConverters.cs      # BoolToString, BoolToColor
│       ├── Platforms/
│       │   └── macOS/
│       │       ├── Info.plist           # Permisos de camara macOS
│       │       └── Entitlements.plist   # Camera entitlement
│       ├── www/
│       │   └── zxing.min.js             # ZXing-js para decodificacion
│       └── Assets/
│           └── avalonia-logo.ico
├── docs/                                # Documentacion (Dante)
├── .vscode/
│   ├── launch.json                      # Configuracion de debug
│   └── tasks.json                       # Tareas de build
├── publish-windows.ps1                  # Script de publicacion Windows (lee version del .csproj)
├── publish-macos.sh                     # Script de publicacion macOS (lee version del .csproj)
├── README.md                            # Este archivo
└── requirements.txt                     # Configuracion
```

---

## Guia de Estilo UI

- **Fondo principal**: `#FAF3F7` (Rosa palido)
- **Tarjetas/Paneles**: `#FFFFFF` (Blanco)
- **Bordes/Botones**: `#E8DAEF` (Lavanda claro)
- **Botones acento**: `#BB8FCE` (Lila)
- **Texto primario**: `#4A148C` (Purpura oscuro)
- **Texto secundario**: `#6C3483` (Purpura medio)
- **Numero destacado**: `#7B1FA2` (Purpura vibrante)
- **Exito**: `#4CAF50` (Verde)
- **Advertencia**: `#FF9800` (Naranja)
- **Error**: `#EF5350` (Rojo)
- **Tema**: Forzado a **Light** en `App.axaml`

---

## Notas Tecnicas

- **Smart App Control (SAC)**: En Windows 11 25H2 en modo enforcement, bloquea ejecutables no firmados en Documents/Desktop. Solucion: ejecutar desde `C:\SS`
- **SQLite Vulnerabilidad**: NU1903 suprimida con `<NoWarn>NU1903</NoWarn>` (no hay version parchada de SQLitePCLRaw)
- **Camara en movil**: `navigator.mediaDevices` no disponible en HTTP. Solo funciona con archivo/foto
- **ZXing-js**: `HTMLCanvasElementLuminanceSource` mas confiable que `RGBLuminanceSource` para fotos
- **StringFormat XAML**: Usar `'{}${0:F2}'` en lugar de `${0:F2}` (el `$` antes de `{` es sintaxis C#, no XAML)
- **DatePicker SelectedDate**: Requiere `DateTimeOffset?` en el ViewModel, no `DateTime?`
- **CameraService**: Usa `ManualResetEventSlim` para esperar a que el loop de captura termine antes de liberar `VideoCapture` (evita SIGSEGV en macOS)
- **macOS Camera Permission**: OpenCV maneja el permiso directamente al abrir la camara. Requiere `NSCameraUsageDescription` en `Info.plist`

---

## Comunicacion

- **Daily check-ins**: 15 min cada manana
- **Integracion**: Lunes y miercoles fusionar cambios
- **Demo final**: Miercoles 22 sep - los 3 presentes

---

*Proyecto: Safe-Sale - Sistema de Gestion de Supermercado*  
*Fecha de entrega: 22 de septiembre de 2026*  
*Integrantes: Lucas (Backend), Jandir (Frontend), Dante (Documentacion)*  
*Ultima actualizacion: 18 de septiembre de 2026*
