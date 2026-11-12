using System.Collections.Generic;
using UnityEngine;

namespace TesisPerezRivera.Geometria
{
    // Mallas de 1 unidad de alto centradas en el origen, igual que las primitivas de Unity.
    static class MallaFormas
    {
        public static Mesh CrearCono(int lados = 40)
        {
            var c = new Constructor();
            var punta = new Vector3(0f, 0.5f, 0f);
            for (int i = 0; i < lados; i++)
            {
                float a0 = Mathf.PI * 2f * i / lados, a1 = Mathf.PI * 2f * (i + 1) / lados;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, -0.5f, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, -0.5f, Mathf.Sin(a1) * 0.5f);

                // Normales suaves en el costado: inclinadas segun la pendiente del cono.
                Vector3 Lateral(float a) => new Vector3(Mathf.Cos(a), 0.5f, Mathf.Sin(a)).normalized;
                c.Tri(p0, p1, punta, Lateral(a0), Lateral(a1), Lateral((a0 + a1) * 0.5f));
                c.Tri(new Vector3(0f, -0.5f, 0f), p0, p1, Vector3.down);
            }
            return c.Construir("Cono");
        }

        public static Mesh CrearPiramide()
        {
            var c = new Constructor();
            var punta = new Vector3(0f, 0.5f, 0f);
            var b = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
            };
            for (int i = 0; i < 4; i++)
                c.Tri(b[i], b[(i + 1) % 4], punta);
            c.Tri(b[0], b[1], b[2]);
            c.Tri(b[0], b[2], b[3]);
            return c.Construir("Piramide");
        }

        // Prisma triangular: triangulo en XY extruido a lo largo de Z.
        public static Mesh CrearPrisma()
        {
            var c = new Constructor();
            Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
            var a0 = P(-0.5f, -0.5f, -0.5f); var b0 = P(0.5f, -0.5f, -0.5f); var c0 = P(0f, 0.5f, -0.5f);
            var a1 = P(-0.5f, -0.5f, 0.5f); var b1 = P(0.5f, -0.5f, 0.5f); var c1 = P(0f, 0.5f, 0.5f);

            c.Tri(a0, b0, c0);
            c.Tri(a1, b1, c1);
            c.Quad(a0, b0, b1, a1);
            c.Quad(b0, c0, c1, b1);
            c.Quad(c0, a0, a1, c1);
            return c.Construir("Prisma");
        }

        // Las formas son convexas y contienen el origen: cada cara se orienta hacia afuera.
        class Constructor
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector3> normales = new List<Vector3>();
            readonly List<int> triangulos = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                var n = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(n, (a + b + c) / 3f) < 0f) n = -n;
                Tri(a, b, c, n, n, n);
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 n) => Tri(a, b, c, n, n, n);

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var n = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(n, (a + b + c + d) / 4f) < 0f) n = -n;
                Tri(a, b, c, n);
                Tri(a, c, d, n);
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), na + nb + nc) < 0f)
                {
                    (b, c) = (c, b);
                    (nb, nc) = (nc, nb);
                }

                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                normales.Add(na); normales.Add(nb); normales.Add(nc);
                triangulos.Add(i); triangulos.Add(i + 1); triangulos.Add(i + 2);
            }

            public Mesh Construir(string nombre)
            {
                var malla = new Mesh { name = nombre };
                malla.SetVertices(vertices);
                malla.SetNormals(normales);
                malla.SetTriangles(triangulos, 0);
                malla.RecalculateBounds();
                return malla;
            }
        }
    }
}
