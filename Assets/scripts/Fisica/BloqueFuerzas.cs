using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TesisPerezRivera.Fisica
{
    // Bloque que se puede agarrar en VR. Detecta cuando queda apoyado sobre la
    // pendiente de un DiagramaPlanoInclinado para que este dibuje las fuerzas.
    [RequireComponent(typeof(Rigidbody))]
    public class BloqueFuerzas : MonoBehaviour
    {
        public static readonly List<BloqueFuerzas> Todos = new List<BloqueFuerzas>();

        Rigidbody cuerpo;
        XRGrabInteractable agarre;
        BoxCollider caja;
        PhysicsMaterial materialFisico;

        public Rigidbody Cuerpo => cuerpo;
        public bool Agarrado => agarre != null && agarre.isSelected;
        public DiagramaPlanoInclinado PlanoActual { get; private set; }

        void Awake()
        {
            cuerpo = GetComponent<Rigidbody>();
            agarre = GetComponent<XRGrabInteractable>();
            caja = GetComponent<BoxCollider>();

            // Friccion 1 con combinacion "minimo": manda el coeficiente de la rampa.
            materialFisico = new PhysicsMaterial("BloqueFuerzas")
            {
                dynamicFriction = 1f,
                staticFriction = 1f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            foreach (var colisionador in GetComponentsInChildren<Collider>())
                colisionador.sharedMaterial = materialFisico;
        }

        void OnEnable() => Todos.Add(this);

        void OnDisable()
        {
            Todos.Remove(this);
            PlanoActual = null;
        }

        void OnDestroy()
        {
            if (materialFisico != null) Destroy(materialFisico);
        }

        public bool ApoyadoEn(DiagramaPlanoInclinado plano) => PlanoActual == plano && !Agarrado;

        // Distancia del centro del bloque a su cara en la direccion indicada (en mundo).
        public float MitadEn(Vector3 direccion)
        {
            if (caja == null) return 0.5f * transform.lossyScale.y;

            var mitad = Vector3.Scale(caja.size, transform.lossyScale) * 0.5f;
            return Mathf.Abs(Vector3.Dot(transform.right * mitad.x, direccion))
                 + Mathf.Abs(Vector3.Dot(transform.up * mitad.y, direccion))
                 + Mathf.Abs(Vector3.Dot(transform.forward * mitad.z, direccion));
        }

        public Vector3 Centro => caja != null ? transform.TransformPoint(caja.center) : transform.position;

        void OnCollisionStay(Collision colision)
        {
            var plano = colision.collider.GetComponentInParent<DiagramaPlanoInclinado>();
            if (plano == null) return;

            // Solo cuenta si toca la cara inclinada, no los costados de la rampa.
            var normal = plano.NormalPendienteMundo;
            for (int i = 0; i < colision.contactCount; i++)
            {
                if (Mathf.Abs(Vector3.Dot(colision.GetContact(i).normal, normal)) > 0.95f)
                {
                    PlanoActual = plano;
                    return;
                }
            }
        }

        void OnCollisionExit(Collision colision)
        {
            if (PlanoActual != null && colision.collider.GetComponentInParent<DiagramaPlanoInclinado>() == PlanoActual)
                PlanoActual = null;
        }
    }
}
