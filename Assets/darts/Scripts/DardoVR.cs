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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbableObject = GetComponent<UxrGrabbableObject>();
        dardoCollider = GetComponent<Collider>();

        rb.useGravity = false;
        rb.linearDamping = 0f; // sin rozamiento del aire para no "frenar" el vuelo

        // A mayor velocidad, evita que el dardo atraviese el tablero
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

            rb.isKinematic = false;
            rb.useGravity = false;

            CancelInvoke(nameof(Desaparecer));
            CancelInvoke(nameof(ActivarColisiones));

            rb.detectCollisions = false;
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
            aplicarEmpuje = true;
            _tiempoVuelo = 0f;

            CalcularParametrosPorGravedad(Physics.gravity.y);

            Invoke(nameof(ActivarColisiones), 0.1f);
            Invoke(nameof(Desaparecer), tiempoDeVida);
        }
    }

    private void ActivarColisiones()
    {
        if (!estaAgarrado && !estaClavado)
            rb.detectCollisions = true;
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

        // Multiplicadores verticales muy bajos para mantener la línea de vuelo sumamente plana
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

            float velocidadDeMano = rb.linearVelocity.magnitude;

            if (velocidadDeMano > umbralLanzamiento)
            {
                Vector3 direccionMano = rb.linearVelocity.normalized;
                // Impulso frontal fuerte y elevación casi nula (0.01) para trayectoria plana
                Vector3 impulsoExtra = (direccionMano * 0.65f) + (Vector3.up * 0.01f);
                Vector3 vel = rb.linearVelocity + impulsoExtra;

                vel.x *= _multiplicadorHorizontal;
                vel.z *= _multiplicadorHorizontal;
                vel.y *= _multiplicadorVertical;

                rb.linearVelocity = vel;
            }
        }

        if (estaEnVuelo)
        {
            // Gravedad progresiva: casi nula al soltar y sube hasta el valor normal.
            // Así el primer tramo del vuelo es una línea recta y la caída llega al final.
            _tiempoVuelo += Time.fixedDeltaTime;
            float rampa = duracionRampaGravedad > 0f
                ? Mathf.Clamp01(_tiempoVuelo / duracionRampaGravedad)
                : 1f;
            float factorRampa = Mathf.Lerp(gravedadInicialFraccion, 1f, rampa);

            rb.linearVelocity += Vector3.down * (_gravedadVueloEfectiva * factorRampa * Time.fixedDeltaTime);

            Vector3 velH = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) * _frenoHorizontal;
            rb.linearVelocity = new Vector3(velH.x, rb.linearVelocity.y, velH.z);

            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                transform.forward = -rb.linearVelocity.normalized;
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

        if (collision.gameObject.CompareTag(tagTablero))
        {
            ClavarDardo(collision);
        }
        else
        {
            estaEnVuelo = false;
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