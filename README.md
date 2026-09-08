# Safe-Sale - Supermercado Avalonia UI

## 📱 Aplicación de mostrador de supermercado
Multiplataforma (Windows, macOS, Linux) usando **Avalonia UI**, **C#** y **SQLite** nativo.

---

## 👥 Integrantes y Roles

| Integrante | Rol | Enfoque Principal |
|------------|-----|-------------------|
| **Lucas** | **Backend** | Base de datos SQLite, repositorios, conexión, lógica de negocio, endpoints locales |
| **Jandir** | **Frontend** | Interfaces Avalonia UI, Views XAML, componentes, experiencia de usuario, pantallas |
| **Dante** | **Documentación** | Diagramas E-R, reportes, manual de usuario, documentación técnica, preparación defensa |

---

## 📅 Cronograma Ajustado (9 días - hasta el 22 sep)

### **Día 1-2: Lunes-Martes**
| Tarea | Integrante |
|-------|------------|
| **Configurar y probar conexión BD** - Verificar que `supermarket.db` se crea y las 6 tablas funcionan | **Lucas** |
| **Diseñar diagramas E-R** (validar esquema) | **Dante** |
| **Definir pantallas MVP** - Login, inventario, POS | **Jandir** & **Lucas** |

### **Día 3-4: Miércoles-Jueves**
| Tarea | Integrante |
|-------|------------|
| **Implementar repositorios CRUD** - Productos (agregar/editar/borrar) | **Lucas** |
| **Crear Views XAML** - Formularios de productos, categorías | **Jandir** |
| **Diseñar pantalla Login** con roles admin/cashier | **Jandir** |

### **Día 5-6: Viernes-Lunes**
| Tarea | Integrante |
|-------|------------|
| **Módulo de Ventas** - Carrito, cálculo de totales, registro de venta | **Lucas** (lógica) + **Jandir** (UI) |
| **Conectar ViewModels** con repositorios | **Lucas** |
| **Pantalla de ventas** - Datagrid, campos de pago | **Jandir** |

### **Día 7-8: Martes-Miércoles**
| Tarea | Integrante |
|-------|------------|
| **Testing en las 3 plataformas** - Windows, macOS, Linux | **Lucas** (backend tests) + **Jandir** (UI tests) |
| **Corregir bugs de UI** | **Jandir** |
| **Optimizar queries SQL** | **Lucas** |

### **Día 9: Jueves**
| Tarea | Integrante |
|-------|------------|
| **Preparar documentación de defensa** | **Dante** |
| **Diapositivas y argumentos técnicos** | **Dante** + **Lucas** (backend) |
| **Capturas de pantalla del sistema completo** | **Jandir** |
| **README final y entrega** | **Todos** |

---

## ✅ Qué ya está concluido (Backend base)

- **Estructura del proyecto**: `.csproj`, `Program.cs`, capas de MVVM
- **Base de datos SQLite**: `Microsoft.Data.Sqlite` instalado y configurado
- **Inicializador de BD**: `SqliteDatabaseInitializer.cs` crea 6 tablas automáticamente en el primer arranque
- **Modelos**: `Product.cs`, `Category.cs` con `ObservableObject` (CommunityToolkit.Mvvm)
- **Repositorios**: `SqliteProductRepository.cs`, `SqliteCategoryRepository.cs` con CRUD completo
- **Conexión en Program.cs**: Base de datos se crea automáticamente en la ruta nativa de cada SO

---

## 📦 Estructura de la Base de Datos (creada automáticamente en el arranque)

```sql
Tablas creadas en la primera ejecución:
-- categories (id, name, description)
-- products (id, name, barcode, price, stock, category_id, min_stock)
-- users (id, username, password_hash, role, created_at)
-- sales (id, user_id, total, created_at)
-- sale_items (id, sale_id, product_id, quantity, price_sold)
```

---

## 🎯 Entregables por Rol (para el 22 sep)

### **Lucas - Backend**
- [x] Base de datos SQLite conectada y funcional
- [x] Repositorios CRUD (Productos, Categorías)
- [ ] Módulo de Ventas con registro en BD
- [ ] Pantalla/Login de roles (authentication)
- [ ] Testing en 3 plataformas
- [ ] Lógica de negocio (stock bajo, cálculos)

