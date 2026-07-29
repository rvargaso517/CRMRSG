using System.Data;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace CRMRSG.Models
{
    public static class DbConnectionFactory
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;

        public static IDbConnection GetConnection()
        {
            var conn = new MySqlConnection(ConnectionString);
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SET time_zone = '-06:00';";
                cmd.ExecuteNonQuery();
            }
            return conn;
        }
    }
}
