using UnityEngine;

namespace TesisPerezRivera.Fracciones
{
    public enum TipoPiezaFraccion
    {
        Sector,
        Barra,
        Cilindro,
    }

    // Datos de una pieza manipulable que representa la fraccion numerador/denominador.
    public class PiezaFraccion : MonoBehaviour
    {
        [SerializeField] TipoPiezaFraccion tipo;
        [SerializeField, Min(1)] int numerador = 1;
        [SerializeField, Min(1)] int denominador = 4;

        public TipoPiezaFraccion Tipo => tipo;
        public int Numerador => numerador;
        public int Denominador => denominador;
        public float Valor => (float)numerador / denominador;

        // La marca una SiluetaFraccion cuando la pieza queda encajada.
        public bool Colocada { get; set; }

        public void Configurar(TipoPiezaFraccion nuevoTipo, int nuevoNumerador, int nuevoDenominador)
        {
            tipo = nuevoTipo;
            numerador = nuevoNumerador;
            denominador = nuevoDenominador;
        }

        public override string ToString() => $"{numerador}/{denominador}";
    }
}
