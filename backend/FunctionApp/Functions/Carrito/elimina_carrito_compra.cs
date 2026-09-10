using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FunctionApp1
{
    public class elimina_carrito_compra
    {
        // Clases para deserialización del JSON
        class ParamEliminaCarrito
        {
            public int? id_usuario;
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

        [Function("elimina_carrito_compra")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                // Leer el cuerpo de la solicitud
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamEliminaCarrito? p = JsonConvert.DeserializeObject<ParamEliminaCarrito>(body);

                if (p == null || p.id_usuario == null || string.IsNullOrEmpty(p.token))
                    return new BadRequestObjectResult(new Error("Faltan parámetros"));

                // Configuración de la conexión a la base de datos
                string cs = DbConfig.GetConnectionString();

                using var conexion = new MySqlConnection(cs);
                await conexion.OpenAsync();

                // Verificar token
                if (!await VerificaToken(conexion, p.id_usuario.Value, p.token))
                    return new UnauthorizedObjectResult(new Error("Token inválido"));

                // Obtener los artículos del carrito de compra del usuario
                var cmdSelect = new MySqlCommand("SELECT id_articulo, cantidad FROM carrito_compra WHERE id_usuario = @id_usuario", conexion);
                cmdSelect.Parameters.AddWithValue("@id_usuario", p.id_usuario);

                // Asegúrate de cerrar el reader después de obtener los resultados
                var articulosEnCarrito = new List<(int id_articulo, int cantidad)>();

                using (var reader = await cmdSelect.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        articulosEnCarrito.Add((
                            id_articulo: reader.GetInt32(0),
                            cantidad: reader.GetInt32(1)
                        ));
                    }
                }

                if (articulosEnCarrito.Count == 0)
                    return new BadRequestObjectResult(new Error("No hay artículos en el carrito de compras"));

                // Iniciar transacción
                using var transaccion = await conexion.BeginTransactionAsync();

                try
                {
                    // Eliminar todos los artículos del carrito de compra
                    using var cmdDelete = new MySqlCommand("DELETE FROM carrito_compra WHERE id_usuario = @id_usuario", conexion, transaccion);
                    cmdDelete.Parameters.AddWithValue("@id_usuario", p.id_usuario);
                    int filasEliminadas = await cmdDelete.ExecuteNonQueryAsync();

                    // Actualizar la cantidad de artículos en la tabla "stock"
                    foreach (var articulo in articulosEnCarrito)
                    {
                        using var cmdUpdate = new MySqlCommand("UPDATE stock SET cantidad = cantidad + @cantidad WHERE id_articulo = @id_articulo", conexion, transaccion);
                        cmdUpdate.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                        cmdUpdate.Parameters.AddWithValue("@id_articulo", articulo.id_articulo);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }

                    // Confirmar transacción
                    await transaccion.CommitAsync();
                    return new OkObjectResult($"Carrito de compra eliminado con éxito. Se eliminaron {filasEliminadas} artículos.");
                }
                catch (Exception ex)
                {
                    // Si hay un error, realizar rollback
                    await transaccion.RollbackAsync();
                    return new BadRequestObjectResult(new Error("Error al eliminar el carrito de compra: " + ex.Message));
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
