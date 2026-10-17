using UnityEngine;
using UltimateXR.Manipulation;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UxrGrabbableObject))]
public class BalonVR : MonoBehaviour
{
    private Rigidbody rb;
    private UxrGrabbableObject grabbableObject;

    private bool estaAgarrado = false;
    private bool estaEnVuelo = false;
    private bool aplicarEmpuje = false;

    [Header("Escala de Gravedad por Planeta (VR)")]
    [Tooltip("Gravedad calibrada en la Tierra (original 0.24).")]
    [Range(0.05f, 1.0f)]
    public float gravedadCalibradaTierra = 0.28f;

    [Tooltip("Marte intacto en 0.55 (aprobado).")]
    [Range(0.2f, 1.0f)]
    public float escalaGravedadMarte = 0.55f;

    [Tooltip("Luna en 0.95.")]
    [Range(0.2f, 1.5f)]
    public float gravedadCalibradaLuna = 0.95f;

    [Header("Caída en la Tierra")]
    [Tooltip("Gravedad extra SOLO cuando el balón ya va bajando (1 = sin extra, 1.3 = caída ligeramente más firme).")]
    [Range(1f, 3f)]
    public float multiplicadorCaidaTierra = 1.3f;

    [Tooltip("Elevación extra al lanzar en la Tierra (original 1.35). Más = el balón sube más y dura más en el aire.")]
    [Range(0f, 1.5f)]
    public float impulsoVerticalTierra = 1.15f;

    [Header("Optimización")]
    [Tooltip("Segundos antes de desaparecer después de soltarlo (120 = 2 minutos)")]
    public float tiempoDeVida = 120f;

    private float _gravedadVerticalEfectiva;
    private float _multiplicadorHorizontal = 1.0f;
    private float _multiplicadorVertical = 1.0f;
    private float _frenoHorizontal = 0.997f;

    // Parámetros que cambian según el planeta (los valores por defecto son los originales)
    private float _multiplicadorCaida = 1.0f;
    private float _asistenciaElevacion = 1.1f;
    private float _impulsoVerticalExtra = 1.35f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbableObject = GetComponent<UxrGrabbableObject>();
        rb.useGravity = false;
        rb.linearDamping = 0f;
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

    private void OnGrabbed(object sender, UxrManipulationEventArgs e)
    {
        estaAgarrado = true;
        estaEnVuelo = false;
        aplicarEmpuje = false;
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
        rb.isKinematic = false;
        rb.useGravity = false;
        aplicarEmpuje = true;

        ActualizarParametrosSegunSceneGravity(Physics.gravity.y);

        Invoke(nameof(ActivarColisiones), 0.08f);
        Invoke(nameof(Desaparecer), tiempoDeVida);
    }

    private void ActivarColisiones()
    {
        if (!estaAgarrado)
            rb.detectCollisions = true;
    }

    private void Desaparecer()
    {
        Destroy(gameObject);
    }

    private void ActualizarParametrosSegunSceneGravity(float gravedadY)
    {
        float gAbs = Mathf.Abs(gravedadY);

        if (gravedadY <= -9.8f) // TIERRA (cae más rápido)
        {
            _gravedadVerticalEfectiva = gAbs * gravedadCalibradaTierra;
            _multiplicadorHorizontal = 3.5f;
            _multiplicadorVertical = 2.65f;
            _frenoHorizontal = 0.9985f;

            _multiplicadorCaida = multiplicadorCaidaTierra; // más gravedad al bajar
            _asistenciaElevacion = 0f;                      // sin "flotar" en el punto más alto
            _impulsoVerticalExtra = impulsoVerticalTierra;  // menos elevación al lanzar
        }
        else if (gravedadY <= -3.7f) // MARTE (Intacto)
        {
            _gravedadVerticalEfectiva = gAbs * escalaGravedadMarte;
            _multiplicadorHorizontal = 2.6f;
            _multiplicadorVertical = 2.2f;
            _frenoHorizontal = 0.998f;

            _multiplicadorCaida = 1.0f;
            _asistenciaElevacion = 1.1f;
            _impulsoVerticalExtra = 1.35f;
        }
        else if (gravedadY <= -0.1f) // LUNA (Intacto)
        {
            _gravedadVerticalEfectiva = gAbs * gravedadCalibradaLuna;
            _multiplicadorHorizontal = 2.65f;
            _multiplicadorVertical = 2.15f;
            _frenoHorizontal = 0.998f;

            _multiplicadorCaida = 1.0f;
            _asistenciaElevacion = 1.1f;
            _impulsoVerticalExtra = 1.35f;
        }
        else // ESPACIO (0)
        {
            _gravedadVerticalEfectiva = 0f;
            _multiplicadorHorizontal = 2.0f;
            _multiplicadorVertical = 1.0f;
            _frenoHorizontal = 1.0f;

            _multiplicadorCaida = 1.0f;
            _asistenciaElevacion = 1.1f;
            _impulsoVerticalExtra = 1.35f;
        }
    }

    void FixedUpdate()
    {
        if (rb.isKinematic || estaAgarrado) return;

        if (aplicarEmpuje)
        {
            aplicarEmpuje = false;
            float velocidadDeMano = rb.linearVelocity.magnitude;

            if (velocidadDeMano > 0.8f)
            {
                Vector3 direccionTiro = rb.linearVelocity.normalized;
                Vector3 impulsoExtra = (direccionTiro * 0.85f) + (Vector3.up * _impulsoVerticalExtra);
                Vector3 vel = rb.linearVelocity + impulsoExtra;

                vel.x *= _multiplicadorHorizontal;
                vel.z *= _multiplicadorHorizontal;
                vel.y *= (_multiplicadorVertical * 0.6f);

                rb.linearVelocity = vel;
            }
        }

        // Gravedad (con extra solo cuando el balón ya va bajando)
        float g = _gravedadVerticalEfectiva;
        if (rb.linearVelocity.y < 0f)
            g *= _multiplicadorCaida;

        rb.linearVelocity += Vector3.down * (g * Time.fixedDeltaTime);

        if (estaEnVuelo)
        {
            // Asistencia de elevación (desactivada en la Tierra)
            if (_asistenciaElevacion > 0f
                && Mathf.Abs(rb.linearVelocity.y) < 1.0f
                && rb.linearVelocity.magnitude > 1.0f)
            {
                rb.linearVelocity += Vector3.up * (_asistenciaElevacion * Time.fixedDeltaTime);
            }

            Vector3 velH = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) * _frenoHorizontal;
            rb.linearVelocity = new Vector3(velH.x, rb.linearVelocity.y, velH.z);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (estaEnVuelo)
        {
            estaEnVuelo = false;
        }
    }
}