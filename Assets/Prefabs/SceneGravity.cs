using UnityEngine;
using UnityEngine.UI;
using UltimateXR.Manipulation;

public class SceneGravity : MonoBehaviour
{
    [Header("Configuración de Gravedad")]
    [Tooltip("Modifica el eje Y. Tierra: -9.81 | Marte: -3.72 | Luna: -1.62 | Espacio: 0")]
    public Vector3 gravedadPlaneta = new Vector3(0, -9.81f, 0);

    [Header("Componentes de Interfaz (UI)")]
    [Tooltip("Arrastra aquí el Slider de tu Canvas")]
    public Slider sliderGravedad;

    [Tooltip("Arrastra aquí el componente Text (Legacy) que mostrará el valor")]
    public Text textoGravedad;

    [Header("Físicas del Balón")]
    [Tooltip("Arrastra aquí el objeto basketBall_mdl de tu escena")]
    public UxrGrabbableObject balon;

    private float _multiplicadorVertical = 1.0f;
    private float _multiplicadorHorizontal = 1.0f;

    private void Start()
    {
        if (sliderGravedad != null)
        {
            sliderGravedad.minValue = 0f;
            sliderGravedad.maxValue = 19.62f;

            // El slider internamente sigue usando el valor en positivo para su barra
            sliderGravedad.value = Mathf.Abs(gravedadPlaneta.y);

            sliderGravedad.onValueChanged.AddListener(ActualizarGravedadPorSlider);
        }

        ActualizarFisicasYMultiplicadores(gravedadPlaneta.y);
    }

    public void ActualizarGravedadPorSlider(float valorSlider)
    {
        // Aquí convertimos el valor del slider al vector negativo real
        gravedadPlaneta.y = -valorSlider;
        ActualizarFisicasYMultiplicadores(gravedadPlaneta.y);
    }

    private void ActualizarFisicasYMultiplicadores(float gravedadY)
    {
        Physics.gravity = new Vector3(gravedadPlaneta.x, gravedadY, gravedadPlaneta.z);

        if (textoGravedad != null)
        {
            // EL CAMBIO ESTÁ AQUÍ: Quitamos Mathf.Abs() para que muestre el valor real (ej: -9.81 m/s²)
            textoGravedad.text = "Gravedad: " + gravedadY.ToString("F2") + " m/s²";
        }

        if (gravedadY <= -9.8f) // Tierra o más pesado
        {
            _multiplicadorVertical = 1.0f;
            _multiplicadorHorizontal = 1.0f;
        }
        else if (gravedadY <= -3.7f) // Marte
        {
            _multiplicadorVertical = 1.25f;
            _multiplicadorHorizontal = 1.4f;
        }
        else if (gravedadY <= -0.1f) // Luna / Baja gravedad
        {
            _multiplicadorVertical = 1.5f;
            _multiplicadorHorizontal = 1.8f;
        }
        else // Espacio (Gravedad 0)
        {
            _multiplicadorVertical = 1.0f;
            _multiplicadorHorizontal = 1.0f;
        }
    }

    private void OnEnable()
    {
        if (balon != null) balon.Released += AlSoltarBalon;
    }

    private void OnDisable()
    {
        if (balon != null) balon.Released -= AlSoltarBalon;
        if (sliderGravedad != null) sliderGravedad.onValueChanged.RemoveListener(ActualizarGravedadPorSlider);
    }

    private void AlSoltarBalon(object sender, UxrManipulationEventArgs e)
    {
        Rigidbody rb = balon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 velocidadActual = rb.linearVelocity;

            velocidadActual.y *= _multiplicadorVertical;
            velocidadActual.x *= _multiplicadorHorizontal;
            velocidadActual.z *= _multiplicadorHorizontal;

            rb.linearVelocity = velocidadActual;
        }
    }
}