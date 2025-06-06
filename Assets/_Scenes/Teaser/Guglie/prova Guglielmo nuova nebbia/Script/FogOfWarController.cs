using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class FogOfWarController : MonoBehaviour
{
    // Singleton
    public static FogOfWarController Instance { get; private set; }

    [Header("Mask Settings")]
    public int textureSize = 256;                  // risoluzione delle mask RT
    public Texture2D lightCookie;                 // la texture cookie importata
    [Range(0.1f, 10f)] public float cookieWorldRadius = 5f;

    [Header("Revealers")]
    public List<Transform> pirateRevealers;       // transforms dei pirati
    public Transform ratRevealer;                 // transform del topo

    [HideInInspector] public RenderTexture PersistentMaskRT;  // RT permanente (pirati)
    [HideInInspector] public RenderTexture WorkingMaskRT;     // RT di lavoro (clone + topo)

    private Camera cam;

    void Awake()
    {
        // setup singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        cam = GetComponent<Camera>();

        // crea le RT
        PersistentMaskRT = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.R8);
        WorkingMaskRT = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.R8);

        // inizializza la mask dei pirati (una volta sola)
        Graphics.SetRenderTarget(PersistentMaskRT);
        GL.Clear(true, true, Color.black);
        DrawRevealersOnMask(pirateRevealers, PersistentMaskRT);
        Graphics.SetRenderTarget(null);
    }

    /// <summary>
    /// Disegna i cookie sfumati per una lista di revealers su una RT.
    /// </summary>
    private void DrawRevealersOnMask(IEnumerable<Transform> revealers, RenderTexture targetRT)
    {
        foreach (var t in revealers)
        {
            Vector3 vp = cam.WorldToViewportPoint(t.position);
            if (vp.z < 0) continue;

            float rUV = cookieWorldRadius / cam.orthographicSize;
            Vector2 scale = new Vector2(rUV * 2f, rUV * 2f);
            Vector2 offset = new Vector2(vp.x - rUV, vp.y - rUV);

            CommandBuffer cmd = new CommandBuffer();
            cmd.SetRenderTarget(targetRT);
            cmd.Blit(lightCookie, targetRT, scale, offset);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }
    }

    /// <summary>
    /// Viene chiamato dal Render Feature per disegnare il cookie live del topo.
    /// </summary>
    public void DrawRatCookie(CommandBuffer cmd, RenderTexture workingMask)
    {
        Vector3 vp = cam.WorldToViewportPoint(ratRevealer.position);
        if (vp.z < 0) return;

        float rUV = cookieWorldRadius / cam.orthographicSize;
        Vector2 scale = new Vector2(rUV * 2f, rUV * 2f);
        Vector2 offset = new Vector2(vp.x - rUV, vp.y - rUV);

        cmd.SetRenderTarget(workingMask);
        cmd.Blit(lightCookie, workingMask, scale, offset);
        // No need to reset render target; URP will restore it after pass
    }
}
