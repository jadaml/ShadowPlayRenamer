using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Console;
using static System.Environment;

namespace JAL.ShadowPlayRenamer.Cli
{
    class Program
    {
        private static int Main(string[] args)
        {
            bool?                  result;
            ArgumentsConfiguration config         = new ArgumentsConfiguration();
            List<string>           otherArguments = new List<string>();
            Assembly               assembly       = Assembly.GetEntryAssembly();

            WriteLine("{0} {1}",
                assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
            WriteLine("Version {0} ({1})", VersionInfo.ProductVersion, VersionInfo.FileVersion);
            WriteLine("This program comes with ABSOLUTELY NO WARRANTY;");
            WriteLine("This is free software, and you are welcome to redistribute it under certain conditions;");
            WriteLine("For more information read the COPYING file next the executable.");

            result = config.LoadArguments(args, otherArguments, out string error);

            if (!result ?? true)
            {
                if (error != null) Error.WriteLine(error);
                config.PrintHelp();
                return result == null ? 0 : -1;
            }

            string sourceDir = otherArguments.ElementAtOrDefault(0) ?? CurrentDirectory;

            Renamer renamer = new Renamer(null);

            foreach (string filePath in Directory.GetFiles(sourceDir, "*.mp4", SearchOption.AllDirectories))
            {
                Write("Renaming “{0}” ... ", filePath);
                try
                {
                    WriteLine("Done => “{1}”", renamer.RenameFile(filePath));
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    WriteLine("Failed. {0}: {1}", ex.GetType(), ex.Message);
                }
                catch (Exception ex)
                {
                    WriteLine(ex);
                    return -1;
                }
            }

            return 0;
        }
    }
}
