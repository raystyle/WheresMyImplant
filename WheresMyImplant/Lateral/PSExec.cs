﻿using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    class PSExec : IDisposable
    {
        String serviceName;
        IntPtr hServiceManager;
        IntPtr hSCObject;

        Boolean disposed;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public PSExec(String serviceName)
        {
            this.serviceName = serviceName;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public PSExec()
        {
            this.serviceName = GenerateUuid(12);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        ~PSExec()
        {
            Dispose();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            if (!disposed)
            {
                Delete();
            }
            disposed = true;
            if (IntPtr.Zero != hSCObject)
            {
                Advapi32.CloseServiceHandle(hSCObject);
            }

            if (IntPtr.Zero != hServiceManager)
            {
                kernel32.CloseHandle(hServiceManager);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public bool Connect(String machineName)
        {
            hServiceManager = Advapi32.OpenSCManager(
                null, null, Winsvc.dwSCManagerDesiredAccess.SC_MANAGER_CONNECT | Winsvc.dwSCManagerDesiredAccess.SC_MANAGER_CREATE_SERVICE
            );

            if (IntPtr.Zero == hServiceManager)
            {
                Console.WriteLine("[-] Failed to connect service controller {0}", machineName);
                return false;
            }

            Console.WriteLine("[+] Connected to {0}", machineName);
            Console.WriteLine("[+] Recieved handle {0}", hServiceManager.ToInt64());
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean Create(String lpBinaryPathName)
        {
            Console.WriteLine("[*] Creating service {0}", serviceName);
            //Console.WriteLine(lpBinaryPathName);
            IntPtr hSCObject = Advapi32.CreateService(
                hServiceManager,
                serviceName, serviceName,
                Winsvc.dwDesiredAccess.SERVICE_ALL_ACCESS,
                Winsvc.dwServiceType.SERVICE_WIN32_OWN_PROCESS,
                Winsvc.dwStartType.SERVICE_DEMAND_START,
                Winsvc.dwErrorControl.SERVICE_ERROR_IGNORE,
                lpBinaryPathName,
                String.Empty, null, String.Empty, null, null
            );

            if (IntPtr.Zero == hSCObject)
            {
                Console.WriteLine("[-] Failed to create service");
                Console.WriteLine(Marshal.GetLastWin32Error());
                return false;
            }

            Advapi32.CloseServiceHandle(hSCObject);
            Console.WriteLine("[+] Created service");
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        public Boolean Open()
        {
            hSCObject = Advapi32.OpenService(hServiceManager, serviceName, Winsvc.dwDesiredAccess.SERVICE_ALL_ACCESS);

            if (IntPtr.Zero == hSCObject)
            {
                Console.WriteLine("[-] Failed to open service");
                Console.WriteLine(Marshal.GetLastWin32Error());
                return false;
            }

            Console.WriteLine("[+] Opened service");
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        public Boolean Start()
        {
            if (!Advapi32.StartService(hSCObject, 0, null))
            {
                Int32 error = Marshal.GetLastWin32Error();
                if (error != 1053)
                {
                    Console.WriteLine("[-] Failed to start service");
                    Console.WriteLine(error);
                    return false;
                }
            }
            Console.WriteLine("[+] Started service");
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean Delete()
        {
            if (!Advapi32.DeleteService(hSCObject))
            {
                Console.WriteLine("[-] Failed to delete service");
                Console.WriteLine(Marshal.GetLastWin32Error());
                return false;
            }
            Console.WriteLine("[+] Deleted service");
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static String GenerateUuid(int length)
        {
            Random random = new Random();
            const String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new String(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}