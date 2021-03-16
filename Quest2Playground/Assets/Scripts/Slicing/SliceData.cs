using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Slicing
{
    public enum MeshSide
    {
        Positive = 0,
        Negative = 1
    }

    public struct SliceVertex
    {
        public Vector3 position;
        public int index;
        public Vector2 uv;
        public Vector3? normal;
        public bool side;
        public int newIndex;
    }

    public class SliceData
    {
        Mesh positiveMesh;
        List<Vector3> positiveVertices;
        List<int> positiveTriangles;
        List<Vector2> positiveUVs;
        List<Vector3> positiveNormals;

        Mesh negativeMesh;
        List<Vector3> negativeVertices;
        List<int> negativeTriangles;
        List<Vector2> negativeUVs;
        List<Vector3> negativeNormals;

        List<Vector3> pointsOnPlane;
        Plane slicePlane;
        Mesh origMesh;
        bool shareVertices;

        public bool isSolid;

        public Mesh PositiveMesh
        {
            get
            {
                SetMeshData(MeshSide.Positive);
                return positiveMesh;
            }
        }

        public Mesh NegativeMesh
        {
            get
            {
                SetMeshData(MeshSide.Negative);
                return negativeMesh;
            }
        }

        public SliceData(Plane plane, Mesh mesh, bool isSolid, bool shareVertices)
        {
            positiveMesh = new Mesh();
            positiveVertices = new List<Vector3>();
            positiveTriangles = new List<int>();
            positiveUVs = new List<Vector2>();
            positiveNormals = new List<Vector3>();

            negativeMesh = new Mesh();
            negativeVertices = new List<Vector3>();
            negativeTriangles = new List<int>();
            negativeUVs = new List<Vector2>();
            negativeNormals = new List<Vector3>();

            pointsOnPlane = new List<Vector3>();
            slicePlane = plane;
            origMesh = mesh;
            this.shareVertices = shareVertices;

            this.isSolid = isSolid;

            ComputeNewMeshes();
        }

        void ComputeNewMeshes()
        {
            Vector3[] meshVerts = origMesh.vertices;
            int[] meshTriangles = origMesh.triangles;
            Vector3[] meshNormals = origMesh.normals;
            Vector2[] meshUVs = origMesh.uv;

            for (int i = 0; i < meshTriangles.Length; i += 3)
            {
                SliceVertex v1 = new SliceVertex();
                v1.position = meshVerts[meshTriangles[i]];
                v1.index = Array.IndexOf(meshVerts, v1.position);
                v1.uv = meshUVs[v1.index];
                v1.normal = meshNormals[v1.index];
                v1.side = slicePlane.GetSide(v1.position);

                SliceVertex v2 = new SliceVertex();
                v2.position = meshVerts[meshTriangles[i + 1]];
                v2.index = Array.IndexOf(meshVerts, v2.position);
                v2.uv = meshUVs[v2.index];
                v2.normal = meshNormals[v2.index];
                v2.side = slicePlane.GetSide(v2.position);

                SliceVertex v3 = new SliceVertex();
                v3.position = meshVerts[meshTriangles[i + 2]];
                v3.index = Array.IndexOf(meshVerts, v3.position);
                v3.uv = meshUVs[v3.index];
                v3.normal = meshNormals[v3.index];
                v3.side = slicePlane.GetSide(v3.position);

                if (v1.side == v2.side && v2.side == v3.side)
                {
                    MeshSide side = v1.side ? MeshSide.Positive : MeshSide.Negative;
                    AddTriangle(side, ref v1, ref v2, ref v3, true);
                }
                else
                {
                    SliceVertex intersection1;
                    SliceVertex intersection2;

                    /*
                    v1.normal = null;
                    v2.normal = null;
                    v3.normal = null;
                    */

                    MeshSide side1 = v1.side ? MeshSide.Positive : MeshSide.Negative;
                    MeshSide side2 = v1.side ? MeshSide.Negative : MeshSide.Positive;

                    if(v1.side == v2.side)
                    {
                        intersection1 = GetRayPlaneIntersectionPointAndUV(ref v2, ref v3);
                        intersection2 = GetRayPlaneIntersectionPointAndUV(ref v3, ref v1);

                        AddTriangle(side1, ref v1, ref v2, ref intersection1, shareVertices);
                        AddTriangle(side1, ref v1, ref intersection1, ref intersection2, shareVertices);

                        AddTriangle(side2, ref intersection1, ref v3, ref intersection2, shareVertices);
                    }
                    else if(v1.side == v3.side)
                    {
                        intersection1 = GetRayPlaneIntersectionPointAndUV(ref v1, ref v2);
                        intersection2 = GetRayPlaneIntersectionPointAndUV(ref v2, ref v3);

                        AddTriangle(side1, ref v1, ref intersection1, ref v3, shareVertices);
                        AddTriangle(side1, ref intersection1, ref intersection2, ref v3, shareVertices);

                        AddTriangle(side2, ref intersection1, ref v2, ref intersection2, shareVertices);
                    }
                    else
                    {
                        intersection1 = GetRayPlaneIntersectionPointAndUV(ref v1, ref v2);
                        intersection2 = GetRayPlaneIntersectionPointAndUV(ref v1, ref v3);

                        AddTriangle(side1, ref v1, ref intersection1, ref intersection2, shareVertices);

                        AddTriangle(side2, ref intersection1, ref v2, ref v3, shareVertices);
                        AddTriangle(side2, ref intersection1, ref v3, ref intersection2, shareVertices);
                    }

                    pointsOnPlane.Add(intersection1.position);
                    pointsOnPlane.Add(intersection2.position);
                }
            }

            if(isSolid)
            {
                JoinPointsAlongPlane();
            }
        }

        void JoinPointsAlongPlane()
        {
            SliceVertex midpoint = new SliceVertex();
            midpoint.position = GetHalfwayPoint(out float distance);
            midpoint.uv = Vector2.zero;

            for (int i = 0; i < pointsOnPlane.Count; i += 2)
            {
                SliceVertex firstVertex = new SliceVertex();
                firstVertex.position = pointsOnPlane[i];
                firstVertex.uv = Vector2.zero;

                SliceVertex secondVertex = new SliceVertex();
                secondVertex.position = pointsOnPlane[i + 1];
                secondVertex.uv = Vector2.zero;

                Vector3 normal = ComputeNormal(midpoint.position, secondVertex.position, firstVertex.position);
                normal.Normalize();

                float direction = Vector3.Dot(normal, slicePlane.normal);

                if(direction > 0)
                {
                    midpoint.normal = -normal;
                    firstVertex.normal = -normal;
                    secondVertex.normal = -normal;

                    AddTriangle(MeshSide.Positive, ref midpoint, ref firstVertex, ref secondVertex, false);

                    midpoint.normal = normal;
                    firstVertex.normal = normal;
                    secondVertex.normal = normal;

                    AddTriangle(MeshSide.Negative, ref midpoint, ref secondVertex, ref firstVertex, false);
                }
                else
                {
                    midpoint.normal = normal;
                    firstVertex.normal = normal;
                    secondVertex.normal = normal;

                    AddTriangle(MeshSide.Positive, ref midpoint, ref secondVertex, ref firstVertex, false);

                    midpoint.normal = -normal;
                    firstVertex.normal = -normal;
                    secondVertex.normal = -normal;

                    AddTriangle(MeshSide.Negative, ref midpoint, ref firstVertex, ref secondVertex, false);
                }
            }
        }

        Vector3 GetHalfwayPoint(out float distance)
        {
            if(pointsOnPlane.Count > 0)
            {
                Vector3 firstPoint = pointsOnPlane[0];
                Vector3 furthestPoint = Vector3.zero;
                distance = 0f;

                foreach(Vector3 point in pointsOnPlane)
                {
                    float curDistance = 0f;
                    curDistance = Vector3.Distance(firstPoint, point);

                    if(curDistance > distance)
                    {
                        distance = curDistance;
                        furthestPoint = point;
                    }
                }

                return Vector3.Lerp(firstPoint, furthestPoint, 0.5f);
            }
            else
            {
                distance = 0;
                return Vector3.zero;
            }
        }

        Vector2 InterpolateUVs(Vector2 uv1, Vector2 uv2, float distance)
        {
            return Vector2.Lerp(uv1, uv2, distance);
        }

        SliceVertex GetRayPlaneIntersectionPointAndUV(ref SliceVertex v1, ref SliceVertex v2)
        {
            SliceVertex intersection = new SliceVertex();

            float distance = GetDistanceRelativeToPlane(v1.position, v2.position, out Vector3 pointOfIntersection);

            intersection.position = pointOfIntersection;
            intersection.normal = Vector3.Lerp(v1.normal.GetValueOrDefault(), v2.normal.GetValueOrDefault(), distance);
            intersection.uv = InterpolateUVs(v1.uv, v2.uv, distance);

            return intersection;
        }

        float GetDistanceRelativeToPlane(Vector3 v1, Vector3 v2, out Vector3 pointOfIntersection)
        {
            Ray ray = new Ray(v1, v2 - v1);
            slicePlane.Raycast(ray, out float distance);
            pointOfIntersection = ray.GetPoint(distance);

            return distance;
        }

        void AddTriangle(MeshSide side, ref SliceVertex v1, ref SliceVertex v2, ref SliceVertex v3, bool vertShare)
        {
            if(side == MeshSide.Positive)
            {
                AddTriangleHelper(ref positiveVertices, ref positiveTriangles, ref positiveNormals, ref positiveUVs, ref v1, ref v2, ref v3, vertShare);
            }
            else
            {
                AddTriangleHelper(ref negativeVertices, ref negativeTriangles, ref negativeNormals, ref negativeUVs, ref v1, ref v2, ref v3, vertShare);
            }
        }

        void AddTriangleHelper(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, ref SliceVertex v1, ref SliceVertex v2, ref SliceVertex v3, bool vertShare)
        {
            if(v1.normal == null)
            {
                v1.normal = ComputeNormal(v1.position, v2.position, v3.position);
            }

            if(v2.normal == null)
            {
                v2.normal = ComputeNormal(v2.position, v3.position, v1.position);
            }

            if(v3.normal == null)
            {
                v3.normal = ComputeNormal(v3.position, v1.position, v2.position);
            }

            if(!vertShare)
            {
                AddVertex(ref vertices, ref triangles, ref normals, ref uvs, ref v1);
                AddVertex(ref vertices, ref triangles, ref normals, ref uvs, ref v2);
                AddVertex(ref vertices, ref triangles, ref normals, ref uvs, ref v3);
            }
            else
            {
                AddVertexShared(ref vertices, ref triangles, ref normals, ref uvs, ref v1);
                AddVertexShared(ref vertices, ref triangles, ref normals, ref uvs, ref v2);
                AddVertexShared(ref vertices, ref triangles, ref normals, ref uvs, ref v3);
            }
        }

        void AddVertex(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, ref SliceVertex vert)
        {
            vertices.Add(vert.position);
            vert.newIndex = vertices.Count - 1;
            triangles.Add(vert.newIndex);
            normals.Add(vert.normal.GetValueOrDefault(Vector3.zero));
            uvs.Add(vert.uv);
        }

        void AddVertexShared(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, ref SliceVertex vert)
        {
            vert.newIndex = vertices.IndexOf(vert.position);

            if(vert.newIndex == -1)
            {
                AddVertex(ref vertices, ref triangles, ref normals, ref uvs, ref vert);
            }
            else
            {
                triangles.Add(vert.newIndex);
            }
        }

        void SetMeshData(MeshSide side)
        {
            if (side == MeshSide.Positive)
            {
                positiveMesh.vertices = positiveVertices.ToArray();
                positiveMesh.triangles = positiveTriangles.ToArray();
                positiveMesh.normals = positiveNormals.ToArray();
                positiveMesh.uv = positiveUVs.ToArray();
            }
            else
            {
                negativeMesh.vertices = negativeVertices.ToArray();
                negativeMesh.triangles = negativeTriangles.ToArray();
                negativeMesh.normals = negativeNormals.ToArray();
                negativeMesh.uv = negativeUVs.ToArray();
            }
        }

        Vector3 ComputeNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vector3 side1 = v2 - v1;
            Vector3 side2 = v3 - v1;

            return Vector3.Cross(side1, side2);
        }
    }
}