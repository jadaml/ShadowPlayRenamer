// This file is based on the online documentation:
// <https://docs.microsoft.com/en-us/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer#setting-service-status>

using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace JAL.ShadowPlayRenamer.Service.Extension
{
    public class StatusReportingServiceBase : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        protected bool SetServiceStatus(ref ServiceStatus serviceStatus)
        {
            return SetServiceStatus(ServiceHandle, ref serviceStatus);
        }
    }

    public enum ServiceType : int
    {
        ServiceKernelDriver                  = 0x0000_0001,
        ServiceFileSystemDriver              = 0x0000_0002,
        ServiceWin32OwnProcess               = 0x0000_0010,
        ServiceWin32ShareProcess             = 0x0000_0020,
        ServiceUserOwnProcess                = 0x0000_0050,
        ServiceUserShareProcess              = 0x0000_0060,
        ServiceWin32OwnInteractiveProcess    = 0x0000_0110,
        ServiceWin32SharedInteractiveProcess = 0x0000_0120,
    }

    public enum ServiceState : int
    {
        ServiceStopped         = 0x0000_0001,
        ServiceStartPending    = 0x0000_0002,
        ServiceStopPending     = 0x0000_0003,
        ServiceRunning         = 0x0000_0004,
        ServiceContinuePending = 0x0000_0005,
        ServicePausePending    = 0x0000_0006,
        ServicePaused          = 0x0000_0007,
    }

    public enum ControlsAccepted : int
    {
        ServiceAcceptStop          = 0x0000_0001,
        ServiceAcceptPauseContinue = 0x0000_0002,
        ServiceAcceptShutdown      = 0x0000_0004,
        ServiceAcceptParamChange   = 0x0000_0008,
        ServiceAcceptNetBindChange = 0x0000_0010,
        ServiceAcceptPreShutdown   = 0x0000_0100,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public ServiceType dwServiceType;
        public ServiceState dwCurrentState;
        public ControlsAccepted dwControlsAccepted;
        public int          dwWin32ExitCode;
        public int          dwServiceSpecificExitCode;
        public int          dwCheckPoint;
        public int          dwWaitHint;
    };
}
