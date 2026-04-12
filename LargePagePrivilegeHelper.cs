using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;




namespace Installer
{
    // ── P/Invoke ───────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    struct LSA_UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LSA_OBJECT_ATTRIBUTES
    {
        public uint Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public long Luid;
        public uint Attributes;
    }


    public class LargePagePrivilegeHelper
    {
        [DllImport("advapi32.dll")]
        static extern uint LsaOpenPolicy(
            ref LSA_UNICODE_STRING SystemName,
            ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
            uint DesiredAccess,
            out IntPtr PolicyHandle);

        [DllImport("advapi32.dll")]
        static extern uint LsaAddAccountRights(
            IntPtr PolicyHandle,
            IntPtr AccountSid,
            LSA_UNICODE_STRING[] UserRights,
            uint CountOfRights);

        [DllImport("advapi32.dll")]
        static extern uint LsaClose(IntPtr ObjectHandle);

        [DllImport("advapi32.dll")]
        static extern uint LsaNtStatusToWinError(uint status);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            uint DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool LookupPrivilegeValue(
            string lpSystemName,
            string lpName,
            out long lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle,
            bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            uint BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        static extern UIntPtr GetLargePageMinimum();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAlloc(
            IntPtr lpAddress, UIntPtr dwSize,
            uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualFree(
            IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);


        const uint POLICY_CREATE_ACCOUNT = 0x00000010;
        const uint POLICY_LOOKUP_NAMES = 0x00000800;
        const uint TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        const uint TOKEN_QUERY = 0x00000008;
        const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint MEM_LARGE_PAGES = 0x20000000;
        const uint MEM_RELEASE = 0x00008000;
        const uint PAGE_READWRITE = 0x04;


        // ── internal helpers ──────────────────────────────────
        static bool EnableLockMemoryPrivilege()
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr token))
            {
                Debug.WriteLine($"[LargePage] OpenProcessToken failed: {Marshal.GetLastWin32Error()}");
                return false;
            }

            try
            {
                if (!LookupPrivilegeValue(String.Empty, "SeLockMemoryPrivilege", out long luid))
                {
                    Debug.WriteLine($"[LargePage] LookupPrivilegeValue failed: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                var tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                if (!AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    Debug.WriteLine($"[LargePage] AdjustTokenPrivileges failed: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                int err = Marshal.GetLastWin32Error();
                if (err != 0)
                {
                    // 1300 = ERROR_NOT_ALL_ASSIGNED: privilege not in token
                    Debug.WriteLine($"[LargePage] AdjustTokenPrivileges partial failure: {err}");
                    return false;
                }

                return true;
            }
            finally
            {
                CloseHandle(token);
            }
        }


        // ── public API ────────────────────────────────────────

        /// <summary>
        /// Tests whether the current process can allocate large pages
        /// right now. Only returns true if the privilege is both
        /// present in the token (i.e. granted before this logon
        /// session) AND large-page allocation succeeds.
        /// </summary>
        public static bool HasPrivilege()
        {
            UIntPtr lpSize = GetLargePageMinimum();
            if (lpSize == UIntPtr.Zero)
            {
                Debug.WriteLine("[LargePage] GetLargePageMinimum returned 0");
                return false;
            }

            if (!EnableLockMemoryPrivilege())
            {
                Debug.WriteLine("[LargePage] EnableLockMemoryPrivilege failed");
                return false;
            }

            Debug.WriteLine("[LargePage] EnableLockMemoryPrivilege succeeded");

            IntPtr ptr = VirtualAlloc(IntPtr.Zero, lpSize, MEM_RESERVE | MEM_COMMIT | MEM_LARGE_PAGES, PAGE_READWRITE);
            if (ptr == IntPtr.Zero)
            {
                Debug.WriteLine($"[LargePage] VirtualAlloc failed: {Marshal.GetLastWin32Error()}, size: {lpSize}");
                return false;
            }

            VirtualFree(ptr, UIntPtr.Zero, MEM_RELEASE);
            Debug.WriteLine("[LargePage] Large page allocation succeeded");
            return true;
        }

        /// <summary>
        /// Adds SeLockMemoryPrivilege to the current user's account.
        /// Requires the process to be elevated (admin).
        /// The privilege only takes effect after a reboot / re-logon.
        /// </summary>
        public static bool Grant(out string errorMessage)
        {
            errorMessage = string.Empty;

            var sid = WindowsIdentity.GetCurrent().User;
            if (sid == null)
            {
                errorMessage = "Failed to acquire SID";
                return false;
            }

            byte[] sidBytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBytes, 0);
            IntPtr sidPtr = Marshal.AllocHGlobal(sidBytes.Length);
            Marshal.Copy(sidBytes, 0, sidPtr, sidBytes.Length);

            try
            {
                var sysName = new LSA_UNICODE_STRING();
                var objAttr = new LSA_OBJECT_ATTRIBUTES
                {
                    Length = (uint)Marshal.SizeOf<LSA_OBJECT_ATTRIBUTES>()
                };

                uint status = LsaOpenPolicy(ref sysName, ref objAttr, POLICY_CREATE_ACCOUNT | POLICY_LOOKUP_NAMES, out IntPtr policy);
                if (status != 0)
                {
                    errorMessage = $"LsaOpenPolicy Failed: ({LsaNtStatusToWinError(status)})";
                    return false;
                }

                try
                {
                    const string RIGHT = "SeLockMemoryPrivilege";
                    IntPtr rightPtr = Marshal.StringToHGlobalUni(RIGHT);

                    var right = new LSA_UNICODE_STRING
                    {
                        Buffer = rightPtr,
                        Length = (ushort)(RIGHT.Length * 2),
                        MaximumLength = (ushort)((RIGHT.Length + 1) * 2)
                    };

                    status = LsaAddAccountRights(policy, sidPtr, new[] { right }, 1);

                    Marshal.FreeHGlobal(rightPtr);

                    if (status != 0)
                    {
                        errorMessage = $"LsaAddAccountRights Failed: ({LsaNtStatusToWinError(status)})";
                        return false;
                    }

                    return true;
                }
                finally
                {
                    LsaClose(policy);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(sidPtr);
            }
        }

        /// <summary>
        /// Ensures large-page support is available. If it already
        /// works, does nothing. Otherwise grants the privilege.
        ///
        /// Returns true if large pages are usable NOW or will be
        /// after a reboot. Check <paramref name="rebootRequired"/>
        /// to know which case applies.
        /// </summary>
        public static bool EnsureGranted(out bool rebootRequired, out string errorMessage)
        {
            rebootRequired = false;
            errorMessage = string.Empty;

            if (HasPrivilege())
            {
                return true;
            }

            if (!Grant(out errorMessage))
            {
                return false;
            }

            // Grant wrote to the LSA policy database, but the
            // current log on session's token was created before
            // that write. The privilege will only appear in
            // tokens created at the next log on.
            rebootRequired = true;
            return true;
        }
    }
}
