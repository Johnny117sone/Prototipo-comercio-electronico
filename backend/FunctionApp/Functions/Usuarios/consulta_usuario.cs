using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System;

namespace FunctionApp1
{
    public class consulta_usuario
    {
        // Clases para deserialización del JSON
        class Usuario
        {
            public int? id_usuario;
            public string? email;
            public string? nombre;
            public string? apellido_paterno;
            public string? apellido_materno;
            public DateTime? fecha_nacimiento;
            public long? telefono;
            public string? genero;
            public string? foto;  // foto en base 64
            public string? password;  // Contraseña añadida
        }

        class ParamConsultaUsuario
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

        [Function("consulta_usuario")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            try
            {
                string? email = req.Query["email"];
                if (email == null)
                {
                    string body = await new StreamReader(req.Body).ReadToEndAsync();
                    ParamConsultaUsuario? data = JsonConvert.DeserializeObject<ParamConsultaUsuario>(body);
                    if (data == null || string.IsNullOrEmpty(data.email)) 
                        throw new Exception("Se espera el email");
                    email = data.email;
                }

                string cs = DbConfig.GetConnectionString();

                var conexion = new MySqlConnection(cs);
                await conexion.OpenAsync();  // Asynchronous Open

                try
                {
                    // Consulta SQL para obtener los datos del usuario, incluyendo la contraseña
                    var cmd = new MySqlCommand("SELECT a.id_usuario, a.email, a.nombre," +
                                                "a.apellido_paterno,a.apellido_materno," +
                                                "a.fecha_nacimiento,a.telefono,a.genero," +
                                                "b.foto,length(b.foto), a.password " + // Añadido el campo de contraseña
                                                "FROM usuarios a LEFT OUTER JOIN fotos_usuarios b ON a.id_usuario=b.id_usuario " +
                                                "WHERE a.email=@email", conexion);
                    cmd.Parameters.AddWithValue("@email", email);

                    using (var r = await cmd.ExecuteReaderAsync())  // Asynchronous data reader
                    {
                        if (!r.Read())
                            throw new Exception("El email no existe");

                        var usuario_foto = new Usuario
                        {
                            id_usuario = r.GetInt32(0),
                            email = r.GetString(1),
                            nombre = r.GetString(2),
                            apellido_paterno = r.GetString(3),
                            apellido_materno = !r.IsDBNull(4) ? r.GetString(4) : null,
                            fecha_nacimiento = r.GetDateTime(5),
                            telefono = !r.IsDBNull(6) ? r.GetInt64(6) : null,
                            genero = !r.IsDBNull(7) ? r.GetString(7) : null,
                            password = r.GetString(10) // Asignando la contraseña
                        };

                        if (!r.IsDBNull(8))
                        {
                            var longitud = r.GetInt32(9);
                            byte[] foto = new byte[longitud];
                            r.GetBytes(8, 0, foto, 0, longitud);
                            usuario_foto.foto = Convert.ToBase64String(foto);
                        }

                        return new OkObjectResult(JsonConvert.SerializeObject(usuario_foto));
                    }
                }
                finally
                {
                    await conexion.CloseAsync();  // Asynchronously close the connection
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}
