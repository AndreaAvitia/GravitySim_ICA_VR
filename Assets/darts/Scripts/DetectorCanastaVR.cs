using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DetectorCanastaVR : MonoBehaviour
{
    public int puntosPorCanasta = 2;
    public ParticleSystem particulasFestejo;

    private float _tiempoUltimaCanasta;

    void OnTriggerEnter(Collider other)
    {
        // Evita contar doble el mismo balón
        if (Time.time - _tiempoUltimaCanasta < 0.5f) return;

        BalonVR balon = other.GetComponentInParent<BalonVR>();
        if (balon != null)
        {
            Rigidbody rbBalon = balon.GetComponent<Rigidbody>();

            // Verifica que el balón venga cayendo de arriba hacia abajo
            if (rbBalon != null && rbBalon.linearVelocity.y < -0.1f)
            {
                _tiempoUltimaCanasta = Time.time;

                if (GameFeelManager.Instance != null)
                {
                    GameFeelManager.Instance.RegistrarCanasta(puntosPorCanasta, transform.position, particulasFestejo);
                }
            }
        }
    }
}