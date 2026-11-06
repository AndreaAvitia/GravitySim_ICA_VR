using UnityEngine;
using UltimateXR.Manipulation;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UxrGrabbableObject))]
public class DardoVR : MonoBehaviour
{
    private Rigidbody rb;
    private UxrGrabbableObject grabbableObject;
    private Collider dardoCollider;

    private bool estaAgarrado = false;
    private bool estaClavado = false;
    private bool estaEnVuelo = false;
    private bool aplicarEmpuje = false;

    [Header("Configuración de Impacto")]
    public string tagTablero = "TableroDardos";
    public float profundidadClavado = 0.02f;

    [Header("Giroscopio de Tiro (Sin pelear con la mano)")]
    [Tooltip("Corrige el ángulo y la dirección en cuanto sueltas el dardo para que no salga chueco.")]
    public bool usarGiroscopioAlLanzar = true;

    [Tooltip("Qué tanto endereza el tiro hacia el frente (0 = libre, 0.75 = corrige muñeca chueca).")]
    [Range(0f, 1f)]
    public float estabilidadGiroscopio =1f;

    [Tooltip("Tiempo en segundos sin chocar con la mano al soltarlo.")]
    public float tiempoIgnorarManoAlSoltar = 0.18f;

    [Header("Falsificación de Gravedad (Parábola muy plana)")]
    [Tooltip("Escala de caída en vuelo para Luna/Marte (0.15 = vuelo súper plano).")]
    [Range(0.05f, 0.5f)]
    public float escalaVueloBajaGravedad = 0.15f;

    [Tooltip("Escala de caída en vuelo para la Tierra (0.10 = trayectoria casi recta).")]
    [Range(0.02f, 0.5f)]
    public float escalaVueloTierra = 0.10f;

    [Tooltip("Escala de gravedad tras rebotar en el piso en la Tierra (0.85 = cae firme al pasto).")]
    [Range(0.5f, 1.0f)]
    public float factorEscalaTierraPiso = 0.85f;

    [Header("Trayectoria plana al inicio del vuelo")]
    [Tooltip("Fracción de la gravedad al soltar el dardo (0 = sin caída al inicio, 1 = gravedad completa).")]
    [Range(0f, 1f)]
    public float gravedadInicialFraccion = 0.15f;

    [Tooltip("Segundos que tarda la gravedad en llegar al 100% (0 = desactivado).")]
    [Range(0f, 2f)]
    public float duracionRampaGravedad = 0.35f;

    [Header("Impulso de Lanzamiento")]
    public float impulsoBajaGravedad = 4.2f;
    public float impulsoAltaGravedad = 4.6f;
    public float umbralLanzamiento = 0.8f;

    [Header("Optimización")]
    [Tooltip("Segundos antes de desaparecer tras soltarlo (120 = 2 minutos).")]
    public float tiempoDeVida = 120f;

    private float _gravedadVueloEfectiva;
    private float _gravedadPisoEfectiva;
    private float _multiplicadorHorizontal;
    private float _multiplicadorVertical;
    private float _frenoHorizontal;
    private float _tiempoVuelo;

    private Transform _manoActualTransform;
    private Collider[] _collidersManoActual;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbableObject = GetComponent<UxrGrabbableObject>();
        dardoCollider = GetComponent<Collider>();

        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void OnEnable()
    {
        if (UxrGrabManager.Instance != null)
        {
            UxrGrabManager.Instance.ObjectGrabbed += OnObjectGrabbed;
            UxrGrabManager.Instance.ObjectReleased += OnObjectReleased;
        }
    }

    void OnDisable()
    {
        if (UxrGrabManager.Instance != null)
        {
            UxrGrabManager.Instance.ObjectGrabbed -= OnObjectGrabbed;
            UxrGrabManager.Instance.ObjectReleased -= OnObjectReleased;
        }
    }

    private void OnObjectGrabbed(object sender, UxrManipulationEventArgs e)
    {
        if (e.GrabbableObject == grabbableObject)
        {
            estaAgarrado = true;
            estaEnVuelo = false;
            estaClavado = false;
            aplicarEmpuje = false;
            dardoCollider.enabled = true;
            transform.SetParent(null);

            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = false;
            rb.useGravity = false;

            CancelInvoke(nameof(Desaparecer));
            CancelInvoke(nameof(ActivarColisiones));

            rb.detectCollisions = false;

            if (e.Grabber != null)
            {
                _manoActualTransform = e.Grabber.transform;
                _collidersManoActual = e.Grabber.GetComponentsInChildren<Collider>(true);
                IgnorarColisionesConMano(true);
            }
        }
    }

    private void OnObjectReleased(object sender, UxrManipulationEventArgs e)
    {
        if (e.GrabbableObject == grabbableObject)
        {
            if (UxrGrabManager.Instance != null && UxrGrabManager.Instance.IsBeingGrabbed(grabbableObject))
                return;

            estaAgarrado = false;
            estaEnVuelo = true;
            estaClavado = false;
            rb.isKinematic = false;
            rb.useGravity = false;

            // CONGELAMOS LA ROTACIÓN FÍSICA EN EL MISMO INSTANTE EN QUE SE SUELTA:
            // Esto elimina de raíz cualquier latigazo o giro errático al soltar rápido el gatillo.
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            aplicarEmpuje = true;
            _tiempoVuelo = 0f;

            CalcularParametrosPorGravedad(Physics.gravity.y);

            Invoke(nameof(ActivarColisiones), tiempoIgnorarManoAlSoltar);
            Invoke(nameof(Desaparecer), tiempoDeVida);
        }
    }

