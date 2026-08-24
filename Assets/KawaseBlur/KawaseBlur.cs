using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class KawaseBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class KawaseBlurSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material blurMaterial = null;

        [Range(2,15)]
        public int blurPasses = 1;

        [Range(1,4)]
        public int downsample = 1;
        public bool copyToFramebuffer;
        public string targetName = "_blurTexture";
    }

    public KawaseBlurSettings settings = new KawaseBlurSettings();

    class KawaseBlurPass : ScriptableRenderPass
    {
        private Material blurMaterial;
        private int passes;
        private int downsample;
        private bool copyToFramebuffer;
        private string targetName;

        public KawaseBlurPass()
        {
        }

        public void Setup(Material material, int passes, int downsample, bool copyToFramebuffer, string targetName)
        {
            this.blurMaterial = material;
            this.passes = passes;
            this.downsample = downsample;
            this.copyToFramebuffer = copyToFramebuffer;
            this.targetName = targetName;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;
            if (!source.IsValid() || blurMaterial == null)
                return;

            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.width /= downsample;
            desc.height /= downsample;

            TextureHandle tmpRT1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "KawaseBlurTemp1", false);
            TextureHandle tmpRT2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "KawaseBlurTemp2", false);

            // First pass: source -> tmpRT1
            blurMaterial.SetFloat("_offset", 1.5f);
            var blitParams0 = new RenderGraphUtils.BlitMaterialParameters(source, tmpRT1, blurMaterial, 0);
            renderGraph.AddBlitPass(blitParams0, "Kawase Blur Pass 0");

            var currentSrc = tmpRT1;
            var currentDst = tmpRT2;

            // Intermediate passes: ping-pong
            for (int i = 1; i < passes - 1; i++)
            {
                blurMaterial.SetFloat("_offset", 0.5f + i);
                var blitParams = new RenderGraphUtils.BlitMaterialParameters(currentSrc, currentDst, blurMaterial, 0);
                renderGraph.AddBlitPass(blitParams, $"Kawase Blur Pass {i}");

                var tmp = currentSrc;
                currentSrc = currentDst;
                currentDst = tmp;
            }

            // Final pass
            blurMaterial.SetFloat("_offset", 0.5f + passes - 1f);
            if (copyToFramebuffer)
            {
                var blitFinal = new RenderGraphUtils.BlitMaterialParameters(currentSrc, source, blurMaterial, 0);
                renderGraph.AddBlitPass(blitFinal, "Kawase Blur Final");
            }
            else
            {
                var blitFinal = new RenderGraphUtils.BlitMaterialParameters(currentSrc, currentDst, blurMaterial, 0);
                renderGraph.AddBlitPass(blitFinal, "Kawase Blur Final");
                // Note: the result is in currentDst. To make it available globally,
                // this would need additional handling via SetGlobalTexture after the pass.
            }
        }
    }

    KawaseBlurPass scriptablePass;

    public override void Create()
    {
        scriptablePass = new KawaseBlurPass();
        scriptablePass.Setup(
            settings.blurMaterial,
            settings.blurPasses,
            settings.downsample,
            settings.copyToFramebuffer,
            settings.targetName
        );
        scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(scriptablePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
    }
}
