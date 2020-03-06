using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct GPUBoid
{
    public Vector3 pos, rot, flockPos;
    public float speed, nearbyDis, boidsCount;
    public static int Size{
        get {
            return sizeof(float) * 3 * 3 + sizeof(float) * 3;
        }
    }
}


        public class GPUFlock : MonoBehaviour {
    #region 字段
    public ComputeShader cshader;
    public GameObject boidPrefab;
    public int boidsCount;
    public float spawnRadius;
    public GameObject[] boidsGo;
    public GPUBoid[] boidsData;
    public float flockSpeed;
    public float nearbyDis;
    private Vector3 targetPos = Vector3.zero;
    private int kernelHandle;
    #endregion
    #region 方法
    void Start()
    {
        this.boidsGo = new GameObject[this.boidsCount];
        this.boidsData = new GPUBoid[this.boidsCount];
        this.kernelHandle = cshader.FindKernel("CSMain");
        for (int i = 0; i < this.boidsCount; i++)
        {
            this.boidsData[i] = this.CreateBoidData();
            this.boidsGo[i] = Instantiate(boidPrefab, this.boidsData[i].pos, Quaternion.Euler(this.boidsData[i].rot)) as GameObject;
            this.boidsData[i].rot = this.boidsGo[i].transform.forward;
        }
    }
    GPUBoid CreateBoidData()
    {
        GPUBoid boidData = new GPUBoid();
        Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
        Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
        boidData.pos = pos;
        boidData.flockPos = transform.position;
        boidData.boidsCount = this.boidsCount;
        boidData.nearbyDis = this.nearbyDis;
        boidData.speed = this.flockSpeed + Random.Range(-0.5f, 0.5f);
        return boidData;
    }
    void Update()
    {
        this.targetPos += new Vector3(2f, 5f, 3f);
        this.transform.localPosition += new Vector3(
            (Mathf.Sin(Mathf.Deg2Rad * this.targetPos.x) * -0.2f),
            (Mathf.Sin(Mathf.Deg2Rad * this.targetPos.y) * 0.2f),
            (Mathf.Sin(Mathf.Deg2Rad * this.targetPos.z) * 0.2f)
        );
        ComputeBuffer buffer = new ComputeBuffer(boidsCount, GPUBoid.Size);
        for (int i = 0; i < this.boidsData.Length; i++)
        {
            this.boidsData[i].flockPos = this.transform.position;
        }
        //注意：和一般的Shader不同的是，compute shader和图形无关，因此在使用compute shader时不会
        //涉及到mesh、material这些内容，相反这些compute shader的设置和执行在C#脚本中，如下：
        //准备数据
        buffer.SetData(this.boidsData);
        //传递数据
        cshader.SetBuffer(this.kernelHandle, "boidBuffer", buffer);
        cshader.SetFloat("deltaTime", Time.deltaTime);
        //分配线程组执行compute shader
        cshader.Dispatch(this.kernelHandle, this.boidsCount, 1, 1);
        //将数据从GPU传回到CPU中，注意数据的传输需要等待，这里比较耗时
        buffer.GetData(this.boidsData);
        buffer.Release();
        for (int i = 0; i < this.boidsData.Length; i++)
        {
            this.boidsGo[i].transform.localPosition = this.boidsData[i].pos;
            if (!this.boidsData[i].rot.Equals(Vector3.zero))
            {
                this.boidsGo[i].transform.rotation = Quaternion.LookRotation(this.boidsData[i].rot);
            }
        }
    }
    #endregion
}
