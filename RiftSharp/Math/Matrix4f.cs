using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiftSharp.Math
{
	//-------------------------------------------------------------------------------------
	// ***** Matrix4f
	//
	// Matrix4f is a 4x4 matrix used for 3d transformations and projections.
	// Translation stored in the last column.
	// The matrix is stored in row-major order in memory, meaning that values
	// of the first row are stored before the next one.
	//
	// The arrangement of the matrix is chosen to be in Right-Handed 
	// coordinate system and counterclockwise rotations when looking down
	// the axis
	//
	// Transformation Order:
	//   - Transformations are applied from right to left, so the expression
	//     M1 * M2 * M3 * V means that the vector V is transformed by M3 first,
	//     followed by M2 and M1. 
	//
	// Coordinate system: Right Handed
	//
	// Rotations: Counterclockwise when looking down the axis. All angles are in radians.
	//    
	//  | sx   01   02   tx |    // First column  (sx, 10, 20): Axis X basis vector.
	//  | 10   sy   12   ty |    // Second column (01, sy, 21): Axis Y basis vector.
	//  | 20   21   sz   tz |    // Third columnt (02, 12, sz): Axis Z basis vector.
	//  | 30   31   32   33 |
	//
	//  The basis vectors are first three columns.

	public class Matrix4f
	{
		static Matrix4f IdentityValue;

		public float[,] M = new float[4, 4];

		// By default, we construct identity matrix.
		public Matrix4f()
		{
			SetIdentity();        
		}

		public Matrix4f(float m11, float m12, float m13, float m14,
				 float m21, float m22, float m23, float m24,
				 float m31, float m32, float m33, float m34,
				 float m41, float m42, float m43, float m44)
		{
			M[0, 0] = m11; M[0, 1] = m12; M[0, 2] = m13; M[0, 3] = m14;
			M[1, 0] = m21; M[1, 1] = m22; M[1, 2] = m23; M[1, 3] = m24;
			M[2, 0] = m31; M[2, 1] = m32; M[2, 2] = m33; M[2, 3] = m34;
			M[3, 0] = m41; M[3, 1] = m42; M[3, 2] = m43; M[3, 3] = m44;
		}

		public Matrix4f(float m11, float m12, float m13,
				 float m21, float m22, float m23,
				 float m31, float m32, float m33)
		{
			M[0, 0] = m11; M[0, 1] = m12; M[0, 2] = m13; M[0, 3] = 0;
			M[1, 0] = m21; M[1, 1] = m22; M[1, 2] = m23; M[1, 3] = 0;
			M[2, 0] = m31; M[2, 1] = m32; M[2, 2] = m33; M[2, 3] = 0;
			M[3, 0] = 0;   M[3, 1] = 0;   M[3, 2] = 0;   M[3, 3] = 1;
		}

		public override string ToString()
		{
			string ret = "";
			for (int r=0; r<4; r++){
				for (int c=0; c<4; c++){
					ret += M[r, c] + " ";
				}
			}
			return ret;
		}

		//public static Matrix4f FromString(const char* src)
		//{
		//    Matrix4f result;
		//    for (int r=0; r<4; r++)
		//        for (int c=0; c<4; c++)
		//        {
		//            result.M[r, c] = (float)atof(src);
		//            while (src && *src != ' ')
		//                src++;
		//            while (src && *src == ' ')
		//                src++;
		//        }
		//    return result;
		//}

		public static Matrix4f Identity()  { return IdentityValue; }

		public void SetIdentity()
		{
			M[0, 0] = M[1, 1] = M[2, 2] = M[3, 3] = 1;
			M[0, 1] = M[1, 0] = M[2, 3] = M[3, 1] = 0;
			M[0, 2] = M[1, 2] = M[2, 0] = M[3, 2] = 0;
			M[0, 3] = M[1, 3] = M[2, 1] = M[3, 0] = 0;
		}

		public static Matrix4f operator+(Matrix4f a, Matrix4f b)
		{
			Matrix4f result = new Matrix4f();
			for (int i = 0; i < 4; i++){
				for (int j = 0; j < 4; j++){
					result[i, j] = a[i, j] + b[i, j];
				}
			}
			return result;
		}

		public float this[int x, int y]{
			get {
				return M[x, y];
			}
			set {
				M[x, y] = value;
			}
		}

		public static Matrix4f operator-(Matrix4f a, Matrix4f b)
		{
			Matrix4f result = new Matrix4f();
			for (int i = 0; i < 4; i++){
				for (int j = 0; j < 4; j++){
					result[i, j] = a[i, j] - b[i, j];
				}
			}
			return result;
		}

		// Multiplies two matrices into destination with minimum copying.
		public static Matrix4f operator*(Matrix4f a, Matrix4f b)
		{
			Matrix4f result = new Matrix4f();
			int i = 0;
			do {
				result[i, 0] = a.M[i, 0] * b.M[0, 0] + a.M[i, 1] * b.M[1, 0] + a.M[i, 2] * b.M[2, 0] + a.M[i, 3] * b.M[3, 0];
				result[i, 1] = a.M[i, 0] * b.M[0, 1] + a.M[i, 1] * b.M[1, 1] + a.M[i, 2] * b.M[2, 1] + a.M[i, 3] * b.M[3, 1];
				result[i, 2] = a.M[i, 0] * b.M[0, 2] + a.M[i, 1] * b.M[1, 2] + a.M[i, 2] * b.M[2, 2] + a.M[i, 3] * b.M[3, 2];
				result[i, 3] = a.M[i, 0] * b.M[0, 3] + a.M[i, 1] * b.M[1, 3] + a.M[i, 2] * b.M[2, 3] + a.M[i, 3] * b.M[3, 3];
			} while((++i) < 4);

			return result;
		}

		public static Matrix4f operator* (Matrix4f a, float s)
		{
			Matrix4f result = new Matrix4f();
			for (int i = 0; i < 4; i++){
				for (int j = 0; j < 4; j++){
					result[i, j] = a[i, j] * s;
				}
			}
			return result;
		}


		public static Matrix4f operator/ (Matrix4f a, float s)
		{
			Matrix4f result = new Matrix4f();
			for (int i = 0; i < 4; i++){
				for (int j = 0; j < 4; j++){
					result[i, j] = a[i, j] / s;
				}
			}
			return result;
		}

		public Vector3f Transform(Vector3f v)
		{
			return new Vector3f(M[0, 0] * v.X + M[0, 1] * v.Y + M[0, 2] * v.Z + M[0, 3],
							    M[1, 0] * v.X + M[1, 1] * v.Y + M[1, 2] * v.Z + M[1, 3],
							    M[2, 0] * v.X + M[2, 1] * v.Y + M[2, 2] * v.Z + M[2, 3]);
		}

		public Matrix4f Transposed()
		{
			return new Matrix4f(M[0, 0], M[1, 0], M[2, 0], M[3, 0],
								M[0, 1], M[1, 1], M[2, 1], M[3, 1],
								M[0, 2], M[1, 2], M[2, 2], M[3, 2],
								M[0, 3], M[1, 3], M[2, 3], M[3, 3]);
		}

		public void     Transpose()
		{
			Matrix4f Transposed = this.Transposed();
			for (int x = 0; x < 4; x++){
				for (int y = 0; y < 4; y++){
					this[x, y] = Transposed[x, y]; //copy the data of the transposed matrix
				}
			}
		}


		//public float SubDet (const UPInt* rows, const UPInt* cols) const
		//{
		//    return M[rows[0], cols[0]] * (M[rows[1], cols[1]] * M[rows[2], cols[2]] - M[rows[1], cols[2]] * M[rows[2], cols[1]])
		//         - M[rows[0], cols[1]] * (M[rows[1], cols[0]] * M[rows[2], cols[2]] - M[rows[1], cols[2]] * M[rows[2], cols[0]])
		//         + M[rows[0], cols[2]] * (M[rows[1], cols[0]] * M[rows[2], cols[1]] - M[rows[1], cols[1]] * M[rows[2], cols[0]]);
		//}

		//public float Cofactor(UPInt I, UPInt J) const
		//{
		//    const UPInt indices[4, 3] = {{1,2,3},{0,2,3},{0,1,3},{0,1,2}};
		//    return ((I+J)&1) ? -SubDet(indices[I],indices[J]) : SubDet(indices[I],indices[J]);
		//}

		//public float Determinant()
		//{
		//    return M[0, 0] * Cofactor(0,0) + M[0, 1] * Cofactor(0,1) + M[0, 2] * Cofactor(0,2) + M[0, 3] * Cofactor(0,3);
		//}

		//public Matrix4f Adjugated() const
		//{
		//    return Matrix4f(Cofactor(0,0), Cofactor(1,0), Cofactor(2,0), Cofactor(3,0), 
		//                    Cofactor(0,1), Cofactor(1,1), Cofactor(2,1), Cofactor(3,1), 
		//                    Cofactor(0,2), Cofactor(1,2), Cofactor(2,2), Cofactor(3,2),
		//                    Cofactor(0,3), Cofactor(1,3), Cofactor(2,3), Cofactor(3,3));
		//}

		//public Matrix4f Inverted() const
		//{
		//    float det = Determinant();
		//    assert(det != 0);
		//    return Adjugated() * (1.0f/det);
		//}

		//public void Invert()
		//{
		//    *this = Inverted();
		//}

		// This is more efficient than general inverse, but ONLY works
		// correctly if it is a homogeneous transform matrix (rot + trans)
		//public Matrix4f InvertedHomogeneousTransform() const
		//{
		//    // Make the inverse rotation matrix
		//    Matrix4f rinv = this->Transposed();
		//    rinv.M[3, 0] = rinv.M[3, 1] = rinv.M[3, 2] = 0.0f;
		//    // Make the inverse translation matrix
		//    Vector3f tvinv = Vector3f(-M[0, 3],-M[1, 3],-M[2, 3]);
		//    Matrix4f tinv = Matrix4f::Translation(tvinv);
		//    return rinv * tinv;  // "untranslate", then "unrotate"
		//}

		// This is more efficient than general inverse, but ONLY works
		// correctly if it is a homogeneous transform matrix (rot + trans)
		//public void InvertHomogeneousTransform()
		//{
		//    *this = InvertedHomogeneousTransform();
		//}

		// Matrix to Euler Angles conversion
		// a,b,c, are the YawPitchRoll angles to be returned
		// rotation a around axis A1
		// is followed by rotation b around axis A2
		// is followed by rotation c around axis A3
		// rotations are CCW or CW (D) in LH or RH coordinate system (S)
		//template <Axis A1, Axis A2, Axis A3, RotateDirection D, HandedSystem S>
		//public void ToEulerAngles(float *a, float *b, float *c)
		//{
		//    OVR_COMPILER_ASSERT((A1 != A2) && (A2 != A3) && (A1 != A3));

		//    float psign = -1.0f;
		//    if (((A1 + 1) % 3 == A2) && ((A2 + 1) % 3 == A3)) // Determine whether even permutation
		//    psign = 1.0f;
        
		//    float pm = psign*M[A1, A3];
		//    if (pm < -1.0f + Math<float>::SingularityRadius)
		//    { // South pole singularity
		//        *a = 0.0f;
		//        *b = -S*D*Math<float>::PiOver2;
		//        *c = S*D*atan2( psign*M[A2, A1], M[A2, A2] );
		//    }
		//    else if (pm > 1.0f - Math<float>::SingularityRadius)
		//    { // North pole singularity
		//        *a = 0.0f;
		//        *b = S*D*Math<float>::PiOver2;
		//        *c = S*D*atan2( psign*M[A2, A1], M[A2, A2] );
		//    }
		//    else
		//    { // Normal case (nonsingular)
		//        *a = S*D*atan2( -psign*M[A2, A3], M[A3, A3] );
		//        *b = S*D*asin(pm);
		//        *c = S*D*atan2( -psign*M[A1, A2], M[A1, A1] );
		//    }

		//    return;
		//}

		// Matrix to Euler Angles conversion
		// a,b,c, are the YawPitchRoll angles to be returned
		// rotation a around axis A1
		// is followed by rotation b around axis A2
		// is followed by rotation c around axis A1
		// rotations are CCW or CW (D) in LH or RH coordinate system (S)
		//template <Axis A1, Axis A2, RotateDirection D, HandedSystem S>
		//public void ToEulerAnglesABA(float *a, float *b, float *c)
		//{        
		//     OVR_COMPILER_ASSERT(A1 != A2);
  
		//    // Determine the axis that was not supplied
		//    int m = 3 - A1 - A2;

		//    float psign = -1.0f;
		//    if ((A1 + 1) % 3 == A2) // Determine whether even permutation
		//        psign = 1.0f;

		//    float c2 = M[A1, A1];
		//    if (c2 < -1.0f + Math<float>::SingularityRadius)
		//    { // South pole singularity
		//        *a = 0.0f;
		//        *b = S*D*Math<float>::Pi;
		//        *c = S*D*atan2( -psign*M[A2, m],M[A2, A2]);
		//    }
		//    else if (c2 > 1.0f - Math<float>::SingularityRadius)
		//    { // North pole singularity
		//        *a = 0.0f;
		//        *b = 0.0f;
		//        *c = S*D*atan2( -psign*M[A2, m],M[A2, A2]);
		//    }
		//    else
		//    { // Normal case (nonsingular)
		//        *a = S*D*atan2( M[A2, A1],-psign*M[m, A1]);
		//        *b = S*D*acos(c2);
		//        *c = S*D*atan2( M[A1, A2],psign*M[A1, m]);
		//    }
		//    return;
		//}
  
		// Creates a matrix that converts the vertices from one coordinate system
		// to another.
		//public static Matrix4f AxisConversion(const WorldAxes& to, const WorldAxes& from)
		//{        
		//    // Holds axis values from the 'to' structure
		//    int toArray[3] = { to.XAxis, to.YAxis, to.ZAxis };

		//    // The inverse of the toArray
		//    int inv[4]; 
		//    inv[0] = inv[abs(to.XAxis)] = 0;
		//    inv[abs(to.YAxis)] = 1;
		//    inv[abs(to.ZAxis)] = 2;

		//    Matrix4f m(0,  0,  0, 
		//               0,  0,  0,
		//               0,  0,  0);

		//    // Only three values in the matrix need to be changed to 1 or -1.
		//    m.M[inv[abs(from.XAxis)], 0] = float(from.XAxis/toArray[inv[abs(from.XAxis)]]);
		//    m.M[inv[abs(from.YAxis)], 1] = float(from.YAxis/toArray[inv[abs(from.YAxis)]]);
		//    m.M[inv[abs(from.ZAxis)], 2] = float(from.ZAxis/toArray[inv[abs(from.ZAxis)]]);
		//    return m;
		//} 


		// Creates a matrix for translation by vector
		//public static Matrix4f Translation(const Vector3f& v)
		//{
		//    Matrix4f t;
		//    t.M[0, 3] = v.x;
		//    t.M[1, 3] = v.y;
		//    t.M[2, 3] = v.z;
		//    return t;
		//}

		// Creates a matrix for translation by vector
		//public static Matrix4f Translation(float x, float y, float z = 0.0f)
		//{
		//    Matrix4f t;
		//    t.M[0, 3] = x;
		//    t.M[1, 3] = y;
		//    t.M[2, 3] = z;
		//    return t;
		//}

		//// Creates a matrix for scaling by vector
		//public static Matrix4f Scaling(const Vector3f& v)
		//{
		//    Matrix4f t;
		//    t.M[0, 0] = v.x;
		//    t.M[1, 1] = v.y;
		//    t.M[2, 2] = v.z;
		//    return t;
		//}

		//// Creates a matrix for scaling by vector
		//public static Matrix4f Scaling(float x, float y, float z)
		//{
		//    Matrix4f t;
		//    t.M[0, 0] = x;
		//    t.M[1, 1] = y;
		//    t.M[2, 2] = z;
		//    return t;
		//}

		//// Creates a matrix for scaling by constant
		//public static Matrix4f Scaling(float s)
		//{
		//    Matrix4f t;
		//    t.M[0, 0] = s;
		//    t.M[1, 1] = s;
		//    t.M[2, 2] = s;
		//    return t;
		//}

  

		//// Creates a rotation matrix rotating around the X axis by 'angle' radians.
		//// Just for quick testing.  Not for final API.  Need to remove case.
		//public static Matrix4f RotationAxis(Axis A, float angle, RotateDirection d, HandedSystem s)
		//{
		//    float sina = s * d *sin(angle);
		//    float cosa = cos(angle);
        
		//    switch(A)
		//    {
		//    case Axis_X:
		//        return Matrix4f(1,  0,     0, 
		//                        0,  cosa,  -sina,
		//                        0,  sina,  cosa);
		//    case Axis_Y:
		//        return Matrix4f(cosa,  0,   sina, 
		//                        0,     1,   0,
		//                        -sina, 0,   cosa);
		//    case Axis_Z:
		//        return Matrix4f(cosa,  -sina,  0, 
		//                        sina,  cosa,   0,
		//                        0,     0,      1);
		//    }
		//}


		//// Creates a rotation matrix rotating around the X axis by 'angle' radians.
		//// Rotation direction is depends on the coordinate system:
		//// RHS (Oculus default): Positive angle values rotate Counter-clockwise (CCW),
		////                        while looking in the negative axis direction. This is the
		////                        same as looking down from positive axis values towards origin.
		//// LHS: Positive angle values rotate clock-wise (CW), while looking in the
		////       negative axis direction.
		//public static Matrix4f RotationX(float angle)
		//{
		//    float sina = sin(angle);
		//    float cosa = cos(angle);
		//    return Matrix4f(1,  0,     0, 
		//                    0,  cosa,  -sina,
		//                    0,  sina,  cosa);
		//}

		//// Creates a rotation matrix rotating around the Y axis by 'angle' radians.
		//// Rotation direction is depends on the coordinate system:
		////  RHS (Oculus default): Positive angle values rotate Counter-clockwise (CCW),
		////                        while looking in the negative axis direction. This is the
		////                        same as looking down from positive axis values towards origin.
		////  LHS: Positive angle values rotate clock-wise (CW), while looking in the
		////       negative axis direction.
		//public static Matrix4f RotationY(float angle)
		//{
		//    float sina = sin(angle);
		//    float cosa = cos(angle);
		//    return Matrix4f(cosa,  0,   sina, 
		//                    0,     1,   0,
		//                    -sina, 0,   cosa);
		//}

		//// Creates a rotation matrix rotating around the Z axis by 'angle' radians.
		//// Rotation direction is depends on the coordinate system:
		////  RHS (Oculus default): Positive angle values rotate Counter-clockwise (CCW),
		////                        while looking in the negative axis direction. This is the
		////                        same as looking down from positive axis values towards origin.
		////  LHS: Positive angle values rotate clock-wise (CW), while looking in the
		////       negative axis direction.
		//public static Matrix4f RotationZ(float angle)
		//{
		//    float sina = sin(angle);
		//    float cosa = cos(angle);
		//    return Matrix4f(cosa,  -sina,  0, 
		//                    sina,  cosa,   0,
		//                    0,     0,      1);
		//}


		//// LookAtRH creates a View transformation matrix for right-handed coordinate system.
		//// The resulting matrix points camera from 'eye' towards 'at' direction, with 'up'
		//// specifying the up vector. The resulting matrix should be used with PerspectiveRH
		//// projection.
		//public static Matrix4f LookAtRH(const Vector3f& eye, const Vector3f& at, const Vector3f& up);

		//// LookAtLH creates a View transformation matrix for left-handed coordinate system.
		//// The resulting matrix points camera from 'eye' towards 'at' direction, with 'up'
		//// specifying the up vector. 
		//public static Matrix4f LookAtLH(const Vector3f& eye, const Vector3f& at, const Vector3f& up);
    
    
		//// PerspectiveRH creates a right-handed perspective projection matrix that can be
		//// used with the Oculus sample renderer. 
		////  yfov   - Specifies vertical field of view in radians.
		////  aspect - Screen aspect ration, which is usually width/height for square pixels.
		////           Note that xfov = yfov * aspect.
		////  znear  - Absolute value of near Z clipping clipping range.
		////  zfar   - Absolute value of far  Z clipping clipping range (larger then near).
		//// Even though RHS usually looks in the direction of negative Z, positive values
		//// are expected for znear and zfar.
		//public static Matrix4f PerspectiveRH(float yfov, float aspect, float znear, float zfar);
    
    
		//// PerspectiveRH creates a left-handed perspective projection matrix that can be
		//// used with the Oculus sample renderer. 
		////  yfov   - Specifies vertical field of view in radians.
		////  aspect - Screen aspect ration, which is usually width/height for square pixels.
		////           Note that xfov = yfov * aspect.
		////  znear  - Absolute value of near Z clipping clipping range.
		////  zfar   - Absolute value of far  Z clipping clipping range (larger then near).
		//public static Matrix4f PerspectiveLH(float yfov, float aspect, float znear, float zfar);


		//public static Matrix4f Ortho2D(float w, float h);
	};
}
