using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdVd.Graphs;

public class TestLogs : MonoBehaviour {

    Rigidbody rb;

	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
	}

    private void OnCollisionEnter(Collision collision)
    {
        Graph.AddData("Collisions", collision.relativeVelocity.magnitude);
    }

    // Update is called once per frame
    void Update () {
        Graph.AddData("Y Velocity", rb.velocity.y);
        Graph.AddData("Y Position", transform.position.y);
	}
}
