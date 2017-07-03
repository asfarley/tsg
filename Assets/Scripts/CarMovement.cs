using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System.IO;

public class CarMovement : MonoBehaviour {
	
	public List<AxleInfo> axleInfos; // the information about each individual axle
	public float maxMotorTorque; // maximum torque the motor can apply to wheel
	public float maxSteeringAngle; // maximum steer angle the wheel can have
    public float steeringGain; // closed-loop heading gain
	public int maxLifetimeSeconds;
    public float brakeTorqueGain;
    public float MinBrakeTorque;

    private bool _impendingCollision = false; //This should actually be a mapping from this vehicle to every other vehicle
    private bool _brakingForRedLight = false;
    private float _distanceToCollision = 0;
	private DateTime _startTime;

	private Guid _guid;

    public List<VehicleState> StateHistory;

    public void Start()
    {
        StateHistory = new List<VehicleState>();
		_startTime = DateTime.Now;
		_guid = Guid.NewGuid();
    }

    public void WriteStateHistory()
    {
        if (StateHistory == null) return;

        if (StateHistory.Count > 0)
        {
            //Get path from configuration
            var path = GameObject.FindGameObjectWithTag("configuration").GetComponent<Configuration>().FullOutputPath();
            //Create new text file
			var filename = Path.Combine(path, _guid.ToString() + " statehistory.txt");

            var stateHistoryTextlines = StateHistory.Select(vs => vs.ToTextfileString());
            System.IO.File.WriteAllLines(filename, stateHistoryTextlines.ToArray());
        }
    }

	public void WriteIncrementalStateHistory()
	{
		//Get path from configuration
		var path = GameObject.FindGameObjectWithTag("configuration").GetComponent<Configuration>().FullOutputPath();
		//Create new text file
		var filename = Path.Combine(path, _guid.ToString() + " statehistory.txt");


		using (TextWriter tw = new StreamWriter(filename,true)) {

			if (!File.Exists(filename))
			{
				File.Create(filename);
			}

			if (StateHistory.Count () >= 1) {
				tw.WriteLine (StateHistory.Last ().ToTextfileString ());
			}
			tw.Close();
		}
	}

    public void Update()
	{   
        //Get state relative to target
		try{
		    TargetNavigation tv = GetComponent<TargetNavigation>();
            Transform target = GetComponent<TargetNavigation>().CurrentTarget;
            Rigidbody carbody = GetComponent<Rigidbody>();

            //Calculate control error
            var angle1 = Mathf.Atan2(transform.forward.z, transform.forward.x);
	        var dir = target.position - transform.position;
            var angle2 = Mathf.Atan2(dir.z, dir.x);
	        var angleDiff = angle2 - angle1;
         
            //Calculate control outputs
	        float steering =  -steeringGain * Mathf.Sin(angleDiff) * maxSteeringAngle;
	        var motor = CalculateMotorTorqueSignal(carbody);
            var brake = CalculateBrakeTorqueSignal(carbody);

            //Implement control signals
            foreach (AxleInfo axleInfo in axleInfos) {
			    if (axleInfo.steering) {
				    axleInfo.leftWheel.steerAngle = steering;
				    axleInfo.rightWheel.steerAngle = steering;
			    }
			    if (axleInfo.motor) {
				    axleInfo.leftWheel.motorTorque = motor;
				    axleInfo.rightWheel.motorTorque = motor;
                    axleInfo.leftWheel.brakeTorque = brake;
                    axleInfo.rightWheel.brakeTorque = brake;
			    }
		    }

            //Log state
            AppendState(carbody);
		    WriteIncrementalStateHistory();

		    var lifetime = DateTime.Now - _startTime;
		    if(lifetime >= TimeSpan.FromSeconds(maxLifetimeSeconds))
		    {
			    Destroy(this.gameObject);
		    }

		    }
		    catch(ArgumentOutOfRangeException ex) {
			    Console.WriteLine (ex.Message);
		    }
	}

    private float CalculateMotorTorqueSignal(Rigidbody carbody)
	{
        if (_impendingCollision || _brakingForRedLight)
        {
            if(_brakingForRedLight)
            {
                Debug.Log("Braking for red light.");
            }
			return 0;
        }
        else
        {
            return (carbody.velocity.magnitude > 5) ? 0 : maxMotorTorque * (5 - carbody.velocity.magnitude);
        }
    }

    private float CalculateBrakeTorqueSignal(Rigidbody carbody)
    {
        if (_impendingCollision || _brakingForRedLight)
        {
            float brakeTorque = brakeTorqueGain * carbody.velocity.magnitude;
            if (brakeTorque < MinBrakeTorque)
                brakeTorque = MinBrakeTorque;
            return brakeTorque;
        }
        else
        {
            return 0;
        }
    }

