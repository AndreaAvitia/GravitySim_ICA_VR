using UnityEngine;
using UltimateXR.Manipulation;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UxrGrabbableObject))]
public class BalonVR : MonoBehaviour
{
    private Rigidbody rb;
    private UxrGrabbableObject grabbableObject;

    private bool estaEnVuelo = false;
    private bool aplicarEmpuje = false;

    [Header("Falsificación de Gravedad (VR)")]
    [Tooltip("Porcentaje de gravedad vertical aplicada durante el vuelo inicial.")]
    [Range(0.2f, 1.0f)]
    public float factorEscalaGravedad = 0.55f;

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
        estaEnVuelo = false;
        aplicarEmpuje = false;
        rb.isKinematic = false;
        rb.useGravity = false;

        // Cancelamos la destrucción porque lo volvimos a agarrar
        CancelInvoke(nameof(Desaparecer));

        rb.detectCollisions = false;
    }

    private void OnReleased(object sender, UxrManipulationEventArgs e)
    {
        estaEnVuelo = true;
        rb.isKinematic = false;
        rb.useGravity = false;
        aplicarEmpuje = true;

        ActualizarParametrosSegunSceneGravity(Physics.gravity.y);

        Invoke(nameof(ActivarColisiones), 0.05f);

        // Iniciamos la cuenta regresiva de 2 minutos para destruirlo
        Invoke(nameof(Desaparecer), tiempoDeVida);
    }

    private void ActivarColisiones()
    {
        rb.detectCollisions = true;
    }

    private void Desaparecer()
    {
        // Se destruye para no saturar la memoria y evitar lag en VR
        Destroy(gameObject);
    }

    private void ActualizarParametrosSegunSceneGravity(float gravedadY)
    {
        float gAbs = Mathf.Abs(gravedadY);
        _gravedadVerticalEfectiva = gAbs * factorEscalaGravedad;

        if (gravedadY <= -9.8f)
        {
            _multiplicadorHorizontal = 2.2f;
            _multiplicadorVertical = 1.8f;
            _frenoHorizontal = 0.997f;
        }
        else if (gravedadY <= -3.7f)
        {
            _multiplicadorHorizontal = 2.6f;
            _multiplicadorVertical = 2.2f;
            _frenoHorizontal = 0.998f;
        }
        else if (gravedadY <= -0.1f)
        {
            _multiplicadorHorizontal = 3.0f;
            _multiplicadorVertical = 2.8f;
            _frenoHorizontal = 0.999f;
        }
        else
        {
            _multiplicadorHorizontal = 2.0f;
            _multiplicadorVertical = 1.0f;
            _frenoHorizontal = 1.0f;
        }
    }

    void FixedUpdate()
    {
        if (aplicarEmpuje)
        {
            aplicarEmpuje = false;
            float velocidadDeMano = rb.linearVelocity.magnitude;

            // UMBRAL: Si la mano iba rápido, inyecta fuerza. Si no, solo lo deja caer.
            if (velocidadDeMano > 0.8f)
            {
                Vector3 impulsoCamara = (Camera.main.transform.forward * 0.8f) + (Vector3.up * 1.4f);
                Vector3 vel = rb.linearVelocity + impulsoCamara;

                vel.x *= _multiplicadorHorizontal;
                vel.z *= _multiplicadorHorizontal;
                vel.y *= (_multiplicadorVertical * 0.6f); // Reducido para que no se vaya al techo

                rb.linearVelocity = vel;
            }
        }

        if (estaEnVuelo)
        {
            rb.linearVelocity += Vector3.down * (_gravedadVerticalEfectiva * Time.fixedDeltaTime);

            if (Mathf.Abs(rb.linearVelocity.y) < 1.0f && rb.linearVelocity.magnitude > 1.0f)
            {
                rb.linearVelocity += Vector3.up * (1.2f * Time.fixedDeltaTime);
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
            rb.useGravity = true;
        }
    }
}