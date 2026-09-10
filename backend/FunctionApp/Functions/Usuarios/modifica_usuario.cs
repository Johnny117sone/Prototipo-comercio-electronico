using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace FunctionApp1
{
    public class modifica_usuario
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

        class ParamModificaUsuario
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

        [Function("modifica_usuario")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamModificaUsuario? data = JsonConvert.DeserializeObject<ParamModificaUsuario>(body);

                if (data == null || data.usuario == null) throw new Exception("Se esperan los datos del usuario");

                Usuario? usuario = data.usuario;

                // Validación de los parámetros
                if (usuario.email == null || usuario.email == "") throw new Exception("Se debe ingresar el email");
                if (usuario.nombre == null || usuario.nombre == "") throw new Exception("Se debe ingresar el nombre");
                if (usuario.apellido_paterno == null || usuario.apellido_paterno == "") throw new Exception("Se debe ingresar el apellido_paterno");
                if (usuario.fecha_nacimiento == null) throw new Exception("Se debe ingresar la fecha de nacimiento");

                string cs = DbConfig.GetConnectionString();

                using (var conexion = new MySqlConnection(cs))
                {
                    await conexion.OpenAsync();

                    using (var transaccion = await conexion.BeginTransactionAsync()) // Usando transacciones asincrónicas
                    {
                        try
                        {
                            // Actualización de datos básicos del usuario
                            var cmd_2 = new MySqlCommand("UPDATE usuarios SET nombre=@nombre, apellido_paterno=@apellido_paterno, apellido_materno=@apellido_materno, fecha_nacimiento=@fecha_nacimiento, telefono=@telefono, genero=@genero WHERE email=@email", conexion, transaccion);
                            cmd_2.Parameters.AddWithValue("@nombre", usuario.nombre);
                            cmd_2.Parameters.AddWithValue("@apellido_paterno", usuario.apellido_paterno);
                            cmd_2.Parameters.AddWithValue("@apellido_materno", usuario.apellido_materno);
                            cmd_2.Parameters.AddWithValue("@fecha_nacimiento", usuario.fecha_nacimiento);
                            cmd_2.Parameters.AddWithValue("@telefono", usuario.telefono);
                            cmd_2.Parameters.AddWithValue("@genero", usuario.genero);
                            cmd_2.Parameters.AddWithValue("@email", usuario.email);
                            await cmd_2.ExecuteNonQueryAsync();

                            // Si se proporciona una nueva contraseña, actualízala
                            if (!string.IsNullOrEmpty(usuario.password))
                            {
                                var cmd_3 = new MySqlCommand("UPDATE usuarios SET password=@password WHERE email=@email", conexion, transaccion);
                                cmd_3.Parameters.AddWithValue("@password", usuario.password);  // Actualizando la contraseña
                                cmd_3.Parameters.AddWithValue("@email", usuario.email);
                                await cmd_3.ExecuteNonQueryAsync();
                            }

                            // Eliminar la foto actual y agregar la nueva foto si se proporciona
                            var cmd_4 = new MySqlCommand("DELETE FROM fotos_usuarios WHERE id_usuario=(SELECT id_usuario FROM usuarios WHERE email=@email)", conexion, transaccion);
                            cmd_4.Parameters.AddWithValue("@email", usuario.email);
                            await cmd_4.ExecuteNonQueryAsync();

                            if (usuario.foto != null)
                            {
                                var cmd_5 = new MySqlCommand("INSERT INTO fotos_usuarios (foto, id_usuario) VALUES (@foto, (SELECT id_usuario FROM usuarios WHERE email=@email))", conexion, transaccion);
                                cmd_5.Parameters.AddWithValue("@foto", Convert.FromBase64String(usuario.foto));
                                cmd_5.Parameters.AddWithValue("@email", usuario.email);
                                await cmd_5.ExecuteNonQueryAsync();
                            }

                            // Confirmar transacción
                            await transaccion.CommitAsync();

                            return new OkObjectResult("Se modificó el usuario");
                        }
                        catch (Exception e)
                        {
                            // Rollback si algo falla
                            await transaccion.RollbackAsync();
                            throw new Exception(e.Message);
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
