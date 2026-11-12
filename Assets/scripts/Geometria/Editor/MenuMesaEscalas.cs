using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace TesisPerezRivera.Geometria
{
    // Arma la mesa de escalas: una fila de siluetas con el tamano objetivo y otra fila
    // con las mismas formas mas grandes o mas chicas, listas para agarrar.
    static class MenuMesaEscalas
    {
        const string CarpetaMallas = "Assets/Geometria/Mallas";
        const string CarpetaMateriales = "Assets/Geometria/Materiales";
        const string NombrePedestal = "Est_Pedestal_Central_Geo";

        const float TamanoBase = 0.1f;
        const float Separacion = 0.2f;

        struct Definicion
        {
            public TipoForma tipo;
            public Vector3 proporcion;
            public float factor;
            public Color color;
        }

        // factor: tamano inicial de la forma respecto a su silueta.
        static readonly Definicion[] Formas =
        {
            new Definicion { tipo = TipoForma.Cubo, proporcion = Vector3.one, factor = 1.8f, color = new Color(0.25f, 0.55f, 1f) },
            new Definicion { tipo = TipoForma.Esfera, proporcion = Vector3.one, factor = 0.5f, color = new Color(1f, 0.35f, 0.35f) },
            new Definicion { tipo = TipoForma.Cilindro, proporcion = new Vector3(1f, 0.5f, 1f), factor = 1.6f, color = new Color(0.3f, 0.85f, 0.45f) },
            new Definicion { tipo = TipoForma.Cono, proporcion = Vector3.one, factor = 0.6f, color = new Color(1f, 0.85f, 0.25f) },
            new Definicion { tipo = TipoForma.Piramide, proporcion = Vector3.one, factor = 1.7f, color = new Color(0.7f, 0.4f, 1f) },
            new Definicion { tipo = TipoForma.Prisma, proporcion = Vector3.one, factor = 0.55f, color = new Color(1f, 0.55f, 0.2f) },
        };

        [MenuItem("GameObject/Geometría/Mesa de escalas", false, 10)]
        static void Crear(MenuCommand comando)
        {
            var raiz = new GameObject("MesaEscalas");
            Undo.RegisterCreatedObjectUndo(raiz, "Crear mesa de escalas");
            UbicarRaiz(raiz.transform, comando.context as GameObject);

            var materialSilueta = ObtenerMaterial("MatSilueta", new Color(0.4f, 0.85f, 1f), 0.2f, 0.8f);
            float inicioX = -(Formas.Length - 1) * Separacion * 0.5f;

            for (int i = 0; i < Formas.Length; i++)
            {
                var def = Formas[i];
                var malla = ObtenerMalla(def.tipo);
                float x = inicioX + i * Separacion;
                var escalaObjetivo = def.proporcion * TamanoBase;

                CrearRanura(raiz.transform, def, malla, materialSilueta, new Vector3(x, 0f, 0.12f), escalaObjetivo);
                CrearForma(raiz.transform, def, malla, new Vector3(x, 0f, -0.14f), escalaObjetivo * def.factor);
            }

            var marcador = CrearTexto("Marcador", raiz.transform, new Vector3(0f, 0.35f, 0.15f), 0.6f, Color.white);
            var mesa = raiz.AddComponent<MesaEscalas>();
            var serializado = new SerializedObject(mesa);
            serializado.FindProperty("marcador").objectReferenceValue = marcador;
            serializado.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = raiz;
        }

        // Sobre el pedestal central de la sala de Geometria si existe; si no, donde mira la vista de escena.
        static void UbicarRaiz(Transform raiz, GameObject padre)
        {
            if (padre != null)
            {
                GameObjectUtility.SetParentAndAlign(raiz.gameObject, padre);
                return;
            }

            var pedestal = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == NombrePedestal);

            var renderer = pedestal != null ? pedestal.GetComponentInChildren<Renderer>() : null;
            if (renderer != null)
            {
                var limites = renderer.bounds;
                raiz.position = new Vector3(limites.center.x, limites.max.y, limites.center.z);
                if (pedestal.GetComponentInChildren<Collider>() == null)
                {
                    foreach (var filtro in pedestal.GetComponentsInChildren<MeshFilter>())
                        Undo.AddComponent<MeshCollider>(filtro.gameObject).sharedMesh = filtro.sharedMesh;
                }
            }
            else if (SceneView.lastActiveSceneView != null)
            {
                raiz.position = SceneView.lastActiveSceneView.pivot;
            }
        }

        static void CrearRanura(Transform raiz, Definicion def, Mesh malla, Material material, Vector3 posicion, Vector3 escala)
        {
            var ranura = new GameObject($"Ranura_{def.tipo}").transform;
            ranura.SetParent(raiz, false);
            ranura.localPosition = posicion;

            var silueta = new GameObject("Silueta");
            silueta.transform.SetParent(ranura, false);
            silueta.transform.localPosition = new Vector3(0f, TamanoBase * 0.5f + 0.002f, 0f);
            silueta.transform.localScale = escala;
            silueta.AddComponent<MeshFilter>().sharedMesh = malla;
            var contorno = silueta.AddComponent<MeshRenderer>();
            contorno.sharedMaterial = material;
            contorno.shadowCastingMode = ShadowCastingMode.Off;

            // Zona donde se suelta la forma: un poco mas grande que la silueta.
            var zona = silueta.AddComponent<BoxCollider>();
            zona.isTrigger = true;
            zona.center = malla.bounds.center;
            zona.size = malla.bounds.size * 1.6f;

            var pista = CrearTexto("Pista", ranura, new Vector3(0f, TamanoBase + 0.08f, 0f), 0.35f, Color.white);
            silueta.AddComponent<SiluetaForma>().Configurar(def.tipo, contorno, pista);
        }

        static void CrearForma(Transform raiz, Definicion def, Mesh malla, Vector3 posicion, Vector3 escala)
        {
            var forma = new GameObject($"Forma_{def.tipo}");
            forma.transform.SetParent(raiz, false);
            forma.transform.localPosition = posicion + Vector3.up * (TamanoBase * def.factor * 0.5f + 0.01f);
            forma.transform.localScale = escala;
            forma.AddComponent<MeshFilter>().sharedMesh = malla;
            forma.AddComponent<MeshRenderer>().sharedMaterial = ObtenerMaterial($"MatForma_{def.tipo}", def.color, 1f, 0.25f);

            switch (def.tipo)
            {
                case TipoForma.Cubo: forma.AddComponent<BoxCollider>(); break;
                case TipoForma.Esfera: forma.AddComponent<SphereCollider>(); break;
                default:
                    var colisionador = forma.AddComponent<MeshCollider>();
                    colisionador.sharedMesh = malla;
                    colisionador.convex = true;
                    break;
            }

            var cuerpo = forma.AddComponent<Rigidbody>();
            cuerpo.mass = 0.3f;
            cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
            cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // Escalado con dos manos: separar o juntar las manos agranda o achica la forma.
            var transformador = forma.AddComponent<XRGeneralGrabTransformer>();
            transformador.allowTwoHandedScaling = true;

            var agarre = forma.AddComponent<XRGrabInteractable>();
            agarre.selectMode = UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Multiple;
            agarre.useDynamicAttach = true;
            agarre.throwOnDetach = false;

            forma.AddComponent<FormaEscalable>().Configurar(def.tipo);
        }

        static TextMeshPro CrearTexto(string nombre, Transform padre, Vector3 posicion, float tamano, Color color)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = posicion;
            var texto = objeto.AddComponent<TextMeshPro>();
            texto.text = "";
            texto.fontSize = tamano;
            texto.fontStyle = FontStyles.Bold;
            texto.color = color;
            texto.alignment = TextAlignmentOptions.Center;
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            texto.rectTransform.sizeDelta = new Vector2(0.8f, 0.3f);
            return texto;
        }

        static Mesh ObtenerMalla(TipoForma tipo)
        {
            switch (tipo)
            {
                case TipoForma.Cubo: return Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                case TipoForma.Esfera: return Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                case TipoForma.Cilindro: return Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
                case TipoForma.Cono: return GuardarMalla(MallaFormas.CrearCono());
                case TipoForma.Piramide: return GuardarMalla(MallaFormas.CrearPiramide());
                default: return GuardarMalla(MallaFormas.CrearPrisma());
            }
        }

        // Reutiliza el asset existente para no romper referencias de mesas ya creadas.
        static Mesh GuardarMalla(Mesh malla)
        {
            CrearCarpeta(CarpetaMallas);
            string ruta = $"{CarpetaMallas}/{malla.name}.asset";
            var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (existente == null)
            {
                AssetDatabase.CreateAsset(malla, ruta);
                return malla;
            }

            EditorUtility.CopySerialized(malla, existente);
            Object.DestroyImmediate(malla);
            return existente;
        }

        static Material ObtenerMaterial(string nombre, Color color, float opacidad, float brillo)
        {
            CrearCarpeta(CarpetaMateriales);
            string ruta = $"{CarpetaMateriales}/{nombre}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material != null) return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            color.a = opacidad;
            if (opacidad < 1f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.6f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * brillo);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
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
