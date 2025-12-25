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
        private static string stringConnection = @"Data Source=localhost;Initial Catalog=HackAChessDB;Integrated Security=True;Trust Server Certificate=True";
        public static SqlConnection GetSqlConnection()
        {
            return new SqlConnection(stringConnection);
        }
    }
}
