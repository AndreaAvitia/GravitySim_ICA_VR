using System.Collections;
using UnityEngine;
using UltimateXR.Manipulation;

[RequireComponent(typeof(UxrGrabbableObject))]
public class EstelaYSonidoVR : MonoBehaviour
{
    [Header("Ajustes del Balón (Más grueso y largo)")]
    [Tooltip("Grosor de la estela para el balón en metros.")]
    public float grosorBalon = 0.18f;
    [Tooltip("Duración en segundos de la estela del balón (más tiempo = más larga).")]
    public float duracionEstelaBalon = 1.8f;

    [Header("Ajustes del Dardo (Más delgado y largo)")]
    [Tooltip("Grosor fino de la estela para el dardo en metros.")]
    public float grosorDardo = 0.008f;
    [Tooltip("Duración en segundos de la estela del dardo (más tiempo = más larga).")]
    public float duracionEstelaDardo = 1.5f;

    [Header("Colores según Gravedad")]
    public bool cambiarColorPorGravedad = true;
    public Color colorTierra = new Color(0f, 0.85f, 1f, 0.9f);
    public Color colorMarte = new Color(1f, 0.45f, 0.1f, 0.9f);
    public Color colorLuna = new Color(0.75f, 0.35f, 1f, 0.9f);

    [Header("Audio de Vuelo y Rebote")]
    public AudioSource audioSourcePropio;
    public AudioClip sonidoLanzarWhoosh;
    public AudioClip sonidoRebotePiso;

    private UxrGrabbableObject _grabbable;
    private Rigidbody _rb;
    private Collider _col;
    private TrailRenderer _estelaInterna;
    private Transform _puntoEmisorCentrado;
    private Coroutine _rutinaEncendido;
    private bool _enVueloActivo = false;

    private static Material _materialEstelaCompartido;

    void Awake()
    {
        _grabbable = GetComponent<UxrGrabbableObject>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponentInChildren<Collider>();

        if (audioSourcePropio == null) audioSourcePropio = GetComponent<AudioSource>();

        // Apagamos cualquier TrailRenderer previo en la raíz para evitar trazos dobles
        TrailRenderer trailViejo = GetComponent<TrailRenderer>();
        if (trailViejo != null)
        {
            trailViejo.emitting = false;
            trailViejo.enabled = false;
        }

        CrearEmisorCentrado();
    }

    private void CrearEmisorCentrado()
    {
        // Detecta automáticamente si este objeto es un dardo o un balón
        bool esDardo = GetComponent<DardoVR>() != null;
        float grosorElegido = esDardo ? grosorDardo : grosorBalon;
        float duracionElegida = esDardo ? duracionEstelaDardo : duracionEstelaBalon;

        GameObject objEmisor = new GameObject("EmisorEstelaCentrado");
        _puntoEmisorCentrado = objEmisor.transform;
        _puntoEmisorCentrado.SetParent(transform, false);

        if (_col != null)
        {
            _puntoEmisorCentrado.position = _col.bounds.center;
        }
        else
        {
            _puntoEmisorCentrado.localPosition = Vector3.zero;
        }

        _estelaInterna = objEmisor.AddComponent<TrailRenderer>();
        _estelaInterna.time = duracionElegida;
        _estelaInterna.minVertexDistance = 0.01f;
        _estelaInterna.autodestruct = false;
        _estelaInterna.emitting = false;
        _estelaInterna.alignment = LineAlignment.View;
        _estelaInterna.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _estelaInterna.receiveShadows = false;

        // En Unity, TrailRenderer ya usa unidades de mundo en su curva (no dividimos por lossyScale)
        AnimationCurve curva = new AnimationCurve();
        curva.AddKey(0f, grosorElegido);
        curva.AddKey(0.7f, grosorElegido * 0.5f); // Mantiene el cuerpo del trazo visible por más distancia
        curva.AddKey(1f, 0f);
        _estelaInterna.widthCurve = curva;
        _estelaInterna.widthMultiplier = 1f;

        if (_materialEstelaCompartido == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh != null) _materialEstelaCompartido = new Material(sh);
        }

