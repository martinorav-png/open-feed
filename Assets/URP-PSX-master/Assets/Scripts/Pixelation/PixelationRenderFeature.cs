using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace PSX
{
    public class PixelationRenderFeature : ScriptableRendererFeature
    {
        PixelationPass pixelationPass;

        public override void Create()
        {
            pixelationPass = new PixelationPass(RenderPassEvent.BeforeRenderingPostProcessing);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pixelationPass == null)
                return;

#if URP_COMPATIBILITY_MODE
#pragma warning disable CS0618
            pixelationPass.Setup(renderer.cameraColorTargetHandle);
#pragma warning restore CS0618
#endif
            renderer.EnqueuePass(pixelationPass);
        }
    }

    public class PixelationPass : ScriptableRenderPass
    {
        const string ShaderPath = "PostEffect/Pixelation";
        const string TempTargetName = "_TempTargetPixelation";
        static readonly string RenderTag = "Render Pixelation Effects";
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int WidthPixelation = Shader.PropertyToID("_WidthPixelation");
        static readonly int HeightPixelation = Shader.PropertyToID("_HeightPixelation");
        static readonly int ColorPrecision = Shader.PropertyToID("_ColorPrecision");

        readonly Material pixelationMaterial;

#if URP_COMPATIBILITY_MODE
        RTHandle currentTarget;
        RTHandle tempTarget;
#endif

        public PixelationPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            requiresIntermediateTexture = true;

            var shader = Shader.Find(ShaderPath);
            if (shader == null)
            {
                Debug.LogError("Shader not found.");
                return;
            }

            pixelationMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!TryGetActivePixelation(out var pixelation))
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            if (!cameraData.postProcessEnabled || resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.activeColorTexture;
            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = TempTargetName;
            destinationDesc.clearBuffer = false;

            var destination = renderGraph.CreateTexture(destinationDesc);
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetFloat(WidthPixelation, pixelation.widthPixelation.value);
            propertyBlock.SetFloat(HeightPixelation, pixelation.heightPixelation.value);
            propertyBlock.SetFloat(ColorPrecision, pixelation.colorPrecision.value);

            var blitParameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, pixelationMaterial, 0)
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
            if (!renderingData.cameraData.postProcessEnabled || !TryGetActivePixelation(out var pixelation) || currentTarget == null || tempTarget == null)
                return;

            pixelationMaterial.SetFloat(WidthPixelation, pixelation.widthPixelation.value);
            pixelationMaterial.SetFloat(HeightPixelation, pixelation.heightPixelation.value);
            pixelationMaterial.SetFloat(ColorPrecision, pixelation.colorPrecision.value);

            var cmd = CommandBufferPool.Get(RenderTag);
            Blitter.BlitCameraTexture(cmd, currentTarget, tempTarget);
            Blitter.BlitCameraTexture(cmd, tempTarget, currentTarget, pixelationMaterial, 0);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif

        bool TryGetActivePixelation(out Pixelation pixelation)
        {
            pixelation = null;

            if (pixelationMaterial == null)
                return false;

            var stack = VolumeManager.instance.stack;
            pixelation = stack.GetComponent<Pixelation>();
            return pixelation != null && pixelation.IsActive();
        }
    }
}
