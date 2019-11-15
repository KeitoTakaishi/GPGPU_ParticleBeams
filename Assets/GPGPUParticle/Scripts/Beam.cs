/*
アイディアとして質量を加える
F = ma
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beam : MonoBehaviour
{
    [SerializeField] GameObject target;
    [SerializeField] Vector2 range;
    [SerializeField] float curvatureRadius;
    [SerializeField] private Vector3 vel;

    [SerializeField] float damping;
    float deltaTime;
    int time;//アクセスするRenderTextureに対応
    float propulsion;
    private Vector3 position;
    private Vector3 toTarget;

    private Vector3 vn;
    private Vector3 centripetal;

    private float theta;
    private float maxCentripetalAccel;
    float speed = 1.0f;


    #region shader
    public RenderTexture rt;
    //[SerializeField] Shader s
    #endregion
    private void Awake()
    {


    }
    void Start()
    {
        target = GameObject.Find("Target");
        this.position = position;
        //this.vel = new Vector3(Random.Range(-range.x, range.x), Random.Range(-range.y, range.y), 1.0f);

        // 速さv、半径rで円を描く時、その向心力はv^2/r。これを計算しておく。
        this.maxCentripetalAccel = speed * speed / curvatureRadius;
        this.damping = damping;
        // 終端速度がspeedになるaccelを求める
        // v = a / kだからa=v*k
        this.propulsion = speed * damping;
    }

    void Update()
    {
        //renderTextureにtimeと先頭座標を送信する
        if ((this.transform.position - target.transform.position).magnitude < 1.3f)
        {
            time = 0;
            Destroy(this.gameObject);

        }
        time++;
    }

    private void LateUpdate()
    {
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
    }
}
