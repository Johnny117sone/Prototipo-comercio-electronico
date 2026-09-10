using System;

namespace FunctionApp1
{
    /// <summary>
    /// Construye la cadena de conexión a la base de datos a partir de las
    /// variables de entorno (Server, UserID, Password, Database).
    ///
    /// Antes, este mismo bloque de 5 líneas estaba copiado y pegado en
    /// 10 archivos distintos. Cualquier cambio (por ejemplo, agregar un
    /// parámetro de conexión) requería editar los 10 archivos por igual,
    /// con el riesgo de que alguno quedara desactualizado. Ahora es un
    /// único punto de verdad.
    /// </summary>
    public static class DbConfig
    {
        public static string GetConnectionString()
        {
            string? server = Environment.GetEnvironmentVariable("Server");
            string? userId = Environment.GetEnvironmentVariable("UserID");
            string? password = Environment.GetEnvironmentVariable("Password");
            string? database = Environment.GetEnvironmentVariable("Database");

            return $"Server={server};UserID={userId};Password={password};Database={database};SslMode=Preferred;";
        }
    }
}
