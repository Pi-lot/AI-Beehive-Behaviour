using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour {
    public float resource = 50;

	// Use this for initialization
	void Start () {
        resource = Random.Range(10, 100);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void DrainResource(float amount) {
        resource -= amount;
        if (resource < 0)
            resource = 0;
    }
}
