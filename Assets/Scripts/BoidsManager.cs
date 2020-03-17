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
    public Matrix4x4 TRSMatrix;

    public static int Size
    {
        get
        {
            return sizeof(float) * (3 * 3 + 4 * 6 );
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

    public Vector3 identitySscale = new Vector3(0.3f, 0.2f, 0.9f);

    BoidData[] boidDataArray;

    [Header("Room Attributes")]
    public int roomSize = 40;
    private Vector3 roomOffset;

    [Header("Shader&Buffer")]
    public ComputeShader compute;
    public ComputeBuffer boidBuffer;
    public ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0 , 0};
    private int kernelIndex;


    private void Start()
    {
        boidDataArray = new BoidData[totalNum];
        roomOffset = new Vector3(-roomSize / 2, 0, -roomSize / 2);
        SpawnBoidData();
        InitializeComputeBuffer();
    }
 private void InitializeComputeBuffer()
    {
        if (boidMesh != null)
        {
            args[0] = (uint)boidMesh.GetIndexCount(0);
            args[1] = (uint)totalNum;
            args[2] = (uint)boidMesh.GetIndexStart(0);
            args[3] = (uint)boidMesh.GetBaseVertex(0);
        }
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        boidBuffer = new ComputeBuffer(totalNum, BoidData.Size);

        boidMat.SetBuffer("boidsBuffer", boidBuffer);
        boidMat.SetVector("scale", identitySscale);

        kernelIndex = compute.FindKernel("CSMain");
        compute.SetBuffer(kernelIndex, "boidBuffer", boidBuffer);
        compute.SetFloat("senseRad", senseRad);
        compute.SetInt("totalNum", totalNum);
    }

    private void SpawnBoidData()
    {
        for(int i = 0; i <totalNum; i++)
        {
            BoidData newBoid = new BoidData();
            newBoid.position =GenRandomPos() + roomOffset;
            newBoid.rotation = GenRandomQuad();
            boidDataArray[i] = newBoid;
            newBoid.TRSMatrix = Matrix4x4.TRS(newBoid.position, newBoid.rotation, identitySscale);
        }
    }

    private void LateUpdate()
    {
        if (boidDataArray == null)
        {
            return;
        }

        boidBuffer.SetData(boidDataArray);
        compute.Dispatch(kernelIndex, totalNum / 64, 1, 1);
        boidBuffer.GetData(boidDataArray);

        // put result back to every boid
        for (int i = 0; i < totalNum; i++)
        {
            UpdateIdentity(i);
        }

        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * roomSize);
        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMat, bounds, argsBuffer);
        //Graphics.DrawMeshInstancedProcedural(boidMesh, 0, boidMat, bounds, totalNum);

    }


    public void UpdateIdentity(int i)
    {
        BoidData data = boidDataArray[i];

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

        data.TRSMatrix = Matrix4x4.TRS(data.position, data.rotation, identitySscale);

        boidDataArray[i] = data;

    }

    private void OnDisable()
    {
        boidBuffer.Release();
        argsBuffer.Release();
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
        return Quaternion.Euler(new Vector3(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180)));
    }
    void DebugPrint()
    {
        int t = 0;
        foreach(BoidData boid in boidDataArray)
        {
            t++;
            Debug.Log(t);
            Debug.Log("cohesion:" + boid.flockCentre.ToString());
            Debug.Log("alignment:" + boid.rotation.ToString());
            Debug.Log("position:" + boid.position.ToString());
            Debug.Log("separation:" + boid.separationHeading.ToString());
        }
    }
}
