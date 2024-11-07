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

    public class Output_test : MonoBehaviour
    {
        private ConcurrentQueue<string> dataQueueTXT = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> dataQueueXYZ = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> dataQueueOBJ = new ConcurrentQueue<string>();
        private bool isWritingTXT = false;
        private bool isWritingXYZ = false;
        private bool isWritingOBJ = false;
        string filePathTXT = "D://RenderData//exp1//exp1_txt//";
        string filePathOBJ = "D://RenderData//exp1//exp1_obj//";
        string filePathXYZ = "D://RenderData//exp1//exp1_xyz//";



        public GameObject solidModel;

        int frameIndex;

        Mesh mesh;

        StringBuilder triangleInfoStringBuilder = new StringBuilder();

        List<string> OutputFluidDataByTXT()
        {
            Vector3[] positions = GetComponent<FluidMain>().positions; // �ú�fluidmain��һ��namespace�����ᱨ��null����
            List<string> txtData = new List<string>();


            int startTime1 = System.Environment.TickCount;
            for (int i = 0; i < positions.Length; i++)
            {
                string line = positions[i].x.ToString() + ";" +
                            positions[i].y.ToString() + ";" +
                            positions[i].z.ToString() + ";0;0;0";
                txtData.Add(line);
            }
            int endTime1 = System.Environment.TickCount;
            Debug.Log("����Ϣд��list����ʱ��Ϊ��"+(endTime1 - startTime1));
            return txtData;
            
        }

        List<string> OutputFluidDataByXYZ()
        {

            Vector3[] positions = GetComponent<FluidMain>().positions; // �ú�fluidmain��һ��namespace�����ᱨ��null����
            List<string> xyzData = new List<string>();
            for (int i = 0; i < positions.Length; i++)
            {
                string line = positions[i].x.ToString() + ";" +
                            positions[i].y.ToString() + ";" +
                            positions[i].z.ToString();
                xyzData.Add(line);
            }
            return xyzData;
        }

        List<string> OutputSolidDataByOBJ()
        {

            solidModel.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);

            Vector3[] vertices = mesh.vertices;
            List<string> objData = new List<string>();
            // ģ�Ͷ�������ת������������ϵ��
            Transform transform = GetComponent<Transform>();
            for (int i = 0; i < vertices.Length; ++i)
            {
                
                Vector3 worldVertex = solidModel.transform.TransformPoint(vertices[i]);

                // stringBuilder�����ַ���ƴ��
                objData.Add("v " + worldVertex.x + " " + worldVertex.y + " " + worldVertex.z);
            }

            objData.Add(triangleInfoStringBuilder.ToString());
            return objData;
        }



        // Start is called before the first frame update
        void Start()
        {
            
            frameIndex = 0;

            // ��ʼ��obj��face��صĲ���
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

            Task.Run(() => WriteDataToFileAsyncTXT());
            Task.Run(() => WriteDataToFileAsyncOBJ());
            Task.Run(() => WriteDataToFileAsyncXYZ());

        }

        // Update is called once per frame
        void Update()
        {

            frameIndex += 1;
            /*
            OutputSolidDataByOBJ();
            OutputFluidDataByXYZ();
            OutputFluidDataByTXT();
            */
            List<string> txtData = OutputFluidDataByTXT();
            List<string> xyzData = OutputFluidDataByXYZ();
            List<string> objData = OutputSolidDataByOBJ();
            foreach(var data in txtData)
            {
                dataQueueTXT.Enqueue(data);
            }
            foreach (var data in xyzData)
            {
                dataQueueXYZ.Enqueue(data);
            }
            foreach (var data in objData)
            {
                dataQueueOBJ.Enqueue(data);
            }
        }

        async Task WriteDataToFileAsyncTXT()
        {
            while (true)
            {
                if (dataQueueTXT.IsEmpty)
                {
                    await Task.Delay(100);
                    continue;
                }

                List<string> dataToWrite = new List<string>();
                while(dataQueueTXT.TryDequeue(out string data))
                {
                    dataToWrite.Add(data);
                }

                if(dataToWrite.Count > 0)
                {
                    int startTime2 = System.Environment.TickCount;
                    isWritingTXT = true;
                    File.AppendAllLines(filePathTXT + frameIndex+".txt", dataToWrite);
                    isWritingTXT = false;
                    int endTime2 = System.Environment.TickCount;
                    Debug.Log("listд���ļ�ʱ��Ϊ"+(endTime2 - startTime2));
                }
            }
        }
        async Task WriteDataToFileAsyncOBJ()
        {
            while (true)
            {
                if (dataQueueOBJ.IsEmpty)
                {
                    await Task.Delay(100);
                    continue;
                }

                List<string> dataToWrite = new List<string>();
                while (dataQueueOBJ.TryDequeue(out string data))
                {
                    dataToWrite.Add(data);
                }

                if (dataToWrite.Count > 0)
                {
                    isWritingOBJ = true;
                    File.AppendAllLines(filePathOBJ + frameIndex + ".obj", dataToWrite);
                    isWritingOBJ = false;
                }
            }
        }

        async Task WriteDataToFileAsyncXYZ()
        {
            while (true)
            {
                if (dataQueueXYZ.IsEmpty)
                {
                    await Task.Delay(100);
                    continue;
                }

                List<string> dataToWrite = new List<string>();
                while (dataQueueXYZ.TryDequeue(out string data))
                {
                    dataToWrite.Add(data);
                }

                if (dataToWrite.Count > 0)
                {
                    isWritingXYZ = true;
                    File.AppendAllLines(filePathXYZ + frameIndex + ".xyz", dataToWrite);
                    isWritingXYZ = false;
                }
            }
        }
    }

}