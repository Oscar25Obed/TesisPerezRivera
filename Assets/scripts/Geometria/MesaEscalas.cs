using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TesisPerezRivera.Geometria
{
    // Lleva la cuenta de las siluetas completadas en la mesa y permite reiniciarla.
    public class MesaEscalas : MonoBehaviour
    {
        [SerializeField] TextMeshPro marcador;
        [SerializeField] UnityEvent alCompletarTodo;

        SiluetaForma[] siluetas;
        FormaEscalable[] formas;

        void Awake()
        {
            siluetas = GetComponentsInChildren<SiluetaForma>();
            formas = GetComponentsInChildren<FormaEscalable>();
            foreach (var silueta in siluetas)
                silueta.Completada += _ => ActualizarMarcador();
        }

        void Start() => ActualizarMarcador();

        void LateUpdate()
        {
            if (Camera.main == null) return;

            var camara = Camera.main.transform.position;
            foreach (var texto in GetComponentsInChildren<TextMeshPro>())
                texto.transform.rotation = Quaternion.LookRotation(texto.transform.position - camara, Vector3.up);
        }

        // Puede conectarse a un boton para volver a empezar.
        public void Reiniciar()
        {
            foreach (var silueta in siluetas) silueta.Reiniciar();
            foreach (var forma in formas) forma.Reiniciar();
            ActualizarMarcador();
        }

        void ActualizarMarcador()
        {
            int completas = 0;
            foreach (var silueta in siluetas)
            {
                if (silueta.Completa) completas++;
            }

            bool terminado = completas == siluetas.Length && siluetas.Length > 0;
            if (marcador != null)
            {
                marcador.text = terminado
                    ? "¡Muy bien!\n<size=70%>Todas las formas encajan</size>"
                    : $"Ajusta el tamaño de cada forma\n<size=70%>Completadas: {completas}/{siluetas.Length}</size>";
            }

            if (terminado)
                alCompletarTodo?.Invoke();
        }
    }
}
