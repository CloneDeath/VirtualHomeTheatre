using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using RiftSharp.Math;

namespace RiftSharp
{
	// A simple circular buffer data structure that stores last N elements in an array
	class CircularBuffer<T> where T : new()
	{
		protected const int DefaultFilterCapacity = 20;

		protected int         LastIdx;                    // The index of the last element that was added to the buffer
		protected int         Capacity;                   // The buffer size (maximum number of elements)
		protected int         Count;                      // Number of elements in the filter
		protected T[]          Elements;

		public CircularBuffer(int Capacity = DefaultFilterCapacity){
			this.Capacity = Capacity;
			this.Count = 0;
			this.LastIdx = 0;
			Elements = new T[Capacity];
			for (int i = 0; i < Capacity; i++){
				Elements[i] = new T();
			}
		}
	
		// Add a new element to the filter
		public virtual void AddElement(T e)
		{
			LastIdx = (LastIdx + 1) % Capacity;
			Elements[LastIdx] = e;
			if (Count < Capacity)
				Count++;
		}

		// Get element i.  0 is the most recent, 1 is one step ago, 2 is two steps ago, ...
		public virtual T GetPrev(int i = 0)
		{
			Debug.Assert(i >= 0);
			if (i >= Count) // return 0 if the filter doesn't have enough elements
				return new T();
			int idx = (LastIdx - i);
			if (idx < 0) // Fix the wraparound case
				idx += Capacity;
			Debug.Assert(idx >= 0); // Multiple wraparounds not allowed
			return Elements[idx];
		}
	};

	// A base class for filters that maintains a buffer of sensor data taken over time and implements
	// various simple filters, most of which are linear functions of the data history.
	// Maintains the running sum of its elements for better performance on large capacity values
	class SensorFilterBase_float : CircularBuffer<float>
	{
		protected float RunningTotal; // Cached sum of the elements

		public SensorFilterBase_float(int capacity = DefaultFilterCapacity)
			: base(capacity)
		{
			RunningTotal = new float();
		}

		// Add a new element to the filter
		// Updates the running sum value
		public override void AddElement(float e)
		{
			int NextIdx = (this.LastIdx + 1) % this.Capacity;
			RunningTotal += (e - this.Elements[NextIdx]);
			base.AddElement(e);
			if (this.LastIdx == 0) {
				// update the cached total to avoid error accumulation
				RunningTotal = new float();
				for (int i = 0; i < this.Count; i++)
					RunningTotal += this.Elements[i];
			}
		}

		// Simple statistics
		public float Total()
		{
			return RunningTotal;
		}

		public float Mean()
		{
			return (this.Count == 0) ? new float() : (Total() / (float)this.Count);
		}

		// A popular family of smoothing filters and smoothed derivatives
		public float SavitzkyGolaySmooth8()
		{
			Debug.Assert(this.Capacity >= 8);
			return this.GetPrev(0) * 0.41667f +
					this.GetPrev(1) * 0.33333f +
					this.GetPrev(2) * 0.25f +
					this.GetPrev(3) * 0.16667f +
					this.GetPrev(4) * 0.08333f -
					this.GetPrev(6) * 0.08333f -
					this.GetPrev(7) * 0.16667f;
		}

		public float SavitzkyGolayDerivative4()
		{
			Debug.Assert(this.Capacity >= 4);
			return this.GetPrev(0) * 0.3f +
					this.GetPrev(1) * 0.1f -
					this.GetPrev(2) * 0.1f -
					this.GetPrev(3) * 0.3f;
		}

		public float SavitzkyGolayDerivative5()
		{
			Debug.Assert(this.Capacity >= 5);
			return this.GetPrev(0) * 0.2f +
					this.GetPrev(1) * 0.1f -
					this.GetPrev(3) * 0.1f -
					this.GetPrev(4) * 0.2f;
		}

		public float SavitzkyGolayDerivative12()
		{
			Debug.Assert(this.Capacity >= 12);
			return this.GetPrev(0) * 0.03846f +
					this.GetPrev(1) * 0.03147f +
					this.GetPrev(2) * 0.02448f +
					this.GetPrev(3) * 0.01748f +
					this.GetPrev(4) * 0.01049f +
					this.GetPrev(5) * 0.0035f -
					this.GetPrev(6) * 0.0035f -
					this.GetPrev(7) * 0.01049f -
					this.GetPrev(8) * 0.01748f -
					this.GetPrev(9) * 0.02448f -
					this.GetPrev(10) * 0.03147f -
					this.GetPrev(11) * 0.03846f;
		}

