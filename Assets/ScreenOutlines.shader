Shader "Pigeon/ScreenOutlines"
{
    Properties
    {
        //_BlitTexture ("Texture", 2D) = "white" {}
        _KernelSize ("Size", Float) = 1
        _EdgeWeights ("Edge Weights (X: Color | Y: Depth | Z: Normals)", Vector) = (0.434, 0, 1, 0)
        _EdgeThresholds ("Edge Thresholds (X: Color | Y: Depth | Z: Normals | W: Far Normals)", Vector) = (0.041, 0.02, 0.88, 0.88)
        _ColorEdgeThresholdFar ("Color Edge Threshold Far", Float) = 0.8
        _DepthNormalThreshold ("Depth Normal Threshold", Float) = 0.002
        _DepthNormalThresholdScale ("Depth Normal Threshold Scale", Float) = 9.63
        _NormalsFadeDistance ("Normal Fade Distance (X: Start Distance | Y: End Distance)", Vector) = (300, 400, 0, 0)
        _ColorFadeDistance ("Color Fade Distance (X: Start Distance | Y: End Distance)", Vector) = (300, 400, 0, 0)
        [HDR] _BackgroundColor ("Background Color", Color) = (0, 0, 0, 0)
        _OutlineSceneColorMultiplier ("Outline Color Multiplier", Float) = 0.4

        _DetailTex ("Detail Texture", 2D) = "black" {}
        _DetailScaleThreshold ("X: Detail Scale | Y: Detail Threshold", Vector) = (0.02, 0.5, 0, 0)
        _DetailFadeDistance ("Detail Fade Distance (X: Start Distance | Y: End Distance", Vector) = (2, 4, 0, 0)
        _LighterOutlineThreshold ("LighterOutlineThreshold", Float) = 0

        _FogIntensity ("Fog Intensity", Float) = 1
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #include "Assets/Shaders/Core/NoisyNodes/NoiseShader/HLSL/ClassicNoise2D.hlsl"
            #include "Assets/Shaders/Core/ShadowVolume.hlsl"
            //#include "Assets/Shaders/Core/LightingV2.hlsl"

            struct v2f
            {
                float2 uv            : TEXCOORD0;
                float4 vertex        : SV_POSITION;
                float3 viewDirection : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            //TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;

            float _KernelSize;

            float3 _EdgeWeights;
            float4 _EdgeThresholds;
            float _ColorEdgeThresholdFar;
            float _DepthNormalThreshold;
            float _DepthNormalThresholdScale;
            float2 _NormalsFadeDistance;
            float2 _ColorFadeDistance;

            float4 _BackgroundColor;
            float _OutlineSceneColorMultiplier;

            float _LighterOutlineThreshold;

            TEXTURE2D(_DetailTex);
            SAMPLER(sampler_DetailTex);
            float2 _DetailScaleThreshold;
            float2 _DetailFadeDistance;

            float4x4 _ClipToView;

            float _FogIntensity;

            float4 _FogParams;
            float3 _DistanceFogParams;
            float3 _FogColor;
            #define FogHeight _FogParams.x
            #define FogFadeDistance _FogParams.y
            #define FogNoiseAmplitude _FogParams.z
            #define FogNoiseFrequency _FogParams.w

            #define DistanceFogStartDistance _DistanceFogParams.x
            #define DistanceFogFadeDistance _DistanceFogParams.y
            #define DistanceFogAlpha _DistanceFogParams.z

            //static const int RobertsCrossX[4] = {
            //    1, 0,
            //    0, -1
            //};
            
            //static const int RobertsCrossY[4] = {
            //    0, 1,
            //    -1, 0
            //};

            #define RobertsCrossX0 1
            #define RobertsCrossX3 -1
            #define RobertsCrossY1 1
            #define RobertsCrossY2 -1

            TEXTURE2D(_EnemyMask);
            SAMPLER(sampler_EnemyMask);

            float hashF(uint state)
            {
                state ^= 2747636419u;
                state *= 2654435769u;
                state ^= state >> 16;
                state *= 2654435769u;
                state ^= state >> 16;
                state *= 2654435769u;
                return state / 4294967295.0;
            }

            float normalizeRand(uint state)
            {
                return state / 4294967295.0;
            }

            // Combines the top and bottom colors using normal blending.
			// https://en.wikipedia.org/wiki/Blend_modes#Normal_blend_mode
			// This performs the same operation as Blend SrcAlpha OneMinusSrcAlpha.
			float4 alphaBlend(float4 top, float4 bottom)
			{
				float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
				float alpha = top.a + bottom.a * (1 - top.a);
				return float4(color, alpha);
			}

            v2f vert (Attributes input)
            {
                //v2f output = (v2f)0;
                //UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                //VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                //output.vertex = vertexInput.positionCS;
                //output.uv = v.uv;
                //output.viewDirection = mul(_ClipToView, output.vertex).xyz;

                //return output;


                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            #if SHADER_API_GLES
                float4 pos = input.positionOS;
                float2 uv  = input.uv;
            #else
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
            #endif

                output.vertex = pos;
                output.uv = uv; // * _BlitScaleBias.xy + _BlitScaleBias.zw;
                output.viewDirection = mul(_ClipToView, output.vertex).xyz;
                return output;
            }

            float3 DecodeNormal(float3 enc)
            {
                float kScale = 1.7777;
                float3 nn = enc*float3(2*kScale,2*kScale,0) + float3(-kScale,-kScale,1);
                float g = 2.0 / dot(nn.xyz,nn.xyz);
                float3 n;
                n.xy = g*nn.xy;
                n.z = g-1;
                return n;
            }

            float luminance(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float4 frag (v2f i) : SV_Target
            {
                float isEnemy = SAMPLE_TEXTURE2D(_EnemyMask, sampler_EnemyMask, i.uv).r;
                // isEnemy = 0;
                // if (isEnemy == 1.0) return 1;

                // Calculate world position from screen uv and depth
                float depth = SampleSceneDepth(i.uv);
                float realDepth = _ProjectionParams.y / depth;
                float3 worldPos = ComputeWorldSpacePosition(i.uv, depth, UNITY_MATRIX_I_VP);
                
                // Calculate outline size based on screen width ration to 4K
                // Don't let our size go below 1.4 or it'll look BAD
                float kernelSize = max(_KernelSize * _ScreenParams.x / 3840.0, 1.4);
                float halfScaleFloor = kernelSize * 0.5;
                float halfScaleCeil = kernelSize * 0.5;

                // Calculate uvs diagonal to the current pixel
                float2 bottomLeftUV = i.uv - float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y) * halfScaleFloor;
                float2 topRightUV = i.uv + float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y) * halfScaleCeil;  
                float2 bottomRightUV = i.uv + float2(_BlitTexture_TexelSize.x * halfScaleCeil, -_BlitTexture_TexelSize.y * halfScaleFloor);
                float2 topLeftUV = i.uv + float2(-_BlitTexture_TexelSize.x * halfScaleFloor, _BlitTexture_TexelSize.y * halfScaleCeil);

                float3 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv).xyz;
                
                // Sample screen color at diagonal uvs
                float3 horizontalColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, topLeftUV).rgb * RobertsCrossX0; // top left (factor +1)
                horizontalColor += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, bottomRightUV).rgb * RobertsCrossX3; // bottom right (factor -1)
                float3 verticalColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, bottomLeftUV).rgb * RobertsCrossY2; // bottom left (factor -1)
                verticalColor += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, topRightUV).rgb * RobertsCrossY1; // top right (factor +1)

                // Sample screen normals at diagonal uvs
                float3 normal0 = SampleSceneNormals(bottomLeftUV); // top left (factor +1)
                float3 normal1 = SampleSceneNormals(topRightUV); // bottom right (factor -1)
                float3 normal2 = SampleSceneNormals(bottomRightUV); // bottom left (factor -1)
                float3 normal3 = SampleSceneNormals(topLeftUV); // top right (factor +1)
                float3 normalDifference0 = normal1 - normal0;
                float3 normalDifference1 = normal3 - normal2;

                // Sample screen depth at diagonal uvs
                // float depthSize = 0;
                // float depth0 = SampleSceneDepth(bottomLeftUV - float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y) * halfScaleFloor * depthSize); // top left (factor +1)
                // float depth1 = SampleSceneDepth(topRightUV + float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y) * halfScaleCeil * depthSize); // bottom right (factor -1)
                // float depth2 = SampleSceneDepth(bottomRightUV + float2(_BlitTexture_TexelSize.x * halfScaleCeil, -_BlitTexture_TexelSize.y * halfScaleFloor) * depthSize); // bottom left (factor -1)
                // float depth3 = SampleSceneDepth(topLeftUV + float2(-_BlitTexture_TexelSize.x * halfScaleFloor, _BlitTexture_TexelSize.y * halfScaleCeil) * depthSize); // top right (factor +1)
                // float depthDifference0 = depth1 - depth0;
                // float depthDifference1 = depth3 - depth2;

                // Transform depth to world space units for fading color edge threshold based on distance
                float colorFade = realDepth;
                colorFade = saturate((colorFade - _ColorFadeDistance.x) / (_ColorFadeDistance.y - _ColorFadeDistance.x));

                // Make color outlines less sensitive as the pixel's luminance gets lower
                float colorThresholdMultiplier = luminance(sceneColor);
                if (colorThresholdMultiplier >= 0.4)
                {
                    colorThresholdMultiplier *= 1.8;
                }

                //float colorThresholdMultiplier = lerp(luminance(sceneColor), 1, 0.5);
                //colorThresholdMultiplier *= colorThresholdMultiplier;
                //colorThresholdMultiplier = 1;

                // Calculate change in color at the current pixel
                // return sceneColor.r > 1 || sceneColor.g > 1 || sceneColor.b > 1 ? float4(1, 0, 0, 1) : float4(0, 0, 0, 1);
                float colorEdge = sqrt(dot(horizontalColor, horizontalColor) + dot(verticalColor, verticalColor)) * _EdgeWeights.x < 
                lerp(_EdgeThresholds.x, _ColorEdgeThresholdFar, colorFade) * colorThresholdMultiplier ? 0 : 1;

                float3 viewNormal = DecodeNormal(normal0) * 2 - 1;
                float nDotV = 1 - dot(viewNormal, -i.viewDirection);
                float normalThreshold = saturate((nDotV - _DepthNormalThreshold) / (1 - _DepthNormalThreshold));
                normalThreshold = normalThreshold * _DepthNormalThresholdScale + 1;
                
                /// DEPTH EDGE
                // float depthThreshold = _EdgeThresholds.y/* * depth*/ * normalThreshold;
                // float depthEdge = sqrt(depthDifference0 * depthDifference0 + depthDifference1 * depthDifference1) * 100 * _EdgeWeights.y < depthThreshold ? 0 : 1;

                // Transform depth to world space units
                float normalFade = realDepth;
                normalFade = saturate((normalFade - _NormalsFadeDistance.x) / (_NormalsFadeDistance.y - _NormalsFadeDistance.x));

                // Calculate change in normal vector at this pixel
                float normalsEdge = sqrt(dot(normalDifference0, normalDifference0) + dot(normalDifference1, normalDifference1)) * _EdgeWeights.z < lerp(_EdgeThresholds.z, _EdgeThresholds.w, normalFade) ? 0 : 1;

                float edge = min(colorEdge + /*depthEdge + */normalsEdge, 1);

                // Add triplanar detail texture
                float3 nodeBlend = pow(abs(normal0), 4);
                nodeBlend /= dot(nodeBlend, 1.0);
                float pX = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, worldPos.zy * _DetailScaleThreshold.x).r > _DetailScaleThreshold.y ? 1 : 0;
                float pY = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, worldPos.xz * _DetailScaleThreshold.x).r > _DetailScaleThreshold.y ? 1 : 0;
                float pZ = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, worldPos.xy * _DetailScaleThreshold.x).r > _DetailScaleThreshold.y ? 1 : 0;
                float p = pX * nodeBlend.x + pY * nodeBlend.y + pZ * nodeBlend.z;
                float detailFade = realDepth;
                detailFade = saturate((detailFade - _DetailFadeDistance.x) / (_DetailFadeDistance.y - _DetailFadeDistance.x));
                //return float4(detailFade, detailFade, detailFade, 1);
                edge = max(edge, p * detailFade);

                /// Blend outline color with screen color
                

                // Add to outline color if the scene color is too dark (so that a black pixel doesn't get a black outline)
                //float outlineIntensityAdd = (sceneColor.x * sceneColor.x + sceneColor.y * sceneColor.y + sceneColor.z * sceneColor.z) <= 
                //    _LighterOutlineThreshold * _LighterOutlineThreshold ? 0.075 : 0;

                float depthSize = 3;
                depthSize = lerp(3, 2, saturate((realDepth - 30) / (45 - 30)));
                bottomLeftUV -= float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y) * halfScaleFloor * depthSize; // top left (factor +1)
                topRightUV += float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y) * halfScaleCeil * depthSize; // bottom right (factor -1)
                bottomRightUV += float2(_BlitTexture_TexelSize.x * halfScaleCeil, -_BlitTexture_TexelSize.y * halfScaleFloor) * depthSize; // bottom left (factor -1)
                topLeftUV += float2(-_BlitTexture_TexelSize.x * halfScaleFloor, _BlitTexture_TexelSize.y * halfScaleCeil) * depthSize; // top right (factor +1)

                float hEnemy = SAMPLE_TEXTURE2D(_EnemyMask, sampler_EnemyMask, topLeftUV).r * RobertsCrossX0; // top left (factor +1)
                hEnemy += SAMPLE_TEXTURE2D(_EnemyMask, sampler_EnemyMask, bottomRightUV).r * RobertsCrossX3; // bottom right (factor -1)
                float vEnemy = SAMPLE_TEXTURE2D(_EnemyMask, sampler_EnemyMask, bottomLeftUV).r * RobertsCrossY2; // bottom left (factor -1)
                vEnemy += SAMPLE_TEXTURE2D(_EnemyMask, sampler_EnemyMask, topRightUV).r * RobertsCrossY1; // top right (factor +1)

                float outlineColorMultiplier = _OutlineSceneColorMultiplier;
                if (isEnemy)
                {
                    outlineColorMultiplier = lerp(0.3, 0.6, saturate((realDepth - 10) / (35 - 10)));
                    outlineColorMultiplier *= 1.0 - sqrt(hEnemy * hEnemy + vEnemy * vEnemy) * 0.8;
                }

                float4 outlineColor = float4(outlineColorMultiplier * edge * sceneColor/* + outlineIntensityAdd*/, edge);
                float4 cameraColor = float4(lerp(sceneColor, _BackgroundColor.xyz, _BackgroundColor.a), 1);
                //return alphaBlend(outlineColor, cameraColor);
                float4 result = alphaBlend(outlineColor, cameraColor);

                float fogHeight = FogHeight + cnoise(worldPos.xz * FogNoiseFrequency) * FogNoiseAmplitude;
                float height = min(worldPos.y, _WorldSpaceCameraPos.y);
                float worldDistance = length(worldPos - _WorldSpaceCameraPos);

                if (height < fogHeight)
                {
                    //float3 lightDir;
                    //float3 lightColor;
                    //half distanceAtten;
                    //half shadowAtten;
                    //CalculateMainLight_float(worldPos, lightDir, lightColor, distanceAtten, shadowAtten);

                    float3 fogColor = _FogColor;
                    fogColor *= 1.0 + cnoise(worldPos.xz * FogNoiseFrequency) * 0.1;
                    //float distance = lerp(saturate((fogHeight - height) / FogFadeDistance), saturate(length(worldPos - _WorldSpaceCameraPos)) / 5.0, distanceBlend);
                    float distance = saturate((fogHeight - height) / FogFadeDistance) * saturate(worldDistance / 25.0);
                    result.rgb = lerp(result.rgb, fogColor, distance * _FogIntensity);
                }

                if (worldDistance > DistanceFogStartDistance)
                {
                    result.rgb = lerp(result.rgb, _FogColor * (_GlobalAmbientLight.x * _GlobalAmbientLight.y), saturate((worldDistance - DistanceFogStartDistance) / DistanceFogFadeDistance) * DistanceFogAlpha * _FogIntensity);
                }

                //float3 screenWorldPos = ComputeWorldSpacePosition(In.xy, In.z, UNITY_MATRIX_I_VP);

                return result;
            }
            ENDHLSL
        }
    }
}