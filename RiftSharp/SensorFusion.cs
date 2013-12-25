using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RiftSharp;
using RiftSharp.Math;
using System.Diagnostics;

namespace RiftSharp
{
	//-------------------------------------------------------------------------------------
	// ***** SensorFusion

	// SensorFusion class accumulates Sensor notification messages to keep track of
	// orientation, which involves integrating the gyro and doing correction with gravity.
	// Magnetometer based yaw drift correction is also supported; it is usually enabled
	// automatically based on loaded magnetometer configuration.
	// Orientation is reported as a quaternion, from which users can obtain either the
	// rotation matrix or Euler angles.
	//
	// The class can operate in two ways:
	//  - By user manually passing MessageBodyFrame messages to the OnMessage() function. 
	//  - By attaching SensorFusion to a SensorDevice, in which case it will
	//    automatically handle notifications from that device.
	public class SensorFusion
	{
		const int MagMaxReferences = 1000;

		private SensorInfo        CachedSensorInfo;
    
		private Quatf             Q;
		private Quatf			  QUncorrected;
		private Vector3f          A;    
		private Vector3f          AngV;
		private Vector3f          CalMag;
		private Vector3f          RawMag;
		private uint      Stage;
		private float             RunningTime;
		private float             DeltaT;
		private float             Gain;
		private volatile bool     EnableGravity;

		private bool              EnablePrediction;
		private float             PredictionDT;
		private float             PredictionTimeIncrement;

		private SensorFilter      FRawMag;
		private SensorFilter      FAngV;

		private Vector3f          GyroOffset;
		private SensorFilterBase_float  TiltAngleFilter;


		private bool              EnableYawCorrection;
		private bool              MagCalibrated;
		private Matrix4f          MagCalibrationMatrix;
		private DateTime          MagCalibrationTime;    
		private int               MagNumReferences;
		private Vector3f[]        MagRefsInBodyFrame = new Vector3f[MagMaxReferences];
		private Vector3f[]        MagRefsInWorldFrame= new Vector3f[MagMaxReferences];
		private int               MagRefIdx;
		private int               MagRefScore;

		private bool              MotionTrackingEnabled;

		public SensorFusion(RiftHeadsetDevice sensor = null){
			Stage = 0;
			RunningTime = 0;
			DeltaT = 0.001f;
			Gain = 0.05f;
			EnableGravity = true;
			EnablePrediction = true; 
			PredictionDT = 0.03f;
			PredictionTimeIncrement = 0.001f;
			FRawMag = new SensorFilter(10);
			FAngV = new SensorFilter(20); 
			GyroOffset = new Vector3f();
			TiltAngleFilter = new SensorFilterBase_float(1000);
			EnableYawCorrection = false;
			MagCalibrated = false;
			MagNumReferences = 0;
			MagRefIdx = -1;
			MagRefScore = 0;
			MotionTrackingEnabled = true;

			if (sensor != null)
				AttachToSensor(sensor);
			MagCalibrationMatrix = new Matrix4f();
			MagCalibrationMatrix.SetIdentity();
		}
		

		// *** Setup
    
		// Attaches this SensorFusion to a sensor device, from which it will receive
		// notification messages. If a sensor is attached, manual message notification
		// is not necessary. Calling this function also resets SensorFusion state.
		public bool AttachToSensor(RiftHeadsetDevice sensor){
			// clear the cached device information
			CachedSensorInfo = sensor.GetDeviceInfo();   // save the device information
			Reset();

			// Automatically load the default mag calibration for this sensor
			//LoadMagCalibration();
			
			return true;
		}

		// Returns true if this Sensor fusion object is attached to a sensor.
		public bool        IsAttachedToSensor() {
			return true;
		}

		// Compute a rotation required to transform "estimated" into "measured"
		// Returns an approximation of the goal rotation in the Simultaneous Orthogonal Rotations Angle representation
		// (vector direction is the axis of rotation, norm is the angle)
		public Vector3f SensorFusion_ComputeCorrection(Vector3f measured, Vector3f estimated)
		{
			measured.Normalize();
			estimated.Normalize();
			Vector3f correction = measured.Cross(estimated);
			float cosError = measured.Dot(estimated);
			// from the def. of cross product, correction.Length() = sin(error)
			// therefore sin(error) * sqrt(2 / (1 + cos(error))) = 2 * sin(error / 2) ~= error in [-pi, pi]
			// Mathf::Tolerance is used to avoid div by 0 if cos(error) = -1
			return correction * (float)System.Math.Sqrt(2 / (1 + cosError + Vector3f.Tolerance));
		}

