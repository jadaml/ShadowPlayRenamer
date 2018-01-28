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

using System;
using System.Collections;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace JAL.ShadowPlayRenamer.Service
{
    static class Program
    {
        private static AssemblyInstaller Installer => new AssemblyInstaller(Assembly.GetExecutingAssembly(), null)
        {
            UseNewContext = true,
        };

        private static bool IsServiceInstalled
        {
            get
            {
                using (ServiceController controller = new ServiceController("SPRSVC"))
                {
                    try
                    {
                        _ = controller.Status;
                    }
                    catch
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            ServiceBase[] ServicesToRun;

            switch (args.FirstOrDefault()?.ToUpper())
            {
                case "INSTALL":
                    InstallService();
                    return;
                case "UNINSTALL":
                    UninstallService();
                    return;
                case "START":
                    StartService();
                    return;
                case "STOP":
                    StopService();
                    return;
                case "MONITOR":
                    ConfigurationManager.AppSettings.Set("MonitorPath", args[1]);
                    return;
            }

            ServicesToRun = new ServiceBase[]
            {
                new MainService()
            };
            ServiceBase.Run(ServicesToRun);
        }

        private static void InstallService()
        {
            if (IsServiceInstalled) return;
            using (AssemblyInstaller installer = Installer)
            {
                IDictionary state = new Hashtable();
                try
                {
                    installer.Install(state);
                    installer.Commit(state);
                }
                catch
                {
                    installer.Rollback(state);
                    throw;
                }
            }
        }

        private static void UninstallService()
        {
            if (!IsServiceInstalled) return;
            using (AssemblyInstaller installer = Installer)
            {
                IDictionary state = new Hashtable();
                try
                {
                    installer.Uninstall(state);
                    installer.Commit(state);
                }
                catch
                {
                    installer.Rollback(state);
                    throw;
                }
            }
        }

        private static void StartService()
        {
            if (!IsServiceInstalled) return;
            using (ServiceController controller = new ServiceController("SPRSVC"))
            {
                if (controller.Status == ServiceControllerStatus.Stopped)
                    controller.Start();
            }
        }

        private static void StopService()
        {
            if (!IsServiceInstalled) _ = 0;
            using (ServiceController controller = new ServiceController("SPRSVC"))
            {
                if (controller.Status == ServiceControllerStatus.Running)
                    controller.Stop();
            }
        }
    }
}
