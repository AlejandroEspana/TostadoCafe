using UnityEngine;

public class ControlTeclado : MonoBehaviour
{
    public Piladora piladora; // referencia a la máquina

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            piladora.EncenderMaquina();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            piladora.ApagarMaquina();
        }
    }
}