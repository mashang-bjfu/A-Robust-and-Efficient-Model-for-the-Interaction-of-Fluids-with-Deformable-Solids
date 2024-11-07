using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

namespace exp3{


/// <summary>
/// 数据流：该脚本->ModelMain->Main->computeShader.
/// 用于计算顶点的邻居顶点，计算六面体体积时会用到。
/// </summary>
/// 
public class NeighbourVertexList
{
    /// <summary>
    /// 一维数组，方便HLSL调用
    /// </summary>
    public Vector2Int[] neighbourVertexList = null;
    /// <summary>
    /// 预设的每个顶点的最大邻居顶点数，决定了数组的第2个维度的长度
    /// </summary>
    public int maxNeighbourNumPerVertex = 0;

    /// <summary>
    /// 构造函数，在ModelMain里被调用.
    /// </summary>
    public NeighbourVertexList(Mesh mesh)
    {
        // Mesh mesh = GetComponent<MeshFilter>().mesh; // 从Awake到构造函数，最小化修改，改的越少越安全额.本来是这样的，现在改成从构造函数的形参传过来.

        int[] triangles = mesh.triangles; // api: https://docs.unity3d.com/ScriptReference/Mesh-triangles.html


        // 分2步，初始化二维list
        List<List<Vector2Int>> tempList = new List<List<Vector2Int>>();
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            tempList.Add(new List<Vector2Int>());
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            // 根据unity的文档，顶点是按逆时针排列的，具体顺序是vertexIndex1,2,3.
            int vertexIndex1 = triangles[i];
            int vertexIndex2 = triangles[i + 1];
            int vertexIndex3 = triangles[i + 2];

            // tempList[vertexIndex1].Add(new Vector2Int(vertexIndex2, vertexIndex3)); // v1——v2,v1——v3.根据这2条边   v1的邻居有v2,v3

            // tempList[vertexIndex2].Add(new Vector2Int(vertexIndex1, vertexIndex3)); // 只能保证连接关系是对的，具体顺序是乱的

            // tempList[vertexIndex3].Add(new Vector2Int(vertexIndex1, vertexIndex2));

            // 把顶点按逆时针排列
            tempList[vertexIndex1].Add(new Vector2Int(vertexIndex2, vertexIndex3));
            tempList[vertexIndex2].Add(new Vector2Int(vertexIndex3, vertexIndex1));
            tempList[vertexIndex3].Add(new Vector2Int(vertexIndex1, vertexIndex2));

        }



        maxNeighbourNumPerVertex = tempList.Max(list => list.Count);

        neighbourVertexList = new Vector2Int[mesh.vertexCount * maxNeighbourNumPerVertex]; // 初始值全是[0,0] 

        Debug.Log("maxNeighbourNumPerVertex = " + maxNeighbourNumPerVertex);    // 事实证明，有个输出还是必要的；有了这个输出，代码忘了的时候至少还可以顺藤摸瓜

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            for (int j = 0; j < tempList[i].Count; j++) // 未被赋值的部分，依旧是初值[0,0]
            {
                neighbourVertexList[i * maxNeighbourNumPerVertex + j] = tempList[i][j];
            }
        }

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