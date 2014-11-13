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
        private HidInterface _hidInterface;

        public Form1()
        {
            InitializeComponent();

            _hidInterface = new HidInterface(0x1FBD, 0x0003);
            _hidInterface.OnDeviceAttached += new EventHandler(usbPort1_OnDeviceAttached);
            _hidInterface.OnDeviceRemoved += new EventHandler(usbPort1_OnDeviceRemoved);
            _hidInterface.OnReportReceived += usbPort1_OnReportReceived;
            _hidInterface.ConnectTargetDevice();
        }

        Stopwatch sw = new Stopwatch();
        private bool _isNew = false;
        int i = 0;
        private double sum = 0;
        void usbPort1_OnReportReceived(object sender, ReportRecievedEventArgs e)
        {
            if (_isNew)
            {
                sw.Stop();
                i++;
                sum += sw.Elapsed.TotalMilliseconds;
                _isNew = false;
            }
            else
            {
                sw.Reset();
                sw.Start();
                _isNew = true;                
            }

            if (i > 100)
            {
                sum /= 100;
                ThreadHelperClass.SetText(this, textBox1, e.Report.UserData[0].ToString());
                ThreadHelperClass.SetText(this, label2, sum.ToString());
                i = 0;
                sum = 0;
            }
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
            byte[] data = new byte[_hidInterface.HidDevice.MaxInputReportLength * 20];
            int i = data.Length;
            
            
            string vid = _hidInterface.HidDevice.VendorID.ToString();
            sw.Stop();
            MessageBox.Show(sw.Elapsed.TotalMilliseconds.ToString());
        }
    }
}