		// *** State Query

		// Obtain the current accumulated orientation. Many apps will want to use GetPredictedOrientation
		// instead to reduce latency.
		public Quatf       GetOrientation() { return Q; }

		// Get predicted orientaion in the near future; predictDt is lookahead amount in seconds.
		public Quatf       GetPredictedOrientation(float predictDt){		
			throw new NotImplementedException();
			//Lock::Locker lockScope(Handler.GetHandlerLock());
			//Quatf        qP = Q;
    
			//if (EnablePrediction)
			//{
			//    // This method assumes a constant angular velocity
			//    Vector3f angVelF  = FAngV.SavitzkyGolaySmooth8();
			//    float    angVelFL = angVelF.Length();

			//    // Force back to raw measurement
			//    angVelF  = AngV;
			//    angVelFL = AngV.Length();

			//    // Dynamic prediction interval: Based on angular velocity to reduce vibration
			//    const float minPdt   = 0.001f;
			//    const float slopePdt = 0.1f;
			//    float       newpdt   = pdt;
			//    float       tpdt     = minPdt + slopePdt * angVelFL;
			//    if (tpdt < pdt)
			//        newpdt = tpdt;
			//    //LogText("PredictonDTs: %d\n",(int)(newpdt / PredictionTimeIncrement + 0.5f));

			//    if (angVelFL > 0.001f)
			//    {
			//        Vector3f    rotAxisP      = angVelF / angVelFL;  
			//        float       halfRotAngleP = angVelFL * newpdt * 0.5f;
			//        float       sinaHRAP      = sin(halfRotAngleP);
			//        Quatf       deltaQP(rotAxisP.x*sinaHRAP, rotAxisP.y*sinaHRAP,
			//                            rotAxisP.z*sinaHRAP, cos(halfRotAngleP));
			//        qP = Q * deltaQP;
			//    }
			//}
			//return qP;
		}
		public Quatf       GetPredictedOrientation()   { return GetPredictedOrientation(PredictionDT); }

		// Obtain the last absolute acceleration reading, in m/s^2.
		public Vector3f    GetAcceleration() { return A; }
		// Obtain the last angular velocity reading, in rad/s.
		public Vector3f    GetAngularVelocity() { return AngV; }

		// Obtain the last raw magnetometer reading, in Gauss
		public Vector3f    GetMagnetometer() { return RawMag; }   
		// Obtain the calibrated magnetometer reading (direction and field strength)
		public Vector3f    GetCalibratedMagnetometer() { Debug.Assert(MagCalibrated); return CalMag; }


		// Resets the current orientation.
		public void        Reset(){
			Q                     = new Quatf();
			QUncorrected          = new Quatf();
			Stage                 = 0;
			RunningTime           = 0;
			MagNumReferences      = 0;
			MagRefIdx             = -1;
			GyroOffset            = new Vector3f();
		}



		// *** Configuration

		public void        EnableMotionTracking(bool enable = true)    { MotionTrackingEnabled = enable; }
		public bool        IsMotionTrackingEnabled()              { return MotionTrackingEnabled;   }



		// *** Prediction Control

		// Prediction functions.
		// Prediction delta specifes how much prediction should be applied in seconds; it should in
		// general be under the average rendering latency. Call GetPredictedOrientation() to get
		// predicted orientation.
		public float       GetPredictionDelta()                   { return PredictionDT; }
		public void        SetPrediction(float dt, bool enable = true) { PredictionDT = dt; EnablePrediction = enable; }
		public void		SetPredictionEnabled(bool enable = true)    { EnablePrediction = enable; }    
		public bool		IsPredictionEnabled()                       { return EnablePrediction; }


		// *** Accelerometer/Gravity Correction Control

		// Enables/disables gravity correction (on by default).
		public void        SetGravityEnabled(bool enableGravity)       { EnableGravity = enableGravity; }   
		public bool        IsGravityEnabled()                     { return EnableGravity;}

