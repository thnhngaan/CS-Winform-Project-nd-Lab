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
        //private static string stringConnection = @"Data Source=LVL-ITK20\MSSQLSERVER02;Initial Catalog=HackAChessDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

        private static string stringConnection = @"Data Source=localhost;Initial Catalog=HackAChessDB;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";
        public static SqlConnection GetSqlConnection()
        {
            return new SqlConnection(stringConnection);
        }
    }
}