		public float SavitzkyGolayDerivativeN(int n)
		{
			Debug.Assert(this.Capacity >= n);
			int m = (n - 1) / 2;
			float result = new float();
			for (int k = 1; k <= m; k++) {
				int ind1 = m - k;
				int ind2 = n - m + k - 1;
				result += (this.GetPrev(ind1) - this.GetPrev(ind2)) * (float)k;
			}
			float coef = 3.0f / (m * (m + 1.0f) * (2.0f * m + 1.0f));
			result = result * coef;
			return result;
		}
	};

	// A base class for filters that maintains a buffer of sensor data taken over time and implements
	// various simple filters, most of which are linear functions of the data history.
	// Maintains the running sum of its elements for better performance on large capacity values
	class SensorFilterBase : CircularBuffer<Vector3f>
	{
		protected Vector3f RunningTotal; // Cached sum of the elements

		public SensorFilterBase(int capacity = DefaultFilterCapacity) : base(capacity){ 
			RunningTotal = new Vector3f();
		}

		// Add a new element to the filter
		// Updates the running sum value
		public override void AddElement(Vector3f e)
		{
			int NextIdx = (this.LastIdx + 1) % this.Capacity;
			RunningTotal += (e - this.Elements[NextIdx]);
			base.AddElement(e);
			if (this.LastIdx == 0)
			{
				// update the cached total to avoid error accumulation
				RunningTotal = new Vector3f();
				for (int i = 0; i < this.Count; i++)
					RunningTotal += this.Elements[i];
			} 
		}

		// Simple statistics
		public Vector3f Total()
		{ 
			return RunningTotal; 
		}

		public Vector3f Mean()
		{
			return (this.Count == 0) ? new Vector3f() : (Total() / (float)this.Count);
		}

		// A popular family of smoothing filters and smoothed derivatives
		public Vector3f SavitzkyGolaySmooth8()
		{
			Debug.Assert(this.Capacity >= 8);
			return this.GetPrev(0)*0.41667f +
					this.GetPrev(1)*0.33333f +
					this.GetPrev(2)*0.25f +
					this.GetPrev(3)*0.16667f +
					this.GetPrev(4)*0.08333f -
					this.GetPrev(6)*0.08333f -
					this.GetPrev(7)*0.16667f;
		}

		public Vector3f SavitzkyGolayDerivative4()
		{
			Debug.Assert(this.Capacity >= 4);
			return this.GetPrev(0)*0.3f +
					this.GetPrev(1)*0.1f -
					this.GetPrev(2)*0.1f -
					this.GetPrev(3)*0.3f;
		}

		public Vector3f SavitzkyGolayDerivative5()
		{
				Debug.Assert(this.Capacity >= 5);
				return this.GetPrev(0)*0.2f +
						this.GetPrev(1)*0.1f -
						this.GetPrev(3)*0.1f -
						this.GetPrev(4)*0.2f;
		}

		public Vector3f SavitzkyGolayDerivative12()
		{
			Debug.Assert(this.Capacity >= 12);
			return this.GetPrev(0)*0.03846f +
					this.GetPrev(1)*0.03147f +
					this.GetPrev(2)*0.02448f +
					this.GetPrev(3)*0.01748f +
					this.GetPrev(4)*0.01049f +
					this.GetPrev(5)*0.0035f -
					this.GetPrev(6)*0.0035f -
					this.GetPrev(7)*0.01049f -
					this.GetPrev(8)*0.01748f -
					this.GetPrev(9)*0.02448f -
					this.GetPrev(10)*0.03147f -
					this.GetPrev(11)*0.03846f;
		} 

		public Vector3f SavitzkyGolayDerivativeN(int n)
		{    
			Debug.Assert(this.Capacity >= n);
			int m = (n-1)/2;
			Vector3f result = new Vector3f();
			for (int k = 1; k <= m; k++) 
			{
				int ind1 = m - k;
				int ind2 = n - m + k - 1;
				result += (this.GetPrev(ind1) - this.GetPrev(ind2)) * (float) k;
			}
			float coef = 3.0f/(m*(m+1.0f)*(2.0f*m+1.0f));
			result = result*coef;
			return result;
		}
	};

