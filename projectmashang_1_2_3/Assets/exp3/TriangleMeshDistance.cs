
namespace exp3{


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 构造的时候，输入mesh的三角形数组
/// 用的时候，输入point，返回sdf值
/// </summary>
public class TriangleMeshDistance
{
    //成员的默认访问标识符是 private

    // 输入的数据
    int vertexNum;
    int triangleNum;
    private Vector3[] vertices;
    int[] triangles;

    // 计算伪法线需要的辅助数据
    enum NearestEntity { V0, V1, V2, E01, E12, E02, F };
    struct Result
    {
        public int triangle_id; // mesh上距离最近的点所在的三角形的索引号 

        public float distance; // 最近的无符号距离
        public Vector3 nearest_point; // mesh上距离最近的点的坐标
        public NearestEntity nearest_entity;
    };
    Hashtable ht_edge_normal = null;
    Hashtable ht_edge_count = null;

    // 伪法线计算结果保存的地方
    Vector3[] pseudonormals_triangles = null; // mesh的面片部分
    Vector3[] pseudonormals_vertices = null; // mesh的顶点部分
    Vector3[] pseudonormals_edges = null; // 

    void AddEdgeNormal(int i, int j, Vector3 triangleNormal)
    {
        int key = Mathf.Min(i, j) * vertexNum + Mathf.Max(i, j);

        if (!ht_edge_normal.ContainsKey(key)) // 这个和tmd是反的，不加！，会报错，空引用什么的
        {
            ht_edge_normal[key] = triangleNormal;
            ht_edge_count[key] = 1;
        }
        else
        {
            ht_edge_normal[key] = (Vector3)ht_edge_normal[key] + triangleNormal;
            ht_edge_count[key] = (int)ht_edge_count[key] + 1;
        }
    }

    Vector3 GetEdgeNormal(int i, int j)
    {
        int key = Mathf.Min(i, j) * vertexNum + Mathf.Max(i, j);
        return (Vector3)ht_edge_normal[key];
    }

    /// <summary>
    /// 伪法线的预计算
    /// </summary>
    void ComputePseudonormals()
    {
        pseudonormals_triangles = new Vector3[triangleNum];
        pseudonormals_vertices = new Vector3[vertexNum]; for (int i = 0; i < vertexNum; i++) pseudonormals_vertices[i] = Vector3.zero;
        pseudonormals_edges = new Vector3[triangleNum * 3];

        // triangle
        for (int i = 0; i < triangleNum; i++)
        {
            Vector3 a = vertices[triangles[i * 3 + 0]];
            Vector3 b = vertices[triangles[i * 3 + 1]];
            Vector3 c = vertices[triangles[i * 3 + 2]];

            pseudonormals_triangles[i] = Vector3.Cross((b - a), (c - a)).normalized;
        }

        // vertex 
        for (int i = 0; i < triangleNum; i++)
        {
            Vector3 a = vertices[triangles[i * 3 + 0]];
            Vector3 b = vertices[triangles[i * 3 + 1]];
            Vector3 c = vertices[triangles[i * 3 + 2]];

            float alpha_0 = Mathf.Acos(Vector3.Dot(Vector3.Normalize(b - a), Vector3.Normalize(c - a)));
            float alpha_1 = Mathf.Acos(Vector3.Dot(Vector3.Normalize(a - b), Vector3.Normalize(c - b)));
            float alpha_2 = Mathf.Acos(Vector3.Dot(Vector3.Normalize(a - c), Vector3.Normalize(b - c)));

            pseudonormals_vertices[triangles[i * 3 + 0]] += alpha_0 * pseudonormals_triangles[i];
            pseudonormals_vertices[triangles[i * 3 + 1]] += alpha_1 * pseudonormals_triangles[i];
            pseudonormals_vertices[triangles[i * 3 + 2]] += alpha_2 * pseudonormals_triangles[i];
        }
        for (int i = 0; i < vertexNum; i++) pseudonormals_vertices[i] = pseudonormals_vertices[i].normalized;

        ht_edge_normal = new Hashtable();
        ht_edge_count = new Hashtable();
        // edge
        for (int i = 0; i < triangleNum; i++)
        {
            AddEdgeNormal(triangles[i * 3 + 0], triangles[i * 3 + 1], pseudonormals_triangles[i]);
            AddEdgeNormal(triangles[i * 3 + 1], triangles[i * 3 + 2], pseudonormals_triangles[i]);
            AddEdgeNormal(triangles[i * 3 + 0], triangles[i * 3 + 2], pseudonormals_triangles[i]);
        }
        for (int i = 0; i < triangleNum; i++)
        {
            pseudonormals_edges[i * 3 + 0] = GetEdgeNormal(triangles[i * 3 + 0], triangles[i * 3 + 1]).normalized;
            pseudonormals_edges[i * 3 + 1] = GetEdgeNormal(triangles[i * 3 + 1], triangles[i * 3 + 2]).normalized;
            pseudonormals_edges[i * 3 + 2] = GetEdgeNormal(triangles[i * 3 + 0], triangles[i * 3 + 2]).normalized;
        }
    }

