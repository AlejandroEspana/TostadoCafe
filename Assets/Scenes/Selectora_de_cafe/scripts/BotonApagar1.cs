using UnityEngine;
using System.Collections;

public class BotonApagar1 : MonoBehaviour
{
    public SelectoraDeCafe selectora;

    private Vector3 posicionInicial;
    public float profundidad = 0.02f;
    public float velocidad = 10f;

    private void Start()
    {
        posicionInicial = transform.localPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<Rigidbody>())
        {
            selectora.Apagar();

            StopAllCoroutines();
            StartCoroutine(AnimarBoton());
        }
    }

    IEnumerator AnimarBoton()
    {
        Vector3 presionado = posicionInicial + new Vector3(0, -profundidad, 0);

        while (Vector3.Distance(transform.localPosition, presionado) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, presionado, velocidad * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        while (Vector3.Distance(transform.localPosition, posicionInicial) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, posicionInicial, velocidad * Time.deltaTime);
            yield return null;
        }
    }
}