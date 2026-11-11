using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TesisPerezRivera.Fracciones
{
    // Ventana de editor que genera los prefabs de las piezas de fraccion
    // (sectores circulares, barras y cilindros) listos para agarrarse en VR.
    public class GeneradorPiezasFraccion : EditorWindow
    {
        const string CarpetaBase = "Assets/Fracciones";
        const string CarpetaMallas = CarpetaBase + "/Mallas";
        const string CarpetaMateriales = CarpetaBase + "/Materiales";
        const string CarpetaPrefabs = CarpetaBase + "/Prefabs";

        [SerializeField] string denominadores = "1,2,3,4,5,6,8,10,12";
        [SerializeField] bool generarSectores = true;
        [SerializeField] bool generarBarras = true;
        [SerializeField] bool generarCilindros = true;

        [SerializeField] float radioSector = 0.12f;
        [SerializeField] float altoSector = 0.03f;
        [SerializeField] float radioCilindro = 0.06f;
        [SerializeField] float altoCilindro = 0.12f;
        [SerializeField] float largoBarraUnidad = 0.36f;
        [SerializeField] float altoBarra = 0.03f;
        [SerializeField] float profundidadBarra = 0.05f;

        [SerializeField] bool transparente = true;
        [SerializeField, Range(0.2f, 1f)] float opacidad = 0.8f;
        [SerializeField] float intensidadBrillo = 1.5f;

        [SerializeField] int numeradorPersonalizado = 3;
        [SerializeField] int denominadorPersonalizado = 4;

        Vector2 desplazamiento;

        [MenuItem("Tools/Fracciones/Generador de piezas")]
        static void Abrir() => GetWindow<GeneradorPiezasFraccion>("Piezas de fraccion");

        void OnGUI()
        {
            desplazamiento = EditorGUILayout.BeginScrollView(desplazamiento);

            EditorGUILayout.LabelField("Piezas unitarias (1/d)", EditorStyles.boldLabel);
            denominadores = EditorGUILayout.TextField("Denominadores", denominadores);
            generarSectores = EditorGUILayout.Toggle("Sectores (pizza)", generarSectores);
            generarBarras = EditorGUILayout.Toggle("Barras", generarBarras);
            generarCilindros = EditorGUILayout.Toggle("Cilindros", generarCilindros);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Medidas (metros)", EditorStyles.boldLabel);
            radioSector = EditorGUILayout.FloatField("Radio sector", radioSector);
            altoSector = EditorGUILayout.FloatField("Alto sector", altoSector);
            radioCilindro = EditorGUILayout.FloatField("Radio cilindro", radioCilindro);
            altoCilindro = EditorGUILayout.FloatField("Alto cilindro", altoCilindro);
            largoBarraUnidad = EditorGUILayout.FloatField("Largo barra entera", largoBarraUnidad);
            altoBarra = EditorGUILayout.FloatField("Alto barra", altoBarra);
            profundidadBarra = EditorGUILayout.FloatField("Profundidad barra", profundidadBarra);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Aspecto", EditorStyles.boldLabel);
            transparente = EditorGUILayout.Toggle("Transparente", transparente);
            if (transparente)
                opacidad = EditorGUILayout.Slider("Opacidad", opacidad, 0.2f, 1f);
            intensidadBrillo = EditorGUILayout.FloatField("Intensidad brillo", intensidadBrillo);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generar prefabs", GUILayout.Height(30)))
                GenerarUnitarias();

            if (GUILayout.Button("Colocar set de ejemplo en la escena"))
                ColocarSetEjemplo();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pieza personalizada (n/d)", EditorStyles.boldLabel);
            numeradorPersonalizado = Mathf.Max(1, EditorGUILayout.IntField("Numerador", numeradorPersonalizado));
            denominadorPersonalizado = Mathf.Max(1, EditorGUILayout.IntField("Denominador", denominadorPersonalizado));
            if (numeradorPersonalizado > denominadorPersonalizado)
                EditorGUILayout.HelpBox("Las fracciones impropias se limitan a una unidad completa en sectores y cilindros.", MessageType.Info);
            if (GUILayout.Button("Generar pieza personalizada"))
                GenerarTipos(numeradorPersonalizado, denominadorPersonalizado);

            EditorGUILayout.EndScrollView();
        }

        List<int> LeerDenominadores()
        {
            return denominadores.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var d) ? d : 0)
                .Where(d => d > 0)
                .Distinct()
                .ToList();
        }

        void GenerarUnitarias()
        {
            var lista = LeerDenominadores();
            try
            {
                for (int i = 0; i < lista.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Piezas de fraccion", $"1/{lista[i]}", (float)i / lista.Count);
                    GenerarTipos(1, lista[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Fracciones: prefabs generados en {CarpetaPrefabs}.");
        }

        void GenerarTipos(int numerador, int denominador)
        {
            CrearCarpetas();
            if (generarSectores) CrearPrefab(TipoPiezaFraccion.Sector, numerador, denominador);
            if (generarBarras) CrearPrefab(TipoPiezaFraccion.Barra, numerador, denominador);
            if (generarCilindros) CrearPrefab(TipoPiezaFraccion.Cilindro, numerador, denominador);
        }

        GameObject CrearPrefab(TipoPiezaFraccion tipo, int numerador, int denominador)
        {
            string nombre = $"Fraccion_{tipo}_{numerador}_{denominador}";
            float fraccion = (float)numerador / denominador;
            var material = ObtenerMaterial(denominador);

            var raiz = new GameObject(nombre);
            try
            {
                var pieza = raiz.AddComponent<PiezaFraccion>();
                pieza.Configurar(tipo, numerador, denominador);

                Mesh malla;
                Vector3 posicionEtiqueta;
                Vector2 tamanoEtiqueta;
                float giroEtiqueta = 0f;
                float alto;

                if (tipo == TipoPiezaFraccion.Barra)
                {
                    float largo = largoBarraUnidad * fraccion;
                    alto = altoBarra;
                    malla = MallaFraccion.CrearBarra(largo, alto, profundidadBarra, nombre);
                    posicionEtiqueta = new Vector3(largo * 0.5f, alto, 0f);
                    tamanoEtiqueta = new Vector2(Mathf.Min(largo * 0.8f, 0.06f), profundidadBarra * 0.9f);

                    var caja = raiz.AddComponent<BoxCollider>();
                    caja.center = new Vector3(largo * 0.5f, alto * 0.5f, 0f);
                    caja.size = new Vector3(largo, alto, profundidadBarra);
                }
                else
                {
                    bool cilindro = tipo == TipoPiezaFraccion.Cilindro;
                    float radio = cilindro ? radioCilindro : radioSector;
                    alto = cilindro ? altoCilindro : altoSector;
                    float angulo = Mathf.PI * 2f * Mathf.Min(fraccion, 1f);
                    int segmentos = Mathf.Max(4, Mathf.RoundToInt(96 * Mathf.Min(fraccion, 1f)));
                    malla = MallaFraccion.CrearSector(radio, alto, angulo, segmentos, nombre);

                    float distancia = MallaFraccion.DistanciaCentroide(radio, angulo);
                    float bisectriz = angulo * 0.5f;
                    posicionEtiqueta = new Vector3(Mathf.Cos(bisectriz) * distancia, alto, Mathf.Sin(bisectriz) * distancia);

                    bool completo = distancia == 0f;
                    float ancho = completo ? radio * 0.6f : Mathf.Min(2f * distancia * Mathf.Sin(Mathf.Min(bisectriz, Mathf.PI * 0.5f)) * 0.8f, radio * 0.6f);
                    tamanoEtiqueta = Vector2.one * ancho;
                    // La parte superior del texto apunta al centro: se lee desde fuera del circulo.
                    giroEtiqueta = completo ? 0f : 270f - bisectriz * Mathf.Rad2Deg;

                    CrearColisionadoresSector(raiz, radio, alto, angulo, nombre);
                }

                malla = GuardarAsset(malla, $"{CarpetaMallas}/{nombre}.asset");
                raiz.AddComponent<MeshFilter>().sharedMesh = malla;
                var render = raiz.AddComponent<MeshRenderer>();
                render.sharedMaterial = material;

                CrearEtiqueta(raiz.transform, numerador, denominador, posicionEtiqueta, tamanoEtiqueta, giroEtiqueta);

                var cuerpo = raiz.AddComponent<Rigidbody>();
                cuerpo.mass = 0.2f;
                cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
                cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                var agarre = raiz.AddComponent<XRGrabInteractable>();
                agarre.useDynamicAttach = true;
                agarre.throwOnDetach = false;

                return PrefabUtility.SaveAsPrefabAsset(raiz, $"{CarpetaPrefabs}/{nombre}.prefab");
            }
            finally
            {
                DestroyImmediate(raiz);
            }
        }

        // Los MeshCollider convexos no admiten sectores mayores a media vuelta,
        // asi que se divide la forma en trozos de hasta 90 grados.
        void CrearColisionadoresSector(GameObject raiz, float radio, float alto, float angulo, string nombre)
        {
            if (angulo <= Mathf.PI + 0.0001f)
            {
                var unico = MallaFraccion.CrearSector(radio, alto, angulo, Mathf.Max(2, Mathf.CeilToInt(angulo / 0.2f)), nombre + "_col");
                var colisionador = raiz.AddComponent<MeshCollider>();
                colisionador.sharedMesh = GuardarAsset(unico, $"{CarpetaMallas}/{nombre}_col.asset");
                colisionador.convex = true;
                return;
            }

            int trozos = Mathf.CeilToInt(angulo / (Mathf.PI * 0.5f));
            float anguloTrozo = angulo / trozos;
            var mallaTrozo = MallaFraccion.CrearSector(radio, alto, anguloTrozo, Mathf.Max(2, Mathf.CeilToInt(anguloTrozo / 0.2f)), nombre + "_col");
            mallaTrozo = GuardarAsset(mallaTrozo, $"{CarpetaMallas}/{nombre}_col.asset");

            for (int i = 0; i < trozos; i++)
            {
                var hijo = new GameObject($"Colision_{i}");
                hijo.transform.SetParent(raiz.transform, false);
                hijo.transform.localRotation = Quaternion.Euler(0f, -anguloTrozo * i * Mathf.Rad2Deg, 0f);
                var colisionador = hijo.AddComponent<MeshCollider>();
                colisionador.sharedMesh = mallaTrozo;
                colisionador.convex = true;
            }
        }

        static void CrearEtiqueta(Transform padre, int numerador, int denominador, Vector3 posicion, Vector2 tamano, float giro)
        {
            var objeto = new GameObject("Etiqueta");
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = posicion + Vector3.up * 0.0015f;
            objeto.transform.localRotation = Quaternion.Euler(90f, giro, 0f);

            var texto = objeto.AddComponent<TextMeshPro>();
            texto.text = $"<u>{numerador}</u>\n{denominador}";
            texto.alignment = TextAlignmentOptions.Center;
            texto.fontStyle = FontStyles.Bold;
            texto.color = Color.black;
            texto.lineSpacing = -20f;
            texto.enableAutoSizing = true;
            texto.fontSizeMin = 0.01f;
            texto.fontSizeMax = 2f;
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            texto.rectTransform.sizeDelta = tamano;
        }

        Material ObtenerMaterial(int denominador)
        {
            string ruta = $"{CarpetaMateriales}/MatFraccion_d{denominador}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ruta);
            }

            var color = ColorDenominador(denominador);
            material.shader = shader;
            material.SetFloat("_Smoothness", 0.85f);
            material.SetFloat("_Metallic", 0f);

            if (transparente)
            {
                color.a = opacidad;
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                color.a = 1f;
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.One);
                material.SetFloat("_DstBlend", (float)BlendMode.Zero);
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                material.SetFloat("_ZWrite", 1f);
                material.SetOverrideTag("RenderType", "Opaque");
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Geometry;
            }

            material.SetColor("_BaseColor", color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * intensidadBrillo);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Color ColorDenominador(int denominador)
        {
            switch (denominador)
            {
                case 1: return new Color(0.30f, 1.00f, 0.50f);
                case 2: return new Color(0.10f, 0.90f, 0.80f);
                case 3: return new Color(0.20f, 0.55f, 1.00f);
                case 4: return new Color(1.00f, 0.55f, 0.10f);
                case 5: return new Color(0.65f, 0.35f, 1.00f);
                case 6: return new Color(1.00f, 0.30f, 0.60f);
                case 8: return new Color(1.00f, 0.90f, 0.20f);
                case 10: return new Color(1.00f, 0.30f, 0.25f);
                case 12: return new Color(0.60f, 1.00f, 0.20f);
                default: return Color.HSVToRGB(denominador * 0.137f % 1f, 0.75f, 1f);
            }
        }

        // Reutiliza el asset existente para no romper las referencias de prefabs ya colocados.
        static Mesh GuardarAsset(Mesh malla, string ruta)
        {
            var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (existente == null)
            {
                AssetDatabase.CreateAsset(malla, ruta);
                return malla;
            }

            EditorUtility.CopySerialized(malla, existente);
            DestroyImmediate(malla);
            return existente;
        }

        static void CrearCarpetas()
        {
            foreach (var carpeta in new[] { CarpetaBase, CarpetaMallas, CarpetaMateriales, CarpetaPrefabs })
            {
                if (AssetDatabase.IsValidFolder(carpeta)) continue;
                AssetDatabase.CreateFolder(Path.GetDirectoryName(carpeta).Replace('\\', '/'), Path.GetFileName(carpeta));
            }
        }

        // Arma circulos completos con sectores, una "pared de fracciones" con barras
        // y una fila de cilindros, frente al punto donde mira la vista de escena.
        void ColocarSetEjemplo()
        {
            var lista = LeerDenominadores();
            var origen = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            var set = new GameObject("SetFracciones");
            set.transform.position = origen;
            Undo.RegisterCreatedObjectUndo(set, "Colocar set de fracciones");

            float paso = radioSector * 2f + 0.06f;
            for (int i = 0; i < lista.Count; i++)
            {
                int d = lista[i];
                var sector = CargarPrefab(TipoPiezaFraccion.Sector, d);
                if (sector != null)
                {
                    var centro = new Vector3(i * paso, 0f, 0f);
                    for (int k = 0; k < d; k++)
                        Instanciar(sector, set.transform, centro, Quaternion.Euler(0f, -360f / d * k, 0f));
                }

                var barra = CargarPrefab(TipoPiezaFraccion.Barra, d);
                if (barra != null)
                {
                    float largo = largoBarraUnidad / d;
                    for (int k = 0; k < d; k++)
                        Instanciar(barra, set.transform, new Vector3(k * largo, 0f, -paso - i * (profundidadBarra + 0.01f)), Quaternion.identity);
                }

                var cilindro = CargarPrefab(TipoPiezaFraccion.Cilindro, d);
                if (cilindro != null)
                    Instanciar(cilindro, set.transform, new Vector3(i * (radioCilindro * 2f + 0.05f), 0f, paso), Quaternion.identity);
            }

            Selection.activeGameObject = set;
        }

        static GameObject CargarPrefab(TipoPiezaFraccion tipo, int denominador)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{CarpetaPrefabs}/Fraccion_{tipo}_1_{denominador}.prefab");
        }

        // Devuelve el prefab de la pieza; si todavia no existe lo genera con los valores por defecto.
        internal static GameObject ObtenerPrefab(TipoPiezaFraccion tipo, int numerador, int denominador)
        {
            string ruta = $"{CarpetaPrefabs}/Fraccion_{tipo}_{numerador}_{denominador}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab != null) return prefab;

            var generador = CreateInstance<GeneradorPiezasFraccion>();
            try
            {
                generador.GenerarTipos(numerador, denominador);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                DestroyImmediate(generador);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        }

        static void Instanciar(GameObject prefab, Transform padre, Vector3 posicionLocal, Quaternion rotacionLocal)
        {
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            instancia.transform.localPosition = posicionLocal;
            instancia.transform.localRotation = rotacionLocal;
        }
    }
}
