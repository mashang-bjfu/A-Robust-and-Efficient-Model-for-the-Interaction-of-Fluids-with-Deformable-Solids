Shader "Unlit/DrawParticles"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        cull off

        // 启用透明效果涉及到的两行，详见：https://blog.csdn.net/ithot/article/details/126063491
        ZWrite Off  //关闭深度写入，透明度混合中都应关闭深度写入
        Blend SrcAlpha OneMinusSrcAlpha   //设置该Pass的混合模式，我们将源颜色（该片元着色器产生的颜色）的混合因子设为SrcAlpha，把目标颜色（已经存在于颜色缓冲中的颜色）的混合因子设为OneMinusSrcAlpha        

        //仿真结果的可视化
        Pass
        {
            CGPROGRAM


            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"


            struct particleData
            {
                float3 pos;
                float3 offset;
            };
            StructuredBuffer<particleData> _particleDataBuffer;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half2 uv: TEXCOORD0;
            };

            v2f vert(uint id : SV_VertexID)
            {
                v2f o;
                //填充第一项

                //法线
                float3 center = _particleDataBuffer[id].pos;//世界坐标系下粒子的坐标，也就是quad的中心，它是固定的
                float3 viewer = _WorldSpaceCameraPos;

                float3 normalDir = -UNITY_MATRIX_V[2].xyz;//取反，转换到世界坐标系下
                normalDir = -normalDir;//再取反，因为方向是从物体中心到摄像机而不是从摄像机到物体中心;否则方向会反，虽然圆的看不出来


                //其它两个轴【广告牌技术的两次叉乘】
                float3 upDir = abs(normalDir.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 rightDir = normalize(cross(upDir, normalDir));
                upDir = normalize(cross(normalDir, rightDir));

                //计算
                float3 tempOffset = _particleDataBuffer[id].offset;
                float3 tempPos = center + rightDir * tempOffset.x + upDir * tempOffset.y;
                o.vertex = UnityObjectToClipPos(float4(tempPos, 0));

                //填充第二项
                if (tempOffset.x > 0 && tempOffset.y > 0) {
                    o.uv = half2(1, 1);
                }
                else if (tempOffset.x > 0 && tempOffset.y < 0) {
                    o.uv = half2(1, 0);
                }
                else if (tempOffset.x < 0 && tempOffset.y < 0) {
                    o.uv = half2(0, 0);
                }
                else if (tempOffset.x < 0 && tempOffset.y>0) {
                    o.uv = half2(0, 1);
                }
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half2 uv = 2 * i.uv - 1;//坐标映射到【-1,+1】
                if (dot(uv, uv) > 1) {
                    discard;
                }
                // 大约是一年前写的了.最早的时候，真实感图形学大作业的时候，是这么写的.
                // return fixed4(0, 0, 1, 1);

                // 引入了透明度的写法.前面得声明下zwrite off ,blend ... 什么的，这里设置的alpha值才能起作用。
                return fixed4(0.3255, 0.6863, 0.7647, 0.05);                
            }

            ENDCG
        }


    }
}

