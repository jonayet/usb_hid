using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using INFRA.USB;

namespace UsbHidApp
{
    public partial class Form1 : Form
    {
        private UsbPort usbPort1;

        public Form1()
        {
            InitializeComponent();

            usbPort1 = new UsbPort(0x1FBD, 0x0003);
            usbPort1.OnDeviceAttached += new EventHandler(usbPort1_OnDeviceAttached);
            usbPort1.OnDeviceRemoved += new EventHandler(usbPort1_OnDeviceRemoved);
            usbPort1.OnDataRecieved += new DataRecievedEventHandler(usbPort1_OnDataRecieved);
            usbPort1.CheckDevice();
        }

        void usbPort1_OnDataRecieved(object sender, DataRecievedEventArgs e)
        {
            textBox1.Text = e.Data.ToString();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            usbPort1.RegisterHandle(Handle);
            base.OnHandleCreated(e);
        }

        protected override void WndProc(ref Message m)
        {
            usbPort1.ParseMessages(ref m);
            base.WndProc(ref m);
        }

        void usbPort1_OnDeviceRemoved(object sender, EventArgs e)
        {
            label1.Text = "Removed";
        }

        void usbPort1_OnDeviceAttached(object sender, EventArgs e)
        {
            label1.Text = "Connected";
        }
    }
}
