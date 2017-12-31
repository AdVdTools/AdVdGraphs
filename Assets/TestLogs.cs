using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLogs : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        AdVd.Graphs.Graph.AddData("New Graph", transform.position.x);
        AdVd.Graphs.Graph.AddData("New Graph 1", transform.position.y);
	}
}
