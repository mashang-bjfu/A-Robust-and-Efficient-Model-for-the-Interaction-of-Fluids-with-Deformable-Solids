using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace test21
{

    public class output_test2 : MonoBehaviour
    {
        //请注意，本脚本不能自动创建目录，需要按照下面三个路径创建目录
        //txt与obj文件目测输出没有问题，但xyz文件不能文本，无法对照确定是否正确
        int index = 0;
        string filePathTXT = "D://RenderData//exp1//exp1_txt//";
        string filePathOBJ = "D://RenderData//exp1//exp1_obj//";
        string filePathXYZ = "D://RenderData//exp1//exp1_xyz//";

        public GameObject solidModel;

        Mesh mesh;

        StringBuilder triangleInfoStringBuilder = new StringBuilder();

        List<float> xyzData = new List<float>();
        List<string> txtData = new List<string>();
        List<string> objData = new List<string>();

        void OutputFluidDataByTXT()
        {
            Vector3[] positions = GetComponent<FluidMain>().positions; // 得和fluidmain在一个namespace里，否则会报错，null引用
            //int startTime1 = System.Environment.TickCount;
            for (int i = 0; i < positions.Length; i++)
            {
                string line = positions[i].x.ToString();
                string line2 = positions[i].y.ToString();
                string line3 = positions[i].z.ToString();
                txtData.Add(line);
                txtData.Add(line2);
                txtData.Add(line3);
                txtData.Add(";");//表示一行结束
            }
            //int endTime1 = System.Environment.TickCount;
            //Debug.Log("将信息写入list所需时间为：" + (endTime1 - startTime1));
            txtData.Add("#");//表示一页结束
        }

        void OutputFluidDataByXYZ()
        {

            Vector3[] positions = GetComponent<FluidMain>().positions; // 得和fluidmain在一个namespace里，否则会报错，null引用
            for (int i = 0; i < positions.Length; i++)
            {
                float line = positions[i].x;
                float line2 = positions[i].y;
                float line3 = positions[i].z;
                xyzData.Add(line);
                xyzData.Add(line2);
                xyzData.Add(line3);
            }
            xyzData.Add(-1000);//表示一页结束
        }

        void OutputSolidDataByOBJ()
        {

            solidModel.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);

            Vector3[] vertices = mesh.vertices;
            // 模型顶点坐标转化到世界坐标系下
            Transform transform = GetComponent<Transform>();
            for (int i = 0; i < vertices.Length; ++i)
            {

                Vector3 worldVertex = solidModel.transform.TransformPoint(vertices[i]);

                string line = worldVertex.x.ToString();
                string line2 = worldVertex.y.ToString();
                string line3 = worldVertex.z.ToString();

                objData.Add("&");//表示一行开始
                objData.Add(line);
                objData.Add(line2);
                objData.Add(line3);
                objData.Add(";");//表示一行结束
            }
            objData.Add("#");//表示一页结束

        }

        void WriteToFileTXT()
        {
            int index = 0;
            Debug.Log("执行WriteToFile");
            for (int frame = 1; frame < 501; frame++)
            {
                FileStream fs = new FileStream("" + filePathTXT + frame + ".txt", FileMode.Create);
                StreamWriter writer = new StreamWriter(fs);
                for (int i = 0; ; index++)
                {
                   if (txtData[index].Equals(";"))
                    {
                        writer.Write("\r\n");
                        continue;
                    }
                    if (txtData[index].Equals("#"))
                    {

                        index++;
                        break;
                    }

                    writer.Write(txtData[index] + ";");
                            
                }
                writer.Close();
            }

        }

        void WriteToFileXYZ()
        {
            int index = 0;
            Debug.Log("执行WriteToFile");
            for (int frame = 1; frame < 501; frame++)
            {
                FileStream fs = new FileStream("" + filePathXYZ + frame + ".xyz", FileMode.Create);
                BinaryWriter writer = new BinaryWriter(fs);
                for (int i = 0; ; index++)
                {

                    if (xyzData[index]==-1000)
                    {

                        index++;
                        break;
                    }

                    writer.Write(xyzData[index]);

                }
                writer.Close();
            }

        }
        void WriteToFileOBJ()
        {
            int index = 0;
            for (int frame = 1; frame < 501; frame++)
            {
                FileStream fs = new FileStream("" + filePathOBJ + frame + ".obj", FileMode.Create);
                StreamWriter writer = new StreamWriter(fs);
                for (int i = 0; ; index++)
                {
                    if (objData[index].Equals("&"))
                    {
                        writer.Write("v ");
                        continue;
                    }
                    if (objData[index].Equals(";"))
                    {
                        writer.Write("\r\n");
                        continue;
                    }
                    if (objData[index].Equals("#"))
                    {
                        writer.Write("\r\n");
                        writer.Write(triangleInfoStringBuilder.ToString());
                        index++;
                        break;
                    }


                    writer.Write(objData[index] + " ");

                }

                writer.Close();
            }

        }


        // Start is called before the first frame update
        void Start()
        {

            // 初始化obj里face相关的部分
            mesh = new Mesh(); // NullReferenceException
            solidModel.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
            int[] triangles = mesh.triangles;

            for (int currentTriangleIndex = 0; currentTriangleIndex < mesh.triangles.Length / 3; currentTriangleIndex++)
            {

                triangleInfoStringBuilder.AppendLine(
                    "f" + " " +
                (triangles[currentTriangleIndex * 3 + 0] + 1) + " " +
                (triangles[currentTriangleIndex * 3 + 1] + 1) + " " +
                (triangles[currentTriangleIndex * 3 + 2] + 1)
                );
            }


        }

        // Update is called once per frame
        void Update()
        {
            index++;
            if (index <= 500)
            {
                OutputFluidDataByTXT();
                OutputFluidDataByXYZ();
                OutputSolidDataByOBJ();
                Debug.Log("index" + index);
            }
            if (index == 501)
            {
                WriteToFileTXT();
                WriteToFileXYZ();
                WriteToFileOBJ();

            }

        }
    }

}