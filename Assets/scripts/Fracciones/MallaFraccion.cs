using System.Collections.Generic;
using UnityEngine;

namespace TesisPerezRivera.Fracciones
{
    // Genera las mallas procedurales de las piezas de fraccion.
    // Todas las mallas apoyan su base en y = 0.
    public static class MallaFraccion
    {
        // Porcion de circulo extruida. El pivote es el centro del circulo, asi las
        // piezas de un mismo denominador encajan rotandolas alrededor del mismo punto.
        // El angulo crece desde +X hacia +Z.
        public static Mesh CrearSector(float radio, float alto, float angulo, int segmentos, string nombre)
        {
            var c = new Constructor();
            bool completo = angulo >= Mathf.PI * 2f - 0.0001f;
            var centroAbajo = Vector3.zero;
            var centroArriba = Vector3.up * alto;

            for (int i = 0; i < segmentos; i++)
            {
                float a0 = angulo * i / segmentos;
                float a1 = angulo * (i + 1) / segmentos;
                var b0 = Punto(a0, radio, 0f);
                var b1 = Punto(a1, radio, 0f);
                var t0 = Punto(a0, radio, alto);
                var t1 = Punto(a1, radio, alto);

                c.Tri(centroArriba, t0, t1, Vector3.up);
                c.Tri(centroAbajo, b0, b1, Vector3.down);

                var n0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var n1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                c.TriSuave(b0, b1, t1, n0, n1, n1);
                c.TriSuave(b0, t1, t0, n0, n1, n0);
            }

            if (!completo)
            {
                c.Quad(centroAbajo, Punto(0f, radio, 0f), Punto(0f, radio, alto), centroArriba, Vector3.back);
                var normalFinal = new Vector3(-Mathf.Sin(angulo), 0f, Mathf.Cos(angulo));
                c.Quad(centroAbajo, Punto(angulo, radio, 0f), Punto(angulo, radio, alto), centroArriba, normalFinal);
            }

            return c.Construir(nombre);
        }

        // Barra rectangular: x de 0 a largo (para alinear barras una tras otra),
        // z centrado en 0.
        public static Mesh CrearBarra(float largo, float alto, float profundidad, string nombre)
        {
            var c = new Constructor();
            float z = profundidad * 0.5f;
            var p000 = new Vector3(0f, 0f, -z);
            var p100 = new Vector3(largo, 0f, -z);
            var p010 = new Vector3(0f, alto, -z);
            var p110 = new Vector3(largo, alto, -z);
            var p001 = new Vector3(0f, 0f, z);
            var p101 = new Vector3(largo, 0f, z);
            var p011 = new Vector3(0f, alto, z);
            var p111 = new Vector3(largo, alto, z);

            c.Quad(p010, p110, p111, p011, Vector3.up);
            c.Quad(p000, p100, p101, p001, Vector3.down);
            c.Quad(p000, p100, p110, p010, Vector3.back);
            c.Quad(p001, p101, p111, p011, Vector3.forward);
            c.Quad(p000, p001, p011, p010, Vector3.left);
            c.Quad(p100, p101, p111, p110, Vector3.right);

            return c.Construir(nombre);
        }

        // Distancia del centro del circulo al centroide del sector.
        public static float DistanciaCentroide(float radio, float angulo)
        {
            if (angulo >= Mathf.PI * 2f - 0.0001f) return 0f;
            return 4f * radio * Mathf.Sin(angulo * 0.5f) / (3f * angulo);
        }

        static Vector3 Punto(float a, float r, float y) => new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);

        class Constructor
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector3> normales = new List<Vector3>();
            readonly List<Vector2> uvs = new List<Vector2>();
            readonly List<int> triangulos = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal) => TriSuave(a, b, c, normal, normal, normal);

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                Tri(a, b, c, normal);
                Tri(a, c, d, normal);
            }

            // Ajusta el orden de los vertices para que la cara mire hacia la normal indicada.
            public void TriSuave(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
            {
                var normalCara = na + nb + nc;
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normalCara) < 0f)
                {
                    (b, c) = (c, b);
                    (nb, nc) = (nc, nb);
                }

                Agregar(a, na);
                Agregar(b, nb);
                Agregar(c, nc);
            }

            void Agregar(Vector3 v, Vector3 n)
            {
                triangulos.Add(vertices.Count);
                vertices.Add(v);
                normales.Add(n.normalized);
                uvs.Add(new Vector2(v.x + v.y, v.z + v.y));
            }

            public Mesh Construir(string nombre)
            {
                var malla = new Mesh { name = nombre };
                malla.SetVertices(vertices);
                malla.SetNormals(normales);
                malla.SetUVs(0, uvs);
                malla.SetTriangles(triangulos, 0);
                malla.RecalculateBounds();
                malla.RecalculateTangents();
                return malla;
            }
        }
    }
}
