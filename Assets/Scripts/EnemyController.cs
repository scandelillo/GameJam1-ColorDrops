using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// IA simple del enemigo: persigue al jugador y lo ataca al estar cerca.
// Para no encimarse con otros enemigos, aplica SEPARACIÓN (estilo boids): además
// de ir hacia el jugador, se aparta de los enemigos cercanos. Así rodean al
// jugador en vez de amontonarse en fila.
// Los tiempos de ataque, la cadencia, el bloqueo de movimiento y la interrupción
// los gestiona CombatState. El daño se aplica en la conexión (frame 18) y solo
// si el jugador sigue en rango (así se puede esquivar).
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EntityColor))]
[RequireComponent(typeof(CombatState))]
public class EnemyController : MonoBehaviour
{
    // Registro de todos los enemigos vivos, para calcular la separación entre ellos.
    private static readonly List<EnemyController> enemigos = new List<EnemyController>();

    [Header("Movimiento")]
    // Velocidad de persecución.
    [SerializeField] private float velocidad = 2.5f;
    // Qué tan rápido gira hacia su dirección.
    [SerializeField] private float velocidadGiro = 10f;

    [Header("Separación (anti-amontonamiento)")]
    // Distancia a la que empieza a apartarse de otro enemigo.
    [SerializeField] private float radioSeparacion = 2.75f;
    // Cuánto pesa ir hacia el jugador.
    [SerializeField] private float pesoPersecucion = 1f;
    // Cuánto pesa apartarse de los demás (súbelo si aún se enciman).
    [SerializeField] private float pesoSeparacion = 2.2f;

    [Header("Combate")]
    // Distancia a la que empieza a atacar en vez de perseguir.
    [SerializeField] private float rangoAtaque = 1.5f;
    // Daño base antes del multiplicador de color.
    [SerializeField] private float danioBase = 10f;

    [Header("Aparición")]
    // Duración del crecimiento de escala (0 -> tamaño real) al aparecer.
    [SerializeField] private float duracionAparicion = 0.4f;
    // Tiempo total inactivo tras aparecer (no se mueve ni ataca). Incluye el crecimiento.
    [SerializeField] private float esperaInicial = 1f;

    [Header("Al morir")]
    // Gota (pickup) que suelta al morir. Puede quedar vacío por ahora.
    [SerializeField] private GameObject prefabGota;
    // Tiempo que tarda en desaparecer tras morir (para que se vea la animación).
    [SerializeField] private float retardoDestruccion = 1.5f;
    // Duración del encogimiento (tamaño real -> 0) justo antes de destruirse.
    [SerializeField] private float duracionEncogimiento = 0.35f;

    [Header("Referencias")]
    // Visual, para animación de caminar.
    [SerializeField] private DroppyAnimationController visual;

    // Componentes propios.
    private Health vida;
    private EntityColor color;
    // Máquina de estados: tiempos de ataque, cadencia, bloqueo e interrupción.
    private CombatState combate;

    // Referencias al jugador (objetivo).
    private Transform objetivo;
    private Health vidaObjetivo;
    private EntityColor colorObjetivo;
    private CombatState combateObjetivo;

    // ¿Ya terminó la aparición? Hasta entonces no se mueve ni ataca.
    private bool activo;
    // Escala real del enemigo (a la que crece al aparecer).
    private Vector3 escalaOriginal;

