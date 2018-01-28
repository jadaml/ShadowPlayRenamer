using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static System.Console;

namespace JAL.ShadowPlayRenamer.Cli
{
    public class ArgumentsConfiguration : IRenamerConfiguration
    {
        private string pattern         = @"(?<name>.*)\s+(?<date>\d{2}\.\d{2}\.\d{4}) - (?<time>\d{2}\.\d{2}\.\d{2}.\d{2})";
        private string outputFormat    = @"{0} {1:yyyy\.MM\.dd} - {1:HH\.mm\.ss\.ff}";
        private string inputDateFormat = @"MM\.dd\.yyyy";
        private string inputTimeFormat = @"hh\.mm\.ss\.ff";

        public string Pattern         => pattern;
        public string OutputFormat    => outputFormat;
        public string InputDateFormat => inputDateFormat;
        public string InputTimeFormat => inputTimeFormat;

        public event PropertyChangedEventHandler PropertyChanged;

        internal bool? LoadArguments(string[] args, List<string> otherArguments, out string error)
        {
            string pattern         = null;
            string outputFormat    = null;
            string inputDateFormat = null;
            string inputTimeFormat = null;

            if (otherArguments == null)
            {
                throw new ArgumentNullException(nameof(otherArguments));
            }

            error = null;

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i][0] != '/')
                {
                    otherArguments.Add(args[i]);
                    continue;
                }

                if (args[i].Contains(':'))
                {
                    string[] pair = args[i].Split(new[] { ':' }, 2);

                    switch (pair[0])
                    {
                        case "/time":
                            inputTimeFormat = pair[1];
                            break;
                        case "/date":
                            inputDateFormat = pair[1];
                            break;
                        case "/output":
                            outputFormat = pair[1];
                            break;
                        case "/pattern":
                            pattern = pair[1];
                            break;
                        default:
                            error = $"Unrecognized option {pair[0]}";
                            return false;
                    }
                    continue;
                }
                switch (args[i])
                {
                    case "/time":
                        ++i;
                        inputTimeFormat = args[i];
                        break;
                    case "/date":
                        ++i;
                        inputDateFormat = args[i];
                        break;
                    case "/output":
                        ++i;
                        outputFormat = args[i];
                        break;
                    case "/pattern":
                        ++i;
                        pattern = args[i];
                        break;
                    case "/?":
                        return null;
                    default:
                        error = $"Unrecognized option {args[i]}";
                        return false;
                }
            }

            this.pattern         = pattern         ?? this.pattern;
            this.outputFormat    = outputFormat    ?? this.outputFormat;
            this.inputDateFormat = inputDateFormat ?? this.inputDateFormat;
            this.inputTimeFormat = inputTimeFormat ?? this.inputTimeFormat;

            OnPropertyChanged(null);

            return true;
        }

        public void PrintHelp()
        {

            WriteLine("This program will check a folder for files with the specified pattern" +
                " containing a date/time value, and renames them with a different date/time format.");
            WriteLine();
            WriteLine("Usage: {0} <Options> <Path>", Path.GetFileName(Assembly.GetExecutingAssembly().Location));
            WriteLine();
            WriteLine("Optioins:");
            WriteLine("  /Time <format>\tSpecifies the time format of the input file name to rename. Default: “hh\\.mm\\.ss\\.ff”");
            WriteLine("  /Date <format>\tSpecifies the date format of the input file name to rename. Default: “MM\\.dd\\.yyyy”");
            WriteLine("  /Output <format>\tSpecifies the new format of the files, where the" +
                " first argument is anything captured by the ‘name’ group, and the second is" +
                " the date/time captured by the ‘date’ and ‘time’ groups." +
                " Default: “{0} {1:yyyy\\.MM\\.dd} - {1:HH\\.mm\\.ss\\.ff}”");
            WriteLine("  /Pattern <pattern>\tSpecifies the regular expression that should" +
                " match the input file name (without the extension) capturing anything in the" +
                " ‘name’, ‘date’ and ‘time’ groups, where name is any arbitrary part of the" +
                " ‘name’ left unchanged, ‘date’ is the date part of the date, and ‘time’ is" +
                " time is the time part of the date and time part." +
                " Default: “(?<name>.*)\\s+(?<date>\\d{2}\\.\\d{2}\\.\\d{4}) - (?<time>\\d" +
                "{2}\\.\\d{2}\\.\\d{2}\\.\\d{2})”");
            WriteLine("  /?\tDisplays this help.");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
