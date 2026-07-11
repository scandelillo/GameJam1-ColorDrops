using UnityEngine;

// Lienzo de pintura del juego. Va en el objeto del PLANO (junto a ArenaBounds).
// Mantiene DOS cosas sincronizadas:
//  - Una rejilla lógica (celdas) con el color pintado en cada punto → la consulta el veneno.
//  - Una Texture2D que se muestra en un QUAD propio (overlay) justo encima del plano.
// El overlay lo generamos nosotros con UVs conocidas (X→U, Z→V) sobre el MISMO
// rectángulo que usamos al pintar, así la pintura cae EXACTAMENTE bajo la entidad.
// Ventaja: no dependemos del material ni de las UVs del plano (funciona con cualquier
// shader, incluso Shader Graph). Asume que el plano no está rotado (igual que ArenaBounds).
[RequireComponent(typeof(Renderer))]
public class PaintCanvas : MonoBehaviour
{
    // Instancia única para acceso sencillo (mismo patrón que ArenaBounds).
    public static PaintCanvas Instancia { get; private set; }

    [Header("Lienzo")]
    // Lado de la textura en texeles (cuadrada). Súbelo a 512 si el trazo se ve muy pixelado.
    [SerializeField] private int resolucion = 256;
    // Color del lienzo sin pintar (el "blanco" del canvas).
    [SerializeField] private Color colorFondo = Color.white;

    [Header("Pincel")]
    // Radio de cada pincelada en unidades del mundo.
    [SerializeField] private float radioPincel = 0.45f;

    [Header("Colores de la pintura (índice = DropletColor)")]
    // Color con el que se pinta cada pigmento. Ajústalos a la paleta del juego.
    [SerializeField] private Color pinturaRojo = new Color(0.85f, 0.20f, 0.20f);
    [SerializeField] private Color pinturaAmarillo = new Color(0.95f, 0.80f, 0.20f);
    [SerializeField] private Color pinturaAzul = new Color(0.25f, 0.40f, 0.85f);

    [Header("Overlay")]
    // Altura del quad de pintura por encima del plano (evita z-fighting con el suelo).
    [SerializeField] private float alturaOverlay = 0.02f;

    // Textura donde se pinta (se crea en runtime y se muestra en el quad overlay).
    private Texture2D textura;
    // Copia en CPU de los pixeles (se sube a la textura solo cuando hay cambios).
    private Color32[] pixeles;
    // Rejilla lógica: -1 = sin pintar, 0/1/2 = DropletColor. Mismo orden que los pixeles.
    private sbyte[] celdas;
    // Renderer del plano (su AABB define el rectángulo XZ del lienzo).
    private Renderer render;
    // Cuántos texeles equivalen a una unidad del mundo (para el radio del pincel).
    private float texelesPorUnidad;
    // ¿Hay pinceladas nuevas pendientes de subir a la textura?
    private bool sucio;

    private void Awake()
    {
        Instancia = this;
        render = GetComponent<Renderer>();

        // Creamos la textura del lienzo, toda del color de fondo y sin nada pintado.
        textura = new Texture2D(resolucion, resolucion, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;

        pixeles = new Color32[resolucion * resolucion];
        celdas = new sbyte[resolucion * resolucion];
        Color32 fondo = colorFondo;
        for (int i = 0; i < pixeles.Length; i++)
        {
            pixeles[i] = fondo;
            celdas[i] = -1;
        }
        textura.SetPixels32(pixeles);
        textura.Apply(false);

        // Escala mundo → texeles, a partir del tamaño real del plano.
        Vector3 tamano = render.bounds.size;
        float ladoPromedio = (tamano.x + tamano.z) * 0.5f;
        texelesPorUnidad = ladoPromedio > 0.0001f ? resolucion / ladoPromedio : 1f;

        // Creamos el quad de pintura (overlay) que muestra la textura sobre el plano.
        CrearOverlay();
    }

    // Genera un quad propio justo encima del plano, con UVs que mapean X→U y Z→V
    // sobre el rectángulo del plano (el MISMO mapeo que usamos al pintar). Así la
    // pintura se ve exactamente donde pasan los personajes, sin depender del material
    // ni de las UVs del plano.
    private void CrearOverlay()
    {
        Bounds limites = render.bounds;
        float alturaQuad = limites.max.y + alturaOverlay;

        // Cuatro esquinas en coordenadas del mundo (el overlay va con transform en identidad).
        Vector3[] vertices =
        {
            new Vector3(limites.min.x, alturaQuad, limites.min.z),
            new Vector3(limites.max.x, alturaQuad, limites.min.z),
            new Vector3(limites.min.x, alturaQuad, limites.max.z),
            new Vector3(limites.max.x, alturaQuad, limites.max.z),
        };
        // UVs: X del mundo → U, Z del mundo → V (coincide con TryTexel).
        Vector2[] uvs =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };
        int[] triangulos = { 0, 2, 1, 2, 3, 1 };

        Mesh malla = new Mesh { name = "PaintOverlayMesh" };
        malla.vertices = vertices;
        malla.uv = uvs;
        malla.triangles = triangulos;
        malla.RecalculateNormals();
        malla.RecalculateBounds();

        // Objeto en la raíz (sin padre) para que la escala del plano no deforme el quad.
        GameObject overlay = new GameObject("PaintOverlay");
        overlay.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        overlay.AddComponent<MeshFilter>().mesh = malla;

        // Material propio: unlit y con transparencia. Sprites/Default va SIEMPRE incluido
        // y dibuja por ambas caras (Cull Off), así no hay que preocuparse del winding.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        Material materialOverlay = new Material(shader);
        materialOverlay.mainTexture = textura;

        MeshRenderer renderOverlay = overlay.AddComponent<MeshRenderer>();
        renderOverlay.sharedMaterial = materialOverlay;
        renderOverlay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderOverlay.receiveShadows = false;
    }

