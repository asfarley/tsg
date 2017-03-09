using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnVehicles : MonoBehaviour
{

    // Probability of spawning a vehicle per frame
    public float Probability; 

	// Use this for initialization
	void Start () {
    }
	
	// Update is called once per frame
	void Update ()
	{
        var diceRoll = Random.Range((float)0.0, (float)1.0);
        foreach (var vehicle in GameObject.FindGameObjectsWithTag("vehicle"))
        {
            var vehiclePosition = vehicle.transform.position;
            var totalDistance = Vector3.Distance(vehiclePosition, transform.position);
            if (totalDistance < 5) //If there's a vehicle within 5 units of this spawn point, don't spawn
            {
                return;
            }
        }

        if (diceRoll < Probability)
	    {
            var newcar = Instantiate(GameObject.Find("CarPrototype"), transform.position, transform.rotation);
            newcar.name = "car";
            newcar.tag = "vehicle";
            var navigation = newcar.GetComponentInChildren<TargetNavigation>();

            for (var i = 0; i < this.transform.root.childCount; i++) //Add exit trajectories under this approach
            {
                if (this.transform.root.GetChild(i).CompareTag("trajectory"))
                {
                    var t = new Trajectory();
                    var child = this.transform.root.GetChild(i);
                    for (var j = 0; j < child.transform.childCount; j++)
                    {
                        var tf = child.GetChild(j);
                        t.Add(tf);
                    }

                    navigation.Exits.Add(t);
                }
            }

            navigation.RandomizeTarget();
            newcar.GetComponentInChildren<CarMovement>().enabled = true;
            newcar.GetComponentInChildren<TargetNavigation>().enabled = true;
        }

    }
}