		// Gain used to correct gyro with accel. Default value is appropriate for typical use.
		public float       GetAccelGain()                         { return Gain; }
		public void        SetAccelGain(float ag)                      { Gain = ag; }


		// *** Magnetometer and Yaw Drift Correction Control

		// Methods to load and save a mag calibration.  Calibrations can optionally
		// be specified by name to differentiate multiple calibrations under different conditions
		// If LoadMagCalibration succeeds, it will override YawCorrectionEnabled based on
		// saved calibration setting.
		
		//// Writes the current calibration for a particular device to a device profile file
		//// sensor - the sensor that was calibrated
		//// cal_name - an optional name for the calibration or default if cal_name == NULL
		//public bool        SaveMagCalibration(string calibrationName = "") {
		//    if (CachedSensorInfo.SerialNumber[0] == 0 || !HasMagCalibration())
		//        return false;
    
		//    // A named calibration may be specified for calibration in different
		//    // environments, otherwise the default calibration is used
		//    if (calibrationName == NULL)
		//        calibrationName = "default";

		//    // Generate a mag calibration event
		//    JSON* calibration = JSON::CreateObject();
		//    // (hardcoded for now) the measurement and representation method 
		//    calibration->AddStringItem("Version", "2.0");   
		//    calibration->AddStringItem("Name", "default");

		//    // time stamp the calibration
		//    char time_str[64];
   
		//    struct tm caltime;
		//    localtime_s(&caltime, &MagCalibrationTime);
		//    strftime(time_str, 64, "%Y-%m-%d %H:%M:%S", &caltime);
   
		//    calibration->AddStringItem("Time", time_str);

		//    // write the full calibration matrix
		//    char matrix[256];
		//    Matrix4f calmat = GetMagCalibration();
		//    calmat.ToString(matrix, 256);
		//    calibration->AddStringItem("CalibrationMatrix", matrix);
		//    // save just the offset, for backwards compatibility
		//    // this can be removed when we don't want to support 0.2.4 anymore
		//    Vector3f center(calmat.M[0][3], calmat.M[1][3], calmat.M[2][3]);
		//    Matrix4f tmp = calmat; tmp.M[0][3] = tmp.M[1][3] = tmp.M[2][3] = 0; tmp.M[3][3] = 1;
		//    center = tmp.Inverted().Transform(center);
		//    Matrix4f oldcalmat; oldcalmat.M[0][3] = center.x; oldcalmat.M[1][3] = center.y; oldcalmat.M[2][3] = center.z; 
		//    oldcalmat.ToString(matrix, 256);
		//    calibration->AddStringItem("Calibration", matrix);
    

		//    String path = GetBaseOVRPath(true);
		//    path += "/Devices.json";

		//    // Look for a prexisting device file to edit
		//    Ptr<JSON> root = *JSON::Load(path);
		//    if (root)
		//    {   // Quick sanity check of the file type and format before we parse it
		//        JSON* version = root->GetFirstItem();
		//        if (version && version->Name == "Oculus Device Profile Version")
		//        {   
		//            int major = atoi(version->Value.ToCStr());
		//            if (major > MAX_DEVICE_PROFILE_MAJOR_VERSION)
		//            {
		//                // don't use the file on unsupported major version number
		//                root->Release();
		//                root = NULL;
		//            }
		//        }
		//        else
		//        {
		//            root->Release();
		//            root = NULL;
		//        }
		//    }

		//    JSON* device = NULL;
		//    if (root)
		//    {
		//        device = root->GetFirstItem();   // skip the header
		//        device = root->GetNextItem(device);
		//        while (device)
		//        {   // Search for a previous calibration with the same name for this device
		//            // and remove it before adding the new one
		//            if (device->Name == "Device")
		//            {   
		//                JSON* item = device->GetItemByName("Serial");
		//                if (item && item->Value == CachedSensorInfo.SerialNumber)
		//                {   // found an entry for this device
		//                    item = device->GetNextItem(item);
		//                    while (item)
		//                    {
		//                        if (item->Name == "MagCalibration")
		//                        {   
		//                            JSON* name = item->GetItemByName("Name");
		//                            if (name && name->Value == calibrationName)
		//                            {   // found a calibration of the same name
		//                                item->RemoveNode();
		//                                item->Release();
		//                                break;
		//                            } 
		//                        }
		//                        item = device->GetNextItem(item);
		//                    }

