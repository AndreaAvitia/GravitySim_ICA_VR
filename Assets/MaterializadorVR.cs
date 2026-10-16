using UnityEngine;
using UltimateXR.Manipulation;

[RequireComponent(typeof(UxrGrabbableObject))]
public class MaterializadorVR : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("El componente Renderer del objeto (arrástralo aquí)")]
    public Renderer rendererObjeto;

    [Tooltip("Los materiales originales del dardo en su orden exacto")]
    public Material[] materialesReales;

    [Header("Generador Infinito")]
    [Tooltip("El Prefab de este mismo objeto (para respawnear)")]
    public GameObject prefabHolograma;

    [Header("Animación del Holograma")]
    public bool animarHolograma = true;
    public float velocidadRotacion = 45f;
    public float amplitudFlote = 0.05f;
    public float velocidadFlote = 2f;

    private UxrGrabbableObject grabbable;
    private Rigidbody rb;
    private bool yaMaterializado = false;

    private Vector3 posicionOriginal;
    private Quaternion rotacionOriginal;
    private float tiempoSpawn;

    void Start()
    {
        grabbable = GetComponent<UxrGrabbableObject>();
        rb = GetComponent<Rigidbody>(); // Atrapamos el Rigidbody

        posicionOriginal = transform.position;
        rotacionOriginal = transform.rotation;
        tiempoSpawn = Time.time;

        // PROTECCIÓN: Lo hacemos intocable mientras sea un holograma para que nada lo mueva
        if (rb != null) rb.isKinematic = true;
    }

    void OnEnable()
    {
        if (UxrGrabManager.Instance != null)
            UxrGrabManager.Instance.ObjectGrabbed += OnObjectGrabbed;
    }

    void OnDisable()
    {
        if (UxrGrabManager.Instance != null)
            UxrGrabManager.Instance.ObjectGrabbed -= OnObjectGrabbed;
    }

    void Update()
    {
        if (!yaMaterializado && animarHolograma)
        {
            float tiempoTranscurrido = Time.time - tiempoSpawn;

            // ANCLAJE DE ROTACIÓN: Se calcula en absoluto desde su rotación original. Imposible que se enchueque.
            float anguloGiro = velocidadRotacion * tiempoTranscurrido;
            transform.rotation = rotacionOriginal * Quaternion.Euler(0, anguloGiro, 0);

            // ANCLAJE DE POSICIÓN: Se bloquean la X y la Z para que nunca se deslice por la mesa.
            float nuevoY = posicionOriginal.y + (Mathf.Sin(tiempoTranscurrido * velocidadFlote) * amplitudFlote);
            transform.position = new Vector3(posicionOriginal.x, nuevoY, posicionOriginal.z);
        }
    }

    private void OnObjectGrabbed(object sender, UxrManipulationEventArgs e)
    {
        if (!yaMaterializado && e.GrabbableObject == grabbable)
        {
            yaMaterializado = true;

            if (rendererObjeto != null && materialesReales.Length > 0)
            {
                rendererObjeto.materials = materialesReales;
            }

            if (prefabHolograma != null)
            {
                Instantiate(prefabHolograma, posicionOriginal, rotacionOriginal);
            }

            Destroy(this);
        }
    }
}