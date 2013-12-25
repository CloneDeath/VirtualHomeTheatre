using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RiftSharp.Math
{
	//-------------------------------------------------------------------------------------//
	// **************************************** Quatf **************************************//
	//
	// Quatf represents a quaternion class used for rotations.
	// 
	// Quaternion multiplications are done in right-to-left order, to match the
	// behavior of matrices.

	public class Quatf
	{
		// w + Xi + Yj + Zk
		public float x, y, z, w;    

		public Quatf() {
			x = 0;
			y = 0;
			z = 0;
			w = 1;
		}
		public Quatf(float x_, float y_, float z_, float w_) {
			x = x_;
			y = y_;
			z = z_;
			w = w_;
		}


		// Constructs quaternion for rotation around the axis by an angle.
		public Quatf(Vector3f axis, float angle)
		{
			Vector3f unitAxis = axis.Normalized();
			float sinHalfAngle = (float)System.Math.Sin(angle * (float)(0.5));

			w = (float)System.Math.Cos(angle * (float)(0.5));
			x = unitAxis.X * sinHalfAngle;
			y = unitAxis.Y * sinHalfAngle;
			z = unitAxis.Z * sinHalfAngle;
		}

		// Constructs quaternion for rotation around one of the coordinate axis by an angle.
		public void AxisAngle(Axis A, float angle, RotateDirection d, HandedSystem s)
		{
			float sinHalfAngle = (float)((int)s * (int)d * System.Math.Sin(angle * (float)(0.5)));
			float[] v = new float[3];
			v[0] = v[1] = v[2] = (float)(0);
			v[(int)A] = sinHalfAngle;

			w = (float)System.Math.Cos(angle * (float)(0.5));
			x = v[0];
			y = v[1];
			z = v[2];
		}


	   // Compute axis and angle from quaternion
	   public void GetAxisAngle(out Vector3f axis, out float angle)
		{
			if ( x*x + y*y + z*z > Vector3f.Tolerance * Vector3f.Tolerance ) {
				axis  = new Vector3f(x, y, z).Normalized();
				angle = (float)(2 * System.Math.Acos(w));
			}
			else 
			{
				axis = new Vector3f(1, 0, 0);
				angle= 0;
			}
		}

		public static bool operator==(Quatf a, Quatf b) { return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w; }

		public static bool operator!=(Quatf a, Quatf b) { return a.x != b.x || a.y != b.y || a.z != b.z || a.w != b.w; }

		public static Quatf operator+(Quatf a, Quatf b) { return new Quatf(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w); }
		public static Quatf operator-(Quatf a, Quatf b) { return new Quatf(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w); }

		public static Quatf operator*(Quatf a, float s) { return new Quatf(a.x * s, a.y * s, a.z * s, a.w * s); }
		public static Quatf operator/(Quatf a, float s) { return new Quatf(a.x / s, a.y / s, a.z / s, a.w / s); }


		// Get Imaginary part vector
		public Vector3f Imag() { return new Vector3f(x, y, z); }

		// Get quaternion length.
		public float       Length() { return (float)System.Math.Sqrt((x * x) + (y * y) + (z * z) + (w * w)); }
		// Get quaternion length squared.
		public float       LengthSq() { return ((x * x) + (y * y) + (z * z) + (w * w)); }

		// Simple Eulidean distance in R^4 (not SLERP distance, but at least respects Haar measure)
		public float       Distance(Quatf q)
		{ 
			float d1 = (this - q).Length();
			float d2 = (this + q).Length(); // Antipodal point check
			return (d1 < d2) ? d1 : d2;
		}

		public float DistanceSq(Quatf q)
		{
			float d1 = (this - q).LengthSq();
			float d2 = (this + q).LengthSq(); // Antipodal point check
			return (d1 < d2) ? d1 : d2;
		}

		// Normalize
		public bool    IsNormalized() { return System.Math.Abs(LengthSq() - 1f) < Vector3f.Tolerance; }

		public void    Normalize()
		{
 			float l = Length();
			Debug.Assert(l != 0f);
			x /= l;
			y /= l;
			z /= l;
			w /= l;
		}

		public Quatf Normalized()              
		{ 
			float l = Length();
			Debug.Assert(l != 0f);
			return this / l; 
		}

		// Returns conjugate of the quaternion. Produces inverse rotation if quaternion is normalized.
		public Quatf    Conj() { return new Quatf(-x, -y, -z, w); }

		// Quaternion multiplication. Combines quaternion rotations, performing the one on the 
		// right hand side first.
		public static Quatf  operator* (Quatf a, Quatf b) { return new Quatf(a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
																			 a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
																			 a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
																			 a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z); }

		// 
		// this^p normalized; same as rotating by this p times.
		public Quatf PowNormalized(float p)
		{
			Vector3f v;
			float a;
			GetAxisAngle(out v, out a);
			return new Quatf(v, a * p);
		}
    
		// Rotate transforms vector in a manner that matches Matrix rotations (counter-clockwise,
		// assuming negative direction of the axis). Standard formula: q(t) * V * q(t)^-1. 
		public Vector3f Rotate(Vector3f v)
		{
			return ((this * new Quatf(v.X, v.Y, v.Z, 0)) * Inverted()).Imag();
		}

    
		// Inversed quaternion rotates in the opposite direction.
		public Quatf Inverted()
		{
			return new Quatf(-x, -y, -z, w);
		}

		// Sets this quaternion to the one rotates in the opposite direction.
		public void Invert()
		{
			x *= -1;
			y *= -1;
			z *= -1;
		}
    
		// Converting quaternion to matrix.
		public static explicit operator Matrix4f(Quatf a)
		{
			float ww = a.w*a.w;
			float xx = a.x*a.x;
			float yy = a.y*a.y;
			float zz = a.z*a.z;

			return new Matrix4f((ww + xx - yy - zz),  (2f * (a.x*a.y - a.w*a.z)), (2f * (a.x*a.z + a.w*a.y)),
								(2f * (a.x*a.y + a.w*a.z)), (ww - xx + yy - zz),  (2f * (a.y*a.z - a.w*a.x)),
								(2f * (a.x*a.z - a.w*a.y)), (2f * (a.y*a.z + a.w*a.x)), (ww - xx - yy + zz) );
		}


		// Converting matrix to quaternion
		public static explicit operator Quatf(Matrix4f m)
		{
			float trace = m.M[0, 0] + m.M[1, 1] + m.M[2, 2];
			Quatf q = new Quatf();

			// In almost all cases, the first part is executed.
			// However, if the trace is not positive, the other
			// cases arise.
			if (trace > 0f) 
			{
				float s = (float)System.Math.Sqrt(trace + 1f) * 2f; // s=4*qw
				q.w = 0.25f * s;
				q.x = (m.M[2, 1] - m.M[1, 2]) / s;
				q.y = (m.M[0, 2] - m.M[2, 0]) / s;
				q.z = (m.M[1, 0] - m.M[0, 1]) / s; 
			} 
			else if ((m.M[0, 0] > m.M[1, 1])&&(m.M[0, 0] > m.M[2, 2])) 
			{
				float s = (float)System.Math.Sqrt(1f + m.M[0, 0] - m.M[1, 1] - m.M[2, 2]) * 2f;
				q.w = (m.M[2, 1] - m.M[1, 2]) / s;
				q.x = 0.25f * s;
				q.y = (m.M[0, 1] + m.M[1, 0]) / s;
				q.z = (m.M[2, 0] + m.M[0, 2]) / s;
			} 
			else if (m.M[1, 1] > m.M[2, 2]) 
			{
				float s = (float)System.Math.Sqrt(1f + m.M[1, 1] - m.M[0, 0] - m.M[2, 2]) * 2f; // S=4*qy
				q.w = (m.M[0, 2] - m.M[2, 0]) / s;
				q.x = (m.M[0, 1] + m.M[1, 0]) / s;
				q.y = 0.25f * s;
				q.z = (m.M[1, 2] + m.M[2, 1]) / s;
			} 
			else 
			{
				float s = (float)System.Math.Sqrt(1f + m.M[2, 2] - m.M[0, 0] - m.M[1, 1]) * 2f; // S=4*qz
				q.w = (m.M[1, 0] - m.M[0, 1]) / s;
				q.x = (m.M[0, 2] + m.M[2, 0]) / s;
				q.y = (m.M[1, 2] + m.M[2, 1]) / s;
				q.z = 0.25f * s;
			}
			return q;
		}


    
		//// GetEulerAngles extracts Euler angles from the quaternion, in the specified order of
		//// axis rotations and the specified coordinate system. Right-handed coordinate system
		//// is the default, with CCW rotations while looking in the negative axis direction.
		//// Here a,b,c, are the Yaw/Pitch/Roll angles to be returned.
		//// rotation a around axis A1
		//// is followed by rotation b around axis A2
		//// is followed by rotation c around axis A3
		//// rotations are CCW or CW (D) in LH or RH coordinate system (S)
		//template <Axis A1, Axis A2, Axis A3, RotateDirection D, HandedSystem S>
		//public void GetEulerAngles(float *a, float *b, float *c)
		//{
		//    OVR_COMPILER_ASSERT((A1 != A2) && (A2 != A3) && (A1 != A3));

		//    float Q[3] = { x, y, z };  //Quaternion components x,y,z

		//    float ww  = w*w;
		//    float Q11 = Q[A1]*Q[A1];
		//    float Q22 = Q[A2]*Q[A2];
		//    float Q33 = Q[A3]*Q[A3];

		//    float psign = (float)(-1);
		//    // Determine whether even permutation
		//    if (((A1 + 1) % 3 == A2) && ((A2 + 1) % 3 == A3))
		//        psign = (float)(1);
        
		//    float s2 = psign * (float)(2) * (psign*w*Q[A2] + Q[A1]*Q[A3]);

		//    if (s2 < (float)(-1) + Math<float>::SingularityRadius)
		//    { // South pole singularity
		//        *a = (float)(0);
		//        *b = -S*D*Math<float>::PiOver2;
		//        *c = S*D*atan2((float)(2)*(psign*Q[A1]*Q[A2] + w*Q[A3]),
		//                       ww + Q22 - Q11 - Q33 );
		//    }
		//    else if (s2 > (float)(1) - Math<float>::SingularityRadius)
		//    {  // North pole singularity
		//        *a = (float)(0);
		//        *b = S*D*Math<float>::PiOver2;
		//        *c = S*D*atan2((float)(2)*(psign*Q[A1]*Q[A2] + w*Q[A3]),
		//                       ww + Q22 - Q11 - Q33);
		//    }
		//    else
		//    {
		//        *a = -S*D*atan2((float)(-2)*(w*Q[A1] - psign*Q[A2]*Q[A3]),
		//                        ww + Q33 - Q11 - Q22);
		//        *b = S*D*asin(s2);
		//        *c = S*D*atan2((float)(2)*(w*Q[A3] - psign*Q[A1]*Q[A2]),
		//                       ww + Q11 - Q22 - Q33);
		//    }      
		//    return;
		//}

		//template <Axis A1, Axis A2, Axis A3, RotateDirection D>
		//public void GetEulerAngles(float *a, float *b, float *c)
		//{ GetEulerAngles<A1, A2, A3, D, Handed_R>(a, b, c); }

		//template <Axis A1, Axis A2, Axis A3>
		//public void GetEulerAngles(float *a, float *b, float *c)
		//{ GetEulerAngles<A1, A2, A3, Rotate_CCW, Handed_R>(a, b, c); }


		//// GetEulerAnglesABA extracts Euler angles from the quaternion, in the specified order of
		//// axis rotations and the specified coordinate system. Right-handed coordinate system
		//// is the default, with CCW rotations while looking in the negative axis direction.
		//// Here a,b,c, are the Yaw/Pitch/Roll angles to be returned.
		//// rotation a around axis A1
		//// is followed by rotation b around axis A2
		//// is followed by rotation c around axis A1
		//// Rotations are CCW or CW (D) in LH or RH coordinate system (S)
		//template <Axis A1, Axis A2, RotateDirection D, HandedSystem S>
		//public void GetEulerAnglesABA(float *a, float *b, float *c)
		//{
		//    OVR_COMPILER_ASSERT(A1 != A2);

		//    float Q[3] = {x, y, z}; // Quaternion components

		//    // Determine the missing axis that was not supplied
		//    int m = 3 - A1 - A2;

		//    float ww = w*w;
		//    float Q11 = Q[A1]*Q[A1];
		//    float Q22 = Q[A2]*Q[A2];
		//    float Qmm = Q[m]*Q[m];

		//    float psign = (float)(-1);
		//    if ((A1 + 1) % 3 == A2) // Determine whether even permutation
		//    {
		//        psign = (float)(1);
		//    }

		//    float c2 = ww + Q11 - Q22 - Qmm;
		//    if (c2 < (float)(-1) + Math<float>::SingularityRadius)
		//    { // South pole singularity
		//        *a = (float)(0);
		//        *b = S*D*Math<float>::Pi;
		//        *c = S*D*atan2( (float)(2)*(w*Q[A1] - psign*Q[A2]*Q[m]),
		//                        ww + Q22 - Q11 - Qmm);
		//    }
		//    else if (c2 > (float)(1) - Math<float>::SingularityRadius)
		//    {  // North pole singularity
		//        *a = (float)(0);
		//        *b = (float)(0);
		//        *c = S*D*atan2( (float)(2)*(w*Q[A1] - psign*Q[A2]*Q[m]),
		//                       ww + Q22 - Q11 - Qmm);
		//    }
		//    else
		//    {
		//        *a = S*D*atan2( psign*w*Q[m] + Q[A1]*Q[A2],
		//                       w*Q[A2] -psign*Q[A1]*Q[m]);
		//        *b = S*D*acos(c2);
		//        *c = S*D*atan2( -psign*w*Q[m] + Q[A1]*Q[A2],
		//                       w*Q[A2] + psign*Q[A1]*Q[m]);
		//    }
		//    return;
		//}
	}
}