    private float CalculateSteeringAngleSignal(Rigidbody carbody)
    {
        if (_impendingCollision)
        {
            //TODO: Implement logic to steer away from collision point

            //Get state relative to target
            Transform target = GetComponent<TargetNavigation>().CurrentTarget;

            //Calculate control error
            var angle1 = Mathf.Atan2(transform.forward.z, transform.forward.x);
            var dir = target.position - transform.position;
            var angle2 = Mathf.Atan2(dir.z, dir.x);
            var angleDiff = angle2 - angle1;

            return -steeringGain * Mathf.Sin(angleDiff) * maxSteeringAngle;
        }
        else
        {
            //Get state relative to target
            Transform target = GetComponent<TargetNavigation>().CurrentTarget;

            //Calculate control error
            var angle1 = Mathf.Atan2(transform.forward.z, transform.forward.x);
            var dir = target.position - transform.position;
            var angle2 = Mathf.Atan2(dir.z, dir.x);
            var angleDiff = angle2 - angle1;

            return -steeringGain * Mathf.Sin(angleDiff) * maxSteeringAngle; 
        }
    }

    private void AppendState(Rigidbody carbody)
    {
        try
        {
            var screenPoint = Camera.main.WorldToScreenPoint(this.transform.position);
            var screenVelocity = Camera.main.WorldToScreenPoint(carbody.velocity);

            //First find a center for your bounds.

            Vector3 center = Vector3.zero;
            var nonrendered = 0;
            try
            {
                foreach (Transform child in transform)
                {
                    if (child.gameObject.tag != "nonrendered")
                    {
                        center +=
                            child.gameObject.GetComponent<Renderer>().GetComponent<MeshFilter>().mesh.bounds.center;
                    }
                    else
                    {
                        nonrendered++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("While adding to center");
                Debug.Log(ex.Message);
            }

            center /= (transform.childCount - nonrendered); //center is average center of children

            //Now you have a center, calculate the bounds by creating a zero sized 'Bounds', 
            Bounds bounds = new Bounds(center, Vector3.zero);

            try
            {
                foreach (Transform child in transform)
                {
                    if (child.gameObject.tag != "nonrendered")
                    {
                        bounds.Encapsulate(
                            child.gameObject.GetComponent<Renderer>().GetComponent<MeshFilter>().mesh.bounds);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("While adding to bounds");
                Debug.Log(ex.Message);
            }


			Rect boundingRect = GUIRectWithObject(carbody.gameObject);
            var state = new VehicleState();
            state.CentroidXGlobalCoordinates = this.transform.position.x;
            state.CentroidYGlobalCoordinates = this.transform.position.y;
            state.CentroidZGlobalCoordinates = this.transform.position.z;
			state.CentroidXScreenCoordinates = boundingRect.center.x;
			state.CentroidYScreenCoordinates = boundingRect.center.y;
            state.VxGlobalCoordinates = carbody.velocity.x;
            state.VyGlobalCoordinates = carbody.velocity.y;
            state.VzGlobalCoordinates = carbody.velocity.z;
            state.VxScreenCoordinates = screenVelocity.x;
            state.VyScreenCoordinates = screenVelocity.y;
			state.ObjectHeightScreenCoordinates = boundingRect.height; 
			state.ObjectWidthScreenCoordinates = boundingRect.width; 
            state.Timestamp = DateTime.Now;
            state.Frame = Time.frameCount;
            StateHistory.Add(state);
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            Debug.Log(ex.InnerException);
            Debug.Log(ex.Source);
            Debug.Log(ex.StackTrace);
        }
    }

	public Rect GUIRectWithObject(GameObject go)
	{
		var renderers = go.GetComponentsInChildren<Renderer>();
		Vector2 min = new Vector2();
		Vector2 max = new Vector2();

		bool initialized = false;

		foreach (var r in renderers) {
			Vector3 cen = r.GetComponent<Renderer>().bounds.center;
			Vector3 ext = r.GetComponent<Renderer>().bounds.extents;
			Vector2[] extentPoints = new Vector2[8]
			{
				WorldToGUIPoint(new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z-ext.z)),
				WorldToGUIPoint(new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z-ext.z)),
				WorldToGUIPoint(new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z+ext.z)),
				WorldToGUIPoint(new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z+ext.z)),
				WorldToGUIPoint(new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z-ext.z)),
				WorldToGUIPoint(new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z-ext.z)),
				WorldToGUIPoint(new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z+ext.z)),
				WorldToGUIPoint(new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z+ext.z))
			};
			if (!initialized) {
				min = extentPoints[0];
				max = extentPoints[0];
				initialized = true;
			}
			foreach (Vector2 v in extentPoints)
			{
				min = Vector2.Min(min, v);
				max = Vector2.Max(max, v);
			}
		}


		return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
	}

	public static Vector2 WorldToGUIPoint(Vector3 world)
	{
		Vector2 screenPoint = Camera.main.WorldToScreenPoint(world);
		screenPoint.y = (float) Screen.height - screenPoint.y;
		return screenPoint;
	}

    void OnTriggerEnter(Collider other)
    {
        HandleCollisions(other);
    }

    void OnTriggerStay(Collider other)
    {
        HandleCollisions(other);
    }

    void OnTriggerExit(Collider other)
    {   
        bool isVehicle = other.gameObject.CompareTag("vehicle");
        bool isIntersection = other.gameObject.CompareTag("intersectionvolume");

        if (isIntersection)
        {
            _brakingForRedLight = false;
        }
        else if (isVehicle)
        {
            _impendingCollision = false; //TODO: This is not necessarily true! Just because we're no longer colliding with some particular vehicle, doesn't mean we're not colliding with a different vehicle.
            Debug.Log("Vehicle impending collision avoided.");
        }

    }

    void HandleCollisions(Collider other)
    {
        bool isVehicle = other.gameObject.CompareTag("vehicle");
        bool isIntersection = other.gameObject.CompareTag("intersectionvolume");
        _distanceToCollision = (other.transform.position - transform.position).magnitude;

        if (isIntersection)
        {
            UpdateBrakingForIntersection(other);
        }
        else if (isVehicle)
        {
            //Check if other object is "in front" of this vehicle
            Vector3 relative = this.transform.InverseTransformDirection(other.transform.position);
            if (relative.x > 0)
            {
                _impendingCollision = true;
            }
            Debug.Log("Vehicle impending collision detected.");
        }
    }

    void UpdateBrakingForIntersection(Collider other)
    {
        try
        {
            IntersectionLightSingleton intersection = other.transform.gameObject.GetComponentInChildren<IntersectionLightSingleton>();
            TargetNavigation tv = GetComponent<TargetNavigation>();
            TurnType turnType = tv.CurrentTrajectory.Turn;
            IntersectionRoad road = tv.CurrentTrajectory.Road;
            LightPhase phase = (road == IntersectionRoad.Road1) ? intersection.phase.road1 : intersection.phase.road2;
            switch (phase)
            {
                case LightPhase.Green:
                    _brakingForRedLight = false;
                    break;
                case LightPhase.Yellow:
                    _brakingForRedLight = true;
                    break;
                case LightPhase.Red:
                    _brakingForRedLight = true;
                    break;
            }

            Debug.Log("Vehicle performing " + turnType.ToString() + " movement on " + road.ToString() + " has entered IntersectionInteriorVolume when light is " + phase.ToString());
        }
        catch (NullReferenceException ex)
        {
            Debug.Log(ex.Message);
        }

        return;
    }
}

