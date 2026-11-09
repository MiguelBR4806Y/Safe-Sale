# MySQL local incluido — notas internas

Documento técnico para desarrollo y defensa. El usuario final **no** necesita leerlo:
la app se encarga de todo.

## Cómo funciona

1. Al arrancar, `MySqlServerManager.Start()`:
   - resuelve `vendor/mysql/<rid>/bin/mysqld`;
   - crea el datadir local si no existe (`mysqld --initialize-insecure`);
   - genera contraseñas aleatorias para el usuario interno y `root`;
   - elige un puerto libre en `127.0.0.1` y lo guarda;
   - arranca `mysqld` con `--bind-address=127.0.0.1`;
   - crea la base `safesale` y el usuario `safesale_app` (solo con permisos sobre `safesale`);
   - ejecuta `DatabaseInitializer` (esquema + seed admin/categorías).
2. `DatabaseConfig.ConnectionString` arma la cadena interna. El usuario nunca la ve.
3. Al cerrar la app, `MySqlServerManager.Stop()` ejecuta `SHUTDOWN`.

## Objetos de base de datos

Además de las tablas, el inicializador crea (con la conexión administrativa `root`,
porque los triggers requieren privilegios elevados si el binlog está activo):

- `sp_registrar_venta`: procedimiento transaccional que exige una caja abierta,
  inserta venta + items (con `cash_session_id`), calcula el total en el servidor,
  descuenta stock y actualiza los totales de la caja. Usa `START TRANSACTION`,
  `COMMIT` y un `EXIT HANDLER` con `ROLLBACK` + `RESIGNAL` ante cualquier error.
  Lo usa `SaleRepository.RegisterSale`.
- `cash_sessions`: apertura/cierre de caja por usuario, con efectivo esperado y diferencia.
- Triggers `trg_products_audit_insert/update/delete`: registran cambios en `product_audit`.
- `vw_stock_bajo`: vista de productos con `stock <= min_stock`.
- Seed de 18 productos (2 por categoría) si `products` está vacía.

La app solo ejecuta `CALL sp_registrar_venta(...)` para cobrar; no hace inserts
sueltos de venta/items ni descuenta stock desde C#. Así el cobro es atómico.

## Dependencias por sistema operativo

- **Windows**: el build de MySQL Community requiere el **Visual C++ Redistributable**. En Windows 11 con Smart App Control, instalar la app fuera de `Documents`/`Desktop`.
- **macOS**: los binarios incluidos pueden quedar en cuarentena por Gatekeeper. Para desarrollo:
  `xattr -dr com.apple.quarantine /Applications/Safe-Sale.app`. Para distribuir, firmar/notarizar la app y los binarios anidados. `publish-macos.sh` genera `SS-ico.icns` automáticamente si existe `iconutil`.
- **Linux**: el tarball de MySQL necesita `libaio` (Debian/Ubuntu: `sudo apt install libaio1`; Fedora: `sudo dnf install libaio`). El AppImage incluye los binarios y usa `--no-defaults` para no heredar `/etc/mysql`.
- **Todos**: el binario de `vendor/mysql/<rid>` debe coincidir con la arquitectura (`win-x64`, `osx-arm64`, `osx-x64`, `linux-x64`). Los scripts de publicación verifican que exista antes de empaquetar.

> Estado de pruebas: Windows está probado de punta a punta (motor, esquema, cajas, ACID, iconos). En macOS y Linux la implementación es multiplataforma y los scripts están validados sintácticamente, pero falta ejecutar la app en esos sistemas con sus binarios de MySQL para confirmar dependencias nativas y firma.

## Rutas locales

- Windows: `%LOCALAPPDATA%\Safe-Sale\`
  - `mysql\data` (datadir)
  - `mysql\secrets.json` (credenciales internas, oculto)
  - `mysql\state.json` (puerto)
  - `logs\mysqld.log`, `logs\startup.log`
- macOS: `~/Library/Application Support/Safe-Sale/`
- Linux: `~/.local/share/Safe-Sale/`

## Desarrollo

Descargar el motor para tu plataforma:

```powershell
# Windows
.\scripts\fetch-mysql-vendor.ps1
```

```bash
# macOS / Linux (pasá la URL oficial del tarball de MySQL 8)
MYSQL_URL="https://dev.mysql.com/get/Downloads/MySQL-8.4/mysql-8.4.5-macos14-arm64.tar.gz" \
  ./scripts/fetch-mysql-vendor.sh
```

Alternativas para desarrollo:

- `SAFESALE_MYSQL_BASEDIR=/ruta/a/mysql` para usar una instalación local.
- `SAFESALE_APPDATA=/ruta/alternativa` para usar un datadir de pruebas aislado (no toca los datos reales).
- `SAFESALE_DB_CONNECTION="Server=127.0.0.1;Port=3307;Database=safesale;User ID=...;Password=...;"` para conectar a un MySQL de pruebas (solo desarrollo).

## Troubleshooting

- **No se encontró el motor**: falta `vendor/mysql/<rid>`; ejecutá el script de descarga.
- **El proceso termina al arrancar**: revisar `logs/mysqld.log` y el `data/<hostname>.err`. Causas comunes: falta VC++ Runtime (Windows), permisos del datadir, o puerto ocupado.
- **Otra instancia**: si queda un `mysqld` huérfano, cerrá Safe-Sale por completo. El manager intenta terminarlo solo si encuentra el PID.
- **Datadir incompleto o de un arranque cortado**: la app lo detecta (falta `mysql/`, `mysql.ibd` o `ibdata1`) y lo **mueve a `mysql/data.incomplete-<fecha>`**, creando un datadir nuevo. No se borra nada automáticamente; si el movimiento falla por archivos bloqueados, muestra un error pidiendo cerrar Safe-Sale.
- **Smart App Control (Windows 11)**: instalar la app fuera de `Documents`/`Desktop` para permitir ejecutar los binarios incluidos.

## Licencia

MySQL Community es GPLv2. Si la redistribución es un problema, se puede reemplazar por
MariaDB portable (LGPL y protocolo compatible con `MySqlConnector`); el código de
`MySqlServerManager` solo necesita ajustar el nombre/opciones del binario.
