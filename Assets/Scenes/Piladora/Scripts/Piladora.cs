using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piladora : MonoBehaviour
{
    [Header("Salidas")]
    public Transform salidaGrano;
    public Transform salidaCascarilla;

    [Header("Prefabs")]
    public GameObject prefabGranoPilado;
    public GameObject prefabCascarilla;

    [Header("Audio")]
    public AudioSource audioMaquina;

    [Header("Estado")]
    public bool maquinaEncendida = false;

    private List<GameObject> granos = new List<GameObject>();
    private Coroutine procesoActivo;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Grano"))
        {
            granos.Add(other.gameObject);
            Debug.Log("📦 Granos en cola: " + granos.Count);
        }
    }

    public void EncenderMaquina()
    {
        if (maquinaEncendida) return;

        maquinaEncendida = true;
        Debug.Log("🟢 Máquina ENCENDIDA");

        // 🔊 Sonido máquina
        if (audioMaquina != null && !audioMaquina.isPlaying)
        {
            audioMaquina.loop = true;
            audioMaquina.Play();
        }

        if (procesoActivo == null)
        {
            procesoActivo = StartCoroutine(Procesar());
        }
    }

    public void ApagarMaquina()
    {
        if (!maquinaEncendida) return;

        maquinaEncendida = false;
        Debug.Log("🔴 Máquina APAGADA");

        // 🔊 Detener sonido
        if (audioMaquina != null && audioMaquina.isPlaying)
        {
            audioMaquina.Stop();
        }

        if (procesoActivo != null)
        {
            StopCoroutine(procesoActivo);
            procesoActivo = null;
        }
    }

    IEnumerator Procesar()
    {
        while (maquinaEncendida)
        {
            if (granos.Count > 0)
            {
                GameObject grano = granos[0];
                granos.RemoveAt(0);

                // Evita errores antes de destruir
                if (grano != null)
                {
                    grano.GetComponent<Collider>().enabled = false;
                    yield return new WaitForSeconds(0.3f);
                    Destroy(grano);
                }

                // 🔥 Offset para efecto de acumulación
                Vector3 offsetGrano = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
                Vector3 offsetCascarilla = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));

                // Crear grano pilado
                Instantiate(prefabGranoPilado, salidaGrano.position + offsetGrano, Quaternion.identity);

                // Crear cascarilla
                Instantiate(prefabCascarilla, salidaCascarilla.position + offsetCascarilla, Quaternion.identity);
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        procesoActivo = null;
    }
}