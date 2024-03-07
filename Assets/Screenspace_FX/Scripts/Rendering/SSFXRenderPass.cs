using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SSFXParticles;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Rendering;
using System.Runtime.Serialization.Formatters.Binary;

public class SSFXRenderPass : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private RTHandle rtCameraColor;
        private RTHandle rtCameraDepth;
        private RTHandle rtColorCopy;
        private RTHandle rtDepthSSFX;
        private Settings settings;
        private FilteringSettings filteringSettingsOverride;
        private FilteringSettings filteringSettingsGeneric;
        // List for object that generate particles without SSFX mat.
        private List<ShaderTagId> shaderTagsListMatOverride = new List<ShaderTagId>();
        private List<ShaderTagId> shaderTagsList = new List<ShaderTagId>();
        private ProfilingSampler _profilingSampler;
        private const int WARP_SIZE = 256;

        private ParticlesBuffer ssfxDatas;

        public CustomRenderPass(Settings settings, string name)
        {
            // pass our settings class to the pass, so we can access them inside OnCameraSetup/Execute/etc
            this.settings = settings;
            // set up ProfilingSampler used in Execute method
            _profilingSampler = new ProfilingSampler(name);
            filteringSettingsOverride = new FilteringSettings(RenderQueueRange.opaque, settings.layerMaskMaterialOverride);
            filteringSettingsGeneric = new FilteringSettings(RenderQueueRange.opaque, ~settings.layerMaskMaterialOverride);
            // Use URP's default shader tags
            shaderTagsListMatOverride.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagsListMatOverride.Add(new ShaderTagId("UniversalForward"));

            shaderTagsList.Add(new ShaderTagId("DepthPrepassSSFX"));
            shaderTagsList.Add(new ShaderTagId("OpaquePassSSFX"));

            ssfxDatas = new ParticlesBuffer();
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            rtCameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
            rtCameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            // Create a Depth Target
            var depthDesc = renderingData.cameraData.cameraTargetDescriptor;
            depthDesc.depthBufferBits = 32; // should be default anyway
            RenderingUtils.ReAllocateIfNeeded(ref rtDepthSSFX, depthDesc,
                name: "RT_DepthSSFX");

            var colorDebugDesc = renderingData.cameraData.cameraTargetDescriptor;
            colorDebugDesc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref rtColorCopy, colorDebugDesc,
                name: "RT_ColorCopySSFX");

            ConfigureTarget(rtCameraColor, rtCameraDepth);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.enableDebug)
                Debug.Log("[SSFX] Execute SSFX Render Pass");

            if (settings.buffersInfo.Count() == 0)
            {
                Debug.LogWarning("[SSFXRender] Can't render ssfx particles. MaxParticles setting at 0");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, _profilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, rtCameraColor, rtColorCopy);
                cmd.SetRenderTarget(rtCameraColor.rt, rtCameraDepth.rt);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                /*
                Note : should always ExecuteCommandBuffer at least once before using
                ScriptableRenderContext functions (e.g. DrawRenderers) even if you 
                don't queue any commands! This makes sure the frame debugger displays 
                everything under the correct title.
                */

                EditorUtils.UpdateTimePassed();
                cmd.SetGlobalVector("_Time_SSFX", new Vector4(EditorUtils.GetTimePassed(), EditorUtils.GetDeltaTime(), 0, 0));

                SSFXRenderPassUtils.CheckResources(ref ssfxDatas, settings);
                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagsList, ref renderingData, sortingCriteria);
                DrawingSettings drawingSettingsOverride = CreateDrawingSettings(shaderTagsListMatOverride, ref renderingData, sortingCriteria);
                //drawingSettings.SetShaderPassName(0, new ShaderTagId("OpaqueSSFXPass"));

                //*** Depth prepass ***// Avoid to draw multiple particles per pixel
                //SSFXRenderPassUtils.RenderDepthPrepass(context, renderingData, drawingSettings, filteringSettingsOverride, settings.overrideMaterial);

                //*** Opaque pass - Generate new particles ***//
                SSFXRenderPassUtils.SetMaterialDataOpaquePass(cmd, ssfxDatas, settings, rtColorCopy);
                SSFXRenderPassUtils.RenderOpaque(context, renderingData, drawingSettings, filteringSettingsGeneric);
                if (settings.overrideMaterial)
                    SSFXRenderPassUtils.RenderOpaqueOverride(context, renderingData, drawingSettingsOverride, filteringSettingsOverride, settings.overrideMaterial);

                //*** Set indirect args for compute shaders ***//
                SSFXRenderPassUtils.ComputeSetIndirectArgs(cmd, ssfxDatas, settings);

                //*** Particle Buffer Union - Merge old and new particles in the same buffer ***//
                SSFXRenderPassUtils.ComputeParticlesBufferUnion(cmd, ssfxDatas, settings);

                //*** Particle Simulation - Handle the mouvement of the particles ***//
                SSFXRenderPassUtils.ComputeParticlesSimulation(cmd, ssfxDatas, settings);

                //SSFXRenderPassUtils.GetParticlesEmittedCount(ssfxDatas, settings);

                //** Particle Draw Pass **//
                for (int i = 0; i < settings.buffersInfo.Count(); i++)
                {
                    SSFXBufferInfo bInfo = settings.buffersInfo[i];
                    SSFXRenderPassUtils.RenderParticles(cmd, ssfxDatas, bInfo, i);
                }

                ssfxDatas.actualFrame += 1;


                ComputeBuffer tmp = ssfxDatas.previousParticlesDatasBuffer;
                ssfxDatas.previousParticlesDatasBuffer = ssfxDatas.particlesDatasBuffer;
                ssfxDatas.particlesDatasBuffer = tmp;
                SSFXRenderPassUtils.ComputeClearDrawArgs(cmd, ssfxDatas, settings);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Called every frame after the Execute function
        }

        public void ReleaseMemory()
        {
            rtDepthSSFX?.Release();
            SSFXRenderPassUtils.FreeResources(ref ssfxDatas);
        }
    }

    CustomRenderPass m_ScriptablePass;

    [System.Serializable]
    public struct SSFXBufferInfo
    {
        public int maxParticlesEmitted;
        public Material particlesMaterial;
        public Mesh particlesMesh;
    }

    [System.Serializable]
    public class Settings
    {
        [Header("Global Renderers Settings")]
        public LayerMask layerMaskMaterialOverride = 1;

        public SSFXBufferInfo[] buffersInfo;

        public ComputeShader particlesComputeShader;
        public bool prioritizeNewParticles;
        public bool enableDebug;
        public float floorHeight;
        public float gravity;
        public float maxParticleSpeed = 2;
        public Texture noiseMap;
        [Tooltip("XY = Tilling, Z = Strength, W = ?")]
        public Vector4 noiseMapSettings = new Vector4(1, 1, 0, 0);
        [Tooltip("XYZ = direction, W = speed")]
        public Vector4 windDirection;

        [Header("Override Settings")]
        public Material overrideMaterial;

    }

    public Settings settings;
    public RenderPassEvent _event = RenderPassEvent.AfterRenderingOpaques;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(settings, name);
        m_ScriptablePass.renderPassEvent = _event;

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.camera == Camera.main)
        {
            // Tell URP to generate the Camera Depth Texture
            m_ScriptablePass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {

    }

    protected override void Dispose(bool disposing)
    {
        //CoreUtils.Destroy(material);
        // (will use DestroyImmediate() or Destroy() depending if we're in editor or not)
        m_ScriptablePass.ReleaseMemory();
    }

}


