using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TesisPerezRivera.Fracciones
{
    // Lleva la cuenta de las siluetas completadas, devuelve a su lugar las piezas
    // que se caen de la mesa y permite reiniciar la actividad.
    public class MesaFracciones : MonoBehaviour
    {
        [SerializeField] TextMeshPro marcador;
        [Tooltip("Si una pieza cae esta distancia por debajo de su posicion inicial, vuelve a su lugar.")]
        [SerializeField] float caidaParaReiniciar = 1f;
        [SerializeField] UnityEvent alCompletarTodo;

        SiluetaFraccion[] siluetas;
        PiezaFraccion[] piezas;
        Pose[] posesIniciales;
        bool terminadoAntes;

        void Awake()
        {
            siluetas = GetComponentsInChildren<SiluetaFraccion>();
            piezas = GetComponentsInChildren<PiezaFraccion>();
            posesIniciales = new Pose[piezas.Length];
            for (int i = 0; i < piezas.Length; i++)
                posesIniciales[i] = new Pose(piezas[i].transform.position, piezas[i].transform.rotation);

            foreach (var silueta in siluetas)
                silueta.Cambio += _ => ActualizarMarcador();
        }

        void Start() => ActualizarMarcador();

        void Update()
        {
            for (int i = 0; i < piezas.Length; i++)
            {
                var pieza = piezas[i];
                if (!pieza.Colocada && pieza.transform.position.y < posesIniciales[i].position.y - caidaParaReiniciar)
                    ReiniciarPieza(i);
            }
        }

        void LateUpdate()
        {
            if (Camera.main == null) return;

            var camara = Camera.main.transform.position;
            foreach (var silueta in siluetas)
            {
                foreach (var texto in silueta.GetComponentsInChildren<TextMeshPro>())
                    texto.transform.rotation = Quaternion.LookRotation(texto.transform.position - camara, Vector3.up);
            }
            if (marcador != null)
                marcador.transform.rotation = Quaternion.LookRotation(marcador.transform.position - camara, Vector3.up);
        }

        // Puede conectarse a un boton para volver a empezar.
        public void Reiniciar()
        {
            foreach (var silueta in siluetas) silueta.Reiniciar();
            for (int i = 0; i < piezas.Length; i++) ReiniciarPieza(i);
            ActualizarMarcador();
        }

        void ReiniciarPieza(int i)
        {
            var pieza = piezas[i];
            pieza.Colocada = false;

            var cuerpo = pieza.GetComponent<Rigidbody>();
            if (cuerpo != null)
            {
                cuerpo.isKinematic = false;
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }

            pieza.transform.SetPositionAndRotation(posesIniciales[i].position, posesIniciales[i].rotation);
        }

        void ActualizarMarcador()
        {
            int completas = 0;
            foreach (var silueta in siluetas)
            {
                if (silueta.Completa) completas++;
            }

            bool terminado = siluetas.Length > 0 && completas == siluetas.Length;
            bool recienTerminado = terminado && !terminadoAntes;
            terminadoAntes = terminado;
            if (marcador != null)
            {
                marcador.text = terminado
                    ? "¡Muy bien!\n<size=70%>Completaste todas las fracciones</size>"
                    : $"Rellena cada fracción con piezas\n<size=70%>Completadas: {completas}/{siluetas.Length}</size>";
            }

            if (recienTerminado)
                alCompletarTodo?.Invoke();
        }
    }
}
