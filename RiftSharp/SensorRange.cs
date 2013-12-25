using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiftSharp
{
	//-------------------------------------------------------------------------------------
	// ***** SensorRange & SensorInfo

	// SensorRange specifies maximum value ranges that SensorDevice hardware is configured
	// to detect. Although this range doesn't affect the scale of MessageBodyFrame values,
	// physical motions whose positive or negative magnitude is outside the specified range
	// may get clamped or misreported. Setting lower values may result in higher precision
	// tracking.
	class SensorRange
	{
		public SensorRange(float maxAcceleration = 0.0f, float maxRotationRate = 0.0f, float maxMagneticField = 0.0f)
		{ 
			MaxAcceleration = maxAcceleration;
			MaxRotationRate = maxRotationRate;
			MaxMagneticField = maxMagneticField;
		}

		// Maximum detected acceleration in m/s^2. Up to 8*G equivalent support guaranteed,
		// where G is ~9.81 m/s^2.
		// Oculus DK1 HW has thresholds near: 2, 4 (default), 8, 16 G.
		public float   MaxAcceleration;  
		// Maximum detected angular velocity in rad/s. Up to 8*Pi support guaranteed.
		// Oculus DK1 HW thresholds near: 1, 2, 4, 8 Pi (default).
		public float MaxRotationRate;
		// Maximum detectable Magnetic field strength in Gauss. Up to 2.5 Gauss support guaranteed.
		// Oculus DK1 HW thresholds near: 0.88, 1.3, 1.9, 2.5 gauss.
		public float MaxMagneticField;
	};

	// SensorScaleImpl provides buffer packing logic for the Sensor Range
	// record that can be applied to DK1 sensor through Get/SetFeature. We expose this
	// through SensorRange class, which has different units.
	class SensorRangeImpl
	{
		// Sensor HW only accepts specific maximum range values, used to maximize
		// the 16-bit sensor outputs. Use these ramps to specify and report appropriate values.
		static UInt16[] AccelRangeRamp = { 2, 4, 8, 16 };
		static UInt16[] GyroRangeRamp = { 250, 500, 1000, 2000 };
		static UInt16[] MagRangeRamp   = { 880, 1300, 1900, 2500 };

		static UInt16 SelectSensorRampValue(UInt16[] ramp, float val, float factor, string label)
		{    
			UInt16 threshold = (UInt16)(val * factor);

			for (int i = 0; i < ramp.Length; i++)
			{
				if (ramp[i] >= threshold)
					return ramp[i];
			}
			Console.WriteLine("SensorDevice::SetRange - {0} clamped to {1}", label, (float)(ramp[ramp.Length-1]) / factor);
			return ramp[ramp.Length - 1];
		}

		public const int PacketSize = 8;
		public byte[] Buffer = new byte[PacketSize];
    
		public UInt16  CommandId;
		public UInt16  AccelScale;
		public UInt16  GyroScale;
		public UInt16  MagScale;

		public SensorRangeImpl(SensorRange r, UInt16 commandId = 0)
		{
			SetSensorRange(r, commandId);
		}

		public void SetSensorRange(SensorRange r, UInt16 commandId = 0)
		{
			CommandId  = commandId;
			AccelScale = SelectSensorRampValue(AccelRangeRamp, r.MaxAcceleration, (1.0f / 9.81f), "MaxAcceleration");
			GyroScale = SelectSensorRampValue(GyroRangeRamp, r.MaxRotationRate, (float)(180.0 / System.Math.PI), "MaxRotationRate");
			MagScale   = SelectSensorRampValue(MagRangeRamp, r.MaxMagneticField, 1000.0f, "MaxMagneticField");
			Pack();
		}

		public void GetSensorRange(out SensorRange r)
		{
			r = new SensorRange();
			r.MaxAcceleration = AccelScale * 9.81f;
			r.MaxRotationRate = (float)(GyroScale * System.Math.PI / 180.0);
			r.MaxMagneticField= MagScale * 0.001f;
		}

		public static SensorRange GetMaxSensorRange()
		{
			return new SensorRange(AccelRangeRamp[AccelRangeRamp.Length - 1] * 9.81f,
								   GyroRangeRamp[GyroRangeRamp.Length - 1] * (float)(System.Math.PI / 180.0),
								   MagRangeRamp[MagRangeRamp.Length - 1]     * 0.001f);
		}

		public void Pack()
		{
			Buffer[0] = 4;
			Buffer[1] = (Byte)(CommandId & 0xFF);
			Buffer[2] = (Byte)(CommandId >> 8);
			Buffer[3] = (Byte)(AccelScale);
			Buffer[4] = (Byte)(GyroScale & 0xFF);
			Buffer[5] = (Byte)(GyroScale >> 8);
			Buffer[6] = (Byte)(MagScale & 0xFF);
			Buffer[7] = (Byte)(MagScale >> 8);
		}

		public void Unpack()
		{
			CommandId = (UInt16)(Buffer[1] | ((UInt16)(Buffer[2]) << 8));
			AccelScale = (UInt16)(Buffer[3]);
			GyroScale = (UInt16)(Buffer[4] | ((UInt16)(Buffer[5]) << 8));
			MagScale = (UInt16)(Buffer[6] | ((UInt16)(Buffer[7]) << 8));
		}
	};
}
