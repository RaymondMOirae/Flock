using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Random = UnityEngine.Random;

public struct BoidData
{
    public Vector3 position;
    public Quaternion rotation;

    public Quaternion flockRot;
    public Vector3 separationHeading;
    public Vector3 flockCentre;

    public static int Size
    {
        get
        {
            return sizeof(float) * (3 * 5 + 2);
        }
    }

    public Matrix4x4 TRSMatrix {
        get
        {
            return Matrix4x4.TRS(position, rotation,  new Vector3(0.3f, 0.2f, 0.9f));
        }
    }
}


public class BoidsManager : MonoBehaviour
{
    [Header("Boid Attributes")]
    public int totalNum = 20;
    public float boidSpeed = 1;
    public float senseRad = 10;

    public Mesh boidMesh;
    public Material boidMat;
    [Range(0.0f, 1.0f)]
    public float lerpInterpolant;
    public float seperationWeight;
    public float cohesionWeight;
    public float marchingWeight;
    


    BoidData[] boidDataBuffer;


    [Header("Room Attributes")]
    public int roomSize = 40;
    private Vector3 roomOffset;


    public ComputeShader compute;


    private void Start()
    {
        boidDataBuffer= new BoidData[totalNum];
        roomOffset = new Vector3(-roomSize / 2, 0, -roomSize / 2);
        SpawnBoidData();
    }

    private void SpawnBoidData()
    {
        for(int i = 0; i <totalNum; i++)
        {
            BoidData newBoid = new BoidData();
            newBoid.position =GenRandomPos() + roomOffset;
            newBoid.rotation = GenRandomQuad();
            boidDataBuffer[i] = newBoid;
        }
    }


    private void Update()
    {
        if (boidDataBuffer == null)
        {
            return;
        }

        var boidBuffer = new ComputeBuffer(totalNum, BoidData.Size);
        boidBuffer.SetData(boidDataBuffer);

        int kernelIndex = compute.FindKernel("CSMain");
        compute.SetBuffer(kernelIndex, "boidBuffer", boidBuffer);
        compute.SetFloat("senseRad", senseRad);
        compute.SetInt("totalNum", totalNum);

        compute.Dispatch(kernelIndex, totalNum, 1, 1);
        boidBuffer.GetData(boidDataBuffer);



        // put result back to every boid
        for (int i = 0; i < totalNum; i++)
        {
            UpdateIdentity(i);
        }

        boidBuffer.Release();

        Graphics.DrawMeshInstanced(boidMesh, 0, boidMat, boidDataBuffer.Select((a) => a.TRSMatrix).ToList());

    }
    void DebugPrint()
    {
        int t = 0;
        foreach(BoidData boid in boidDataBuffer)
        {
            t++;
            Debug.Log(t);
            Debug.Log("cohesion:" + boid.flockCentre.ToString());
            Debug.Log("alignment:" + boid.rotation.ToString());
            Debug.Log("position:" + boid.position.ToString());
            Debug.Log("separation:" + boid.separationHeading.ToString());
        }
    }

    public void UpdateIdentity(int i)
    {
        BoidData data = boidDataBuffer[i];

        Vector3 acceleration = Vector3.zero;
        Vector3 cohesion = data.flockCentre - data.position;
        Vector3 forward = data.rotation * Vector3.forward;
        acceleration = data.separationHeading.normalized * seperationWeight
                       + cohesion.normalized * cohesionWeight
                       + forward * marchingWeight;
        Vector3 velocity = acceleration * Time.deltaTime * boidSpeed;
        Vector3.ClampMagnitude(velocity, boidSpeed);
        data.rotation = Quaternion.Lerp(data.rotation, data.flockRot, lerpInterpolant);
        data.position += velocity;


        if(Mathf.Abs(data.position.x) >= roomSize / 2||
            Mathf.Abs(data.position.y) >= roomSize||
            Mathf.Abs(data.position.z) >= roomSize / 2||
            data.position.y <= 0){
            Vector3 tempPos = data.position - roomOffset;
            Func<Vector3, Vector3> normPosFunc = GetNormFunc((float a) => { return Mathf.Repeat(a, roomSize); });
            tempPos = normPosFunc(tempPos);
            data.position = tempPos + roomOffset;
        }


        boidDataBuffer[i] = data;

    }

    Func<Vector3, Vector3> GetNormFunc(Func<float, float> metaFunc)
    {
        return (Vector3 rawPos) =>
        {
            return new Vector3(metaFunc(rawPos.x), metaFunc(rawPos.y), metaFunc(rawPos.z));
        };
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
