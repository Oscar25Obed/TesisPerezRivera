using System;
using TMPro;
using UnityEngine;

namespace TesisPerezRivera.Geometria
{
    // Silueta transparente con el tamano objetivo. Su transform (posicion, rotacion y
    // escala) es exactamente donde queda la forma al encajar. Necesita un collider
    // trigger que marque la zona donde se suelta la forma.
    public class SiluetaForma : MonoBehaviour
    {
        [SerializeField] TipoForma tipo;
        [Tooltip("Diferencia de tamano aceptada (0.12 = 12% mas grande o mas chica).")]
        [SerializeField, Range(0.02f, 0.3f)] float tolerancia = 0.12f;
        [SerializeField] Renderer contorno;
        [SerializeField] TextMeshPro pista;

        static readonly Color ColorPista = new Color(1f, 0.85f, 0.3f);
        static readonly Color ColorCorrecto = new Color(0.3f, 1f, 0.5f);
        static readonly Color ColorError = new Color(1f, 0.4f, 0.35f);

        FormaEscalable ocupante;
        FormaEscalable cercana;
        float ultimaVez;

        public event Action<SiluetaForma> Completada;
        public bool Completa => ocupante != null;
        public TipoForma Tipo => tipo;

        public void Configurar(TipoForma nuevoTipo, Renderer nuevoContorno, TextMeshPro nuevaPista)
        {
            tipo = nuevoTipo;
            contorno = nuevoContorno;
            pista = nuevaPista;
        }

        public void Reiniciar()
        {
            ocupante = null;
            cercana = null;
            if (contorno != null) contorno.enabled = true;
        }

        void OnTriggerStay(Collider otro)
        {
            if (Completa || otro.attachedRigidbody == null) return;

            var forma = otro.attachedRigidbody.GetComponent<FormaEscalable>();
            if (forma == null || forma.Colocada) return;

            cercana = forma;
            ultimaVez = Time.time;

            if (forma.Tipo == tipo && !forma.Agarrado && TamanoCorrecto(forma))
                Encajar(forma);
        }

        float Proporcion(FormaEscalable forma) => forma.transform.lossyScale.x / transform.lossyScale.x;

        bool TamanoCorrecto(FormaEscalable forma) => Mathf.Abs(Proporcion(forma) - 1f) <= tolerancia;

        void Encajar(FormaEscalable forma)
        {
            ocupante = forma;
            cercana = null;
            forma.Colocar(transform);
            if (contorno != null) contorno.enabled = false;
            Completada?.Invoke(this);
        }

        void Update()
        {
            if (cercana != null && Time.time - ultimaVez > 0.2f)
                cercana = null;

            if (pista != null)
                ActualizarPista();
        }

        void ActualizarPista()
        {
            if (Completa)
            {
                Mostrar("¡Correcto!", ColorCorrecto);
            }
            else if (cercana == null)
            {
                Mostrar("", ColorPista);
            }
            else if (cercana.Tipo != tipo)
            {
                Mostrar("Esta forma\nno va aquí", ColorError);
            }
            else
            {
                float proporcion = Proporcion(cercana);
                string tamano = $"<size=70%>Tamaño: {proporcion * 100f:0}%</size>";
                if (TamanoCorrecto(cercana))
                    Mostrar($"¡Ese tamaño!\nSuéltala\n{tamano}", ColorCorrecto);
                else if (proporcion > 1f)
                    Mostrar($"Hazla más\npequeña\n{tamano}", ColorPista);
                else
                    Mostrar($"Hazla más\ngrande\n{tamano}", ColorPista);
            }
        }

        void Mostrar(string texto, Color color)
        {
            pista.text = texto;
            pista.color = color;
        }
    }
}
