# Safe-Sale - Sistema de Gestión de Supermercado

## Aplicación Multiplataforma de Mostrador
Sistema punto de venta y gestión de supermercado desarrollado con **Avalonia UI**, **C#** y **MySQL local incluido** para Windows, macOS y Linux.

## Descripción general

Safe-Sale es una aplicación de mostrador que combina **punto de venta**, **inventario**, **caja** y **registros** en un solo sistema local. Está pensada para funcionar sin servidores externos ni configuración de base de datos por parte del usuario.

- **Inventario**: alta, edición, eliminación y búsqueda de productos; stock y stock mínimo; carga inicial de 18 productos de demostración.
- **Ventas**: carrito, búsqueda por nombre o código de barras, métodos de pago y descuento de stock.
- **Caja**: apertura y cierre por turno, efectivo esperado y diferencia de cierre; las ventas se asocian a la caja abierta.
- **Usuarios y roles**: login con contraseña hasheada; **Administrador** gestiona todo, **Cajero** ve inventario en modo lectura y opera ventas/caja.
- **Base de datos**: MySQL local incluido y administrado por la app, con transacciones ACID, triggers de auditoría y una vista de stock bajo.
- **Escáneres**: cámara de la PC y escáner móvil por HTTP/QR.
- **Multiplataforma**: Windows, macOS y Linux; auto-actualización con Velopack.

Para el detalle técnico ver las secciones **Base de Datos**, **Estructura del Proyecto** y la guía de **Presentación en PowerPoint** al final.

### Roles

| Rol | Inventario | Ventas | Caja | Registros |
|-----|-----------|--------|------|-----------|
| **Administrador** | CRUD completo | Si | Si | Si |
| **Cajero** | Solo lectura | Si | Si (su caja) | Si |

### Flujo básico

1. Iniciar sesion con `admin` / `admin123` o registrar un usuario Cajero desde el login.
2. Ir a **Caja** y abrir la caja con el efectivo inicial del turno.
3. **Inventario**: el administrador agrega, edita o elimina productos; el cajero solo consulta.
4. **Ventas**: buscar productos por nombre o codigo, armar el carrito y **COBRAR**.
5. **Caja**: cerrar el turno con el efectivo contado; la app calcula el efectivo esperado y la diferencia.
6. **Registros**: consultar el historial de ventas.

---

## Equipo de Desarrollo

| Integrante | Rol | Responsabilidades |
|------------|-----|-------------------|
| **Lucas** | **Backend** | Base de datos MySQL local, repositorios, lógica de negocio, conexión a datos |
| **Jandir** | **Frontend** | Interfaces Avalonia UI, Views XAML, componentes, diseño UI/UX |
| **Dante** | **Documentación** | Diagramas E-R, manual de usuario, documentación técnica, diapositivas |

---

## Estado del Progreso — 9 de Noviembre 2026

### Progreso General: **~95%** Completado

```
Backend:       ████████████████████ 100%
Frontend:      ████████████████████ 100%
Integracion:   ████████████████████ 100%
Documentacion: ████████░░░░░░░░░░░░  40%
```

### Nuevas Funcionalidades (9 nov 2026) — Ventas, Base de Datos, Roles y Caja

