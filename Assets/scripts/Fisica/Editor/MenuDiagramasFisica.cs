using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TesisPerezRivera.Fisica
{
    static class MenuDiagramasFisica
    {
        const string RutaMaterialBloque = "Assets/Fisica/Materiales/MatBloqueFuerzas.mat";

        [MenuItem("GameObject/Física/Plano inclinado", false, 10)]
        static void CrearPlanoInclinado(MenuCommand comando)
        {
            var objeto = new GameObject("PlanoInclinado");
            Colocar(objeto, comando, Vector3.zero);
            objeto.AddComponent<DiagramaPlanoInclinado>();
            Undo.RegisterCreatedObjectUndo(objeto, "Crear plano inclinado");
            Selection.activeGameObject = objeto;
        }

        [MenuItem("GameObject/Física/Bloque para fuerzas", false, 11)]
        static void CrearBloque(MenuCommand comando)
        {
            var objeto = CrearObjetoBloque();
            Colocar(objeto, comando, Vector3.up * 0.09f);
            Undo.RegisterCreatedObjectUndo(objeto, "Crear bloque para fuerzas");
            Selection.activeGameObject = objeto;
        }

        // Rampa y bloque juntos: el bloque queda a un lado, sobre el mismo piso.
        [MenuItem("GameObject/Física/Plano inclinado con bloque", false, 12)]
        static void CrearConjunto(MenuCommand comando)
        {
            var raiz = new GameObject("PlanoInclinadoConBloque");
            Colocar(raiz, comando, Vector3.zero);

            var plano = new GameObject("PlanoInclinado");
            plano.transform.SetParent(raiz.transform, false);
            plano.AddComponent<DiagramaPlanoInclinado>();

            var bloque = CrearObjetoBloque();
            bloque.transform.SetParent(raiz.transform, false);
            bloque.transform.localPosition = new Vector3(-0.35f, 0.09f, 0f);

            Undo.RegisterCreatedObjectUndo(raiz, "Crear plano inclinado con bloque");
            Selection.activeGameObject = raiz;
        }

        static GameObject CrearObjetoBloque()
        {
            var objeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objeto.name = "BloqueFuerzas";
            objeto.transform.localScale = Vector3.one * 0.18f;
            objeto.GetComponent<MeshRenderer>().sharedMaterial = ObtenerMaterialBloque();

            var cuerpo = objeto.AddComponent<Rigidbody>();
            cuerpo.mass = 2f;
            cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
            cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var agarre = objeto.AddComponent<XRGrabInteractable>();
            agarre.useDynamicAttach = true;
            agarre.throwOnDetach = false;

            objeto.AddComponent<BloqueFuerzas>();
            return objeto;
        }

        static void Colocar(GameObject objeto, MenuCommand comando, Vector3 desplazamiento)
        {
            GameObjectUtility.SetParentAndAlign(objeto, comando.context as GameObject);
            if (comando.context == null && SceneView.lastActiveSceneView != null)
                objeto.transform.position = SceneView.lastActiveSceneView.pivot + desplazamiento;
        }

        static Material ObtenerMaterialBloque()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialBloque);
            if (material != null) return material;

            CrearCarpeta(Path.GetDirectoryName(RutaMaterialBloque).Replace('\\', '/'));
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.55f, 0.6f, 0.72f));
            material.SetFloat("_Smoothness", 0.5f);
            AssetDatabase.CreateAsset(material, RutaMaterialBloque);
            return material;
        }

        static void CrearCarpeta(string carpeta)
        {
            if (AssetDatabase.IsValidFolder(carpeta)) return;
            CrearCarpeta(Path.GetDirectoryName(carpeta).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(carpeta).Replace('\\', '/'), Path.GetFileName(carpeta));
        }
    }
}
