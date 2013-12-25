using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiftSharp
{
	// SensorKeepAlive - feature report that needs to be sent at regular intervals for sensor
	// to receive commands.
	class SensorKeepAliveImpl
	{
		public const int PacketSize = 5;
		public Byte[] Buffer = new Byte[PacketSize];

		public UInt16 CommandId;
		public UInt16 KeepAliveIntervalMs;

		public SensorKeepAliveImpl(UInt16 interval = 0, UInt16 commandId = 0)
		{
			CommandId = commandId;
			KeepAliveIntervalMs = interval;
			Pack();
		}

		public void Pack()
		{
			Buffer[0] = 8;
			Buffer[1] = (Byte)(CommandId & 0xFF);
			Buffer[2] = (Byte)(CommandId >> 8);
			Buffer[3] = (Byte)(KeepAliveIntervalMs & 0xFF);
			Buffer[4] = (Byte)(KeepAliveIntervalMs >> 8);
		}

		public void Unpack()
		{
			CommandId = (UInt16)(Buffer[1] | ((UInt16)(Buffer[2]) << 8));
			KeepAliveIntervalMs = (UInt16)(Buffer[3] | ((UInt16)(Buffer[4]) << 8));
		}
	};
}