    public TriangleMeshDistance(int vertexNum,Vector3[] vertices,int triangleNum,int[] triangles)
    {
        this.vertexNum = vertexNum;
        this.vertices = vertices;
        this.triangleNum = triangleNum;
        this.triangles = triangles;

        ComputePseudonormals();
    }

    /// <summary>
    /// 具体实现，是个for循环，把mesh的三角形给循环了一遍
    /// </summary>
    /// <param name="point">点到mesh的最近带符号距离，point就表示那个点</param>
    /// <returns></returns>
    public float Distance_from_point_to_mesh_numerical(Vector3 point)
    {
        Result result;
        result.triangle_id = -1;
        result.distance = float.MaxValue;
        result.nearest_point = Vector3.zero;
        result.nearest_entity = NearestEntity.F;


        for (int i = 0; i < triangleNum; i++)
        {
            NearestEntity nearest_entity = NearestEntity.F;
            Vector3 nearest_point = Vector3.zero;

            float temp = ComputeDistance(ref nearest_entity, ref nearest_point, point, vertices[triangles[i * 3 + 0]], vertices[triangles[i * 3 + 1]], vertices[triangles[i * 3 + 2]]);
            temp = Mathf.Sqrt(temp);

            if (temp < result.distance)
            {
                result.triangle_id = i; // 根据这两个，就能计算伪法线了
                result.nearest_entity = nearest_entity;

                result.distance = temp;
                result.nearest_point = nearest_point;

            }
        }

        Vector3 pseudonormal = Vector3.zero;
        switch (result.nearest_entity)
        {
            case NearestEntity.F:
                pseudonormal = pseudonormals_triangles[result.triangle_id];
                break;
            case NearestEntity.V0:
                pseudonormal = pseudonormals_vertices[triangles[result.triangle_id * 3 + 0]];
                break;
            case NearestEntity.V1:
                pseudonormal = pseudonormals_vertices[triangles[result.triangle_id * 3 + 1]];
                break;
            case NearestEntity.V2:
                pseudonormal = pseudonormals_vertices[triangles[result.triangle_id * 3 + 2]];
                break;
            case NearestEntity.E01:
                pseudonormal = pseudonormals_edges[result.triangle_id * 3 + 0];
                break;
            case NearestEntity.E12:
                pseudonormal = pseudonormals_edges[result.triangle_id * 3 + 1];
                break;
            case NearestEntity.E02:
                pseudonormal = pseudonormals_edges[result.triangle_id * 3 + 2];
                break;
        }

        Vector3 u = point - result.nearest_point;

        return Mathf.Sign(Vector3.Dot(u, pseudonormal)) * result.distance;
    }

    void ComputeDistance_Region0(ref NearestEntity nearest_entity, ref float s, ref float t, float det)
    {
        nearest_entity = NearestEntity.F;
        s /= det;
        t /= det;
    }

    void ComputeDistance_Region1(ref NearestEntity nearest_entity, ref float s, ref float t, float a, float b, float c, float d, float e)
    {
        float numer = (c + e) - (b + d);
        if (numer <= 0)
        {
            nearest_entity = NearestEntity.V2;
            s = 0;
        }
        else
        {
            float denom = a - 2 * b + c;
            if (numer >= denom)
            {
                nearest_entity = NearestEntity.V1;
                s = 1;
            }
            else
            {
                nearest_entity = NearestEntity.E12;
                s = numer / denom;
            }
        }
        t = 1 - s;
    }

    void ComputeDistance_Region3(ref NearestEntity nearest_entity, ref float s, ref float t, float c, float e)
    {
        s = 0;
        if (e >= 0)
        {
            nearest_entity = NearestEntity.V0;
            t = 0;
        }
        else if (-e >= c)
        {
            nearest_entity = NearestEntity.V2;
            t = 1;
        }
        else
        {
            nearest_entity = NearestEntity.E02;
            t = -e / c;
        }
    }

    void ComputeDistance_Region5(ref NearestEntity nearest_entity, ref float s, ref float t, float a, float d)
    {
        t = 0;
        if (d >= 0)
        {
            nearest_entity = NearestEntity.V0;
            s = 0;
        }
        else if (-d >= a)
        {
            nearest_entity = NearestEntity.V1;
            s = 1;
        }
        else
        {
            nearest_entity = NearestEntity.E01;
            s = -d / a;
        }
    }