		//                    // update the auto-mag flag
		//                    item = device->GetItemByName("EnableYawCorrection");
		//                    if (item)
		//                        item->dValue = (double)EnableYawCorrection;
		//                    else
		//                        device->AddBoolItem("EnableYawCorrection", EnableYawCorrection);

		//                    break;
		//                }
		//            }

		//            device = root->GetNextItem(device);
		//        }
		//    }
		//    else
		//    {   // Create a new device root
		//        root = *JSON::CreateObject();
		//        root->AddStringItem("Oculus Device Profile Version", "1.0");
		//    }

		//    if (device == NULL)
		//    {
		//        device = JSON::CreateObject();
		//        device->AddStringItem("Product", CachedSensorInfo.ProductName);
		//        device->AddNumberItem("ProductID", CachedSensorInfo.ProductId);
		//        device->AddStringItem("Serial", CachedSensorInfo.SerialNumber);
		//        device->AddBoolItem("EnableYawCorrection", EnableYawCorrection);

		//        root->AddItem("Device", device);
		//    }

		//    // Create and the add the new calibration event to the device
		//    device->AddItem("MagCalibration", calibration);

		//    return root->Save(path);
		//}
		//// Loads a saved calibration for the specified device from the device profile file
		//// sensor - the sensor that the calibration was saved for
		//// cal_name - an optional name for the calibration or the default if cal_name == NULL
		//public bool        LoadMagCalibration(const char* calibrationName = NULL){
		//    if (CachedSensorInfo.SerialNumber[0] == 0)
		//        return false;

		//    // A named calibration may be specified for calibration in different
		//    // environments, otherwise the default calibration is used
		//    if (calibrationName == NULL)
		//        calibrationName = "default";

		//    String path = GetBaseOVRPath(true);
		//    path += "/Devices.json";

		//    // Load the device profiles
		//    Ptr<JSON> root = *JSON::Load(path);
		//    if (root == NULL)
		//        return false;

		//    // Quick sanity check of the file type and format before we parse it
		//    JSON* version = root->GetFirstItem();
		//    if (version && version->Name == "Oculus Device Profile Version")
		//    {   
		//        int major = atoi(version->Value.ToCStr());
		//        if (major > MAX_DEVICE_PROFILE_MAJOR_VERSION)
		//            return false;   // don't parse the file on unsupported major version number
		//    }
		//    else
		//    {
		//        return false;
		//    }

		//    bool autoEnableCorrection = false;    

		//    JSON* device = root->GetNextItem(version);
		//    while (device)
		//    {   // Search for a previous calibration with the same name for this device
		//        // and remove it before adding the new one
		//        if (device->Name == "Device")
		//        {   
		//            JSON* item = device->GetItemByName("Serial");
		//            if (item && item->Value == CachedSensorInfo.SerialNumber)
		//            {   // found an entry for this device

		//                JSON* autoyaw = device->GetItemByName("EnableYawCorrection");
		//                if (autoyaw)
		//                    autoEnableCorrection = (autoyaw->dValue != 0);

		//                int maxCalibrationVersion = 0;
		//                item = device->GetNextItem(item);
		//                while (item)
		//                {
		//                    if (item->Name == "MagCalibration")
		//                    {   
		//                        JSON* calibration = item;
		//                        JSON* name = calibration->GetItemByName("Name");
		//                        if (name && name->Value == calibrationName)
		//                        {   // found a calibration with this name
                            
		//                            int major = 0;
		//                            JSON* version = calibration->GetItemByName("Version");
		//                            if (version)
		//                                major = atoi(version->Value.ToCStr());

		//                            if (major > maxCalibrationVersion && major <= 2)
		//                            {
		//                                time_t now;
		//                                time(&now);

		//                                // parse the calibration time
		//                                time_t calibration_time = now;
		//                                JSON* caltime = calibration->GetItemByName("Time");
		//                                if (caltime)
		//                                {
		//                                    const char* caltime_str = caltime->Value.ToCStr();

		//                                    tm ct;
		//                                    memset(&ct, 0, sizeof(tm));
                            
