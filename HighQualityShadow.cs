
// 1.计算角色的包围盒

// 2.将角色的包围盒的8个点投射到光源空间（的V矩阵）。

// 3.利用包围盒的最大最小值决定（摄影机的）投影矩阵。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class HighQualityShadow : MonoBehaviour
    {
        // Start is called before the first frame update
        Bounds bounds = new Bounds();
        //可以升级为很多个transform
        public Transform shadowCaster;
        public Light mainLight;
        public float shadowClipDistance = 10;
        private Matrix4x4 viewMatrix, projMatrix;
        
        private List<Vector3> vertexPositions = new List<Vector3>();
        private List<MeshRenderer> vertexRenderer = new List<MeshRenderer>();
        private SkinnedMeshRenderer[] skinmeshes;
        private int boundsCount;
        
        void Start()
        {
            //对于每一个skinMeshRenderer计算总的包围盒：

            skinmeshes = shadowCaster.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            Debug.Log(skinmeshes.Length + "  Length");

            for(int i = 0;i < skinmeshes.Length; i++)
            {
                
                CalculateAABB(boundsCount, skinmeshes[i]);
                boundsCount += 1;
            }

            float x = bounds.extents.x;                                       //范围这里是三维向量，分别取得X Y Z
            float y = bounds.extents.y;
            float z = bounds.extents.z;

            vertexPositions.Add(new Vector3(x, y, z));
            vertexPositions.Add(new Vector3(x, -y, z));
            vertexPositions.Add(new Vector3(x, y, -z));
            vertexPositions.Add(new Vector3(x, -y, -z));
            vertexPositions.Add(new Vector3(-x, y, z));
            vertexPositions.Add(new Vector3(-x, -y, z));
            vertexPositions.Add(new Vector3(-x, y, -z));
            vertexPositions.Add(new Vector3(-x, -y, -z));

            //先创建8个球，之后更新包围盒的时候让他们动
            for(int i =0;i< vertexPositions.Count;i++)
            {

                vertexRenderer.Add(GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>());

                //vertexRenderer[i].GetComponent<MeshRenderer>().enabled = false;//不显示8个AABB边缘的球
                vertexRenderer[i].transform.position = vertexPositions[i] + bounds.center;
                vertexRenderer[i].material.SetColor("_BaseColor", Color.red);
                vertexRenderer[i].transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                vertexRenderer[i].GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //关闭影子
            }
         
        }

        // Update is called once per frame
        //干了两件事，一个是更新包围盒，一个是与摄影机&灯光适配
        void Update()
        {

                UpdateAABB();

                fitToScene();

        }

        void CalculateAABB(int boundsCount, SkinnedMeshRenderer skinmeshRender)
        {
            if(boundsCount != 0)
            {

                bounds.Encapsulate(skinmeshRender.bounds);
                
            
            }
            else
            {
                bounds = skinmeshRender.bounds;
               
            }
            
            Debug.Log(skinmeshRender.name + " is being encapsulate");
            Debug.Log(boundsCount);
        }

        public void UpdateAABB()
        {


            int boundscount = 0;

            foreach(var skinmesh in skinmeshes) 
            { 
                //if(skinmesh.sharedMesh.name == "UpperBody")
                //{
                CalculateAABB(boundscount, skinmesh);
                boundscount += 1;
               // }
                  
            }


            float x = bounds.extents.x;                                       //范围这里是三维向量，分别取得X Y Z
            float y = bounds.extents.y;
            float z = bounds.extents.z;


            vertexPositions[0] = (new Vector3(x, y, z));
            vertexPositions[1] = (new Vector3(x, -y, z));
            vertexPositions[2] = (new Vector3(x, y, -z));
            vertexPositions[3] = (new Vector3(x, -y, -z));
            vertexPositions[4] = (new Vector3(-x, y, z));
            vertexPositions[5] = (new Vector3(-x, -y, z));
            vertexPositions[6] = (new Vector3(-x, y, -z));
            vertexPositions[7] = (new Vector3(-x, -y, -z));

            for (int i = 0; i < vertexPositions.Count; i++)
            {
                //  vertexRenderer.Add(GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshRenderer>());
                vertexRenderer[i].transform.position = vertexPositions[i] + bounds.center;
                vertexRenderer[i].material.SetColor("_BaseColor", Color.yellow);
                vertexRenderer[i].transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                vertexPositions[i] = vertexRenderer[i].transform.position;
            }
        }

        //然后，再定义一个计算光空间内的包围盒的最小最大值，以此确定摄像机的投影矩阵【V矩阵】
        public void fitToScene()
        {

            float xmin = float.MaxValue, xmax = float.MinValue;
            float ymin = float.MaxValue, ymax = float.MinValue;
            float zmin = float.MaxValue, zmax = float.MinValue;

            //foreach (Vector3 cornerPoints in aabbBounds) 
            //{  
            //    Vector3 pointInLightSpace = transform.worldToLocalMatrix.MultiplyPoint(cornerPoints);  
            //}

            foreach(var vertex in vertexPositions)
            {

                Vector3 vertexLS = mainLight.transform.worldToLocalMatrix.MultiplyPoint(vertex);
                xmin = Mathf.Min(xmin, vertexLS.x);
                xmax = Mathf.Max(xmax, vertexLS.x);
                ymin = Mathf.Min(ymin, vertexLS.y);
                ymax = Mathf.Max(ymax, vertexLS.y);
                zmin = Mathf.Min(zmin, vertexLS.z);
                zmax = Mathf.Max(zmax, vertexLS.z);

            }
           
            viewMatrix = mainLight.transform.worldToLocalMatrix;
          

            if (SystemInfo.usesReversedZBuffer)
            {
                viewMatrix.m20 = -viewMatrix.m20;
                viewMatrix.m21 = -viewMatrix.m21;
                viewMatrix.m22 = -viewMatrix.m22;
                viewMatrix.m23 = -viewMatrix.m23;
            }
            //注意，光源处的摄像机的position是朝向的相反方向，摄像机朝向为-Z方向，如果取反Z时，要将投影矩阵的后4个参数取负。
          

            UniversalRenderPipeline.viewMatrix = viewMatrix;//【V赋值】

            //再利用正交投影矩阵（代入包围盒最小最大值）【P矩阵】
            zmax += shadowClipDistance * shadowCaster.localScale.x;

            // //【正交矩阵】
            Vector4 row0 = new Vector4(2/(xmax - xmin),0, 0,-(xmax+xmin)/(xmax-xmin));
            Vector4 row1 = new Vector4(0, 2 / (ymax - ymin), 0, -(ymax + ymin) / (ymax - ymin));
            Vector4 row2 = new Vector4(0, 0, -2 / (zmax - zmin), -(zmax + zmin) / (zmax - zmin));
            Vector4 row3 = new Vector4(0, 0, 0, 1);
        
            projMatrix.SetRow(0, row0);
            projMatrix.SetRow(1, row1);
            projMatrix.SetRow(2, row2);
            projMatrix.SetRow(3, row3);

            UniversalRenderPipeline.projMatrix = projMatrix;//【P赋值】

        }

        public void OnDestroy()
        {
            // foreach (var sphere in vertexRenderer)
            // {
            //    vertexRenderer.Remove(sphere);
            // }
        }
    }
}