### **Jandir - Frontend**
- [ ] Pantallas XAML (MainWindow, Products, Categories)
- [ ] Vista de Login con validación de roles
- [ ] Carrito de compras y POS
- [ ] DataGrids y formularios conectados a BD
- [ ] Diseño visual y UX
- [ ] Capturas para documentación

### **Dante - Documentación**
- [ ] Diagrama Entidad-Relación (MER) actualizado
- [ ] README del proyecto (este archivo)
- [ ] Manual de usuario del sistema
- [ ] Diapositivas para la defensa
- [ ] Argumentos técnicos (arquitectura MVVM, SQLite)

---

## 🛠️ Cómo correr el proyecto

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

## 📱 Funcionalidad QR/Code de Barras (Planificada para después)

El proyecto incluye preparación para el escaneo de códigos de barras y QR, esencial para un supermercado. Los componentes están listos para integrarse:

### Paquete NuGet agregado:
- `ZXing.Net.Mobile` - Lectura de códigos multiplataforma

### Servicio creado (estructura lista):
- **`Services/QrScannerService.cs`** - Encapsula la lógica de escaneo
  - Método `ScanAsync()` devuelve el texto del código detectado
  - Soporta QR_CODE, CODE_128, EAN_13, UPC_A
  - Funciona en Windows, macOS, Linux con Avalonia

### ViewModel listo para usar:
- **`ViewModels/MainViewModel.cs`** - Tiene `ScanQrCommand` y `ScannedCode` property
- **`Views/MainWindow.axaml`** - Tiene botón y display para el código escaneado

### Integración futura (cuando ZXing versión sea compatible):
1. El código escaneado sería el `barcode` del producto en la tabla `products`
2. Al escanear, el sistema busca el producto en la BD
3. Si existe: muestra precio, nombre, stock - listo para vender
4. Si no existe: opción para registrar nuevo producto con ese código

### En la UI actual (MainWindow.axaml):
```xml
<Button Content="Escanear Código QR/Barra"
        Command="{Binding ScanQrCommand}"
        HorizontalAlignment="Center" />
<TextBlock Text="Código escaneado:"
           FontSize="14" HorizontalAlignment="Center" />
<TextBlock Text="{Binding ScannedCode}"
           FontSize="24" Foreground="Green" HorizontalAlignment="Center" />
```

---

## 📁 Estructura Actual del Proyecto

```
Safe-Sale/
├── docs/                           # Diagramas E-R (pendirá Dante)
├── src/
│   └── SS/                         # Proyecto Avalonia UI con SQLite
│       ├── App.axaml               # Aplicación Avalonia principal
│       ├── App.axaml.cs            # Code-behind de App.axaml
│       ├── MainWindow.axaml        # Ventana principal de la UI
│       ├── MainWindow.axaml.cs     # Code-behind de MainWindow
│       ├── MainViewModel.cs        # ViewModel principal (Patrón MVVM) - con QR command
│       ├── ViewModelBase.cs        # Clase base para ViewModels
│       ├── SS.csproj               # Proyecto Avalonia con SQLite
│       ├── ViewLocator.cs          # Localizador de vistas
│       ├── Assets/                 # Recursos e iconos
│       │   └── avalonia-logo.ico
│       ├── Services/               # Servicios incluyendo QR (pendiente integración)
│       │   └── QrScannerService.cs # Servicio QR listo para cuando ZXing sea compatible
│       ├── Views/                  # Vistas XAML (trabaja Jandir)
│       │   └── MainWindow.axaml
│       └── ViewModels/             # Lógica de vistas (trabaja Lucas)
│           ├── MainViewModel.cs
│           └── ViewModelBase.cs
├── README.md                       # Documentación (trabaja Dante)
├── requirements.txt
├── run.sh
└── run.ps1
```

---

## 📞 Comunicación y Follow-up

- **Daily check-ins**: 15 min cada mañana para alinear tareas del día
- **Integración**: Los lunes y miércoles fusionar cambios en main
- **Demo final**: Jueves 22 sep - los 3 presentes para defensa

---

*Proyecto: Safe-Sale - Supermercado*  
*Fecha de entrega: 22 de septiembre de 2026*  
*Integrantes: Lucas (Backend), Jandir (Frontend), Dante (Documentación)*