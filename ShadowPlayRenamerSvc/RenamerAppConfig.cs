// Copyright (c) 2018 Ádám Juhász
// This file is part of ShadowPlayRenamerSvc.
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

using System.ComponentModel;
using System.Configuration;

namespace JAL.ShadowPlayRenamer.Service
{
    internal class RenamerAppConfig : IRenamerConfiguration
    {
        private readonly string pattern;
        private readonly string outputFormat;
        private readonly string inputDateFormat;
        private readonly string inputTimeFormat;

        public string Pattern => pattern;
        public string OutputFormat => outputFormat;
        public string InputDateFormat => inputDateFormat;
        public string InputTimeFormat => inputTimeFormat;

        public event PropertyChangedEventHandler PropertyChanged;

        public RenamerAppConfig()
        {
            pattern         = ConfigurationManager.AppSettings["Pattern"]         ?? @"(?<name>.*)\s+(?<date>\d{2}\.\d{2}\.\d{4}) - (?<time>\d{2}\.\d{2}\.\d{2}.\d{2})";
            outputFormat    = ConfigurationManager.AppSettings["OutputFormat"]    ?? @"{0} {1:yyyy\.MM\.dd} - {1:HH\.mm\.ss\.ff}";
            inputDateFormat = ConfigurationManager.AppSettings["InputDateFormat"] ?? @"MM\.dd\.yyyy";
            inputTimeFormat = ConfigurationManager.AppSettings["InputTimeFormat"] ?? @"hh\.mm\.ss\.ff";
        }
    }
}
