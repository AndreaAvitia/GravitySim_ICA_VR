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
    [Tooltip("Subimos un poco la gravedad en Tierra (de 0.18 a 0.24).")]
    [Range(0.05f, 1.0f)]
    public float gravedadCalibradaTierra = 0.24f;

    [Tooltip("Marte intacto en 0.55 (aprobado).")]
    [Range(0.2f, 1.0f)]
    public float escalaGravedadMarte = 0.55f;

    [Tooltip("Subimos un poco la gravedad en Luna (de 0.80 a 0.95).")]
    [Range(0.2f, 1.5f)]
    public float gravedadCalibradaLuna = 0.95f;

    [Header("Optimización")]
    [Tooltip("Segundos antes de desaparecer después de soltarlo (120 = 2 minutos)")]
    public float tiempoDeVida = 120f;

    private float _gravedadVerticalEfectiva;
    private float _multiplicadorHorizontal = 1.0f;
    private float _multiplicadorVertical = 1.0f;
    private float _frenoHorizontal = 0.997f;

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

        if (gravedadY <= -9.8f) // TIERRA (Un poco más de gravedad que en 0.18)
        {
            _gravedadVerticalEfectiva = gAbs * gravedadCalibradaTierra;
            _multiplicadorHorizontal = 2.7f;
            _multiplicadorVertical = 2.45f;
            _frenoHorizontal = 0.9985f;
        }
        else if (gravedadY <= -3.7f) // MARTE (Intacto)
        {
            _gravedadVerticalEfectiva = gAbs * escalaGravedadMarte;
            _multiplicadorHorizontal = 2.6f;
            _multiplicadorVertical = 2.2f;
            _frenoHorizontal = 0.998f;
        }
        else if (gravedadY <= -0.1f) // LUNA (Un poco más de gravedad que en 0.80)
        {
            _gravedadVerticalEfectiva = gAbs * gravedadCalibradaLuna;
            _multiplicadorHorizontal = 2.65f;
            _multiplicadorVertical = 2.15f;
            _frenoHorizontal = 0.998f;
        }
        else // ESPACIO (0)
        {
            _gravedadVerticalEfectiva = 0f;
            _multiplicadorHorizontal = 2.0f;
            _multiplicadorVertical = 1.0f;
            _frenoHorizontal = 1.0f;
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
                Vector3 impulsoExtra = (direccionTiro * 0.65f) + (Vector3.up * 1.35f);
                Vector3 vel = rb.linearVelocity + impulsoExtra;

                vel.x *= _multiplicadorHorizontal;
                vel.z *= _multiplicadorHorizontal;
                vel.y *= (_multiplicadorVertical * 0.62f);

                rb.linearVelocity = vel;
            }
        }

        if (estaEnVuelo)
        {
            rb.linearVelocity += Vector3.down * (_gravedadVerticalEfectiva * Time.fixedDeltaTime);

            if (Mathf.Abs(rb.linearVelocity.y) < 1.0f && rb.linearVelocity.magnitude > 1.0f)
            {
                rb.linearVelocity += Vector3.up * (1.1f * Time.fixedDeltaTime);
            }

            Vector3 velH = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) * _frenoHorizontal;
            rb.linearVelocity = new Vector3(velH.x, rb.linearVelocity.y, velH.z);
        }
        else
        {
            rb.linearVelocity += Vector3.down * (_gravedadVerticalEfectiva * Time.fixedDeltaTime);
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