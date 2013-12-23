using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiftSharp
{
	public class Vec3i
	{
		public int X, Y, Z;
	}
	public class TrackerSample
	{
		public Vec3i Accel;
		public Vec3i Gyro;
	}

	public class RiftInputReport : InputReport
	{
		enum TrackerMessageType : byte
		{
			None = 0,
			Sensors = 1,
			//Unknown = 0x100,
			//SizeError = 0x101,
		}

		public byte SampleCount;
		public ushort Timestamp;
		public ushort LastCommandID;
		public short Temperature;

		public TrackerSample[] Samples = new TrackerSample[3];

		public short MagX, MagY, MagZ;

		public RiftInputReport(HIDDevice device) : base(device) { }

		public override void ProcessData()
		{
			if (BufferLength < 4) {
				throw new Exception("Bad Size");
			}

			switch (Buffer[0]) {
				case (byte)TrackerMessageType.Sensors:
					if (BufferLength < 62) {
						throw new Exception("Size Error");
					}

					SampleCount = Buffer[1];
					Timestamp		= BitConverter.ToUInt16(Buffer, 2);
					LastCommandID	= BitConverter.ToUInt16(Buffer, 4);
					Temperature		= BitConverter.ToInt16(Buffer, 6);

					int iterationCount = (SampleCount > 2) ? 3 : SampleCount;

					for (int i = 0; i < iterationCount; i++) {
						Samples[i] = new TrackerSample();
						Samples[i].Accel = UnpackSensor(Buffer, 8 + (16 * i));
						Samples[i].Gyro = UnpackSensor(Buffer, 16 + (16 * i));
					}

					MagX = BitConverter.ToInt16(Buffer, 56);
					MagY = BitConverter.ToInt16(Buffer, 58);
					MagZ = BitConverter.ToInt16(Buffer, 60);

					break;
				default:
					break;
			}
		}

		private Vec3i UnpackSensor(byte[] Buffer, int offset)
		{
			Vec3i ret = new Vec3i();
			uint[] Data = new uint[8];
			Buffer.CopyTo(Data, offset);

			ret.X = SignExtension((Data[0] << 13) | (Data[1] << 5) | ((Data[2] & 0xF8) >> 3), 21);
			ret.Y = SignExtension(((Data[2] & 0x07) << 18) | (Data[3] << 10) | (Data[4] << 2) | ((Data[5] & 0xC0) >> 6), 21);
			ret.Z = SignExtension(((Data[5] & 0x3F) << 15) | (Data[6] << 7) | (Data[7] >> 1), 21);

			return ret;
		}

		private int SignExtension(uint value, int repeatbit)
		{
			uint sign = (uint)(0x01 << (repeatbit - 1));
			int ret = (int)((sign - 1) & value);
			if ((sign & value) != 0) {
				ret += (int)~(sign-1);
			}
			return ret;
		}
	}
}
