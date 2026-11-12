using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace TesisPerezRivera.Fisica
{
    // Plano inclinado con diagrama de cuerpo libre. La rampa es solida; cuando un
    // BloqueFuerzas queda apoyado sobre la pendiente (y nadie lo sostiene) se dibujan
    // las fuerzas que actuan sobre el, con largo proporcional a su magnitud.
    [ExecuteAlways]
    public class DiagramaPlanoInclinado : MonoBehaviour
    {
        [Header("Geometria (metros)")]
        [SerializeField, Range(5f, 60f)] float anguloGrados = 20f;
        [SerializeField] float largoBase = 1.2f;
        [SerializeField] float ancho = 0.4f;

        [Header("Fisica")]
        [Tooltip("Si tan(angulo) es mayor que este valor, el bloque se desliza.")]
        [SerializeField, Range(0f, 1f)] float coeficienteFriccion = 0.5f;
        [Tooltip("Fuerza que se aplica al bloque apoyado. Positiva lo empuja pendiente abajo, negativa pendiente arriba.")]
        [SerializeField] float fuerzaAplicada;

        [Header("Flechas")]
        [SerializeField] float metrosPorNewton = 0.015f;
        [SerializeField] float largoMinimo = 0.06f;
        [SerializeField] float grosor = 0.012f;
        [SerializeField] bool mostrarValores = true;

        static readonly Color ColorAplicada = new Color(1f, 0.45f, 0.15f);
        static readonly Color ColorNormal = new Color(0.3f, 0.72f, 1f);
        static readonly Color ColorFriccion = new Color(1f, 0.75f, 0.2f);
        static readonly Color ColorGravedad = new Color(0.3f, 0.85f, 0.45f);
        static readonly Color ColorRampa = new Color(0.75f, 0.8f, 0.9f);

        GameObject generado;
        MeshFilter rampa;
        MeshCollider colisionadorRampa;
        PhysicsMaterial materialFisico;
        BloqueFuerzas bloque;
        Flecha flechaAplicada, flechaNormal, flechaFriccion, flechaGravedad;
        LineRenderer contorno, arco, anguloRecto;
        TextMeshPro etiquetaAngulo, etiquetaResumen;
        readonly List<Object> recursos = new List<Object>();
        bool pendiente = true;

        public float AnguloGrados { get => anguloGrados; set { anguloGrados = Mathf.Clamp(value, 5f, 60f); pendiente = true; } }
        public float CoeficienteFriccion { get => coeficienteFriccion; set { coeficienteFriccion = Mathf.Clamp01(value); pendiente = true; } }
        public float FuerzaAplicada { get => fuerzaAplicada; set { fuerzaAplicada = value; pendiente = true; } }

        float Angulo => anguloGrados * Mathf.Deg2Rad;
        Vector3 AbajoLocal => new Vector3(Mathf.Cos(Angulo), -Mathf.Sin(Angulo), 0f);
        Vector3 NormalLocal => new Vector3(Mathf.Sin(Angulo), Mathf.Cos(Angulo), 0f);
        public Vector3 NormalPendienteMundo => transform.TransformDirection(NormalLocal);

        void OnEnable()
        {
            Construir();
            pendiente = true;
        }

        void OnDisable() => Limpiar();

        void OnValidate() => pendiente = true;

        void Update()
        {
            if (generado == null) Construir();

            // La geometria (y el collider) solo se rehace cuando cambia un valor;
            // las flechas siguen al bloque en cada frame.
            if (pendiente)
            {
                Actualizar();
                pendiente = false;
            }

            bloque = Application.isPlaying ? BuscarBloqueApoyado() : null;
            ActualizarFlechas(AbajoLocal, NormalLocal);
        }

        BloqueFuerzas BuscarBloqueApoyado()
        {
            foreach (var candidato in BloqueFuerzas.Todos)
            {
                if (candidato.ApoyadoEn(this)) return candidato;
            }
            return null;
        }

        void FixedUpdate()
        {
            if (bloque == null || Mathf.Abs(fuerzaAplicada) < 0.0001f) return;
            bloque.Cuerpo.AddForce(transform.TransformDirection(AbajoLocal) * fuerzaAplicada, ForceMode.Force);
        }

        void LateUpdate()
        {
            if (!Application.isPlaying || Camera.main == null || generado == null) return;

            var camara = Camera.main.transform.position;
            foreach (var texto in generado.GetComponentsInChildren<TextMeshPro>())
                texto.transform.rotation = Quaternion.LookRotation(texto.transform.position - camara, Vector3.up);
        }

        struct Fuerzas
        {
            public float peso, normal, impulso, friccion, neta;
        }

        // Fuerzas a lo largo de la pendiente; el sentido positivo es pendiente abajo.
        // velocidad es la componente de la velocidad del bloque pendiente abajo.
        Fuerzas Calcular(float masa, float velocidad)
        {
            float angulo = Angulo;
            var f = new Fuerzas();
            f.peso = masa * Physics.gravity.magnitude;
            f.normal = f.peso * Mathf.Cos(angulo);
            f.impulso = fuerzaAplicada + f.peso * Mathf.Sin(angulo);
            float friccionMaxima = coeficienteFriccion * f.normal;

            if (Mathf.Abs(velocidad) > 0.02f)
                f.friccion = friccionMaxima * Mathf.Sign(velocidad);
            else if (Mathf.Abs(f.impulso) <= friccionMaxima)
                f.friccion = f.impulso;
            else
                f.friccion = friccionMaxima * Mathf.Sign(f.impulso);

            f.neta = f.impulso - f.friccion;
            return f;
        }

        void Actualizar()
        {
            float angulo = Angulo;
            float alto = largoBase * Mathf.Tan(angulo);
            float frente = -ancho * 0.5f - 0.002f;

            // La rampa queda en el plano XY: el lado alto a la izquierda (x = 0) y la
            // pendiente baja hacia +X, igual que el diagrama de referencia.
            rampa.sharedMesh = CrearMallaRampa(largoBase, alto, ancho, rampa.sharedMesh);
            colisionadorRampa.sharedMesh = null;
            colisionadorRampa.sharedMesh = rampa.sharedMesh;
            materialFisico.dynamicFriction = coeficienteFriccion;
            materialFisico.staticFriction = coeficienteFriccion;

            contorno.positionCount = 3;
            contorno.SetPositions(new[] { new Vector3(0f, 0f, frente), new Vector3(largoBase, 0f, frente), new Vector3(0f, alto, frente) });

            float radioArco = Mathf.Min(0.18f, largoBase * 0.25f);
            const int puntosArco = 16;
            arco.positionCount = puntosArco + 1;
            for (int i = 0; i <= puntosArco; i++)
            {
                float a = Mathf.PI - angulo * i / puntosArco;
                arco.SetPosition(i, new Vector3(largoBase + Mathf.Cos(a) * radioArco, Mathf.Sin(a) * radioArco, frente));
            }

            float lado = Mathf.Min(0.06f, alto * 0.3f);
            anguloRecto.positionCount = 3;
            anguloRecto.SetPositions(new[] { new Vector3(lado, 0f, frente), new Vector3(lado, lado, frente), new Vector3(0f, lado, frente) });

            float bisectriz = Mathf.PI - angulo * 0.5f;
            etiquetaAngulo.transform.localPosition = new Vector3(largoBase + Mathf.Cos(bisectriz) * (radioArco + 0.07f), Mathf.Sin(bisectriz) * (radioArco + 0.07f), frente);
            etiquetaAngulo.text = $"θ = {anguloGrados:0}°";

            etiquetaResumen.transform.localPosition = new Vector3(largoBase * 0.5f, alto + 0.3f, 0f);
        }

        void ActualizarFlechas(Vector3 abajo, Vector3 normal)
        {
            if (bloque == null)
            {
                flechaGravedad.Ocultar();
                flechaNormal.Ocultar();
                flechaAplicada.Ocultar();
                flechaFriccion.Ocultar();
                etiquetaResumen.text = $"μ = {coeficienteFriccion:0.##}\n<size=80%>Coloca el bloque sobre la rampa</size>";
                return;
            }

            // Todo se calcula en el espacio local de la rampa.
            var centro = transform.InverseTransformPoint(bloque.Centro);
            float MitadEn(Vector3 direccionLocal) => bloque.MitadEn(transform.TransformDirection(direccionLocal));

            float masa = bloque.Cuerpo.mass;
            float velocidad = Vector3.Dot(bloque.Cuerpo.linearVelocity, transform.TransformDirection(abajo));
            var f = Calcular(masa, velocidad);
            var gravedadLocal = transform.InverseTransformDirection(Vector3.down);

            flechaGravedad.Colocar(centro, gravedadLocal, Largo(f.peso), "Gravedad (mg)", f.peso, true);
            flechaNormal.Colocar(centro, normal, Largo(f.normal), "Fuerza\nNormal (N)", f.normal, true);

            var sentidoAplicada = abajo * Mathf.Sign(fuerzaAplicada);
            float largoAplicada = Largo(Mathf.Abs(fuerzaAplicada));
            flechaAplicada.Colocar(centro - sentidoAplicada * (MitadEn(sentidoAplicada) + largoAplicada), sentidoAplicada, largoAplicada,
                "Fuerza\nAplicada (F)", Mathf.Abs(fuerzaAplicada), false);

            // La friccion actua en la superficie de contacto y se opone al movimiento.
            var sentidoFriccion = -abajo * Mathf.Sign(f.friccion);
            float largoFriccion = Largo(Mathf.Abs(f.friccion));
            var contacto = centro - normal * (MitadEn(normal) - 0.01f);
            flechaFriccion.Colocar(contacto - sentidoFriccion * (MitadEn(sentidoFriccion) + largoFriccion), sentidoFriccion, largoFriccion,
                "Fricción (f)", Mathf.Abs(f.friccion), false);

            etiquetaResumen.text = $"m = {masa:0.#} kg   μ = {coeficienteFriccion:0.##}\n" +
                                   $"a = {f.neta / masa:0.00} m/s²" + (Mathf.Abs(f.neta) < 0.0001f ? "  (en reposo)" : "");
        }

        float Largo(float newtons) => Mathf.Max(largoMinimo, newtons * metrosPorNewton);

        void Construir()
        {
            Limpiar();

            generado = new GameObject("Generado");
            generado.transform.SetParent(transform, false);

            var matRampa = CrearMaterial(ColorRampa, 0.3f, 0.15f);
            var matLinea = CrearMaterial(ColorRampa, 1f, 0.6f);

            var objetoRampa = new GameObject("Rampa");
            objetoRampa.transform.SetParent(generado.transform, false);
            rampa = objetoRampa.AddComponent<MeshFilter>();
            objetoRampa.AddComponent<MeshRenderer>().sharedMaterial = matRampa;

            // Friccion con combinacion "minimo": el bloque usa friccion 1, asi que manda este valor.
            materialFisico = new PhysicsMaterial("Rampa")
            {
                hideFlags = HideFlags.DontSave,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            recursos.Add(materialFisico);
            colisionadorRampa = objetoRampa.AddComponent<MeshCollider>();
            colisionadorRampa.convex = true;
            colisionadorRampa.sharedMaterial = materialFisico;

            var punta = CrearMallaCono();
            flechaAplicada = new Flecha(this, "Flecha_Aplicada", ColorAplicada, punta);
            flechaNormal = new Flecha(this, "Flecha_Normal", ColorNormal, punta);
            flechaFriccion = new Flecha(this, "Flecha_Friccion", ColorFriccion, punta);
            flechaGravedad = new Flecha(this, "Flecha_Gravedad", ColorGravedad, punta);

            contorno = CrearLinea("Contorno", matLinea, true);
            arco = CrearLinea("Arco_Angulo", matLinea, false);
            anguloRecto = CrearLinea("Angulo_Recto", matLinea, false);

            etiquetaAngulo = CrearTexto("Etiqueta_Angulo", Color.white, 0.4f);
            etiquetaResumen = CrearTexto("Etiqueta_Resumen", Color.white, 0.4f);

            foreach (var t in generado.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.DontSave;
        }

        void Limpiar()
        {
            if (generado != null) Destruir(generado);
            generado = null;
            foreach (var recurso in recursos)
            {
                if (recurso != null) Destruir(recurso);
            }
            recursos.Clear();
        }

        static void Destruir(Object objeto)
        {
            if (Application.isPlaying) Destroy(objeto);
            else DestroyImmediate(objeto);
        }

        static void QuitarColisionador(GameObject objeto)
        {
            var colisionador = objeto.GetComponent<Collider>();
            if (colisionador != null) Destruir(colisionador);
        }

        Material CrearMaterial(Color color, float opacidad, float brillo)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.DontSave };
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
            recursos.Add(material);
            return material;
        }

        LineRenderer CrearLinea(string nombre, Material material, bool cerrada)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(generado.transform, false);
            var linea = objeto.AddComponent<LineRenderer>();
            linea.useWorldSpace = false;
            linea.loop = cerrada;
            linea.widthMultiplier = 0.008f;
            linea.sharedMaterial = material;
            linea.shadowCastingMode = ShadowCastingMode.Off;
            return linea;
        }

        TextMeshPro CrearTexto(string nombre, Color color, float tamano)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(generado.transform, false);
            var texto = objeto.AddComponent<TextMeshPro>();
            texto.alignment = TextAlignmentOptions.Center;
            texto.fontStyle = FontStyles.Bold;
            texto.color = color;
            texto.fontSize = tamano;
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            texto.rectTransform.sizeDelta = new Vector2(0.6f, 0.2f);
            return texto;
        }

        Mesh CrearMallaRampa(float largo, float alto, float profundidad, Mesh reutilizar)
        {
            var malla = reutilizar;
            if (malla == null)
            {
                malla = new Mesh { name = "Rampa", hideFlags = HideFlags.DontSave };
                recursos.Add(malla);
            }

            float z = profundidad * 0.5f;
            var a0 = new Vector3(0f, 0f, -z); var b0 = new Vector3(largo, 0f, -z); var c0 = new Vector3(0f, alto, -z);
            var a1 = new Vector3(0f, 0f, z); var b1 = new Vector3(largo, 0f, z); var c1 = new Vector3(0f, alto, z);
            var normalPendiente = new Vector3(alto, largo, 0f).normalized;

            var vertices = new List<Vector3>();
            var normales = new List<Vector3>();
            void Cara(Vector3 n, params Vector3[] puntos)
            {
                for (int i = 1; i < puntos.Length - 1; i++)
                {
                    Vector3 p = puntos[0], q = puntos[i], r = puntos[i + 1];
                    if (Vector3.Dot(Vector3.Cross(q - p, r - p), n) < 0f) (q, r) = (r, q);
                    vertices.Add(p); vertices.Add(q); vertices.Add(r);
                    normales.Add(n); normales.Add(n); normales.Add(n);
                }
            }

            Cara(Vector3.back, a0, b0, c0);
            Cara(Vector3.forward, a1, b1, c1);
            Cara(Vector3.down, a0, b0, b1, a1);
            Cara(Vector3.left, a0, c0, c1, a1);
            Cara(normalPendiente, c0, b0, b1, c1);

            var triangulos = new int[vertices.Count];
            for (int i = 0; i < triangulos.Length; i++) triangulos[i] = i;

            malla.Clear();
            malla.SetVertices(vertices);
            malla.SetNormals(normales);
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            return malla;
        }

        // Cono de radio 0.5 y alto 1 con la base en y = 0, para la punta de las flechas.
        Mesh CrearMallaCono()
        {
            const int lados = 20;
            var vertices = new List<Vector3>();
            var normales = new List<Vector3>();
            var triangulos = new List<int>();
            var punta = Vector3.up;

            for (int i = 0; i < lados; i++)
            {
                float a0 = Mathf.PI * 2f * i / lados, a1 = Mathf.PI * 2f * (i + 1) / lados;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);

                int n = vertices.Count;
                var lateral = Vector3.Cross(p1 - p0, punta - p0).normalized;
                if (Vector3.Dot(lateral, p0) < 0f) lateral = -lateral;
                vertices.AddRange(new[] { p0, punta, p1 });
                normales.AddRange(new[] { lateral, lateral, lateral });
                triangulos.AddRange(new[] { n, n + 1, n + 2 });

                n = vertices.Count;
                vertices.AddRange(new[] { Vector3.zero, p0, p1 });
                normales.AddRange(new[] { Vector3.down, Vector3.down, Vector3.down });
                triangulos.AddRange(new[] { n, n + 1, n + 2 });
            }

            // Ajusta la orientacion de cada triangulo segun su normal.
            for (int i = 0; i < triangulos.Count; i += 3)
            {
                var p = vertices[triangulos[i]]; var q = vertices[triangulos[i + 1]]; var r = vertices[triangulos[i + 2]];
                if (Vector3.Dot(Vector3.Cross(q - p, r - p), normales[triangulos[i]]) < 0f)
                    (triangulos[i + 1], triangulos[i + 2]) = (triangulos[i + 2], triangulos[i + 1]);
            }

            var malla = new Mesh { name = "Cono", hideFlags = HideFlags.DontSave };
            malla.SetVertices(vertices);
            malla.SetNormals(normales);
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            recursos.Add(malla);
            return malla;
        }

        class Flecha
        {
            readonly DiagramaPlanoInclinado diagrama;
            readonly Transform raiz, cuerpo, punta;
            readonly TextMeshPro etiqueta;

            public Flecha(DiagramaPlanoInclinado diagrama, string nombre, Color color, Mesh mallaPunta)
            {
                this.diagrama = diagrama;
                var material = diagrama.CrearMaterial(color, 1f, 1.2f);

                raiz = new GameObject(nombre).transform;
                raiz.SetParent(diagrama.generado.transform, false);

                cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
                cuerpo.name = "Cuerpo";
                cuerpo.SetParent(raiz, false);
                QuitarColisionador(cuerpo.gameObject);
                cuerpo.GetComponent<MeshRenderer>().sharedMaterial = material;

                punta = new GameObject("Punta").transform;
                punta.SetParent(raiz, false);
                punta.gameObject.AddComponent<MeshFilter>().sharedMesh = mallaPunta;
                punta.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;

                etiqueta = diagrama.CrearTexto(nombre + "_Etiqueta", color, 0.35f);
            }

            public void Ocultar()
            {
                raiz.gameObject.SetActive(false);
                etiqueta.gameObject.SetActive(false);
            }

            // etiquetaEnPunta: la etiqueta va junto a la punta (flechas que salen del bloque)
            // o junto a la cola (flechas que empujan hacia el bloque).
            public void Colocar(Vector3 inicio, Vector3 direccion, float largo, string nombre, float newtons, bool etiquetaEnPunta)
            {
                bool visible = newtons > 0.0001f;
                raiz.gameObject.SetActive(visible);
                etiqueta.gameObject.SetActive(visible);
                if (!visible) return;

                float g = diagrama.grosor;
                float largoPunta = Mathf.Min(g * 4f, largo * 0.4f);
                float largoCuerpo = largo - largoPunta;

                raiz.localPosition = inicio;
                raiz.localRotation = Quaternion.FromToRotation(Vector3.up, direccion);
                cuerpo.localPosition = new Vector3(0f, largoCuerpo * 0.5f, 0f);
                cuerpo.localScale = new Vector3(g, largoCuerpo * 0.5f, g);
                punta.localPosition = new Vector3(0f, largoCuerpo, 0f);
                punta.localScale = new Vector3(g * 3f, largoPunta, g * 3f);

                var extremo = etiquetaEnPunta ? inicio + direccion * (largo + 0.07f) : inicio - direccion * 0.09f;
                extremo.z = -diagrama.ancho * 0.5f - 0.03f;
                etiqueta.transform.localPosition = extremo;
                etiqueta.text = diagrama.mostrarValores ? $"{nombre}\n<size=80%>{newtons:0.0} N</size>" : nombre;
            }
        }
    }
}
