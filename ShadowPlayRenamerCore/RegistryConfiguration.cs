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

using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JAL.ShadowPlayRenamer
{
    public class RegistryConfiguration : IRenamerConfiguration
    {
        private const string defaultPattern      = @"(?<name>.*)\s+(?<date>\d{2}\.\d{2}\.\d{4}) - (?<time>\d{2}\.\d{2}\.\d{2}.\d{2})";
        private const string defaultOutputFormat = @"{0} {1:yyyy\.MM\.dd} - {1:HH\.mm\.ss\.ff}";
        private const string defaultInputDate    = @"MM\.dd\.yyyy";
        private const string defaultInputTime    = @"hh\.mm\.ss\.ff";

        private RegistryKey baseKey;

        public string Pattern
        {
            get => baseKey?.GetValue(nameof(Pattern), defaultPattern, RegistryValueOptions.DoNotExpandEnvironmentNames)
                .ToString() ?? defaultPattern;
            set
            {
                if (Pattern == value) return;
                baseKey.SetValue(nameof(Pattern), value, RegistryValueKind.String);
                OnPropertyChanged();
            }
        }

        public string OutputFormat
        {
            get => baseKey?.GetValue(nameof(OutputFormat), defaultOutputFormat, RegistryValueOptions.DoNotExpandEnvironmentNames)
                .ToString() ?? defaultOutputFormat;
            set
            {
                if (OutputFormat == value) return;
                baseKey.SetValue(nameof(OutputFormat), value, RegistryValueKind.String);
                OnPropertyChanged();
            }
        }

        public string InputDateFormat
        {
            get => baseKey?.GetValue(nameof(InputDateFormat), defaultInputDate, RegistryValueOptions.DoNotExpandEnvironmentNames)
                .ToString() ?? defaultInputDate;
            set
            {
                if (InputDateFormat == value) return;
                baseKey.SetValue(nameof(InputDateFormat), value, RegistryValueKind.String);
                OnPropertyChanged();
            }
        }

        public string InputTimeFormat
        {
            get => baseKey?.GetValue(nameof(InputTimeFormat), defaultInputTime, RegistryValueOptions.DoNotExpandEnvironmentNames)
                .ToString() ?? defaultInputTime;
            set
            {
                if (InputTimeFormat == value) return;
                baseKey.SetValue(nameof(InputTimeFormat), value, RegistryValueKind.String);
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public RegistryConfiguration()
        {
            baseKey = Registry.CurrentUser.OpenSubKey(@"Software\JAL\ShadowPlayRenamer", true);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
