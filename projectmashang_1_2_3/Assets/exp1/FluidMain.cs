using System.Collections;
using System.Collections.Generic;
using test20;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace test21{

public class FluidMain : MonoBehaviour
{
    /**************************************************************/

    [Header("交互的一些项目")]

    public bool stopSimulate = false;//是否暂停仿真
    private float timeBoard=0;//板子运行的时间
    public bool stopBoard = true;//是否暂停板子
    private float boardSpeed = 1.0f;//板子运行的速度

    [Header("变量的设置")]

    public GameObject board;
    public ComputeShader computeShaderPBFSolver;

    public Material materialDrawParticles;
    
    /**********************************************************************************************/

    float gridSize = 2.51f;//格子的大小

    float[] boundary;//虽然是float3[2]，但是考虑到内存对齐，得弄成float[8]的
    int[] gridScale; //这个gridScale中每个元素是一个方向上的网格数量
    private int particleNum;
    private float particleRadius;

    private int maxParticleNumPerCell;
    private int maxNeighborNumPerParticle;

    private float supportRadius;

    private float mass;
    private float rho0;
    //我猜positions是粒子当前位置，oldPositions是粒子上一帧的位置，lambdas未知，deltaPositions是之前和与现在位置的差，
    //particleNumPerGrid未知，particleNumPerGrid是记录每个格子上节点，neiborNumPerParticle未知，neighborIndexPerParticle未知
    public Vector3[] positions;
    public ComputeBuffer _positions;
    Vector3[] oldPositions;
    ComputeBuffer _oldPositions;
    Vector3[] velocities;
    ComputeBuffer _velocities;
    float[] lambdas;
    ComputeBuffer _lambdas;
    Vector3[] deltaPositoins;
    ComputeBuffer _deltaPositions;
    uint[] particleNumPerGrid;
    ComputeBuffer _particleNumPerGrid;
    uint[] particleIndexsPerGrid;
    ComputeBuffer _particleIndexsPerGrid;
    uint[] neighborNumPerParticle;
    ComputeBuffer _neighborNumPerParticle;
    uint[] neighborIndexsPerParticle;
    ComputeBuffer _neighborIndexsPerParticle;

    private int kernelIDPreTreatment;
    private int kernelIDComputeLambdas;
    private int kernelIDComputeDeltaPositions;
    private int kernelIDUpdatePositions;
    private int kernelIDPostTreatment;

    private int kernelIDNeighbourSearchReset;
    private int kernelIDNeighbourSearchInit;
    private int kernelIDComputeNeighbourList;

    private int kernelIDSetParticleDatasForRender;


    private struct ParticleData //3*4=12 12*2=24字节
    {
        public Vector3 position;
        //这个offset是什么？
        public Vector3 offset;
    }
    ParticleData[] particleDatasForRender;
    ComputeBuffer _particleDatasForRender;

    /// <summary>
    /// 边界上的虚拟粒子，用于基于volume map的碰撞检测.
    /// 它是共享的，所以得public出去.
    /// </summary>
    /// 这个虚拟粒子应该指的是流体粒子球体范围与固体边界交叠部分的采样粒子吧，不对，好像是固体边界所在网格上的粒子
    public struct VirtualBoundaryParticle
    {
        public Vector4[] vbp;
    }
    // VirtualBoundaryParticle[] virtualBoundaryParticlesData = null; // 仅作形式上的说明，写起来的话，下面的写法更方便.
    public Vector4[] virtualBoundaryParticlesData = null; // 3个物体，那数组长度就是粒子数的3倍.  这句话什么意思？
    public ComputeBuffer _virtualBoundaryParticlesData = null;    


    //三个步骤，初始化computerShader
    void InitComputeShaderFindKernels()
    {
        kernelIDPreTreatment = computeShaderPBFSolver.FindKernel("PreTreatment");
        kernelIDComputeLambdas = computeShaderPBFSolver.FindKernel("ComputeLambdas");
        kernelIDComputeDeltaPositions = computeShaderPBFSolver.FindKernel("ComputeDeltaPositions");
        kernelIDUpdatePositions = computeShaderPBFSolver.FindKernel("UpdatePositions");
        kernelIDPostTreatment = computeShaderPBFSolver.FindKernel("PostTreatment");

        kernelIDNeighbourSearchReset = computeShaderPBFSolver.FindKernel("NeighbourSearchReset");
        kernelIDNeighbourSearchInit = computeShaderPBFSolver.FindKernel("NeighbourSearchInit");
        kernelIDComputeNeighbourList = computeShaderPBFSolver.FindKernel("ComputeNeighbourList");

        kernelIDSetParticleDatasForRender = computeShaderPBFSolver.FindKernel("SetParticleDatasForRender");
    }

    void InitComputeShaderSetValues()
    {
        // //根据菜单场景设置的参数，设置粒子数
        // if(Parameter.particleNumLevel == 0)
        // {
        //     particleNum = (int)Mathf.Pow(32, 3);//30k
        // }else if(Parameter.particleNumLevel == 1)
        // {
        //     particleNum = (int)Mathf.Pow(40, 3);//60k
        // }else if (Parameter.particleNumLevel == 2)
        // {
        //     particleNum = (int)Mathf.Pow(48, 3);//100k
        // }

        particleNum = (int)Mathf.Pow(32, 3);//30k
        particleNum = (int)Mathf.Pow(40, 3);//60k

        computeShaderPBFSolver.SetInt("_particleNums", particleNum);

        //设置boundary(边界的八个点)
        boundary = new float[8];//boundary[0:3],boundary[4:7]对应两个[x,y,z]格式的点
        //初始化为0了，所以boundary[0:3]自然的等于[0,0,0],不用设置了
        //初始化边界为[60,60,40]
        boundary[4] = 60;
        boundary[5] = 120;
        boundary[6] = 60;
        computeShaderPBFSolver.SetFloats("_boundary", boundary);

        // 下面这几个，都是有联系的
        particleRadius =0.3f; // 粒子半径，单位是国际单位制的“米”
        computeShaderPBFSolver.SetFloat("_particleRadius", particleRadius);
        supportRadius = particleRadius * 4; // splash里就是这么取的
        computeShaderPBFSolver.SetFloat("_supportRadius", supportRadius);


        float particleDiameter = 2 * particleRadius;
        float particleVolume = 0.8f * Mathf.Pow(particleDiameter, 3);
        mass = 1000 * particleVolume; // 0.3米的粒子半径，粒子质量是172kg
        print("mass；" + mass);
        rho0 = 1000f * 0.17f; // 172/150 = 1.147，所以不改也没啥大问题……

        computeShaderPBFSolver.SetFloat("_mass",mass);
        computeShaderPBFSolver.SetFloat("_rho0", rho0);



        computeShaderPBFSolver.SetFloat("_deltaTime", 1.0f / 40.0f);
        computeShaderPBFSolver.SetFloats("G", new float[4] { 0, -9.8f, 0, 0 });//对齐


        computeShaderPBFSolver.SetFloat("_epsilonOfLambda", 100f);

        computeShaderPBFSolver.SetFloat("_poly6Factor", 315.0f / 64.0f / Mathf.PI);
        computeShaderPBFSolver.SetFloat("_spikyGradFactor", -45.0f / Mathf.PI);

        //设置网格，加速邻域查找
        //设置gridScale
        gridScale = new int[4];
        float dx = boundary[4] - boundary[0];
        float dy = boundary[5] - boundary[1];
        float dz = boundary[6] - boundary[2];

        gridScale[0] = Mathf.FloorToInt(dx / gridSize) + 1;
        gridScale[1] = Mathf.FloorToInt(dy / gridSize) + 1;
        gridScale[2] = Mathf.FloorToInt(dz / gridSize) + 1;
            Debug.Log("gridScale" + gridScale[0] + "  " + gridScale[1] + "  " + gridScale[2]);
            computeShaderPBFSolver.SetInts("_gridScale", gridScale);

        computeShaderPBFSolver.SetFloat("_gridSize", gridSize);
        computeShaderPBFSolver.SetFloat("_gridRecpr", 1.0f / gridSize);

        maxParticleNumPerCell = 200;
        computeShaderPBFSolver.SetInt("_maxParticleNumPerCell", maxParticleNumPerCell);
        maxNeighborNumPerParticle = 200;
        computeShaderPBFSolver.SetInt("_maxNeighborNumPerParticle",maxNeighborNumPerParticle);
    }

    void InitPositions()
    {


        // int rowNum = 32;
        int rowNum = 40;

        // if (Parameter.particleNumLevel == 0)
        // {
        //     rowNum = 32;
        // }else if (Parameter.particleNumLevel == 1)
        // {
        //     rowNum = 40;
        // }else if(Parameter.particleNumLevel == 2)
        // {
        //     rowNum = 48;
        // }

        float delta = 1.1f * 0.8f;
        float temp = (60 - rowNum * delta) / 2;
        Vector3 anchor = new Vector3(temp, temp, temp); 
        anchor.y += 60; // 增加方块的高度.

        for (int i = 0; i < particleNum; i++)
        {
            positions[i] = anchor + new Vector3(i % rowNum, i/rowNum%rowNum, i/(rowNum*rowNum)) * delta;//初始化粒子位置为rowNum*rowNum*rowNum规模的立方体
        }

    }

    void InitPositionsDoublePeak()//用于双峰溃坝模拟的初始化
    {

        float delta = 1.1f * 0.8f;
        int firstPartNum = particleNum/2;

        int zNum = 60;
        int yNum = 60;
        Vector3 anchor;
        anchor = new Vector3(0, 0.1f, 3.5f);
        for (int i = 0; i < firstPartNum; ++i)//用一半的粒子，初始化第一个峰
        {
            positions[i] = anchor + new Vector3(i / (zNum * yNum), i / yNum % zNum, i % zNum)*delta;//先z再y再x
        }

        anchor = new Vector3(59.5f, 0.1f, 3.5f);//整体平移

        for(int i = firstPartNum; i < particleNum; ++i)//用另一半粒子，初始化第二个峰
        {
            int index = i - firstPartNum;
            positions[i] = anchor + new Vector3(-index / (zNum * yNum), index / yNum % zNum, index % zNum) * delta;
        }
    }

    void InitComputeShaderSetBuffers()//两个步骤：01设置buffer，02逐kernel绑定相关buffer
    {
        
        //01设置buffer
        positions = new Vector3[particleNum];
        InitPositions();//初始化粒子位置为立方体

        // if (Parameter.isSinglePeak)
        // {
        //     InitPositions();//初始化粒子位置为立方体
        // }
        // else
        // {
        //     InitPositionsDoublePeak();
        // }
        

        _positions = new ComputeBuffer(particleNum, sizeof(float) * 3);
        _positions.SetData(positions);

        velocities = new Vector3[particleNum];
        _velocities = new ComputeBuffer(particleNum, sizeof(float) * 3);
        _velocities.SetData(velocities);

        oldPositions = new Vector3[particleNum];
        _oldPositions = new ComputeBuffer(particleNum, sizeof(float) * 3);
        _oldPositions.SetData(oldPositions);

        lambdas = new float[particleNum];
        _lambdas = new ComputeBuffer(particleNum, sizeof(float));
        _lambdas.SetData(lambdas);

        deltaPositoins = new Vector3[particleNum];
        _deltaPositions = new ComputeBuffer(particleNum, sizeof(float) * 3);
        _deltaPositions.SetData(deltaPositoins);

        particleNumPerGrid = new uint[gridScale[0] * gridScale[1] * gridScale[2]];
        _particleNumPerGrid = new ComputeBuffer(gridScale[0] * gridScale[1] * gridScale[2], sizeof(uint));
        _particleNumPerGrid.SetData(particleNumPerGrid);

        particleIndexsPerGrid = new uint[gridScale[0] * gridScale[1] * gridScale[2] * maxParticleNumPerCell];
        _particleIndexsPerGrid = new ComputeBuffer(gridScale[0] * gridScale[1] * gridScale[2] * maxParticleNumPerCell, sizeof(uint));
        _particleIndexsPerGrid.SetData(particleIndexsPerGrid);

        neighborNumPerParticle = new uint[particleNum];
        _neighborNumPerParticle = new ComputeBuffer(particleNum, sizeof(uint));
        _neighborNumPerParticle.SetData(neighborNumPerParticle);

        neighborIndexsPerParticle = new uint[particleNum * maxNeighborNumPerParticle];
        _neighborIndexsPerParticle = new ComputeBuffer(particleNum * maxNeighborNumPerParticle, sizeof(uint));
        _neighborIndexsPerParticle.SetData(neighborIndexsPerParticle);

        //不知道为什么固体的边界虚拟粒子的数量是流体粒子数量的三倍？可能只是这样规定的
        virtualBoundaryParticlesData = new Vector4[particleNum * 3];
        _virtualBoundaryParticlesData = new ComputeBuffer(particleNum, 3*4*sizeof(float));
        _virtualBoundaryParticlesData.SetData(new Vector4[particleNum * 3]);


        //准备渲染的数据
        particleDatasForRender = new ParticleData[particleNum * 4];
        _particleDatasForRender = new ComputeBuffer(particleNum * 4, sizeof(float) * (3 + 3));
        _particleDatasForRender.SetData(particleDatasForRender);

        //02逐kernel绑定相关buffer
        //计算相关的
        computeShaderPBFSolver.SetBuffer(kernelIDPreTreatment, "_positions", _positions);
        computeShaderPBFSolver.SetBuffer(kernelIDPreTreatment, "_velocities", _velocities);
        computeShaderPBFSolver.SetBuffer(kernelIDPreTreatment, "_oldPositions", _oldPositions);

        computeShaderPBFSolver.SetBuffer(kernelIDComputeLambdas, "_positions", _positions);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeLambdas, "_lambdas", _lambdas);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeLambdas, "_neighborNumPerParticle", _neighborNumPerParticle);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeLambdas, "_neighborIndexsPerParticle", _neighborIndexsPerParticle);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeLambdas, "_virtualBoundaryParticlesData", _virtualBoundaryParticlesData);//碰撞检测要用


        computeShaderPBFSolver.SetBuffer(kernelIDComputeDeltaPositions, "_positions", _positions);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeDeltaPositions, "_deltaPositions", _deltaPositions);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeDeltaPositions, "_lambdas", _lambdas);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeDeltaPositions, "_neighborNumPerParticle", _neighborNumPerParticle);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeDeltaPositions, "_neighborIndexsPerParticle", _neighborIndexsPerParticle);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeDeltaPositions, "_virtualBoundaryParticlesData", _virtualBoundaryParticlesData);//碰撞检测要用


        computeShaderPBFSolver.SetBuffer(kernelIDUpdatePositions, "_positions", _positions);
        computeShaderPBFSolver.SetBuffer(kernelIDUpdatePositions, "_deltaPositions", _deltaPositions);

        computeShaderPBFSolver.SetBuffer(kernelIDPostTreatment, "_positions", _positions);
        computeShaderPBFSolver.SetBuffer(kernelIDPostTreatment, "_oldPositions", _oldPositions);
        computeShaderPBFSolver.SetBuffer(kernelIDPostTreatment, "_velocities", _velocities);
        //邻居搜索加速相关的
        computeShaderPBFSolver.SetBuffer(kernelIDNeighbourSearchReset, "_particleNumPerGrid", _particleNumPerGrid);

        computeShaderPBFSolver.SetBuffer(kernelIDNeighbourSearchInit, "_particleNumPerGrid", _particleNumPerGrid);
        computeShaderPBFSolver.SetBuffer(kernelIDNeighbourSearchInit, "_particleIndexsPerGrid", _particleIndexsPerGrid);
        computeShaderPBFSolver.SetBuffer(kernelIDNeighbourSearchInit, "_positions", _positions);

        computeShaderPBFSolver.SetBuffer(kernelIDComputeNeighbourList, "_positions", _positions);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeNeighbourList, "_particleNumPerGrid", _particleNumPerGrid);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeNeighbourList, "_particleIndexsPerGrid", _particleIndexsPerGrid);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeNeighbourList, "_neighborNumPerParticle", _neighborNumPerParticle);
        computeShaderPBFSolver.SetBuffer(kernelIDComputeNeighbourList, "_neighborIndexsPerParticle", _neighborIndexsPerParticle);

        //渲染相关的
        computeShaderPBFSolver.SetBuffer(kernelIDSetParticleDatasForRender, "_positions", _positions);
        computeShaderPBFSolver.SetBuffer(kernelIDSetParticleDatasForRender, "_particleDatasForRender", _particleDatasForRender);

    }

    // 那个共享buffer，别人引用的时候不能是null，得提取初始化好，而awake比start早，所以这里用了awake.
    void Awake()
    {
        //初始化计算着色器，用于仿真计算
        InitComputeShaderFindKernels();
        InitComputeShaderSetValues();
        InitComputeShaderSetBuffers();
       
    }

    // Update is called once per frame
    void Update()
    {

        //回到菜单场景
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(0);
        }

        //控制仿真是否暂停
        if (Input.GetKeyDown(KeyCode.Space))
        {
            stopSimulate = !stopSimulate;
        }

        if (stopSimulate)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.B))//仿真暂停的时候，不能更新板子的状态
        {
            stopBoard = !stopBoard;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            boardSpeed++;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            boardSpeed = Mathf.Max(1, boardSpeed - 1);
        }

        //更新板子的位置
        if (!stopBoard)
        {
            timeBoard += boardSpeed*0.015f;
            float boundaryMaxX = 60 - 20 * Mathf.Abs(Mathf.Sin(1.0f*timeBoard));
            board.transform.position = new Vector3(boundaryMaxX, 30, 30);

            //更新boundary
            boundary = new float[8];//boundary[0:3],boundary[4:7]对应两个[x,y,z]格式的点
            //初始化为0了，所以boundary[0:3]自然的等于[0,0,0],不用设置了
            //初始化边界为[60,60,40]
            boundary[4] = boundaryMaxX;
            boundary[5] = 120;
            boundary[6] = 60;
            computeShaderPBFSolver.SetFloats("_boundary", boundary);
        }


        //仿真计算
        computeShaderPBFSolver.Dispatch(kernelIDPreTreatment, particleNum / 32, 1, 1);
        //构造邻居列表，加速计算
        computeShaderPBFSolver.Dispatch(kernelIDNeighbourSearchReset, gridScale[0] * gridScale[1] * gridScale[2] / 32, 1, 1);
        computeShaderPBFSolver.Dispatch(kernelIDNeighbourSearchInit, particleNum / 32, 1, 1);
        computeShaderPBFSolver.Dispatch(kernelIDComputeNeighbourList, particleNum / 32, 1, 1);
        //构造结束
        for (int i = 0; i < 10; ++i)
        {
            // 准备碰撞检测
            GetComponent<ModelsMain>().ComputeVolumeAndPositionForBoundaryParticles(particleNum);
            // 仿真
            computeShaderPBFSolver.Dispatch(kernelIDComputeLambdas, particleNum / 32, 1, 1);
            computeShaderPBFSolver.Dispatch(kernelIDComputeDeltaPositions, particleNum / 32, 1, 1);
            computeShaderPBFSolver.Dispatch(kernelIDUpdatePositions, particleNum / 32, 1, 1);
        }
        computeShaderPBFSolver.Dispatch(kernelIDPostTreatment, particleNum / 32, 1, 1);
        
        // 更新数据，供obicollidespheremanage使用
        _positions.GetData(positions);
        _virtualBoundaryParticlesData.GetData(virtualBoundaryParticlesData);
    }

    /// <summary>
    /// 在Unity中，OnRenderObject是一个重要的函数，它在摄像机完成所有常规场景渲染后调用。这意味着，在场景中的其他对象已经被渲染后，OnRenderObject将被触发。OnRenderObject的主要用途是允许开发者通过Graphics.DrawMeshNow或其他函数来绘制自己的对象或自定义几何图形。这使得开发者能够在常规的渲染过程之后，添加额外的渲染步骤或效果。与OnPostRender类似，OnRenderObject也是用于后期渲染处理，但它们的区别在于调用时机和调用对象。OnPostRender是在相机完成场景渲染后调用，而OnRenderObject是在具有包含该函数的脚本的任何对象上调用，无论这些对象是否附加到摄像机。
    /// </summary>
    void OnRenderObject()
    {
        //准备渲染数据
        computeShaderPBFSolver.Dispatch(kernelIDSetParticleDatasForRender, particleNum / 32, 1, 1);

        //直接以粒子形式渲染
        materialDrawParticles.SetBuffer("_particleDataBuffer", _particleDatasForRender);//相关设置
        materialDrawParticles.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Quads, particleNum * 4);        

    }

    void OnDestroy()
    {
        _positions.Release();
        _velocities.Release();
        _oldPositions.Release();
        _lambdas.Release();
        _deltaPositions.Release();

        _particleNumPerGrid.Release();
        _particleIndexsPerGrid.Release();
        _neighborNumPerParticle.Release();
        _neighborIndexsPerParticle.Release();

        _particleDatasForRender.Release();

        _virtualBoundaryParticlesData.Release();
    }

    //先调用update，再调用这个。详见：https://blog.csdn.net/zw514159799/article/details/50445821
    private void OnDrawGizmos()
    {
        // Gizmos.matrix = GetComponent<Renderer>().localToWorldMatrix;

        // 将空间范围可视化，api详见：https://blog.csdn.net/qq_35030499/article/details/88658452
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center:new Vector3(30,60,30), size:new Vector3(60,120,60));
    }



}


}
