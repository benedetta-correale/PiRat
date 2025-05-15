using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FogOfWarRenderFeature : ScriptableRendererFeature
{
    class FogOfWarPass : ScriptableRenderPass
    {
        Material compositeMat;
        RenderTexture persistentMask, workingMask;
        RenderTargetIdentifier cameraColorTarget;

        public FogOfWarPass(Material mat)
        {
            compositeMat = mat;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public void Setup(RenderTargetIdentifier camColor, RenderTexture persistent, RenderTexture working)
        {
            cameraColorTarget = camColor;
            persistentMask = persistent;
            workingMask = working;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (compositeMat == null || persistentMask == null || workingMask == null)
                return;

            var cmd = CommandBufferPool.Get("FogOfWarComposite");
            cmd.Blit(persistentMask, workingMask);
            FogOfWarController.Instance.DrawRatCookie(cmd, workingMask);
            compositeMat.SetTexture("_MaskTex", workingMask);
            cmd.Blit(cameraColorTarget, cameraColorTarget, compositeMat);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public Shader compositeShader;
    Material compositeMaterial;
    FogOfWarPass fogPass;

    public override void Create()
    {
        if (compositeShader == null)
        {
            Debug.LogError("FogOfWarRenderFeature: compositeShader mancante");
            return;
        }
        compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
        fogPass = new FogOfWarPass(compositeMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (fogPass == null) return;
        var ctrl = FogOfWarController.Instance;
        if (ctrl == null) return;

        // Usare la proprietà obsoleta ma compatibile
#pragma warning disable CS0618
        var camTarget = renderer.cameraColorTargetHandle;
#pragma warning restore CS0618

        fogPass.Setup(
            camTarget,
            ctrl.PersistentMaskRT,
            ctrl.WorkingMaskRT
        );
        renderer.EnqueuePass(fogPass);
    }
}
