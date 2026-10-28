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
    private bool fueLanzado = false;

    [Header("Configuración de Impacto")]
    public string tagTablero = "TableroDardos";
    public float profundidadClavado = 0.02f;

    [Header("1. Velocidad según la Gravedad (Más lento a menor gravedad)")]
    [Tooltip("Velocidad muy lenta en baja gravedad (Luna / Cero = 2.2 m/s).")]
    public float velocidadBajaGravedadV5 = 2.2f;

    [Tooltip("Velocidad moderada en la Tierra (6.5 m/s, mucho más controlada que antes).")]
    public float velocidadTierraV5 = 6.5f;

    [Tooltip("Velocidad máxima si el slider personalizado sube hasta 19.62.")]
    public float velocidadAltaGravedadV5 = 9.0f;

    [Header("2. Forma de la Parábola (Menor amplitud)")]
    [Tooltip("Elevación mínima (0.03 = casi no sube el arco).")]
    [Range(0.0f, 0.2f)]
    public float elevacionArcoV5 = 0.03f;

    [Tooltip("Caída de la parábola en baja gravedad (0.35 = curva suave acorde a la baja velocidad).")]
    public float gravedadParabolaBajaV5 = 0.35f;

    [Tooltip("Caída de la parábola en la Tierra (2.4 = curva más cerrada y con menos amplitud).")]
    public float gravedadParabolaTierraV5 = 2.4f;

    [Header("3. Soltar sin lanzar / Piso")]
    public float umbralLanzamiento = 0.35f;
    public float factorEscalaTierraPiso = 0.85f;

    [Header("Optimización")]
    public float tiempoDeVida = 120f;

    private Vector3 _posAnteriorMano;
    private Vector3 _velocidadManoReal;

    // Variables de la fórmula P(t) = P0 + V0*t + 0.5*g*t^2
    private Vector3 _p0;
    private Vector3 _v0;
    private float _gravedadParabolaActual;
    private float _gravedadCaidaSimple;
    private float _velocidadBaseSegunGravedad;
    private float _aporteMaximoMano;
    private float _tiempoVuelo;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbableObject = GetComponent<UxrGrabbableObject>();
        dardoCollider = GetComponent<Collider>();

        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void OnEnable()
    {
        if (grabbableObject != null)
        {
            grabbableObject.Grabbed += OnGrabbed;
            grabbableObject.Released += OnReleased;
        }
    }

    void OnDisable()
    {
        if (grabbableObject != null)
        {
            grabbableObject.Grabbed -= OnGrabbed;
            grabbableObject.Released -= OnReleased;
        }
    }

    void Update()
    {
        if (estaAgarrado && Time.deltaTime > 0f)
        {
            Vector3 velInstantanea = (transform.position - _posAnteriorMano) / Time.deltaTime;
            _velocidadManoReal = Vector3.Lerp(_velocidadManoReal, velInstantanea, 0.5f);
            _posAnteriorMano = transform.position;
        }
    }

    private void OnGrabbed(object sender, UxrManipulationEventArgs e)
    {
        estaAgarrado = true;
        estaEnVuelo = false;
        estaClavado = false;
        fueLanzado = false;

        _posAnteriorMano = transform.position;
        _velocidadManoReal = Vector3.zero;

        dardoCollider.enabled = true;
        transform.SetParent(null);

        rb.isKinematic = false;
        rb.useGravity = false;

        CancelInvoke(nameof(Desaparecer));
        CancelInvoke(nameof(ActivarColisiones));

        rb.detectCollisions = false;
    }

    private void OnReleased(object sender, UxrManipulationEventArgs e)
    {
        if (UxrGrabManager.Instance != null && UxrGrabManager.Instance.IsBeingGrabbed(grabbableObject))
            return;

        estaAgarrado = false;
        estaEnVuelo = true;
        estaClavado = false;
        _tiempoVuelo = 0f;

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.angularVelocity = Vector3.zero;

        CalcularParametrosSegunGravedad(Physics.gravity.y);

        Vector3 velDetectada = rb.linearVelocity.magnitude > _velocidadManoReal.magnitude
            ? rb.linearVelocity
            : _velocidadManoReal;

        float rapidezMano = velDetectada.magnitude;

        if (rapidezMano > umbralLanzamiento)
        {
            fueLanzado = true;

            Vector3 dirPunta = -transform.forward;
            Vector3 dirMano = velDetectada.normalized;

            Vector3 dirCombinada = Vector3.Slerp(dirMano, dirPunta, 0.75f);

            // Tope de altura muy bajo para que el arco tenga poca amplitud
            dirCombinada.y = Mathf.Clamp(dirCombinada.y, -0.02f, 0.06f) + elevacionArcoV5;
            Vector3 dirFinal = dirCombinada.normalized;

            // Aporte de la mano reducido para que nunca salga disparado de más
            float extraMano = Mathf.Clamp(rapidezMano * 0.4f, 0f, _aporteMaximoMano);
            float rapidezFinal = _velocidadBaseSegunGravedad + extraMano;

            _p0 = transform.position;
            _v0 = dirFinal * rapidezFinal;
            rb.linearVelocity = _v0;
        }
        else
        {
            fueLanzado = false;
        }

        Invoke(nameof(ActivarColisiones), 0.08f);
        Invoke(nameof(Desaparecer), tiempoDeVida);
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

    private void CalcularParametrosSegunGravedad(float gravedadY)
    {
        float gAbs = Mathf.Abs(gravedadY);

        if (gAbs <= 9.81f)
        {
            // Mientras menor sea la gravedad, menor es la velocidad (t va de 0 a 1)
            float t = Mathf.InverseLerp(0f, 9.81f, gAbs);
            _velocidadBaseSegunGravedad = Mathf.Lerp(velocidadBajaGravedadV5, velocidadTierraV5, t);
            _gravedadParabolaActual = Mathf.Lerp(gravedadParabolaBajaV5, gravedadParabolaTierraV5, t);
            _aporteMaximoMano = Mathf.Lerp(0.4f, 1.4f, t);
        }
        else
        {
            // Rango personalizado superior a la Tierra (9.81 a 19.62)
            float tExtra = Mathf.InverseLerp(9.81f, 19.62f, gAbs);
            _velocidadBaseSegunGravedad = Mathf.Lerp(velocidadTierraV5, velocidadAltaGravedadV5, tExtra);
            _gravedadParabolaActual = Mathf.Lerp(gravedadParabolaTierraV5, gravedadParabolaTierraV5 * 1.6f, tExtra);
            _aporteMaximoMano = Mathf.Lerp(1.4f, 2.2f, tExtra);
        }

        // Gravedad para cuando solo se suelta sin lanzar o tras rebotar en el piso
        float tPiso = Mathf.InverseLerp(0f, 9.81f, gAbs);
        float factorPiso = Mathf.Lerp(0.20f, factorEscalaTierraPiso, tPiso);
        _gravedadCaidaSimple = gAbs * factorPiso;
    }

    void FixedUpdate()
    {
        if (rb.isKinematic || estaClavado || estaAgarrado) return;

        if (estaEnVuelo)
        {
            if (fueLanzado)
            {
                // --- FÓRMULA DE LA PARÁBOLA ---
                // P(t) = P0 + V0 * t + 0.5 * g * t^2
                // V(t) = V0 + g * t
                _tiempoVuelo += Time.fixedDeltaTime;
                float t = _tiempoVuelo;

                Vector3 vectorGravedad = Vector3.down * _gravedadParabolaActual;

                Vector3 velocidadInstantanea = _v0 + (vectorGravedad * t);
                rb.linearVelocity = velocidadInstantanea;

                if (velocidadInstantanea.sqrMagnitude > 0.05f)
                {
                    transform.forward = -velocidadInstantanea.normalized;
                }
            }
            else
            {
                // --- SOLTAR SIN LANZAR ---
                rb.linearVelocity += Vector3.down * (_gravedadCaidaSimple * Time.fixedDeltaTime);

                if (rb.linearVelocity.sqrMagnitude > 0.1f)
                {
                    transform.forward = -rb.linearVelocity.normalized;
                }
            }
        }
        else
        {
            rb.linearVelocity += Vector3.down * (_gravedadCaidaSimple * Time.fixedDeltaTime);
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
            fueLanzado = false;
        }
    }

    private void ClavarDardo(Collision collision)
    {
        estaClavado = true;
        estaEnVuelo = false;
        fueLanzado = false;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        dardoCollider.enabled = false;

        transform.SetParent(collision.transform);
        transform.forward = collision.contacts[0].normal;
        transform.position -= transform.forward * profundidadClavado;
    }
}