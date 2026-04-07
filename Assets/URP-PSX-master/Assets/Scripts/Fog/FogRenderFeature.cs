using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace PSX
{
    public class FogRenderFeature : ScriptableRendererFeature
    {
        FogPass fogPass;

        public override void Create()
        {
            fogPass = new FogPass(RenderPassEvent.BeforeRenderingPostProcessing);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (fogPass == null)
                return;

            if (renderingData.cameraData.isSceneViewCamera)
                return;

#if URP_COMPATIBILITY_MODE
#pragma warning disable CS0618
            fogPass.Setup(renderer.cameraColorTargetHandle);
#pragma warning restore CS0618
#endif
            renderer.EnqueuePass(fogPass);
        }
    }

    public class FogPass : ScriptableRenderPass
    {
        const string ShaderPath = "PostEffect/Fog";
        const string TempTargetName = "_TempTargetFog";
        static readonly string RenderTag = "Render Fog Effects";
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int FogDensity = Shader.PropertyToID("_FogDensity");
        static readonly int FogDistance = Shader.PropertyToID("_FogDistance");
        static readonly int FogColor = Shader.PropertyToID("_FogColor");
        static readonly int FogNear = Shader.PropertyToID("_FogNear");
        static readonly int FogFar = Shader.PropertyToID("_FogFar");
        static readonly int FogAltScale = Shader.PropertyToID("_FogAltScale");
        static readonly int FogThinning = Shader.PropertyToID("_FogThinning");
        static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
        static readonly int NoiseStrength = Shader.PropertyToID("_NoiseStrength");

        readonly Material fogMaterial;

#if URP_COMPATIBILITY_MODE
        RTHandle currentTarget;
        RTHandle tempTarget;
#endif

        public FogPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);

            var shader = Shader.Find(ShaderPath);
            if (shader == null)
            {
                Debug.LogError("Shader not found.");
                return;
            }

            fogMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!TryGetActiveFog(out var fog))
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.isSceneViewCamera)
                return;

            if (!cameraData.postProcessEnabled || resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.activeColorTexture;
            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = TempTargetName;
            destinationDesc.clearBuffer = false;

            var destination = renderGraph.CreateTexture(destinationDesc);
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetFloat(FogDensity, fog.fogDensity.value);
            propertyBlock.SetFloat(FogDistance, fog.fogDistance.value);
            propertyBlock.SetColor(FogColor, fog.fogColor.value);
            propertyBlock.SetFloat(FogNear, fog.fogNear.value);
            propertyBlock.SetFloat(FogFar, fog.fogFar.value);
            propertyBlock.SetFloat(FogAltScale, fog.fogAltScale.value);
            propertyBlock.SetFloat(FogThinning, fog.fogThinning.value);
            propertyBlock.SetFloat(NoiseScale, fog.noiseScale.value);
            propertyBlock.SetFloat(NoiseStrength, fog.noiseStrength.value);

            var blitParameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, fogMaterial, 0)
            {
                propertyBlock = propertyBlock,
                sourceTexturePropertyID = MainTexId
            };

            renderGraph.AddBlitPass(blitParameters, passName: RenderTag);
            resourceData.cameraColor = destination;
        }

#if URP_COMPATIBILITY_MODE
        public void Setup(RTHandle colorTarget)
        {
            currentTarget = colorTarget;
        }

        [System.Obsolete("Compatibility-mode fallback for URP.")]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref tempTarget, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: TempTargetName);
        }

        [System.Obsolete("Compatibility-mode fallback for URP.")]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isSceneViewCamera)
                return;

            if (!renderingData.cameraData.postProcessEnabled || !TryGetActiveFog(out var fog) || currentTarget == null || tempTarget == null)
                return;

            fogMaterial.SetFloat(FogDensity, fog.fogDensity.value);
            fogMaterial.SetFloat(FogDistance, fog.fogDistance.value);
            fogMaterial.SetColor(FogColor, fog.fogColor.value);
            fogMaterial.SetFloat(FogNear, fog.fogNear.value);
            fogMaterial.SetFloat(FogFar, fog.fogFar.value);
            fogMaterial.SetFloat(FogAltScale, fog.fogAltScale.value);
            fogMaterial.SetFloat(FogThinning, fog.fogThinning.value);
            fogMaterial.SetFloat(NoiseScale, fog.noiseScale.value);
            fogMaterial.SetFloat(NoiseStrength, fog.noiseStrength.value);

            var cmd = CommandBufferPool.Get(RenderTag);
            Blitter.BlitCameraTexture(cmd, currentTarget, tempTarget);
            Blitter.BlitCameraTexture(cmd, tempTarget, currentTarget, fogMaterial, 0);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif

        bool TryGetActiveFog(out Fog fog)
        {
            fog = null;

            if (fogMaterial == null)
                return false;

            var stack = VolumeManager.instance.stack;
            fog = stack.GetComponent<Fog>();
            return fog != null && fog.IsActive();
        }
    }
}
