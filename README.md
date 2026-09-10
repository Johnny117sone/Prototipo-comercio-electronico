# Tienda Johnny117 — Prototipo de Comercio Electrónico

Prototipo de tienda en línea , extendiendo un servicio web para implementar un flujo completo de comercio electrónico: alta/consulta/baja de usuarios con autenticación, captura de artículos, búsqueda, carrito de compra y checkout básico.

Backend en **C# sobre Azure Functions** (modelo aislado, .NET 8) conectado a **MySQL**, frontend en HTML/CSS/JavaScript sin frameworks, probado en dispositivo móvil.

## Funcionalidades

- **Usuarios:** alta, consulta, modificación y baja, con foto de perfil (subida como imagen, convertida a base64).
- **Login con token:** al iniciar sesión se genera un token de sesión de un solo uso por login, requerido para dar de alta artículos y comprar.
- **Catálogo de artículos:** alta de artículos con nombre, descripción, precio, cantidad en stock y foto.
- **Búsqueda de artículos** por palabra clave.
- **Carrito de compra:** persistido tanto en el backend (base de datos) como en `localStorage` del navegador, con eliminación de artículos individuales o del carrito completo.
- **Servido de archivos estáticos** (HTML/CSS/JS/imágenes) a través de una Azure Function propia (`Get`), en vez de un servidor web tradicional.

## Tecnologías

| Categoría | Tecnología |
|---|---|
| Backend | C# / .NET 8, Azure Functions (modelo aislado, isolated worker) |
| Base de datos | MySQL |
| Cliente HTTP en frontend | `WSClient.js` (framework proporcionado por el profesor, ver atribución abajo) |
| Frontend | HTML5, CSS3, JavaScript vanilla |

## Estructura del proyecto

```
ecommerce-t8/
├── backend/
│   └── FunctionApp/
│       ├── Functions/
│       │   ├── Usuarios/         # login, alta, baja, modifica, consulta
│       │   ├── Articulos/        # alta y consulta de artículos
│       │   ├── Carrito/          # compra y eliminación de artículos/carrito
│       │   └── Static/           # Get (servidor de archivos estáticos) y MimeMapping
│       ├── Shared/
│       │   └── DbConfig.cs       # cadena de conexión centralizada (antes duplicada en 10 archivos)
│       ├── Program.cs
│       ├── host.json
│       ├── FunctionApp.csproj
│       └── local.settings.json.example   # plantilla sin credenciales reales
├── frontend/
│   ├── index.html
│   ├── css/styles.css            # antes: estilos inline repetidos en cada botón/input
│   ├── js/app.js                 # lógica de la app (antes vivía embebida en el HTML)
│   ├── js/WSClient.js            # framework de terceros, ver atribución
│   └── img/usuario_sin_foto.png
├── database/
│   └── schema.sql
└── .gitignore
```

## Instalación y ejecución

### Requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- MySQL (local o en la nube)

### Pasos

1. Crea la base de datos ejecutando `database/schema.sql` en tu servidor MySQL.

2. Configura las credenciales locales:
   ```bash
   cd backend/FunctionApp
   cp local.settings.json.example local.settings.json
   ```
   Edita `local.settings.json` con los datos reales de tu servidor MySQL (`Server`, `UserID`, `Password`, `Database`). Este archivo está en `.gitignore` y nunca se sube al repositorio.

3. Ejecuta el backend:
   ```bash
   func start
   ```

4. Abre el frontend navegando a `http://localhost:7071/api/Get?nombre=/index.html` (el HTML, CSS y JS se sirven a través de la función `Get`, no de un servidor web aparte).

## Nota de seguridad

Este prototipo **guarda y compara las contraseñas en texto plano** (ver `login.cs` y `alta_usuario.cs`, ya documentado explícitamente en el código original). Es una limitación conocida y aceptable para un prototipo académico, pero **no debe usarse así en producción**. La mejora natural sería aplicar hash (ej. BCrypt.Net) antes de guardar la contraseña, igual que se hizo en otro proyecto de este portafolio.

Además, durante la limpieza de este repositorio se encontró una contraseña real de base de datos en `local.settings.json`. Ese archivo ya estaba correctamente excluido por el `.gitignore` que trae por defecto Visual Studio, así que nunca llegó a subirse — pero si reutilizas esa contraseña en algún otro lugar, considera cambiarla.

## Código eliminado durante la limpieza

Se removió una función duplicada y nunca alcanzable (`solo_cierra_carrito_y_muestra_compra`, definida dentro de otra función donde nunca se ejecutaba) que quedó de una versión anterior del código. No afecta la funcionalidad.

## Mejoras futuras

- [ ] Hashear las contraseñas (actualmente en texto plano).
- [ ] Expirar el token de sesión (actualmente no tiene fecha de caducidad).
- [ ] Mover la configuración de artículos favoritos/pedidos a su propia tabla en vez de depender solo de `localStorage` para el carrito.
- [ ] Agregar paginación a la búsqueda de artículos.
- [ ] Pruebas automatizadas del backend (por ahora solo se verificó estáticamente: sintaxis, balance de llaves e imports, ya que este entorno no cuenta con el SDK de .NET para compilar).

## Atribución

`frontend/js/WSClient.js` es un framework de terceros escrito por **Carlos Pineda Guerrero** (2015–2024), distribuido bajo licencia GNU GPL v3. No es autoría propia de este proyecto; se incluye tal cual fue proporcionado para el curso.

## Autor

Desarrollado por Johnny Hernández Torres.

GitHub: [@Johnny117sone](https://github.com/Johnny117sone)
