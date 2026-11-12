using UnityEngine;
using TMPro;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Haptics;

public class GameFeelManager : MonoBehaviour
{
    public static GameFeelManager Instance { get; private set; }

    [Header("Marcadores UI (TextMeshPro)")]
    public TMP_Text textoPuntosBasket;
    public TMP_Text textoPuntosDardos;
    public TMP_Text textoUltimoTiroDardo;

    [Header("Efectos Visuales y Sonoros")]
    public AudioSource audioSourceEfectos;
    public AudioClip sonidoCanasta;
    public AudioClip sonidoDardoClavado;
    public AudioClip sonidoBullseye;

    private int _puntosBasket = 0;
    private int _puntosDardos = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ActualizarUI();
    }

    public void RegistrarCanasta(int puntos, Vector3 posicionAro, ParticleSystem particulasAro = null)
    {
        _puntosBasket += puntos;
        ActualizarUI();

        if (particulasAro != null) particulasAro.Play();
        ReproducirSonidoEnPunto(sonidoCanasta, posicionAro);
        VibrarControles(0.7f, 0.25f);
    }

    public void RegistrarDardo(int puntos, Vector3 posicionImpacto, bool esCentro)
    {
        _puntosDardos += puntos;
        if (textoUltimoTiroDardo != null)
        {
            textoUltimoTiroDardo.text = esCentro ? $"¡BULLSEYE! +{puntos}" : $"+{puntos} pts";
        }

        ActualizarUI();

        AudioClip clip = (esCentro && sonidoBullseye != null) ? sonidoBullseye : sonidoDardoClavado;
        ReproducirSonidoEnPunto(clip, posicionImpacto);
        VibrarControles(esCentro ? 0.9f : 0.4f, esCentro ? 0.3f : 0.1f);
    }

    public void ReiniciarPuntajes()
    {
        _puntosBasket = 0;
        _puntosDardos = 0;
        if (textoUltimoTiroDardo != null) textoUltimoTiroDardo.text = "";
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (textoPuntosBasket != null)
            textoPuntosBasket.text = $"Básquet: {_puntosBasket}";

        if (textoPuntosDardos != null)
            textoPuntosDardos.text = $"Dardos: {_puntosDardos}";
    }

    private void ReproducirSonidoEnPunto(AudioClip clip, Vector3 posicion)
    {
        if (clip == null) return;

        if (audioSourceEfectos != null)
        {
            audioSourceEfectos.transform.position = posicion;
            audioSourceEfectos.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, posicion);
        }
    }

    private void VibrarControles(float amplitud, float duracion)
    {
        if (UxrAvatar.LocalAvatar != null && UxrAvatar.LocalAvatar.ControllerInput != null)
        {
            UxrAvatar.LocalAvatar.ControllerInput.SendHapticFeedback(
                UxrHandSide.Left, UxrHapticClipType.Click, amplitud, duracion);
            UxrAvatar.LocalAvatar.ControllerInput.SendHapticFeedback(
                UxrHandSide.Right, UxrHapticClipType.Click, amplitud, duracion);
        }
    }
}