using UnityEngine;

public class GeneradorDeGranoUnico : MonoBehaviour
{
    public GameObject prefabGrano;
    public Transform puntoSpawn;
    public float fuerzaInicial = 2f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            GenerarGrano();
        }
    }

    void GenerarGrano()
    {
        if (prefabGrano == null || puntoSpawn == null) return;

        GameObject nuevoGrano = Instantiate(prefabGrano, puntoSpawn.position, Quaternion.identity);

        Rigidbody rb = nuevoGrano.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(Vector3.down * fuerzaInicial, ForceMode.Impulse);
        }

        Debug.Log("Grano generado en: " + gameObject.name);
    }
}