    void ComputeDistance_Region2(ref NearestEntity nearest_entity, ref float s, ref float t, float a, float b, float c, float d, float e)
    {
        float tmp0 = b + d;
        float tmp1 = c + e;
        if (tmp1 > tmp0)
        {
            float numer = tmp1 - tmp0;
            float denom = a - 2 * b + c;
            if (numer > denom)
            {
                nearest_entity = NearestEntity.V1;
                s = 1;
            }
            else
            {
                nearest_entity = NearestEntity.E12;
                s = numer / denom;
            }
            t = 1 - s;
        }
        else
        {
            s = 0;
            if (tmp1 <= 0)
            {
                nearest_entity = NearestEntity.V2;
                t = 1;
            }
            else if (e >= 0)
            {
                nearest_entity = NearestEntity.V0;
                t = 0;
            }
            else
            {
                nearest_entity = NearestEntity.E02;
                t = -e / c;
            }
        }
    }

    void ComputeDistance_Region6(ref NearestEntity nearest_entity, ref float s, ref float t, float a, float b, float c, float d, float e)
    {
        float tmp0 = b + e;
        float tmp1 = a + d;

        if (tmp1 > tmp0)
        {
            float numer = tmp1 - tmp0;
            float denom = a - 2 * b + c;

            if (numer >= denom)
            {
                nearest_entity = NearestEntity.V2;
                t = 1;
            }
            else
            {
                nearest_entity = NearestEntity.E12;
                t = numer / denom;
            }
            s = 1 - t;

        }
        else
        {
            t = 0;
            if (tmp1 <= 0)
            {
                nearest_entity = NearestEntity.V1;
                s = 1;
            }
            else if (d >= 0)
            {
                nearest_entity = NearestEntity.V0;
                s = 0;
            }
            else
            {
                nearest_entity = NearestEntity.E01;
                s = -d / a;
            }
        }
    }

    void ComputeDistance_Region4(ref NearestEntity nearest_entity, ref float s, ref float t, float a, float b, float c, float d, float e)
    {
        if (d < 0)
        {
            t = 0;
            if (-d >= a)
            {
                nearest_entity = NearestEntity.V1;
                s = 1;
            }
            else
            {
                nearest_entity = NearestEntity.E01;
                s = -d / a;
            }
        }
        else
        {
            s = 0;
            if (e >= 0)
            {
                nearest_entity = NearestEntity.V0;
                t = 0;
            }
            else if (-e >= c)
            {
                nearest_entity = NearestEntity.V2;
                t = 1;
            }
            else
            {
                nearest_entity = NearestEntity.E02;
                t = -e / c;
            }

        }
    }


    /// <summary>
    /// 计算点point到p0,p1,p2组成的三角形的最近距离的平方值[标量]
    /// -refe https://www.geometrictools.com/Documentation/DistancePoint3Triangle3.pdf
    /// </summary>
    /// <param name="nearest_entity"></param>
    /// <param name="nearest_point"></param>
    /// <param name="point"></param>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    float ComputeDistance(ref NearestEntity nearest_entity, ref Vector3 nearest_point, Vector3 point, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float distance2 = 0;//最终结果,距离的平方；很明显，是没有符号的
        Vector3 P = point; // 所有字母，一律按论文里来
        Vector3 B = p0;
        Vector3 E0 = p1 - p0;
        Vector3 E1 = p2 - p0;

        float a = Vector3.Dot(E0, E0);
        float b = Vector3.Dot(E0, E1);
        float c = Vector3.Dot(E1, E1);
        float d = Vector3.Dot(E0, (B - P));
        float e = Vector3.Dot(E1, (B - P));
        float f = Vector3.Dot((B - P), (B - P));

        float det = Mathf.Abs(a * c - b * b); // 数学上是恒正的，但是float不一定是，加个abs，确保一下

        float s = b * e - c * d; // 仅仅是分母部分，但是也够用
        float t = b * d - a * e;

        if (s + t <= det) // region 4,3,5,0
        {
            if (s < 0)// region 4,3
            {
                if (t < 0) // region 4
                {
                    ComputeDistance_Region4(ref nearest_entity, ref s, ref t, a, b, c, d, e);
                }
                else //region 3
                {
                    ComputeDistance_Region3(ref nearest_entity, ref s, ref t, c, e);
                }
            }
            else // region 5,0
            {
                if (t < 0) // region 5
                {
                    ComputeDistance_Region5(ref nearest_entity, ref s, ref t, a, d);
                }
                else // region 0
                {
                    ComputeDistance_Region0(ref nearest_entity, ref s, ref t, det);
                }
            }
        }
        else // region 2,6,1
        {
            if (s < 0) // region 2
            {
                ComputeDistance_Region2(ref nearest_entity, ref s, ref t, a, b, c, d, e);
            }
            else
            {
                if (t < 0) // region 6
                {
                    ComputeDistance_Region6(ref nearest_entity, ref s, ref t, a, b, c, d, e);
                }
                else // region 1
                {
                    ComputeDistance_Region1(ref nearest_entity, ref s, ref t, a, b, c, d, e);
                }
            }
        }

        nearest_point = B + s * E0 + t * E1;

        distance2 = (nearest_point - point).sqrMagnitude;

        return distance2;
    }
}


}

