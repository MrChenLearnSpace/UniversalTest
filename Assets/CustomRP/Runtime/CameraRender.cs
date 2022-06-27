using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRender
{
    ScriptableRenderContext context;
    const string bufferName = "Render Camera";
    CommandBuffer buffer =new CommandBuffer { name = bufferName};
    Camera camera;
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };
    public void Render(ScriptableRenderContext context, Camera camera) {
        this.context = context; 
        this.camera = camera;
        if(!Cull()) {
            return;
        }
        Setup();
        /// <summary>
        ///绘制可见物
        /// </summary>
        
        DrawVisibleGeometry();

        //绘制SRP不支持的着色器类型
        DrawUnSupportedShader();
        ///<summary>
        ///提交缓冲区渲染命令
        ///</summary>
        Submit();
    }

    private void DrawUnSupportedShader() {
        //数组第一个元素用来构造DrawingSettings对象的时候设置
        DrawingSettings drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera));
        for(int i = 1; i < legacyShaderTagIds.Length; i++) {
            //遍历数组逐个设置着色器的PassName，从i=1开始
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        //绘制不支持的物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    void Setup() {
        //设置相机的属性和矩阵
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.BeginSample(bufferName);
        
        ExecuteBuffer();
        
        
    }

  

    private void Submit() {
        buffer.EndSample(bufferName);
        ExecuteBuffer();
        context.Submit();
    }

    private void DrawVisibleGeometry() {
        SortingSettings sortingSettings = new SortingSettings(camera) {
            criteria =SortingCriteria.CommonOpaque
        };
        //设置渲染的shader Pass 和渲染排序
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId,sortingSettings);
        //只绘制RenderQueue为不透明的物体
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
       //绘制不透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        //绘制天空球
        context.DrawSkybox(camera);
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;

        //只绘制RenderQueue为Transparent透明的物体
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //绘制透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    
    private void ExecuteBuffer() {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    CullingResults cullingResults;
    bool Cull() {
        ScriptableCullingParameters p;
        if(camera.TryGetCullingParameters(out p)) {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;

    }

}
