/*
beamの軌跡上にParticleをinstancingする
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEmitter : MonoBehaviour
{

    #region instancingParams
    ComputeBuffer argsBuffer;
    private uint[] args = new uint[5];
    private int instancingCount;
    [SerializeField] private Mesh srcMesh;
    [SerializeField] Material instancingMat;
    #endregion

    #region ComputeShaderParams
    [SerializeField] RenderTexture emitPositionBuffer;//particleの軌跡を表している
    [SerializeField] int particleNumPerEmitter;
    [SerializeField] BakeBeamTop bakeBeamTop;
    #endregion


    private void Awake()
    {
        initInstancingParams();

        if (bakeBeamTop.isUseComputeBuffer)
        {
            Shader.DisableKeyword("_BUFTYPE_RT");
            Shader.EnableKeyword("_BUFTYPE_CB");
        }
        else
        {
            Shader.DisableKeyword("_BUFTYPE_CB");
            Shader.EnableKeyword("_BUFTYPE_RT");
        }
    }
    void Start()
    {
        instancingMat.SetInt("InstanceNum", emitPositionBuffer.width * emitPositionBuffer.height);
    }

    void Update()
    {

    }

    private void LateUpdate()
    {
        //instancingMat.SetInt
        instancingMat.SetInt("particleNumPerEmitter", particleNumPerEmitter);
        instancingMat.SetVector("textureSize", new Vector4(emitPositionBuffer.width, emitPositionBuffer.height, 1, 0));
        if (bakeBeamTop.isUseComputeBuffer)
        {
            instancingMat.SetBuffer("beamTopBuffer", bakeBeamTop.BeamTopBuffer);
        }
        else
        {
            instancingMat.SetTexture("positionRenderTexture", emitPositionBuffer);
        }

        instancingMat.SetBuffer("lifeBuffer", bakeBeamTop.LifeBuffer);
        Graphics.DrawMeshInstancedIndirect(srcMesh, 0, instancingMat,
        new Bounds(Vector3.zero, Vector3.one * 100.0f), argsBuffer);
    }

    void initInstancingParams()
    {
        instancingCount = emitPositionBuffer.width * emitPositionBuffer.height * particleNumPerEmitter;
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        args[0] = srcMesh.GetIndexCount(0);
        args[1] = (uint)instancingCount;
        args[2] = srcMesh.GetIndexStart(0);
        args[3] = srcMesh.GetBaseVertex(0);
        args[4] = 0;
        argsBuffer.SetData(args);
    }

    private void OnDisable()
    {
        argsBuffer.Release();
    }
}
