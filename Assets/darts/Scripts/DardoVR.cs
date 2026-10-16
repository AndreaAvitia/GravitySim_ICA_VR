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

    [Header("Falsificación de Gravedad (VR)")]
    [Tooltip("Escala de gravedad en Luna/Marte (0.3 = flota bastante para que no se sienta pesado el 1.62 o 3.72).")]
    [Range(0.1f, 0.8f)]
    public float factorEscalaBajaGravedad = 0.3f;

    [Tooltip("Escala de gravedad al lanzar en la Tierra (0.5 = mitad de 9.81 para que el tiro no se sienta pesado).")]
    [Range(0.2f, 0.8f)]
    public float factorEscalaTierraVuelo = 0.5f;

    [Tooltip("Escala de gravedad tras rebotar en el piso en la Tierra (0.85 = cae firme al pasto sin flotar).")]
    [Range(0.5f, 1.0f)]
    public float factorEscalaTierraPiso = 0.85f;

    [Header("Impulso de Lanzamiento")]
    public float multiplicadorBajaGravedad = 3.4f;
    public float multiplicadorAltaGravedad = 2.8f;
    public float umbralLanzamiento = 0.8f;

    [Header("Optimización")]
    [Tooltip("Segundos antes de desaparecer tras soltarlo (120 = 2 minutos).")]
    public float tiempoDeVida = 120f;

    private float _gravedadVueloEfectiva;
    private float _gravedadPisoEfectiva;
    private float _multiplicadorHorizontal;
    private float _multiplicadorVertical;
    private float _frenoHorizontal;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbableObject = GetComponent<UxrGrabbableObject>();
        dardoCollider = GetComponent<Collider>();

        // Mantenemos apagada la gravedad nativa de Unity en todo momento
        rb.useGravity = false;
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
            rb.detectCollisions = false;
        }
    }

    private void OnObjectReleased(object sender, UxrManipulationEventArgs e)
    {
        if (e.GrabbableObject == grabbableObject)
        {
            estaAgarrado = false;
            estaEnVuelo = true;
            estaClavado = false;
            rb.isKinematic = false;
            rb.useGravity = false;
            aplicarEmpuje = true;

            CalcularParametrosPorGravedad(Physics.gravity.y);

            Invoke(nameof(ActivarColisiones), 0.05f);
            Invoke(nameof(Desaparecer), tiempoDeVida);
        }
    }

    private void ActivarColisiones()
    {
        rb.detectCollisions = true;
    }

    private void Desaparecer()
    {
        Destroy(gameObject);
    }

    private void CalcularParametrosPorGravedad(float gravedadY)
    {
        float gAbs = Mathf.Abs(gravedadY);

        // t = 0 en Gravedad Cero | t ≈ 0.16 en Luna | t ≈ 0.38 en Marte | t = 1 en Tierra (9.81 o más)
        float t = Mathf.InverseLerp(0f, 9.81f, gAbs);

        // 1. Gravedad durante el vuelo: interpola entre el factor muy flotante (Luna/Marte) y el factor de tiro en Tierra
        float factorVuelo = Mathf.Lerp(factorEscalaBajaGravedad, factorEscalaTierraVuelo, t);
        _gravedadVueloEfectiva = gAbs * factorVuelo;

        // 2. Gravedad al tocar el piso/rebotar: en Luna/Marte sigue flotando (0.3), en Tierra cae firme (0.85)
        float factorPiso = Mathf.Lerp(factorEscalaBajaGravedad, factorEscalaTierraPiso, t);
        _gravedadPisoEfectiva = gAbs * factorPiso;

        // 3. Fuerza y resistencia proporcionales al slider (incluyendo valores personalizados)
        float tSliderCompleto = Mathf.InverseLerp(0f, 19.62f, gAbs);
        _multiplicadorHorizontal = Mathf.Lerp(multiplicadorBajaGravedad, multiplicadorAltaGravedad, tSliderCompleto);
        _multiplicadorVertical = Mathf.Lerp(1.1f, 0.8f, tSliderCompleto);
        _frenoHorizontal = Mathf.Lerp(0.999f, 0.994f, tSliderCompleto);
    }

    void FixedUpdate()
    {
        // Si es un holograma estático, está clavado o está en la mano, no aplicamos fuerzas
        if (rb.isKinematic || estaClavado || estaAgarrado) return;

        if (aplicarEmpuje)
        {
            aplicarEmpuje = false;
            float velocidadDeMano = rb.linearVelocity.magnitude;

            if (velocidadDeMano > umbralLanzamiento)
            {
                Vector3 impulsoExtra = Camera.main.transform.forward * 0.5f;
                Vector3 vel = rb.linearVelocity + impulsoExtra;

                vel.x *= _multiplicadorHorizontal;
                vel.z *= _multiplicadorHorizontal;
                vel.y *= _multiplicadorVertical;

                rb.linearVelocity = vel;
            }
        }

        if (estaEnVuelo)
        {
            // Caída falsificada durante el vuelo hacia la diana
            rb.linearVelocity += Vector3.down * (_gravedadVueloEfectiva * Time.fixedDeltaTime);

            Vector3 velH = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) * _frenoHorizontal;
            rb.linearVelocity = new Vector3(velH.x, rb.linearVelocity.y, velH.z);

            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                transform.forward = -rb.linearVelocity.normalized;
            }
        }
        else
        {
            // Cuando choca contra el piso o rebota:
            // En Tierra usa _gravedadPisoEfectiva (cae rápido), en Luna/Marte sigue flotando lento
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
            // Termina la orientación automática de vuelo, pero mantiene nuestra gravedad falsificada en FixedUpdate
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