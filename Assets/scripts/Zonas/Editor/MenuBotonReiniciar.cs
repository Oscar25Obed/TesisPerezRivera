using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TesisPerezRivera.Zonas
{
    static class MenuBotonReiniciar
    {
        const string CarpetaMateriales = "Assets/Lobby/Materiales";
        const float AltoPedestal = 0.9f;

        // Crea un pedestal con un boton rojo cerca de donde aparece el jugador en el lobby.
        [MenuItem("GameObject/Lobby/Botón reiniciar", false, 10)]
        static void Crear(MenuCommand comando)
        {
            var raiz = new GameObject("BotonReiniciar");
            Undo.RegisterCreatedObjectUndo(raiz, "Crear boton reiniciar");
            Ubicar(raiz.transform, comando.context as GameObject);

            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(raiz.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, AltoPedestal * 0.5f, 0f);
            pedestal.transform.localScale = new Vector3(0.25f, AltoPedestal * 0.5f, 0.25f);
            Object.DestroyImmediate(pedestal.GetComponent<Collider>());
            var cajaPedestal = pedestal.AddComponent<BoxCollider>();
            cajaPedestal.size = new Vector3(1f, 2f, 1f);
            pedestal.GetComponent<MeshRenderer>().sharedMaterial = ObtenerMaterial("MatPedestalBoton", new Color(0.2f, 0.22f, 0.26f), 0f);

            var boton = new GameObject("Boton");
            boton.transform.SetParent(raiz.transform, false);
            boton.transform.localPosition = new Vector3(0f, AltoPedestal, 0f);

            var capuchon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capuchon.name = "Capuchon";
            capuchon.transform.SetParent(boton.transform, false);
            capuchon.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            capuchon.transform.localScale = new Vector3(0.14f, 0.02f, 0.14f);
            Object.DestroyImmediate(capuchon.GetComponent<Collider>());
            capuchon.GetComponent<MeshRenderer>().sharedMaterial = ObtenerMaterial("MatBotonReiniciar", new Color(0.9f, 0.15f, 0.1f), 0.6f);

            // Zona de presion un poco mas alta que el capuchon para que el toque con la mano se detecte bien.
            var zona = boton.AddComponent<BoxCollider>();
            zona.center = new Vector3(0f, 0.03f, 0f);
            zona.size = new Vector3(0.15f, 0.06f, 0.15f);

            var etiqueta = new GameObject("Etiqueta").AddComponent<TextMeshPro>();
            etiqueta.transform.SetParent(raiz.transform, false);
            etiqueta.transform.localPosition = new Vector3(0f, AltoPedestal + 0.2f, 0f);
            etiqueta.text = "Reiniciar todo";
            etiqueta.fontSize = 0.8f;
            etiqueta.fontStyle = FontStyles.Bold;
            etiqueta.alignment = TextAlignmentOptions.Center;
            etiqueta.textWrappingMode = TextWrappingModes.NoWrap;
            etiqueta.rectTransform.sizeDelta = new Vector2(0.6f, 0.15f);

            boton.AddComponent<XRSimpleInteractable>();
            boton.AddComponent<BotonReiniciar>().Configurar(capuchon.transform, etiqueta);

            Selection.activeGameObject = raiz;
        }

        // Frente al punto de llegada del lobby (o al XR Origin), un poco a la derecha y mirando al jugador.
        static void Ubicar(Transform raiz, GameObject padre)
        {
            if (padre != null)
            {
                GameObjectUtility.SetParentAndAlign(raiz.gameObject, padre);
                return;
            }

            var llegada = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "Llegada_Lobby");
            Transform referencia = llegada;
            if (referencia == null)
            {
                var origenXR = Object.FindFirstObjectByType<XROrigin>();
                if (origenXR != null) referencia = origenXR.transform;
            }

            if (referencia == null)
            {
                if (SceneView.lastActiveSceneView != null)
                    raiz.position = SceneView.lastActiveSceneView.pivot;
                return;
            }

            var frente = Vector3.ProjectOnPlane(referencia.forward, Vector3.up).normalized;
            if (frente.sqrMagnitude < 0.01f) frente = Vector3.forward;
            var derecha = Vector3.Cross(Vector3.up, frente);
            var posicion = referencia.position + frente * 1.2f + derecha * 0.8f;

            // La etiqueta de TextMeshPro se lee de frente cuando su eje Z se aleja del jugador.
            var haciaAfuera = Vector3.ProjectOnPlane(posicion - referencia.position, Vector3.up);
            raiz.SetPositionAndRotation(posicion, Quaternion.LookRotation(haciaAfuera.normalized, Vector3.up));
        }

        static Material ObtenerMaterial(string nombre, Color color, float brillo)
        {
            CrearCarpeta(CarpetaMateriales);
            string ruta = $"{CarpetaMateriales}/{nombre}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material != null) return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.6f);
            if (brillo > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * brillo);
            }
            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }

        static void CrearCarpeta(string carpeta)
        {
            if (AssetDatabase.IsValidFolder(carpeta)) return;
            string padre = Path.GetDirectoryName(carpeta).Replace('\\', '/');
            CrearCarpeta(padre);
            AssetDatabase.CreateFolder(padre, Path.GetFileName(carpeta));
        }
    }
}