		//                                    struct tm nowtime;
		//                                    localtime_s(&nowtime, &now);
		//                                    ct.tm_isdst = nowtime.tm_isdst;
		//                                    sscanf_s(caltime_str, "%d-%d-%d %d:%d:%d", 
		//                                        &ct.tm_year, &ct.tm_mon, &ct.tm_mday,
		//                                        &ct.tm_hour, &ct.tm_min, &ct.tm_sec);

		//                                    ct.tm_year -= 1900;
		//                                    ct.tm_mon--;
		//                                    calibration_time = mktime(&ct);
		//                                }
                                                        
		//                                // parse the calibration matrix
		//                                JSON* cal = calibration->GetItemByName("CalibrationMatrix");
		//                                if (cal == NULL)
		//                                    cal = calibration->GetItemByName("Calibration");
                               
		//                                if (cal)
		//                                {
		//                                    Matrix4f calmat = Matrix4f::FromString(cal->Value.ToCStr());
		//                                    SetMagCalibration(calmat);
		//                                    MagCalibrationTime  = calibration_time;
		//                                    EnableYawCorrection = autoEnableCorrection;

		//                                    maxCalibrationVersion = major;
		//                                }
		//                            }
		//                        } 
		//                    }
		//                    item = device->GetNextItem(item);
		//                }

		//                return (maxCalibrationVersion > 0);
		//            }
		//        }

		//        device = root->GetNextItem(device);
		//    }
    
		//    return false;
		//}

		// Enables/disables magnetometer based yaw drift correction. Must also have mag calibration
		// data for this correction to work.
		public void        SetYawCorrectionEnabled(bool enable)    { EnableYawCorrection = enable; }
		// Determines if yaw correction is enabled.
		public bool        IsYawCorrectionEnabled()           { return EnableYawCorrection;}

		// Store the calibration matrix for the magnetometer
		public void        SetMagCalibration(Matrix4f m)
		{
			MagCalibrationMatrix = m;
			MagCalibrationTime = DateTime.Now;   // time stamp the calibration
			MagCalibrated = true;
		}

		// Retrieves the magnetometer calibration matrix
		public Matrix4f    GetMagCalibration()         { return MagCalibrationMatrix; }
		// Retrieve the time of the calibration
		public DateTime      GetMagCalibrationTime() { return MagCalibrationTime; }

		// True only if the mag has calibration values stored
		public bool        HasMagCalibration()         { return MagCalibrated;}  
		// Force the mag into the uncalibrated state
		public void        ClearMagCalibration()            { MagCalibrated = false; }

		// These refer to reference points that associate mag readings with orientations
		public void        ClearMagReferences()             { MagNumReferences = 0; }


		public Vector3f    GetCalibratedMagValue(Vector3f rawMag) {
			Debug.Assert(HasMagCalibration());
			return MagCalibrationMatrix.Transform(rawMag);
		}



		// *** Message Handler Logic

		// Notifies SensorFusion object about a new BodyFrame message from a sensor.
		// Should be called by user if not attaching to a sensor.
		public void OnMessage(MessageBodyFrame msg)
		{
			handleMessage(msg);
		}

		//public void        SetDelegateMessageHandler(MessageHandler* handler)
		//{ pDelegate = handler; }


