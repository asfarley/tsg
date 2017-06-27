﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public class CarMovement : MonoBehaviour {
	public List<AxleInfo> axleInfos; // the information about each individual axle
	public float maxMotorTorque; // maximum torque the motor can apply to wheel
	public float maxSteeringAngle; // maximum steer angle the wheel can have
    public float steeringGain; // closed-loop heading gain

    private bool _impendingCollision = false;
    private float _distanceToCollision = 0;

    public List<VehicleState> StateHistory;

    public void Start()
    {
        StateHistory = new List<VehicleState>();
    }

    public void WriteStateHistory()
    {
        if (StateHistory == null) return;

        if (StateHistory.Count > 0)
        {
            //Get path from configuration
            var path = GameObject.FindGameObjectWithTag("configuration").GetComponent<Configuration>().FullOutputPath();
            //Create new text file
            var filename = path + @"\" + Guid.NewGuid().ToString() + " statehistory.txt";

            var stateHistoryTextlines = StateHistory.Select(vs => vs.ToTextfileString());
            System.IO.File.WriteAllLines(filename, stateHistoryTextlines.ToArray());
        }
    }

    public void Update()
	{   
        //Get state relative to target
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

        //Implement control signals
        foreach (AxleInfo axleInfo in axleInfos) {
			if (axleInfo.steering) {
				axleInfo.leftWheel.steerAngle = steering;
				axleInfo.rightWheel.steerAngle = steering;
			}
			if (axleInfo.motor) {
				axleInfo.leftWheel.motorTorque = motor;
				axleInfo.rightWheel.motorTorque = motor;
			}
		}

        //Log state
        AppendState(carbody);
	}

    private float CalculateMotorTorqueSignal(Rigidbody carbody)
    {
        if (_impendingCollision)
        {
            return -5*maxMotorTorque/_distanceToCollision;
        }
        else
        {
            return (carbody.velocity.magnitude > 5) ? 0 : maxMotorTorque * (5 - carbody.velocity.magnitude);
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

            Bounds screenBounds = new Bounds();
            screenBounds.center = Camera.main.WorldToScreenPoint(bounds.center);
            screenBounds.max = Camera.main.WorldToScreenPoint(bounds.max);
            screenBounds.min = Camera.main.WorldToScreenPoint(bounds.min);
            var state = new VehicleState();
            state.CentroidXGlobalCoordinates = this.transform.position.x;
            state.CentroidYGlobalCoordinates = this.transform.position.y;
            state.CentroidZGlobalCoordinates = this.transform.position.z;
            state.CentroidXScreenCoordinates = screenPoint.x;
            state.CentroidYScreenCoordinates = screenPoint.y;
            state.VxGlobalCoordinates = carbody.velocity.x;
            state.VyGlobalCoordinates = carbody.velocity.y;
            state.VzGlobalCoordinates = carbody.velocity.z;
            state.VxScreenCoordinates = screenVelocity.x;
            state.VyScreenCoordinates = screenVelocity.y;
            state.ObjectHeightScreenCoordinates = GUIRectWithObject(carbody.gameObject).height;
            state.ObjectWidthScreenCoordinates = GUIRectWithObject(carbody.gameObject).width;
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

    public static Rect GUIRectWithObject(GameObject go)
    {

        Vector2 min = new Vector2();
        Vector2 max = new Vector2();

        bool initialized = false;

        try
        {

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                Vector3 cen = r.bounds.center;
                Vector3 ext = r.bounds.extents;
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

                if (!initialized)
                {
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
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }


    public static Vector2 WorldToGUIPoint(Vector3 world)
    {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(world);
        screenPoint.y = (float)Screen.height - screenPoint.y;
        return screenPoint;
    }

    void OnTriggerEnter(Collider other)
    {
        _impendingCollision = true;
        _distanceToCollision = (other.transform.position - transform.position).magnitude;
    }

    void OnTriggerStay(Collider other)
    {
        _distanceToCollision = (other.transform.position - transform.position).magnitude;
    }

    void OnTriggerExit(Collider other)
    {
        _impendingCollision = false;
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

