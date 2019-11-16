using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BakeBeamTop : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] float curvatureRadius;
    [SerializeField] float damping;
    [SerializeField] private Vector3 vel;

    [SerializeField] GameObject target;
    float speed = 3.0f;
    float deltaTime;
    float propulsion;
    private Vector3 position;
    private Vector3 toTarget;
    private Vector3 vn;
    private Vector3 centripetal;
    private float theta;
    private float maxCentripetalAccel;


    #region buffer
    [SerializeField] RenderTexture srcBeamTopRenderBuffer;//read Only
    [SerializeField] RenderTexture dstBeamTopRenderBuffer;//write Only
    [SerializeField] ComputeShader cs_cb;
    [SerializeField] ComputeShader cs_rt;
    ComputeBuffer beamtopBuffer;
    ComputeBuffer lifeBuffer;
    int kernelId;
    int topIndex = 0;

    public ComputeBuffer BeamTopBuffer
    {
        get
        {
            return beamtopBuffer;
        }
    }



    public ComputeBuffer LifeBuffer
    {
        get
        {
            return this.lifeBuffer;
        }
    }
    #endregion

    public bool isUseComputeBuffer = true;

    #region private
    private bool isCollision = false;
    private Vector3 coliisionPos;
    [SerializeField] private Text fpsText;
    [SerializeField] private Text bufferTypeText;
    private float fps;
    private int meanNum = 60;
    private List<float> ms;
    #endregion


    #region accessor
    public bool IsCollision
    {
        get { return this.isCollision; }
    }

    public Vector3 CollisionPos
    {
        get { return this.coliisionPos; }
    }
    #endregion

    private void Awake()
    {
            dstBeamTopRenderBuffer.Release();
            dstBeamTopRenderBuffer.enableRandomWrite = true;
            dstBeamTopRenderBuffer.Create();
            coliisionPos = Vector3.zero;
            ms = new List<float>();
            for(int i = 0; i < meanNum; i++)
            {
                ms.Add(0.0f);
            }
    }


    void Start()
    {

        //target = GameObject.Find("Target");
        this.position = position;
        this.maxCentripetalAccel = speed * speed / curvatureRadius;
        this.damping = damping;
        this.propulsion = speed * damping;
        initComBuf();

        if (isUseComputeBuffer)
        {
            bufferTypeText.text = "BufferType : ComputeBuffer";
        }
        else
        {
            bufferTypeText.text = "BufferType : RenderTexture";
        }
    }


    int i = 0;
    void Update()
    {
        fps += Time.deltaTime;
        i = i % meanNum;
        ms[i] = Time.deltaTime;
        if (Time.frameCount % meanNum == 0)
        {
            fps /= meanNum;
            fpsText.text = "Avg : " + fps.ToString("f3") + ", Min : " + ms.Min().ToString("f3");
            fps = 0;
        }
        i++;
    }

    private void LateUpdate()
    {
        TargetMode();
        if((this.transform.position - target.transform.position).magnitude < 0.75f){
            isCollision = true;
            coliisionPos = target.transform.position;
        }
        else
        {
            isCollision = false;
        }

        if (Input.GetKeyDown(KeyCode.A) || isCollision)
        {
            ResetParames();
        }

        position = this.transform.position;
        var toTarget = target.transform.position - position;
        var vn = vel.normalized;
        var dot = Vector3.Dot(toTarget, vn);
        var centripetalAccel = toTarget - (vn * dot);
        var centripetalAccelMagnitude = centripetalAccel.magnitude;
        if (centripetalAccelMagnitude > 1f)
        {
            centripetalAccel /= centripetalAccelMagnitude;
        }
        var force = centripetalAccel * maxCentripetalAccel;
        force += vn * propulsion;
        force -= vel * damping;
        vel += force * Time.deltaTime;
        position += vel * Time.deltaTime;

        this.transform.position = position;

        //var _ = this.transform.position;

       
      
        if (isUseComputeBuffer)
        {
            cs_cb.SetInt("topIndex", topIndex);
            cs_cb.SetFloat("dt", Time.deltaTime);
            cs_cb.SetVector("topPos", new Vector4(position.x, position.y, position.z, 1.0f));
            cs_cb.SetVector("targetPos", new Vector4(target.transform.position.x, target.transform.position.y, target.transform.position.z, 1.0f));
            cs_cb.SetBuffer(kernelId, "beamTopBuffer", beamtopBuffer);
            cs_cb.SetBuffer(kernelId, "lifeBuffer", lifeBuffer);

            uint thread_x, thread_y, thread_z;
            cs_cb.GetKernelThreadGroupSizes(kernelId, out thread_x, out thread_y, out thread_z);
            cs_cb.Dispatch(kernelId, (int)(dstBeamTopRenderBuffer.width / thread_x), (int)(dstBeamTopRenderBuffer.height / thread_y), 1);
        }
        else
        {
            cs_rt.SetInt("topIndex", topIndex);
            cs_rt.SetFloat("dt", Time.deltaTime);
            cs_rt.SetVector("topPos", new Vector4(position.x, position.y, position.z, 1.0f));
            cs_rt.SetVector("targetPos", new Vector4(target.transform.position.x, target.transform.position.y, target.transform.position.z, 1.0f));
            cs_rt.SetBuffer(kernelId, "lifeBuffer", lifeBuffer);
            cs_rt.SetTexture(kernelId, "srcBeamTopRenderBuffer", srcBeamTopRenderBuffer);
            cs_rt.SetTexture(kernelId, "dstBeamTopRenderBuffer", dstBeamTopRenderBuffer);
            uint thread_x, thread_y, thread_z;
            cs_rt.GetKernelThreadGroupSizes(kernelId, out thread_x, out thread_y, out thread_z);
            cs_rt.Dispatch(kernelId, (int)(dstBeamTopRenderBuffer.width / thread_x), (int)(dstBeamTopRenderBuffer.height / thread_y), 1);
        }
       

        if (!isUseComputeBuffer)
        {
            Graphics.CopyTexture(dstBeamTopRenderBuffer, srcBeamTopRenderBuffer);
        }
        if (Time.frameCount % 2 == 0)
        {
            //topIndex = (topIndex + 1) % (dstBeamTopRenderBuffer.width * dstBeamTopRenderBuffer.height + 1);
        }

        topIndex = (topIndex + 1) % (dstBeamTopRenderBuffer.width * dstBeamTopRenderBuffer.height + 1);


    }

    public void ResetParames()
    {
        this.transform.position = position = new Vector3(0, 0, 0);

        Vector3 rnd = Random.insideUnitSphere;
        while( (Mathf.Abs(rnd.x) < 0.3f) || (Mathf.Abs(rnd.y) < 0.3f))
        {
            rnd = Random.insideUnitSphere;
        }

        this.vel = vel = new Vector3(rnd.x, rnd.y, Mathf.Abs(rnd.z)) * 10.0f;

        //var d = target.transform.position - this.transform.position;
        //d = d.normalized;
        //this.vel = vel = new Vector3(-1 * d.x, -1 * d.y, Mathf.Abs(rnd.z)) * 6.5f;
    }


    void initComBuf()
    {
        kernelId = cs_cb.FindKernel("ArchiveTop");
        var bufferSize = dstBeamTopRenderBuffer.width * dstBeamTopRenderBuffer.height;
        lifeBuffer = new ComputeBuffer(bufferSize, sizeof(float) * 2);
        beamtopBuffer = new ComputeBuffer(bufferSize, sizeof(float) * 3);
        var lifeList = new List<Vector2>();
        var beamTopList = new List<Vector3>();

        for(int i = 0; i < bufferSize; i++)
        {
            lifeList.Add(new Vector2( Random.RandomRange(1.0f, 3.0f), 0.0f));
            if (isUseComputeBuffer)
            {
                beamTopList.Add(Vector3.zero);
            }
        }
        lifeBuffer.SetData(lifeList);
        if (isUseComputeBuffer)
        {
            beamtopBuffer.SetData(beamTopList);
        }

    }

    static int count = 0;
    int span = 100;
    //float int curT;
    Vector3 next;
    Vector3 dir;
    private void TargetMode()
    {
        if (count % span == 0)
        {
            next = Random.insideUnitSphere *10.0f + new Vector3(0, 0, 10);
            dir = next - target.transform.position;
            dir = dir.normalized;
            count = 0;
        }
        target.transform.position += dir * ((float)count / (float)span) * 0.05f;
        count++;
    }

    private void OnDisable()
    {
        lifeBuffer.Release();
        if (isUseComputeBuffer)
        {
            beamtopBuffer.Release();
        }
    }
}
