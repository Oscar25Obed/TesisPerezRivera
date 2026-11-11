using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace TesisPerezRivera.Geometria
{
    public enum TipoForma
    {
        Cubo,
        Esfera,
        Cilindro,
        Cono,
        Piramide,
        Prisma,
    }

    // Forma solida que se agarra en VR y se agranda o achica con las dos manos.
    // Una SiluetaForma la encaja cuando su tamano coincide.
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public class FormaEscalable : MonoBehaviour
    {
        [SerializeField] TipoForma tipo;
        [Tooltip("Si cae esta distancia por debajo de su posicion inicial, vuelve a su lugar.")]
        [SerializeField] float caidaParaReiniciar = 1f;
        [SerializeField] float escalaMinima = 0.2f;
        [SerializeField] float escalaMaxima = 5f;

        static readonly Color BrilloCorrecto = new Color(0.2f, 1f, 0.4f) * 1.5f;

        Rigidbody cuerpo;
        XRGrabInteractable agarre;
        Renderer render;
        Vector3 posicionInicial, escalaInicial;
        Quaternion rotacionInicial;
        Color brilloOriginal;

        public TipoForma Tipo => tipo;
        public bool Agarrado => agarre.isSelected;
        public bool Colocada { get; private set; }

        public void Configurar(TipoForma nuevoTipo) => tipo = nuevoTipo;

        void Awake()
        {
            cuerpo = GetComponent<Rigidbody>();
            agarre = GetComponent<XRGrabInteractable>();
            render = GetComponentInChildren<Renderer>();
            posicionInicial = transform.position;
            rotacionInicial = transform.rotation;
            escalaInicial = transform.localScale;
            if (render != null && render.sharedMaterial.HasProperty("_EmissionColor"))
                brilloOriginal = render.sharedMaterial.GetColor("_EmissionColor");

            agarre.selectEntered.AddListener(_ => HabilitarEscalado());
        }

        // XRGrabInteractable puede agregar su propio transformador por defecto, asi que
        // se habilita el escalado con dos manos en todos al momento de agarrar.
        void HabilitarEscalado()
        {
            foreach (var transformador in GetComponents<XRGeneralGrabTransformer>())
            {
                transformador.allowTwoHandedScaling = true;
                transformador.minimumScaleRatio = escalaMinima;
                transformador.maximumScaleRatio = escalaMaxima;
            }
        }

        void Update()
        {
            if (!Colocada && !Agarrado && transform.position.y < posicionInicial.y - caidaParaReiniciar)
                Reiniciar();
        }

        public void Colocar(Transform destino)
        {
            Colocada = true;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            cuerpo.isKinematic = true;
            agarre.enabled = false;

            transform.SetPositionAndRotation(destino.position, destino.rotation);
            var escalaPadre = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(
                destino.lossyScale.x / escalaPadre.x,
                destino.lossyScale.y / escalaPadre.y,
                destino.lossyScale.z / escalaPadre.z);

            CambiarBrillo(BrilloCorrecto);
        }

        public void Reiniciar()
        {
            Colocada = false;
            agarre.enabled = true;
            cuerpo.isKinematic = false;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(posicionInicial, rotacionInicial);
            transform.localScale = escalaInicial;
            CambiarBrillo(brilloOriginal);
        }

        void CambiarBrillo(Color color)
        {
            if (render == null) return;
            render.material.EnableKeyword("_EMISSION");
            render.material.SetColor("_EmissionColor", color);
        }
    }
}
