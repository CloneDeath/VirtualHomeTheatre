using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiftSharp
{
	public class RiftHeadsetDevice : HIDDevice
	{
		public EventHandler<RiftInputEventArgs> OnMoveHead;

		public override InputReport CreateInputReport()
		{
			return new RiftInputReport(this);
		}

		protected override void HandleDataReceived(InputReport oInRep)
		{
			RiftInputReport RiftIn = oInRep as RiftInputReport;
			if (OnMoveHead != null) {
				OnMoveHead(this, new RiftInputEventArgs(RiftIn));
			}
		}

		public static RiftHeadsetDevice FindRiftDevice()
		{
			return (RiftHeadsetDevice)HIDDevice.FindDevice(0x2833, 0x0001, typeof(RiftHeadsetDevice));
		}
	}
}
