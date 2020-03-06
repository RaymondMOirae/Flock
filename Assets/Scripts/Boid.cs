using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Boid : MonoBehaviour
{
    public BoidData data = new BoidData();
    public BoidsManager manager;

    void Start()
    {
        manager = GameObject.Find("Spawner").GetComponent<BoidsManager>();

    }

    void Update()
    {
        data.position = transform.position;
        data.rotation = transform.rotation.eulerAngles;
    }

}
