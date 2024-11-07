using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using System;

namespace exp3{



/// <summary>
/// 数据流：该脚本->ModelMain->Main->computeShader.
/// 根据顶点移动，对volume node进行更新；可以并行计算；但是不建议直接并行计算，那个是完全随机乱序的。先按连接关系分个组，误差应该会更小.
/// 这个类就是在预计算阶段，对模型顶点进行分组的.
/// </summary>

public class VertexGroup
{
    /// <summary>
    /// 对顶点进行分组操作以后，顶点组的数量。
    /// </summary>
    public int numOfVertexGroup;

    /// <summary>
    /// 长度还是模型顶点的数量，里面存的是分了组的顶点索引。
    /// 不分组的情况下，直接从0依次加到顶点数-1就完了.这里为什么额外写呢？原因同https://zhuanlan.zhihu.com/p/563182093
    /// </summary>
    public int[] vertexGroupArray = null;

    /// <summary>
    /// 前缀和是可以用来记录每段起始索引的.
    /// 比如，长度为10的数组，分了3段每段的长度依次是2,5,3
    /// 那么，该数组的长度就是4，具体内容就是0,2,7,10。
    /// 访问第1段，就是[0,2)
    /// 访问第2段，就是[2,7)
    /// 访问第3段，就是[7,10)
    /// </summary>
    public int[] vertexGroupPrefixSumArray = null;

    /// <summary>
    /// 构造函数，在ModelMain里被调用.
    /// </summary>
    public VertexGroup(Mesh mesh){

        // 01.根据边的连接关系，构建查找表，备用

        // Mesh mesh = GetComponent<MeshFilter>().mesh;

        // 顶点的连接情况，vct[i] ={j,k,m,n},就表示第i个顶点和第j,k,m,n个顶点都存在连接关系
        List<List<int>> vertexConnectTable = new List<List<int>>();
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            vertexConnectTable.Add(new List<int>());
        }

        int[] triangles = mesh.triangles; 
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // 根据unity的文档，顶点是按逆时针排列的，具体顺序是vertexIndex1,2,3.
            int vertexIndex1 = triangles[i + 0];
            int vertexIndex2 = triangles[i + 1];
            int vertexIndex3 = triangles[i + 2];

            // 相邻三角形，是存在公共顶点的，这么干，连接关系可能会重复记录的
            // 按边记录试试
            // [1,2]    [2,3]   [3,1]
            // 1->2     2->3    3->1
            // 如果是水密网格的话，每个边都会被刚好遍历两次，且遍历顺序相反
            vertexConnectTable[vertexIndex1].Add(vertexIndex2);
            vertexConnectTable[vertexIndex2].Add(vertexIndex3);
            vertexConnectTable[vertexIndex3].Add(vertexIndex1);
        }

        // 02.对顶点进行分组操作

        // 第一维对应组序号，第二维对应组内第几个顶点
        // 刚开始，组数未知，没法初始化第二维，所以只初始化了第一维
        List<List<int>> vertexGroupList = new List<List<int>>();

        // 尚未被分组的顶点，刚开始的时候所有顶点都没有被分组，所以按顺序初始化就行了
        List<int> unGroupedVertices = new List<int>(Enumerable.Range(0,mesh.vertexCount));
        List<int> currentGroup = null;
        while(unGroupedVertices.Count!=0){
            currentGroup = new List<int>(unGroupedVertices.ToArray()); // 通过深拷贝来粗糙的初始化，用法详见：https://blog.csdn.net/keneyr/article/details/88263962
            unGroupedVertices.Clear();

            // 去粗取精，去伪存真
            for(int i=0;i<currentGroup.Count;i++){
                int vertexIndex = currentGroup[i];

                // foreach比for循环写起来更简便。用foreach的时候，不能修改；这里也用不到修改，模型拓扑结构不变，顶点连接关系也不变的
                foreach(int adjacentVertexIndex in vertexConnectTable[vertexIndex]){
                    if(currentGroup.Remove(adjacentVertexIndex)){ // 如果有，只会出现在vertexIndex的右边
                        unGroupedVertices.Add(adjacentVertexIndex);
                    }else{
                        continue;
                    }
                }
            }

            // vertexGroupList.Add(currentGroup); // 可能是浅拷贝
            vertexGroupList.Add(new List<int>(currentGroup.ToArray()));
        }
            // 看看结果

            numOfVertexGroup = vertexGroupList.Count;

            Debug.Log("顶点组的数量是："+ numOfVertexGroup);

            int totalCount = 0;  
            
            foreach (List<int> vertexGroup in vertexGroupList)  
            {  
                totalCount += vertexGroup.Count;  
            }  
            
            Debug.Log("顶点组中顶点的总数：" + totalCount);

        // 03.保存数据到static数组，备用
        vertexGroupArray = new int[mesh.vertexCount]; // 怎么把数据从二维list拷进来额？
        vertexGroupPrefixSumArray = new int[vertexGroupList.Count + 1];

        for(int i=0;i<vertexGroupList.Count;i++){ // 根据vertexGroupList，计算前缀和
            vertexGroupPrefixSumArray[i+1] = vertexGroupPrefixSumArray[i] + vertexGroupList[i].Count;
        }

        for(int i=0;i<vertexGroupList.Count;i++){
            int startIndex = vertexGroupPrefixSumArray[i]; // 左闭右开的区间，即[startIndex,endIndex)
            int endIndex = vertexGroupPrefixSumArray[i+1];
            int length = endIndex - startIndex;
            Array.Copy(vertexGroupList[i].ToArray(),0,vertexGroupArray,startIndex,length); // https://blog.csdn.net/jiao1902676909/article/details/89363608    https://blog.csdn.net/qq826364410/article/details/79729727
        }

        // // 对比，看看顺序，乱序，cache miss对帧率是否存在影响.
        // for(int i=0;i<mesh.vertexCount;i++){
        //     vertexGroupArray[i] = i;
        // }   

    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

        
}

}