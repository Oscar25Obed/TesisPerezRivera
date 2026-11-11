using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TesisPerezRivera.Zonas
{
    // Boton fisico que recarga la escena: deja todo como al abrir la aplicacion.
    // Se presiona con el rayo (gatillo) o tocandolo con la mano.
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class BotonReiniciar : MonoBehaviour
    {
        [SerializeField] Transform capuchon;
        [SerializeField] TextMeshPro etiqueta;
        [SerializeField] float hundimiento = 0.015f;
        [Tooltip("Tiempo que se ve el boton presionado antes de recargar.")]
        [SerializeField] float espera = 0.5f;

        static readonly Color BrilloNormal = new Color(1f, 0.2f, 0.15f) * 0.6f;
        static readonly Color BrilloEncima = new Color(1f, 0.3f, 0.2f) * 2f;

        Renderer render;
        bool reiniciando;

        public void Configurar(Transform nuevoCapuchon, TextMeshPro nuevaEtiqueta)
        {
            capuchon = nuevoCapuchon;
            etiqueta = nuevaEtiqueta;
        }

        void Awake()
        {
            render = capuchon != null ? capuchon.GetComponent<Renderer>() : null;
            var interactuable = GetComponent<XRSimpleInteractable>();
            interactuable.hoverEntered.AddListener(_ => CambiarBrillo(BrilloEncima));
            interactuable.hoverExited.AddListener(_ => CambiarBrillo(BrilloNormal));
            interactuable.selectEntered.AddListener(_ => Reiniciar());
            CambiarBrillo(BrilloNormal);
        }

        public void Reiniciar()
        {
            if (!reiniciando)
                StartCoroutine(RecargarEscena());
        }

        IEnumerator RecargarEscena()
        {
            reiniciando = true;
            if (capuchon != null) capuchon.localPosition -= Vector3.up * hundimiento;
            if (etiqueta != null) etiqueta.text = "Reiniciando...";

            yield return new WaitForSeconds(espera);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void CambiarBrillo(Color color)
        {
            if (render == null || reiniciando) return;
            render.material.EnableKeyword("_EMISSION");
            render.material.SetColor("_EmissionColor", color);
        }
    }
}
