/*
  Tienda Johnny117 -- lógica de la aplicación.

  Extraído del <script> inline que antes vivía dentro del HTML. El
  comportamiento es exactamente el mismo que antes; lo único que cambió
  son las plantillas de artículos/carrito, que ahora usan clases CSS
  (article-card, article-info, etc.) en vez de estilos inline repetidos.
*/

var URL = "/api";
var foto = null;      // foto del usuario (alta/consulta)
var foto_articulo = null;
var id_usuario = null;
var token = null;
var destino_post_login = null;
var carrito = [];

function get(id) {
    return document.getElementById(id);
}

function muestra(id) {
    get(id).style.display = "block";
}

function oculta(id) {
    get(id).style.display = "none";
}

function muestra_pantalla(id) {
    oculta("menu");
    muestra(id);
}

function oculta_pantalla(id) {
    oculta(id);
    muestra("menu");
}

function oculta_todos() {
    oculta("menu");
    oculta("compra_articulos");
    oculta("carrito");
}

function solo_cierra_carrito_y_muestra_compra() {
    oculta("carrito");
    muestra("compra_articulos");
}

/* --- Alta / consulta de foto de usuario --- */
function readSingleFile(files, imagen) {
    var file = files[0];
    if (!file) return;
    var reader = new FileReader();
    reader.onload = function (e) {
        imagen.src = reader.result;
        foto = reader.result.split(',')[1];
    };
    reader.readAsDataURL(file);
}

/* --- Alta de usuario --- */
function limpia_alta() {
    get("alta_email").value = "";
    get("alta_nombre").value = "";
    get("alta_apellido_paterno").value = "";
    get("alta_apellido_materno").value = "";
    get("alta_fecha_nacimiento").value = "";
    get("alta_telefono").value = "";
    get("alta_genero").value = "";
    get("alta_imagen").src = "/api/Get?nombre=/img/usuario_sin_foto.png";
    get("alta_password").value = "";
    foto = null;
}

function alta() {
    var cliente = new WSClient(URL);
    var usuario = {
        email: get("alta_email").value,
        nombre: get("alta_nombre").value,
        apellido_paterno: get("alta_apellido_paterno").value,
        apellido_materno: get("alta_apellido_materno").value != "" ? get("alta_apellido_materno").value : null,
        fecha_nacimiento: get("alta_fecha_nacimiento").value != "" ? new Date(get("alta_fecha_nacimiento").value).toISOString() : null,
        telefono: get("alta_telefono").value != "" ? get("alta_telefono").value : null,
        genero: get("alta_genero").value == "Masculino" ? "M" : get("alta_genero").value == "Femenino" ? "F" : null,
        foto: foto,
        password: get("alta_password").value
    };

    cliente.postJson("alta_usuario", { usuario: usuario }, function (code, result) {
        if (code == 200) alert("OK");
        else alert(JSON.stringify(result));
    });
}

/* --- Consulta / modificación de usuario --- */
function limpia_consulta() {
    get("consulta_email").value = "";
    get("consulta_nombre").value = "";
    get("consulta_apellido_paterno").value = "";
    get("consulta_apellido_materno").value = "";
    get("consulta_fecha_nacimiento").value = "";
    get("consulta_telefono").value = "";
    get("consulta_genero").value = "";
    get("consulta_imagen").src = "/api/Get?nombre=/img/usuario_sin_foto.png";
    get("consulta_password").value = "";
}

function cierra_pantalla_consulta() {
    oculta_pantalla('consulta_usuario');
    muestra("encabezado_consulta");
    muestra("boton_consulta");
    oculta("encabezado_modifica");
    oculta("modifica_usuario");
    get("consulta_email").readOnly = false;
}

function quita_foto() {
    foto = null;
    get('consulta_imagen').src = '/api/Get?nombre=/img/usuario_sin_foto.png';
    get('consulta_file').value = '';
}

function formatearFecha(fecha) {
    var fecha = new Date(fecha);
    var año = fecha.getFullYear();
    var mes = (fecha.getMonth() + 1).toString().padStart(2, '0');
    var dia = fecha.getDate().toString().padStart(2, '0');
    var horas = fecha.getHours().toString().padStart(2, '0');
    var minutos = fecha.getMinutes().toString().padStart(2, '0');
    var segundos = fecha.getSeconds().toString().padStart(2, '0');
    return año + "-" + mes + "-" + dia + "T" + horas + ":" + minutos + ":" + segundos;
}

function consulta() {
    var cliente = new WSClient(URL);
    cliente.postJson("consulta_usuario", { email: get("consulta_email").value }, function (code, result) {
        if (code == 200) {
            limpia_consulta();
            get("consulta_email").value = result.email;
            get("consulta_nombre").value = result.nombre;
            get("consulta_apellido_paterno").value = result.apellido_paterno;
            get("consulta_apellido_materno").value = result.apellido_materno != null ? result.apellido_materno : "";
            get("consulta_fecha_nacimiento").value = formatearFecha(new Date(result.fecha_nacimiento + "Z").toLocaleString('en-US'));
            get("consulta_telefono").value = result.telefono != null ? result.telefono : "";
            get("consulta_genero").value = result.genero == "M" ? "Masculino" : result.genero == "F" ? "Femenino" : "";
            foto = result.foto;
            get("consulta_imagen").src = foto != null ? "data:image/jpeg;base64," + foto : "/api/Get?nombre=/img/usuario_sin_foto.png";
            get("consulta_password").value = result.password;

            oculta("encabezado_consulta");
            muestra("encabezado_modifica");
            muestra("modifica_usuario");
            oculta("boton_consulta");
            get("consulta_email").readOnly = true;
        } else {
            alert(JSON.stringify(result));
        }
    });
}

