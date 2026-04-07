using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace PSX
{
    public class DitheringRenderFeature : ScriptableRendererFeature
    {
        DitheringPass ditheringPass;

        public override void Create()
        {
            ditheringPass = new DitheringPass(RenderPassEvent.BeforeRenderingPostProcessing);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (ditheringPass == null)
                return;

            if (renderingData.cameraData.isSceneViewCamera)
                return;

#if URP_COMPATIBILITY_MODE
#pragma warning disable CS0618
            ditheringPass.Setup(renderer.cameraColorTargetHandle);
#pragma warning restore CS0618
#endif
            renderer.EnqueuePass(ditheringPass);
        }
    }

    public class DitheringPass : ScriptableRenderPass
    {
        const string ShaderPath = "PostEffect/Dithering";
        const string TempTargetName = "_TempTargetDithering";
        static readonly string RenderTag = "Render Dithering Effects";
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int PatternIndex = Shader.PropertyToID("_PatternIndex");
        static readonly int DitherThreshold = Shader.PropertyToID("_DitherThreshold");
        static readonly int DitherStrength = Shader.PropertyToID("_DitherStrength");
        static readonly int DitherScale = Shader.PropertyToID("_DitherScale");

        readonly Material ditheringMaterial;

#if URP_COMPATIBILITY_MODE
        RTHandle currentTarget;
        RTHandle tempTarget;
#endif

        public DitheringPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            requiresIntermediateTexture = true;

            var shader = Shader.Find(ShaderPath);
            if (shader == null)
            {
                Debug.LogError("Shader not found.");
                return;
            }

            ditheringMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!TryGetActiveDithering(out var dithering))
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
            propertyBlock.SetInt(PatternIndex, dithering.patternIndex.value);
            propertyBlock.SetFloat(DitherThreshold, dithering.ditherThreshold.value);
            propertyBlock.SetFloat(DitherStrength, dithering.ditherStrength.value);
            propertyBlock.SetFloat(DitherScale, dithering.ditherScale.value);

            var blitParameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, ditheringMaterial, 0)
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

            if (!renderingData.cameraData.postProcessEnabled || !TryGetActiveDithering(out var dithering) || currentTarget == null || tempTarget == null)
                return;

            ditheringMaterial.SetInt(PatternIndex, dithering.patternIndex.value);
            ditheringMaterial.SetFloat(DitherThreshold, dithering.ditherThreshold.value);
            ditheringMaterial.SetFloat(DitherStrength, dithering.ditherStrength.value);
            ditheringMaterial.SetFloat(DitherScale, dithering.ditherScale.value);

            var cmd = CommandBufferPool.Get(RenderTag);
            Blitter.BlitCameraTexture(cmd, currentTarget, tempTarget);
            Blitter.BlitCameraTexture(cmd, tempTarget, currentTarget, ditheringMaterial, 0);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif

        bool TryGetActiveDithering(out Dithering dithering)
        {
            dithering = null;

            if (ditheringMaterial == null)
                return false;

            var stack = VolumeManager.instance.stack;
            dithering = stack.GetComponent<Dithering>();
            return dithering != null && dithering.IsActive();
        }
    }
}
