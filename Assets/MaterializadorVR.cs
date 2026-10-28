using UnityEngine;
using UltimateXR.Manipulation;

[RequireComponent(typeof(UxrGrabbableObject))]
public class MaterializadorVR : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("El componente Renderer del objeto (arrástralo aquí)")]
    public Renderer rendererObjeto;

    [Tooltip("Los materiales originales en su orden exacto")]
    public Material[] materialesReales;

    [Header("Generador Infinito")]
    [Tooltip("El Prefab de este mismo objeto (para respawnear)")]
    public GameObject prefabHolograma;

    [Tooltip("Ajuste extra de altura sobre la plataforma si sientes que nace muy abajo.")]
    public float offsetAlturaExtra = 0.0f;

    [Header("Animación del Holograma")]
    public bool animarHolograma = true;
    public float velocidadRotacion = 45f;
    public float amplitudFlote = 0.03f;
    public float velocidadFlote = 2f;

    private UxrGrabbableObject grabbable;
    private Rigidbody rb;
    private bool yaMaterializado = false;
    private bool coordenadasGuardadas = false;

    private Transform padrePlataforma;
    private Vector3 posicionLocalOriginal;
    private Quaternion rotacionLocalOriginal;
    private Vector3 escalaLocalOriginal;
    private float tiempoSpawn;

    void Awake()
    {
        grabbable = GetComponent<UxrGrabbableObject>();
        rb = GetComponent<Rigidbody>();
        tiempoSpawn = Time.time;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Start()
    {
        // Si es el primer holograma puesto en la escena, guarda su lugar exacto arriba de la plataforma
        if (!coordenadasGuardadas)
        {
            padrePlataforma = transform.parent;
            posicionLocalOriginal = transform.localPosition + new Vector3(0f, offsetAlturaExtra, 0f);
            rotacionLocalOriginal = transform.localRotation;
            escalaLocalOriginal = transform.localScale;
            coordenadasGuardadas = true;
        }
    }

    public void InicializarClon(Transform padre, Vector3 posLocal, Quaternion rotLocal, Vector3 escalaLocal)
    {
        padrePlataforma = padre;
        posicionLocalOriginal = posLocal;
        rotacionLocalOriginal = rotLocal;
        escalaLocalOriginal = escalaLocal;
        coordenadasGuardadas = true;

        transform.SetParent(padrePlataforma, false);
        transform.localPosition = posicionLocalOriginal;
        transform.localRotation = rotacionLocalOriginal;
        transform.localScale = escalaLocalOriginal;
    }

    void OnEnable()
    {
        if (grabbable != null)
        {
            // Usamos Grabbing (antes de mover el objeto) para que UXR no corrompa la posición
            grabbable.Grabbing += OnGrabbing;
        }
    }

    void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.Grabbing -= OnGrabbing;
        }
    }

    void Update()
    {
        if (!yaMaterializado && animarHolograma && coordenadasGuardadas)
        {
            // Nos aseguramos de que siga siendo hijo de la plataforma mientras sea holograma
            if (padrePlataforma != null && transform.parent != padrePlataforma)
            {
                transform.SetParent(padrePlataforma, false);
            }

            float tiempoTranscurrido = Time.time - tiempoSpawn;

            float anguloGiro = velocidadRotacion * tiempoTranscurrido;
            transform.localRotation = rotacionLocalOriginal * Quaternion.Euler(0f, anguloGiro, 0f);

            float nuevoY = posicionLocalOriginal.y + (Mathf.Sin(tiempoTranscurrido * velocidadFlote) * amplitudFlote);
            transform.localPosition = new Vector3(posicionLocalOriginal.x, nuevoY, posicionLocalOriginal.z);
        }
    }

    private void OnGrabbing(object sender, UxrManipulationEventArgs e)
    {
        if (!yaMaterializado)
        {
            yaMaterializado = true;

            if (rendererObjeto != null && materialesReales.Length > 0)
            {
                rendererObjeto.materials = materialesReales;
            }

            if (prefabHolograma != null)
            {
                // Creamos el nuevo holograma y lo forzamos a las coordenadas locales exactas sobre la plataforma
                GameObject nuevoHolograma = Instantiate(prefabHolograma);
                MaterializadorVR matNuevo = nuevoHolograma.GetComponent<MaterializadorVR>();
                if (matNuevo != null)
                {
                    matNuevo.InicializarClon(padrePlataforma, posicionLocalOriginal, rotacionLocalOriginal, escalaLocalOriginal);
                }
            }

            // Liberamos este objeto para que el jugador se lo lleve en la mano
            transform.SetParent(null, true);
            Destroy(this);
        }
    }
}