    private void OnDestroy()
    {
        // Limpiamos la instancia si somos nosotros (por si se recarga la escena).
        if (Instancia == this)
            Instancia = null;
    }

    // Subimos los cambios a la GPU una sola vez por frame (aunque pinten muchos a la vez).
    private void LateUpdate()
    {
        if (!sucio) return;
        textura.SetPixels32(pixeles);
        textura.Apply(false);
        sucio = false;
    }

    // Pinta un círculo del color dado en la posición del mundo indicada.
    // Si alguien ya pintó ahí, el color nuevo REEMPLAZA al viejo.
    public void Pintar(Vector3 posicionMundo, DropletColor color)
    {
        // Localizamos el texel bajo esa posición del mundo.
        if (!TryTexel(posicionMundo, out int centroX, out int centroY)) return;

        // Radio del pincel pasado a texeles (mínimo 1 para que siempre marque algo).
        int radio = Mathf.Max(1, Mathf.RoundToInt(radioPincel * texelesPorUnidad));
        int radioCuadrado = radio * radio;
        Color32 pintura = ColorDePintura(color);
        sbyte indice = (sbyte)color;

        // Recorremos el cuadrado que envuelve al círculo y pintamos lo que cae dentro.
        for (int dy = -radio; dy <= radio; dy++)
        {
            int y = centroY + dy;
            if (y < 0 || y >= resolucion) continue;

            for (int dx = -radio; dx <= radio; dx++)
            {
                int x = centroX + dx;
                if (x < 0 || x >= resolucion) continue;
                if (dx * dx + dy * dy > radioCuadrado) continue;

                int i = y * resolucion + x;
                celdas[i] = indice;
                pixeles[i] = pintura;
            }
        }

        sucio = true;
    }

    // Devuelve el color pintado bajo esa posición del mundo.
    // false = ahí no hay pintura (lienzo virgen) o la posición cae fuera del plano.
    public bool TryObtenerColor(Vector3 posicionMundo, out DropletColor color)
    {
        color = DropletColor.Rojo;
        if (!TryTexel(posicionMundo, out int x, out int y)) return false;

        sbyte celda = celdas[y * resolucion + x];
        if (celda < 0) return false;

        color = (DropletColor)celda;
        return true;
    }

    // Convierte una posición del mundo al texel correspondiente usando el rectángulo
    // XZ del plano (su AABB): X del mundo → U, Z del mundo → V. Sin raycasts ni UVs
    // del mesh, así que no puede fallar en silencio. Devuelve false si cae fuera del plano.
    private bool TryTexel(Vector3 posicionMundo, out int x, out int y)
    {
        x = y = 0;

        Bounds limites = render.bounds;
        if (limites.size.x <= 0.0001f || limites.size.z <= 0.0001f) return false;

        // Normalizamos la posición dentro del rectángulo del plano (0..1).
        float u = (posicionMundo.x - limites.min.x) / limites.size.x;
        float v = (posicionMundo.z - limites.min.z) / limites.size.z;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;

        x = Mathf.Clamp((int)(u * resolucion), 0, resolucion - 1);
        y = Mathf.Clamp((int)(v * resolucion), 0, resolucion - 1);
        return true;
    }

    // Color visual de cada pigmento.
    private Color32 ColorDePintura(DropletColor color)
    {
        switch (color)
        {
            case DropletColor.Amarillo: return pinturaAmarillo;
            case DropletColor.Azul: return pinturaAzul;
            default: return pinturaRojo;
        }
    }
}
