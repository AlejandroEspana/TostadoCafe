using UnityEngine;
using System.Collections;

public class SelectoraDeCafe : MonoBehaviour
{
    [Header("Estado")]
    public bool encendida = false;

    [Header("Salidas")]
    public Transform salidaGrande;
    public Transform salidaMediano;
    public Transform salidaPequeno;
    public Transform salidaDefectuoso;

    [Header("Movimiento")]
    public float velocidad = 5f;

    [Header("Audio")]
    public AudioSource audioBoton;     // sonido del botón (NO loop)
    public AudioSource audioMaquina;   // sonido máquina (LOOP)
    public AudioSource audioGranos;    // sonido granos (LOOP)

    private int granosActivos = 0;

    void Start()
    {
        // Seguridad por código
        if (audioMaquina != null) audioMaquina.loop = true;
        if (audioGranos != null) audioGranos.loop = true;
        if (audioBoton != null) audioBoton.loop = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Encender();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            Apagar();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!encendida) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        Transform destino = null;

        if (other.CompareTag("G_Grande"))
            destino = salidaGrande;
        else if (other.CompareTag("G_Mediano"))
            destino = salidaMediano;
        else if (other.CompareTag("G_Pequeño"))
            destino = salidaPequeno;
        else if (other.CompareTag("G_Defectuoso"))
            destino = salidaDefectuoso;

        if (destino != null)
        {
            Debug.Log("Grano detectado: " + other.tag);
            StartCoroutine(MoverGrano(other.gameObject, destino));
        }
    }

    IEnumerator MoverGrano(GameObject grano, Transform destino)
    {
        Rigidbody rb = grano.GetComponent<Rigidbody>();

        // 🔊 ACTIVAR sonido de granos
        granosActivos++;
        if (audioGranos != null && !audioGranos.isPlaying)
        {
            audioGranos.Play();
        }

        // Desactivar física
        rb.isKinematic = true;

        // Punto destino elevado para que caiga natural
        Vector3 objetivo = destino.position + new Vector3(
            Random.Range(-0.2f, 0.2f),
            1.5f,
            Random.Range(-0.2f, 0.2f)
        );

        // Movimiento hacia la salida
        while (Vector3.Distance(grano.transform.position, objetivo) > 0.05f)
        {
            grano.transform.position = Vector3.MoveTowards(
                grano.transform.position,
                objetivo,
                velocidad * Time.deltaTime
            );
            yield return null;
        }

        // Reactivar física
        rb.isKinematic = false;
        rb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);

        // Esperar a que termine de caer
        yield return new WaitForSeconds(1f);

        // 🔊 CONTROL DE SONIDO DE GRANOS
        granosActivos--;

        if (granosActivos <= 0)
        {
            granosActivos = 0;
            if (audioGranos != null)
                audioGranos.Stop();
        }
    }

    public void Encender()
    {
        if (encendida) return;

        encendida = true;
        Debug.Log("Máquina encendida");

        StartCoroutine(SonidoEncendido());
    }

    IEnumerator SonidoEncendido()
    {
        // 🔘 sonido botón
        if (audioBoton != null)
        {
            audioBoton.Play();
            yield return new WaitForSeconds(audioBoton.clip.length);
        }

        // 🔊 sonido máquina (loop)
        if (audioMaquina != null && !audioMaquina.isPlaying)
        {
            audioMaquina.Play();
        }
    }

    public void Apagar()
    {
        if (!encendida) return;

        encendida = false;
        Debug.Log("Máquina apagada");

        // 🔘 sonido botón
        if (audioBoton != null)
        {
            audioBoton.Play();
        }

        // 🔊 detener sonidos
        if (audioMaquina != null)
            audioMaquina.Stop();

        if (audioGranos != null)
            audioGranos.Stop();

        granosActivos = 0;
    }
}