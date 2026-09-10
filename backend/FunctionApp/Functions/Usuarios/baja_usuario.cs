using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System;

namespace FunctionApp1
{
    public class borra_usuario
    {
        // Clases para deserialización del JSON
        class ParamBorraUsuario
        {
            public string? email;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [Function("borra_usuario")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            try
            {
                // Obtener el email de la consulta o del cuerpo de la solicitud
                string? email = req.Query["email"];
                if (email == null)
                {
                    string body = await new StreamReader(req.Body).ReadToEndAsync();
                    ParamBorraUsuario? data = JsonConvert.DeserializeObject<ParamBorraUsuario>(body);
                    if (data == null || string.IsNullOrEmpty(data.email)) throw new Exception("Se espera el email");
                    email = data.email;
                }

                // Conexión a la base de datos
                string cs = DbConfig.GetConnectionString();

                using (var conexion = new MySqlConnection(cs))
                {
                    await conexion.OpenAsync();

                    using (var transaccion = await conexion.BeginTransactionAsync()) // Usando transacciones asincrónicas
                    {
                        try
                        {
                            // Eliminar las fotos del usuario
                            var cmd_2 = new MySqlCommand("DELETE FROM fotos_usuarios WHERE id_usuario=(SELECT id_usuario FROM usuarios WHERE email=@email)", conexion, transaccion);
                            cmd_2.Parameters.AddWithValue("@email", email);
                            await cmd_2.ExecuteNonQueryAsync();

                            // Eliminar el usuario
                            var cmd_3 = new MySqlCommand("DELETE FROM usuarios WHERE email=@email", conexion, transaccion);
                            cmd_3.Parameters.AddWithValue("@email", email);
                            await cmd_3.ExecuteNonQueryAsync();

                            // Confirmar la transacción
                            await transaccion.CommitAsync();

                            return new OkObjectResult("Usuario y fotos eliminados exitosamente");
                        }
                        catch (Exception e)
                        {
                            // Si ocurre un error, hacer rollback
                            await transaccion.RollbackAsync();
                            return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}
