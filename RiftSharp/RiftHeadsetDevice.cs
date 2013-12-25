using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using RiftSharp.Math;

namespace RiftSharp
{
	public class RiftHeadsetDevice : HIDDevice
	{
		public Action<SensorFusion> OnMoveHead;
		Timer KeepAlivetimer;

		bool SequenceValid;
		byte LastSampleCount;
		UInt16 LastTimestamp;
		UInt16 OldCommandId;

		float LastTemperature;
		Vector3f LastAcceleration;
		Vector3f LastRotationRate;
		Vector3f LastMagneticField;

		CoordinateFrame Coordinates;
		CoordinateFrame HWCoordinates;

		SensorInfo SensorInfo;

		enum CoordinateFrame
		{
			Sensor = 0,
			HMD = 1
		};

		public SensorFusion Sensor;

		public RiftHeadsetDevice()
		{
			Sensor = new SensorFusion(this);
			SequenceValid = false;
			LastSampleCount = 0;
			LastTimestamp = 0;

			OldCommandId = 0;
		}

		protected override void HandleDeviceRemoved()
		{
			base.HandleDeviceRemoved();
			KeepAlivetimer.Stop();
		}

		public override InputReport CreateInputReport()
		{
			return new RiftInputReport(this);
		}

		protected override void HandleDataReceived(InputReport oInRep)
		{
			RiftInputReport RiftIn = oInRep as RiftInputReport;
			OnTrackerMessage(RiftIn);

			if (OnMoveHead != null) {
				OnMoveHead(Sensor);
			}
		}

		// Sensor reports data in the following coordinate system:
		// Accelerometer: 10^-4 m/s^2; X forward, Y right, Z Down.
		// Gyro:          10^-4 rad/s; X positive roll right, Y positive pitch up; Z positive yaw right.


		// We need to convert it to the following RHS coordinate system:
		// X right, Y Up, Z Back (out of screen)
		//
		Vector3f AccelFromBodyFrameUpdate(RiftInputReport update, Byte sampleNumber, bool convertHMDToSensor = false)
		{
			TrackerSample sample = update.Samples[sampleNumber];
			float                ax = (float)sample.Accel.X;
			float                ay = (float)sample.Accel.Y;
			float                az = (float)sample.Accel.Z;

			Vector3f val = convertHMDToSensor ? new Vector3f(ax, az, -ay) : new Vector3f(ax, ay, az);
			return val * 0.0001f;
		}

		Vector3f MagFromBodyFrameUpdate(RiftInputReport update, bool convertHMDToSensor = false)
		{   
			// Note: Y and Z are swapped in comparison to the Accel.  
			// This accounts for DK1 sensor firmware axis swap, which should be undone in future releases.
			if (!convertHMDToSensor)
			{
				return new Vector3f( (float)update.MagX,
									(float)update.MagZ,
									(float)update.MagY) * 0.0001f;
			}

			return new Vector3f((float)update.MagX,
								(float)update.MagY,
							-(float)update.MagZ) * 0.0001f;
		}

		Vector3f EulerFromBodyFrameUpdate(RiftInputReport update, Byte sampleNumber, bool convertHMDToSensor = false)
		{
			TrackerSample sample = update.Samples[sampleNumber];
			float                gx = (float)sample.Gyro.X;
			float                gy = (float)sample.Gyro.Y;
			float                gz = (float)sample.Gyro.Z;

			Vector3f val = convertHMDToSensor ? new Vector3f(gx, gz, -gy) : new Vector3f(gx, gy, gz);
			return val * 0.0001f;
		}

