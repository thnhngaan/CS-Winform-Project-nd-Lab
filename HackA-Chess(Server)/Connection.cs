using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace HackA_Chess_Server_
{
    internal class Connection
    {
        private static string stringConnection = @"Server=localhost\SQLEXPRESS;Database=HackAChessDB;Trusted_Connection=True;TrustServerCertificate=True";
        public static SqlConnection GetSqlConnection()
        {
            return new SqlConnection(stringConnection);
        }
    }
}
