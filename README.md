## 📁 Estructura del Proyecto

```
Save-Sale/
├── docs/                           # Diagramas E-R, prototipos de Figma y documentación
├── scripts/                        # Scripts SQL de base de datos (schema.sql, seed.sql, SPs)
└── src/                            # Código fuente en .NET
    ├── SaveSale.sln                # Solución principal de .NET
    │
    ├── SaveSale.Shared/            # Biblioteca de Clases (Compartida)
    │   ├── Models/                 # Entidades de base de datos (Venta.cs, Producto.cs, etc.)
    │   └── DTOs/                   # Objetos de transferencia de datos para la API
    │
    ├── SaveSale.Api/               # Backend REST (Minimal API .NET)
    │   ├── Endpoints/              # Rutas REST (VentasEndpoints.cs, ProductosEndpoints.cs)
    │   ├── Data/                   # Configuración y conexión a MySQL
    │   ├── Services/               # Lógica de negocio y Stored Procedures
    │   └── Program.cs              # Punto de entrada del servidor API
    │
    ├── SaveSale.SharedUI/          # Vistas e Interfaz de Usuario (Avalonia UI)
    │   ├── Views/                  # Vistas XAML (LoginView, PosView, InventarioView)
    │   ├── ViewModels/             # Lógica de las vistas en C# (Patrón MVVM)
    │   └── Services/               # Cliente HTTP (HttpClient) para consumir la API
    │
    ├── SaveSale.Desktop/           # Aplicación Ejecutable de Escritorio
    │   └── Program.cs              # Inicializador para Windows, macOS y Linux
    │
    ├── SaveSale.Web/               # [Opcional] Compilación para WebAssembly (WASM)
    └── SaveSale.Android/           # [Opcional] Compilación móvil para Android (APK)
```

---

## 🔄 Fase Actual de Desarrollo

**🟢 Actualmente en: Fase 1 (Planificación, Arquitectura y Prototipado)**  
*(1 de Septiembre al 13 de Septiembre de 2026)*

---

## 🚀 Fases de Desarrollo (Fecha Límite: 30 de Octubre, 2026)

```
[Fase 1: Diseño] ──> [Fase 2: BD & API] ──> [Fase 3: Inventario] ──> [Fase 4: POS] ──> [Fase 5: Roles/Reportes] ──> [Fase 6: QA & Defensa]
  (Sep 01-13)          (Sep 14-20)          (Sep 21-27)           (Sep 28-Oct 11)        (Oct 12-18)            (Oct 19-30)
```

### **Fase 1: Planificación, Arquitectura y Prototipado (Sep 1 – Sep 13)**
* **Integrante 1:** Levanta requerimientos técnicos. Modela y genera el diagrama Entidad-Relación (MER) formal y el esquema relacional en la carpeta docs/.
* **Integrante 2:** Configura la estructura de la solución multi-proyecto (SaveSale.sln), definición de carpetas, dependencias entre proyectos y configuración base de MVVM.
* **Integrante 3:** Diseña la experiencia de usuario (UX) e interfaces de pantalla en **Figma**: Login, Dashboard principal, Formulario de productos y la caja de cobro (POS).
* **📌 Entregable de Fase:** Diagrama E-R aprobado, bocetos de Figma validados y estructura base inicializada en el repositorio de GitHub.

### **Fase 2: Base de Datos y Servicios Base (Sep 14 – Sep 20)**
* **Integrante 1:** Ejecución y publicación del script schema.sql en MySQL. Creación de Stored Procedures iniciales (sp_Login, sp_ObtenerProductos).
* **Integrante 2:** Configuración de la capa de conexión a base de datos y desarrollo de los primeros endpoints en la Minimal API (SaveSale.Api).
* **Integrante 3:** Elaboración y prueba del script de carga masiva (seed.sql) con categorías, productos reales y usuarios de prueba.
* **📌 Entregable de Fase:** Base de datos activa con datos de prueba cargados y cliente C# realizando consultas exitosas a MySQL.

### **Fase 3: Módulo de Gestión de Inventario (Sep 21 – Sep 27)**
* **Integrante 1:** Creación de Stored Procedures para Alta, Baja y Modificación de productos y categorías con reglas de validación de negocio.
* **Integrante 2:** Desarrollo de los endpoints HTTP CRUD y creación de las pantallas XAML en Avalonia UI (DataGrid, formularios de productos).
* **Integrante 3:** Construcción de la interfaz de gestión de categorías y primera ronda de pruebas QA para validar campos y formularios de inventario.
* **📌 Entregable de Fase:** Módulo de Inventarios 100% funcional capaz de crear, listar, editar y desactivar productos desde la interfaz.

### **Fase 4: Desarrollo del Punto de Venta (POS) (Sep 28 – Oct 11)**
* **Integrante 1:** Implementación del Stored Procedure transaccional sp_RegistrarVenta (START TRANSACTION...COMMIT) asegurando la atomicidad del cobro y el descuento automático de stock con control de concurrencia.
* **Integrante 2:** Programación del flujo visual del POS: carrito de compras dinámico, cálculo en tiempo real de subtotales, impuestos, total y procesamiento del cobro.
* **Integrante 3:** Diseño del modelo de recibo/ticket de venta, ventana de confirmación de transacción y ejecución de pruebas de carga registrando ventas continuas.
* **📌 Entregable de Fase:** Flujo de venta completo funcionando desde la UI de escritorio con actualización en tiempo real del inventario en MySQL.

### **Fase 5: Control de Acceso, Roles y Reportes (Oct 12 – Oct 18)**
* **Integrante 1:** Restricción de permisos a nivel de base de datos y endpoints según el perfil del usuario (*Administrador* y *Cajero*).
* **Integrante 2:** Integración de la pantalla de Login, gestión de la sesión activa y ocultamiento/bloqueo de menús según el rol que inició sesión.
* **Integrante 3:** Pantalla de reporte diario ("Ventas del Día"), compilación de la documentación técnica y redactado inicial del manual de usuario.
* **📌 Entregable de Fase:** Sistema seguro con inicio de sesión por roles y panel de métricas de ventas diarias.

### **Fase 6: Pruebas Globales, Empaquetado y Presentación (Oct 19 – Oct 30)**
* **Equipo Completo:** Pruebas integrales de punta a punta, resolución de errores (bugs), optimización del sistema y generación del instalador ejecutable autónomo.
* **Integrante 3:** Lidera el diseño de la diapositiva y presentación ejecutiva para la defensa.
* **Integrantes 1 y 2:** Preparación de los argumentos técnicos de defensa (transacciones ACID, arquitectura MVVM, consumo de APIs).
* **📌 Entregable de Fase:** Software empaquetado para distribución y defensa exitosa del proyecto.