function modifica() {
    var cliente = new WSClient(URL);
    var usuario = {
        email: get("consulta_email").value,
        nombre: get("consulta_nombre").value,
        apellido_paterno: get("consulta_apellido_paterno").value,
        apellido_materno: get("consulta_apellido_materno").value != "" ? get("consulta_apellido_materno").value : null,
        fecha_nacimiento: get("consulta_fecha_nacimiento").value != "" ? new Date(get("consulta_fecha_nacimiento").value).toISOString() : null,
        telefono: get("consulta_telefono").value != "" ? get("consulta_telefono").value : null,
        genero: get("consulta_genero").value == "Masculino" ? "M" : get("consulta_genero").value == "Femenino" ? "F" : null,
        foto: foto,
        password: get("consulta_password").value
    };

    cliente.postJson("modifica_usuario", { usuario: usuario }, function (code, result) {
        if (code == 200) alert("OK");
        else alert(JSON.stringify(result));
    });
}

/* --- Baja de usuario --- */
function limpia_borra() {
    get("borra_email").value = "";
}

function borra() {
    var client = new WSClient(URL);
    client.postJson("borra_usuario", { email: get("borra_email").value }, function (code, result) {
        if (code == 200) alert("OK");
        else alert(JSON.stringify(result));
    });
}

/* --- Login --- */
function muestra_login(destino) {
    destino_post_login = destino;
    get("login_email").value = "";
    get("login_password").value = "";
    muestra_pantalla("login");
}

function login() {
    var cliente = new WSClient(URL);
    cliente.postJson("login", {
        email: get("login_email").value,
        password: get("login_password").value
    }, function (code, result) {
        if (code == 200) {
            id_usuario = result.id_usuario;
            token = result.token;
            oculta_pantalla("login");
            if (destino_post_login) {
                muestra_pantalla(destino_post_login);
            }
        } else {
            let msg = "Error desconocido";
            if (result && result.mensaje) msg = result.mensaje;
            else if (result && result.message) msg = result.message;
            else if (result && result.error) msg = result.error;
            else if (typeof result === "string") msg = result;
            alert(msg);
        }
    });
}

/* --- Alta de artículo --- */
function readSingleFileArticulo(files, imagen) {
    var file = files[0];
    if (!file) return;
    var reader = new FileReader();
    reader.onload = function (e) {
        imagen.src = reader.result;
        foto_articulo = reader.result.split(',')[1];
        get("msg_foto_articulo").style.display = "none";
    };
    reader.readAsDataURL(file);
}

function limpia_alta_articulo() {
    get("articulo_nombre").value = "";
    get("articulo_descripcion").value = "";
    get("articulo_precio").value = "";
    get("articulo_cantidad").value = "";
    get("articulo_imagen").src = "/api/Get?nombre=/img/usuario_sin_foto.png";
    foto_articulo = null;
    get("msg_foto_articulo").style.display = "none";
}

function altaArticulo() {
    if (!id_usuario || !token) {
        alert("Debes iniciar sesión.");
        return;
    }
    if (!foto_articulo) {
        get("msg_foto_articulo").style.display = "inline";
        return;
    }
    var cliente = new WSClient(URL);
    var articulo = {
        nombre: get("articulo_nombre").value,
        descripcion: get("articulo_descripcion").value,
        precio: parseFloat(get("articulo_precio").value),
        cantidad: parseInt(get("articulo_cantidad").value),
        foto: foto_articulo,
        id_usuario: id_usuario
    };
    cliente.postJson("alta_articulo", { articulo: articulo, token: token }, function (code, result) {
        if (code == 200) {
            alert("Artículo dado de alta correctamente");
            limpia_alta_articulo();
            oculta_pantalla('alta_articulo');
        } else {
            alert("Error: " + (result && result.mensaje ? result.mensaje : "Error desconocido"));
        }
    });
}

/* --- Búsqueda y compra de artículos --- */
function buscarArticulos() {
    var palabra = get("busqueda_palabra").value.trim();
    if (!palabra) {
        alert("Ingresa una palabra clave para buscar.");
        return;
    }
    var cliente = new WSClient(URL);
    cliente.postJson("consulta_articulos", {
        palabra_clave: palabra,
        id_usuario: id_usuario,
        token: token
    }, function (code, result) {
        if (code == 200) {
            mostrarResultadosArticulos(result);
        } else {
            alert("Error (" + code + "): " + JSON.stringify(result));
        }
    });
}

