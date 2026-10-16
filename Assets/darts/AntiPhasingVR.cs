using UnityEngine;

public class AntiPhasingVR : MonoBehaviour
{
    [Header("Configuración de Rescate")]
    [Tooltip("La capa (Layer) donde está tu piso de Marte")]
    public LayerMask capaDelPiso;

    [Tooltip("Si te hundes esta cantidad de metros bajo la arena, el script te rescata")]
    public float margenDeError = 0.5f;

    void Update()
    {
        // 1. Calculamos un punto a 1000 metros en el cielo, exactamente arriba de ti
        Vector3 origenEnElCielo = new Vector3(transform.position.x, 1000f, transform.position.z);

        // 2. Lanzamos un rayo láser hacia abajo buscando EXCLUSIVAMENTE la capa del piso
        if (Physics.Raycast(origenEnElCielo, Vector3.down, out RaycastHit choque, 2000f, capaDelPiso))
        {
            float alturaRealDelPiso = choque.point.y;

            // 3. Comparamos. Si estás por debajo de la altura real del piso (más el margen)
            if (transform.position.y < alturaRealDelPiso - margenDeError)
            {
                // ¡Te atrapamos! Te teletransportamos de regreso a la superficie
                transform.position = new Vector3(transform.position.x, alturaRealDelPiso, transform.position.z);
            }
        }
    }
}