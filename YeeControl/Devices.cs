﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YeeControl.Helpers;
using YeelightAPI;

namespace YeeControl
{
    public partial class Devices : Form
    {

        List<YeeControlDevice> yeelightControlDevices = new List<YeeControlDevice>();

        public Devices()
        {
            InitializeComponent();
        }

        private void Devices_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "YeeControl " + GlobalVariables.VERSION + " | www.yeecontrol.com";

            yeelightControlDevices = YeeControlDeviceHelper.GetYeeControlDevices();
            RefreshDeviceList();
            groupBoxActions.Enabled = false;
        }

        private async void button_Discover_Click(object sender, EventArgs e)
        {
            button_Discover.BeginInvoke((MethodInvoker)delegate ()
            {
                button_Discover.Enabled = false;
            });
            button_Open.BeginInvoke((MethodInvoker)delegate ()
            {
                button_Open.Enabled = false;
            });
            await GetDevicesAsync();
            button_Discover.BeginInvoke((MethodInvoker)delegate ()
            {
                button_Discover.Enabled = true;
            });
            button_Open.BeginInvoke((MethodInvoker)delegate ()
            {
                button_Open.Enabled = true;
            });

            YeeControlDeviceHelper.SaveYeeControlDevices(yeelightControlDevices);
            RefreshDeviceList();
        }

        private void OnDeviceFound(Device device)
        {
            YeeControlDevice ycd = new YeeControlDevice();
            ycd.Hostname = device.Hostname;

            foreach(YeeControlDevice y in yeelightControlDevices)
            {
                if (y.Hostname == ycd.Hostname)
                    return;
            }

            yeelightControlDevices.Add(ycd);

            RefreshDeviceList();
        }

        private async Task GetDevicesAsync()
        {
            var progressReporter = new Progress<Device>(OnDeviceFound);

            await DeviceLocator.DiscoverAsync(progressReporter);

            IEnumerable<Device> discoveredDevices = await DeviceLocator.DiscoverAsync(progressReporter);
        }

        private void RefreshDeviceList()
        {
            groupBoxActions.Enabled = false;
            listBox_Devices.Items.Clear();
            foreach (YeeControlDevice yeelightControlDevice in yeelightControlDevices)
            {
                listBox_Devices.Items.Add(yeelightControlDevice.Hostname);
            }

            if(listBox_Devices.Items.Count == 0)
            {
                button_Open.Enabled = false;
            }

            label_Hostname.Text = "Hostname: ";
            label_Name.Text = "Name: ";
        }

        private async void listBox_Devices_SelectedIndexChanged(object sender, EventArgs e)
        {
            string hostname = listBox_Devices.SelectedItem?.ToString();
            if (hostname is string)
            {
                groupBoxActions.Enabled = true;

                Device d = new Device(hostname);
                await d.Connect();

                label_Hostname.Text = "Hostname: " + hostname;
                label_Name.Text = "Name: " + (await d.GetProp(YeelightAPI.Models.PROPERTIES.name)).ToString();
                textBox_Name.Text = d.Name;

                d.Disconnect();
            }
        }

        private async void button_Name_Click(object sender, EventArgs e)
        {
            string hostname = listBox_Devices.SelectedItem?.ToString();
            if (hostname is string)
            {
                Device d = new Device(hostname);
                await d.Connect();

                if (textBox_Name.Text.Length >= 1)
                {
                    await d.SetName(textBox_Name.Text);
                }

                d.Disconnect();
                label_Name.Text = "Name: " + textBox_Name.Text;
            }
        }

        private void button_Open_Click(object sender, EventArgs e)
        {
            this.Hide();
            new MainForm().Show();
        }

        private void button_Delete_Click(object sender, EventArgs e)
        {
            string hostname = listBox_Devices.SelectedItem?.ToString();
            if (hostname is string)
            {
                int found = -1;
                for (int i = 0; i < yeelightControlDevices.Count; i++)
                {
                    YeeControlDevice y = yeelightControlDevices[i];
                    if (y.Hostname == hostname)
                    {
                        found = i;
                        break;
                    }
                }
                if(found > -1)
                {
                    yeelightControlDevices = YeeControlDeviceHelper.DeleteYeeControlAtIndex(found);
                    RefreshDeviceList();
                }
            }
        }
    }
}
