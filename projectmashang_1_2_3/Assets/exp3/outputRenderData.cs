using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using System.Threading;

namespace exp3
{

    public class outputRenderData : MonoBehaviour
    {
        public GameObject solidModel_1;
        public GameObject solidModel_2;
        public GameObject solidModel_3;
        string directoryName;
        int frameIndex;

        Mesh mesh1;
        Mesh mesh2;
        Mesh mesh3;

        StringBuilder triangleInfoStringBuilder1 = new StringBuilder();
        StringBuilder triangleInfoStringBuilder2 = new StringBuilder();
        StringBuilder triangleInfoStringBuilder3 = new StringBuilder();

        void OutputFluidDataByTXT()
        {
            // 定义输出文件的路径  
            string outputPath = "D://RenderData//exp3//exp3_txt//" + Time.frameCount.ToString() + ".txt";


            string directoryPath = Path.GetDirectoryName(outputPath); // 获取文件所在的目录路径  

            // 如果目录不存在，则创建它  
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 如果文件不存在，则创建它
            FileStream fs = new FileStream(outputPath, FileMode.Create);
            StreamWriter wr = new StreamWriter(fs);

            Vector3[] positions = GetComponent<FluidMain>().positions; // 得和fluidmain在一个namespace里，否则会报错，null引用

            for (int i = 0; i < positions.Length; i++)
            {
                string line = positions[i].x.ToString() + ";" +
                            positions[i].y.ToString() + ";" +
                            positions[i].z.ToString() + ";0;0;0";
                wr.WriteLine(line);
            }

            //关闭
            wr.Close();


        }

