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
    public class consulta_articulos
    {
        // Parámetros de entrada
        class ParamConsultaArticulos
        {
            public string? palabra_clave;
            public int? id_usuario;
            public string? token;
        }

        // Artículo como respuesta de consulta
        class ArticuloConsulta
        {
            public int id_articulo;
            public string nombre;
            public string descripcion;
            public double precio;
            public string foto;  // Base64
        }

        // Estructura de error
        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [Function("consulta_articulos")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                ParamConsultaArticulos? p = JsonConvert.DeserializeObject<ParamConsultaArticulos>(body);

                if (p == null || string.IsNullOrEmpty(p.palabra_clave))
                    throw new Exception("Se debe ingresar la palabra clave");

                if (p.id_usuario == null)
                    throw new Exception("Se debe ingresar el id_usuario");

                if (string.IsNullOrEmpty(p.token))
                    throw new Exception("Se debe ingresar el token");

                string cs = DbConfig.GetConnectionString();

                using var conexion = new MySqlConnection(cs);
                await conexion.OpenAsync();

                if (!await VerificaToken(conexion, p.id_usuario.Value, p.token))
                    return new UnauthorizedObjectResult(new Error("Token inválido"));

                var cmd = new MySqlCommand(@"
                    SELECT s.id_articulo, s.nombre, s.descripcion, s.precio, f.foto, LENGTH(f.foto)
                    FROM stock s
                    JOIN fotos_articulos f ON s.id_articulo = f.id_articulo
                    WHERE s.descripcion LIKE @clave", conexion);

                cmd.Parameters.AddWithValue("@clave", "%" + p.palabra_clave + "%");

                var articulos = new List<ArticuloConsulta>();

                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var art = new ArticuloConsulta
                    {
                        id_articulo = r.GetInt32(0),
                        nombre = r.GetString(1),
                        descripcion = r.GetString(2),
                        precio = r.GetDouble(3),
                        foto = ""
                    };

                    if (!r.IsDBNull(4))
                    {
                        int len = r.GetInt32(5);
                        byte[] foto = new byte[len];
                        r.GetBytes(4, 0, foto, 0, len);
                        art.foto = Convert.ToBase64String(foto);
                    }

                    articulos.Add(art);
                }

                return new OkObjectResult(JsonConvert.SerializeObject(articulos));
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(ex.Message)));
            }
        }

        // Validación del token
        private async Task<bool> VerificaToken(MySqlConnection conexion, int id_usuario, string token)
        {
            var cmd = new MySqlCommand("SELECT 1 FROM usuarios WHERE id_usuario = @id AND token = @token", conexion);
            cmd.Parameters.AddWithValue("@id", id_usuario);
            cmd.Parameters.AddWithValue("@token", token);

            using var r = await cmd.ExecuteReaderAsync();
            return r.HasRows;
        }
    }
}
