using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FunctionApp1
{
    public class compra_articulo
    {
        class ParamCompraArticulo
        {
            public int? id_articulo;
            public int? cantidad;
            public int? id_usuario;
            public string? token;
        }

        class Error
{
    public string mensaje { get; set; }
    public Error(string mensaje)
    {
        this.mensaje = mensaje;
    }
}

        [Function("compra_articulo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                // Leer y deserializar el JSON recibido
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamCompraArticulo? p = JsonConvert.DeserializeObject<ParamCompraArticulo>(body);

                if (p == null || p.id_articulo == null || p.cantidad == null || p.id_usuario == null || string.IsNullOrEmpty(p.token))
                    return new BadRequestObjectResult(new Error("Faltan parámetros"));

                // Conexión a la base de datos
                string cs = DbConfig.GetConnectionString();

                using var conexion = new MySqlConnection(cs);
                await conexion.OpenAsync();

                // Verificar token
                if (!await VerificaToken(conexion, p.id_usuario.Value, p.token))
                    return new UnauthorizedObjectResult(new Error("Token inválido"));

                // Verificar stock disponible
                int cantidadDisponible = 0;
                using (var cmdStock = new MySqlCommand("SELECT cantidad FROM stock WHERE id_articulo = @id", conexion))
                {
                    cmdStock.Parameters.AddWithValue("@id", p.id_articulo);
                    var result = await cmdStock.ExecuteScalarAsync();
                    if (result == null)
                        return new BadRequestObjectResult(new Error("Artículo no encontrado"));
                    cantidadDisponible = Convert.ToInt32(result);
                }

                if (p.cantidad > cantidadDisponible)
                    return new BadRequestObjectResult(new Error("No hay suficientes artículos"));

                // Iniciar transacción
                using var transaccion = await conexion.BeginTransactionAsync();

                try
                {
                    // Insertar en carrito_compra
                    using (var cmdInsert = new MySqlCommand("INSERT INTO carrito_compra(id_usuario, id_articulo, cantidad) VALUES (@id_usuario, @id_articulo, @cantidad)", conexion, transaccion))
                    {
                        cmdInsert.Parameters.AddWithValue("@id_usuario", p.id_usuario);
                        cmdInsert.Parameters.AddWithValue("@id_articulo", p.id_articulo);
                        cmdInsert.Parameters.AddWithValue("@cantidad", p.cantidad);
                        await cmdInsert.ExecuteNonQueryAsync();
                    }

                    // Actualizar stock
                    using (var cmdUpdate = new MySqlCommand("UPDATE stock SET cantidad = cantidad - @cantidad WHERE id_articulo = @id", conexion, transaccion))
                    {
                        cmdUpdate.Parameters.AddWithValue("@cantidad", p.cantidad);
                        cmdUpdate.Parameters.AddWithValue("@id", p.id_articulo);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }

                    // Confirmar la transacción
                    await transaccion.CommitAsync();
                    return new OkObjectResult("Compra realizada con éxito");
                }
                catch (Exception ex)
                {
                    await transaccion.RollbackAsync();
                    return new BadRequestObjectResult(new Error("Error al realizar la compra: " + ex.Message));
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new Error("Error global: " + e.Message));
            }
        }

        private async Task<bool> VerificaToken(MySqlConnection conexion, int id_usuario, string token)
        {
            var cmd = new MySqlCommand("SELECT 1 FROM usuarios WHERE id_usuario=@id AND token=@token", conexion);
            cmd.Parameters.AddWithValue("@id", id_usuario);
            cmd.Parameters.AddWithValue("@token", token);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.HasRows;
        }
    }
}