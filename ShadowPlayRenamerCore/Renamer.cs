// Copyright (c) 2018 Ádám Juhász
// This file is part of ShadowPlayRenamerCore.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using static System.String;

namespace JAL.ShadowPlayRenamer
{
    public class Renamer
    {
        private IRenamerConfiguration configuration;
        private Regex                 pattern;

        public Renamer(IRenamerConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            configuration.PropertyChanged += Configuration_PropertyChanged;

            pattern = new Regex(configuration.Pattern);
        }

        private void Configuration_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IRenamerConfiguration.Pattern):
                case null:
                    pattern = new Regex(configuration.Pattern);
                    break;
            }
        }

        public string RenameFile(string filePath)
        {
            string baseDir   = GetDirectoryName(filePath);
            string fileName  = GetFileNameWithoutExtension(filePath);
            string extension = GetExtension(filePath);
            string finalPath;

            if (!pattern.IsMatch(fileName))
            {
                return filePath;
            }

            finalPath = Path.Combine(baseDir, pattern.Replace(fileName, MatchEvaluator) + extension);

            try
            {
                File.Move(filePath, finalPath);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }

                throw;
            }

            return finalPath;
        }

        private string MatchEvaluator(Match match)
        {
            return Format(configuration.OutputFormat, match.Groups["name"].Value,
                DateTime.ParseExact(match.Groups["date"].Value, configuration.InputDateFormat, CurrentCulture) +
                TimeSpan.ParseExact(match.Groups["time"].Value, configuration.InputTimeFormat, CurrentCulture));
        }
    }
}
