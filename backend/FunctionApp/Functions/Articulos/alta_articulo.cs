using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System;

namespace FunctionApp1
{
    public class alta_articulo
    {
        // Clases para deserialización del JSON
        class Articulo
        {
            public string? nombre;
            public string? descripcion;
            public double? precio;
            public int? cantidad;
            public byte[]? foto;  // Foto en base64
            public int? id_usuario;  // ID del usuario
        }

        class ParamAltaArticulo
        {
            public Articulo? articulo;
            public string? token;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        // Método para alta de artículo
        [Function("alta_articulo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                // Leer el cuerpo de la solicitud
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamAltaArticulo p = JsonConvert.DeserializeObject<ParamAltaArticulo>(body);
                Articulo articulo = p?.articulo;

                // Validaciones de los datos
                if (articulo == null)
                    return new BadRequestObjectResult(new Error("Faltan datos del artículo"));

                if (string.IsNullOrEmpty(articulo.nombre))
                    return new BadRequestObjectResult(new Error("Se debe ingresar el nombre del artículo"));

                if (string.IsNullOrEmpty(articulo.descripcion))
                    return new BadRequestObjectResult(new Error("Se debe ingresar la descripción del artículo"));

                if (articulo.precio == null || articulo.precio <= 0)
                    return new BadRequestObjectResult(new Error("Se debe ingresar un precio válido"));

                if (articulo.cantidad == null || articulo.cantidad < 0)
                    return new BadRequestObjectResult(new Error("Se debe ingresar la cantidad en existencia"));

                if (articulo.foto == null || articulo.foto.Length == 0)
                    return new BadRequestObjectResult(new Error("Se debe ingresar la fotografía del artículo"));

                if (articulo.id_usuario == null)
                    return new BadRequestObjectResult(new Error("Se debe ingresar el id_usuario"));

                if (string.IsNullOrEmpty(p?.token))
                    return new BadRequestObjectResult(new Error("Se debe ingresar el token"));

                // Configuración de la conexión a la base de datos
                string cs = DbConfig.GetConnectionString();

                using (var conexion = new MySqlConnection(cs))
                {
                    await conexion.OpenAsync();

                    MySqlTransaction transaccion = null;

                    try
                    {
                        // Verificar el token
                        if (!await VerificaToken(conexion, articulo.id_usuario.Value, p.token))
                            return new UnauthorizedObjectResult(new Error("Token inválido"));

                        // Iniciar transacción
                        transaccion = await conexion.BeginTransactionAsync();

                        // Insertar artículo en stock
                        using (var cmd_1 = new MySqlCommand("INSERT INTO stock(nombre, descripcion, precio, cantidad) VALUES (@nombre, @descripcion, @precio, @cantidad)", conexion, transaccion))
                        {
                            cmd_1.Parameters.AddWithValue("@nombre", articulo.nombre);
                            cmd_1.Parameters.AddWithValue("@descripcion", articulo.descripcion);
                            cmd_1.Parameters.AddWithValue("@precio", articulo.precio);
                            cmd_1.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                            await cmd_1.ExecuteNonQueryAsync();

                            // Obtener el ID del artículo insertado
                            long id_articulo = cmd_1.LastInsertedId;

                            // Insertar foto en fotos_articulos usando el id_articulo recién generado
                            using (var cmd_2 = new MySqlCommand("INSERT INTO fotos_articulos (foto, id_articulo) VALUES (@foto, @id_articulo)", conexion, transaccion))
                            {
                                cmd_2.Parameters.AddWithValue("@foto", articulo.foto);
                                cmd_2.Parameters.AddWithValue("@id_articulo", id_articulo);
                                await cmd_2.ExecuteNonQueryAsync();
                            }
                        }

                        // Confirmar transacción
                        await transaccion.CommitAsync();

                        return new OkObjectResult("Artículo dado de alta con éxito");
                    }
                    catch (Exception e)
                    {
                        // Si hay error, hacer rollback
                        if (transaccion != null)
                        {
                            await transaccion.RollbackAsync();  // Rollback de la transacción
                        }
                        return new BadRequestObjectResult(new Error(e.Message));
                    }
                    finally
                    {
                        conexion.Close();
                    }
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new Error(e.Message));
            }
        }

        // Método para verificar el token
        private async Task<bool> VerificaToken(MySqlConnection conexion, int id_usuario, string token)
        {
            var cmd = new MySqlCommand("SELECT 1 FROM usuarios WHERE id_usuario=@IdUsuario AND token=@Token", conexion);
            cmd.Parameters.AddWithValue("@IdUsuario", id_usuario);
            cmd.Parameters.AddWithValue("@Token", token);

            using (var r = await cmd.ExecuteReaderAsync())
            {
                return r.HasRows;
            }
        }
    }
}
