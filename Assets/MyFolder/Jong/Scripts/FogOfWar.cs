using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public ComputeShader fogCompute;
    public RenderTexture rt_Current;
    public RenderTexture rt_Overlap;

    private void Awake()
    {
        ClearRenderTexture(rt_Overlap);
    }
    private void Start()
    {
        rt_Overlap.enableRandomWrite = true;
        rt_Overlap.Create();
    }

    private void Update()
    {
        int kernel = fogCompute.FindKernel("Fow");

        fogCompute.SetTexture(kernel, "RT_Current", rt_Current);
        fogCompute.SetTexture(kernel, "RT_Overlap", rt_Overlap);

        int threadGroupsX = Mathf.CeilToInt(rt_Overlap.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(rt_Overlap.height / 8.0f);
        fogCompute.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
    }

    private void ClearRenderTexture(RenderTexture _rt)
    {
        RenderTexture.active = _rt;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
    }
}