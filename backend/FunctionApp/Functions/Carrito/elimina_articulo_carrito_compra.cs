using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace FunctionApp1
{
    public class elimina_articulo_carrito_compra
    {
        // Clases para deserialización del JSON
        class ParamEliminaArticulo
        {
            public int? id_usuario;
            public int? id_articulo;
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

        [Function("elimina_articulo_carrito_compra")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                // Leer el cuerpo de la solicitud
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamEliminaArticulo? p = JsonConvert.DeserializeObject<ParamEliminaArticulo>(body);

                if (p == null || p.id_usuario == null || p.id_articulo == null || string.IsNullOrEmpty(p.token))
                    return new BadRequestObjectResult(new Error("Faltan parámetros"));

                // Configuración de la conexión a la base de datos
                string cs = DbConfig.GetConnectionString();

                using var conexion = new MySqlConnection(cs);
                await conexion.OpenAsync();

                // Verificar token
                if (!await VerificaToken(conexion, p.id_usuario.Value, p.token))
                    return new UnauthorizedObjectResult(new Error("Token inválido"));

                // Verificar existencia del artículo en el carrito de compra
                int cantidadEnCarrito = 0;
                using (var cmdCheck = new MySqlCommand("SELECT cantidad FROM carrito_compra WHERE id_usuario = @id_usuario AND id_articulo = @id_articulo", conexion))
                {
                    cmdCheck.Parameters.AddWithValue("@id_usuario", p.id_usuario);
                    cmdCheck.Parameters.AddWithValue("@id_articulo", p.id_articulo);
                    var result = await cmdCheck.ExecuteScalarAsync();
                    if (result == null)
                        return new BadRequestObjectResult(new Error("El artículo no está en el carrito de compras"));

                    cantidadEnCarrito = Convert.ToInt32(result);
                }

                // Iniciar transacción
                using var transaccion = await conexion.BeginTransactionAsync();

                try
                {
                    // Eliminar el artículo del carrito de compra
                    using (var cmdDelete = new MySqlCommand("DELETE FROM carrito_compra WHERE id_usuario = @id_usuario AND id_articulo = @id_articulo", conexion, transaccion))
                    {
                        cmdDelete.Parameters.AddWithValue("@id_usuario", p.id_usuario);
                        cmdDelete.Parameters.AddWithValue("@id_articulo", p.id_articulo);
                        await cmdDelete.ExecuteNonQueryAsync();
                    }

                    // Actualizar la cantidad de artículos en la tabla "stock"
                    using (var cmdUpdate = new MySqlCommand("UPDATE stock SET cantidad = cantidad + @cantidad WHERE id_articulo = @id_articulo", conexion, transaccion))
                    {
                        cmdUpdate.Parameters.AddWithValue("@cantidad", cantidadEnCarrito);
                        cmdUpdate.Parameters.AddWithValue("@id_articulo", p.id_articulo);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }

                    // Confirmar transacción
                    await transaccion.CommitAsync();
                    return new OkObjectResult("Artículo eliminado del carrito con éxito");
                }
                catch (Exception ex)
                {
                    // Si hay un error, realizar rollback
                    await transaccion.RollbackAsync();
                    return new BadRequestObjectResult(new Error("Error al eliminar el artículo del carrito: " + ex.Message));
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new Error("Error global: " + e.Message));
            }
        }

        // Método para verificar el token
        private async Task<bool> VerificaToken(MySqlConnection conexion, int id_usuario, string token)
        {
            var cmd = new MySqlCommand("SELECT 1 FROM usuarios WHERE id_usuario=@id AND token=@token", conexion);
            cmd.Parameters.AddWithValue("@id", id_usuario);
            cmd.Parameters.AddWithValue("@token", token);

            using var r = await cmd.ExecuteReaderAsync();
            return r.HasRows;
        }
    }
}