public class VehicleState
{
    public double CentroidXGlobalCoordinates;
    public double CentroidYGlobalCoordinates;
    public double CentroidZGlobalCoordinates;
    public double VxGlobalCoordinates;
    public double VyGlobalCoordinates;
    public double VzGlobalCoordinates;
    public double CentroidXScreenCoordinates;
    public double CentroidYScreenCoordinates;
    public double VxScreenCoordinates;
    public double VyScreenCoordinates;
    public double ObjectWidthScreenCoordinates;
    public double ObjectHeightScreenCoordinates;
    public DateTime Timestamp;
    public int Frame;

    public string ToTextfileString()
    {
        string text = "";
        text += Math.Round(CentroidXGlobalCoordinates, 1) + " ";
        text += Math.Round(CentroidYGlobalCoordinates, 1) + " ";
        text += Math.Round(CentroidZGlobalCoordinates, 1) + " ";
        text += Math.Round(VxGlobalCoordinates, 1) + " ";
        text += Math.Round(VyGlobalCoordinates, 1) + " ";
        text += Math.Round(VzGlobalCoordinates, 1) + " ";
        text += Math.Round(CentroidXScreenCoordinates, 1) + " ";
        text += Math.Round(CentroidYScreenCoordinates, 1) + " ";
        text += Math.Round(VxScreenCoordinates, 1) + " ";
        text += Math.Round(VyScreenCoordinates, 1) + " ";
        text += Math.Round(ObjectWidthScreenCoordinates, 1) + " ";
        text += Math.Round(ObjectHeightScreenCoordinates, 1) + " ";
        text += Timestamp + " ";
        text += Frame + " ";
        return text;
    }
}

[System.Serializable]
public class AxleInfo {
	public WheelCollider leftWheel;
	public WheelCollider rightWheel;
	public bool motor; // is this wheel attached to motor?
	public bool steering; // does this wheel apply steer angle?
}

