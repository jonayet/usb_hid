using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using INFRA.USB.HidHelper;

namespace INFRA.USB
{
    public class HidManager
    {
        public static List<HidDevice> GetDeviceList()
        {
            return HidDeviceDiscovery.GetDeviceList();
        }
    }
}
