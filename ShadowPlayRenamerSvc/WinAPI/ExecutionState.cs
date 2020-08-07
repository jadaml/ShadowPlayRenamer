using System;
using System.ComponentModel;

namespace JAL.ShadowPlayRenamer.WinAPI
{
    // Documentation comment for the values in this enumeration type is copied from the Microsoft documentation and altered to refer to the entities specified in this program.
    [Flags]
    public enum ExecutionState : uint
    {
        /// <summary>No flags are set or should be set.</summary>
        None             = 0,
        /// <summary>Forces the system to be in the working state by resetting the system idle timer.</summary>
        SystemRequired   = 0b_0000_0000_0000_0000_0000_0000_0000_0001u,
        /// <summary>Forces the display to be on by resetting the display idle timer.</summary>
        DisplayRequired  = 0b_0000_0000_0000_0000_0000_0000_0000_0010u,
        /// <summary>This value is not supported. If <see cref="UserPresent"/> is combined with other <see cref="ExecutionState"/> values, the call will fail and none of the specified states will be set.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This value is not supported.", true)]
        UserPresent      = 0b_0000_0000_0000_0000_0000_0000_0000_0100u,
        /// <summary>Enables away mode. This value can only be used with <see cref="ThreadExecutionState"/>'s constructor.</summary>
        /// <remarks>Away mode should be used only by media-recording and media-distribution applications that must perform critical background processing on desktop computers while the computer appears to be sleeping. See Remarks.</remarks>
        AwayModeRquired  = 0b_0000_0000_0000_0000_0000_0000_0100_0000u,
    }
}
