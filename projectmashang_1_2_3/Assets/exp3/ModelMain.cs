
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Threading.Tasks;
using System.Linq;  

namespace exp3{

/// <summary>
/// 相当于大号的NodeVisu，model的main，是个总枢纽，关于模型建模的相关数据全都统一的通过它获取
/// </summary>
public class ModelMain : MonoBehaviour
{
    // 供Main使用的一些数据

    /// <summary>
    /// 模型的法线，计算sdf的点到三角形面片距离的时候会用到.
    /// </summary>
    public Vector3[] normals = null;

    /// <summary>
    /// 前后两帧之间顶点坐标的差，用来驱动map的更新.
    /// 分别对应前一帧的顶点，当前帧的顶点.它俩的差，就是顶点位置的改变量.有点类似快慢指针……
    /// </summary>
    public Vector3[] oldVertices,nowVertices,tempDeltaModelVertex = null;

    /// <summary>
    /// 世界坐标系下的粒子与模型局部坐标系下离散网格坐标的转化.
    /// </summary>
    public Matrix4x4 worldToLocalMatrix,localToWorldMatrix;

    /// <summary>
    /// hlsl里在四面体中生成均匀散点用到的随机数.
    /// </summary>
    public float[] randomASTU = null;

    // 后续计算用到的公共数据 /////////////////////////////////////////////////////////////
    [Header("调试时表示节点的box")]
    public GameObject box;

    [Header("离散网格分辨率的设置")]
    public Vector3Int resolution = new Vector3Int(10, 10, 10); 

    [Header("该模型对应的cache文件名")]
    public string cacheName = null; // 前提是这个文件得在cache文件夹里

    /// <summary>
    /// 模型法线，顶点改变量，都需要每帧重算的。每帧new一个mesh的话，可能会内存泄漏；所以统一放到这里了.
    /// </summary>
    private Mesh mesh = null;

    /// <summary>
    /// ModelData所在模型的顶点总数
    /// </summary>
    public int vertexNum = 0;

    /// <summary>
    /// ModelData所在模型的顶点坐标数组.不new行吗？官网里也没new额.
    /// </summary>
    public Vector3[] vertices = null;

    /// <summary>
    /// 三角形面片总数
    /// </summary>
    public int triangleNum = 0;

    /// <summary>
    /// 三角形索引
    /// </summary>
    public int[] triangles = null;

    // 后续计算的对象 ///////////////////////////////////////////////////////////////////

    // 离散网格相关数据
    private TriangleMeshDistance td = null;
    public DiscreteGrid dg = null; // public出去，供main跨脚本调用

    /// <summary>
    /// 邻居顶点组，查找以顶点A为中心的三角形带，构造四面体组的时候会用到.
    /// </summary>
    public NeighbourVertexList nvl = null;

    /// <summary>
    /// 为了避免并行的线程冲突，所以顶点分组.
    /// </summary>
    public VertexGroup vg = null;

    void Awake(){

        // 给公共数据赋值
        mesh = new Mesh();


        if(GetComponent<MeshFilter>()!=null){
            mesh = GetComponent<MeshFilter>().mesh;
        }else if(GetComponent<SkinnedMeshRenderer>()!=null){
            // 提到了包围盒是不重算的：https://blog.csdn.net/mansir123/article/details/84328015
            GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh); // 大概是局部覆盖，bounds大小依旧是刚new时留下的0.获取当前mesh的快照
            mesh.RecalculateBounds();
            // mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh; // 大概是整体覆盖，所以bounds的大小正常了
        }
        
        vertexNum = mesh.vertexCount;
        vertices = mesh.vertices;
        triangleNum = mesh.triangles.Length / 3;
        triangles = mesh.triangles;

        // 01.生成离散网格的相关数据

        td = new TriangleMeshDistance(vertexNum, vertices, triangleNum, triangles); // 辅助的

        Bounds domain = mesh.bounds;
        Vector3 domainOffset = new Vector3(1.5f, 1.5f, 1.5f); // 更合理一点
        domain.min -= domainOffset;
        domain.max += domainOffset;

        dg = new DiscreteGrid(domain, resolution, td,cacheName); // 构造dg类

        // Debug();

        // 02.生成顶点组，三角形带，四面体组的相关数据
        nvl = new NeighbourVertexList(mesh);

        // 03.并行数据竞争，所以顶点分组
        vg = new VertexGroup(mesh);

        // 04.Main.cs会调用的相关数组的初始化.

        tempDeltaModelVertex = new Vector3[vertexNum];
        oldVertices = new Vector3[vertexNum];
        nowVertices = new Vector3[vertexNum];

        mesh.RecalculateNormals();
        normals = mesh.normals;

        randomASTU = new float[1024 * 4]; // 全程不变的，放在awake里就行额.
        for(int i=0;i<1024;i++){
            Vector4 tmp_result = GenerateRandomASTU();
            randomASTU[i*4+0]=tmp_result.x;
            randomASTU[i*4+1]=tmp_result.y;
            randomASTU[i*4+2]=tmp_result.z;
            randomASTU[i*4+3]=tmp_result.w;
        }        

    }

