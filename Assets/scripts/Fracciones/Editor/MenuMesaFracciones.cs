using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TesisPerezRivera.Fracciones
{
    // Arma la mesa de fracciones: siluetas de circulo y de barra para rellenar y
    // montones de piezas sueltas (las mismas del generador de piezas).
    static class MenuMesaFracciones
    {
        const string CarpetaMallas = "Assets/Fracciones/Mallas";
        const string CarpetaMateriales = "Assets/Fracciones/Materiales";
        const float AltoSilueta = 0.006f;

        static readonly (int n, int d)[] ObjetivosCirculo = { (3, 4), (2, 3), (5, 8) };
        static readonly (int n, int d)[] ObjetivosBarra = { (1, 2), (5, 6), (3, 4) };

        // (denominador, cantidad) de piezas unitarias 1/d en cada monton.
        static readonly (int d, int cantidad)[] MontonesSector = { (2, 2), (3, 2), (4, 3), (8, 5) };
        static readonly (int d, int cantidad)[] MontonesBarra = { (2, 2), (3, 2), (4, 3), (6, 3) };

        [MenuItem("GameObject/Fracciones/Mesa de fracciones", false, 10)]
        static void Crear(MenuCommand comando)
        {
            // Las medidas se leen de las piezas para que las siluetas coincidan con ellas.
            var mitadSector = GeneradorPiezasFraccion.ObtenerPrefab(TipoPiezaFraccion.Sector, 1, 2);
            var mitadBarra = GeneradorPiezasFraccion.ObtenerPrefab(TipoPiezaFraccion.Barra, 1, 2);
            if (mitadSector == null || mitadBarra == null)
            {
                EditorUtility.DisplayDialog("Mesa de fracciones", "No se pudieron generar las piezas de fracción.", "OK");
                return;
            }

            var limitesSector = mitadSector.GetComponent<MeshFilter>().sharedMesh.bounds;
            var limitesBarra = mitadBarra.GetComponent<MeshFilter>().sharedMesh.bounds;
            float radio = limitesSector.extents.x;
            float largoBarra = limitesBarra.size.x * 2f;
            float profundidadBarra = limitesBarra.size.z;

            var raiz = new GameObject("MesaFracciones");
            Undo.RegisterCreatedObjectUndo(raiz, "Crear mesa de fracciones");
            GameObjectUtility.SetParentAndAlign(raiz, comando.context as GameObject);
            if (comando.context == null && SceneView.lastActiveSceneView != null)
                raiz.transform.position = SceneView.lastActiveSceneView.pivot;

            var matObjetivo = ObtenerMaterial("MatSiluetaFraccion", new Color(0.4f, 0.85f, 1f), 0.35f, 0.8f);
            var matEntero = ObtenerMaterial("MatEnteroFraccion", new Color(0.8f, 0.85f, 0.95f), 0.08f, 0.1f);

            float pasoCirculo = radio * 2f + 0.08f;
            for (int i = 0; i < ObjetivosCirculo.Length; i++)
            {
                var (n, d) = ObjetivosCirculo[i];
                float x = (i - (ObjetivosCirculo.Length - 1) * 0.5f) * pasoCirculo;
                CrearSilueta(raiz.transform, TipoPiezaFraccion.Sector, n, d, new Vector3(x, 0f, 0.38f), radio, largoBarra, profundidadBarra, matObjetivo, matEntero);
            }

            for (int i = 0; i < ObjetivosBarra.Length; i++)
            {
                var (n, d) = ObjetivosBarra[i];
                float z = 0.12f - i * (profundidadBarra + 0.04f);
                CrearSilueta(raiz.transform, TipoPiezaFraccion.Barra, n, d, new Vector3(-largoBarra * 0.5f, 0f, z), radio, largoBarra, profundidadBarra, matObjetivo, matEntero);
            }

            float pasoMonton = radio * 2f + 0.06f;
            for (int i = 0; i < MontonesSector.Length; i++)
            {
                float x = (i - (MontonesSector.Length - 1) * 0.5f) * pasoMonton;
                CrearMonton(raiz.transform, TipoPiezaFraccion.Sector, MontonesSector[i], new Vector3(x, 0f, -0.3f));
            }

            for (int i = 0; i < MontonesBarra.Length; i++)
            {
                float x = (i - (MontonesBarra.Length - 1) * 0.5f) * (largoBarra * 0.5f + 0.06f) - largoBarra * 0.25f;
                CrearMonton(raiz.transform, TipoPiezaFraccion.Barra, MontonesBarra[i], new Vector3(x, 0f, -0.55f));
            }

            var marcador = CrearTexto("Marcador", raiz.transform, new Vector3(0f, 0.4f, 0.5f), 0.6f);
            var mesa = raiz.AddComponent<MesaFracciones>();
            var serializado = new SerializedObject(mesa);
            serializado.FindProperty("marcador").objectReferenceValue = marcador;
            serializado.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = raiz;
        }

        static void CrearSilueta(Transform raiz, TipoPiezaFraccion tipo, int n, int d, Vector3 posicion,
            float radio, float largoBarra, float profundidadBarra, Material matObjetivo, Material matEntero)
        {
            var silueta = new GameObject($"Silueta_{tipo}_{n}_{d}");
            silueta.transform.SetParent(raiz, false);
            silueta.transform.localPosition = posicion;

            bool barra = tipo == TipoPiezaFraccion.Barra;
            float fraccion = (float)n / d;
            Mesh mallaEntero, mallaObjetivo;
            if (barra)
            {
                mallaEntero = GuardarMalla(MallaFraccion.CrearBarra(largoBarra, AltoSilueta * 0.5f, profundidadBarra, "SiluetaBarra_Entero"));
                mallaObjetivo = GuardarMalla(MallaFraccion.CrearBarra(largoBarra * fraccion, AltoSilueta, profundidadBarra, $"SiluetaBarra_{n}_{d}"));
            }
            else
            {
                mallaEntero = GuardarMalla(MallaFraccion.CrearSector(radio, AltoSilueta * 0.5f, Mathf.PI * 2f, 96, "SiluetaSector_Entero"));
                mallaObjetivo = GuardarMalla(MallaFraccion.CrearSector(radio, AltoSilueta, Mathf.PI * 2f * fraccion, Mathf.Max(4, Mathf.RoundToInt(96 * fraccion)), $"SiluetaSector_{n}_{d}"));
            }

            CrearVisual("Entero", silueta.transform, mallaEntero, matEntero);
            var objetivo = CrearVisual("Objetivo", silueta.transform, mallaObjetivo, matObjetivo);

            // Zona donde se sueltan las piezas: cubre el entero y un poco de margen.
            var zona = silueta.AddComponent<BoxCollider>();
            zona.isTrigger = true;
            zona.center = barra ? new Vector3(largoBarra * 0.5f, 0.06f, 0f) : new Vector3(0f, 0.06f, 0f);
            zona.size = barra
                ? new Vector3(largoBarra + 0.08f, 0.12f, profundidadBarra + 0.04f)
                : new Vector3(radio * 2f + 0.08f, 0.12f, radio * 2f + 0.08f);

            var posicionEtiqueta = barra ? new Vector3(largoBarra + 0.14f, 0.03f, 0f) : new Vector3(0f, 0.12f, 0f);
            var etiqueta = CrearTexto("Etiqueta", silueta.transform, posicionEtiqueta, 0.3f);

            silueta.AddComponent<SiluetaFraccion>().Configurar(tipo, n, d, largoBarra, objetivo, etiqueta);
        }

        static Renderer CrearVisual(string nombre, Transform padre, Mesh malla, Material material)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.AddComponent<MeshFilter>().sharedMesh = malla;
            var render = objeto.AddComponent<MeshRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            return render;
        }

        // Apila las piezas una sobre otra; la fisica las acomoda al empezar.
        static void CrearMonton(Transform raiz, TipoPiezaFraccion tipo, (int d, int cantidad) monton, Vector3 posicion)
        {
            var prefab = GeneradorPiezasFraccion.ObtenerPrefab(tipo, 1, monton.d);
            if (prefab == null) return;

            float alto = prefab.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
            for (int k = 0; k < monton.cantidad; k++)
            {
                var pieza = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz);
                pieza.transform.localPosition = posicion + Vector3.up * (0.005f + k * (alto + 0.005f));
                pieza.transform.localRotation = Quaternion.identity;
            }
        }

        static TextMeshPro CrearTexto(string nombre, Transform padre, Vector3 posicion, float tamano)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = posicion;
            var texto = objeto.AddComponent<TextMeshPro>();
            texto.text = "";
            texto.fontSize = tamano;
            texto.fontStyle = FontStyles.Bold;
            texto.color = Color.white;
            texto.alignment = TextAlignmentOptions.Center;
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            texto.rectTransform.sizeDelta = new Vector2(0.6f, 0.25f);
            return texto;
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
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * brillo);
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
