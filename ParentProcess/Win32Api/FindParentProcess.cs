using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;

namespace ParentProcess.Win32Api
{
    
    /// <summary>
    /// https://stackoverflow.com/questions/394816/how-to-get-parent-process-in-net-in-managed-way
    /// A utility class to determine a process parent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FindParentProcess
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref FindParentProcess processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(int id)
        {
            Process process = Process.GetProcessById(id);
            return GetParentProcess(process.Handle);
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            FindParentProcess parentProcessUtilities = new FindParentProcess();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref parentProcessUtilities, Marshal.SizeOf(parentProcessUtilities), out returnLength);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(parentProcessUtilities.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }

        /// <summary>
        /// https://stackoverflow.com/questions/7189117/find-all-child-processes-of-my-own-net-process-find-out-if-a-given-process-is
        /// </summary>
        public static IEnumerable<Process> FindProcessesSpawnedBy(UInt32 parentProcessId)
        {
            IList<Process> processes = new List<Process>();
            // NOTE: Process Ids are reused!
            System.Management.ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT * " +
                "FROM Win32_Process " +
                "WHERE ParentProcessId=" + parentProcessId);
            ManagementObjectCollection collection = searcher.Get();
            if (collection.Count > 0)
            {
                foreach (var item in collection)
                {
                    UInt32 childProcessId = (UInt32)item["ProcessId"];
                    if (childProcessId != parentProcessId)
                    {
                        Process childProcess = Process.GetProcessById((int)childProcessId);
                        processes.Add(childProcess);
                    }
                }
            }
            return processes;
        }
    }
}
