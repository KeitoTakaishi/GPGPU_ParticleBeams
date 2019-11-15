using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SparkParticle : MonoBehaviour
{
    #region instancingParameters
    ComputeBuffer argsBuffer;
    private uint[] args = new uint[5];
    [SerializeField] private Mesh srcMesh;
    [SerializeField] Material instancingMat;
    [SerializeField] int instancingCount;
    #endregion


    #region privatData
    [SerializeField] ComputeShader cs;

    [SerializeField] BakeBeamTop bakeBeamTop;
    private int kernelID;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer lifeBuffer;
    private ComputeBuffer velocityBuffer;
    private ComputeBuffer accelerateBuffer;
    #endregion



    private void Awake()
    {
        initInstancingParams();
        initBuffers();
        kernelID = cs.FindKernel("spark");
    }


    void Start()
    {
        
    }

    void Update()
    {

    }

    private void LateUpdate()
    {
        cs.SetFloat("dt", Time.deltaTime);
        cs.SetFloat("time", Time.realtimeSinceStartup);
        cs.SetBool("isCollision", bakeBeamTop.IsCollision);
        var cp = bakeBeamTop.CollisionPos;
        cs.SetVector("collisionPos", new Vector4(cp.x, cp.y, cp.z, 0));
        
        cs.SetBuffer(kernelID, "positionBuffer", positionBuffer);
        cs.SetBuffer(kernelID, "velocityBuffer", velocityBuffer);
        cs.SetBuffer(kernelID, "accelerateBuffer", accelerateBuffer);
        cs.SetBuffer(kernelID, "lifeBuffer", lifeBuffer);
        uint threadX, threadY, threadZ;
        cs.GetKernelThreadGroupSizes(kernelID, out threadX, out threadY, out threadZ);
        cs.Dispatch(kernelID, (int)((instancingCount) / threadX), (int)threadY, (int)threadZ);


        instancingMat.SetBuffer("positionBuffer", positionBuffer); 
        instancingMat.SetBuffer("velocityBuffer", velocityBuffer);

        instancingMat.SetBuffer("lifeBuffer", lifeBuffer);
        Graphics.DrawMeshInstancedIndirect(srcMesh, 0, instancingMat,
        new Bounds(Vector3.zero, Vector3.one * 100.0f), argsBuffer);
    }

    void initInstancingParams()
    {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        args[0] = srcMesh.GetIndexCount(0);
        args[1] = (uint)instancingCount;
        args[2] = srcMesh.GetIndexStart(0);
        args[3] = srcMesh.GetBaseVertex(0);
        args[4] = 0;
        argsBuffer.SetData(args);
    }

    void initBuffers()
    {
        positionBuffer = new ComputeBuffer(instancingCount, sizeof(float) * 3);
        velocityBuffer = new ComputeBuffer(instancingCount, sizeof(float) * 3);
        accelerateBuffer = new ComputeBuffer(instancingCount, sizeof(float) * 3);
        lifeBuffer = new ComputeBuffer(instancingCount, sizeof(float) * 2);

        var fdir = new List<Vector3>();
        var lifeList = new List<Vector2>();
        for(int i = 0; i < instancingCount; i++)
        {
            fdir.Add(Random.insideUnitSphere);
            lifeList.Add(new Vector2(Random.Range(0.5f, 1.0f), 0.0f));
        }

        lifeBuffer.SetData(lifeList);
        accelerateBuffer.SetData(fdir);
       
    }

    private void OnDisable()
    {
        positionBuffer.Release();
        lifeBuffer.Release();
        velocityBuffer.Release();
        accelerateBuffer.Release();
        argsBuffer.Release();
    }
}