    private void IgnorarColisionesConMano(bool ignorar)
    {
        if (_collidersManoActual == null || dardoCollider == null) return;

        foreach (Collider col in _collidersManoActual)
        {
            if (col != null && col != dardoCollider)
            {
                Physics.IgnoreCollision(dardoCollider, col, ignorar);
            }
        }
    }

    private void ActivarColisiones()
    {
        if (!estaAgarrado && !estaClavado)
        {
            rb.detectCollisions = true;
        }
    }

    private void Desaparecer()
    {
        Destroy(gameObject);
    }

    private void CalcularParametrosPorGravedad(float gravedadY)
    {
        float gAbs = Mathf.Abs(gravedadY);
        float t = Mathf.InverseLerp(0f, 9.81f, gAbs);

        float factorVuelo = Mathf.Lerp(escalaVueloBajaGravedad, escalaVueloTierra, t);
        _gravedadVueloEfectiva = gAbs * factorVuelo;

        float factorPiso = Mathf.Lerp(escalaVueloBajaGravedad, factorEscalaTierraPiso, t);
        _gravedadPisoEfectiva = gAbs * factorPiso;

        float tSliderCompleto = Mathf.InverseLerp(0f, 19.62f, gAbs);
        _multiplicadorHorizontal = Mathf.Lerp(impulsoBajaGravedad, impulsoAltaGravedad, tSliderCompleto);

        _multiplicadorVertical = Mathf.Lerp(0.55f, 0.45f, tSliderCompleto);
        _frenoHorizontal = Mathf.Lerp(0.999f, 0.997f, tSliderCompleto);
    }

    void FixedUpdate()
    {
        if (rb.isKinematic || estaClavado || estaAgarrado) return;

        if (aplicarEmpuje)
        {
            aplicarEmpuje = false;
            rb.angularVelocity = Vector3.zero;

            // Tope de seguridad por si un movimiento ultra rápido del control genera un pico irreal en VR
            float velocidadDeMano = Mathf.Min(rb.linearVelocity.magnitude, 6.0f);

            if (velocidadDeMano > umbralLanzamiento)
            {
                Vector3 direccionMano = rb.linearVelocity.normalized;

                // Enderezamos la trayectoria con el giroscopio al momento de lanzar (sin pelear con la mano antes)
                if (usarGiroscopioAlLanzar && Camera.main != null)
                {
                    Vector3 dirFrente = Camera.main.transform.forward;
                    dirFrente.y = Mathf.Clamp(dirFrente.y, -0.02f, 0.18f);
                    dirFrente.Normalize();

                    // Solo corrige si el lanzamiento va hacia la zona frontal del jugador
                    if (Vector3.Dot(direccionMano, dirFrente) > 0.1f)
                    {
                        direccionMano = Vector3.Slerp(direccionMano, dirFrente, estabilidadGiroscopio * 0.55f).normalized;
                    }
                }

                Vector3 impulsoExtra = (direccionMano * 0.65f) + (Vector3.up * 0.01f);
                Vector3 vel = (direccionMano * velocidadDeMano) + impulsoExtra;

                vel.x *= _multiplicadorHorizontal;
                vel.z *= _multiplicadorHorizontal;
                vel.y *= _multiplicadorVertical;

                rb.linearVelocity = vel;

                if (vel.sqrMagnitude > 0.1f)
                {
                    rb.MoveRotation(Quaternion.LookRotation(-vel.normalized));
                }
            }
        }

        if (estaEnVuelo)
        {
            _tiempoVuelo += Time.fixedDeltaTime;

            float rampa = duracionRampaGravedad > 0f
                ? Mathf.Clamp01(_tiempoVuelo / duracionRampaGravedad)
                : 1f;

            float factorRampa = Mathf.Lerp(gravedadInicialFraccion, 1f, rampa);

            rb.linearVelocity += Vector3.down * (_gravedadVueloEfectiva * factorRampa * Time.fixedDeltaTime);

            Vector3 velH = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) * _frenoHorizontal;
            rb.linearVelocity = new Vector3(velH.x, rb.linearVelocity.y, velH.z);

            // Usamos rb.MoveRotation en lugar de transform.forward para no pelear con el motor de físicas
            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                rb.MoveRotation(Quaternion.LookRotation(-rb.linearVelocity.normalized));
            }
        }
        else
        {
            rb.linearVelocity += Vector3.down * (_gravedadPisoEfectiva * Time.fixedDeltaTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (estaClavado) return;

        if (_manoActualTransform != null && collision.transform.IsChildOf(_manoActualTransform))
            return;

        if (collision.gameObject.CompareTag(tagTablero))
        {
            ClavarDardo(collision);
        }
        else
        {
            estaEnVuelo = false;
            // Si choca con el piso o pared, le devolvemos la rotación física normal para que ruede/caiga natural
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    private void ClavarDardo(Collision collision)
    {
        estaClavado = true;
        estaEnVuelo = false;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        dardoCollider.enabled = false;

        transform.SetParent(collision.transform);
        transform.forward = collision.contacts[0].normal;
        transform.position -= transform.forward * profundidadClavado;
    }
}