        void OutputFluidDataByXYZ()
        {
            // 定义输出文件的路径  
            string outputPath = "D://RenderData//exp3//exp3_xyz//" + Time.frameCount.ToString() + ".xyz";


            string directoryPath = Path.GetDirectoryName(outputPath); // 获取文件所在的目录路径  

            // 如果目录不存在，则创建它  
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            Vector3[] positions = GetComponent<FluidMain>().positions;

            using (BinaryWriter writer = new BinaryWriter(File.Open(outputPath, FileMode.Create)))
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    // 写入x, y, z坐标的原始字节表示  
                    writer.Write(positions[i].x);
                    writer.Write(positions[i].y);
                    writer.Write(positions[i].z);
                }
            }
        }

        void OutputSolidDataByOBJ_1()
        {

            solidModel_1.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh1);

            string filePath = "D://RenderData//exp3//exp3_obj//obj1//" + Time.frameCount.ToString() + ".obj";
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            StringBuilder stringBuilder = new StringBuilder();


            stringBuilder.AppendLine("// 顶点数: " + mesh1.vertices.Length);

            // 获取顶点数组的引用
            Vector3[] vertices = mesh1.vertices;

            // 模型顶点坐标转化到世界坐标系下
            Transform transform = GetComponent<Transform>();
            for (int i = 0; i < vertices.Length; ++i)
            {
                // 将Mesh的局部坐标转换为世界坐标  
                // https://docs.unity3d.com/ScriptReference/Transform.TransformPoints.html
                // Vector3 worldVertex = transform.TransformPoint(mesh.vertices[i]);  
                // Vector3 worldVertex = transform.TransformPoint(vertices[i]);  
                Vector3 worldVertex = solidModel_1.transform.TransformPoint(vertices[i]);

                // Vector3 worldVertex = Vector3.zero;

                // 拼接的写法                
                // string line = "v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z;
                // wr.WriteLine(line);

                // stringBuilder加速字符串拼接
                stringBuilder.AppendLine("v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z);


            }


            stringBuilder.AppendLine("// 三角形数： " + mesh1.triangles.Length / 3);



            // 将StringBuilder的内容写入文件  
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (StreamWriter wr = new StreamWriter(fs))
            {
                wr.Write(stringBuilder.ToString());
                wr.Write(triangleInfoStringBuilder1.ToString());
            }



        }
        void OutputSolidDataByOBJ_2()
        {

            solidModel_2.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh2);

            string filePath = "D://RenderData//exp3//exp3_obj//obj2//" + Time.frameCount.ToString() + ".obj";
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            StringBuilder stringBuilder = new StringBuilder();


            stringBuilder.AppendLine("// 顶点数: " + mesh2.vertices.Length);

            // 获取顶点数组的引用
            Vector3[] vertices = mesh2.vertices;

            // 模型顶点坐标转化到世界坐标系下
            Transform transform = GetComponent<Transform>();
            for (int i = 0; i < vertices.Length; ++i)
            {
                // 将Mesh的局部坐标转换为世界坐标  
                // https://docs.unity3d.com/ScriptReference/Transform.TransformPoints.html
                // Vector3 worldVertex = transform.TransformPoint(mesh.vertices[i]);  
                // Vector3 worldVertex = transform.TransformPoint(vertices[i]);  
                Vector3 worldVertex = solidModel_2.transform.TransformPoint(vertices[i]);

                // Vector3 worldVertex = Vector3.zero;

                // 拼接的写法                
                // string line = "v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z;
                // wr.WriteLine(line);

                // stringBuilder加速字符串拼接
                stringBuilder.AppendLine("v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z);


            }


            stringBuilder.AppendLine("// 三角形数： " + mesh2.triangles.Length / 3);



            // 将StringBuilder的内容写入文件  
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (StreamWriter wr = new StreamWriter(fs))
            {
                wr.Write(stringBuilder.ToString());
                wr.Write(triangleInfoStringBuilder2.ToString());
            }



        }
        void OutputSolidDataByOBJ_3()
        {

            solidModel_3.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh3);

            string filePath = "D://RenderData//exp3//exp3_obj//obj3//" + Time.frameCount.ToString() + ".obj";
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            StringBuilder stringBuilder = new StringBuilder();


            stringBuilder.AppendLine("// 顶点数: " + mesh3.vertices.Length);

            // 获取顶点数组的引用
            Vector3[] vertices = mesh3.vertices;

            // 模型顶点坐标转化到世界坐标系下
            Transform transform = GetComponent<Transform>();
            for (int i = 0; i < vertices.Length; ++i)
            {
                // 将Mesh的局部坐标转换为世界坐标  
                // https://docs.unity3d.com/ScriptReference/Transform.TransformPoints.html
                // Vector3 worldVertex = transform.TransformPoint(mesh.vertices[i]);  
                // Vector3 worldVertex = transform.TransformPoint(vertices[i]);  
                Vector3 worldVertex = solidModel_3.transform.TransformPoint(vertices[i]);

                // Vector3 worldVertex = Vector3.zero;

                // 拼接的写法                
                // string line = "v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z;
                // wr.WriteLine(line);

                // stringBuilder加速字符串拼接
                stringBuilder.AppendLine("v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z);


            }


            stringBuilder.AppendLine("// 三角形数： " + mesh3.triangles.Length / 3);



            // 将StringBuilder的内容写入文件  
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (StreamWriter wr = new StreamWriter(fs))
            {
                wr.Write(stringBuilder.ToString());
                wr.Write(triangleInfoStringBuilder3.ToString());
            }



        }


        // Start is called before the first frame update
        void Start()
        {
            directoryName = "exp3_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

            Debug.Log(directoryName);

            frameIndex = 0;

            // 初始化obj里face相关的部分
            mesh1 = new Mesh(); // NullReferenceException
            solidModel_1.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh1);
            int[] triangles1 = mesh1.triangles;

            for (int currentTriangleIndex = 0; currentTriangleIndex < mesh1.triangles.Length / 3; currentTriangleIndex++)
            {

                triangleInfoStringBuilder1.AppendLine(
                    "f" + " " +
                (triangles1[currentTriangleIndex * 3 + 0] + 1) + " " +
                (triangles1[currentTriangleIndex * 3 + 1] + 1) + " " +
                (triangles1[currentTriangleIndex * 3 + 2] + 1)
                );
            }

            mesh2 = new Mesh(); // NullReferenceException
            solidModel_2.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh2);
            int[] triangles2 = mesh2.triangles;

            for (int currentTriangleIndex = 0; currentTriangleIndex < mesh2.triangles.Length / 3; currentTriangleIndex++)
            {

                triangleInfoStringBuilder2.AppendLine(
                    "f" + " " +
                (triangles2[currentTriangleIndex * 3 + 0] + 1) + " " +
                (triangles2[currentTriangleIndex * 3 + 1] + 1) + " " +
                (triangles2[currentTriangleIndex * 3 + 2] + 1)
                );
            }

            mesh3 = new Mesh(); // NullReferenceException
            solidModel_3.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh3);
            int[] triangles3 = mesh3.triangles;

            for (int currentTriangleIndex = 0; currentTriangleIndex < mesh3.triangles.Length / 3; currentTriangleIndex++)
            {

                triangleInfoStringBuilder3.AppendLine(
                    "f" + " " +
                (triangles3[currentTriangleIndex * 3 + 0] + 1) + " " +
                (triangles3[currentTriangleIndex * 3 + 1] + 1) + " " +
                (triangles3[currentTriangleIndex * 3 + 2] + 1)
                );
            }

        }

        // Update is called once per frame
        void Update()
        {

            frameIndex += 1;

            // Debug.Log(frameIndex);

            OutputFluidDataByTXT();
            OutputFluidDataByXYZ();
            OutputSolidDataByOBJ_1();
            OutputSolidDataByOBJ_2();
            OutputSolidDataByOBJ_3();

        }
    }

}