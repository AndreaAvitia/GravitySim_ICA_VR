using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UltimateXR.Avatar;

public class ReturnPortal : MonoBehaviour
{
    [Header("Configuración del Destino")]
    [Tooltip("El nombre exacto de la escena de tu Hub principal")]
    public string escenaHub = "GravitySimulator";

    [Header("Transición")]
    public float tiempoDeFade = 1.0f;

    private bool _cruzando = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_cruzando) return;

        // 1. Que Unity nos diga el chisme exacto de qué acaba de chocar
        Debug.Log(" ALGO TOCÓ EL PORTAL: " + other.gameObject.name);
        Debug.Log(" SU RAÍZ ES: " + other.transform.root.name);

        // 2. Buscamos al jugador de 3 formas distintas (A prueba de balas)
        bool esElJugador = false;

        if (other.transform.root.CompareTag("Player")) esElJugador = true; // Por etiqueta
        if (other.CompareTag("MainCamera")) esElJugador = true; // Por si lo que cruzó fue tu cabeza (el visor)
        if (other.GetComponentInParent<UxrAvatar>() != null) esElJugador = true; // Por componente de UltimateXR

        if (esElJugador)
        {
            Debug.Log(" ¡Jugador detectado! Viajando...");
            StartCoroutine(RutinaRegreso()); // Cambia esto a RutinaRegreso() si estás en el otro script
        }
    }

    private IEnumerator RutinaRegreso()
    {
        _cruzando = true;

        // 1. Fade a negro
        if (UxrAvatar.LocalAvatar != null && UxrAvatar.LocalAvatar.CameraFade != null)
        {
            UxrAvatar.LocalAvatar.CameraFade.EnableFadeColor(Color.black, tiempoDeFade);
            yield return new WaitForSeconds(tiempoDeFade);
        }

        // 2. Carga asíncrona de regreso al Hub
        AsyncOperation operacion = SceneManager.LoadSceneAsync(escenaHub);

        while (!operacion.isDone)
        {
            yield return null;
        }
    }
}