/*
* Copyright (c) 2022-2023, NVIDIA CORPORATION.  All rights reserved.
*
* NVIDIA CORPORATION, its affiliates and licensors retain all intellectual property
* and proprietary rights in and to this material, related documentation
* and any modifications thereto.  Any use, reproduction, disclosure or
* distribution of this material and related documentation without an express
* license agreement from NVIDIA CORPORATION or its affiliates is strictly prohibited.
*/

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace CloudXR {
    internal class CxruRendererFeature : ScriptableRendererFeature
    {
        public Shader m_Shader;

        Material m_Material;
        CxruBlitPass m_RenderPass = null;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
                renderer.EnqueuePass(m_RenderPass);
        }

    #if UNITY_2022_1_OR_NEWER
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                m_RenderPass.SetTarget(renderer.cameraColorTargetHandle);
            }
        }
    #endif


        public override void Create()
        {

            Debug.Assert(m_Shader != null,"Null shader");

            m_Material = new Material(m_Shader);
            m_RenderPass = new CxruBlitPass(m_Material);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
        }

    }
}