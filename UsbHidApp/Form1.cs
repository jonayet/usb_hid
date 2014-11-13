using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using INFRA.USB;
using INFRA.USB.Classes;
using VoltageCurrentGraphApp;

namespace UsbHidApp
{
    public partial class Form1 : Form
    {
        private HidInterface usbPort1;

        public Form1()
        {
            InitializeComponent();

            usbPort1 = new HidInterface(0x1FBD, 0x0003);
            usbPort1.OnDeviceAttached += new EventHandler(usbPort1_OnDeviceAttached);
            usbPort1.OnDeviceRemoved += new EventHandler(usbPort1_OnDeviceRemoved);
            usbPort1.OnReportReceived += usbPort1_OnReportReceived;
            usbPort1.ConnectTargetDevice();
        }

        void usbPort1_OnReportReceived(object sender, ReportRecievedEventArgs e)
        {
            ThreadHelperClass.SetText(this, textBox1, e.Report.UserData[0].ToString());
        }

        void usbPort1_OnDeviceRemoved(object sender, EventArgs e)
        {
            ThreadHelperClass.SetText(this, label1, "Removed");
        }

        void usbPort1_OnDeviceAttached(object sender, EventArgs e)
        {
            ThreadHelperClass.SetText(this, label1, "Attached");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[usbPort1.HidDevice.MaxInputReportLength * 20];
            int i = data.Length;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string vid = usbPort1.HidDevice.VendorID.ToString();
            sw.Stop();
            MessageBox.Show(sw.Elapsed.TotalMilliseconds.ToString());
        }
    }
}
