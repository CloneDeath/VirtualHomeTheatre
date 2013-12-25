using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiftSharp
{
	public class SensorInfo
	{
		public RiftSharp.Win32Usb.HidCaps Capabilities;
		public UInt16 VendorId;
		public UInt16 ProductId;
		public string SerialNumber;

		public SensorInfo()
		{
			VendorId = 0;
			ProductId = 0;
			SerialNumber = "";
		}

		public SensorInfo(RiftSharp.Win32Usb.HidCaps caps, UInt16 vendor, UInt16 product, string serial)
		{
			this.Capabilities = caps;
			this.VendorId = vendor;
			this.ProductId = product;
			this.SerialNumber = serial;
		}
	}
}
