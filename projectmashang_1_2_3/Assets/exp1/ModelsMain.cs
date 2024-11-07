using System.Collections;
using System.Collections.Generic;
using test21;
using UnityEngine;

namespace test20{

  

public class ModelsMain : MonoBehaviour
{
    /// <summary>
    /// 场景中作为边界的obj模型数量.虽然是public，但面板里依旧没有.调数量的时候，可能会忘了……怎么办？手动注意吧……
    /// </summary>
    public const int BOUNDARYMODELNUM = 2;
    //modelMains里应该存的是模型的所有数据，似乎还包括SDF数据和体积数据
    public ModelMain[] modelMains = new ModelMain[BOUNDARYMODELNUM];
    // public ModelMain[] modelMains = new ModelMain[1];
    //这个是一个核心ComputeShader计算了大量内容
    public ComputeShader[] computeShaderBoundaryMap = new ComputeShader[BOUNDARYMODELNUM];

    /// <summary>
    ///boundaryMap.compute里出现的kernel.
    /// </summary>
    private int[] 
    kernelIDUpdateVolumeMap = new int[BOUNDARYMODELNUM],kernelIDUpdateVolumeMapPost = new int[BOUNDARYMODELNUM],
    kernelIDUpdateSDFMapPre = new int[BOUNDARYMODELNUM], kernelIDUpdateSDFMapPost = new int[BOUNDARYMODELNUM],
    kernelIDUpdateSDFMap_1 = new int[BOUNDARYMODELNUM],kernelIDUpdateSDFMap_2 = new int[BOUNDARYMODELNUM],
    kernelIDUpdateSDFMap = new int[BOUNDARYMODELNUM],kernelIDComputeVolumeAndPositionForBoundaryParticles = new int[BOUNDARYMODELNUM];    

    /// <summary>
    /// boundaryMap.compute里出现的computeBuffer.和hlsl是同名的.
    /// 场景里有3个模型，所以数组长度都是3.
    /// </summary>
    /// 创建各ComputerBuffer
    /// 先假设一下各Buffer的意义吧，_dg_NodeLocalPosition是各点上一帧的位置，dg_SDFNode是各网格粒子的SDF距离数据
    /// _dg_VolumeNode是各网格粒子的体积数据，_dg_Cells是网格上节点的索引
    /// _modelVertex存储模型各顶点的位置？_modelNormal存储模型的法向量（不清楚是顶点的还是面片的）
    /// _triangles存储三角形组织方式，_vertexGroupArray是分组用于并行计算加速用的吧
    /// _neighbourVertexList存储固体边界所在网格上的邻居节点，_tempDeltaModelVertex应该存的也是模型结点，但其他不需要每帧更新这个需要？为什么
    /// _tempDeltaVolume和_tempSDFNode应该是辅助体积数据和SDF距离数据计算的，也存的是这两个数据
    private ComputeBuffer[] 
    _dg_NodeLocalPosition = new ComputeBuffer[BOUNDARYMODELNUM],_dg_SDFNode = new ComputeBuffer[BOUNDARYMODELNUM],
    _dg_VolumeNode = new ComputeBuffer[BOUNDARYMODELNUM],_dg_Cells = new ComputeBuffer[BOUNDARYMODELNUM],
    _modelVertex = new ComputeBuffer[BOUNDARYMODELNUM],_modelNormal = new ComputeBuffer[BOUNDARYMODELNUM],
    _triangles = new ComputeBuffer[BOUNDARYMODELNUM],_vertexGroupArray = new ComputeBuffer[BOUNDARYMODELNUM],
    _neighbourVertexList = new ComputeBuffer[BOUNDARYMODELNUM],_tempDeltaModelVertex = new ComputeBuffer[BOUNDARYMODELNUM],/*这个需要每帧更新，不像其它的，传进去就行了*/
    _tempDeltaVolume = new ComputeBuffer[BOUNDARYMODELNUM],_tempSDFNode = new ComputeBuffer[BOUNDARYMODELNUM];

    /// <summary>
    /// 那个共享的buffer，通过fluidmain里取引用来赋值.
    /// </summary>
    private ComputeBuffer _positions = null ,_virtualBoundaryParticlesData = null;  