    void Debug(){
        // 可视化
        // unity相关的，只能在主线程里做，不能CPU多核并行
        for (int i = 0; i < dg.numberOfNodes; i++)
        {

            // 设置颜色：http://www.manongjc.com/detail/58-jjflqxwnlxntabk.html

            // 把节点全部可视化出来，太卡，太杂；所以只可视化一部分
            if(dg.sdfNode[i]<-1.2f || dg.sdfNode[i] > +1.2f)continue;

            GameObject node_repre = Instantiate(box); // https://blog.csdn.net/qq_42139931/article/details/120431942

            // 用法：https://blog.csdn.net/qq_51533157/article/details/127030312
            node_repre.transform.position = dg.nodeLocalPosition[i]; // 和下面那行，呼应的很好额……
            node_repre.transform.SetParent(this.transform,false); // 设置为false，原本的世界坐标直接变成局部坐标

            if (dg.sdfNode[i] < 0)
               node_repre.GetComponent<MeshRenderer>().material.color = Color.blue; // 设置颜色：http://www.manongjc.com/detail/58-jjflqxwnlxntabk.html
            else
               node_repre.GetComponent<MeshRenderer>().material.color = Color.green;

            // //Volume的可视化
            // float volumeMax = 4*Mathf.Pow(1.2f, 3); // 全在mesh内部，volume 取最大值
            // // float volumeMax = 0.8f * Mathf.Pow(2 * 4 * 0.3f, 3); // 支撑直径才是box的边长，支撑半径只是一半;0.8，box转sphere，比例就是0.8
            // float ratio = dg.volumeNode[i] / volumeMax;

            // node_repre.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white,Color.red,ratio);

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // 这个update，得先于main的update执行.因为它是给main提供数据的.

        // GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);

        if(GetComponent<MeshFilter>()!=null){
            mesh = GetComponent<MeshFilter>().mesh;
        }else if(GetComponent<SkinnedMeshRenderer>()!=null){
            GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh); 
        }


        // 更新法线.
        mesh.RecalculateNormals();
        normals = mesh.normals;

        // vertices = mesh.vertices;

        // 计算deltaVertex
        if(1 == Time.frameCount){ // 改变是个相对的概念，而它就是最开头，也就不存在所谓的改变.即改变量为0.
            Array.Copy(mesh.vertices, oldVertices, oldVertices.Length); // 数组深拷贝，用法详见：http://t.csdnimg.cn/xdPhw
            Array.Copy(mesh.vertices, nowVertices, nowVertices.Length);
        }else{ // 从第2帧开始，才有改变量的概念.
            Array.Copy(nowVertices , oldVertices , nowVertices.Length);
            Array.Copy(mesh.vertices , nowVertices , nowVertices.Length);
        }

        Parallel.For(0,mesh.vertices.Length,i=>
        {
            tempDeltaModelVertex[i] = nowVertices[i] - oldVertices[i];
        });        

        // 更新坐标系转换用到的矩阵.
        // obi，好像get skin的那个render更合理，但是这么写也没报错；先这样吧……
        // 可能是因为，render的子类是mesh render和skinrender吧.obi物体上有子类就等于有父类了.
        // if(GetComponent<Renderer>() != null){print("matrix not null");}
        worldToLocalMatrix = GetComponent<Renderer>().worldToLocalMatrix;
        localToWorldMatrix = GetComponent<Renderer>().localToWorldMatrix;


    }

    void OnDrawGizmos(){

        if(!Application.isPlaying)
            return;

        Gizmos.matrix = GetComponent<Renderer>().localToWorldMatrix;

        // print("sdf 的最大值"+ dg.sdfNode.Max());
        // print("sdf 的最小值"+ dg.sdfNode.Min());

        // // 绘制sdf
        // for(int i=0;i<dg.numberOfNodes;i++){
        //     float sdf = dg.sdfNode[i];

        //     if(Mathf.Abs(sdf)>1.2f)continue;
        //     // if(sdf < -1.2f)continue;

        //     Gizmos.color = Color.Lerp(Color.blue,Color.green,(sdf+1.2f)/2.4f);

        //     Gizmos.DrawCube(dg.nodeLocalPosition[i],Vector3.one);
        // }

        // // 绘制volume map
        // for(int i=0;i<dg.numberOfNodes;i++){
        //     float volume = dg.volumeNode[i];
        //     if(volume < 0.05f) continue;

        //     Gizmos.color = Color.Lerp(Color.cyan,Color.red,volume/7.2f);

        //     Gizmos.DrawCube(dg.nodeLocalPosition[i],Vector3.one);
        // }        

    }


    /// <summary>
    /// 还是生成a,s,t,u.这次按随机的来，并且赋值给数组而不是rwstructbuffer.
    /// 关于unity里random的介绍，详见：https://docs.unity3d.com/Manual/class-Random.html
    /// </summary>
    /// <returns></returns>
    Vector4 GenerateRandomASTU(){
        Vector4 result = Vector4.zero;

        float s = UnityEngine.Random.Range(0.0f,1.0f); // api详细介绍：https://docs.unity3d.com/ScriptReference/Random.Range.html
        float t = UnityEngine.Random.Range(0.0f,1.0f);
        float u = UnityEngine.Random.Range(0.0f,1.0f);

        if(s+t>1.0) { // cut'n fold the cube into a prism
        
        s = 1.0f - s;
        t = 1.0f - t;
        
        }
        if(t+u>1.0) { // cut'n fold the prism into a tetrahedron
        
        float tmp = u;
        u = 1.0f - s - t;
        t = 1.0f - tmp;
        
        } else if(s+t+u>1.0) {
        
        float tmp = u;
        u = s + t + u - 1.0f;
        s = 1 - t - tmp;
        
        }
        float a=1-s-t-u; // a,s,t,u are the barycentric coordinates of the random point.

        result = new Vector4(a,s,t,u);
        return result;
    }

    
}




}

