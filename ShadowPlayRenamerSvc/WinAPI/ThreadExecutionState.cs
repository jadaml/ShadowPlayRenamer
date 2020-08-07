// Copyright (c) 2020 Ádám Juhász
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JAL.ShadowPlayRenamer.WinAPI
{
    /// <summary>
    /// Represents a long-term thread execution state.
    /// </summary>
    public sealed class ThreadExecutionState : IDisposable
    {
        private const string m_kernel32 = "Kernel32.dll";

        // Documentation comment for this entry is copied from the Microsoft documentation and altered to refer to the entities specified in this program.
        /// <summary>Informs the system that the state being set should remain in effect until the next call that uses <see cref="m_continous"/> and one of the other state flags is cleared.</summary>
        private const ExecutionState m_continous = (ExecutionState)0x80000000u;

        private const string m_failWhale = "Failed to set the execution state.";

        // Documentation comment for this entry is copied from the Microsoft documentation and altered to refer to the entities specified in this program.
        /// <summary>
        /// Enables an application to inform the system that it is in use, thereby preventing the system from entering sleep or turning off the display while the application is running.
        /// </summary>
        /// <param name="esFlags">The thread's execution requirements.</param>
        /// <returns>
        /// <para>If the function succeeds, the return value is the previous thread execution state.</para>
        /// <para>If the function fails, the return value is <see cref="ExecutionState.NULL"/>.</para>
        /// </returns>
        /// <remarks>
        /// <para>The system automatically detects activities such as local keyboard or mouse input, server activity, and changing window focus. Activities that are not automatically detected include disk or CPU activity and video display.</para>
        /// <para>Calling <see cref="SetThreadExecutionState(ExecutionState)"/> without <see cref="m_continous"/> simply resets the idle timer; to keep the display or system in the working state, the thread must call <see cref="SetThreadExecutionState(ExecutionState)"/> periodically.</para>
        /// <para>To run properly on a power-managed computer, applications such as fax servers, answering machines, backup agents, and network management applications must use both <see cref="ExecutionState.SystemRequired"/> and <see cref="m_continous"/> when they process events. Multimedia applications, such as video players and presentation applications, must use <see cref="ExecutionState.DisplayRequired"/> when they display video for long periods of time without user input. Applications such as word processors, spreadsheets, browsers, and games do not need to call <see cref="SetThreadExecutionState(ExecutionState)"/>.</para>
        /// <para>The <see cref="ExecutionState.AwayModeRquired"/> value should be used only when absolutely necessary by media applications that require the system to perform background tasks such as recording television content or streaming media to other devices while the system appears to be sleeping. Applications that do not require critical background processing or that run on portable computers should not enable away mode because it prevents the system from conserving power by entering true sleep.</para>
        /// <para>To enable away mode, an application uses both <see cref="ExecutionState.AwayModeRquired"/> and <see cref="m_continous"/>; to disable away mode, an application calls <see cref="SetThreadExecutionState(ExecutionState)"/> with <see cref="m_continous"/> and clears <see cref="ExecutionState.AwayModeRquired"/>. When away mode is enabled, any operation that would put the computer to sleep puts it in away mode instead. The computer appears to be sleeping while the system continues to perform tasks that do not require user input. Away mode does not affect the sleep idle timer; to prevent the system from entering sleep when the timer expires, an application must also set the <see cref="ExecutionState.SystemRequired"/> value.</para>
        /// <para>The <see cref="SetThreadExecutionState(ExecutionState)"/> function cannot be used to prevent the user from putting the computer to sleep. Applications should respect that the user expects a certain behavior when they close the lid on their laptop or press the power button.</para>
        /// <para>This function does not stop the screen saver from executing.</para>
        /// </remarks>
        [DllImport(m_kernel32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        private extern static ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        [ThreadStatic]
        private static Queue<ExecutionState> m_pastExecutionState = new Queue<ExecutionState>();

        private bool m_disposed = false;
        private ExecutionState m_state;

        /// <summary>
        /// Gets or sets the current thread's <see cref="ExecutionState"/>.
        /// </summary>
        /// <value>
        /// <para>When read, it will retrieve the currently active execution state.</para>
        /// <para>When written, it will reset the idle timers.</para>
        /// </value>
        /// <remarks>
        /// When assigning the value to this property, it retains it's original value, but resets the idle timers.
        /// To make the changes permanent, create a new <see cref="ThreadExecutionState"/> and preserve the object
        /// as long as the state is needed to be held.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// When the value that is assigned to this property has either
        /// the <see cref="ExecutionState.AwayModeRquired"/> or
        /// the <see cref="ExecutionState.UserPresent"/>
        /// flags set.
        /// </exception>
        public static ExecutionState CurrentState
        {
            get => InternalSetThreadExecutionState(0) & ~m_continous;
            set
            {
                if (value.HasFlag(ExecutionState.AwayModeRquired)) throw new InvalidOperationException("This state isn't supported.");
                InternalSetThreadExecutionState(value & ~m_continous);
            }
        }

        ~ThreadExecutionState()
        {
            CoreDispose();
        }

        /// <summary>
        /// Creates a new <see cref="ThreadExecutionState"/> instance, that will hold the defined <paramref name="state"/>
        /// until it is freed.
        /// </summary>
        /// <param name="state">The new <see cref="ExecutionState"/> to keep.</param>
        /// <exception cref="InvalidOperationException">
        /// If the <see cref="ExecutionState.UserPresent"/> is set.
        /// </exception>
        public ThreadExecutionState(ExecutionState state) => m_pastExecutionState.Enqueue(InternalSetThreadExecutionState(m_state = state | m_continous));

        /// <summary>
        /// Restores the previous state before creating this object.
        /// </summary>
        public void Dispose()
        {
            CoreDispose();
            GC.SuppressFinalize(this);
        }

        public override string ToString() => (m_state & ~m_continous).ToString();

        /// <summary>
        /// Updates the thread execution state while also making some precondition checks before calling the
        /// native method.
        /// </summary>
        /// <param name="state">The new state to set.</param>
        /// <returns>The state that was set before.</returns>
        /// <exception cref="InvalidOperationException">
        /// If <see cref="ExecutionState.UserPresent"/> flag is set in <paramref name="state"/>.
        /// </exception>
        private static ExecutionState InternalSetThreadExecutionState(ExecutionState state)
        {
            if (state.HasFlag((ExecutionState)4)) throw new InvalidOperationException("This state isn't supported.");

            ExecutionState result = SetThreadExecutionState(state);

            if (result == 0) throw Marshal.GetLastWin32Error() == 0 ? new InvalidOperationException(m_failWhale) : Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

            Debug.Assert(result.HasFlag(m_continous), "Your assumption isn't true.");

            return result;
        }

        private void CoreDispose()
        {
            if (m_disposed) return;
            if (m_pastExecutionState.Count > 0) SetThreadExecutionState(m_pastExecutionState.Dequeue());
            m_disposed = true;
        }
    }
}
