using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RiftSharp.Math;

namespace RiftSharp
{
	// Sensor BodyFrame notification.
	// Sensor uses Right-Handed coordinate system to return results, with the following
	// axis definitions:
	//  - Y Up positive
	//  - X Right Positive
	//  - Z Back Positive
	// Rotations a counter-clockwise (CCW) while looking in the negative direction
	// of the axis. This means they are interpreted as follows:
	//  - Roll is rotation around Z, counter-clockwise (tilting left) in XY plane.
	//  - Yaw is rotation around Y, positive for turning left.
	//  - Pitch is rotation around X, positive for pitching up.

	public class MessageBodyFrame
	{
		public MessageBodyFrame(HIDDevice dev)
		{
			Temperature = 0.0f;
			TimeDelta = 0.0f;
			pDevice = dev;
		}

		public Vector3f Acceleration;   // Acceleration in m/s^2.
		public Vector3f RotationRate;   // Angular velocity in rad/s^2.
		public Vector3f MagneticField;  // Magnetic field strength in Gauss.
		public float    Temperature;    // Temperature reading on sensor surface, in degrees Celsius.
		public float    TimeDelta;      // Time passed since last Body Frame, in seconds.
		HIDDevice pDevice;
	};
}
