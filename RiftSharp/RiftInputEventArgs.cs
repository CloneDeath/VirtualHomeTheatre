using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiftSharp
{
	public class RiftInputEventArgs : EventArgs
	{
		public RiftInputReport Report;

		public RiftInputEventArgs(RiftInputReport report)
		{
			this.Report = report;
		}
	}
}