function mostrarResultadosArticulos(articulos) {
    window.ultimaBusquedaArticulos = articulos;
    var html = "";
    if (!articulos || articulos.length === 0) {
        html = '<p class="empty-state">No se encontraron artículos.</p>';
    } else {
        for (var i = 0; i < articulos.length; i++) {
            var art = articulos[i];
            html += `
                <div class="article-card">
                    <img src="data:image/png;base64,${art.foto}" alt="${art.nombre}"/>
                    <div class="article-info">
                        <span class="article-name">${art.nombre}</span>
                        <span class="article-desc">${art.descripcion || ""}</span>
                        <span class="article-price">$${art.precio}</span>
                        <div class="article-actions">
                            <input type="number" id="cantidad_compra_${art.id_articulo}" value="1" min="1"/>
                            <button type="button" class="btn btn-success" onclick="comprarArticulo(${art.id_articulo})">Comprar</button>
                        </div>
                    </div>
                </div>
            `;
        }
    }
    get("resultados_articulos").innerHTML = html;
}

function comprarArticulo(id_articulo) {
    var cantidad = parseInt(get("cantidad_compra_" + id_articulo).value);
    if (!cantidad || cantidad <= 0) {
        alert("Cantidad inválida.");
        return;
    }
    var cliente = new WSClient(URL);
    cliente.postJson("compra_articulo", {
        id_articulo: id_articulo,
        cantidad: cantidad,
        id_usuario: id_usuario,
        token: token
    }, function (code, result) {
        if (code == 200) {
            var articulos = window.ultimaBusquedaArticulos || [];
            var art = articulos.find(a => a.id_articulo === id_articulo);
            if (art) {
                var enCarrito = carrito.find(a => a.id_articulo === id_articulo);
                if (enCarrito) {
                    enCarrito.cantidad += cantidad;
                } else {
                    var artCopia = Object.assign({}, art);
                    artCopia.cantidad = cantidad;
                    carrito.push(artCopia);
                }
                guardarCarrito();
            }
            alert("Artículo agregado al carrito.");
        } else {
            let msg = "Error desconocido";
            if (result) {
                if (result.mensaje) msg = result.mensaje;
                else if (result.error) msg = result.error;
                else if (typeof result === "string") msg = result;
                else msg = JSON.stringify(result);
            }
            alert(msg);
        }
    });
}

/* --- Carrito (persistido en localStorage + backend) --- */
function guardarCarrito() {
    localStorage.setItem("carrito", JSON.stringify(carrito));
}

function cargarCarrito() {
    var datos = localStorage.getItem("carrito");
    carrito = datos ? JSON.parse(datos) : [];
}

function verCarrito() {
    cargarCarrito();
    var html = "";
    var total = 0;
    if (!carrito.length) {
        html = '<p class="empty-state">El carrito está vacío.</p>';
    } else {
        for (var i = 0; i < carrito.length; i++) {
            var art = carrito[i];
            var costo = art.precio * art.cantidad;
            total += costo;
            html += `
                <div class="article-card">
                    <img src="data:image/png;base64,${art.foto}" alt="${art.nombre}"/>
                    <div class="article-info">
                        <span class="article-name">${art.nombre}</span>
                        <span class="article-desc">${art.descripcion || ""}</span>
                        <span class="article-price">$${art.precio} &times; ${art.cantidad} = <b>$${costo.toFixed(2)}</b></span>
                        <div class="article-actions">
                            <button type="button" class="btn btn-danger" onclick="eliminarArticuloCarrito(${art.id_articulo})">Eliminar</button>
                        </div>
                    </div>
                </div>
            `;
        }
        html += `<div class="cart-total">Total: $${total.toFixed(2)}</div>`;
    }
    html += `
        <button type="button" class="btn btn-danger" onclick="eliminarCarrito()">Vaciar carrito</button>
        <button type="button" class="btn btn-secondary" onclick="solo_cierra_carrito_y_muestra_compra()">Seguir comprando</button>
        <button type="button" class="btn btn-secondary" onclick="oculta_pantalla('carrito')">Regresar al menú</button>
    `;
    get("contenido_carrito").innerHTML = html;
    oculta_todos();
    muestra("carrito");
}

function eliminarArticuloCarrito(id_articulo) {
    var cliente = new WSClient(URL);
    cliente.postJson("elimina_articulo_carrito_compra", {
        id_usuario: id_usuario,
        id_articulo: id_articulo,
        token: token
    }, function (code, result) {
        if (code == 200) {
            carrito = carrito.filter(a => a.id_articulo !== id_articulo);
            guardarCarrito();
            verCarrito();
        } else {
            alert(result && result.mensaje ? result.mensaje : "Error desconocido");
        }
    });
}

function eliminarCarrito() {
    var cliente = new WSClient(URL);
    cliente.postJson("elimina_carrito_compra", {
        id_usuario: id_usuario,
        token: token
    }, function (code, result) {
        if (code == 200) {
            carrito = [];
            guardarCarrito();
            verCarrito();
        } else {
            alert(result && result.mensaje ? result.mensaje : "Error desconocido");
        }
    });
}

// Al cargar la página, recupera el carrito guardado en este navegador.
cargarCarrito();
