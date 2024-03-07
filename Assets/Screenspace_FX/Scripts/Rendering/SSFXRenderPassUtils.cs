using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SSFXParticles
{

    public struct ParticlesBuffer
    {
        public ComputeBuffer particlesDatasBuffer;
        public ComputeBuffer previousParticlesDatasBuffer;
        public ComputeBuffer indirectDrawArgs;
        public ComputeBuffer indirectArgs;
        public ComputeBuffer maxParticleCounts;
        public int actualFrame;
    }

    // If change are made here, it MUST be replicated to ParticlesCommons.cginc
    struct ParticleDatas
    {
        public Vector4 position;
        public Vector3 color;
        public Vector3 startColor;
        public float timeApparition;
        public Vector3 normal;
        public float duration;
        public float3 speed;
        public float indexConfig;
        public float startSize;
        public float size;
        public float startMaxSpeed;
    }

    public static class SSFXRenderPassUtils
    {
        private static uint[] previousMeshVertexCount = null;
        public static int indirectDrawArgsSize = 8;
        public static int NbBuffers = 0;

        public static int GetMaxParticleCount(SSFXRenderPass.Settings settings)
        {
            int maxEmitted = 0;
            foreach (var bInfo in settings.buffersInfo)
            {
                maxEmitted += bInfo.maxParticlesEmitted;
            }
            NbBuffers = settings.buffersInfo.Count();
            return maxEmitted;
        }

        public static void SetIndirectDrawArgs(ref ParticlesBuffer datas, SSFXRenderPass.Settings settings)
        {
            int index = 0;
            int[] indirectDrawArgs = new int[settings.buffersInfo.Count() * indirectDrawArgsSize];

            bool changes = false;
            foreach (var bInfo in settings.buffersInfo)
            {
                uint vertexCount = bInfo.particlesMesh.GetIndexCount(0);
                indirectDrawArgs[indirectDrawArgsSize * index] = (int)vertexCount;
                if (previousMeshVertexCount[index] != vertexCount)
                    changes = true;
                previousMeshVertexCount[index] = vertexCount;
                index++;
            }

            if (changes)
                datas.indirectDrawArgs.SetData(indirectDrawArgs);
        }
        public static void CheckResources(ref ParticlesBuffer datas, SSFXRenderPass.Settings settings)
        {
            int maxEmitted = GetMaxParticleCount(settings);
            if (datas.particlesDatasBuffer == null || datas.particlesDatasBuffer.count != maxEmitted)
            {
                if (datas.particlesDatasBuffer != null)
                    datas.particlesDatasBuffer.Release();
                datas.particlesDatasBuffer = new ComputeBuffer(maxEmitted, Marshal.SizeOf(typeof(ParticleDatas)), ComputeBufferType.Structured);
                datas.maxParticleCounts = new ComputeBuffer(settings.buffersInfo.Count(), sizeof(int), ComputeBufferType.Default);

                int[] maxCount = new int[settings.buffersInfo.Count()];
                for (int i = 0; i < settings.buffersInfo.Count(); i++)
                {
                    maxCount[i] = settings.buffersInfo[i].maxParticlesEmitted;
                }
                datas.maxParticleCounts.SetData(maxCount);
            }

            if (datas.previousParticlesDatasBuffer == null || datas.previousParticlesDatasBuffer.count != maxEmitted)
            {
                if (datas.previousParticlesDatasBuffer != null)
                    datas.previousParticlesDatasBuffer.Release();
                datas.previousParticlesDatasBuffer = new ComputeBuffer(maxEmitted, Marshal.SizeOf(typeof(ParticleDatas)), ComputeBufferType.Structured);
            }

            if (datas.indirectDrawArgs == null || !datas.indirectDrawArgs.IsValid())
            {
                datas.indirectDrawArgs = new ComputeBuffer(indirectDrawArgsSize * settings.buffersInfo.Count(), sizeof(int), ComputeBufferType.IndirectArguments);
            }

            if (previousMeshVertexCount == null || previousMeshVertexCount.Count() != settings.buffersInfo.Count()
                || !datas.indirectDrawArgs.IsValid())
            {
                previousMeshVertexCount = new uint[settings.buffersInfo.Count()];
            }

            SetIndirectDrawArgs(ref datas, settings);

            if (datas.indirectArgs == null || !datas.indirectArgs.IsValid())
            {
                datas.indirectArgs = new ComputeBuffer(6, sizeof(int), ComputeBufferType.IndirectArguments);
                datas.indirectArgs.SetData(new int[6] { 1, 1, 1, 1, 1, 1 });
            }
        }

        public static void FreeResources(ref ParticlesBuffer datas)
        {
            datas.particlesDatasBuffer?.Release();
            datas.particlesDatasBuffer = null;
            datas.previousParticlesDatasBuffer?.Release();
            datas.previousParticlesDatasBuffer = null;
            datas.indirectDrawArgs?.Release();
            datas.indirectDrawArgs = null;
            datas.indirectArgs?.Release();
            datas.indirectArgs = null;
        }

        public static int GetParticlesEmittedCount(ParticlesBuffer datas, SSFXRenderPass.Settings settings)
        {
            int[] counterData = new int[4];
            datas.indirectDrawArgs.GetData(counterData);
            int particlesToEmitCount = counterData[1];
            if (settings.enableDebug)
                Debug.Log($"Total particles to emit: {particlesToEmitCount}");
            return particlesToEmitCount;
        }

        public static void SetMaterialDataOpaquePass(CommandBuffer cmd, ParticlesBuffer datas, SSFXRenderPass.Settings settings, RTHandle rtCameraColor)
        {
            Material materialSSFX = settings.overrideMaterial;
            cmd.SetGlobalBuffer("_MaxParticlesCount", datas.maxParticleCounts);
            cmd.SetGlobalBuffer("_ParticlesDatasBuffer", datas.particlesDatasBuffer);
            cmd.SetGlobalBuffer("_ParticlesDrawArgs", datas.indirectDrawArgs);
            cmd.SetGlobalTexture("_CameraColor", rtCameraColor.rt);

            materialSSFX.SetFloat("_durationEffect", 2.0f);

            Graphics.ClearRandomWriteTargets();
            Graphics.SetRandomWriteTarget(1, datas.particlesDatasBuffer, true);
            Graphics.SetRandomWriteTarget(2, datas.indirectDrawArgs, true);
        }

        public static void RenderDepthPrepass(ScriptableRenderContext context, RenderingData renderingData, DrawingSettings drawingSettings, FilteringSettings filteringSettings, Material material)
        {
            drawingSettings.overrideMaterialPassIndex = 0;
            drawingSettings.overrideMaterial = material;

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }

        public static void RenderOpaqueOverride(ScriptableRenderContext context, RenderingData renderingData, DrawingSettings drawingSettings, FilteringSettings filteringSettings, Material material)
        {
            drawingSettings.overrideMaterialPassIndex = 1;
            drawingSettings.overrideMaterial = material;

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }

        public static void RenderOpaque(ScriptableRenderContext context, RenderingData renderingData, DrawingSettings drawingSettings, FilteringSettings filteringSettings)
        {
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }

        public static void ComputeSetIndirectArgs(CommandBuffer cmd, ParticlesBuffer datas, SSFXRenderPass.Settings settings)
        {
            ComputeShader compute = settings.particlesComputeShader;
            int setIndirectArgsKernelID = compute.FindKernel("CSSetIndirectArgs");
            compute.SetBuffer(setIndirectArgsKernelID, "_ParticlesDrawArgs", datas.indirectDrawArgs);
            compute.SetBuffer(setIndirectArgsKernelID, "_MaxParticlesCount", datas.maxParticleCounts);
            compute.SetBuffer(setIndirectArgsKernelID, "_IndirectArgs", datas.indirectArgs);

            cmd.DispatchCompute(compute, setIndirectArgsKernelID, 1, 1, 1);
        }

        public static void ComputeParticlesBufferUnion(CommandBuffer cmd, ParticlesBuffer datas, SSFXRenderPass.Settings settings)
        {
            ComputeShader compute = settings.particlesComputeShader;
            int bufferUnionKernelID = compute.FindKernel("CSBufferUnion");
            compute.SetBuffer(bufferUnionKernelID, "_ParticlesDatasBuffer", datas.particlesDatasBuffer);
            compute.SetBuffer(bufferUnionKernelID, "_ParticlesDrawArgs", datas.indirectDrawArgs);
            compute.SetBuffer(bufferUnionKernelID, "_PreviousParticlesDatasBuffer", datas.previousParticlesDatasBuffer);
            compute.SetBuffer(bufferUnionKernelID, "_MaxParticlesCount", datas.maxParticleCounts);
            compute.SetInt("_PrioritizeNewParticles", settings.prioritizeNewParticles ? 1 : 0);
            compute.SetInt("_ActualFrame", datas.actualFrame);
            compute.SetInt("_NbBuffers", settings.buffersInfo.Count());

            cmd.DispatchCompute(compute, bufferUnionKernelID, datas.indirectArgs, 0);
        }

        public static void ComputeParticlesSimulation(CommandBuffer cmd, ParticlesBuffer datas, SSFXRenderPass.Settings settings)
        {
            ComputeShader compute = settings.particlesComputeShader;
            int simulationKernelID = compute.FindKernel("CSParticlesSimulation");
            compute.SetBuffer(simulationKernelID, "_ParticlesDatasBuffer", datas.particlesDatasBuffer);
            compute.SetBuffer(simulationKernelID, "_ParticlesDrawArgs", datas.indirectDrawArgs);
            compute.SetFloat("_FloorHeight", settings.floorHeight);
            compute.SetVector("_SimulationBase", new Vector4(settings.gravity, settings.floorHeight, settings.maxParticleSpeed, 0));

            //compute.SetVector("_ParticlesTarget", ParticlesTargetHandler.GetTarget());
            ComputeBuffer configs = SSFX.SSFXParticleSystemHandler.UpdateConfigsComputeBuffer();
            if (configs != null)
                compute.SetBuffer(simulationKernelID, "_ParticlesConfigs", configs);

            if (settings.noiseMap != null)
            {
                compute.SetTexture(simulationKernelID, "_NoiseMap", settings.noiseMap);
                compute.SetVector("_NoiseMap_ST", settings.noiseMapSettings);
            }
            compute.SetVector("_Wind", settings.windDirection);


            cmd.DispatchCompute(compute, simulationKernelID, datas.indirectArgs, sizeof(int) * 3);
        }

        public static void ComputeClearDrawArgs(CommandBuffer cmd, ParticlesBuffer datas, SSFXRenderPass.Settings settings)
        {
            ComputeShader compute = settings.particlesComputeShader;
            int clearKernelID = compute.FindKernel("CSClearIndirectDrawArgs");
            compute.SetBuffer(clearKernelID, "_ParticlesDrawArgs", datas.indirectDrawArgs);
            compute.SetInt("_NbBuffers", settings.buffersInfo.Count());

            cmd.DispatchCompute(compute, clearKernelID, 1, 1, 1);
        }

        public static void RenderParticles(CommandBuffer cmd, ParticlesBuffer datas, SSFXRenderPass.SSFXBufferInfo settings, int bufferIndex)
        {
            Material materialParticles = settings.particlesMaterial;
            materialParticles.SetBuffer("_ParticlesDatasBuffer", datas.particlesDatasBuffer);

            cmd.DrawMeshInstancedIndirect(settings.particlesMesh, 0, materialParticles, 0, datas.indirectDrawArgs, bufferIndex * indirectDrawArgsSize);
        }
    }
}