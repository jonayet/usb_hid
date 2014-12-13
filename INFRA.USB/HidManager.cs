using System.Collections.Generic;
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
