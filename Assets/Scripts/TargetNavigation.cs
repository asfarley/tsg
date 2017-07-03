using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

public enum TurnType
{
    Left,
    Right,
    Through
};

public enum IntersectionRoad
{
    Road1,
    Road2
};

public class TargetNavigation : MonoBehaviour
{
    private int _targetIndex;
    private int _trajectoryIndex;

	public double ThresholdWaypointDistance = 8.0;

    public Trajectory CurrentTrajectory
    {
        get { return Exits[_trajectoryIndex]; }
    }

    public Transform CurrentTarget
    {
        get { 
			return Exits[_trajectoryIndex][_targetIndex];  
		}
    }

    public List<Trajectory> Exits = new List<Trajectory>();

    // Use this for initialization
    void Start ()
    {
    }
	
	// Update is called once per frame
	void Update ()
	{
        var distance = AbsoluteDistanceToTarget();
		if (distance < ThresholdWaypointDistance)
	    {
	        if (_targetIndex >= Exits[_trajectoryIndex].Count - 1) //If this is the last target in the list
	        {
                //this.GetComponent<CarMovement>().WriteStateHistory();
                Destroy(this.gameObject);
	        }
            else
	        {
	            _targetIndex++;
	        }
        }
	}

    public void RandomizeTarget()
    {
        _trajectoryIndex = Random.Range(0, Exits.Count);
        _targetIndex = 0;
    }

    private float AbsoluteDistanceToTarget()
    {
        return Vector3.Distance(transform.position, CurrentTarget.position);
    }
}

public class Trajectory : List<Transform>
{
    public TurnType Turn;
    public IntersectionRoad Road;
}

