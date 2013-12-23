using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiftSharp
{
	// Sensor configuration command, ReportId == 2.
	class SensorConfig
	{
		public const int PacketSize = 7;
		public byte[] Buffer = new byte[PacketSize];

		// Flag values for Flags.
		[Flags]
		public enum Flag{
			RawMode            = 0x01,
			CallibrationTest   = 0x02, // Internal test mode
			UseCallibration    = 0x04,
			AutoCallibration   = 0x08,
			MotionKeepAlive    = 0x10,
			CommandKeepAlive   = 0x20,
			SensorCoordinates  = 0x40
		};

		public UInt16  CommandId;
		public Flag   Flags;
		public UInt16  PacketInterval;
		public UInt16  KeepAliveIntervalMs;

		public SensorConfig()
		{
			this.CommandId = 0;
			this.Flags = 0;
			this.PacketInterval = 0;
			this.KeepAliveIntervalMs = 0;
			Array.Clear(Buffer, 0, 0);
			Buffer[0] = 2;
		}

		public void SetSensorCoordinates(bool sensorCoordinates)
		{ 
			Flags = (Flags & ~Flag.SensorCoordinates) | (sensorCoordinates ? Flag.SensorCoordinates : 0); 
		}
		public bool IsUsingSensorCoordinates()
		{ 
			return (Flags & Flag.SensorCoordinates) != 0; 
		}

		public void Pack()
		{
			Buffer[0] = 2;
			Buffer[1] = (Byte)(CommandId & 0xFF);
			Buffer[2] = (Byte)(CommandId >> 8);
			Buffer[3] = (byte)Flags;
			Buffer[4] = (Byte)(PacketInterval);
			Buffer[5] = (Byte)(KeepAliveIntervalMs & 0xFF);
			Buffer[6] = (Byte)(KeepAliveIntervalMs >> 8);
		}

		public void Unpack()
		{
			CommandId          = (UInt16)(Buffer[1] | (((UInt16)Buffer[2]) << 8));
			Flags              = (Flag)Buffer[3];
			PacketInterval     = Buffer[4];
			KeepAliveIntervalMs= (UInt16)(Buffer[5] | (((UInt16)Buffer[6]) << 8));
		}
    
	};
}
