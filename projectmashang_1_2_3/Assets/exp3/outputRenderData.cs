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
            // ��������ļ���·��  
            string outputPath = "D://RenderData//exp3//exp3_txt//" + Time.frameCount.ToString() + ".txt";


            string directoryPath = Path.GetDirectoryName(outputPath); // ��ȡ�ļ����ڵ�Ŀ¼·��  

            // ���Ŀ¼�����ڣ��򴴽���  
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // ����ļ������ڣ��򴴽���
            FileStream fs = new FileStream(outputPath, FileMode.Create);
            StreamWriter wr = new StreamWriter(fs);

            Vector3[] positions = GetComponent<FluidMain>().positions; // �ú�fluidmain��һ��namespace�����ᱨ��null����

            for (int i = 0; i < positions.Length; i++)
            {
                string line = positions[i].x.ToString() + ";" +
                            positions[i].y.ToString() + ";" +
                            positions[i].z.ToString() + ";0;0;0";
                wr.WriteLine(line);
            }

            //�ر�
            wr.Close();


        }

        void OutputFluidDataByXYZ()
        {
            // ��������ļ���·��  
            string outputPath = "D://RenderData//exp3//exp3_xyz//" + Time.frameCount.ToString() + ".xyz";


            string directoryPath = Path.GetDirectoryName(outputPath); // ��ȡ�ļ����ڵ�Ŀ¼·��  

            // ���Ŀ¼�����ڣ��򴴽���  
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            Vector3[] positions = GetComponent<FluidMain>().positions;

            using (BinaryWriter writer = new BinaryWriter(File.Open(outputPath, FileMode.Create)))
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    // д��x, y, z�����ԭʼ�ֽڱ�ʾ  
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


            stringBuilder.AppendLine("// ������: " + mesh1.vertices.Length);

            // ��ȡ�������������
            Vector3[] vertices = mesh1.vertices;

            // ģ�Ͷ�������ת������������ϵ��
            Transform transform = GetComponent<Transform>();
            for (int i = 0; i < vertices.Length; ++i)
            {
                // ��Mesh�ľֲ�����ת��Ϊ��������  
                // https://docs.unity3d.com/ScriptReference/Transform.TransformPoints.html
                // Vector3 worldVertex = transform.TransformPoint(mesh.vertices[i]);  
                // Vector3 worldVertex = transform.TransformPoint(vertices[i]);  
                Vector3 worldVertex = solidModel_1.transform.TransformPoint(vertices[i]);

                // Vector3 worldVertex = Vector3.zero;

                // ƴ�ӵ�д��                
                // string line = "v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z;
                // wr.WriteLine(line);

                // stringBuilder�����ַ���ƴ��
                stringBuilder.AppendLine("v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z);


            }


            stringBuilder.AppendLine("// ���������� " + mesh1.triangles.Length / 3);



            // ��StringBuilder������д���ļ�  
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


            stringBuilder.AppendLine("// ������: " + mesh2.vertices.Length);

            // ��ȡ�������������
            Vector3[] vertices = mesh2.vertices;

            // ģ�Ͷ�������ת������������ϵ��
            Transform transform = GetComponent<Transform>();
            for (int i = 0; i < vertices.Length; ++i)
            {
                // ��Mesh�ľֲ�����ת��Ϊ��������  
                // https://docs.unity3d.com/ScriptReference/Transform.TransformPoints.html
                // Vector3 worldVertex = transform.TransformPoint(mesh.vertices[i]);  
                // Vector3 worldVertex = transform.TransformPoint(vertices[i]);  
                Vector3 worldVertex = solidModel_2.transform.TransformPoint(vertices[i]);

                // Vector3 worldVertex = Vector3.zero;

                // ƴ�ӵ�д��                
                // string line = "v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z;
                // wr.WriteLine(line);

                // stringBuilder�����ַ���ƴ��
                stringBuilder.AppendLine("v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z);


            }


            stringBuilder.AppendLine("// ���������� " + mesh2.triangles.Length / 3);



            // ��StringBuilder������д���ļ�  
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


            stringBuilder.AppendLine("// ������: " + mesh3.vertices.Length);

            // ��ȡ�������������
            Vector3[] vertices = mesh3.vertices;

            // ģ�Ͷ�������ת������������ϵ��
            Transform transform = GetComponent<Transform>();
            for (int i = 0; i < vertices.Length; ++i)
            {
                // ��Mesh�ľֲ�����ת��Ϊ��������  
                // https://docs.unity3d.com/ScriptReference/Transform.TransformPoints.html
                // Vector3 worldVertex = transform.TransformPoint(mesh.vertices[i]);  
                // Vector3 worldVertex = transform.TransformPoint(vertices[i]);  
                Vector3 worldVertex = solidModel_3.transform.TransformPoint(vertices[i]);

                // Vector3 worldVertex = Vector3.zero;

                // ƴ�ӵ�д��                
                // string line = "v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z;
                // wr.WriteLine(line);

                // stringBuilder�����ַ���ƴ��
                stringBuilder.AppendLine("v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z);


            }


            stringBuilder.AppendLine("// ���������� " + mesh3.triangles.Length / 3);



            // ��StringBuilder������д���ļ�  
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

            // ��ʼ��obj��face��صĲ���
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