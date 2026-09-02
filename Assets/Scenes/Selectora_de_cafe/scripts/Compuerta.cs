using UnityEngine;

public class Compuerta : MonoBehaviour
{
    public bool abierta = false;

    public float desplazamiento = 2f; // cuánto se mueve en X
    public float velocidad = 5f;

    private Vector3 posicionCerrada;
    private Vector3 posicionAbierta;

    void Start()
    {
        posicionCerrada = transform.position;
        posicionAbierta = posicionCerrada + new Vector3(desplazamiento, 0, 0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            Abrir();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Cerrar();
        }

        if (abierta)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                posicionAbierta,
                velocidad * Time.deltaTime
            );
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                posicionCerrada,
                velocidad * Time.deltaTime
            );
        }
    }

    public void Abrir()
    {
        abierta = true;
        Debug.Log("Compuerta ABIERTA");
    }

    public void Cerrar()
    {
        abierta = false;
        Debug.Log("Compuerta CERRADA");
    }
}