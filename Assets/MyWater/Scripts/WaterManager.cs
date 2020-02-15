using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// Author:DreamFairy
/// Date:20200214

public class WaterManager : MonoBehaviour
{
    public GameObject WaterPlane;
    public float WaterPlaneWidth;
    public float WaterPlaneLength;
    public float WaveRadius = 1.0f;
    public float WaveSpeed = 1.0f;
    public float WaveViscosity = 1.0f; //粘度
    public float WaveAtten = 0.99f; //衰减
    [Range(0, 0.999f)]
    public float WaveHeight = 0.999f;
    public int WaveTextureResolution = 512;

    public UnityEngine.UI.RawImage WaveMarkDebugImg;
    public UnityEngine.UI.RawImage WaveTransmitDebugImg;
    public UnityEngine.UI.RawImage PrevWaveTransmitDebugImg;

    private RenderTexture m_waterWaveMarkTexture;
    private RenderTexture m_waveTransmitTexture;
    private RenderTexture m_prevWaveMarkTexture;

    private Material m_waterWaveMarkMat;
    private Material m_waveTransmitMat;

    private Vector4 m_waveTransmitParams;
    private Vector4 m_waveMarkParams;

    private CommandBuffer m_cmd;

    // Start is called before the first frame update
    void Awake()
    {
        m_waterWaveMarkTexture = new RenderTexture(WaveTextureResolution, WaveTextureResolution, 0, RenderTextureFormat.Default);
        m_waterWaveMarkTexture.name = "m_waterWaveMarkTexture";
        m_waveTransmitTexture = new RenderTexture(WaveTextureResolution, WaveTextureResolution, 0, RenderTextureFormat.Default);
        m_waveTransmitTexture.name = "m_waveTransmitTexture";
        m_prevWaveMarkTexture = new RenderTexture(WaveTextureResolution, WaveTextureResolution, 0, RenderTextureFormat.Default);
        m_prevWaveMarkTexture.name = "m_prevWaveMarkTexture";

        m_waterWaveMarkMat = new Material(Shader.Find("Unlit/WaveMarkerShader"));
        m_waveTransmitMat = new Material(Shader.Find("Unlit/WaveTransmitShader"));

        Shader.SetGlobalTexture("_WaveResult", m_waterWaveMarkTexture);
        Shader.SetGlobalFloat("_WaveHeight", WaveHeight);

        WaveMarkDebugImg.texture = m_waterWaveMarkTexture;
        WaveTransmitDebugImg.texture = m_waveTransmitTexture;
        PrevWaveTransmitDebugImg.texture = m_prevWaveMarkTexture;

        InitWaveTransmitParams();
    }

    /// <summary>
    /// 公式中的除uv外的常数在外部计算，以提升Shader性能
    /// </summary>
    void InitWaveTransmitParams()
    {
        float uvStep = 1.0f / WaveTextureResolution;
        float dt = Time.fixedDeltaTime;
        //最大递进粘性
        float maxWaveStepVisosity = uvStep / (2 * dt) * (Mathf.Sqrt(WaveViscosity * dt + 2));
        //粘度平方 u^2
        float waveVisositySqr = WaveViscosity * WaveViscosity;
        //当前速度
        float curWaveSpeed = maxWaveStepVisosity * WaveSpeed;
        //速度平方 c^2
        float curWaveSpeedSqr = curWaveSpeed * curWaveSpeed;
        //波单次位移平方 d^2
        float uvStepSqr = uvStep * uvStep;

        float i = Mathf.Sqrt(waveVisositySqr + 32 * curWaveSpeedSqr / uvStepSqr);
        float j = 8 * curWaveSpeedSqr / uvStepSqr;

        //波传递公式
        // (4 - 8 * c^2 * t^2 / d^2) / (u * t + 2) + (u * t - 2) / (u * t + 2) * z(x,y,z, t - dt) + (2 * c^2 * t^2 / d ^2) / (u * t + 2)
        // * (z(x + dx,y,t) + z(x - dx, y, t) + z(x,y + dy, t) + z(x, y - dy, t);

        //ut
        float ut = WaveViscosity * dt;
        //c^2 * t^2 / d^2
        float ctdSqr = curWaveSpeedSqr * dt * dt / uvStepSqr;
        // ut + 2
        float utp2 = ut + 2;
        // ut - 2
        float utm2 = ut - 2;
        //(4 - 8 * c^2 * t^2 / d^2) / (u * t + 2) 
        float p1 = (4 - 8 * ctdSqr) / utp2;
        //(u * t - 2) / (u * t + 2)
        float p2 = utm2 / utp2;
        //(2 * c^2 * t^2 / d ^2) / (u * t + 2)
        float p3 = (2 * ctdSqr) / utp2;

        m_waveTransmitParams.Set(p1, p2, p3, uvStep);

        Debug.LogFormat("i {0} j {1} maxSpeed {2}", i, j, maxWaveStepVisosity);
        Debug.LogFormat("p1 {0} p2 {1} p3 {2}", p1, p2, p3);
    }

    private void OnPreRender()
    {
        WaterPlaneCollider();
        WaterMark();
        WaveTransmit();
    }

    Vector2 hitPos = Vector2.zero;
    bool hasHit = false;
    void WaterPlaneCollider()
    {
        hasHit = false;
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo = new RaycastHit();
            bool ret = Physics.Raycast(ray.origin, ray.direction, out hitInfo);
            if (ret)
            {
                Vector3 waterPlaneSpacePos = WaterPlane.transform.worldToLocalMatrix * new Vector4(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z, 1);

                float dx = (waterPlaneSpacePos.x / WaterPlaneWidth) + 0.5f;
                float dy = (waterPlaneSpacePos.z / WaterPlaneLength) + 0.5f;

                hitPos.Set(dx, dy);
                m_waveMarkParams.Set(dx, dy, WaveRadius * WaveRadius, WaveHeight);

                hasHit = true;
            }
        }
    }

    void WaterMark()
    {
        if (hasHit)
        {
            m_waterWaveMarkMat.SetVector("_WaveMarkParams", m_waveMarkParams);
            Graphics.Blit(m_waveTransmitTexture, m_waterWaveMarkTexture, m_waterWaveMarkMat);
        }
    }

    void WaveTransmit()
    {
        m_waveTransmitMat.SetVector("_WaveTransmitParams", m_waveTransmitParams);
        m_waveTransmitMat.SetFloat("_WaveAtten", WaveAtten);
        m_waveTransmitMat.SetTexture("_PrevWaveMarkTex", m_prevWaveMarkTexture);

        RenderTexture rt = RenderTexture.GetTemporary(WaveTextureResolution, WaveTextureResolution);
        Graphics.Blit(m_waterWaveMarkTexture, rt, m_waveTransmitMat);
        Graphics.Blit(m_waterWaveMarkTexture, m_prevWaveMarkTexture);
        Graphics.Blit(rt, m_waterWaveMarkTexture);
        Graphics.Blit(rt, m_waveTransmitTexture);
        RenderTexture.ReleaseTemporary(rt);
    }
}
