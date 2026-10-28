using UnityEngine;
using UltimateXR.Manipulation;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UxrGrabbableObject))]
public class PlataformaMovilVR : MonoBehaviour
{
    private Rigidbody rb;
    private UxrGrabbableObject grabbable;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<UxrGrabbableObject>();

        rb.useGravity = false;
        rb.isKinematic = true;

        // MAGIA ANTIVOLTEOS: Congelamos los giros en X y Z. 
        // La plataforma SOLO podrá girar sobre su propio eje (Y), por lo que es físicamente 
        // IMPOSIBLE que la pongas de lado o de cabeza. Siempre estará plana.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.Grabbed += AlAgarrarPlataforma;
            grabbable.Released += AlSoltarPlataforma;
        }
    }

    void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.Grabbed -= AlAgarrarPlataforma;
            grabbable.Released -= AlSoltarPlataforma;
        }
    }

    private void AlAgarrarPlataforma(object sender, UxrManipulationEventArgs e)
    {
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void AlSoltarPlataforma(object sender, UxrManipulationEventArgs e)
    {
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}