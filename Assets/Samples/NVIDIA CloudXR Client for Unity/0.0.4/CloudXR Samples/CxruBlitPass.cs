/*
* Copyright (c) 2022-2023, NVIDIA CORPORATION.  All rights reserved.
*
* NVIDIA CORPORATION, its affiliates and licensors retain all intellectual property
* and proprietary rights in and to this material, related documentation
* and any modifications thereto.  Any use, reproduction, disclosure or
* distribution of this material and related documentation without an express
* license agreement from NVIDIA CORPORATION or its affiliates is strictly prohibited.
*/

using System.IO;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using CloudXR;

    class CxruBlitPass : ScriptableRenderPass
    {

        private RTHandle m_CameraColorTarget;
        private Material m_Material;

        // ========================================================================
        // Constructor and SetTarget are called by CxrRendererFeature
        public CxruBlitPass(Material material)
        {
            m_Material = material;

            // TODO: Maybe make this a public settable enumeration
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        public void SetTarget(RTHandle colorHandle)
        {
            m_CameraColorTarget = colorHandle;
        }



        // ========================================================================
        // This is needed to ensure our pass uses the right render target.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(m_CameraColorTarget);
        }

        // ========================================================================
        //  Unity calls this to enqueue graphics commands, NOT to actually perform them

        // private Texture2DArray xrTex = null;

        CloudXRManager cxrManager = null;
        // CxruClientSampleAutoconnect mainScript = null;

        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (camera.cameraType != CameraType.Game)
                return;

            if (cxrManager == null)
                cxrManager = camera.GetComponent<CloudXRManager>();
            if (cxrManager == null) return;
            StereoPosedFrame theFrame = cxrManager.latestValidFrame;
            if (theFrame == null) return;
            if (theFrame.leftFrame.tex == null) return;
            if (theFrame.rightFrame.tex == null) return;

            // mainScript = camera.GetComponent<CxruClientSampleAutoconnect>();
            // if (mainScript == null) return;
            
            CommandBuffer cmd = CommandBufferPool.Get();

            Texture2D left = theFrame.leftFrame.tex, right = theFrame.rightFrame.tex;

            m_Material.SetTexture("_LeftEyeTex", left);
            m_Material.SetTexture("_RightEyeTex", right);

            m_Material.SetMatrix("_StreamingRotation", Matrix4x4.TRS(Vector3.zero, theFrame.pose.poseInUnityCoords.angular_position, Vector3.one));

            /*
            if (mainScript.isPaused) {
                m_Material.SetFloat("_Intensity",0.5f);
            }
            else
                m_Material.SetFloat("_Intensity",1.0f);
            */
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material, 0, -1, m_PropertyBlock);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            

            /*
            // Alternate rendering path that uses Tex2DArray (the stereo textures stacked into an array)
            if ((xrTex==null) || (left.width!=xrTex.width))
                xrTex = new Texture2DArray(left.width,left.height,2,TextureFormat.RGBA32, false, true);

            cmd.CopyTexture(left,0,0,0,0,left.width,left.height,xrTex,0,0,0,0);
            cmd.CopyTexture(right,0,0,0,0,right.width,right.height,xrTex,1,0,0,0);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            m_Material.SetTexture("_BothEyeTex", xrTex);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material, 0, -1, m_PropertyBlock);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            */

            CommandBufferPool.Release(cmd);
        }



        void LogContextData( ref RenderingData renderingData) {
            string msg = " ===================================================================== renderingData.cameraData\n";
            msg += $"    antialiasing {renderingData.cameraData.antialiasing}\n";
            msg += $"    antialiasingQuality {renderingData.cameraData.antialiasingQuality}\n";
            msg += $"    cameraTargetDescriptor {renderingData.cameraData.cameraTargetDescriptor}\n";
            msg += $"           autoGenerateMips {renderingData.cameraData.cameraTargetDescriptor.autoGenerateMips}\n";
            msg += $"           bindMS {renderingData.cameraData.cameraTargetDescriptor.bindMS}\n";
            msg += $"           colorFormat {renderingData.cameraData.cameraTargetDescriptor.colorFormat}\n";
            msg += $"           depthBufferBits {renderingData.cameraData.cameraTargetDescriptor.depthBufferBits}\n";
            #if UNITY_2022_1_OR_NEWER
            msg += $"           depthStencilFormat {renderingData.cameraData.cameraTargetDescriptor.depthStencilFormat}\n";
            #endif
            msg += $"           dimension {renderingData.cameraData.cameraTargetDescriptor.dimension}\n";
            msg += $"           enableRandomWrite {renderingData.cameraData.cameraTargetDescriptor.enableRandomWrite}\n";
            msg += $"           flags {renderingData.cameraData.cameraTargetDescriptor.flags}\n";
            msg += $"           graphicsFormat {renderingData.cameraData.cameraTargetDescriptor.graphicsFormat}\n";
            msg += $"           height {renderingData.cameraData.cameraTargetDescriptor.height}\n";
            msg += $"           memoryless {renderingData.cameraData.cameraTargetDescriptor.memoryless}\n";
            msg += $"           mipCount {renderingData.cameraData.cameraTargetDescriptor.mipCount}\n";
            msg += $"           msaaSamples {renderingData.cameraData.cameraTargetDescriptor.msaaSamples}\n";
            msg += $"           shadowSamplingMode {renderingData.cameraData.cameraTargetDescriptor.shadowSamplingMode}\n";
            msg += $"           sRGB {renderingData.cameraData.cameraTargetDescriptor.sRGB}\n";
            msg += $"           stencilFormat {renderingData.cameraData.cameraTargetDescriptor.stencilFormat}\n";
            msg += $"           useDynamicScale {renderingData.cameraData.cameraTargetDescriptor.useDynamicScale}\n";
            msg += $"           useMipMap {renderingData.cameraData.cameraTargetDescriptor.useMipMap}\n";
            msg += $"           volumeDepth {renderingData.cameraData.cameraTargetDescriptor.volumeDepth}\n";
            msg += $"           vrUsage {renderingData.cameraData.cameraTargetDescriptor.vrUsage}\n";
            msg += $"           width {renderingData.cameraData.cameraTargetDescriptor.width}\n";
            msg += $"    cameraType {renderingData.cameraData.cameraType}\n";
            msg += $"    captureActions {renderingData.cameraData.captureActions}\n";
            msg += $"    clearDepth {renderingData.cameraData.clearDepth}\n";
            msg += $"    defaultOpaqueSortFlags {renderingData.cameraData.defaultOpaqueSortFlags}\n";
            msg += $"    isDefaultViewport {renderingData.cameraData.isDefaultViewport}\n";
            msg += $"    isDitheringEnabled {renderingData.cameraData.isDitheringEnabled}\n";
            msg += $"    isHdrEnabled {renderingData.cameraData.isHdrEnabled}\n";
            msg += $"    isStopNaNEnabled {renderingData.cameraData.isStopNaNEnabled}\n";
            msg += $"    maxShadowDistance {renderingData.cameraData.maxShadowDistance}\n";
            msg += $"    postProcessEnabled {renderingData.cameraData.postProcessEnabled}\n";
            #if UNITY_2022_1_OR_NEWER
            msg += $"    postProcessingRequiresDepthTexture {renderingData.cameraData.postProcessingRequiresDepthTexture}\n";
            #endif
            msg += $"    renderer {renderingData.cameraData.renderer}\n";
            msg += $"    renderScale {renderingData.cameraData.renderScale}\n";
            msg += $"    renderType {renderingData.cameraData.renderType}\n";
            msg += $"    requiresDepthTexture {renderingData.cameraData.requiresDepthTexture}\n";
            msg += $"    requiresOpaqueTexture {renderingData.cameraData.requiresOpaqueTexture}\n";
            msg += $"    resolveFinalTarget {renderingData.cameraData.resolveFinalTarget}\n";
            msg += $"    targetTexture {renderingData.cameraData.targetTexture}\n";
            msg += $"    volumeLayerMask {renderingData.cameraData.volumeLayerMask}\n";
            msg += $"    volumeTrigger {renderingData.cameraData.volumeTrigger}\n";
            #if UNITY_2022_1_OR_NEWER
            msg += $"    worldSpaceCameraPos {renderingData.cameraData.worldSpaceCameraPos}\n";
            #endif
            msg += $"    isPreviewCamera {renderingData.cameraData.isPreviewCamera}\n";
            msg += $"    isSceneViewCamera {renderingData.cameraData.isSceneViewCamera}\n";
            Debug.Log(msg);
            msg = "============================================== m_CameraColorTarget (RTHandle)\n";
            msg += $"    isMSAAEnabled {m_CameraColorTarget.isMSAAEnabled}\n";
            msg += $"    name {m_CameraColorTarget.name}\n";
            msg += $"    nameID {m_CameraColorTarget.nameID}\n";
            msg += $"    referenceSize {m_CameraColorTarget.referenceSize}\n";
            msg += $"    rt {m_CameraColorTarget.rt}\n";
            msg += $"    rtHandleProperties {m_CameraColorTarget.rtHandleProperties}\n";
            msg += $"    scaleFactor {m_CameraColorTarget.scaleFactor}\n";
            msg += $"    useScaling {m_CameraColorTarget.useScaling}\n";
            Debug.Log(msg);
                    
        }
    }
