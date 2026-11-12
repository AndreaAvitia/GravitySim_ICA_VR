using UnityEngine;

public class DianaPuntajeVR : MonoBehaviour
{
    [Tooltip("Arrastra un Transform vacío colocado justo en el Bullseye (centro exacto de la diana). Si lo dejas vacío, usará el centro de este objeto.")]
    public Transform centroDiana;

    [Header("Radios de Puntuación (en metros)")]
    public float radioBullseye = 0.04f; // 100 pts
    public float radioAnilloMedio = 0.12f; // 50 pts
    public float radioAnilloExterior = 0.22f; // 25 pts

    void OnCollisionEnter(Collision collision)
    {
        DardoVR dardo = collision.gameObject.GetComponent<DardoVR>();
        if (dardo != null && collision.contactCount > 0)
        {
            Vector3 puntoImpacto = collision.contacts[0].point;
            Vector3 origenCentro = centroDiana != null ? centroDiana.position : transform.position;

            float distanciaAlCentro = Vector3.Distance(puntoImpacto, origenCentro);

            int puntosGanados = 10;
            bool esBullseye = false;

            if (distanciaAlCentro <= radioBullseye)
            {
                puntosGanados = 100;
                esBullseye = true;
            }
            else if (distanciaAlCentro <= radioAnilloMedio)
            {
                puntosGanados = 50;
            }
            else if (distanciaAlCentro <= radioAnilloExterior)
            {
                puntosGanados = 25;
            }

            if (GameFeelManager.Instance != null)
            {
                GameFeelManager.Instance.RegistrarDardo(puntosGanados, puntoImpacto, esBullseye);
            }
        }
    }

    // Dibuja los anillos en la vista Scene para que los calibres visualmente al tamaño de tu modelo 3D
    void OnDrawGizmosSelected()
    {
        Vector3 origen = centroDiana != null ? centroDiana.position : transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origen, radioBullseye);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origen, radioAnilloMedio);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origen, radioAnilloExterior);
    }
}