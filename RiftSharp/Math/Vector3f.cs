using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RiftSharp.Math
{
	public class Vector3f
	{
		public const float Tolerance = 0.00001f;

		public Vector3f(float _x, float _y, float _z)
		{
			this.X = _x;
			this.Y = _y;
			this.Z = _z;
		}

		public Vector3f(){
			X = 0;
			Y = 0;
			Z = 0;
		}
		public float X, Y, Z;

		public static bool operator ==(Vector3f a, Vector3f b) { return a.X == b.X && a.Y == b.Y && a.Z == b.Z; }
		public static bool operator !=(Vector3f a, Vector3f b) { return !(a == b); }

		public static Vector3f operator +(Vector3f a, Vector3f b) { return new Vector3f(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
		public static Vector3f operator -(Vector3f a, Vector3f b) { return new Vector3f(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
		public static Vector3f operator -(Vector3f a) { return new Vector3f(-a.X, -a.Y, -a.Z); }

		public static Vector3f operator *(Vector3f a, float b) { return new Vector3f(a.X * b, a.Y * b, a.Z * b); }
		public static Vector3f operator /(Vector3f a, float b) { return new Vector3f(a.X / b, a.Y / b, a.Z / b); }

		// Compare two vectors for equality with tolerance. Returns true if vectors match withing tolerance.
		public bool Compare(Vector3f b, float tolerance = Tolerance)
		{
			return (System.Math.Abs(b.X - X) < tolerance) &&
				   (System.Math.Abs(b.Y - Y) < tolerance) &&
				   (System.Math.Abs(b.Z - Z) < tolerance);
		}

		// Entrywise product of two vectors
		public Vector3f EntrywiseMultiply(Vector3f b) { return new Vector3f(X * b.X, Y * b.Y, Z * b.Z); }

		// Dot product
		// Used to calculate angle q between two vectors among other things,
		// as (A dot B) = |a||b|cos(q).
		public float Dot(Vector3f b) { return X * b.X + Y * b.Y + Z * b.Z; }

		// Compute cross product, which generates a normal vector.
		// Direction vector can be determined by right-hand rule: Pointing indeX finder in
		// direction a and middle finger in direction b, thumb will point in a.Cross(b).
		public Vector3f Cross(Vector3f b) { return new Vector3f(Y * b.Z - Z * b.Y, Z * b.X - X * b.Z, X * b.Y - Y * b.X); }

		// Returns the angle from this vector to b, in radians.
		public float Angle(Vector3f b)
		{
			float div = LengthSq() * b.LengthSq();
			Debug.Assert(div != 0);
			return (float)(System.Math.Acos((this.Dot(b)) / System.Math.Sqrt(div)));
		}

		// Return Length of the vector squared.
		public float LengthSq() { return (X * X + Y * Y + Z * Z); }
		// Return vector length.
		public float Length() { return (float)System.Math.Sqrt(LengthSq()); }

		// Returns distance between two points represented by vectors.
		public float Distance(Vector3f b) { return (this - b).Length(); }

		// Determine if this a unit vector.
		public bool IsNormalized() { return System.Math.Abs(LengthSq() - 1.0) < Tolerance; }

		// Normalize, convention vector length to 1.    
		public void Normalize()
		{
			float l = Length();
			Debug.Assert(l != 0);
			this.X /= l; this.Y /= l; this.Z /= l;
		}

		// Returns normalized (unit) version of the vector without modifying itself.
		public Vector3f Normalized()
		{
			float l = Length();
			Debug.Assert(l != 0);
			return new Vector3f(X / l, Y / l, Z / l);
		}

		// Linearly interpolates from this vector to another.
		// Factor should be between 0.0 and 1.0, with 0 giving full value to this.
		public Vector3f Lerp(Vector3f b, float f) { return this * (1 - f) + b * f; }

		// Projects this vector onto the argument; in other words,
		// A.Project(B) returns projection of vector A onto B.
		public Vector3f ProjectTo(Vector3f b)
		{
			float l2 = b.LengthSq();
			Debug.Assert(l2 != 0);
			return b * (Dot(b) / l2);
		}

		// Projects this vector onto a plane defined by a normal vector
		public Vector3f ProjectToPlane(Vector3f normal) { return this - this.ProjectTo(normal); }
	}
}