    /// <summary>
    /// 通过FindKernel，设置boundaryMap.compute里的kernelID.
    /// </summary>
    void BoundaryMap_InitKernelID(){

        for(int modelIndex = 0; modelIndex < BOUNDARYMODELNUM; modelIndex++){
            kernelIDUpdateVolumeMap[modelIndex] = computeShaderBoundaryMap[modelIndex].FindKernel("UpdateVolumeMap");
            kernelIDUpdateVolumeMapPost[modelIndex] = computeShaderBoundaryMap[modelIndex].FindKernel("UpdateVolumeMapPost");
            kernelIDUpdateSDFMapPre[modelIndex] = computeShaderBoundaryMap[modelIndex].FindKernel("UpdateSDFMapPre");
            kernelIDUpdateSDFMapPost[modelIndex] = computeShaderBoundaryMap[modelIndex].FindKernel("UpdateSDFMapPost");
            kernelIDUpdateSDFMap_1[modelIndex] = computeShaderBoundaryMap[modelIndex].FindKernel("UpdateSDFMap_1");
            kernelIDUpdateSDFMap_2[modelIndex] = computeShaderBoundaryMap[modelIndex].FindKernel("UpdateSDFMap_2");   

            kernelIDUpdateSDFMap[modelIndex] = computeShaderBoundaryMap[modelIndex].FindKernel("UpdateSDFMap"); // 调试用的.

            kernelIDComputeVolumeAndPositionForBoundaryParticles[modelIndex] = computeShaderBoundaryMap[modelIndex].FindKernel("ComputeVolumeAndPositionForBoundaryParticles"); // 碰撞检测用的.
        }


    }    


    
    /// <summary>
    /// 初始化buffer。所有的都new出来，然后，对于全程固定的直接赋值；对于每帧都变的这里就不setdata了，延迟到update时再做.
    /// </summary>
    void BoundaryMap_InitBuffer(){
        for(int i=0;i<BOUNDARYMODELNUM;i++){
                Debug.Log("数量" + modelMains[i].dg.sdfNode);
            _dg_NodeLocalPosition[i] = new ComputeBuffer(modelMains[i].dg.nodeLocalPosition.Length,sizeof(float)*3);
            _dg_NodeLocalPosition[i].SetData(modelMains[i].dg.nodeLocalPosition);

            _dg_SDFNode[i] = new ComputeBuffer(modelMains[i].dg.sdfNode.Length,sizeof(float));
            _dg_SDFNode[i].SetData(modelMains[i].dg.sdfNode);

            _dg_VolumeNode[i] = new ComputeBuffer(modelMains[i].dg.volumeNode.Length,sizeof(float));
            _dg_VolumeNode[i].SetData(modelMains[i].dg.volumeNode);

            _dg_Cells[i] = new ComputeBuffer(modelMains[i].dg.cells.Length,sizeof(int));
            _dg_Cells[i].SetData(modelMains[i].dg.cells);


            // 每帧都不同，初始化的时候定不下来.在setbuffer的时候做setdata吧.
            _modelVertex[i] = new ComputeBuffer(modelMains[i].vertices.Length,sizeof(float)*3);
            _modelVertex[i].SetData(modelMains[i].vertices); // 能定下来的.

            // // 取回，看看通过数组索引赋值行不行
            // Vector3[] debug = new Vector3[modelMains[i].vertices.Length];
            // _modelVertex[i].GetData(debug);
            // for(int j = 0;j<1000;j++)print(debug[j]);

            _tempDeltaModelVertex[i] = new ComputeBuffer(modelMains[i].vertices.Length,sizeof(float)*3); // 同理，这里就不做setdata了.

            _modelNormal[i] = new ComputeBuffer(modelMains[i].normals.Length,sizeof(float)*3);

            _triangles[i] = new ComputeBuffer(modelMains[i].triangles.Length,sizeof(int));
            _triangles[i].SetData(modelMains[i].triangles);

            _vertexGroupArray[i] = new ComputeBuffer(modelMains[i].vg.vertexGroupArray.Length,sizeof(int));
            _vertexGroupArray[i].SetData(modelMains[i].vg.vertexGroupArray);

            _neighbourVertexList[i] = new ComputeBuffer(modelMains[i].nvl.neighbourVertexList.Length,sizeof(int)*2);
            _neighbourVertexList[i].SetData(modelMains[i].nvl.neighbourVertexList);

            _tempDeltaVolume[i] = new ComputeBuffer(modelMains[i].dg.numberOfNodes,sizeof(int));
            _tempDeltaVolume[i].SetData(new int[modelMains[i].dg.numberOfNodes]);

            _tempSDFNode[i] = new ComputeBuffer(modelMains[i].dg.numberOfNodes,sizeof(uint));
            _tempSDFNode[i].SetData(new uint[modelMains[i].dg.numberOfNodes]);

        }

        // 这个buffer不是数组，就单独一份，所以不在for循环里做
        // 由于FluidMain里初始化过了，new过了；这里直接取引用就行了.
        _positions = GetComponent<FluidMain>()._positions;
        _virtualBoundaryParticlesData = GetComponent<FluidMain>()._virtualBoundaryParticlesData;
    }