    private void Awake()
    {
        vida = GetComponent<Health>();
        color = GetComponent<EntityColor>();
        combate = GetComponent<CombatState>();
        if (visual == null)
            visual = GetComponentInChildren<DroppyAnimationController>();

        // Guardamos la escala real y nacemos en 0 (la aparición nos hace crecer).
        escalaOriginal = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    private void OnEnable()
    {
        // Nos apuntamos al registro para la separación.
        enemigos.Add(this);
        // Al morir soltamos la gota; al conectar el ataque aplicamos el daño.
        vida.AlMorir.AddListener(Morir);
        combate.AlConectarAtaque += GolpearJugador;
    }

    private void OnDisable()
    {
        enemigos.Remove(this);
        vida.AlMorir.RemoveListener(Morir);
        combate.AlConectarAtaque -= GolpearJugador;
    }

    private void Start()
    {
        // Buscamos al jugador una sola vez y cacheamos lo que necesitamos.
        PlayerController jugador = FindObjectOfType<PlayerController>();
        if (jugador != null)
        {
            objetivo = jugador.transform;
            vidaObjetivo = jugador.GetComponent<Health>();
            colorObjetivo = jugador.GetComponent<EntityColor>();
            combateObjetivo = jugador.GetComponent<CombatState>();
        }

        // Aparición: crecer de 0 al tamaño real y esperar inactivo un momento.
        StartCoroutine(RutinaAparicion());
    }

    // Crece de escala 0 a la real y espera el resto de la inactividad inicial.
    private IEnumerator RutinaAparicion()
    {
        // Crecimiento 0 -> escala real.
        float transcurrido = 0f;
        while (transcurrido < duracionAparicion)
        {
            transcurrido += Time.deltaTime;
            transform.localScale = escalaOriginal * Mathf.Clamp01(transcurrido / duracionAparicion);
            yield return null;
        }
        transform.localScale = escalaOriginal;

        // Resto de la espera inicial (inactivo, sin moverse ni atacar).
        float resto = Mathf.Max(0f, esperaInicial - duracionAparicion);
        yield return new WaitForSeconds(resto);

        activo = true;
    }

    private void Update()
    {
        // Si no hay jugador o ya morimos, no hacemos nada.
        if (objetivo == null || vida.EstaMuerto) return;

        // Si el jugador ya murió, dejamos de perseguirlo y atacarlo (nos quedamos quietos).
        if (vidaObjetivo != null && vidaObjetivo.EstaMuerto)
        {
            if (visual != null)
                visual.SetSpeed(0f);
            return;
        }

        // Aún apareciendo: inactivo (ni se mueve ni ataca).
        if (!activo) return;

        // Si está atacando o aturdido, no se mueve (lo gestiona CombatState).
        if (!combate.PuedeMoverse) return;

        // Si compartimos color el daño es x0: no tiene sentido perseguir.
        if (colorObjetivo != null &&
            ColorRules.Multiplicador(color.ColorActual, colorObjetivo.ColorActual) <= 0f)
        {
            if (visual != null)
                visual.SetSpeed(0f);
            return;
        }

        // Dirección al jugador (aplanada al suelo) y distancia.
        Vector3 haciaObjetivo = objetivo.position - transform.position;
        haciaObjetivo.y = 0f;
        float distancia = haciaObjetivo.magnitude;

        // Ya en zona de ataque: nos plantamos y atacamos. NO nos movemos aunque
        // otros enemigos empujen (así se evita el girar sobre sí mismos).
        if (distancia <= rangoAtaque)
        {
            if (visual != null)
                visual.SetSpeed(0f);
            GirarHacia(haciaObjetivo);
            combate.SolicitarAtaque();
            return;
        }

        // Fuera de rango: perseguir + separación para no encimarse ni ir en fila.
        Vector3 separacion = CalcularSeparacion();
        Vector3 deseo = haciaObjetivo.normalized * pesoPersecucion + separacion * pesoSeparacion;
        deseo.y = 0f;

        // Aplicamos el movimiento con matemáticas puras.
        if (deseo.sqrMagnitude > 0.0001f)
        {
            Vector3 direccion = deseo.normalized;
            // El movimiento se limita al área jugable (el plano).
            transform.position = ArenaBounds.LimitarPosicion(transform.position + direccion * velocidad * Time.deltaTime);
            GirarHacia(direccion);
            if (visual != null)
                visual.SetSpeed(1f);
        }
        else
        {
            GirarHacia(haciaObjetivo);
            if (visual != null)
                visual.SetSpeed(0f);
        }
    }

    // Suma de empujes que apartan a este enemigo de los que tiene demasiado cerca.
    private Vector3 CalcularSeparacion()
    {
        Vector3 acumulado = Vector3.zero;

        foreach (EnemyController otro in enemigos)
        {
            if (otro == this || otro == null) continue;

            Vector3 diferencia = transform.position - otro.transform.position;
            diferencia.y = 0f;
            float distancia = diferencia.magnitude;

            // Solo cuentan los que están dentro del radio de separación.
            if (distancia > 0.0001f && distancia < radioSeparacion)
            {
                // Cuanto más cerca, más fuerte el empuje (peso inverso a la distancia).
                acumulado += diferencia.normalized * (1f - distancia / radioSeparacion);
            }
        }

        return acumulado;
    }

    // Rota suavemente hacia la dirección dada.
    private void GirarHacia(Vector3 direccion)
    {
        if (direccion.sqrMagnitude <= 0.0001f) return;
        Quaternion giro = Quaternion.LookRotation(direccion);
        transform.rotation = Quaternion.Slerp(transform.rotation, giro, velocidadGiro * Time.deltaTime);
    }

    // Se llama en la conexión del ataque (frame 18): aplica el daño al jugador
    // solo si sigue en rango (permite esquivar) y no comparten color.
    private void GolpearJugador()
    {
        if (vidaObjetivo == null || colorObjetivo == null) return;
        if (vidaObjetivo.EstaMuerto) return;

        // El jugador pudo esquivar: si ya no está en rango, el golpe falla.
        float distancia = Vector3.Distance(transform.position, objetivo.position);
        if (distancia > rangoAtaque) return;

        // Daño según el triángulo de color (0 si comparten color).
        float mult = ColorRules.Multiplicador(color.ColorActual, colorObjetivo.ColorActual);
        // Mismo color: ni daño ni empuje.
        if (mult <= 0f) return;

        vidaObjetivo.RecibirDanio(danioBase * mult);

        // Empujamos al jugador hacia atrás (lejos del enemigo).
        if (combateObjetivo != null)
            combateObjetivo.Empujar(objetivo.position - transform.position);
    }

    // Al morir: soltar gota, dejar ver la animación de muerte, encogerse y destruirse.
    // La animación de muerte la reproduce Health (llama a Die).
    private void Morir()
    {
        if (prefabGota != null)
            Instantiate(prefabGota, transform.position, Quaternion.identity);

        // Cortamos cualquier rutina en curso (p. ej. la aparición) para que no
        // pelee con el encogimiento por la escala.
        StopAllCoroutines();
        StartCoroutine(RutinaMuerte());
    }

    // Secuencia de muerte: ver la animación, encoger a 0 y destruir.
    private IEnumerator RutinaMuerte()
    {
        // Tiempo para que se aprecie la animación de muerte.
        yield return new WaitForSeconds(retardoDestruccion);

        // Encogimiento: escala actual -> 0.
        Vector3 inicio = transform.localScale;
        float transcurrido = 0f;
        while (transcurrido < duracionEncogimiento)
        {
            transcurrido += Time.deltaTime;
            transform.localScale = Vector3.Lerp(inicio, Vector3.zero, transcurrido / duracionEncogimiento);
            yield return null;
        }

        Destroy(gameObject);
    }

    // Dibuja los rangos en el editor: rojo = ataque, azul = separación.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioSeparacion);
    }
}
