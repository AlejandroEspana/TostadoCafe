using UnityEngine;

public class BotonApagar : MonoBehaviour
{
    public Piladora piladora;
    public AudioSource audioBoton;

    private bool activado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (activado) return;
        activado = true;

        // 🔊 Sonido botón
        if (audioBoton != null)
        {
            audioBoton.Play();
        }

        // 🔴 Apagar máquina
        piladora.ApagarMaquina();

        Invoke(nameof(ResetBoton), 0.5f);
    }

    void ResetBoton()
    {
        activado = false;
    }
}