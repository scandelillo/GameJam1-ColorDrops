using System;
using System.Collections;
using UnityEngine;

// Máquina de estados de combate compartida por jugador y enemigo.
// Se encarga de tres cosas:
//   1. Que al ATACAR o al ser GOLPEADO el personaje NO se pueda mover.
//   2. Que el daño del ataque solo se aplique en el "punto de conexión"
//      (frame 18 a 30 fps = 0.6 s), no al instante en que se pulsa.
//   3. Que recibir daño INTERRUMPA el ataque en curso: si aún no conectaba,
//      el golpe se pierde y no hace daño.
[RequireComponent(typeof(Health))]
public class CombatState : MonoBehaviour
{
    // Estados posibles del personaje.
    private enum Estado { Libre, Atacando, Aturdido, Muerto }

    [Header("Tiempos de ataque")]
    // Tiempo desde que arranca la animación hasta que el golpe conecta.
    // Frame 18 a 30 fps = 0.6 s.
    [SerializeField] private float tiempoConexion = 0.6f;
    // Duración total del bloqueo por atacar (hasta poder moverse otra vez).
    // Debe ser >= tiempoConexion; ajústalo a lo que dura tu clip de ataque.
    [SerializeField] private float duracionAtaque = 0.9f;
    // Segundos mínimos entre el inicio de un ataque y el siguiente.
    [SerializeField] private float cadenciaAtaque = 1f;

    [Header("Reacción al daño")]
    // Tiempo que queda aturdido (sin moverse) tras recibir un golpe.
    [SerializeField] private float duracionAturdido = 0.3f;

    [Header("Empuje (knockback)")]
    // Cuánto retrocede este personaje al ser golpeado.
    [SerializeField] private float distanciaEmpuje = 0.7f;
    // En cuánto tiempo se recorre ese empuje (más corto = más seco).
    [SerializeField] private float duracionEmpuje = 0.12f;

    [Header("Referencias")]
    // Visual, para disparar la animación de ataque y frenar el movimiento.
    [SerializeField] private DroppyAnimationController visual;

    // Se dispara en el punto de conexión del ataque (frame 18).
    // El script de combate se suscribe aquí para aplicar el daño.
    public event Action AlConectarAtaque;

    // Componente de vida propio.
    private Health vida;
    // Estado actual del personaje.
    private Estado estado = Estado.Libre;
    // Momento en que empezó el último ataque (para la cadencia).
    private float ultimoAtaque = -999f;
    // Corrutinas activas (para poder cortarlas al interrumpir).
    private Coroutine rutinaAtaque;
    private Coroutine rutinaAturdido;
    // Empuje en curso: velocidad a aplicar y momento en que termina.
    private Vector3 velocidadEmpuje;
    private float finEmpuje;

    // ¿Puede moverse el personaje? Solo cuando está libre (vivo y sin actuar).
    public bool PuedeMoverse => estado == Estado.Libre;
    // ¿Está en plena animación de ataque?
    public bool EstaAtacando => estado == Estado.Atacando;

    private void Awake()
    {
        vida = GetComponent<Health>();
        if (visual == null)
            visual = GetComponentInChildren<DroppyAnimationController>();
    }

    private void OnEnable()
    {
        // Escuchamos la vida para interrumpir al recibir daño y detenernos al morir.
        vida.AlRecibirDanio.AddListener(Interrumpir);
        vida.AlMorir.AddListener(Detener);
    }

    private void OnDisable()
    {
        vida.AlRecibirDanio.RemoveListener(Interrumpir);
        vida.AlMorir.RemoveListener(Detener);
    }

    private void Update()
    {
        // Aplicamos el empuje (knockback) mientras dure, desplazando el transform.
        // Se limita al área jugable para que un empujón no te saque del plano.
        if (Time.time < finEmpuje)
            transform.position = ArenaBounds.LimitarPosicion(transform.position + velocidadEmpuje * Time.deltaTime);
    }

    // Empuja al personaje en la dirección dada (el atacante la calcula: víctima
    // menos atacante). La distancia y duración son propias de este personaje.
    public void Empujar(Vector3 direccion)
    {
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) return;
        direccion.Normalize();

        // velocidad = distancia / duración, para recorrer el empuje en ese tiempo.
        velocidadEmpuje = direccion * (distanciaEmpuje / Mathf.Max(duracionEmpuje, 0.0001f));
        finEmpuje = Time.time + duracionEmpuje;
    }

    // Intenta iniciar un ataque. Devuelve true si se pudo (libre y cadencia
    // cumplida). El daño real se aplica más tarde, en la conexión.
    public bool SolicitarAtaque()
    {
        // Solo se puede atacar estando libre.
        if (estado != Estado.Libre) return false;
        // Respetamos la cadencia entre ataques.
        if (Time.time < ultimoAtaque + cadenciaAtaque) return false;

        ultimoAtaque = Time.time;
        estado = Estado.Atacando;

        // Frenamos y disparamos la animación de ataque.
        if (visual != null)
        {
            visual.SetSpeed(0f);
            visual.Attack();
        }

        rutinaAtaque = StartCoroutine(RutinaAtaque());
        return true;
    }

    // Secuencia del ataque: preámbulo -> conexión -> recuperación.
    private IEnumerator RutinaAtaque()
    {
        // Preámbulo: esperamos hasta el punto de conexión (frame 18).
        yield return new WaitForSeconds(tiempoConexion);

        // Conexión: ahora sí el golpe hace efecto (aquí se aplica el daño).
        AlConectarAtaque?.Invoke();

        // Recuperación: resto de la animación antes de poder moverse.
        float recuperacion = Mathf.Max(0f, duracionAtaque - tiempoConexion);
        yield return new WaitForSeconds(recuperacion);

        rutinaAtaque = null;
        estado = Estado.Libre;
    }

    // Al recibir daño: interrumpe el ataque en curso (si no conectó, no hace
    // daño) y deja al personaje aturdido un instante.
    private void Interrumpir()
    {
        // La muerte la gestiona Detener; aquí solo el daño no letal.
        if (vida.EstaMuerto) return;

        // Cancelamos el ataque en curso: si aún no conectaba, se pierde el golpe.
        if (rutinaAtaque != null)
        {
            StopCoroutine(rutinaAtaque);
            rutinaAtaque = null;
        }

        // Reiniciamos el aturdimiento (sin moverse) por un momento.
        if (rutinaAturdido != null)
            StopCoroutine(rutinaAturdido);
        rutinaAturdido = StartCoroutine(RutinaAturdido());
    }

    // Bloquea el movimiento mientras dura el aturdimiento por el golpe.
    private IEnumerator RutinaAturdido()
    {
        estado = Estado.Aturdido;
        if (visual != null)
            visual.SetSpeed(0f);

        yield return new WaitForSeconds(duracionAturdido);

        rutinaAturdido = null;
        estado = Estado.Libre;
    }

    // Al morir: se corta todo y ya no puede actuar ni moverse.
    private void Detener()
    {
        if (rutinaAtaque != null) StopCoroutine(rutinaAtaque);
        if (rutinaAturdido != null) StopCoroutine(rutinaAturdido);
        rutinaAtaque = null;
        rutinaAturdido = null;
        estado = Estado.Muerto;
    }
}