		private void OnTrackerMessage(RiftInputReport report)
		{
			const float timeUnit = (1.0f / 1000.0f);

			if (SequenceValid) {
				uint timestampDelta;

				if (report.Timestamp < LastTimestamp)
					timestampDelta = (uint)((((int)report.Timestamp) + 0x10000) - (int)LastTimestamp);
				else
					timestampDelta = (uint)(report.Timestamp - LastTimestamp);

				// If we missed a small number of samples, replicate the last sample.
				if ((timestampDelta > LastSampleCount) && (timestampDelta <= 254)) {
					MessageBodyFrame sensors = new MessageBodyFrame(this);
					sensors.TimeDelta = (timestampDelta - LastSampleCount) * timeUnit;
					sensors.Acceleration = LastAcceleration;
					sensors.RotationRate = LastRotationRate;
					sensors.MagneticField = LastMagneticField;
					sensors.Temperature = LastTemperature;

					Sensor.OnMessage(sensors);
				}
			} else {
				LastAcceleration = new Vector3f();
				LastRotationRate = new Vector3f();
				LastMagneticField = new Vector3f();
				LastTemperature = 0;
				SequenceValid = true;
			}

			LastSampleCount = report.SampleCount;
			LastTimestamp = report.Timestamp;

			bool convertHMDToSensor = (Coordinates == CoordinateFrame.Sensor) && (HWCoordinates == CoordinateFrame.HMD);

			//if (HandlerRef.GetHandler())
			{
				MessageBodyFrame sensors = new MessageBodyFrame(this);
				Byte iterations = report.SampleCount;

				if (report.SampleCount > 3) {
					iterations = 3;
					sensors.TimeDelta = (report.SampleCount - 2) * timeUnit;
				} else {
					sensors.TimeDelta = timeUnit;
				}

				for (Byte i = 0; i < iterations; i++) {
					sensors.Acceleration = AccelFromBodyFrameUpdate(report, i, convertHMDToSensor);
					sensors.RotationRate = EulerFromBodyFrameUpdate(report, i, convertHMDToSensor);
					sensors.MagneticField = MagFromBodyFrameUpdate(report, convertHMDToSensor);
					sensors.Temperature = report.Temperature * 0.01f;
					Sensor.OnMessage(sensors);
					// TimeDelta for the last two sample is always fixed.
					sensors.TimeDelta = timeUnit;
				}

				LastAcceleration = sensors.Acceleration;
				LastRotationRate = sensors.RotationRate;
				LastMagneticField = sensors.MagneticField;
				LastTemperature = sensors.Temperature;
			}
			//else
			//{
			//    UByte i = (report.SampleCount > 3) ? 2 : (report.SampleCount - 1);
			//    LastAcceleration  = AccelFromBodyFrameUpdate(report, i, convertHMDToSensor);
			//    LastRotationRate  = EulerFromBodyFrameUpdate(report, i, convertHMDToSensor);
			//    LastMagneticField = MagFromBodyFrameUpdate(report, convertHMDToSensor);
			//    LastTemperature   = report.Temperature * 0.01f;
			//}
		}

		public static RiftHeadsetDevice FindRiftDevice()
		{
			return (RiftHeadsetDevice)HIDDevice.FindDevice(0x2833, 0x0001, typeof(RiftHeadsetDevice));
		}

		protected override void Initialize(string path)
		{
			base.Initialize(path);

			this.SensorInfo = new SensorInfo(oCaps, 0x2833, 0x0001, "");

			if (!HidD_SetNumInputBuffers(m_hHandle, 128)) {
				throw new Exception("Failed 'HidD_SetNumInputBuffers' while initializing device.");
			}

			//Sensor Range
			SensorRangeImpl sr = new SensorRangeImpl(new SensorRange(), 0);
			if (GetFeature(ref sr.Buffer)) {
				sr.Unpack();
				SensorRange CurrentRange;
				sr.GetSensorRange(out CurrentRange);
				// Increase the magnetometer range, since the default value is not enough in practice
				CurrentRange.MaxMagneticField = 2.5f;

				SensorRangeImpl sr2 = new SensorRangeImpl(CurrentRange);
				if (SetFeature(ref sr.Buffer)) {
					sr.GetSensorRange(out CurrentRange);
				}
			}

			//Set Report Rate
			SensorConfig scfg = new SensorConfig();
			if (GetFeature(ref scfg.Buffer)) {
				scfg.Unpack();
			}
			int RateHz = 500;
			if (RateHz > 1000) {
				RateHz = 1000;
			} else if (RateHz == 0) {
				RateHz = 500;
			}
			scfg.PacketInterval = (UInt16)((1000 / RateHz) - 1);
			scfg.KeepAliveIntervalMs = 10000;
			scfg.Pack();
			SetFeature(ref scfg.Buffer);

			KeepAlivetimer = new Timer(6 * 1000);
			KeepAlivetimer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
			KeepAlivetimer.AutoReset = true;
			KeepAlivetimer.Start();
		}

		void timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			SensorKeepAliveImpl skeepAlive = new SensorKeepAliveImpl(10 * 1000);
			skeepAlive.Pack();
			SetFeature(ref skeepAlive.Buffer);
		}

		internal SensorInfo GetDeviceInfo()
		{
			return this.SensorInfo;
		}
	}
}