	// This class maintains a buffer of sensor data taken over time and implements
	// various simple filters, most of which are linear functions of the data history.
	class SensorFilter : SensorFilterBase
	{
		public SensorFilter(int capacity = DefaultFilterCapacity) : base(capacity) { 
		
		}

		// Simple statistics - bubble sort
		public Vector3f Median()
		{
			int half_window = Count / 2;
			float[] sortx = new float[Count];
			float[] sorty = new float[Count];
			float[] sortz = new float[Count];
			float resultx = 0.0f, resulty = 0.0f, resultz = 0.0f;

			for (int i = 0; i < Count; i++) 
			{
				sortx[i] = Elements[i].X;
				sorty[i] = Elements[i].Y;
				sortz[i] = Elements[i].Z;
			}
			for (int j = 0; j <= half_window; j++) 
			{
				int minx = j;
				int miny = j;
				int minz = j;
				for (int k = j + 1; k < Count; k++) 
				{
					if (sortx[k] < sortx[minx]) minx = k;
					if (sorty[k] < sorty[miny]) miny = k;
					if (sortz[k] < sortz[minz]) minz = k;
				}
				float tempx = sortx[j];
				float tempy = sorty[j];
				float tempz = sortz[j];
				sortx[j] = sortx[minx];
				sortx[minx] = tempx;

				sorty[j] = sorty[miny];
				sorty[miny] = tempy;

				sortz[j] = sortz[minz];
				sortz[minz] = tempz;
			}
			resultx = sortx[half_window];
			resulty = sorty[half_window];
			resultz = sortz[half_window];

			return new Vector3f(resultx, resulty, resultz);
		}
		// The diagonal of covariance matrix
		public Vector3f Variance() {
			Vector3f mean = Mean();
			Vector3f total = new Vector3f(0.0f, 0.0f, 0.0f);
			for (int i = 0; i < Count; i++) 
			{
				total.X += (Elements[i].X - mean.X) * (Elements[i].X - mean.X);
				total.Y += (Elements[i].Y - mean.Y) * (Elements[i].Y - mean.Y);
				total.Z += (Elements[i].Z - mean.Z) * (Elements[i].Z - mean.Z);
			}
			return total / (float) Count;
		}
		// Should be a 3x3 matrix returned, but OVR_math.h doesn't have one
		public Matrix4f Covariance(){
			Vector3f mean = Mean();
			Matrix4f total = new Matrix4f(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
			for (int i = 0; i < Count; i++) 
			{
				total.M[0, 0] += (Elements[i].X - mean.X) * (Elements[i].X - mean.X);
				total.M[1, 0] += (Elements[i].Y - mean.Y) * (Elements[i].X - mean.X);
				total.M[2, 0] += (Elements[i].Z - mean.Z) * (Elements[i].X - mean.X);
				total.M[1, 1] += (Elements[i].Y - mean.Y) * (Elements[i].Y - mean.Y);
				total.M[2, 1] += (Elements[i].Z - mean.Z) * (Elements[i].Y - mean.Y);
				total.M[2, 2] += (Elements[i].Z - mean.Z) * (Elements[i].Z - mean.Z);
			}
			total.M[0, 1] = total.M[1, 0];
			total.M[0, 2] = total.M[2, 0];
			total.M[1, 2] = total.M[2, 1];
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					total.M[i, j] *= 1.0f / Count;
			return total;
		}
		public Vector3f PearsonCoefficient(){
			Matrix4f cov = Covariance();
			Vector3f pearson = new Vector3f();
			pearson.X = (float)(cov.M[0, 1] / (System.Math.Sqrt(cov.M[0, 0]) * System.Math.Sqrt(cov.M[1, 1])));
			pearson.Y = (float)(cov.M[1, 2] / (System.Math.Sqrt(cov.M[1, 1]) * System.Math.Sqrt(cov.M[2, 2])));
			pearson.Z = (float)(cov.M[2, 0] / (System.Math.Sqrt(cov.M[2, 2]) * System.Math.Sqrt(cov.M[0, 0])));

			return pearson;
		}
	};
}
