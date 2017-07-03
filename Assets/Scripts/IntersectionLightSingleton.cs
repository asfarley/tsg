using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum LightPhase
{
    Red,
    Green,
    Yellow
}

public struct IntersectionPhase
{
    public LightPhase road1;
    public LightPhase road2;
}

public class IntersectionLightSingleton : MonoBehaviour {

    public float GreenPhaseSeconds;
    public float YellowPhaseSeconds;
    public float RedPhaseSeconds;

    public float RoadTimingOffsetSeconds;

    public IntersectionPhase phase;

    // Use this for initialization
    void Start () {
        phase.road1 = LightPhase.Red;
        phase.road2 = LightPhase.Red;

        Road1LightPhaseTransition();
        Invoke("Road2LightPhaseTransition", RoadTimingOffsetSeconds);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void Road1LightPhaseTransition()
    {
        switch (phase.road1)
        {
            case LightPhase.Red:
                phase.road1 = LightPhase.Green;
                Invoke("Road1LightPhaseTransition",  GreenPhaseSeconds);
                Debug.Log("Road 1 changed to Green.");
                break;
            case LightPhase.Yellow:
                phase.road1 = LightPhase.Red;
                Invoke("Road1LightPhaseTransition", RedPhaseSeconds);
                Debug.Log("Road 1 changed to Red.");
                break;
            case LightPhase.Green:
                phase.road1 = LightPhase.Yellow;
                Invoke("Road1LightPhaseTransition", YellowPhaseSeconds);
                Debug.Log("Road 1 changed to Yellow.");
                break;
        }
    }

    void Road2LightPhaseTransition()
    {
        switch (phase.road2)
        {
            case LightPhase.Red:
                phase.road2 = LightPhase.Green;
                Invoke("Road2LightPhaseTransition", GreenPhaseSeconds);
                Debug.Log("Road 2 changed to Green.");
                break;
            case LightPhase.Yellow:
                phase.road2 = LightPhase.Red;
                Invoke("Road2LightPhaseTransition", RedPhaseSeconds);
                Debug.Log("Road 2 changed to Red.");
                break;
            case LightPhase.Green:
                phase.road2 = LightPhase.Yellow;
                Invoke("Road2LightPhaseTransition", YellowPhaseSeconds);
                Debug.Log("Road 2 changed to Yellow.");
                break;
        }
    }
}
