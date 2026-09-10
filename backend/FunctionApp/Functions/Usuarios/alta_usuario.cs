using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System;

namespace FunctionApp1
{
    public class alta_usuario
    {
        // Clases para deserialización del JSON
        class Usuario
        {
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

        class ParamAltaUsuario
        {
            public Usuario? usuario;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [Function("alta_usuario")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                // Leer el cuerpo de la solicitud
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamAltaUsuario? data = JsonConvert.DeserializeObject<ParamAltaUsuario>(body);

                if (data == null || data.usuario == null)
                    throw new Exception("Se esperan los datos del usuario");

                Usuario? usuario = data.usuario;

                // Validaciones de los parámetros
                if (usuario.email == null || usuario.email == "") throw new Exception("Se debe ingresar el email");
                if (usuario.nombre == null || usuario.nombre == "") throw new Exception("Se debe ingresar el nombre");
                if (usuario.apellido_paterno == null || usuario.apellido_paterno == "") throw new Exception("Se debe ingresar el apellido_paterno");
                if (usuario.fecha_nacimiento == null) throw new Exception("Se debe ingresar la fecha de nacimiento");
                if (usuario.password == null || usuario.password == "") throw new Exception("Se debe ingresar la contraseña"); // Validación de contraseña

                string cs = DbConfig.GetConnectionString();

                using (var conexion = new MySqlConnection(cs))
                {
                    await conexion.OpenAsync();

                    MySqlTransaction transaccion = await conexion.BeginTransactionAsync();

                    try
                    {
                        // Inserción de datos del usuario en la base de datos
                        var cmd_1 = new MySqlCommand("INSERT INTO usuarios(id_usuario,email,nombre,apellido_paterno,apellido_materno,fecha_nacimiento,telefono,genero,password) VALUES (0,@email,@nombre,@apellido_paterno,@apellido_materno,@fecha_nacimiento,@telefono,@genero,@password)", conexion, transaccion);
                        cmd_1.Parameters.AddWithValue("@email", usuario.email);
                        cmd_1.Parameters.AddWithValue("@nombre", usuario.nombre);
                        cmd_1.Parameters.AddWithValue("@apellido_paterno", usuario.apellido_paterno);
                        cmd_1.Parameters.AddWithValue("@apellido_materno", usuario.apellido_materno);
                        cmd_1.Parameters.AddWithValue("@fecha_nacimiento", usuario.fecha_nacimiento);
                        cmd_1.Parameters.AddWithValue("@telefono", usuario.telefono);
                        cmd_1.Parameters.AddWithValue("@genero", usuario.genero);
                        cmd_1.Parameters.AddWithValue("@password", usuario.password); // Insertando la contraseña
                        await cmd_1.ExecuteNonQueryAsync();

                        // Obtener el id del usuario insertado
                        long id_usuario = cmd_1.LastInsertedId;

                        // Si se proporciona una foto, insertarla en la tabla fotos_usuarios
                        if (usuario.foto != null)
                        {
                            var cmd_2 = new MySqlCommand("INSERT INTO fotos_usuarios (foto,id_usuario) VALUES (@foto,@id_usuario)", conexion, transaccion);
                            cmd_2.Parameters.AddWithValue("@foto", Convert.FromBase64String(usuario.foto));
                            cmd_2.Parameters.AddWithValue("@id_usuario", id_usuario);
                            await cmd_2.ExecuteNonQueryAsync();
                        }

                        // Confirmar la transacción
                        await transaccion.CommitAsync();

                        return new OkObjectResult("Se dió de alta el usuario");
                    }
                    catch (Exception e)
                    {
                        // Si ocurre un error, hacer rollback
                        if (transaccion != null)
                        {
                            await transaccion.RollbackAsync(); // Rollback de la transacción
                        }
                        throw new Exception(e.Message);
                    }
                    finally
                    {
                        await conexion.CloseAsync();
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
