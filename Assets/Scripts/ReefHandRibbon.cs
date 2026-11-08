using UnityEngine;
using UnityEngine.Rendering;

namespace UnderTheSea
{
    /// <summary>Draws the existing Tag 2 tentacle points as a continuous jellyfish ribbon.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ReefHandRibbon : MonoBehaviour
    {
        public Transform[] points;
        public Camera viewer;
        Mesh mesh;
        MeshRenderer ribbonRenderer;
        Vector3[] vertices;

        void Awake()
        {
            mesh = new Mesh { name = "Tracked jellyfish hand ribbon" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            ribbonRenderer = GetComponent<MeshRenderer>();
            ribbonRenderer.shadowCastingMode = ShadowCastingMode.Off;
            ribbonRenderer.receiveShadows = false;
            vertices = new Vector3[points.Length * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(points.Length - 1) * 6];
            for (int i = 0; i < points.Length; i++)
            {
                uv[i*2] = new Vector2(0f, i / (float)(points.Length - 1));
                uv[i*2+1] = new Vector2(1f, i / (float)(points.Length - 1));
                if (i == points.Length - 1) continue;
                int k = i * 6, a = i * 2;
                triangles[k] = a; triangles[k+1] = a+2; triangles[k+2] = a+1;
                triangles[k+3] = a+1; triangles[k+4] = a+2; triangles[k+5] = a+3;
            }
            mesh.vertices = vertices;
            mesh.uv = uv; mesh.triangles = triangles;
        }

        void LateUpdate()
        {
            bool show = points.Length > 1 && points[0].gameObject.activeInHierarchy;
            ribbonRenderer.enabled = show;
            if (!show) return;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i].position;
                Vector3 tangent = points[Mathf.Min(i+1, points.Length-1)].position
                    - points[Mathf.Max(i-1, 0)].position;
                Vector3 towardEye = viewer ? viewer.transform.position - p : Vector3.forward;
                Vector3 side = Vector3.Cross(tangent, towardEye).normalized;
                if (side.sqrMagnitude < .01f) side = Vector3.right;
                float width = points[i].localScale.x * .5f;
                vertices[i*2] = transform.InverseTransformPoint(p - side * width);
                vertices[i*2+1] = transform.InverseTransformPoint(p + side * width);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }

        void OnDestroy() { if (mesh) Destroy(mesh); }
    }
}