        if (_materialEstelaCompartido != null)
        {
            _estelaInterna.sharedMaterial = _materialEstelaCompartido;
        }

        ActualizarColorSegunGravedad();
    }

    void OnEnable()
    {
        if (UxrGrabManager.Instance != null)
        {
            UxrGrabManager.Instance.ObjectGrabbed += AlAgarrarGlobal;
            UxrGrabManager.Instance.ObjectReleased += AlSoltarGlobal;
        }
    }

    void OnDisable()
    {
        if (UxrGrabManager.Instance != null)
        {
            UxrGrabManager.Instance.ObjectGrabbed -= AlAgarrarGlobal;
            UxrGrabManager.Instance.ObjectReleased -= AlSoltarGlobal;
        }
    }

    private void AlAgarrarGlobal(object sender, UxrManipulationEventArgs e)
    {
        if (e.GrabbableObject != _grabbable) return;

        _enVueloActivo = false;
        if (_rutinaEncendido != null) StopCoroutine(_rutinaEncendido);

        if (_estelaInterna != null)
        {
            _estelaInterna.emitting = false;
            _estelaInterna.Clear();
        }
    }

    private void AlSoltarGlobal(object sender, UxrManipulationEventArgs e)
    {
        if (e.GrabbableObject != _grabbable) return;
        if (UxrGrabManager.Instance != null && UxrGrabManager.Instance.IsBeingGrabbed(_grabbable)) return;

        if (_rutinaEncendido != null) StopCoroutine(_rutinaEncendido);
        _rutinaEncendido = StartCoroutine(EncenderEstelaEnVueloLimpio());

        if (audioSourcePropio != null && sonidoLanzarWhoosh != null)
        {
            audioSourcePropio.pitch = Random.Range(0.92f, 1.08f);
            audioSourcePropio.PlayOneShot(sonidoLanzarWhoosh, 0.6f);
        }
    }

    private IEnumerator EncenderEstelaEnVueloLimpio()
    {
        if (_estelaInterna == null) yield break;

        _estelaInterna.emitting = false;
        _estelaInterna.Clear();

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (_rb != null && _rb.linearVelocity.magnitude > 0.8f)
        {
            if (cambiarColorPorGravedad) ActualizarColorSegunGravedad();
            _estelaInterna.Clear();
            _estelaInterna.emitting = true;
            _enVueloActivo = true;
        }
    }

    void LateUpdate()
    {
        if (_enVueloActivo && _rb != null && _rb.linearVelocity.magnitude < 0.4f)
        {
            _enVueloActivo = false;
            if (_estelaInterna != null) _estelaInterna.emitting = false;
        }
    }

    private void ActualizarColorSegunGravedad()
    {
        if (_estelaInterna == null) return;

        float g = Physics.gravity.y;
        Color elegido = colorTierra;

        if (g > -2.0f) elegido = colorLuna;
        else if (g > -5.0f) elegido = colorMarte;

        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new GradientColorKey[] { new GradientColorKey(elegido, 0.0f), new GradientColorKey(elegido, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(elegido.a, 0.0f), new GradientAlphaKey(elegido.a * 0.6f, 0.6f), new GradientAlphaKey(0.0f, 1.0f) }
        );

        _estelaInterna.colorGradient = gradiente;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_enVueloActivo && _estelaInterna != null)
        {
            _enVueloActivo = false;
            _estelaInterna.emitting = false;
        }

        if (audioSourcePropio != null && sonidoRebotePiso != null && !collision.gameObject.CompareTag("TableroDardos"))
        {
            float fuerzaImpacto = collision.relativeVelocity.magnitude;
            if (fuerzaImpacto > 0.5f)
            {
                float volumen = Mathf.Clamp01(fuerzaImpacto / 8f);
                audioSourcePropio.pitch = Random.Range(0.9f, 1.1f);
                audioSourcePropio.PlayOneShot(sonidoRebotePiso, volumen);
            }
        }
    }
}