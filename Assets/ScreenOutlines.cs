using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenOutlines : ScriptableRendererFeature
{
    BlurOutlinesPass pass;

    [field: SerializeField] public ScreenOutlinesSettings Settings { get; private set; }

    [System.Serializable]
    public class ScreenOutlinesSettings
    {
        [field: SerializeField] public RenderPassEvent Event { get; set; } = RenderPassEvent.BeforeRenderingPostProcessing;
        [field: SerializeField] public bool RenderSceneView { get; set; } = true;
        [field: SerializeField] public Material Material { get; set; }
    }

    public override void Create()
    {
        pass = new BlurOutlinesPass(Settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        pass.Setup(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        pass.Dispose();
    }

    class BlurOutlinesPass : ScriptableRenderPass
    {
        ScreenOutlinesSettings settings;

        static readonly int _ClipToView = Shader.PropertyToID("_ClipToView");

        //RTHandle colorTarget;
        RTHandle temp;

        RTHandle enemyMask;
        FilteringSettings filteringSettings;
        RenderStateBlock renderStateBlock;
        static readonly ShaderTagId shaderTag = new ShaderTagId("SRPDefaultUnlit");
        Material maskMat;
        static readonly int _EnemyMask = Shader.PropertyToID(nameof(_EnemyMask));

        public BlurOutlinesPass(ScreenOutlinesSettings settings)
        {
            renderPassEvent = settings.Event;
            this.settings = settings;

            renderStateBlock.mask = RenderStateMask.Nothing;
            renderStateBlock.depthState = new DepthState(false);
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, Global.SurfaceLayerMask | Global.TargetLayerMask, Global.EnemyRenderingLayer);
            maskMat = CoreUtils.CreateEngineMaterial(Shader.Find("Pigeon/Mask"));
        }

        public void Setup(RTHandle colorHandle)
        {
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            //colorTarget = colorHandle;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor desc)
        {
            desc.depthBufferBits = 0; // Color and depth cannot be combined in RTHandles

            //RenderingUtils.ReAllocateIfNeeded(ref temp, Vector2.one, desc, name: "_TemporaryColorTexture");
            // These resizable RTHandles seem quite glitchy when switching between game and scene view :\
            // instead,
            RenderingUtils.ReAllocateIfNeeded(ref temp, desc, name: "_TemporaryColorTexture");

            RenderingUtils.ReAllocateIfNeeded(ref enemyMask, desc, name: "_EnemyMask");

            //var renderer = renderingData.cameraData.renderer;
            //colorTarget = renderer.cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (renderingData.cameraData.cameraType == CameraType.Preview || (!settings.RenderSceneView && renderingData.cameraData.cameraType == CameraType.SceneView))
            {
                return;
            }

            // In the new Unity version we get a bunch of Blitter errors when we try to blit the 'temp' RT in an in-context prefab stage
            //var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            //if (prefabStage != null && prefabStage.mode == UnityEditor.SceneManagement.PrefabStage.Mode.InContext)
            //{
            //    return;
            //}
#endif
            //Shader.SetGlobalTexture("_EnemyMask", enemyMask);

            settings.Material.SetMatrix(_ClipToView, renderingData.cameraData.GetGPUProjectionMatrix().inverse);

            CommandBuffer cmd = CommandBufferPool.Get("ScreenOutlines");


            cmd.SetGlobalTexture(_EnemyMask, enemyMask);
            cmd.SetRenderTarget(enemyMask);
            cmd.ClearRenderTarget(false, true, Color.black);

            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTag, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = maskMat;

            // In case of camera stacking we need to take the viewport rect from base camera
            Rect pixelRect = camera.pixelRect;
            float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

            //Matrix4x4 projectionMatrix = Matrix4x4.Perspective(90f, cameraAspect, /// TODO -- set fov based on CAMERA FOV
            //                camera.nearClipPlane, camera.farClipPlane);
            //projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, cameraData.IsCameraProjectionMatrixFlipped());

            //Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
            //RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);

            // Ensure we flush our command-buffer before we render...
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Render the objects...
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings/*, ref renderStateBlock*/);

            /// DELETE THIS??
            //RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);



            //var colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            //Blitter.BlitCameraTexture(cmd, colorTarget, temp, settings.Material, 0);
            //Blitter.BlitCameraTexture(cmd, enemyMask, colorTarget);



            //Blitter.BlitCameraTexture(cmd, colorTarget, temp, settings.Material, 0);
            //Blitter.BlitCameraTexture(cmd, temp, colorTarget);

            var colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            Blitter.BlitCameraTexture(cmd, colorTarget, temp, settings.Material, 0);
            Blitter.BlitCameraTexture(cmd, temp, colorTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        //public override void OnCameraCleanup(CommandBuffer cmd)
        //{
        //    colorTarget = null;
        //}

        public void Dispose()
        {
            temp?.Release();
            enemyMask?.Release();
            CoreUtils.Destroy(maskMat);
        }
    }
}