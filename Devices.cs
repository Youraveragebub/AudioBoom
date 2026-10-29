using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AudioBoom
{
    class AudioDevice
    {
        public string Id;
        public string Name;
    }

    static class AudioDevices
    {
        const int RENDER = 0;
        const int CAPTURE = 1;
        const int ACTIVE = 1;

        [DllImport("ole32.dll")]
        static extern int PropVariantClear(ref PropVariant value);

        static int Flow(bool mic)
        {
            if (mic)
            {
                return CAPTURE;
            }
            return RENDER;
        }

        static AudioDevice Read(IMMDevice device)
        {
            AudioDevice item = new AudioDevice();
            device.GetId(out item.Id);
            item.Name = "Unknown device";

            IPropertyStore store;
            if (device.OpenPropertyStore(0, out store) == 0)
            {
                PropertyKey key = new PropertyKey();
                key.fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0");
                key.pid = 14;
                PropVariant value;
                if (store.GetValue(ref key, out value) == 0)
                {
                    if (value.vt == 31)
                    {
                        item.Name = Marshal.PtrToStringUni(value.p);
                    }
                    PropVariantClear(ref value);
                }
            }

            item.Name = Regex.Replace(item.Name, @"\(\d+- ", "(");
            return item;
        }

        public static List<AudioDevice> List(bool mic)
        {
            List<AudioDevice> found = new List<AudioDevice>();
            try
            {
                IMMDeviceEnumerator devices = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                IMMDeviceCollection all;
                devices.EnumAudioEndpoints(Flow(mic), ACTIVE, out all);
                int count;
                all.GetCount(out count);
                for (int i = 0; i < count; i++)
                {
                    IMMDevice device;
                    all.Item(i, out device);
                    found.Add(Read(device));
                }
            }
            catch
            {
            }
            return found;
        }

        public static AudioDevice Default(bool mic)
        {
            try
            {
                IMMDeviceEnumerator devices = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                IMMDevice device;
                if (devices.GetDefaultAudioEndpoint(Flow(mic), 0, out device) != 0)
                {
                    return null;
                }
                return Read(device);
            }
            catch
            {
                return null;
            }
        }

        public static bool MakeDefault(string id)
        {
            try
            {
                IPolicyConfig policy = (IPolicyConfig)new PolicyConfigCom();
                int failed = 0;
                for (int role = 0; role < 3; role++)
                {
                    if (policy.SetDefaultEndpoint(id, role) != 0)
                    {
                        failed = failed + 1;
                    }
                }
                return failed == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PropertyKey
    {
        public Guid fmtid;
        public int pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PropVariant
    {
        public ushort vt;
        public ushort r1;
        public ushort r2;
        public ushort r3;
        public IntPtr p;
        public IntPtr p2;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumeratorCom
    {
    }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceCollection
    {
        [PreserveSig] int GetCount(out int count);
        [PreserveSig] int Item(int index, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int context, IntPtr parameters, [MarshalAs(UnmanagedType.IUnknown)] out object result);
        [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore properties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetState(out int state);
    }

    [ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore
    {
        [PreserveSig] int GetCount(out int count);
        [PreserveSig] int GetAt(int index, out PropertyKey key);
        [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
        [PreserveSig] int Commit();
    }

    [ComImport, Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
    class PolicyConfigCom
    {
    }

    [ComImport, Guid("f8679f50-850a-41cf-9c72-430f290290c8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat(IntPtr a, IntPtr b);
        [PreserveSig] int GetDeviceFormat(IntPtr a, int b, IntPtr c);
        [PreserveSig] int ResetDeviceFormat(IntPtr a);
        [PreserveSig] int SetDeviceFormat(IntPtr a, IntPtr b, IntPtr c);
        [PreserveSig] int GetProcessingPeriod(IntPtr a, int b, IntPtr c, IntPtr d);
        [PreserveSig] int SetProcessingPeriod(IntPtr a, IntPtr b);
        [PreserveSig] int GetShareMode(IntPtr a, IntPtr b);
        [PreserveSig] int SetShareMode(IntPtr a, IntPtr b);
        [PreserveSig] int GetPropertyValue(IntPtr a, int b, IntPtr c, IntPtr d);
        [PreserveSig] int SetPropertyValue(IntPtr a, int b, IntPtr c, IntPtr d);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string id, int role);
        [PreserveSig] int SetEndpointVisibility(IntPtr a, int b);
    }
}
