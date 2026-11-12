using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TesisPerezRivera.Zonas
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class PanelInformativoZona : MonoBehaviour
    {
        [SerializeField] string titulo = "Zona de aprendizaje";
        [SerializeField, TextArea] string descripcion = "Descripcion de la zona.";
        [SerializeField] Vector3 offsetPanel = new Vector3(0f, 0.4f, 0f);
        [SerializeField] float tamanoFuente = 6f;
        [SerializeField] Vector2 tamanoPanel = new Vector2(2f, 1f);

        TextMeshPro texto;

        void Awake()
        {
            GetComponent<XRSimpleInteractable>().selectEntered.AddListener(_ => AlternarPanel());
            CrearTexto();
        }

        void CrearTexto()
        {
            var panelGO = new GameObject("Texto Informativo");
            texto = panelGO.AddComponent<TextMeshPro>();
            texto.text = $"{titulo}\n{descripcion}";
            texto.fontSize = tamanoFuente;
            texto.alignment = TextAlignmentOptions.Center;
            texto.enableWordWrapping = true;
            texto.rectTransform.sizeDelta = tamanoPanel;

            panelGO.transform.SetParent(transform, false);
            panelGO.transform.localPosition = offsetPanel;
            panelGO.SetActive(false);
        }

        void AlternarPanel()
        {
            texto.gameObject.SetActive(!texto.gameObject.activeSelf);
        }
    }
}
