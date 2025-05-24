using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Espana_Finals
{
    public partial class DataBase
    {
        public MySqlConnection mySqlConnection { get; private set; }

        public DataBase()
        {
            string connection = "server=127.0.0.1;port=3306;database=machinevision;user=root;password=;SslMode=none;Pooling=true;";
            mySqlConnection = new MySqlConnection(connection);
        }

        public bool OpenSQLConnection()
        {
            try
            {
                if (mySqlConnection.State != System.Data.ConnectionState.Open)
                {
                    mySqlConnection.Open();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection error: " + ex.Message);
                return false;
            }
        }

        public void CloseSQLConnection()
        {
            if (mySqlConnection.State != System.Data.ConnectionState.Closed)
            {
                mySqlConnection.Close();
            }
        }

        public MySqlDataReader ExecuteQuery(string query)
        {
            if (OpenSQLConnection())
            {
                MySqlCommand cmd = new MySqlCommand(query, mySqlConnection);
                return cmd.ExecuteReader();
            }
            else
            {
                throw new InvalidOperationException("Connection not open.");
            }
        }
        public DataTable ExecuteSelectQuery(string query)
        {
            DataTable dt = new DataTable();

            try
            {
                if (OpenSQLConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, mySqlConnection))
                    {
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
            }
            finally
            {
                CloseSQLConnection();
            }

            return dt;
        }
        public bool ExecuteQueryWithParameters(string query, Dictionary<string, object> parameters)
        {
            try
            {
                if (OpenSQLConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, mySqlConnection))
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
                return false;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
                return false;
            }
            finally
            {
                CloseSQLConnection();
            }
        }
    }
}
