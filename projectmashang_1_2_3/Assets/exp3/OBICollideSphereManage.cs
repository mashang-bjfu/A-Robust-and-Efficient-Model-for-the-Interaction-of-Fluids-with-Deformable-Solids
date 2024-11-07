using System.Collections;
using System.Collections.Generic;
// using test21;
using UnityEngine;

namespace exp3{

public class OBICollideSphereManage : MonoBehaviour
{
    /// <summary>
    /// 通过取引用来赋值，具体的值在FluidMain里
    /// </summary>
    Vector3[] positions = null;
    /// <summary>
    /// 通过取引用来赋值，具体的值在FluidMain里.
    /// </summary>
    Vector4[] virtualBoundaryParticlesData = null;

    /// <summary>
    /// 绑定了obi collider的预制体.
    /// </summary>
    public GameObject obiSphere;

    /// <summary>
    /// 对象池数组的长度.
    /// </summary>
    public static int maxNum = 100;

    /// <summary>
    /// 对象池数组当前元素的数量.
    /// </summary>
    public static int curNum = 0;

    /// <summary>
    /// 足够靠近固体边界，对固体边界存在作用力的粒子；它和下面的相加，总数恒等于maxnum.
    /// int表示obi sphere在positions数组里的索引，gameobject表示从prefab实例化的obi sphere.
    /// </summary>
    Dictionary<int,GameObject> sphereActive = new Dictionary<int,GameObject>();

    /// <summary>
    /// 非常远离固体边界，对固体边界不存在作用力的粒子；它和上面的相加，总数恒等于maxnum.
    /// </summary>
    Stack<GameObject> sphereInactive = new Stack<GameObject>(); 

    /// <summary>
    /// 根据vbpd里的体积值进行判断：如果positions[particleIndex]靠近边界，就返回true，否则返回false
    /// </summary>
    /// <param name="particleIndex">待判断流体粒子的索引号</param>
    /// <returns></returns>
    bool NearBoundary(int particleIndex){
        if(virtualBoundaryParticlesData[particleIndex*3+0].w > 0.6 ||
           virtualBoundaryParticlesData[particleIndex*3+1].w > 0.6 ||
           virtualBoundaryParticlesData[particleIndex*3+2].w > 0.6)
            return true;
        else
            return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        for(int i=0; i<maxNum; i++){
            GameObject sphere = Instantiate(obiSphere);
            sphere.transform.position = new Vector3(0,-100,0);
            // sphere.SetActive(false);
            sphereInactive.Push(sphere);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // 调试
        Debug.Log(sphereActive.Count);

        // 取引用，更新
        positions = GetComponent<FluidMain>().positions;
        virtualBoundaryParticlesData = GetComponent<FluidMain>().virtualBoundaryParticlesData;

        // 01.拆旧.先删除已经对碰撞检测没有贡献的老粒子.
        List<int> toRemove = new List<int>(); // 待删除的粒子的索引，也就是key，根据key可以删除value.

        // 预删除
        foreach(int key in sphereActive.Keys){
            int particleIndex = key ;

            if(!NearBoundary(particleIndex))  // 根据particleIndex判断是否需要移除
                toRemove.Add(particleIndex);
        }

        // 实际删除
        foreach(int item in toRemove){
            // sphereActive[item].SetActive(false);
            sphereInactive.Push(sphereActive[item]);
            sphereActive[item].transform.position = new Vector3(0,-10,0);
            sphereActive.Remove(item); // 根据key，从dictionary里移除对碰撞检测不再有贡献的粒子.

        }

        toRemove.Clear();

        // 02.加新.再添加刚开始对碰撞检测起作用的新粒子
        for(int i=0;i<positions.Length;i=i+2){ // 也可以i=i+2，有限数量下，雨露均沾
            if(NearBoundary(i) && !sphereActive.ContainsKey(i) && sphereInactive.Count>0){
                // sphereInactive.Peek().SetActive(true);
                sphereActive.Add(i,sphereInactive.Peek());
                sphereInactive.Pop();
            }
        }

        // 03.设置位置.设置dictionary里，粒子的位置.
        foreach(int key in sphereActive.Keys){
            sphereActive[key].transform.position = positions[key];
        }

        // 仅输出600帧，所以600帧以后直接清零，不做流固交互了.
        if(Time.frameCount>300){
            sphereActive.Clear();
            sphereInactive.Clear();
        }


    }
}

}