    /// <summary>
    /// 由于1拖3的关系，所以运行时每帧中大号函数的形参变量都得变3次.
    /// modelIndex是模型的索引号.该函数的作用是，把第modelIndex个模型的相关数据，传给shader里的普通变量.
    /// 2024-05-01.改成3对3了.
    /// </summary>
    void BoundaryMap_SetValue(){
        for(int modelIndex = 0; modelIndex < BOUNDARYMODELNUM; modelIndex++){

            computeShaderBoundaryMap[modelIndex].SetFloat("_supportRadius",1.2f);

            computeShaderBoundaryMap[modelIndex].SetFloats("_dg_DomainMin",new float[4]{
                //dg是DiscreteGrid
                modelMains[modelIndex].dg.domain.min.x,
                modelMains[modelIndex].dg.domain.min.y,
                modelMains[modelIndex].dg.domain.min.z,0});

            computeShaderBoundaryMap[modelIndex].SetFloats("_dg_DomainMax",new float[4]{
                modelMains[modelIndex].dg.domain.max.x,
                modelMains[modelIndex].dg.domain.max.y,
                modelMains[modelIndex].dg.domain.max.z,0});            

            computeShaderBoundaryMap[modelIndex].SetInts("_dg_Resolution",new int[4]{
                modelMains[modelIndex].dg.resolution.x,
                modelMains[modelIndex].dg.resolution.y,
                modelMains[modelIndex].dg.resolution.z,0});

            computeShaderBoundaryMap[modelIndex].SetFloats("_dg_CellSize", new float[4]{
                modelMains[modelIndex].dg.cellSize.x,
                modelMains[modelIndex].dg.cellSize.y,
                modelMains[modelIndex].dg.cellSize.z,0});

            computeShaderBoundaryMap[modelIndex].SetInt("_dg_NumberOfCells",modelMains[modelIndex].dg.numberOfCells);

            computeShaderBoundaryMap[modelIndex].SetInt("_dg_nv",modelMains[modelIndex].dg.nv);

            computeShaderBoundaryMap[modelIndex].SetInt("_dg_ne_x",modelMains[modelIndex].dg.ne_x);

            computeShaderBoundaryMap[modelIndex].SetInt("_dg_ne_y",modelMains[modelIndex].dg.ne_y);

            computeShaderBoundaryMap[modelIndex].SetInt("_dg_ne_z",modelMains[modelIndex].dg.ne_z);

            computeShaderBoundaryMap[modelIndex].SetInt("_dg_NumberOfNodes",modelMains[modelIndex].dg.numberOfNodes);

            computeShaderBoundaryMap[modelIndex].SetFloat("_dg_nodeRadius",1.2f);

            computeShaderBoundaryMap[modelIndex].SetInt("_dg_NumberOfModelVertex",modelMains[modelIndex].vertexNum);

            computeShaderBoundaryMap[modelIndex].SetMatrix("_dg_worldToLocalMatrix",modelMains[modelIndex].worldToLocalMatrix);

            computeShaderBoundaryMap[modelIndex].SetMatrix("_dg_localToWorldMatrix",modelMains[modelIndex].localToWorldMatrix);

            computeShaderBoundaryMap[modelIndex].SetInt("_maxNeighbourNumPerVertex",modelMains[modelIndex].nvl.maxNeighbourNumPerVertex);

            computeShaderBoundaryMap[modelIndex].SetInt("_triangleNum",modelMains[modelIndex].triangleNum); // 这个value之前漏掉了，没赋值，是0；所以更新sdf的循环就相当于没有，所以没反应.

            computeShaderBoundaryMap[modelIndex].SetFloats("_randomASTU",modelMains[modelIndex].randomASTU);

        }
        
        
    }

