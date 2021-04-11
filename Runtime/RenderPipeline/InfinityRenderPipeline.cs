using System;
using UnityEngine;
using UnityEngine.VFX;
using Unity.Mathematics;
using UnityEngine.Rendering;
using InfinityTech.Component;
using System.Collections.Generic;
using InfinityTech.Rendering.RDG;
using InfinityTech.Rendering.Core;
using InfinityTech.Rendering.GPUResource;
using InfinityTech.Rendering.MeshPipeline;
using InfinityTech.Rendering.TerrainPipeline;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InfinityTech.Rendering.Pipeline
{
    public partial struct FViewUnifrom
    {
        private static readonly int ID_FrameIndex = Shader.PropertyToID("FrameIndex");
        private static readonly int ID_TAAJitter = Shader.PropertyToID("TAAJitter");
        private static readonly int ID_Matrix_WorldToView = Shader.PropertyToID("Matrix_WorldToView");
        private static readonly int ID_Matrix_ViewToWorld = Shader.PropertyToID("Matrix_ViewToWorld");
        private static readonly int ID_Matrix_Proj = Shader.PropertyToID("Matrix_Proj");
        private static readonly int ID_Matrix_InvProj = Shader.PropertyToID("Matrix_InvProj");
        private static readonly int ID_Matrix_JitterProj = Shader.PropertyToID("Matrix_JitterProj");
        private static readonly int ID_Matrix_InvJitterProj = Shader.PropertyToID("Matrix_InvJitterProj");
        private static readonly int ID_Matrix_FlipYProj = Shader.PropertyToID("Matrix_FlipYProj");
        private static readonly int ID_Matrix_InvFlipYProj = Shader.PropertyToID("Matrix_InvFlipYProj");
        private static readonly int ID_Matrix_FlipYJitterProj = Shader.PropertyToID("Matrix_FlipYJitterProj");
        private static readonly int ID_Matrix_InvFlipYJitterProj = Shader.PropertyToID("Matrix_InvFlipYJitterProj");
        private static readonly int ID_Matrix_ViewProj = Shader.PropertyToID("Matrix_ViewProj");
        private static readonly int ID_Matrix_InvViewProj = Shader.PropertyToID("Matrix_InvViewProj");
        private static readonly int ID_Matrix_ViewFlipYProj = Shader.PropertyToID("Matrix_ViewFlipYProj");
        private static readonly int ID_Matrix_InvViewFlipYProj = Shader.PropertyToID("Matrix_InvViewFlipYProj");
        private static readonly int ID_Matrix_ViewJitterProj = Shader.PropertyToID("Matrix_ViewJitterProj");
        private static readonly int ID_Matrix_InvViewJitterProj = Shader.PropertyToID("Matrix_InvViewJitterProj");
        private static readonly int ID_Matrix_ViewFlipYJitterProj = Shader.PropertyToID("Matrix_ViewFlipYJitterProj");
        private static readonly int ID_Matrix_InvViewFlipYJitterProj = Shader.PropertyToID("Matrix_InvViewFlipYJitterProj");

        private static readonly int ID_Prev_FrameIndex = Shader.PropertyToID("Prev_FrameIndex");
        private static readonly int ID_Matrix_PrevViewProj = Shader.PropertyToID("Matrix_PrevViewProj");
        private static readonly int ID_Matrix_PrevViewFlipYProj = Shader.PropertyToID("Matrix_PrevViewFlipYProj");


        public int FrameIndex;
        public float2 TAAJitter;
        public Matrix4x4 Matrix_WorldToView;
        public Matrix4x4 Matrix_ViewToWorld;
        public Matrix4x4 Matrix_Proj;
        public Matrix4x4 Matrix_InvProj;
        public Matrix4x4 Matrix_JitterProj;
        public Matrix4x4 Matrix_InvJitterProj;
        public Matrix4x4 Matrix_FlipYProj;
        public Matrix4x4 Matrix_InvFlipYProj;
        public Matrix4x4 Matrix_FlipYJitterProj;
        public Matrix4x4 Matrix_InvFlipYJitterProj;
        public Matrix4x4 Matrix_ViewProj;
        public Matrix4x4 Matrix_InvViewProj;
        public Matrix4x4 Matrix_ViewFlipYProj;
        public Matrix4x4 Matrix_InvViewFlipYProj;
        public Matrix4x4 Matrix_ViewJitterProj;
        public Matrix4x4 Matrix_InvViewJitterProj;
        public Matrix4x4 Matrix_ViewFlipYJitterProj;
        public Matrix4x4 Matrix_InvViewFlipYJitterProj;

        public int Prev_FrameIndex;
        public float2 Prev_TAAJitter;
        public Matrix4x4 Matrix_PrevViewProj;
        public Matrix4x4 Matrix_PrevViewFlipYProj;

        private Matrix4x4 GetJitteredProjectionMatrix(Matrix4x4 origProj, Camera UnityCamera)
        {

            float jitterX = HaltonSequence.Get((FrameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = HaltonSequence.Get((FrameIndex & 1023) + 1, 3) - 0.5f;
            TAAJitter = new float2(jitterX, jitterY);
            float4 taaJitter = new float4(jitterX, jitterY, jitterX / UnityCamera.pixelRect.size.x, jitterY / UnityCamera.pixelRect.size.y);

            if (++FrameIndex >= 8)
                FrameIndex = 0;

            Matrix4x4 proj;

            if (UnityCamera.orthographic) {
                float vertical = UnityCamera.orthographicSize;
                float horizontal = vertical * UnityCamera.aspect;

                var offset = taaJitter;
                offset.x *= horizontal / (0.5f * UnityCamera.pixelRect.size.x);
                offset.y *= vertical / (0.5f * UnityCamera.pixelRect.size.y);

                float left = offset.x - horizontal;
                float right = offset.x + horizontal;
                float top = offset.y + vertical;
                float bottom = offset.y - vertical;

                proj = Matrix4x4.Ortho(left, right, bottom, top, UnityCamera.nearClipPlane, UnityCamera.farClipPlane);
            } else {
                var planes = origProj.decomposeProjection;

                float vertFov = Math.Abs(planes.top) + Math.Abs(planes.bottom);
                float horizFov = Math.Abs(planes.left) + Math.Abs(planes.right);

                var planeJitter = new Vector2(jitterX * horizFov / UnityCamera.pixelRect.size.x, jitterY * vertFov / UnityCamera.pixelRect.size.y);

                planes.left += planeJitter.x;
                planes.right += planeJitter.x;
                planes.top += planeJitter.y;
                planes.bottom += planeJitter.y;

                proj = Matrix4x4.Frustum(planes);
            }

            return proj;
        }

        private void UnpateCurrBufferData(Camera RenderCamera)
        {
            Matrix_WorldToView = RenderCamera.worldToCameraMatrix;
            Matrix_ViewToWorld = Matrix_WorldToView.inverse;
            Matrix_Proj = GL.GetGPUProjectionMatrix(RenderCamera.projectionMatrix, true);
            Matrix_InvProj = Matrix_Proj.inverse;
            Matrix_JitterProj = GetJitteredProjectionMatrix(Matrix_Proj, RenderCamera);
            Matrix_InvJitterProj = Matrix_JitterProj.inverse;
            Matrix_FlipYProj = GL.GetGPUProjectionMatrix(RenderCamera.projectionMatrix, false);
            Matrix_InvFlipYProj = Matrix_FlipYProj.inverse;
            Matrix_FlipYJitterProj = GetJitteredProjectionMatrix(Matrix_FlipYProj, RenderCamera);
            Matrix_InvFlipYJitterProj = Matrix_FlipYJitterProj.inverse;
            Matrix_ViewProj = Matrix_Proj * Matrix_WorldToView;
            Matrix_InvViewProj = Matrix_ViewProj.inverse;
            Matrix_ViewFlipYProj = Matrix_FlipYProj * Matrix_WorldToView;
            Matrix_InvViewFlipYProj = Matrix_ViewFlipYProj.inverse;
            Matrix_ViewJitterProj = Matrix_JitterProj * Matrix_WorldToView;
            Matrix_InvViewJitterProj = Matrix_ViewJitterProj.inverse;
            Matrix_ViewFlipYJitterProj = Matrix_FlipYJitterProj * Matrix_WorldToView;
            Matrix_InvViewFlipYJitterProj = Matrix_ViewFlipYJitterProj.inverse;
        }

        private void UnpateLastBufferData()
        {
            Prev_FrameIndex = FrameIndex;
            Prev_TAAJitter = TAAJitter;
            Matrix_PrevViewProj = Matrix_ViewProj;
            Matrix_PrevViewFlipYProj = Matrix_ViewFlipYProj;
        }

        public void UnpateViewUnifrom(bool bLastData, Camera RenderCamera)
        {
            if(!bLastData) {
                UnpateCurrBufferData(RenderCamera);
            } else {
                UnpateLastBufferData();
            }
        }

        public void SetViewUnifrom(CommandBuffer CmdBuffer)
        {
            CmdBuffer.SetGlobalInt(ID_FrameIndex, FrameIndex);
            CmdBuffer.SetGlobalInt(ID_Prev_FrameIndex, Prev_FrameIndex);
            CmdBuffer.SetGlobalVector(ID_TAAJitter, new float4(TAAJitter.x, TAAJitter.y, Prev_TAAJitter.x, Prev_TAAJitter.y));

            CmdBuffer.SetGlobalMatrix(ID_Matrix_WorldToView, Matrix_WorldToView);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_ViewToWorld, Matrix_ViewToWorld);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_Proj, Matrix_Proj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_InvProj, Matrix_InvProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_JitterProj, Matrix_JitterProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_InvJitterProj, Matrix_InvJitterProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_FlipYProj, Matrix_FlipYProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_InvFlipYProj, Matrix_InvFlipYProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_FlipYJitterProj, Matrix_FlipYJitterProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_InvFlipYJitterProj, Matrix_InvFlipYJitterProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_ViewProj, Matrix_ViewProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_InvViewProj, Matrix_InvViewProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_ViewFlipYProj, Matrix_ViewFlipYProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_InvViewFlipYProj, Matrix_InvViewFlipYProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_ViewJitterProj, Matrix_ViewJitterProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_InvViewJitterProj, Matrix_InvViewJitterProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_ViewFlipYJitterProj, Matrix_ViewFlipYJitterProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_InvViewFlipYJitterProj, Matrix_InvViewFlipYJitterProj);

            CmdBuffer.SetGlobalMatrix(ID_Matrix_PrevViewProj, Matrix_PrevViewProj);
            CmdBuffer.SetGlobalMatrix(ID_Matrix_PrevViewFlipYProj, Matrix_PrevViewFlipYProj);
        }
    }

    public partial class InfinityRenderPipeline : RenderPipeline
    {
        private FGPUScene GPUScene;
        private FViewUnifrom ViewUnifrom;
        private RDGGraphBuilder GraphBuilder;
        private InfinityRenderPipelineAsset RenderPipelineAsset;

        private FMeshPassProcessor DepthPassMeshProcessor;
        private FMeshPassProcessor GBufferPassMeshProcessor;
        private FMeshPassProcessor ForwardPassMeshProcessor;


        public InfinityRenderPipeline()
        {
            SetGraphicsSetting();

            GPUScene = new FGPUScene();
            ViewUnifrom = new FViewUnifrom();
            GraphBuilder = new RDGGraphBuilder("InfinityGraph");
            RenderPipelineAsset = (InfinityRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;

            DepthPassMeshProcessor = new FMeshPassProcessor(GPUScene);
            GBufferPassMeshProcessor = new FMeshPassProcessor(GPUScene);
            ForwardPassMeshProcessor = new FMeshPassProcessor(GPUScene);
        }

        protected override void Render(ScriptableRenderContext RenderContext, Camera[] Views)
        {
            //Init FrameContext
            CommandBuffer CmdBuffer = CommandBufferPool.Get("");
            FResourceFactory gpuResourcePool = GetWorld().gpuResourcePool;
            GPUScene.Gather(GetWorld().GetMeshBatchColloctor(), gpuResourcePool, CmdBuffer, 2, false);
            //RTHandles.Initialize(Screen.width, Screen.height, false, MSAASamples.None);

            //Do FrameRender
            BeginFrameRendering(RenderContext, Views);
            for (int ViewIndex = 0; ViewIndex < Views.Length; ++ViewIndex)
            {
                //Init View
                Camera View = Views[ViewIndex];
                CameraComponent HDView = View.GetComponent<CameraComponent>();

                //Render View
                BeginCameraRendering(RenderContext, View);
                {
                    using (new ProfilingScope(CmdBuffer, HDView ? HDView.ViewProfiler : ProfilingSampler.Get(ERGProfileId.InfinityRenderer)))
                    {
                        #region InitViewContext
                        bool bSceneView = View.cameraType == CameraType.SceneView;
                        bool bRendererView = View.cameraType == CameraType.Game || View.cameraType == CameraType.Reflection || View.cameraType == CameraType.SceneView;

                        #if UNITY_EDITOR
                            if (bSceneView) { ScriptableRenderContext.EmitWorldGeometryForSceneView(View); }
                        #endif

                        VFXManager.PrepareCamera(View);
                        ViewUnifrom.UnpateViewUnifrom(false, View);
                        ViewUnifrom.SetViewUnifrom(CmdBuffer);
                        RenderContext.SetupCameraProperties(View);
                        VFXManager.ProcessCameraCommand(View, CmdBuffer);

                        //Culling Context
                        FCullingData CullingData = new FCullingData();
                        { CullingData.bRendererView = bRendererView; }
                        ScriptableCullingParameters CullingParameters;
                        View.TryGetCullingParameters(out CullingParameters);
                        CullingResults CullingResult = RenderContext.Cull(ref CullingParameters); //Unity Culling
                        RenderContext.DispatchCull(GPUScene, ref CullingParameters, ref CullingData); //Infinity Culling

                        //Terrain Context
                        List<TerrainComponent> WorldTerrains = GetWorld().GetWorldTerrains();
                        float4x4 Matrix_Proj = TerrainUtility.GetProjectionMatrix((View.fieldOfView) * 0.5f, View.pixelWidth, View.pixelHeight, View.nearClipPlane, View.farClipPlane);
                        for(int i = 0; i < WorldTerrains.Count; ++i)
                        {
                            TerrainComponent Terrain = WorldTerrains[i];
                            Terrain.UpdateLODData(View.transform.position, Matrix_Proj);
                            #if UNITY_EDITOR
                                if (Handles.ShouldRenderGizmos())
                                {
                                    Terrain.DrawBounds(true);
                                }
                            #endif
                        }
                        #endregion //InitViewContext

                        #region InitViewCommand
                        RenderOpaqueDepth(View, CullingData, CullingResult);
                        RenderOpaqueGBuffer(View, CullingData, CullingResult);
                        RenderOpaqueMotion(View, CullingData, CullingResult);
                        RenderOpaqueForward(View, CullingData, CullingResult);
                        RenderSkyBox(View);
                        RenderGizmos(View, GizmoSubset.PostImageEffects);
                        RenderPresentView(View, GraphBuilder.ScopeTexture(InfinityShaderIDs.DiffuseBuffer), View.targetTexture);
                        #endregion //InitViewCommand

                        #region ExecuteViewRender
                        //Wait All MeshPassProcessor
                        DepthPassMeshProcessor.WaitSetupFinish();
                        GBufferPassMeshProcessor.WaitSetupFinish();
                        ForwardPassMeshProcessor.WaitSetupFinish();

                        //Execute RenderGraph
                        GraphBuilder.Execute(GetWorld(), gpuResourcePool, RenderContext, CmdBuffer, ViewUnifrom.FrameIndex);
                        #endregion //ExecuteViewRender

                        #region ReleaseViewContext
                        CullingData.Release();
                        ViewUnifrom.UnpateViewUnifrom(true, View);
                        #endregion //ReleaseViewContext
                    }
                }
                EndCameraRendering(RenderContext, View);

                //Submit ViewCommand
                RenderContext.ExecuteCommandBuffer(CmdBuffer);
                CmdBuffer.Clear();
                RenderContext.Submit();
            }
            EndFrameRendering(RenderContext, Views);

            //Release FrameContext
            GPUScene.Release(gpuResourcePool);
            CommandBufferPool.Release(CmdBuffer);
        }

        protected FRenderWorld GetWorld()
        {
            if (FRenderWorld.RenderWorld != null) 
            {
                return FRenderWorld.RenderWorld;
            }

            return null;
        }

        protected void SetGraphicsSetting()
        {
            Shader.globalRenderPipeline = "InfinityRenderPipeline";

            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;
            InfinityRenderPipelineAsset PipelineAsset = (InfinityRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            GraphicsSettings.useScriptableRenderPipelineBatching = PipelineAsset.EnableSRPBatch;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.Rotation,
                defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly,
                mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask,
                lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
                lightmapsModes = LightmapsMode.NonDirectional | LightmapsMode.CombinedDirectional,
                lightProbeProxyVolumes = true,
                motionVectors = true,
                receiveShadows = true,
                reflectionProbes = true,
                rendererPriority = true,
                overridesFog = true,
                overridesOtherLightingSettings = true,
                editableMaterialRenderQueue = true
                , enlighten = true
                , overridesLODBias = true
                , overridesMaximumLODLevel = true
                , terrainDetailUnsupported = true
            };
        }        
       
        protected override void Dispose(bool disposing)
        {
            GraphBuilder.Cleanup();
        }
    }
}