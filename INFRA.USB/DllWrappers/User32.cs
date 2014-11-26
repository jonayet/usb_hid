using System;
using System.Runtime.InteropServices;
using INFRA.USB.HelperClasses;

namespace INFRA.USB.DllWrappers
{
    internal static class User32
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient,IntPtr notificationFilter,Int32 flags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr RegisterDeviceNotification(IntPtr hwnd, Structures.DeviceBroadcastInterface oInterface, uint nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean UnregisterDeviceNotification(IntPtr handle);

        public static IntPtr RegisterForUsbEvents(IntPtr hWnd, Guid gClass)
        {
            var oInterfaceIn = new Structures.DeviceBroadcastInterface();
            oInterfaceIn.Size = Marshal.SizeOf(oInterfaceIn);
            oInterfaceIn.ClassGuid = gClass;
            oInterfaceIn.DeviceType = Constants.DEVTYP_DEVICEINTERFACE;
            oInterfaceIn.Reserved = 0;
            return RegisterDeviceNotification(hWnd, oInterfaceIn, Constants.DEVICE_NOTIFY_WINDOW_HANDLE);
        }
        /// <summary>
        /// Unregisters notifications. Can be used in form dispose
        /// </summary>
        /// <param name="hHandle">Handle returned from RegisterForUSBEvents</param>
        /// <returns>True if successful</returns>
        public static bool UnregisterForUsbEvents(IntPtr hHandle)
        {
            return UnregisterDeviceNotification(hHandle);
        }
    }
}

