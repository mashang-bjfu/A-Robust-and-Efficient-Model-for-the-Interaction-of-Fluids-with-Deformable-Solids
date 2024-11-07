using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace exp3{

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading.Tasks;
using System.ComponentModel;
using System;

// 序列化类需要的头文件
using System;  
using System.IO;  
using System.Runtime.Serialization.Formatters.Binary;  


// 尝试把这些单独抽出来，这样看着更清晰
public class DiscreteGrid
{
    /// <summary>
    /// 如果cache文件夹下有这个文件，就直接加载；如果没有，就算完了再按这个名字存一份
    /// </summary>
    private string cacheName = null;
    public Bounds domain;
    public Vector3Int resolution;
    public TriangleMeshDistance td = null; // 一层套一层，有内味儿了……

    public Vector3 cellSize;
    public int numberOfCells;
    public int nv, ne_x, ne_y, ne_z,ne;
    public int numberOfNodes;


    public Vector3[] nodeLocalPosition = null;
    public float[] sdfNode = null;
    public float[] volumeNode = null;

    /// <summary>
    /// cells数组的长度为numberOfCells * 32，存的是到node的索引，node数组里是具体的数值
    /// </summary>
    public int[] cells = null;

    public DiscreteGrid(Bounds domain,Vector3Int resolution,TriangleMeshDistance td,string cacheName)
    {
        this.domain = domain;
        this.resolution = resolution;
        this.td = td;
        this.cacheName = cacheName;

        InitValues();

        Debug.Log("domain center:" + domain.center);
        Debug.Log("domain size:" + domain.size);
        Debug.Log("resolution:" + resolution);
        Debug.Log("cell size:" + cellSize);
        Debug.Log("number of cells:" + numberOfCells);
        Debug.Log("number of nodes:" + numberOfNodes);

        InitArrays();

    }

    void InitValues()
    {
        cellSize = new Vector3(
        this.domain.size.x / this.resolution.x,
        this.domain.size.y / this.resolution.y,
        this.domain.size.z / this.resolution.z);

        numberOfCells = resolution.x * resolution.y * resolution.z;


        // 计算node的数量
        nv = (resolution.x + 1) * (resolution.y + 1) * (resolution.z + 1); // number of cell's vertex
        // cell的集合就是obj模型的AABB，而AABB是根据obj模型的局部坐标系来算的，所以，cell和局部坐标系的xyz轴是有间接的联系的
        ne_x = (resolution.x + 0) * (resolution.y + 1) * (resolution.z + 1); // number of cell's edge parallel to the x-axis of the local coordinate system
        ne_y = (resolution.x + 1) * (resolution.y + 0) * (resolution.z + 1); // number of cell's edge parallel to the y-axis of the local coordinate system
        ne_z = (resolution.x + 1) * (resolution.y + 1) * (resolution.z + 0); // number of cell's edge parallel to the z-axis of the local coordinate system

        ne = ne_x + ne_y + ne_z; // number of cell's edges

        numberOfNodes = 1 * nv + 2 * ne; // 1个顶点处有1个node，1条边上串着2个node


    }


    /// <summary>
    /// 计算第index个node在模型局部坐标系下的三维序号；
    /// 并根据三维序号，计算该node在模型局部坐标系下的坐标
    /// </summary>
    /// <param name="index">node的一维序号</param>
    /// <returns>模型局部坐标系下的坐标</returns>
    Vector3 NodeLogicPosToPhyPos(int index)
    {
        Vector3 result = Vector3.zero;

        // 计算node的数量
        int nv = (resolution.x + 1) * (resolution.y + 1) * (resolution.z + 1); // number of cell's vertex
        int ne_x = (resolution.x + 0) * (resolution.y + 1) * (resolution.z + 1); // number of cell's edge parallel to the x-axis of the local coordinate system
        int ne_y = (resolution.x + 1) * (resolution.y + 0) * (resolution.z + 1); // number of cell's edge parallel to the y-axis of the local coordinate system
        int ne_z = (resolution.x + 1) * (resolution.y + 1) * (resolution.z + 0); // number of cell's edge parallel to the z-axis of the local coordinate system

        Vector3Int ijk = Vector3Int.zero; // 第k层第j行第i个 node/edge

        if (index < nv)
        {
            ijk[2] = index / ((resolution.y + 1) * (resolution.x + 1)); // 层号
            int temp = index % ((resolution.y + 1) * (resolution.x + 1));
            ijk[1] = temp / (resolution.x + 1); // 行号
            ijk[0] = temp % (resolution.x + 1); // 行内序号

            result = domain.min + new Vector3(cellSize.x * ijk[0], cellSize.y * ijk[1], cellSize.z * ijk[2]);
        }
        else if (index < nv + 2 * ne_x)
        {
            index -= nv;
            int e_ind = index / 2; // 一边两点：node0和node1，都在edge0上；node2和node3，都在edge1上……
            ijk[2] = e_ind / ((resolution.y + 1) * resolution.x);
            int temp = e_ind % ((resolution.y + 1) * resolution.x);
            ijk[1] = temp / resolution.x;
            ijk[0] = temp % resolution.x; // edge，也是先x再y再z这么排列的

            result = domain.min + new Vector3(cellSize.x * ijk[0], cellSize.y * ijk[1], cellSize.z * ijk[2]);
            result.x += (1.0f + index % 2) / 3.0f * cellSize.x;

            // draw line
            if (index % 2 == 0) // 为了防止一条edge绘制2次。因为点：边=2：1
            {
                Vector3 start = domain.min + new Vector3(cellSize.x * ijk[0], cellSize.y * ijk[1], cellSize.z * ijk[2]);
                Vector3 end = start;
                end.x += cellSize.x;

                //Debug.DrawLine(start, end, Color.red,1000,false); // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Debug.DrawLine.html
            }
        }
        else if (index < nv + 2 * ne_x + 2 * ne_y)
        {
            index -= (nv + 2 * ne_x);
            int e_ind = index / 2;
            ijk[0] = e_ind / ((resolution.z + 1) * resolution.y); // 最后沿x轴排
            int temp = e_ind % ((resolution.z + 1) * resolution.y);
            ijk[2] = temp / resolution.y; // 再沿z轴
            ijk[1] = temp % resolution.y; // 先沿y轴排

            result = domain.min + new Vector3(cellSize.x * ijk[0], cellSize.y * ijk[1], cellSize.z * ijk[2]);
            result.y += (1.0f + index % 2) / 3.0f * cellSize.y;

            // draw line
            if (index % 2 == 0) // 为了防止一条edge绘制2次。因为点：边=2：1
            {
                Vector3 start = domain.min + new Vector3(cellSize.x * ijk[0], cellSize.y * ijk[1], cellSize.z * ijk[2]);
                Vector3 end = start;
                end.y += cellSize.y;

                //Debug.DrawLine(start, end, Color.green, 1000, false); // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Debug.DrawLine.html
            }
        }
        else
        {
            index -= (nv + 2 * ne_x + 2 * ne_y);
            int e_ind = index / 2;

            ijk[1] = e_ind / ((resolution.x + 1) * resolution.z); // 最后排的是y
            int temp = e_ind % ((resolution.x + 1) * resolution.z);
            ijk[0] = temp / resolution.z; // 再沿x轴排
            ijk[2] = temp % resolution.z; // 先沿z轴排 

            result = domain.min + new Vector3(cellSize.x * ijk[0], cellSize.y * ijk[1], cellSize.z * ijk[2]);
            result.z += (1.0f + index % 2) / 3.0f * cellSize.z;

            // draw line
            if (index % 2 == 0) // 为了防止一条edge绘制2次。因为点：边=2：1
            {
                Vector3 start = domain.min + new Vector3(cellSize.x * ijk[0], cellSize.y * ijk[1], cellSize.z * ijk[2]);
                Vector3 end = start;
                end.z += cellSize.z;

                //Debug.DrawLine(start, end, Color.blue, 1000, false); // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Debug.DrawLine.html
            }

        }

        // xyz,yzx,zxy，这个排列，可能是按字母序来的……

        return result; // 这个是模型局部坐标系下的坐标
    }

    /// <summary>
    /// 将第index个cell，按xyz的排列方式，映射到第k层第j行第i个cell
    /// 并找到该cell对应的32个node的索引
    /// 使后续的使用更加方便
    /// </summary>
    /// <param name="index"> cell的索引值，范围是[0,numberOfCells-1] </param>
    void NodeToCell(int index)
    {

        // 常规操作
        int nv = (resolution.x + 1) * (resolution.y + 1) * (resolution.z + 1); // number of cell's vertex
        int ne_x = (resolution.x + 0) * (resolution.y + 1) * (resolution.z + 1); // number of cell's edge parallel to the x-axis of the local coordinate system
        int ne_y = (resolution.x + 1) * (resolution.y + 0) * (resolution.z + 1); // number of cell's edge parallel to the y-axis of the local coordinate system
        int ne_z = (resolution.x + 1) * (resolution.y + 1) * (resolution.z + 0); // number of cell's edge parallel to the z-axis of the local coordinate system


        // ijk,xyz,字典序。格子是按xyz的顺序排列的
        int k = index / (resolution.y * resolution.x); // 再z
        int temp = index % (resolution.y * resolution.x);
        int j = temp / resolution.x; // 再y
        int i = temp % resolution.x; // 先x

        int nx = resolution.x; // x轴方向的cell的number，和分辨率的x分量挂钩
        int ny = resolution.y;
        int nz = resolution.z;

        // 01.cell里顶点处的8个node对应的索引值
        cells[index * 32 + 0] = (nx + 1) * (ny + 1) * k + (nx + 1) * j + i; // [i+1|0,j+1|0,k+1|0],2*2*2=8种情况 ‘|’或的意思
        cells[index * 32 + 1] = (nx + 1) * (ny + 1) * k + (nx + 1) * j + i + 1;
        cells[index * 32 + 2] = (nx + 1) * (ny + 1) * k + (nx + 1) * (j + 1) + i;
        cells[index * 32 + 3] = (nx + 1) * (ny + 1) * k + (nx + 1) * (j + 1) + i + 1;
        cells[index * 32 + 4] = (nx + 1) * (ny + 1) * (k + 1) + (nx + 1) * j + i;
        cells[index * 32 + 5] = (nx + 1) * (ny + 1) * (k + 1) + (nx + 1) * j + i + 1;
        cells[index * 32 + 6] = (nx + 1) * (ny + 1) * (k + 1) + (nx + 1) * (j + 1) + i;
        cells[index * 32 + 7] = (nx + 1) * (ny + 1) * (k + 1) + (nx + 1) * (j + 1) + i + 1;

        // 02.cell里edge x处的8个node对应的索引值(4条edge，每个edge上2个node，2*4=8)
        // [x,y,z]，因为是edge x，所以x是不变的，所以有如下几种情况：[x,y+1|0，z+1|0] 2*2=4种排列组合
        int offset = nv;
        cells[index * 32 + 8] = offset + 2 * (nx * (ny + 1) * k + nx * j + i);  // 边1上的第一个node
        cells[index * 32 + 9] = cells[index * 32 + 8] + 1; // 边1上的第二个node
        cells[index * 32 + 10] = offset + 2 * (nx * (ny + 1) * (k + 1) + nx * j + i); // 边2
        cells[index * 32 + 11] = cells[index * 32 + 10] + 1;
        cells[index * 32 + 12] = offset + 2 * (nx * (ny + 1) * k + nx * (j + 1) + i); // 边3
        cells[index * 32 + 13] = cells[index * 32 + 12] + 1;
        cells[index * 32 + 14] = offset + 2 * (nx * (ny + 1) * (k + 1) + nx * (j + 1) + i); // 边4
        cells[index * 32 + 15] = cells[index * 32 + 14] + 1;

        // 03.cell里edge y处的8个node对应的索引值
        offset += 2 * ne_x;
        cells[index * 32 + 16] = offset + 2 * (ny * (nz + 1) * i + ny * k + j);
        cells[index * 32 + 17] = cells[index * 32 + 16] + 1;
        cells[index * 32 + 18] = offset + 2 * (ny * (nz + 1) * (i + 1) + ny * k + j);
        cells[index * 32 + 19] = cells[index * 32 + 18] + 1;
        cells[index * 32 + 20] = offset + 2 * (ny * (nz + 1) * i + ny * (k + 1) + j);
        cells[index * 32 + 21] = cells[index * 32 + 20] + 1;
        cells[index * 32 + 22] = offset + 2 * (ny * (nz + 1) * (i + 1) + ny * (k + 1) + j);
        cells[index * 32 + 23] = cells[index * 32 + 22] + 1;

        // 04.cell里edge z处的8个node对应的索引值
        offset += 2 * ne_y;
        cells[index * 32 + 24] = offset + 2 * (nz * (nx + 1) * j + nz * i + k);
        cells[index * 32 + 25] = cells[index * 32 + 24] + 1;
        cells[index * 32 + 26] = offset + 2 * (nz * (nx + 1) * (j + 1) + nz * i + k);
        cells[index * 32 + 27] = cells[index * 32 + 26] + 1;
        cells[index * 32 + 28] = offset + 2 * (nz * (nx + 1) * j + nz * (i + 1) + k);
        cells[index * 32 + 29] = cells[index * 32 + 28] + 1;
        cells[index * 32 + 30] = offset + 2 * (nz * (nx + 1) * (j + 1) + nz * (i + 1) + k);
        cells[index * 32 + 31] = cells[index * 32 + 30] + 1;
    }


    void InitArrays()
    {
        // 初始化数值nodeLocalPosition
        nodeLocalPosition = new Vector3[numberOfNodes];
        Parallel.For(0, numberOfNodes, i => {
            nodeLocalPosition[i] = NodeLogicPosToPhyPos(i);
        });

        // 设置cells数组的值
        cells = new int[numberOfCells * 32];
        for (int i = 0; i < numberOfCells; i++)
        {
            NodeToCell(i);
        }


        // start 

        string filePath = Application.dataPath + "/cache/" + cacheName;

        try  // 如果文件存在，直接读取
        {  
            using (FileStream stream = new FileStream(filePath, FileMode.Open))  
            {  
                BinaryFormatter formatter = new BinaryFormatter();  
                // 反序列化第一个数组  
                sdfNode = (float[])formatter.Deserialize(stream);  
                // 反序列化第二个数组  
                volumeNode = (float[])formatter.Deserialize(stream);  

                Debug.Log("cache加载完成");

                return;            
            }  
        }  
        catch (FileNotFoundException ex)  // 处理文件不存在的异常  ：如果文件不存在，先算再存
        {  

            // 插桩，开始计时
            System.DateTime start = System.DateTime.Now;

            // 初始化数组sdfNode
            sdfNode = new float[numberOfNodes];
            Parallel.For(0, numberOfNodes, i =>
            {
                // 这些，最早是c++的，本来就和unity不相关……所以，可以用C#的并行
                sdfNode[i] = td.Distance_from_point_to_mesh_numerical(nodeLocalPosition[i]);
            });

            // 插值，结束计时
            System.DateTime end = System.DateTime.Now;
            Debug.Log("计算sdf用时：" + (end - start));

            // 插桩，开始计时
            start = System.DateTime.Now;

            // 初始化数组volumeNode [数值积分]
            volumeNode = new float[numberOfNodes];

            // 提前构造random sample in sphere array,所有节点的计算都统一引用这一个数组。
            // 这里的球，球心在原点，半径长度为1.实际使用的时候需要按需调整一下
            // int totalNum = 16*16*16;
            int totalNum = 16*16*16;
            Vector3[] sampleBasic = new Vector3[totalNum];
            System.Random random = new System.Random();
            for(int i=0;i<totalNum;i++){
                float phi = 0 + 2*Mathf.PI*(float)random.NextDouble(); //从[0,1]到[0,2*pi]
                float sita = Mathf.Acos(-1 + 2*(float)random.NextDouble());
                float r = 1.0f * Mathf.Pow((float)random.NextDouble(),1.0f/3.0f); // 按r=1.0的标准的球来的，用的时候按需缩放一下就行了
                sampleBasic[i] = new Vector3(
                    r * Mathf.Cos(phi) * Mathf.Sin(sita),
                    r * Mathf.Cos(sita),
                    r * Mathf.Sin(phi) * Mathf.Sin(sita)
                );
            }



            Parallel.For(0, numberOfNodes, i =>
            {
                volumeNode[i] = ComputeVolumeByMC_sampleInSphere_soft(i,ref sampleBasic);
            });

            // 插值，结束计时
            end = System.DateTime.Now;

            Debug.Log("计算volume map用时：" + (end - start));

            // 把sdf和volume这2个大数组给序列化到硬盘里

            // 创建一个二进制格式化器  
            BinaryFormatter formatterWrite = new BinaryFormatter();  
    
            // 创建一个文件流  
            using (FileStream stream = new FileStream(filePath, FileMode.Create))  
            {  
                // 序列化第一个数组  
                formatterWrite.Serialize(stream, sdfNode);  
    
                // 序列化第二个数组  
                formatterWrite.Serialize(stream, volumeNode);  
            }

            Debug.Log("两个float数组序列化已完成");
        }  


        // end

    }

    /// <summary>
    /// 这里用到的随机数，是在函数外构造，然后当引用传进来的.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    float ComputeVolumeByMC_sampleInSphere_soft(int index,ref Vector3[] sampleBasic){

        float result = 0;

        // float supportRadius = 1.2f;

        // supportRadius *= 2.0f; // 扩大体积节点半径到sph粒子支持半径的1.5倍，尝试减少all in volume nodes造成的伪影

        // float nodeRadius = Parameter.nodeRadius;
        float nodeRadius =1.2f;

        // 有一些情况，是不需要计算的
        float distance = sdfNode[index];
        if(distance < -1.0f * nodeRadius){
            return (4.0f / 3.0f) * Mathf.PI * Mathf.Pow(nodeRadius, 3);
        }

        if(distance > 1.0f * nodeRadius){
            return 0;
        }

        float insideNum = 0;
        int totalNum = sampleBasic.Length; // 球内均匀采样，采样点的总数提前就能知道，对于接受拒绝采样来说，这是个区别

        for(int i=0;i<totalNum;i++){

            Vector3 offset = nodeRadius * sampleBasic[i]; // 调节，使球的大小匹配

            // 当前采样点在模型局部坐标系下的坐标
            Vector3 samplePos = nodeLocalPosition[index] + offset; // 调节，使球的位置匹配

            //边缘的node，其支持域不全在AABB包围盒内，没有sdf值，需要剔除这部分对应的采样点
            if (!InDomain(samplePos))
            {
                continue; // AABB的外面，是空，volume当然是0了
            }

            // 把特殊情况过滤掉以后，就可以正式计算了
            float sdf = Interpolate(samplePos);

            // if(sdf < 0){
            //     insideNum++;
            // }

            float nodeExtendRadius = 0.15f; // 采样点扩展出来一个虚拟的“半径”，用来缓解数值积分+01边界造成的伪影
            if(sdf<-nodeExtendRadius){
                insideNum += 1;
            }else if(sdf<+nodeExtendRadius){ // sdf - (-nodeRadius) = sdf + nodeRadius
                // insideNum += Mathf.Lerp(1.0f,0.0f,(sdf+nodeExtendRadius)/(2*nodeExtendRadius));  //api介绍，详见：https://docs.unity3d.com/ScriptReference/Mathf.Lerp.html
                float t = (sdf+nodeExtendRadius)/(2*nodeExtendRadius);
                insideNum += (Mathf.Cos(Mathf.PI * t)+1)/2; // 按cos计算，cos比linear更光滑，而且很接近球缺
            }else{
                insideNum += 0;
            }

        }

        result = (insideNum * 1.0f / totalNum) * (4.0f / 3.0f) * Mathf.PI * Mathf.Pow(nodeRadius, 3);

        return result;

    }



    /// <summary>
    /// xi是标准cell下的坐标，函数返回值是一个表示权重的数组。长度为32，对应插值时用到的32个node的32个权重
    /// </summary>
    /// <param name="xi"></param>
    /// <returns></returns>
    private float[] ShapeFunction(Vector3 xi) // C# 可以返回数组的，详见：https://blog.csdn.net/qq_43548464/article/details/100936802
    {
        float[] result = new float[32];

        // 出现的变量的计算
        float x = xi[0];
        float y = xi[1];
        float z = xi[2];

        float x2 = x * x;
        float y2 = y * y;
        float z2 = z * z;

        float _1mx = 1.0f - x;
        float _1my = 1.0f - y;
        float _1mz = 1.0f - z;

        float _1px = 1.0f + x;
        float _1py = 1.0f + y;
        float _1pz = 1.0f + z;

        float _1m3x = 1.0f - 3.0f * x;
        float _1m3y = 1.0f - 3.0f * y;
        float _1m3z = 1.0f - 3.0f * z;

        float _1p3x = 1.0f + 3.0f * x;
        float _1p3y = 1.0f + 3.0f * y;
        float _1p3z = 1.0f + 3.0f * z;

        float _1mxt1my = _1mx * _1my;
        float _1mxt1py = _1mx * _1py;
        float _1pxt1my = _1px * _1my;
        float _1pxt1py = _1px * _1py;

        float _1mxt1mz = _1mx * _1mz;
        float _1mxt1pz = _1mx * _1pz;
        float _1pxt1mz = _1px * _1mz;
        float _1pxt1pz = _1px * _1pz;

        float _1myt1mz = _1my * _1mz;
        float _1myt1pz = _1my * _1pz;
        float _1pyt1mz = _1py * _1mz;
        float _1pyt1pz = _1py * _1pz;

        float _1mx2 = 1.0f - x2;
        float _1my2 = 1.0f - y2;
        float _1mz2 = 1.0f - z2;

        // Corner nodes.
        float fac = 1.0f / 64.0f * (9.0f * (x2 + y2 + z2) - 19.0f);
        result[0] = fac * _1mxt1my * _1mz;
        result[1] = fac * _1pxt1my * _1mz;
        result[2] = fac * _1mxt1py * _1mz;
        result[3] = fac * _1pxt1py * _1mz;
        result[4] = fac * _1mxt1my * _1pz;
        result[5] = fac * _1pxt1my * _1pz;
        result[6] = fac * _1mxt1py * _1pz;
        result[7] = fac * _1pxt1py * _1pz;

        // Edge nodes.

        // section1
        fac = 9.0f / 64.0f * _1mx2;
        float fact1m3x = fac * _1m3x;
        float fact1p3x = fac * _1p3x;
        result[8] = fact1m3x * _1myt1mz;
        result[9] = fact1p3x * _1myt1mz;
        result[10] = fact1m3x * _1myt1pz;
        result[11] = fact1p3x * _1myt1pz;
        result[12] = fact1m3x * _1pyt1mz;
        result[13] = fact1p3x * _1pyt1mz;
        result[14] = fact1m3x * _1pyt1pz;
        result[15] = fact1p3x * _1pyt1pz;

        // section2
        fac = 9.0f / 64.0f * _1my2;
        float fact1m3y = fac * _1m3y;
        float fact1p3y = fac * _1p3y;
        result[16] = fact1m3y * _1mxt1mz;
        result[17] = fact1p3y * _1mxt1mz;
        result[18] = fact1m3y * _1pxt1mz;
        result[19] = fact1p3y * _1pxt1mz;
        result[20] = fact1m3y * _1mxt1pz;
        result[21] = fact1p3y * _1mxt1pz;
        result[22] = fact1m3y * _1pxt1pz;
        result[23] = fact1p3y * _1pxt1pz;

        // section3
        fac = 9.0f / 64.0f * _1mz2;
        float fact1m3z = fac * _1m3z;
        float fact1p3z = fac * _1p3z;
        result[24] = fact1m3z * _1mxt1my;
        result[25] = fact1p3z * _1mxt1my;
        result[26] = fact1m3z * _1mxt1py;
        result[27] = fact1p3z * _1mxt1py;
        result[28] = fact1m3z * _1pxt1my;
        result[29] = fact1p3z * _1pxt1my;
        result[30] = fact1m3z * _1pxt1py;
        result[31] = fact1p3z * _1pxt1py;

        return result;
    }

    /// <summary>
    /// 根据node，对x位置处的SDF进行插值；x是obj局部坐标系下的坐标，不是世界坐标系下的坐标
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public float Interpolate(Vector3 x)
    {
        float result = 0;

        if (!domain.Contains(x)) // 第一次用，不知道对不对：https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Bounds.Contains.html
        {
            result = float.MaxValue;
            return result;
        }

        Vector3 cellIndex = x - domain.min;
        for (int i = 0; i < 3; i++) 
            cellIndex[i] = cellIndex[i] / cellSize[i];

        Vector3Int mi = new Vector3Int((int)cellIndex.x, (int)cellIndex.y, (int)cellIndex.z); // 点x所在的cell的序号

        for(int i = 0; i < 3; i++)
        {
            if (mi[i] >= resolution[i]) // float，是不大准的
            {
                mi[i] = resolution[i] - 1;
            }
            if (mi[i] < 0)
            {
                mi[i] = 0;
            }
        }

        //int cellIndex1D = resolution.x * resolution.y * mi[2] + resolution.x * mi[1] + resolution.x; // 把[x,y,z]展开成1维的index
        int cellIndex1D = resolution.x * resolution.y * mi[2] + resolution.x * mi[1] + mi[0]; // 难怪越界

        Vector3 subDomainMin = domain.min + new Vector3(mi.x * cellSize.x, mi.y * cellSize.y, mi.z * cellSize.z);
        //Vector3 subDomainMax = domain.min + new Vector3((mi.x + 1) * cellSize.x, (mi.y + 1) * cellSize.y, (mi.z + 1) * cellSize.z);
        Vector3 subDomainMax = subDomainMin + cellSize; // 这么写，不是更简洁吗？
        Vector3 subDomainCenter = (subDomainMin + subDomainMax) / 2;
        //Vector3 subDomainSize = subDomainMax - subDomainMin;
        Vector3 subDomainSize = cellSize; // 这么写，能少点计算量
        Bounds subDomain = new Bounds(subDomainCenter, subDomainSize); // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Bounds-ctor.html

        // 开始几何变换，把点的坐标x映射到大小为2*2*2且坐标原点在中心的标准cell中去
        Vector3 denom = (subDomain.max - subDomain.min);
        Vector3 c0 = new Vector3(2.0f / denom.x, 2.0f / denom.y, 2.0f / denom.z); // cell大小的走样程度，用于缩放变换
        Vector3 c1 = new Vector3(c0.x * subDomainCenter.x, c0.y * subDomainCenter.y, c0.z * subDomainCenter.z);
        Vector3 xi = new Vector3(c0.x * x[0], c0.y * x[1], c0.z * x[2]) - c1; // 标准cell下的坐标xi

        // 根据xi，计算出32个权重
        float[] N = ShapeFunction(xi);

        for(int j = 0; j < 32; j++)
        {
            int v = cells[cellIndex1D * 32 + j]; // IndexOutOfRangeException: Index was outside the bounds of the array.
            float c = sdfNode[v];
            result += N[j] * c;
        }

        return result;
    }


    /// <summary>
    /// position可能在离散网格的外面，这个时候不能做中心差分，这个函数就是个筛子，把不能做中心差分的情况都阻拦在这儿；
    /// position是局部坐标系下的点的坐标，不是世界坐标系下的
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool InDomain(Vector3 position)
    {
        bool result = true;

        float eps = 1e-2f; // 需要和中心差分里的eps保持一致

        // 判断position是否在domain内【留出余量，给中心差分的eps】
        for (int i = 0; i < 3; i++)
        {
            if (position[i] < domain.min[i] + 2 * eps)
            {
                result = false;
            }
            if (position[i] > domain.max[i] - 2 * eps)
            {
                result = false;
            }
        }

        return result;
    }


}



}