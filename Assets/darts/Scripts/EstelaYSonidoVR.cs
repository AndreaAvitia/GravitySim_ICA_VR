using UnityEngine;
using UltimateXR.Manipulation;

[RequireComponent(typeof(UxrGrabbableObject))]
public class EstelaYSonidoVR : MonoBehaviour
{
    [Header("Estela de Trayectoria")]
    public TrailRenderer estela;
    [Tooltip("Si está activo, cambia el color de la estela según la gravedad del planeta actual.")]
    public bool cambiarColorPorGravedad = true;
    public Color colorTierra = new Color(0f, 0.8f, 1f, 0.8f); // Cian
    public Color colorMarte = new Color(1f, 0.4f, 0.1f, 0.8f); // Naranja
    public Color colorLuna = new Color(0.7f, 0.3f, 1f, 0.8f);  // Violeta

    [Header("Audio de Vuelo y Rebote")]
    public AudioSource audioSourcePropio;
    public AudioClip sonidoLanzarWhoosh;
    public AudioClip sonidoRebotePiso;

    private UxrGrabbableObject _grabbable;
    private Rigidbody _rb;

    void Awake()
    {
        _grabbable = GetComponent<UxrGrabbableObject>();
        _rb = GetComponent<Rigidbody>();

        if (estela == null) estela = GetComponent<TrailRenderer>();
        if (audioSourcePropio == null) audioSourcePropio = GetComponent<AudioSource>();

        if (estela != null) estela.emitting = false;
    }

    void OnEnable()
    {
        if (_grabbable != null)
        {
            _grabbable.Grabbed += AlAgarrar;
            _grabbable.Released += AlSoltar;
        }
    }

    void OnDisable()
    {
        if (_grabbable != null)
        {
            _grabbable.Grabbed -= AlAgarrar;
            _grabbable.Released -= AlSoltar;
        }
    }

    private void AlAgarrar(object sender, UxrManipulationEventArgs e)
    {
        if (estela != null)
        {
            estela.emitting = false;
            estela.Clear();
        }
    }

    private void AlSoltar(object sender, UxrManipulationEventArgs e)
    {
        if (estela != null)
        {
            if (cambiarColorPorGravedad) ActualizarColorSegunGravedad();
            estela.Clear();
            estela.emitting = true;
        }

        if (audioSourcePropio != null && sonidoLanzarWhoosh != null)
        {
            audioSourcePropio.pitch = Random.Range(0.92f, 1.08f);
            audioSourcePropio.PlayOneShot(sonidoLanzarWhoosh, 0.6f);
        }
    }

    private void ActualizarColorSegunGravedad()
    {
        float g = Physics.gravity.y;
        Color elegida = colorTierra;

        if (g > -2.0f) elegida = colorLuna;
        else if (g > -5.0f) elegida = colorMarte;

        estela.startColor = elegida;
        estela.endColor = new Color(elegida.r, elegida.g, elegida.b, 0f);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Si es un dardo y se clavó al tablero, apagamos la emisión pero dejamos visible el trazo
        if (collision.gameObject.CompareTag("TableroDardos") && estela != null)
        {
            estela.emitting = false;
            return;
        }

        // Sonido de rebote (balón en cancha/tablero o dardo que cae al suelo)
        if (audioSourcePropio != null && sonidoRebotePiso != null)
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