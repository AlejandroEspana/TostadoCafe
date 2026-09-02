using UnityEngine;
using System.Collections;

public class BotonEncender : MonoBehaviour
{
    public Piladora piladora;
    public AudioSource audioBoton;

    private bool activado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (activado) return;
        activado = true;

        StartCoroutine(Activar());
    }

    IEnumerator Activar()
    {
        // 🔊 Sonido del botón
        if (audioBoton != null)
        {
            audioBoton.Play();
            yield return new WaitForSeconds(audioBoton.clip.length);
        }

        // ⚙️ Encender máquina
        piladora.EncenderMaquina();

        // Permite volver a usar el botón después de un momento
        yield return new WaitForSeconds(0.5f);
        activado = false;
    }
}