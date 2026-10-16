using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UltimateXR.Avatar;

public class PlanetPortal : MonoBehaviour
{
    // 1. Agregamos "Personalizado" a la lista de opciones
    public enum Planet { Tierra, Luna, Marte, Personalizado }

    [Header("Configuración del Destino")]
    [Tooltip("Elige a qué planeta lleva esta puerta")]
    public Planet destino;

    [Header("Nombres exactos de tus escenas")]
    public string escenaTierra = "Scene_Tierra";
    public string escenaLuna = "Scene_Luna";
    public string escenaMarte = "Scene_Marte";
    // 2. Nueva variable para el nombre de la escena de pruebas
    public string escenaPersonalizado = "Scene_Personalizado";

    [Header("Transición")]
    public float tiempoDeFade = 1.0f;

    private bool _cruzando = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_cruzando) return;

        // Que Unity nos diga el chisme exacto de qué acaba de chocar
        Debug.Log(" ALGO TOCÓ EL PORTAL: " + other.gameObject.name);
        Debug.Log(" SU RAÍZ ES: " + other.transform.root.name);

        // Buscamos al jugador de 3 formas distintas (A prueba de balas)
        bool esElJugador = false;

        if (other.transform.root.CompareTag("Player")) esElJugador = true; // Por etiqueta
        if (other.CompareTag("MainCamera")) esElJugador = true; // Por si lo que cruzó fue tu cabeza (el visor)
        if (other.GetComponentInParent<UxrAvatar>() != null) esElJugador = true; // Por componente de UltimateXR

        if (esElJugador)
        {
            Debug.Log(" ¡Jugador detectado! Viajando...");
            StartCoroutine(RutinaTransicion());
        }
    }

    private IEnumerator RutinaTransicion()
    {
        _cruzando = true;

        // Oscurecer la pantalla gradualmente (Fade) usando la cámara de UltimateXR
        if (UxrAvatar.LocalAvatar != null && UxrAvatar.LocalAvatar.CameraFade != null)
        {
            UxrAvatar.LocalAvatar.CameraFade.EnableFadeColor(Color.black, tiempoDeFade);
            yield return new WaitForSeconds(tiempoDeFade);
        }

        // 3. Determinar la escena a cargar según la opción elegida (incluyendo el nuevo destino)
        string escenaACargar = "";
        switch (destino)
        {
            case Planet.Tierra: escenaACargar = escenaTierra; break;
            case Planet.Luna: escenaACargar = escenaLuna; break;
            case Planet.Marte: escenaACargar = escenaMarte; break;
            case Planet.Personalizado: escenaACargar = escenaPersonalizado; break;
        }

        // Carga Asíncrona (Vital en VR)
        AsyncOperation operacion = SceneManager.LoadSceneAsync(escenaACargar);

        while (!operacion.isDone)
        {
            yield return null;
        }
    }

    // =========================================================
    // NUEVAS FUNCIONES PARA LOS BOTONES
    // =========================================================

    public void AsignarDestinoTierra()
    {
        destino = Planet.Tierra;
        Debug.Log("Destino actualizado a: TIERRA");
    }

    public void AsignarDestinoLuna()
    {
        destino = Planet.Luna;
        Debug.Log("Destino actualizado a: LUNA");
    }

    public void AsignarDestinoMarte()
    {
        destino = Planet.Marte;
        Debug.Log("Destino actualizado a: MARTE");
    }

    // 4. Nueva función para el cuarto botón de tu interfaz
    public void AsignarDestinoPersonalizado()
    {
        destino = Planet.Personalizado;
        Debug.Log("Destino actualizado a: AMBIENTE PERSONALIZADO");
    }
}