using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace TesisPerezRivera.Zonas
{
    // Configura los teleports de la escena con los componentes de XR Interaction Toolkit
    // y los deja guardados en la escena. Se puede ejecutar varias veces: reutiliza lo
    // que ya existe y no mueve los puntos de llegada que ya fueron creados.
    static class ConfiguradorTeleports
    {
        const string NombreRaiz = "Teleports";
        const string NombreLlegadaLobby = "Llegada_Lobby";

        static readonly (string pedestal, string sala)[] Pedestales =
        {
            ("PROP_Pedestal_01", "Geo"),
            ("PROP_Pedestal_02", "Fis"),
            ("PROP_Pedestal_03", "Cal"),
        };

        static readonly string[] Salas = { "Cal", "Fis", "Geo" };
        static readonly string[] Pisos = { "ENV_Floor", "Arq_Piso_Cal", "Arq_Piso_Fis", "Arq_Piso_Geo" };

        // Default = rayo normal, bit 31 = capa "Teleport" del arco de teletransporte.
        static readonly InteractionLayerMask CapasAnchor = (1 << 0) | (1 << 31);
        static readonly InteractionLayerMask CapasArea = 1 << 31;

        [MenuItem("Tools/Teleports/Configurar")]
        static void Configurar()
        {
            var escena = SceneManager.GetActiveScene();
            var transforms = escena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .ToList();
            Transform Buscar(string nombre) => transforms.FirstOrDefault(t => t.name == nombre);

            var informe = new StringBuilder();
            Undo.SetCurrentGroupName("Configurar teleports");
            int grupo = Undo.GetCurrentGroup();

            var raiz = Buscar(NombreRaiz);
            if (raiz == null)
            {
                raiz = new GameObject(NombreRaiz).transform;
                SceneManager.MoveGameObjectToScene(raiz.gameObject, escena);
                Undo.RegisterCreatedObjectUndo(raiz.gameObject, "Crear raiz de teleports");
            }

            var lobby = ObtenerLlegada(raiz, NombreLlegadaLobby, out bool lobbyNueva);
            if (lobbyNueva)
            {
                var origenXR = Object.FindFirstObjectByType<XROrigin>();
                if (origenXR != null)
                {
                    var frente = Vector3.ProjectOnPlane(origenXR.transform.forward, Vector3.up);
                    lobby.SetPositionAndRotation(origenXR.transform.position, Rotacion(frente));
                }
                else
                {
                    informe.AppendLine("- No se encontro el XR Origin: coloca 'Llegada_Lobby' a mano.");
                }
            }

            var llegadas = new Dictionary<string, Transform>();
            foreach (var sala in Salas)
            {
                var llegada = ObtenerLlegada(raiz, "Llegada_" + sala, out bool nueva);
                llegadas[sala] = llegada;
                if (!nueva) continue;

                var pad = Buscar("Pad_Llegada_" + sala);
                if (pad == null)
                {
                    informe.AppendLine($"- No se encontro 'Pad_Llegada_{sala}': coloca 'Llegada_{sala}' a mano.");
                    continue;
                }

                ColocarSobrePad(llegada, pad, Buscar("Arq_Piso_" + sala));
            }

            foreach (var (nombrePedestal, sala) in Pedestales)
            {
                var pedestal = Buscar(nombrePedestal);
                if (pedestal == null)
                    informe.AppendLine($"- No se encontro '{nombrePedestal}'.");
                else
                    ConfigurarAnchor(pedestal, llegadas[sala]);
            }

            foreach (var sala in Salas)
            {
                var portal = Buscar("Portal_Retorno_Hub_" + sala);
                if (portal == null)
                    informe.AppendLine($"- No se encontro 'Portal_Retorno_Hub_{sala}'.");
                else
                    ConfigurarAnchor(portal, lobby);
            }

            foreach (var nombrePiso in Pisos)
            {
                var piso = Buscar(nombrePiso);
                if (piso == null)
                    informe.AppendLine($"- No se encontro el piso '{nombrePiso}'.");
                else
                    ConfigurarArea(piso);
            }

            Undo.CollapseUndoOperations(grupo);
            EditorSceneManager.MarkSceneDirty(escena);
            Selection.activeTransform = raiz;

            string mensaje = informe.Length == 0
                ? "Teleports configurados. Guarda la escena (Ctrl+S)."
                : "Teleports configurados con avisos:\n" + informe + "\nGuarda la escena (Ctrl+S).";
            Debug.Log("ConfiguradorTeleports: " + mensaje);
            EditorUtility.DisplayDialog("Teleports", mensaje, "OK");
        }

        static Transform ObtenerLlegada(Transform raiz, string nombre, out bool nueva)
        {
            var existente = raiz.Find(nombre);
            nueva = existente == null;
            if (!nueva) return existente;

            var punto = new GameObject(nombre).transform;
            Undo.RegisterCreatedObjectUndo(punto.gameObject, "Crear " + nombre);
            punto.SetParent(raiz, false);
            return punto;
        }

        // Pone el punto sobre el pad, derecho y mirando hacia el centro de la sala.
        static void ColocarSobrePad(Transform llegada, Transform pad, Transform piso)
        {
            var posicion = pad.position;
            var renderer = pad.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var limites = renderer.bounds;
                float y = limites.size.y < 0.3f ? limites.max.y : limites.min.y;
                posicion = new Vector3(limites.center.x, y, limites.center.z);
            }

            var frente = Vector3.zero;
            var rendererPiso = piso != null ? piso.GetComponentInChildren<Renderer>() : null;
            if (rendererPiso != null)
                frente = Vector3.ProjectOnPlane(rendererPiso.bounds.center - posicion, Vector3.up);
            if (frente.sqrMagnitude < 0.01f)
                frente = Vector3.ProjectOnPlane(pad.forward, Vector3.up);
            if (frente.sqrMagnitude < 0.01f)
                frente = Vector3.ProjectOnPlane(pad.up, Vector3.up);

            llegada.SetPositionAndRotation(posicion, Rotacion(frente));
        }

        static Quaternion Rotacion(Vector3 frente)
        {
            if (frente.sqrMagnitude < 0.01f) frente = Vector3.forward;
            return Quaternion.LookRotation(frente.normalized, Vector3.up);
        }

        static void ConfigurarAnchor(Transform origen, Transform destino)
        {
            AsegurarColisionador(origen);

            var anchor = origen.GetComponent<TeleportationAnchor>();
            if (anchor == null)
                anchor = Undo.AddComponent<TeleportationAnchor>(origen.gameObject);

            Undo.RecordObject(anchor, "Configurar anchor");
            anchor.teleportAnchorTransform = destino;
            anchor.matchOrientation = MatchOrientation.TargetUpAndForward;
            anchor.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
            anchor.interactionLayers = CapasAnchor;
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
        }

        static void ConfigurarArea(Transform piso)
        {
            AsegurarColisionador(piso);

            var area = piso.GetComponent<TeleportationArea>();
            if (area == null)
                area = Undo.AddComponent<TeleportationArea>(piso.gameObject);

            Undo.RecordObject(area, "Configurar area");
            area.matchOrientation = MatchOrientation.WorldSpaceUp;
            area.interactionLayers = CapasArea;
            PrefabUtility.RecordPrefabInstancePropertyModifications(area);
        }

        // Collider con la forma real del modelo, para que el rayo no choque con espacio vacio.
        static void AsegurarColisionador(Transform objeto)
        {
            if (objeto.GetComponentInChildren<Collider>(true) != null) return;

            foreach (var filtro in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filtro.sharedMesh == null) continue;
                var colisionador = Undo.AddComponent<MeshCollider>(filtro.gameObject);
                colisionador.sharedMesh = filtro.sharedMesh;
            }
        }
    }
}
