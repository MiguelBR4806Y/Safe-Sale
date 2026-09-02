## 📁 Estructura del Proyecto

```
Safe-Sale/
├── docs/                           # Diagramas E-R, prototipos de Figma y documentación
├── src/
    └── SS/                         # Proyecto Avalonia UI con SQLite
        ├── App.axaml               # Aplicación Avalonia principal
        ├── App.axaml.cs            # Code-behind de App.axaml
        ├── MainWindow.axaml        # Ventana principal de la UI
        ├── MainWindow.axaml.cs     # Code-behind de MainWindow.axaml
        ├── MainViewModel.cs        # ViewModel principal (Patrón MVVM)
        ├── ViewModelBase.cs        # Clase base para ViewModels
        ├── SS.csproj               # Proyecto Avalonia con SQLite
        ├── ViewLocator.cs          # Localizador de vistas
        ├── Assets/                 # Recursos e iconos
        │   └── avalonia-logo.ico
        ├── Views/                  # Vistas XAML
        └── ViewModels/             # Lógica de vistas en C# (MVVM)
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
* **Dante (Integrante 1):** Levanta requerimientos técnicos. Modela y genera el diagrama Entidad-Relación (MER) formal y el esquema relacional en la carpeta docs/.
* **Lucas (Integrante 2):** Configura la estructura de la solución multi-proyecto (SaveSale.sln), definición de carpetas, dependencias entre proyectos y configuración base de MVVM.
* **Jandir (Integrante 3):** Diseña la experiencia de usuario (UX) e interfaces de pantalla en **Figma**: Login, Dashboard principal, Formulario de productos y la caja de cobro (POS).
* **📌 Entregable de Fase:** Diagrama E-R aprobado, bocetos de Figma validados y estructura base inicializada en el repositorio de GitHub.

### **Fase 2: Base de Datos y Servicios Base (Sep 14 – Sep 20)**
* **Dante (Integrante 1):** Ejecución y publicación del script schema.sql en SQLite. Creación de la base de datos local con las tablas necesarias.
* **Lucas (Integrante 2):** Configuración de la capa de conexión a base de datos SQLite y desarrollo de los endpoints para acceso a datos locales.
* **Jandir (Integrante 3):** Elaboración y prueba del script de carga masiva (seed.sql) con datos de prueba para SQLite.
* **📌 Entregable de Fase:** Base de datos SQLite activa con datos de prueba cargados y cliente C# realizando consultas exitosas.

### **Fase 3: Módulo de Gestión de Inventario (Sep 21 – Sep 27)**
* **Dante (Integrante 1):** Creación de tablas y Stored Procedures (o queries) para Alta, Baja y Modificación de productos y categorías con validación de negocio en SQLite.
* **Lucas (Integrante 2):** Desarrollo de los endpoints HTTP CRUD y creación de las pantallas XAML en Avalonia UI (DataGrid, formularios de productos) con conexión a SQLite.
* **Jandir (Integrante 3):** Construcción de la interfaz de gestión de categorías y primera ronda de pruebas QA para validar campos y formularios de inventario usando SQLite.
* **📌 Entregable de Fase:** Módulo de Inventarios 100% funcional capaz de crear, listar, editar y desactivar productos desde la interfaz.

### **Fase 4: Desarrollo del Punto de Venta (POS) (Sep 28 – Oct 11)**
* **Dante (Integrante 1):** Implementación del Stored Procedure (o transacción) sp_RegistrarVenta usando SQLite para asegurar atomicidad del cobro y descuento automático de stock.
* **Lucas (Integrante 2):** Programación del flujo visual del POS: carrito de compras dinámico, cálculo en tiempo real de subtotales, impuestos, total y procesamiento del cobro con base de datos local SQLite.
* **Jandir (Integrante 3):** Diseño del modelo de recibo/ticket de venta, ventana de confirmación de transacción y ejecución de pruebas de carga registrando ventas continuas con base de datos SQLite.
* **📌 Entregable de Fase:** Flujo de venta completo funcionando desde la UI de escritorio con actualización en tiempo real del inventario en SQLite.

### **Fase 5: Control de Acceso, Roles y Reportes (Oct 12 – Oct 18)**
* **Dante (Integrante 1):** Restricción de permisos a nivel de base de datos SQLite y endpoints según el perfil del usuario (*Administrador* y *Cajero*).
* **Lucas (Integrante 2):** Integración de la pantalla de Login, gestión de la sesión activa y ocultamiento/bloqueo de menús según el rol que inició sesión, usando autenticación con SQLite.
* **Jandir (Integrante 3):** Pantalla de reporte diario ("Ventas del Día"), compilación de la documentación técnica y redactado inicial del manual de usuario usando datos de SQLite.
* **📌 Entregable de Fase:** Sistema seguro con inicio de sesión por roles y panel de métricas de ventas diarias.

### **Fase 6: Pruebas Globales, Empaquetado y Presentación (Oct 19 – Oct 30)**
* **Equipo Completo:** Pruebas integrales de punta a punta, resolución de errores (bugs), optimización del sistema y generación del instalador ejecutable autónomo.
* **Jandir (Integrante 3):** Lidera el diseño de la diapositiva y presentación ejecutiva para la defensa usando datos de la base de datos local SQLite.
* **Dante y Lucas:** Preparación de los argumentos técnicos de defensa (arquitectura MVVM, consumo de APIs locales con SQLite).
* **📌 Entregable de Fase:** Software empaquetado para distribución y defensa exitosa del proyecto.