		// Internal handler for messages; bypasses error checking.
		private void handleMessage(MessageBodyFrame msg){
			// Put the sensor readings into convenient local variables
			Vector3f gyro = msg.RotationRate;
			Vector3f accel = msg.Acceleration;
			Vector3f mag = msg.MagneticField;

			// Insert current sensor data into filter history
			FRawMag.AddElement(mag);
			FAngV.AddElement(gyro);

			// Apply the calibration parameters to raw mag
			Vector3f calMag = MagCalibrated ? GetCalibratedMagValue(FRawMag.Mean()) : FRawMag.Mean();

			// Set variables accessible through the class API
			DeltaT = msg.TimeDelta;
			AngV = gyro;
			A = accel;
			RawMag = mag;
			CalMag = calMag;

			// Keep track of time
			Stage++;
			RunningTime += DeltaT;

			// Small preprocessing
			Quatf Qinv = Q.Inverted();
			Vector3f up = Qinv.Rotate(new Vector3f(0, 1, 0));

			Vector3f gyroCorrected = gyro;

			// Apply integral term
			// All the corrections are stored in the Simultaneous Orthogonal Rotations Angle representation,
			// which allows to combine and scale them by just addition and multiplication
			if (EnableGravity || EnableYawCorrection)
				gyroCorrected -= GyroOffset;

			if (EnableGravity) {
				const float spikeThreshold = 0.01f;
				const float gravityThreshold = 0.1f;
				float proportionalGain = 5 * Gain; // Gain parameter should be removed in a future release
				float integralGain = 0.0125f;

				Vector3f tiltCorrection = SensorFusion_ComputeCorrection(accel, up);

				if (Stage > 5) {
					// Spike detection
					float tiltAngle = up.Angle(accel);
					TiltAngleFilter.AddElement(tiltAngle);
					if (tiltAngle > TiltAngleFilter.Mean() + spikeThreshold)
						proportionalGain = integralGain = 0;
					// Acceleration detection
					const float gravity = 9.8f;
					if (System.Math.Abs(accel.Length() / gravity - 1) > gravityThreshold)
						integralGain = 0;
				} else // Apply full correction at the startup
			    {
					proportionalGain = 1 / DeltaT;
					integralGain = 0;
				}

				gyroCorrected += (tiltCorrection * proportionalGain);
				GyroOffset -= (tiltCorrection * integralGain * DeltaT);
			}

			if (EnableYawCorrection && MagCalibrated && RunningTime > 2.0f) {
				const float maxMagRefDist = 0.1f;
				const float maxTiltError = 0.05f;
				float proportionalGain = 0.01f;
				float integralGain = 0.0005f;

				// Update the reference point if needed
				if (MagRefIdx < 0 || calMag.Distance(MagRefsInBodyFrame[MagRefIdx]) > maxMagRefDist) {
					// Delete a bad point
					if (MagRefIdx >= 0 && MagRefScore < 0) {
						MagNumReferences--;
						MagRefsInBodyFrame[MagRefIdx] = MagRefsInBodyFrame[MagNumReferences];
						MagRefsInWorldFrame[MagRefIdx] = MagRefsInWorldFrame[MagNumReferences];
					}
					// Find a new one
					MagRefIdx = -1;
					MagRefScore = 1000;
					float bestDist = maxMagRefDist;
					for (int i = 0; i < MagNumReferences; i++) {
						float dist = calMag.Distance(MagRefsInBodyFrame[i]);
						if (bestDist > dist) {
							bestDist = dist;
							MagRefIdx = i;
						}
					}
					// Create one if needed
					if (MagRefIdx < 0 && MagNumReferences < MagMaxReferences) {
						MagRefIdx = MagNumReferences;
						MagRefsInBodyFrame[MagRefIdx] = calMag;
						MagRefsInWorldFrame[MagRefIdx] = Q.Rotate(calMag).Normalized();
						MagNumReferences++;
					}
				}

				if (MagRefIdx >= 0) {
					Vector3f magEstimated = Qinv.Rotate(MagRefsInWorldFrame[MagRefIdx]);
					Vector3f magMeasured = calMag.Normalized();

					// Correction is computed in the horizontal plane (in the world frame)
					Vector3f yawCorrection = SensorFusion_ComputeCorrection(magMeasured.ProjectToPlane(up),
																			magEstimated.ProjectToPlane(up));

					if (System.Math.Abs(up.Dot(magEstimated - magMeasured)) < maxTiltError) {
						MagRefScore += 2;
					} else // If the vertical angle is wrong, decrease the score
			        {
						MagRefScore -= 1;
						proportionalGain = integralGain = 0;
					}
					gyroCorrected += (yawCorrection * proportionalGain);
					GyroOffset -= (yawCorrection * integralGain * DeltaT);
				}
			}

			// Update the orientation quaternion based on the corrected angular velocity vector
			float angle = gyroCorrected.Length() * DeltaT;
			if (angle > 0.0f)
				Q = Q * new Quatf(gyroCorrected, angle);

			// The quaternion magnitude may slowly drift due to numerical error,
			// so it is periodically normalized.
			if (Stage % 500 == 0)
				Q.Normalize();
		}

		//// Set the magnetometer's reference orientation for use in yaw correction
		//// The supplied mag is an uncalibrated value
		//private void        setMagReference(Quatf q, Vector3f rawMag);
		//// Default to current HMD orientation
		//private void        setMagReference()  { setMagReference(Q, RawMag); }
	};
}
