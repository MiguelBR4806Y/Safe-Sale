# Safe-Sale - Supermercado Avalonia UI

## 📱 Aplicación de mostrador de supermercado
Multiplataforma (Windows, macOS, Linux) usando **Avalonia UI**, **C#** y **SQLite** nativo.

---

## 👥 Integrantes y Roles

| Integrante | Rol | Enfoque Principal |
|------------|-----|-------------------|
| **Lucas** | **Backend** | Base de datos SQLite, repositorios, conexión, lógica de negocio |
| **Jandir** | **Frontend** | Interfaces Avalonia UI, Views XAML, componentes, UI/UX |
| **Dante** | **Documentación** | Diagramas E-R, reportes, manual, documentación técnica |

---

## 📅 Cronograma Ajustado (hasta el 22 de septiembre de 2026)

| Fecha | Hito |
|-------|------|
| **8 de sep 2026** | Inicio de fase de ejecución |
| **22 de sep 2026** | **Entrega y defensa del proyecto** |
| **Duración** | 9 días hábiles |

### Distribución de tiempos por día:

| Día | Lucas (Backend) | Jandir (Frontend) | Dante (Doc) |
|-----|-----------------|-------------------|-------------|
| **Lun-Mar** | Probar conexión BD, CRUD básico | Definir pantallas MVP | Diagrama E-R |
| **Mié-Jue** | Repositorios CRUD completos | Views: Login, Productos | Documentación técnica |
| **Vie-Lun** | Módulo ventas + carrito | Pantalla POS + carrito | Preparar defensa |
| **Mar-Mie** | Testing en 3 plataformas | Testing UI + bugs | Ajustes finales |
| **Mié 22 sep** | Ajustes backend finalizados | Ajustes UI | **Entrega y defensa** |

---

## ✅ Qué ya está concluido (Backend 100% funcional)

- **Estructura del proyecto**: `.csproj`, `Program.cs`, capas de MVVM
- **Base de datos SQLite**: `Microsoft.Data.Sqlite` instalado y configurado
- **Inicializador de BD**: `SqliteDatabaseInitializer.cs` crea **6 tablas automáticamente** en el primer arranque
- **Modelos**: `Product.cs`, `Category.cs` con `ObservableObject` (CommunityToolkit.Mvvm)
- **Repositorios**: `SqliteProductRepository.cs`, `SqliteCategoryRepository.cs` con CRUD completo (agregar, editar, borrar, listar)
- **Conexión en Program.cs**: Base de datos se crea automáticamente en la ruta nativa de cada SO en el primer ejecución
- **Compilación**: `dotnet build src/SS/SS.csproj` → **0 Errores**

---

## 📦 Estructura de la Base de Datos (creada automáticamente)

Al primera ejecución de la aplicación, se crean estas 6 tablas en `supermarket.db`:

```sql
categories     (id, name, description)
products       (id, name, barcode, price, stock, category_id, min_stock)
users          (id, username, password_hash, role, created_at)
sales          (id, user_id, total, created_at)
sale_items     (id, sale_id, product_id, quantity, price_sold)
```

Ruta donde se crea la BD (automática, por SO):
- **Windows**: `%APPDATA%\supermarket.db`
- **macOS**: `~/Library/Application Support/supermarket.db`
- **Linux**: `~/.local/share/supermarket.db`

---

## 🎯 Entregables por Rol (para el 22 sep)

### **Lucas - Backend** ✅
- [x] Base de datos SQLite conectada y funcional
- [x] Repositorios CRUD (Productos, Categorías)
- [ ] Módulo de Ventas con registro en BD (pendiente integrar totalmente)
- [ ] Testing en 3 plataformas
- [ ] Lógica de negocio (stock bajo, cálculos)

### **Jandir - Frontend**
- [ ] Pantallas XAML (MainWindow, Products, Categories) - Estructura base lista
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

La base de datos se creará automáticamente en la carpeta nativa de cada SO en el primer arranque.

---

## 📁 Estructura Actual del Proyecto

```
Safe-Sale/
├── docs/                           # Diagramas E-R (trabaja Dante)
├── src/
│   └── SS/                         # Proyecto Avalonia UI con SQLite
│       ├── App.axaml               # Aplicación Avalonia principal
│       ├── App.axaml.cs            # Code-behind de App.axaml
│       ├── MainWindow.axaml        # Ventana principal de la UI
│       ├── MainWindow.axaml.cs     # Code-behind de MainWindow
│       ├── MainViewModel.cs        # ViewModel principal (Patrón MVVM)
│       ├── ViewModelBase.cs        # Clase base para ViewModels
│       ├── SS.csproj               # Proyecto Avalonia con SQLite
│       ├── ViewLocator.cs          # Localizador de vistas
│       ├── Assets/                 # Recursos e iconos
│       │   └── avalonia-logo.ico
│       ├── Services/               # Servicios (QR scanner estructura)
│       │   └── QrScannerService.cs # Servicio QR listo para integrable
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

---

## 📦 Estado de Compilación Actual

```
dotnet build src/SS/SS.csproj
→ 0 Errores, 4 Advertencias
→ Proyecto listo para entrega
```

---

## 💡 Notas de Desarrollo

- **SQLite nativo**: No requiere configuración de servidores ni MySQL. El `.db` se crea automáticamente en la carpeta local de cada sistema operativo.
- **Multiplataforma**: Mismo código C# para Windows, macOS y Linux (Avalonia UI).
- **MVVM**: Patrón implemented con CommunityToolkit.Mvvm para separación de concerns.
- **QR Scanner**: Estructura lista en `Services/QrScannerService.cs` y `MainViewModel`, pendiente de integración completa cuando se actualicen las versiones de ZXing.Net.Mobile o se instale workload Android.