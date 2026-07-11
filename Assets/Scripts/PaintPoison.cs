using System.Collections;
using UnityEngine;

// Veneno del lienzo: pisar pintura de OTRO color hace daño periódico.
// Va tanto en el jugador como en los enemigos.
//  - Tic INMEDIATO al pisar pintura ajena; luego otro cada "intervalo" (1.5 s)
//    mientras sigas encima. Al salir (o igualar el color) se reinicia.
//  - Color que nos hace counter (fuerte contra nosotros): -2 de vida.
//  - Color contra el que somos fuertes (nos daña a la mitad): -1 de vida.
//  - Pintura propia o lienzo sin pintar: nada.
// El daño es SILENCIOSO (Health.RecibirDanioVeneno): sin animación de daño,
// sin interrumpir ataques, sin empuje ni sacudida de cámara. El único feedback
// es una ligera contracción de escala (se encoge y vuelve a su tamaño).
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EntityColor))]
public class PaintPoison : MonoBehaviour
{
    [Header("Veneno")]
    // Cada cuántos segundos hace daño la pintura ajena.
    [SerializeField] private float intervalo = 1.5f;
    // Vida que quita la pintura contra la que somos fuertes (relación a la mitad).
    [SerializeField] private float danioDebil = 1f;
    // Vida que quita la pintura que nos hace counter (fuerte contra nosotros).
    [SerializeField] private float danioFuerte = 2f;

    [Header("Contracción (feedback visual del veneno)")]
    // A qué fracción del tamaño se contrae la entidad al recibir el tic.
    [SerializeField] private float escalaContraccion = 0.85f;
    // Duración total de la contracción (encoger y volver).
    [SerializeField] private float duracionContraccion = 0.22f;

    // Vida propia (recibe el daño del veneno).
    private Health vida;
    // Color propio (para comparar con la pintura pisada).
    private EntityColor color;
    // Visual, para el destello de color al recibir el tic.
    private DroppyAnimationController visual;
    // Tiempo acumulado sobre la pintura ajena actual (para la cadencia de 1.5 s).
    private float temporizador;
    // ¿El frame anterior ya estábamos sobre pintura que nos daña?
    private bool sobreVeneno;
    // Contracción en curso (para no encadenar dos a la vez).
    private Coroutine contraccion;

    private void Awake()
    {
        vida = GetComponent<Health>();
        color = GetComponent<EntityColor>();
        visual = GetComponentInChildren<DroppyAnimationController>();
    }

    private void Update()
    {
        // Los muertos ya no se envenenan.
        if (vida.EstaMuerto) return;

        // Miramos la pintura bajo los pies y cuánto nos daña (relación del triángulo).
        float multiplicador = 0f;
        if (PaintCanvas.Instancia != null &&
            PaintCanvas.Instancia.TryObtenerColor(transform.position, out DropletColor pintura))
        {
            multiplicador = ColorRules.Multiplicador(pintura, color.ColorActual);
        }

        // Pintura propia, lienzo virgen o fuera del plano: no hay veneno.
        // Reiniciamos para que al volver a pisar pintura ajena duela AL INSTANTE.
        if (multiplicador <= 0f)
        {
            sobreVeneno = false;
            return;
        }

        // Acabamos de pisar pintura ajena: primer tic inmediato.
        if (!sobreVeneno)
        {
            sobreVeneno = true;
            temporizador = 0f;
            AplicarVeneno(multiplicador);
            return;
        }

        // Seguimos encima: siguiente tic al cumplir el intervalo.
        temporizador += Time.deltaTime;
        if (temporizador >= intervalo)
        {
            temporizador -= intervalo;
            AplicarVeneno(multiplicador);
        }
    }

    // Aplica un tic de veneno: daño silencioso + contracción de escala.
    private void AplicarVeneno(float multiplicador)
    {
        // Counter (x2) → daño fuerte; relación débil (x0.5) → daño leve.
        float danio = multiplicador >= 2f ? danioFuerte : danioDebil;
        vida.RecibirDanioVeneno(danio);

        // Feedback visual: contracción de escala + destello sutil de color (si seguimos vivos).
        if (!vida.EstaMuerto)
        {
            if (contraccion == null)
                contraccion = StartCoroutine(RutinaContraccion());
            if (visual != null)
                visual.FlashDamage();
        }
    }

    // Encoge la entidad a una fracción de su tamaño y la devuelve a como estaba.
    private IEnumerator RutinaContraccion()
    {
        // Escala de partida (se restaura EXACTA al final, para no acumular error).
        Vector3 escalaBase = transform.localScale;
        Vector3 escalaChica = escalaBase * escalaContraccion;
        float mitad = duracionContraccion * 0.5f;

        // Encoger...
        float transcurrido = 0f;
        while (transcurrido < mitad)
        {
            // Si morimos a mitad de la contracción, restauramos y salimos
            // (la animación de muerte ya manda sobre la escala).
            if (vida.EstaMuerto) break;
            transcurrido += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaBase, escalaChica, transcurrido / mitad);
            yield return null;
        }

        // ...y volver.
        transcurrido = 0f;
        while (transcurrido < mitad)
        {
            if (vida.EstaMuerto) break;
            transcurrido += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaChica, escalaBase, transcurrido / mitad);
            yield return null;
        }

        transform.localScale = escalaBase;
        contraccion = null;
    }
}
