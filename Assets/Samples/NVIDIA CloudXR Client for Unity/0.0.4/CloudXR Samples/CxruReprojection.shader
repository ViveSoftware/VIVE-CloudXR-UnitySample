// *
// * Copyright (c) 2019-2020, NVIDIA CORPORATION.  All rights reserved.
// *
// * NVIDIA CORPORATION and its licensors retain all intellectual property
// * and proprietary rights in and to this software, related documentation
// * and any modifications thereto.  Any use, reproduction, disclosure or
// * distribution of this software and related documentation without an express
// * license agreement from NVIDIA CORPORATION is strictly prohibited.
// *

Shader "CxruReprojection"
{
    Properties
    {
        //_BothEyeTex("Texture2DArray", 2DArray) = "black" {}
        _LeftEyeTex("Texture", 2D) = "grey" {}
        _RightEyeTex("Texture", 2D) = "grey" {}
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "CxruShaderPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionHCS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float2  uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Note: The pass is setup with a mesh already in clip
                // space, that's why, it's enough to just output vertex
                // positions
                output.positionCS = float4(input.positionHCS.xyz, 1.0);

                output.uv = input.uv;

                #if UNITY_UV_STARTS_AT_TOP
                output.positionCS.y *= -1;
                // Invert vertical axis manually?!?? TODO WHY DO I HAVE TO DO THIS?!?
                output.uv.y = 1.0 - output.uv.y;
                #endif

                return output;

                //float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
                //return uv.xy * scaleOffset.xy + scaleOffset.zw * w;

            }

            // TEXTURE2D_X(_BothEyeTex);
            // SAMPLER(sampler_BothEyeTex);

            TEXTURE2D(_LeftEyeTex);
            SAMPLER(sampler_LeftEyeTex);
            TEXTURE2D(_RightEyeTex);
            SAMPLER(sampler_RightEyeTex);
            float4 _RightEyeTex_TexelSize;

            float4 SampleEye(float2 uv){
                float4 c=0;
                // uv=(floor(uv.xy * _RightEyeTex_TexelSize.zw) + 0.5) * _RightEyeTex_TexelSize.xy;
                if (unity_StereoEyeIndex == 0) {
                    // c = SAMPLE_TEXTURE2D_X_LOD(_LeftEyeTex, sampler_LeftEyeTex, uv, 0.0) ;
                    // c = _LeftEyeTex.Sample(eyeclampSampler, uv);
                    c = _LeftEyeTex.SampleLevel(sampler_LeftEyeTex, uv,0);
                }else{
                    // c = SAMPLE_TEXTURE2D_X_LOD(_RightEyeTex, sampler_RightEyeTex, uv, 0.0) ;
                    // c = _RightEyeTex.Sample(eyeclampSampler, uv);
                    c = _RightEyeTex.SampleLevel(sampler_RightEyeTex, uv,0);
                    
                }
                return c;
            }

            #define _ReprojectionEnabled true
            #define _NoiseDitherEnabled false //add per pixel noise do dither colors (unity dither options did not work)
            #define _UVClampEnabled false // clamp uv's with margin, cutting out few pixels from the edges
            #define _BorderFill true //draw black color outside uv bounds
            #define _ColorConvert false

            float4x4 _StreamingRotation;

            float4 rndz(int3 p, int s) {int4 c=int4(p.xyz,s);int r=(int(0x3504f333*c.x*c.x+c.y)*int(0xf1bbcdcb*c.y*c.y+c.x)*int(0xbf5c3da7*c.z*c.z+c.y)*int(0x2eb164b3*c.w*c.w+c.z));
            int4 r4=int4(0xbf5c3da7,0xa4f8e125,0x9284afeb,0xe4f5ae21)*r;return (float4(r4)*(2.0/8589934592.0)+0.5)*0.99999;}

            float2 ReprojectUV(float2 uv,float4x4 tV,float4x4 tVS){
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y=1.0-uv.y;
                #endif
                //ray direction from screenspace uv's
                float3 rd = normalize(ComputeViewSpacePosition(float2(uv.x,uv.y), 0.9, UNITY_MATRIX_I_P));
                rd.z=-rd.z;
                rd=mul(rd,transpose((float3x3)tV));// current view ray direction in world space
                
                //streamed frame ray direction
                float3 rds=mul(rd,(float3x3)tVS); 
                // float3 ro=_WorldSpaceCameraPos;
                // The view space uses a right-handed coordinate system.
                rd.z=-rd.z;
                rds.z=-rds.z;
                // ro.z=-ro.z;
        
                float4 pp=mul(UNITY_MATRIX_P,float4(normalize(rds.xyz),1));
                float2 uvp=(pp.xy/pp.w)*float2(1,1)*.5*(-sign(rds.z))+float2(.5,.5);
                return uvp;
            }

            float3 ConvertSRGBToLinear(float3 srgbColor)
            {
                // sRGB > linear conversion
                // https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Colorspace-Conversion-Node.html
                return float3(
                    (srgbColor.r <= 0.04045) ? srgbColor.r / 12.92 : pow((srgbColor.r + 0.055) / 1.055, 2.4),
                    (srgbColor.g <= 0.04045) ? srgbColor.g / 12.92 : pow((srgbColor.g + 0.055) / 1.055, 2.4),
                    (srgbColor.b <= 0.04045) ? srgbColor.b / 12.92 : pow((srgbColor.b + 0.055) / 1.055, 2.4)
                );
            }
            float4 ConvertSRGBAToLinear(float4 srgbaColor)
            {
                float3 linearRGB = ConvertSRGBToLinear(srgbaColor.rgb); // Use the previously defined function for RGB channels.
                return float4(linearRGB, srgbaColor.a); // Keep the alpha channel as-is.
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // half4 color = SAMPLE_TEXTURE2D_X(_BothEyeTex, sampler_BothEyeTex, input.uv);
                float4 color=float4(0,0,0,0);
                float2 uvp=input.uv;


                if(_ReprojectionEnabled)uvp=ReprojectUV(uvp,UNITY_MATRIX_I_V,_StreamingRotation);

                bool bmask=any(abs(uvp-0.5)>.5);

                if(_UVClampEnabled){
                    float2 clampmargin=float2(2,9)*_RightEyeTex_TexelSize.xy;
                    uvp=clamp(uvp,clampmargin,1.0-clampmargin);
                }

                color.rgb=SampleEye(uvp);
                // This is needed for Lenovo VRX to display non-black pixels.
                // It does not seem to impact other platforms.
                color.a = float(1);

                if(_ColorConvert){
                    color = ConvertSRGBAToLinear(color);
                }
                
                if(_NoiseDitherEnabled){
                    int seed=10000*frac(_SinTime.w*100);
                    color.rgb+=(rndz(int3(input.uv.xy*4000,1),seed).xyz-0.5)/256.;
                }

                if(_BorderFill){
                    color.rgb=lerp(color.rgb,float3(0,0,0),bmask);
                }
                return color;
            }
            ENDHLSL
       }
    }
}