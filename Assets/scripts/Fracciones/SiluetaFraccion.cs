using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TesisPerezRivera.Fracciones
{
    // Silueta de una fraccion (de circulo o de barra) que se rellena con piezas.
    // Cada pieza que cabe se acomoda en el siguiente hueco libre, asi que vale
    // cualquier combinacion que sume la fraccion objetivo (3/4 = 1/2 + 1/4 = 1/4 + 1/4 + 1/4).
    // El pivote es el centro del circulo o el extremo izquierdo de la barra, igual
    // que en las piezas. Necesita un collider trigger que marque la zona de la mesa.
    public class SiluetaFraccion : MonoBehaviour
    {
        // Minimo comun multiplo de 1..6, 8, 10 y 12: todas esas piezas ocupan unidades enteras.
        const int Unidades = 120;

        [SerializeField] TipoPiezaFraccion tipo = TipoPiezaFraccion.Sector;
        [SerializeField, Min(1)] int numerador = 3;
        [SerializeField, Min(1)] int denominador = 4;
        [Tooltip("Solo barras: largo de la barra entera.")]
        [SerializeField] float largoEntero = 0.36f;
        [SerializeField] Renderer objetivo;
        [SerializeField] TextMeshPro etiqueta;

        static readonly Color ColorNormal = Color.white;
        static readonly Color ColorPista = new Color(1f, 0.85f, 0.3f);
        static readonly Color ColorCorrecto = new Color(0.3f, 1f, 0.5f);
        static readonly Color ColorError = new Color(1f, 0.45f, 0.4f);

        readonly List<PiezaFraccion> colocadas = new List<PiezaFraccion>();
        int llenas;
        PiezaFraccion cercana;
        float ultimaVez;

        readonly Dictionary<PiezaFraccion, UnityAction<SelectEnterEventArgs>> alAgarrar = new Dictionary<PiezaFraccion, UnityAction<SelectEnterEventArgs>>();

        // Se dispara cada vez que se pone o se saca una pieza.
        public event Action<SiluetaFraccion> Cambio;
        public bool Completa => llenas >= UnidadesObjetivo;
        int UnidadesObjetivo => Unidades * numerador / denominador;

        public void Configurar(TipoPiezaFraccion nuevoTipo, int nuevoNumerador, int nuevoDenominador, float largo, Renderer nuevoObjetivo, TextMeshPro nuevaEtiqueta)
        {
            tipo = nuevoTipo;
            numerador = nuevoNumerador;
            denominador = nuevoDenominador;
            largoEntero = largo;
            objetivo = nuevoObjetivo;
            etiqueta = nuevaEtiqueta;
        }

        // Las piezas las devuelve a su lugar MesaFracciones.
        public void Reiniciar()
        {
            foreach (var pieza in colocadas)
                DejarDeEscuchar(pieza);
            colocadas.Clear();
            llenas = 0;
            cercana = null;
            if (objetivo != null) objetivo.enabled = true;
        }

        static int UnidadesDe(PiezaFraccion pieza)
        {
            if (Unidades % pieza.Denominador != 0) return -1;
            return Unidades / pieza.Denominador * pieza.Numerador;
        }

        bool Cabe(PiezaFraccion pieza)
        {
            int unidades = UnidadesDe(pieza);
            return pieza.Tipo == tipo && unidades > 0 && llenas + unidades <= UnidadesObjetivo;
        }

        static bool Agarrada(PiezaFraccion pieza)
        {
            var agarre = pieza.GetComponent<XRGrabInteractable>();
            return agarre != null && agarre.isSelected;
        }

        void OnTriggerStay(Collider otro)
        {
            if (Completa || otro.attachedRigidbody == null) return;

            var pieza = otro.attachedRigidbody.GetComponent<PiezaFraccion>();
            if (pieza == null || pieza.Colocada) return;

            cercana = pieza;
            ultimaVez = Time.time;

            if (!Agarrada(pieza) && Cabe(pieza))
                Encajar(pieza);
        }

        void Encajar(PiezaFraccion pieza)
        {
            var cuerpo = pieza.GetComponent<Rigidbody>();
            if (cuerpo != null)
            {
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
                cuerpo.isKinematic = true;
            }

            // La pieza sigue siendo agarrable: al sacarla se corrige la silueta.
            var agarre = pieza.GetComponent<XRGrabInteractable>();
            if (agarre != null)
            {
                UnityAction<SelectEnterEventArgs> accion = _ => Retirar(pieza);
                alAgarrar[pieza] = accion;
                agarre.selectEntered.AddListener(accion);
            }

            pieza.Colocada = true;
            colocadas.Add(pieza);
            cercana = null;
            Reacomodar();
        }

        void Retirar(PiezaFraccion pieza)
        {
            if (!colocadas.Remove(pieza)) return;

            DejarDeEscuchar(pieza);
            pieza.Colocada = false;

            // Al soltarla, XRGrabInteractable le devuelve el estado cinematico que tenia al
            // agarrarla; se vuelve a activar la fisica para que caiga normalmente.
            var agarre = pieza.GetComponent<XRGrabInteractable>();
            var cuerpo = pieza.GetComponent<Rigidbody>();
            if (agarre != null && cuerpo != null)
            {
                UnityAction<SelectExitEventArgs> alSoltar = null;
                alSoltar = _ =>
                {
                    agarre.selectExited.RemoveListener(alSoltar);
                    if (!pieza.Colocada) cuerpo.isKinematic = false;
                };
                agarre.selectExited.AddListener(alSoltar);
            }

            Reacomodar();
        }

        void DejarDeEscuchar(PiezaFraccion pieza)
        {
            if (!alAgarrar.TryGetValue(pieza, out var accion)) return;

            var agarre = pieza.GetComponent<XRGrabInteractable>();
            if (agarre != null) agarre.selectEntered.RemoveListener(accion);
            alAgarrar.Remove(pieza);
        }

        // Coloca las piezas una tras otra desde el inicio, sin dejar huecos.
        // Sectores: se giran alrededor del centro (el angulo de la pieza crece de +X hacia +Z).
        // Barras: se desplazan a lo largo de X.
        void Reacomodar()
        {
            llenas = 0;
            foreach (var pieza in colocadas)
            {
                float avance = (float)llenas / Unidades;
                if (tipo == TipoPiezaFraccion.Barra)
                    pieza.transform.SetPositionAndRotation(transform.TransformPoint(Vector3.right * largoEntero * avance), transform.rotation);
                else
                    pieza.transform.SetPositionAndRotation(transform.position, transform.rotation * Quaternion.Euler(0f, -360f * avance, 0f));
                llenas += UnidadesDe(pieza);
            }

            if (objetivo != null) objetivo.enabled = !Completa;
            Cambio?.Invoke(this);
        }

        void Update()
        {
            if (cercana != null && Time.time - ultimaVez > 0.2f)
                cercana = null;

            if (etiqueta != null)
                ActualizarEtiqueta();
        }

        void ActualizarEtiqueta()
        {
            string titulo = $"<size=140%>{numerador}/{denominador}</size>";
            string suma = colocadas.Count > 0 ? string.Join(" + ", colocadas.Select(p => p.ToString())) : "";
            string falta = Formatear(UnidadesObjetivo - llenas);

            string pista;
            Color color = ColorPista;
            if (Completa)
            {
                suma += $" = {numerador}/{denominador}";
                pista = "¡Correcto!";
                color = ColorCorrecto;
            }
            else if (cercana != null && cercana.Tipo != tipo)
            {
                pista = tipo == TipoPiezaFraccion.Barra ? "Usa piezas de barra" : "Usa piezas de círculo";
                color = ColorError;
            }
            else if (cercana != null && !Cabe(cercana))
            {
                pista = $"{cercana} no cabe: falta {falta}";
                color = ColorError;
            }
            else if (cercana != null)
            {
                pista = "Cabe: suéltala";
                color = ColorCorrecto;
            }
            else
            {
                pista = llenas > 0 ? $"Falta {falta}" : "";
                color = ColorNormal;
            }

            etiqueta.text = $"{titulo}\n<size=75%>{suma}</size>\n<size=75%><color=#{ColorUtility.ToHtmlStringRGB(color)}>{pista}</color></size>";
        }

        static string Formatear(int unidades)
        {
            if (unidades <= 0) return "0";
            int divisor = Mcd(unidades, Unidades);
            return $"{unidades / divisor}/{Unidades / divisor}";
        }

        static int Mcd(int a, int b) => b == 0 ? a : Mcd(b, a % b);
    }
}
