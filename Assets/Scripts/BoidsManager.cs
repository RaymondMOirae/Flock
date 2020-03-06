using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BoidData
{
    public Vector3 position;
    public Vector3 flockPos;
    public Vector3 rotation;    

    public static int Size
    {
        get
        {
            return sizeof(float) * 3 * 3;
        }
    }
}

public class BoidsManager : MonoBehaviour
{
    [Header("Boid Attributes")]
    public int totalNum = 20;
    public float boidSpeed = 1;
    public float senseRad = 10;
    public Vector3 targetPos = Vector3.zero;
    public Boid[] boids;
    public Boid boidPrefab;

    [Header("Room Attributes")]
    public int roomSize = 40;
    public Vector3 roomOffset = new Vector3(-20, 0, -20);

    public ComputeShader compute;


    private void Start()
    {
        SpawnBoids();
    }

    private void SpawnBoids()
    {

        boids = new Boid[totalNum];

        for(int i = 0; i <totalNum; i++)
        {
            Boid newBoid = GameObject.Instantiate<Boid>(boidPrefab);
            Transform transform = newBoid.GetComponent<Transform>();
            Vector3 pos = GenRandomPos() + roomOffset;
            Quaternion rot = GenRandomQuad();
            transform.SetPositionAndRotation(pos, rot);
            newBoid.data.position = pos;
            newBoid.data.rotation = rot.eulerAngles;
            boids[i] = newBoid;

        }
    }

    private void Update()
    {
        ManagerUpdate();
    }


    public void ManagerUpdate()
    {
        if(boids == null)
        {
            return;
        }

        targetPos += new Vector3(2f, 5f, 3f);
        transform.localPosition += new Vector3(
                         (Mathf.Sin(Mathf.Deg2Rad * this.targetPos.x) * -0.2f),
                         (Mathf.Sin(Mathf.Deg2Rad * this.targetPos.y) * 0.2f),
                         (Mathf.Sin(Mathf.Deg2Rad * this.targetPos.z) * 0.2f)
                     );

        var boidData = new BoidData[totalNum];
        for(int i = 0; i < boids.Length; i++)
        {
            boidData[i] = boids[i].data;
        }

        var boidBuffer = new ComputeBuffer(totalNum, BoidData.Size);
        boidBuffer.SetData(boidData);
        int kernelIndex = compute.FindKernel("CSMain");
        compute.SetBuffer(kernelIndex, "output", boidBuffer);
        compute.SetFloat("senseRad", senseRad);
        compute.SetInt("totalNum", totalNum);
        compute.SetFloat("deltaTime", Time.deltaTime);
        compute.SetFloat("boidSpeed", boidSpeed);

        compute.Dispatch(kernelIndex, totalNum, 1, 1);
        boidBuffer.GetData(boidData);
        boidBuffer.Release();

        for(int i = 0; i < totalNum; i++)
        {
            boids[i].transform.localPosition= boidData[i].position;
            if (!boidData[i].rotation.Equals(Vector3.zero))
            {
                boids[i].transform.rotation = Quaternion.LookRotation(boidData[i].rotation);
            }
        }    
        
    }


    Vector3 GenRandomPos()
    {
        return new Vector3(Random.Range(0, roomSize), Random.Range(0, roomSize), Random.Range(0, roomSize));
    }

    Quaternion GenRandomQuad()
    {
        return Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
    }
}
