using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiftSharp.Math
{
	// RotateDirection describes the rotation direction around an axis, interpreted as follows:
	//  CW  - Clockwise while looking "down" from positive axis towards the origin.
	//  CCW - Counter-clockwise while looking from the positive axis towards the origin,
	//        which is in the negative axis direction.
	//  CCW is the default for the RHS coordinate system. Oculus standard RHS coordinate
	//  system defines Y up, X right, and Z back (pointing out from the screen). In this
	//  system Rotate_CCW around Z will specifies counter-clockwise rotation in XY plane.
	public enum RotateDirection
	{
		CCW = 1,
		CW = -1
	};
}
