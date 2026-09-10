using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System;

namespace FunctionApp1
{
    public class login
    {
        // Clases para deserialización del JSON
        class ParamLogin
        {
            public string? email;
            public string? password;
        }

     class Error
{
    public string mensaje { get; set; }
    public Error(string mensaje)
    {
        this.mensaje = mensaje;
    }
}

        // Método para login
        [Function("login")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                // Leer el cuerpo de la solicitud
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamLogin p = JsonConvert.DeserializeObject<ParamLogin>(body);

                // Validaciones de los datos
                if (string.IsNullOrEmpty(p?.email?.Trim()))
                    return new BadRequestObjectResult(new Error("Se debe ingresar el email"));

                if (string.IsNullOrEmpty(p?.password?.Trim()))
                    return new BadRequestObjectResult(new Error("Se debe ingresar la contraseña"));

                // Configuración de la conexión a la base de datos
                string cs = DbConfig.GetConnectionString();

                // Usamos una conexión para la consulta de usuario
                using (var conexion = new MySqlConnection(cs))
                {
                    await conexion.OpenAsync();

                    // Consulta SQL para verificar el usuario y la contraseña (sin hashing, texto plano)
                    var cmd = new MySqlCommand("SELECT id_usuario FROM usuarios WHERE email=@Email AND password=@Password", conexion);
                    cmd.Parameters.AddWithValue("@Email", p.email);
                    cmd.Parameters.AddWithValue("@Password", p.password); // Contraseña en texto plano

                    // Cerrar el DataReader después de ejecutar la primera consulta
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        if (r.Read())
                        {
                            int id_usuario = r.GetInt32(0);

                            // Generar token aleatorio de 20 caracteres
                            string token = Guid.NewGuid().ToString("N").Substring(0, 20);

                            // Usar otra conexión para actualizar el token (cerrando la anterior conexión)
                            using (var conexion2 = new MySqlConnection(cs))
                            {
                                await conexion2.OpenAsync();
                                var cmdUpdate = new MySqlCommand("UPDATE usuarios SET token=@Token WHERE id_usuario=@IdUsuario", conexion2);
                                cmdUpdate.Parameters.AddWithValue("@Token", token);
                                cmdUpdate.Parameters.AddWithValue("@IdUsuario", id_usuario);
                                await cmdUpdate.ExecuteNonQueryAsync();
                            }

                            // Crear respuesta con el id_usuario y token
                            var respuesta = new
                            {
                                id_usuario = id_usuario,
                                token = token
                            };

                            return new OkObjectResult(JsonConvert.SerializeObject(respuesta));
                        }
                        else
                        {
                            return new UnauthorizedObjectResult(new Error("Email/contraseña incorrectos"));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
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