    /// <summary>
    /// 逐kernel进行setbuffer操作，同时按需进行setdata操作.
    /// 同理，1拖3，在运行时每帧都变。
    /// 所有buffer都要根据modelIndex进行重绑定，部分buffer还需要重新setdata传值.
    /// 2024-05-01
    /// 同理，由1拖3改成3拖3，更方便.根据见名知意，避免杂糅，这里计划把那几个会变的buffer的setdata操作独立出去，这个函数里只保留setbuffer操作.
    /// </summary>
    void BoundaryMap_SetBuffer(){


        for(int modelIndex = 0; modelIndex < BOUNDARYMODELNUM; modelIndex++){

            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMap[modelIndex],"_tempDeltaModelVertex",_tempDeltaModelVertex[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMap[modelIndex],"_modelVertex",_modelVertex[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMap[modelIndex],"_dg_VolumeNode",_dg_VolumeNode[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMap[modelIndex], "_vertexGroupArray", _vertexGroupArray[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMap[modelIndex], "_dg_Cells", _dg_Cells[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMap[modelIndex], "_neighbourVertexList", _neighbourVertexList[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMap[modelIndex],  "_dg_NodeLocalPosition", _dg_NodeLocalPosition[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMap[modelIndex],  "_tempDeltaVolume", _tempDeltaVolume[modelIndex]); // 连接本kernel与下一个kernel

            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMapPost[modelIndex],"_tempDeltaVolume", _tempDeltaVolume[modelIndex]); 
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateVolumeMapPost[modelIndex],"_dg_VolumeNode", _dg_VolumeNode[modelIndex]);

            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMapPre[modelIndex],"_tempSDFNode" ,_tempSDFNode[modelIndex]);

            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_1[modelIndex],"_modelVertex", _modelVertex[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_1[modelIndex],"_triangles", _triangles[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_1[modelIndex],"_tempSDFNode" ,_tempSDFNode[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_1[modelIndex],"_dg_Cells", _dg_Cells[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_1[modelIndex],"_tempDeltaModelVertex", _tempDeltaModelVertex[modelIndex]);

            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_2[modelIndex],"_modelVertex", _modelVertex[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_2[modelIndex],"_modelNormal", _modelNormal[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_2[modelIndex],"_triangles", _triangles[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_2[modelIndex],"_dg_NodeLocalPosition", _dg_NodeLocalPosition[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_2[modelIndex],"_tempSDFNode" ,_tempSDFNode[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_2[modelIndex],"_dg_Cells", _dg_Cells[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMap_2[modelIndex],"_tempDeltaModelVertex", _tempDeltaModelVertex[modelIndex]);


            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMapPost[modelIndex] , "_tempSDFNode" ,_tempSDFNode[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMapPost[modelIndex] , "_dg_SDFNode", _dg_SDFNode[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDUpdateSDFMapPost[modelIndex] , "_dg_VolumeNode", _dg_VolumeNode[modelIndex]);

            //这个kernel，做的是边界虚拟粒子的计算；根据前4个，计算出第5个
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDComputeVolumeAndPositionForBoundaryParticles[modelIndex], "_dg_NodeLocalPosition", _dg_NodeLocalPosition[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDComputeVolumeAndPositionForBoundaryParticles[modelIndex], "_dg_SDFNode", _dg_SDFNode[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDComputeVolumeAndPositionForBoundaryParticles[modelIndex], "_dg_VolumeNode", _dg_VolumeNode[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDComputeVolumeAndPositionForBoundaryParticles[modelIndex], "_dg_Cells", _dg_Cells[modelIndex]);
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDComputeVolumeAndPositionForBoundaryParticles[modelIndex], "_positions", _positions);           
            computeShaderBoundaryMap[modelIndex].SetBuffer(kernelIDComputeVolumeAndPositionForBoundaryParticles[modelIndex], "_virtualBoundaryParticlesData", _virtualBoundaryParticlesData);             

        }


    }


    // Start is called before the first frame update
    void Start()
    {
        BoundaryMap_InitKernelID();
        BoundaryMap_InitBuffer();
        BoundaryMap_SetBuffer();
        BoundaryMap_SetValue();


    }

    // Update is called once per frame
    void Update()
    {

        //并行计算，更新边界映射.

        for(int modelIndex = 0; modelIndex < BOUNDARYMODELNUM; modelIndex++ ){
            // 更新变化了的参数.
            _modelNormal[modelIndex].SetData(modelMains[modelIndex].normals);
            _tempDeltaModelVertex[modelIndex].SetData(modelMains[modelIndex].tempDeltaModelVertex);          
            computeShaderBoundaryMap[modelIndex].SetMatrix("_dg_worldToLocalMatrix",modelMains[modelIndex].worldToLocalMatrix);
            computeShaderBoundaryMap[modelIndex].SetMatrix("_dg_localToWorldMatrix",modelMains[modelIndex].localToWorldMatrix);            

            // 增量更新volume map.
            for(int groupIndex = 0; groupIndex < modelMains[modelIndex].vg.numOfVertexGroup; groupIndex++){
                int vertexGroupIndexStart = modelMains[modelIndex].vg.vertexGroupPrefixSumArray[groupIndex];
                int vertexGroupIndexEnd = modelMains[modelIndex].vg.vertexGroupPrefixSumArray[groupIndex + 1];

                computeShaderBoundaryMap[modelIndex].SetInt("_vertexGroupIndexStart",vertexGroupIndexStart);
                computeShaderBoundaryMap[modelIndex].SetInt("_vertexGroupIndexEnd",vertexGroupIndexEnd);

                int numOfCurrentGroup = vertexGroupIndexEnd - vertexGroupIndexStart;

                computeShaderBoundaryMap[modelIndex].Dispatch(kernelIDUpdateVolumeMap[modelIndex],(numOfCurrentGroup/32)+1,1,1);
            }

            computeShaderBoundaryMap[modelIndex].Dispatch(kernelIDUpdateVolumeMapPost[modelIndex], (modelMains[modelIndex].dg.numberOfNodes / 64) + 1, 1, 1); 

            // 增量更新SDF map.
            computeShaderBoundaryMap[modelIndex].Dispatch(kernelIDUpdateSDFMapPre[modelIndex],(modelMains[modelIndex].dg.numberOfNodes / 64) + 1, 1, 1);   
            computeShaderBoundaryMap[modelIndex].Dispatch(kernelIDUpdateSDFMap_1[modelIndex],(modelMains[modelIndex].triangleNum / 64) + 1, 1, 1);   // sdf的增量更新的2个步骤.
            computeShaderBoundaryMap[modelIndex].Dispatch(kernelIDUpdateSDFMap_2[modelIndex],(modelMains[modelIndex].triangleNum / 64) + 1, 1, 1);    
            computeShaderBoundaryMap[modelIndex].Dispatch(kernelIDUpdateSDFMapPost[modelIndex],(modelMains[modelIndex].dg.numberOfNodes / 64) + 1, 1, 1);   


        }


        // // 数据取回，可视化map
        // for(int modelIndex = 0; modelIndex < BOUNDARYMODELNUM; modelIndex++ ){
        //     _dg_SDFNode[modelIndex].GetData(modelMains[modelIndex].dg.sdfNode);

        //     _dg_VolumeNode[modelIndex].GetData(modelMains[modelIndex].dg.volumeNode);
        // }


    } // end of update.

    /// <summary>
    /// 借助N个物体对应的boundarymap.compute，分别计算出N个虚拟边界粒子的值，分别填充到结构体里的对应位置.
    /// 这个方法在哪调用了？
    /// </summary>
    /// <param name="particleNum">流体粒子的数量，设置线程组会用到.</param>
    public void ComputeVolumeAndPositionForBoundaryParticles(int particleNum){
        for(int modelIndex = 0; modelIndex < BOUNDARYMODELNUM; modelIndex++){
              computeShaderBoundaryMap[modelIndex].Dispatch(kernelIDComputeVolumeAndPositionForBoundaryParticles[modelIndex], particleNum / 32, 1, 1);
        }
    }



    void OnDestroy(){
        for(int modelIndex = 0;modelIndex<BOUNDARYMODELNUM;modelIndex++){
            _dg_NodeLocalPosition[modelIndex].Release();
            _dg_SDFNode[modelIndex].Release();
            _dg_VolumeNode[modelIndex].Release();
            _dg_Cells[modelIndex].Release();
            _modelVertex[modelIndex].Release();
            _modelNormal[modelIndex].Release();
            _triangles[modelIndex].Release();
            _vertexGroupArray[modelIndex].Release();
            _neighbourVertexList[modelIndex].Release();
            _tempDeltaModelVertex[modelIndex].Release();
            _tempDeltaVolume[modelIndex].Release();
            _tempSDFNode[modelIndex].Release();
        }
    }    


}

}