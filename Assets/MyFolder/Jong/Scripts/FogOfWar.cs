using UnityEngine;
using UnityEngine.Rendering;

public class FogOfWar : MonoBehaviour
{
    [Header("Camera")]
    public Camera cameraAlpha;
    
    public ComputeShader fogCompute;
    public RenderTexture rt_Current;
    public RenderTexture rt_Overlap;
    public RenderTexture rt_Temp;

    private RenderTexture rt_BlurOverlap;
    [Range(1,10)]
    public int blurDetail = 1;
    private void Awake()
    {
        ClearRenderTexture(rt_Overlap);
    }
    private void Start()
    {
        rt_Overlap.enableRandomWrite = true;
        rt_Overlap.Create();
        rt_BlurOverlap = new RenderTexture(rt_Overlap.descriptor);
        rt_BlurOverlap.Create();
        rt_BlurOverlap.enableRandomWrite = true;
    }

    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

    }
    private void Update()
    {
        
    }

    private void OnEndCameraRendering(ScriptableRenderContext _context, Camera _renderedCamera)
    {
        if(_renderedCamera == cameraAlpha)
        {
            int kernel = fogCompute.FindKernel("Fow");
            fogCompute.SetTexture(kernel, "RT_Current", rt_Current);
            fogCompute.SetTexture(kernel, "RT_Overlap", rt_Overlap);
            int threadGroupsX = Mathf.CeilToInt(rt_Overlap.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(rt_Overlap.height / 8.0f);
            fogCompute.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
            
            kernel = fogCompute.FindKernel("Blur");
            fogCompute.SetTexture(kernel, "RT_Overlap", rt_Overlap);
            fogCompute.SetTexture(kernel, "RT_BlurOverlap", rt_BlurOverlap);
            fogCompute.SetInt("BlurDetail", blurDetail);
            fogCompute.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

            Shader.SetGlobalVector("_MapParams", new Vector4(0, 0, 100, 0));
            Shader.SetGlobalTexture("_GlobalMap", rt_BlurOverlap);
            Shader.SetGlobalTexture("_GlobalCurrentMap", rt_Current);
            if(rt_Temp != null)
            {
                Shader.SetGlobalTexture("_GlobalTempMap", rt_Temp);
            }
        }
    }

    private void ClearRenderTexture(RenderTexture _rt)
    {
        RenderTexture.active = _rt;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
    }
}