- [x] **Layout de Ventas renovado**: Titulo "VENTAS" centrado, escaneres (camara + movil + codigo manual) compactados en una sola fila superior, franja de KPIs con iconos (Items, Total, Metodo de pago, Ultima venta) y mismo patron visual que Inventario (bandas #F5F0FA, tarjetas con sombra)
- [x] **Busqueda por nombre**: `AutoCompleteBox` con sugerencias de productos (`FilterMode="Contains"`) ademas del codigo de barras, con boton Agregar
- [x] **Codigo de barras con coincidencias**: El campo manual ahora es un `AutoCompleteBox` que muestra "codigo - nombre" de los productos coincidentes; acepta match exacto o por prefijo al confirmar
- [x] **Feedback unificado**: `StatusMessage` con color se muestra solo en el panel de resumen (antes aparecia en 2 lugares); los errores de stock y de producto se muestran ahi
- [x] **Confirmaciones**: `ConfirmDialog` para vaciar el carrito y para confirmar el cobro
- [x] **Limites de stock**: Botones +/- se deshabilitan al llegar al stock disponible (`CanIncrease`/`CanDecrease`); COBRAR y Vaciar Carrito se deshabilitan con carrito vacio; estilo global `Button:disabled` con opacidad para que todo boton deshabilitado se vea apagado
- [x] **Resumen responsivo**: Panel de resumen con `ScrollViewer` (hace scroll al redimensionar la ventana), tiles de Items/Total, header de seccion y feedback con acento lateral
- [x] **Botones renovados**: COBRAR con degradado (#7B1FA2 → #4A148C), sombra, icono y monto dinamico; Vaciar Carrito en estilo outline rojo con hover tinteado
- [x] **Carrito diferenciado**: Header con badge de cantidad, encabezados PRODUCTO/CANT./SUBTOTAL, filas con precio unitario ("c/u"), controles de cantidad en pill, subtotal morado, quitar en rojo suave y estado vacio ilustrado
- [x] **Bug corregido**: Los escaneos moviles en modo Ventas escribian el feedback en el ViewModel de Inventario (`MainViewModel.PollBarcodes`)
- [x] **ACID en el cobro**: procedimiento `sp_registrar_venta` que registra venta + items + descuento de stock en una sola transaccion con `COMMIT`/`ROLLBACK`
- [x] **Objetos de base de datos**: triggers de auditoria (`product_audit`) y vista `vw_stock_bajo`
- [x] **Inventario de demostracion**: 18 productos (2 por categoria) cargados automaticamente si la tabla esta vacia
- [x] **Permisos por rol**: el cajero ve Inventario en modo solo lectura; el administrador mantiene el CRUD completo
- [x] **Caja**: apertura y cierre de caja con monto inicial, ventas asociadas, efectivo esperado y diferencia de cierre
- [x] **Registro publico**: desde el login solo se crean usuarios Cajero (los administradores usan la cuenta admin)
- [x] **Datos copiables**: nombre y codigo de producto seleccionables en Inventario (Ctrl+C); nombre seleccionable en el carrito
- [x] **Branding**: icono de la app en ventanas, login, ejecutable e instaladores; banner de marca en el sidebar

### Nuevas Funcionalidades (27 sep 2026)

- [x] **Busqueda con autocompletado**: El buscador de Inventario usa `AutoCompleteBox` con sugerencias en vivo de los productos (coincidencia en cualquier parte del nombre, `FilterMode="Contains"`), sin nuevas consultas a la base de datos
- [x] **Busqueda 100% en vivo**: El listado y las categorias se filtran al escribir con un debounce de 180ms; se elimino el boton "Buscar" porque quedo redundante
- [x] **Categorias se expanden al buscar**: Con busqueda activa, las categorias con productos coincidentes aparecen expandidas; al limpiar la busqueda o usar "Refrescar" vuelven a estar colapsadas por defecto
- [x] **Auto-expand en modo seleccion**: Al pulsar "Eliminar" o "Agregar al Carrito" todas las categorias visibles se expanden para revisar la seleccion y colapsan al salir del modo (si hay busqueda activa al salir se conserva el estado de la busqueda)
- [x] **Boton "Editar" mas visible**: Estado habilitado con el primario fuerte (#7B1FA2) y hover oscuro (#4A148C), mismos acentos del resto de botones activos; el estado deshabilitado no cambia

### Nuevas Funcionalidades (21 sep 2026)

- [x] **Categorias colapsables**: Cada categoria se expande/contrae individualmente con chevron (▶/▼), estado independiente por categoria, todas colapsadas por defecto
- [x] **Selector de camara**: ComboBox funcional junto a "Iniciar Cámara" en Inventario, reutiliza `AvailableCameras`/`SelectedCameraIndex` del ViewModel
- [x] **Confirmacion al vaciar**: `ConfirmDialog` al presionar "Vaciar Inventario" con mensaje explícito antes de borrar todo
- [x] **Refrescar limpia busqueda**: `LoadData()` resetea `SearchText` y recalcula `CategoryGroups` sin filtro
- [x] **Botones deshabilitados**: Eliminar, Agregar al Carrito y Vaciar Inventario se deshabilitan visualmente cuando `HasProducts=false` (0 productos)
- [x] **Botón Iniciar Cámara mejorado**: Color verde (#4CAF50) consistente con SalesView en vez del lila claro anterior
- [x] **Layout de cámara reorganizado**: Spacing y alineación corregidos, dropdown con `MinWidth="180"` para nombres largos
- [x] **Botón "Rellenar Inventario" eliminado**: Removido junto con toda su lógica demo (`FillDemoDataCommand`, datos hardcodeados)

### Bugs Encontrados y Corregidos (19 sep 2026)

#### Bug 1: Botón "Agregar" no abre diálogo — NullReferenceException
- **Causa raíz**: `FuncDataTemplate<Category>` en `QuickAddProductDialog.axaml.cs:50` recibía `item = null` al renderizar el ComboBox de categorías
- **Síntoma**: Excepción silenciada por `catch {}` vacío en `OnAddProductRequested`
- **Fix**: Null-check en el FuncDataTemplate (`item is null` → retorna TextBlock vacío)

#### Bug 2: Botón "Buscar" no filtra — Binding SearchText no actualiza
- **Causa raíz**: `TextBox.Text` con `Mode=TwoWay` sin `UpdateSourceTrigger` usa `LostFocus` por defecto en Avalonia, causando que el binding no se actualice antes del click del botón
- **Síntoma**: `SearchText` siempre `''` al ejecutar OnSearch
- **Fix**: Agregado `UpdateSourceTrigger=PropertyChanged` al binding del TextBox de búsqueda

#### Bug 3: Búsqueda filtra internamente pero no se refleja en UI
- **Causa raíz**: `RebuildCategoryGroupsWithFilter()` iteraba TODAS las categorías de la BD sin importar si tenían productos coincidentes con el filtro
- **Síntoma**: CategoryGroups siempre mostraba 9 categorías (todas), incluso con búsqueda activa
- **Fix**: Cuando `SearchText` no está vacío, se ocultan categorías sin productos coincidentes

#### Bug 4: ComboBox de categoría no muestra selección
- **Causa raíz**: `FuncDataTemplate` en code-behind + `DisplayMemberBinding = null` causaba que el área de selección del ComboBox no renderizara el item correctamente
- **Síntoma**: Al elegir categoría, el campo quedaba visualmente vacío
- **Fix**: Movido `ItemTemplate` a XAML con `x:DataType="md:Category"`, eliminado `DisplayMemberBinding = null`

#### Bug 5: Botones Editar/Eliminar/Agregar al Carrito no funcionaban
- **Causa raiz**: `ListBox` no tenia `SelectedItems` binding, `EditProductCommand`/`DeleteProductCommand`/`AddToCartCommand` fallaban sin feedback visual
- **Sintoma**: Clic en Editar/Eliminar/Carrito no abria dialogo ni ejecutaba accion
- **Fix**: Removidos diagnósticos, restaurada funcionalidad

### Nuevas Funcionalidades (19 sep 2026)

- [x] **Edicion con seleccion**: Boton Editar abre dialogo precargado con datos del producto seleccionado en la lista
- [x] **Eliminacion multiple**: Checkboxes para seleccionar productos → boton Eliminar borra todos los seleccionados con confirmacion
- [x] **Agregar al carrito multiple**: Checkboxes → boton Agregar al Carrito agrega todos los seleccionados al carrito
- [x] **Colores diferenciados**: Editar (púrpura oscuro), Eliminar (rojo), Agregar al Carrito (verde)
- [x] **Espaciado mejorado**: Cada subseccion de categoria ahora tiene separacion visual clara
- [x] **Categoria en Edicion**: EditProductDialog ahora incluye dropdown de categorias (antes solo tenia Barcode/Nombre/Precio/Stock/MinStock)
- [x] **Dialogo de confirmacion**: ConfirmDialog reutilizable para acciones criticas (Eliminar multiple)

### Nuevas Funcionalidades (18 sep 2026)

- [x] **Vaciar Inventario**: Botón "Vaciar Inventario" con `ClearInventoryCommand` que borra todos los productos (conserva categorías)
- [x] **Encabezados de columna**: Cada categoría muestra etiquetas (Producto, Código, Precio, Stock) antes de listar productos
- [x] **DeleteAll en repositorio**: Método `ProductRepository.DeleteAll()` que limpia `sale_items` y `products`

### Completado

#### Backend (100%)
- [x] **Base de datos MySQL local**: `MySqlConnector` + motor MySQL incluido y gestionado por la app (sin configuración del usuario)
- [x] **Inicializador de BD**: `DatabaseInitializer.cs` crea el esquema automaticamente + migraciones
- [x] **Modelos**: `Product.cs`, `Category.cs`, `Sale.cs` (con `PaymentMethod`), `SaleItem.cs`, `CartItem.cs`, `User.cs`, `CashSession.cs`
- [x] **Repositorios CRUD**: `ProductRepository.cs` (con `GetById`, `GetByBarcode`, `DeleteAll`), `CategoryRepository.cs`, `SaleRepository.cs` (con `payment_method`), `UserRepository.cs`, `CashSessionRepository.cs` (apertura/cierre y totales)
- [x] **Objetos de BD**: `sp_registrar_venta` transaccional (ACID + rollback), triggers de auditoría y vista `vw_stock_bajo`
- [x] **Validaciones**: `InputValidator.cs` (precio con punto decimal, stock ≥ 1, montos) y `UserValidator.cs` (usuario/contraseña)
- [x] **Compilacion**: `dotnet build` → 0 errores, 0 warnings
- [x] **Estructura MVVM**: Separacion de concerns implementada
- [x] **Autenticacion**: Sistema de login con SHA256 + salt, usuario admin por defecto
- [x] **Eliminacion segura**: DELETE con cascade en `sale_items` (no crashea al eliminar productos con ventas)

#### Frontend - Integracion Completa (100%)
- [x] **Navegacion**: Sidebar con 5 secciones (Dashboard, Inventario, Ventas, Caja, Registros)
- [x] **MainWindow**: Barra lateral con navegacion MVVM, estilos globales para ListBox/ListBoxItem
- [x] **DashboardView**: Tarjetas KPI modernas con iconos, sombras, colores diferenciados, lista de ventas recientes con diseno de tarjetas
- [x] **InventoryView**: CRUD completo conectado a MySQL local (el rol Cajero lo ve en modo solo lectura), busqueda en vivo con autocompletado y filtrado por categoria, estadisticas, escaner de camara PC, escaner movil QR, encabezados de columna, categorias colapsables, selector de camara, boton vaciar inventario con confirmacion, botones deshabilitados cuando inventario vacio
- [x] **SalesView**: Rediseño completo — titulo VENTAS, KPIs con iconos, busqueda por nombre con autocompletado, codigo de barras con coincidencias, carrito con encabezados y estado vacio, resumen con scroll responsivo y botones renovados, confirmaciones de cobro/vaciado, escaner de camara PC, escaner movil QR, selector de metodo de pago (Efectivo/Tarjeta/Transferencia), cobro con actualizacion de stock
- [x] **RecordsView**: Filtros modernos con DatePicker, estadisticas con iconos, tabla de registros con badges
- [x] **LoginWindow**: Diseno moderno con fondo decorativo, logo con fondo morado, inputs redondeados, credenciales por defecto en tarjeta y enlace para **registrar usuarios nuevos** (siempre como Cajero)
- [x] **EditProductDialog**: Dialogo de edicion con Barcode/Name/Price/Stock/MinStock, validacion
- [x] **Estilos**: Paleta lavanda/lila consistente, tema forzado a Light
- [x] **Logout**: Boton cerrar sesion en sidebar

#### Sistema de Estilos Compartido (100%)
- [x] **Styles.axaml**: Estilos reutilizables para TextBlock, Border, Button (incluye `checkout` y `danger-outline`), TextBox, AutoCompleteBox, ListBox, ComboBox, DatePicker
- [x] **App.axaml**: Recursos centralizados (brushes, colores, degradados `CheckoutGradient`) en `Application.Resources`
- [x] **Classes consistentes**: `page-title`, `page-subtitle`, `section-title`, `kpi-value`, `kpi-label`, `card-value`, `card-label`, `body`, `caption`, `card`, `card-flat`, `stat-card`, `primary`, `secondary`, `secondary.edit`, `danger`, `danger-outline`, `success`, `ghost`, `checkout`, `icon`, `separator`
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
- [x] **Scripts sincronizados**: Los tres scripts (Windows/macOS/Linux) leen la version del `.csproj` y generan tags unicos

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

- [x] **Testing Windows**: build, motor MySQL, esquema, ACID, cajas, roles e iconos verificados
- [ ] **Testing macOS/Linux**: ejecutar con los binarios de MySQL de cada plataforma y validar dependencias nativas
- [ ] **Documentacion**: Diagrama E-R y manual de usuario (Dante)
- [x] **Guia de presentacion**: instrucciones completas agregadas al final de este README
- [ ] **Diapositivas**: armar el `.pptx` final para la defensa

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
- Logo institucional (`Assets/SS-ico.png`)
- Inputs con bordes redondeados y focus highlight
- Boton "Iniciar Sesion" con spinner de carga
- Credenciales por defecto: admin / admin123

#### 2. **MainWindow** - Ventana Principal
- Barra lateral (sidebar) con navegacion y banner de marca (`SS-general.jpg`)
- 5 botones de seccion con iconos: Dashboard, Inventario, Ventas, Caja y Registros
- Tarjeta de usuario con nombre y rol; boton de cerrar sesion
- Estilos globales para ListBox transparentes con hover lavanda

#### 3. **DashboardView** - Panel de Control
- Tarjetas KPI con iconos Unicode, sombras, valores grandes
- Colores diferenciados por metrica (purpura, verde, naranja)
- Lista de ultimas ventas con diseno de tarjetas y badges

#### 4. **InventoryView** - Gestion de Inventario
- Busqueda en vivo por nombre o codigo con sugerencias de autocompletado mientras se escribe (sin boton "Buscar"; resultado filtrado al dejar de teclear)
- Estadisticas: Total, Stock bajo, Categorias, Valor total
- Botones de escaner: Camara PC + Movil QR (tamano normal)
- Selector de camara: ComboBox con label "Cámara:" alineado horizontalmente, `MinWidth="180"`, botón "Iniciar Cámara" en verde (#4CAF50)
- Vista previa de camara con marco de escaneo
- QR inline cuando el escaner movil esta activo
- Categorias colapsables: chevron (▶/▼) por categoria, click en header alterna visibilidad de productos; se expanden solas al buscar y al entrar en modo seleccion (Eliminar/Carrito), y regresan a colapsadas al limpiar la busqueda o salir del modo seleccion
- Tabla de productos con checkboxes (modo seleccion activado por Editar/Eliminar/Carrito)
- Botones deshabilitados visualmente cuando inventario vacio: Eliminar, Agregar al Carrito, Vaciar Inventario
- Botones de accion se iluminan segun el modo seleccionado

#### 5. **SalesView** - Punto de Venta
- **Titulo**: "VENTAS" centrado con fondo rosa palido y tarjetas blancas con sombra (mismo patron que Inventario)
- **KPIs**: Items, Total, Metodo de pago y Ultima venta con iconos
- **Escaner de Camara**: Selector de camara + boton toggle (verde/rojo), preview en vivo con marco de escaneo
- **Escaner Movil**: Boton toggle, QR inline, solo productos existentes
- **Busqueda por nombre**: `AutoCompleteBox` con sugerencias de productos + boton Agregar
- **Busqueda manual**: `AutoCompleteBox` de codigo de barras que muestra coincidencias "codigo - nombre" + boton Buscar
- **Carrito**: Header con badge de cantidad, encabezados de columna (PRODUCTO/CANT./SUBTOTAL), filas con precio unitario, cantidades (+/-) con limites de stock, subtotal y boton quitar; estado vacio ilustrado
- **Resumen**: Panel con scroll responsivo al redimensionar la ventana, tiles de Items/Total, Metodo de pago (Efectivo/Tarjeta/Transferencia), feedback unificado con color
- **Botones**: COBRAR con degradado, sombra y monto dinamico; Vaciar Carrito en outline rojo; ambos con `ConfirmDialog` y deshabilitados cuando el carrito esta vacio

#### 6. **CashSessionView** - Caja
- Estado de la caja: abierta/cerrada, apertura, ventas en efectivo, total de ventas y efectivo esperado.
- **Abrir caja**: monto inicial del turno.
- **Cerrar caja**: monto contado al cierre; calcula la diferencia contra el efectivo esperado.
- Historial de cajas (todas para administrador, propias para cajero).
- Las ventas se asocian a la caja abierta y el cobro se bloquea si no hay caja.

#### 7. **RecordsView** - Historial
- Filtros modernos con DatePicker (DateTimeOffset)
- Estadisticas con iconos: Total Ventas, Transacciones
- Tabla de registros con badges de tipo y totales destacados

#### 8. **EditProductDialog** - Editar Producto
- Campos: Codigo, Nombre, Precio, Categorias (dropdown), Stock, Stock Minimo
- Validacion de campos obligatorios
- Guardar con actualizacion en BD

---

## Cronograma original (referencia histórica)

> Fechas planificadas originalmente. El proyecto continuó después con MySQL local, roles, caja, ACID y documentación; el estado actual está en la sección **Pendiente**.

| Fecha | Hito | Estado |
|-------|------|--------|
| **8 sep 2026** | Inicio de fase de ejecucion | Completado |
| **10 sep** | Backend MySQL local funcional | Completado |
| **11 sep** | Integracion Frontend-Backend + Navegacion + Ventas | Completado |
| **14 sep** | Login de usuarios | Completado |
| **15 sep** | QR Scanner + Estilos UI | Completado |
| **16 sep** | Escaner movil HTTP + Carrito desde movil | Completado |
| **17 sep** | Escaner camara PC + Dashboard/Login/Registros mejorados | Completado |
| **18 sep** | Estilos compartidos, camara selection, macOS permissions, bug fixes | Completado |
| **20 sep** | Testing en 3 plataformas | Parcial: Windows OK; macOS/Linux pendiente |
| **22 sep** | **Entrega y defensa** | Pendiente (fecha a confirmar) |

---

## Base de Datos

### MySQL local, sin configuración

La app incluye un motor **MySQL 8 Community** y lo administra sola:

- En el primer arranque crea el datadir, elige un puerto **loopback** (`127.0.0.1`), genera credenciales internas, crea la base `safesale` y el usuario de la app.
- El usuario final **no** instala MySQL, no escribe credenciales ni edita archivos de configuración: solo usa el login de la app.
- El motor y los datos quedan **en la máquina local**; no hay servidor remoto ni necesidad de internet.
- Al cerrar la app, el motor local se apaga de forma ordenada.

Datos locales (según SO, dentro del perfil del usuario):

- **Windows**: `%LOCALAPPDATA%\Safe-Sale\mysql\data`
- **macOS**: `~/Library/Application Support/Safe-Sale/mysql/data`
- **Linux**: `~/.local/share/Safe-Sale/mysql/data`

Logs: `%LOCALAPPDATA%\Safe-Sale\logs` (o equivalente).

### Tablas Creadas Automaticamente
```sql
categories     (id, name, icon, color)
products       (id, name, barcode, price, stock, category_id, min_stock)
users          (id, username, password_hash, role, created_at)
cash_sessions  (id, user_id, opened_at, closed_at, opening_amount, closing_amount,
                cash_sales, total_sales, expected_cash, difference)
sales          (id, user_id, cash_session_id, total, payment_method, created_at)
sale_items     (id, sale_id, product_id, quantity, price_sold)
product_audit  (id, product_id, action, old_price, new_price, old_stock, new_stock, changed_at)
```

### Objetos de Base de Datos
- **Procedimiento `sp_registrar_venta`**: exige una caja abierta del usuario; inserta la venta (con `cash_session_id`), sus items y descuenta stock dentro de una transaccion; ante cualquier error hace `ROLLBACK` (probado con stock insuficiente y caja cerrada).
- **Triggers de auditoria**: `trg_products_audit_insert`, `trg_products_audit_update` y `trg_products_audit_delete` registran cambios de precio/stock en `product_audit`.
- **Vista `vw_stock_bajo`**: productos con `stock <= min_stock`, usada por el repositorio.
- **Seed de productos**: 2 productos por cada una de las 9 categorias (18 en total), solo si `products` esta vacia.

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

La base de datos local se crea y configura automaticamente en el primer arranque. Para desarrollo, si no tenés el motor en `vendor/mysql`, ejecutá `scripts/fetch-mysql-vendor.ps1` (Windows) o `scripts/fetch-mysql-vendor.sh` (macOS/Linux), o definí `SAFESALE_MYSQL_BASEDIR`.

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
   - **macOS Apple Silicon** (M1/M2/M3/M4): `SS-Installer-arm64.pkg`
   - **macOS Intel**: `SS-Installer-Intel.pkg`
   - **Linux x64**: `SS-Installer.AppImage` (ejecutar con `chmod +x SS-Installer.AppImage && ./SS-Installer.AppImage`)
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

#### Publicar desde Linux
```bash
# Ejecutar el script de publicacion
chmod +x publish-linux.sh
./publish-linux.sh
```

Cada plataforma publica en su propio canal (`win`, `osx-arm64`, `osx-x64`, `linux-x64`) para que el auto-update descargue el paquete correcto sin colisiones entre arquitecturas.

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
│       ├── Program.cs                   # Punto de entrada (App inicia el MySQL local)
│       ├── SS.csproj                    # Configuracion del proyecto
│       ├── Styles.axaml                 # Estilos compartidos reutilizables
│       ├── ViewLocator.cs               # Localizador de vistas
│       ├── Models/
│       │   ├── Product.cs               # Modelo de producto
│       │   ├── Category.cs              # Modelo de categoria
│       │   ├── Sale.cs                  # Modelo de venta (con PaymentMethod)
│       │   ├── SaleItem.cs              # Modelo de item de venta
│       │   ├── CartItem.cs              # Modelo de item del carrito
│       │   ├── CashSession.cs           # Modelo de caja (apertura/cierre)
│       │   └── User.cs                  # Modelo de usuario
│       ├── ViewModels/
│       │   ├── ViewModelBase.cs         # Clase base MVVM
│       │   ├── MainViewModel.cs         # Navegacion + polling centralizado
│       │   ├── DashboardViewModel.cs    # Panel de control
│       │   ├── InventoryViewModel.cs    # Gestion de inventario + escaner + camara
│       │   ├── SalesViewModel.cs        # Punto de venta + escaner + camara
│       │   ├── CashSessionViewModel.cs  # Caja: apertura, cierre e historial
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
│       │   ├── CashSessionView.axaml    # Caja: apertura/cierre e historial
│       │   ├── CashSessionView.axaml.cs
│       │   ├── RegisterWindow.axaml     # Registro de usuarios (Cajero)
│       │   ├── RegisterWindow.axaml.cs
│       │   ├── RecordsView.axaml        # Historial (diseno moderno)
│       │   ├── RecordsView.axaml.cs
│       │   └── AboutView.axaml          # Acerca de + check updates
│       ├── Dialogs/
│       │   ├── EditProductDialog.axaml  # Editar producto
│       │   ├── EditProductDialog.axaml.cs
│       │   ├── QuickAddProductDialog.axaml  # Alta rapida de producto
│       │   ├── QuickAddProductDialog.axaml.cs
│       │   ├── ConfirmDialog.axaml      # Dialogo de confirmacion
│       │   └── ConfirmDialog.axaml.cs
│       ├── Data/
│       │   ├── DatabaseConfig.cs            # Cadena de conexión interna (sin configuración de usuario)
│       │   ├── DatabaseInitializer.cs       # Crea tablas + migra columnas
│       │   ├── DbErrors.cs                  # Detección de duplicados (1062)
│       │   ├── ProductRepository.cs         # CRUD productos + GetByBarcode
│       │   ├── CategoryRepository.cs        # CRUD categorias
│       │   ├── SaleRepository.cs            # CRUD ventas + payment_method
│       │   ├── CashSessionRepository.cs     # Cajas: apertura, cierre y totales
│       │   └── UserRepository.cs            # CRUD usuarios
│       ├── Services/
│       │   ├── MySqlServerManager.cs    # Arranque/apagado y aprovisionamiento del MySQL local
│       │   ├── CredentialStore.cs       # Credenciales internas generadas automáticamente
│       │   ├── AppPaths.cs              # Rutas locales (datadir, logs, secretos)
│       │   ├── MobileScannerService.cs  # Servidor HTTP + QR + escaner movil
│       │   ├── CameraService.cs         # Captura de camara PC (thread-safe)
│       │   ├── BarcodeScannerService.cs # Decodificacion de codigo de barras
│       │   ├── PasswordHasher.cs        # Hash SHA256 + salt
│       │   ├── InputValidator.cs        # Validacion de precio/stock/montos
│       │   ├── UserValidator.cs         # Validacion de usuario/contrasena
│       │   ├── StartupState.cs          # Estado de error de arranque
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
│           ├── SS-ico.ico        # Icono del ejecutable/instalador Windows
│           ├── SS-ico.png        # Icono de ventanas/login y AppImage Linux
│           ├── SS-general.jpg    # Banner de marca del sidebar
│           └── avalonia-logo.ico # Icono legado
├── docs/                                # Documentacion (Dante) + mysql-local.md
├── scripts/
│   ├── fetch-mysql-vendor.ps1           # Descarga el motor MySQL para Windows
│   └── fetch-mysql-vendor.sh            # Descarga el motor MySQL para macOS/Linux
├── .vscode/
│   ├── launch.json                      # Configuracion de debug
│   └── tasks.json                       # Tareas de build
├── publish-windows.ps1                  # Script de publicacion Windows (lee version del .csproj)
├── publish-macos.sh                     # Script de publicacion macOS (arm64 + Intel)
├── publish-linux.sh                     # Script de publicacion Linux (AppImage)
├── tests/SS.Tests/                      # Pruebas unitarias de validacion de entrada
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
- **MySQL local incluido**: el instalador pesa más porque incluye el motor. La app lo inicia en `127.0.0.1`, con datadir y credenciales internas propias; el usuario no configura nada.
- **Camara en movil**: `navigator.mediaDevices` no disponible en HTTP. Solo funciona con archivo/foto
- **ZXing-js**: `HTMLCanvasElementLuminanceSource` mas confiable que `RGBLuminanceSource` para fotos
- **StringFormat XAML**: Usar `'{}${0:F2}'` en lugar de `${0:F2}` (el `$` antes de `{` es sintaxis C#, no XAML). Ademas, el compilador de bindings NO acepta espacios dentro de `StringFormat` (error `AVLN2000: Unable to resolve type 0`) — para textos con espacios usar propiedades calculadas en el ViewModel (ej. `CheckoutLabel`, `CartCountLabel`, `PriceLabel`)
- **DatePicker SelectedDate**: Requiere `DateTimeOffset?` en el ViewModel, no `DateTime?`
- **CameraService**: Usa `ManualResetEventSlim` para esperar a que el loop de captura termine antes de liberar `VideoCapture` (evita SIGSEGV en macOS)
- **macOS Camera Permission**: OpenCV maneja el permiso directamente al abrir la camara. Requiere `NSCameraUsageDescription` en `Info.plist`

---

## Presentacion en PowerPoint (instrucciones para el compañero)

### Objetivo
- Presentación **corta**: 8 a 10 diapositivas, **8 a 10 minutos**.
- Formato **16:9**, archivo `.pptx` y una copia en PDF por si falla la notebook.
- Público: docentes de la defensa. Priorizar: **base de datos, lógica, reglas de negocio y código real**.
- No convertir las diapositivas en documentación: una idea por slide, bullets cortos, capturas grandes.

### Diseno (mismo lenguaje visual que la app)
- Fondo: `#FAF3F7`. Tarjetas/paneles: `#FFFFFF`. Bordes: `#E8DAEF`.
- Acentos: lila `#BB8FCE`, violeta `#7B1FA2`, purpura `#4A148C`, texto medio `#6C3483`.
- Estados: verde `#4CAF50`, naranja `#FF9800`, rojo `#EF5350`.
- Tipografia: **Segoe UI** (o Inter). Titulos 30-36 pt, cuerpo 18-22 pt, codigo 14-18 pt.
- Logo: usar `src/SS/Assets/SS-general.jpg` en la portada y `src/SS/Assets/SS-ico.png` como ícono/footer.
- Tarjetas blancas con esquinas redondeadas y sombra suave, igual que las vistas de la app.
- No mostrar contraseñas reales, `secrets.json` ni cadenas de conexión en las capturas.

### Estructura sugerida (10 slides)
| # | Slide | Contenido | Tiempo |
|---|-------|-----------|--------|
| 1 | Portada | Logo Safe-Sale, nombre del sistema, integrantes y roles | 30 s |
| 2 | Problema y objetivo | Punto de venta + gestión de inventario, local, sin configuracion | 45 s |
| 3 | Arquitectura | Avalonia UI + C# + MVVM + MySQL local incluido | 1 min |
| 4 | Base de datos | Modelo de tablas, relaciones y restricciones | 1.5 min |
| 5 | Objetos de BD | `sp_registrar_venta`, triggers de auditoría, `vw_stock_bajo` | 1.5 min |
| 6 | ACID y rollback | Flujo de cobro transaccional; qué pasa si falla | 1 min |
| 7 | Roles y reglas | Admin vs Cajero, validaciones y reglas de negocio | 1.5 min |
| 8 | Caja | Apertura/cierre, efectivo esperado y diferencia | 1 min |
| 9 | Código real | 2 o 3 fragmentos cortos y legibles | 1.5 min |
| 10 | Pruebas y cierre | 67 tests, build limpio, conclusión y gracias | 45 s |

> Si hay que recortar, unir 5+6 y 8+9. No bajar de 8 slides ni superar 10 minutos.

### Contenido minimo por slide

**Slide 4 - Base de datos.** Mostrar el diagrama simple:
- `categories` 1—N `products`
- `users` 1—N `sales` y 1—N `cash_sessions`
- `sales` 1—N `sale_items`
- `products` 1—N `sale_items`
- `product_audit` (sin FK, guarda historial)
Mencionar: PK autoincrementales, `UNIQUE` en usuario/código de barras, `FOREIGN KEY`, `CHECK` del rol y la columna `sales.cash_session_id`.

**Slide 5 - Objetos de base de datos.**
- `sp_registrar_venta`: valida caja abierta, stock, calcula total, inserta venta/items, descuenta stock.
- Triggers `trg_products_audit_insert/update/delete`: registran cambios de precio/stock en `product_audit`.
- Vista `vw_stock_bajo`: productos con `stock <= min_stock`.
- Seed: 18 productos (2 por categoría) solo si `products` está vacía.

**Slide 6 - ACID.** Explicar el flujo de `sp_registrar_venta`:
`START TRANSACTION` → validaciones → insert venta → insert items → update stock → update caja → `COMMIT`.
Ante error: `EXIT HANDLER` → `ROLLBACK` → `RESIGNAL`. Mostrar que una venta sin caja o sin stock no deja nada a medias.

**Slide 7 - Roles y reglas.**
- `admin`: CRUD completo de inventario, ventas, cajas, registros.
- `cajero`: inventario **solo lectura**, ventas y caja.
- Registro desde login: solo crea Cajero. Admin por defecto: `admin` / `admin123`.
- Precio: solo punto decimal, mayor a 0, hasta 2 decimales.
- Stock: entero mayor o igual a 1. Stock mínimo: entero mayor o igual a 0.
- Código de barras y usuario únicos; duplicado muestra alerta.
- Venta exige caja abierta del usuario; si no, se bloquea.
- Caja: una abierta por usuario; al cerrar se calcula efectivo esperado = apertura + ventas en efectivo, y diferencia = cierre − esperado.

**Slide 9 - Código real.** Mostrar 2 o 3 fragmentos de 6 a 10 lineas, con la ruta del archivo en el pie:
1. `src/SS/Data/DatabaseInitializer.cs` → fragmento del `sp_registrar_venta` (SIGNAL/ROLLBACK).
2. `src/SS/Data/SaleRepository.cs` → `RegisterSale(...)` llamando al procedimiento.
3. `src/SS/Data/CashSessionRepository.cs` → `OpenSession`/`CloseSession` con transacción.
4. Opcional: `src/SS/ViewModels/InventoryViewModel.cs` → guards de `CanManageInventory`.
5. Opcional: `src/SS/Services/MySqlServerManager.cs` → arranque local (`--no-defaults`, `127.0.0.1`, datadir propio).
No pegar archivos completos; resaltar solo la línea importante.

### Capturas recomendadas
- Login.
- Dashboard.
- Inventario como **admin** y como **cajero** lado a lado (se ve la diferencia de permisos).
- Ventas con caja abierta y carrito cargado.
- Caja abierta y caja cerrada con diferencia.
- MySQL Workbench:
  - `SHOW TABLES;`
  - `SHOW CREATE PROCEDURE sp_registrar_venta;`
  - `SHOW TRIGGERS;`
  - `SELECT * FROM vw_stock_bajo;`
  - `SELECT * FROM product_audit;`
- Opcional: diagrama E-R hecho en Workbench (`Database → Reverse Engineer`).

### Guion corto para el expositor
1. "Safe-Sale es un punto de venta y gestión de supermercado, local y sin configuración de base de datos."
2. "Usamos Avalonia + C# con MVVM y un MySQL local que la app levanta, configura y apaga sola."
3. "El núcleo es la base: tablas normalizadas, restricciones y un procedimiento que cobra de forma atómica."
4. "El cajero solo consulta inventario y opera ventas/caja; el administrador gestiona todo."
5. "Cada venta exige una caja abierta; si algo falla, rollback y no queda stock ni venta a medias."
6. "Todo está validado con pruebas y build limpio."

### Checklist antes de exponer
- [ ] 8-10 slides, 16:9, PDF de respaldo.
- [ ] Capturas sin credenciales ni datos sensibles.
- [ ] Fragmentos de código legibles (fuente grande, pocas lineas).
- [ ] Diagrama de BD claro y con relaciones.
- [ ] Demo de respaldo grabada (video/GIF) por si falla la app.
- [ ] Ensayo cronometrado una vez.
- [ ] Preguntas preparadas: ¿por qué MySQL local?, ¿por qué no SQLite?, ¿cómo garantizan ACID?, ¿cómo se aplican los roles?, ¿qué pasa si se corta la luz?

---

## Comunicacion

- **Daily check-ins**: 15 min cada manana
- **Integracion**: Lunes y miercoles fusionar cambios
- **Demo final**: fecha a confirmar - los 3 presentes

---

*Proyecto: Safe-Sale - Sistema de Gestion de Supermercado*  
*Fecha de entrega: a confirmar*  
*Integrantes: Lucas (Backend), Jandir (Frontend), Dante (Documentacion)*  
*Ultima actualizacion: 9 de noviembre de 2026*
