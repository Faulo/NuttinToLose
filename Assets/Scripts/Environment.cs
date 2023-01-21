using System.Collections.Generic;
using System.Linq;
using MyBox;
using NuttinToLose.Networking;
using Slothsoft.UnityExtensions;
using UnityEngine;

namespace NuttinToLose {
    sealed class Environment : MonoBehaviour {
        [SerializeField]
        GameObject fallInstance = default;
        [SerializeField]
        GameObject winterInstance = default;

        [Space]
        [SerializeField]
        ServerConnection server = default;

        void OnEnable() {
            server.onStateEnter += HandleState;
        }
        void OnDisable() {
            server.onStateEnter -= HandleState;
        }
        void HandleState(WorldState state) {
            switch (state) {
                case WorldState.Fall:
                case WorldState.HighScore:
                    fallInstance.SetActive(true);
                    winterInstance.SetActive(false);
                    break;
                case WorldState.Lobby:
                case WorldState.Winter:
                    fallInstance.SetActive(false);
                    winterInstance.SetActive(true);
                    break;
            }
        }
        void Start() {
            fallInstance.SetActive(false);
            winterInstance.SetActive(true);
        }
#if UNITY_EDITOR

        [Space]
        [SerializeField]
        bool alignPositionToTerrain = true;
        [SerializeField, ConditionalField(nameof(alignPositionToTerrain))]
        Vector3 positionOffset = Vector3.zero;
        [SerializeField]
        bool alignRotationToTerrain = false;
        [SerializeField, ConditionalField(nameof(alignRotationToTerrain))]
        Quaternion rotationOffset = Quaternion.identity;

        [SerializeField, ReadOnly]
        Bounds meshBounds = new();
        [SerializeField, ReadOnly]
        Bounds vertexBounds = new();
        MeshRenderer meshRenderer => fallInstance.GetComponent<MeshRenderer>();
        Mesh mesh => fallInstance.GetComponent<MeshFilter>().sharedMesh;

        void Awake() {
            OnValidate();
        }
        [ContextMenu(nameof(OnValidate))]
        void OnValidate() {
            if (!server) {
                server = FindObjectOfType<ServerConnection>();
            }
            fallInstance = transform.childCount > 0
                ? transform.GetChild(0).gameObject
                : null;
            winterInstance = transform.childCount > 1
                ? transform.GetChild(1).gameObject
                : null;
            SetupEnvironment(fallInstance);
            SetupEnvironment(winterInstance);

            rotationOffset = transform.localRotation;
        }
        public void SetupTransform() {
            if (gameObject.scene.isLoaded && fallInstance) {
                SetupBounds();
                SetupTerrain();
                SetupBounds();
            } else {
                meshBounds = default;
                transform.localPosition = Vector3.zero;
            }
        }
        void SetupEnvironment(GameObject obj) {
            if (!obj) {
                return;
            }
            obj.SetActive(true);
            obj.isStatic = true;
        }
        void SetupBounds() {
            meshBounds = meshRenderer.bounds;

            var vertices = mesh.vertices
                .Select(fallInstance.transform.TransformPoint)
                .ToList();

            float minY = vertices.Min(v => v.y);
            float maxY = vertices.Max(v => v.y);
            float centerY = (maxY + minY) * 0.5f;
            float extendsY = (maxY - minY) * 0.5f;

            vertexBounds.center = meshBounds.center;
            vertexBounds.extents = meshBounds.extents.WithY(meshBounds.center.y - minY);
        }
        void SetupTerrain() {
            if (alignPositionToTerrain) {
                float minY = GetBoundsPositions()
                    .Min(SampleTerrainHeight);

                minY += vertexBounds.extents.y;

                transform.localPosition = transform.localPosition.WithY(minY);
            }

            if (alignRotationToTerrain) {
                var normals = GetBoundsPositions()
                    .Select(SampleTerrainNormal)
                    .ToList();
                var normal = normals.Aggregate((a, b) => a + b) / normals.Count;
                transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal) * rotationOffset;
            }
        }
        IEnumerable<Vector3> GetBoundsPositions() {
            yield return meshBounds.center;
            yield return meshBounds.center + Vector3.Scale(meshBounds.extents, Vector3.down) + Vector3.Scale(meshBounds.extents, Vector3.forward);
            yield return meshBounds.center + Vector3.Scale(meshBounds.extents, Vector3.down) + Vector3.Scale(meshBounds.extents, Vector3.back);
            yield return meshBounds.center + Vector3.Scale(meshBounds.extents, Vector3.down) + Vector3.Scale(meshBounds.extents, Vector3.left);
            yield return meshBounds.center + Vector3.Scale(meshBounds.extents, Vector3.down) + Vector3.Scale(meshBounds.extents, Vector3.right);
        }
        static Vector3 SampleTerrainPosition(Vector3 position) {
            return position.WithY(SampleTerrainHeight(position));
        }
        static float SampleTerrainHeight(Vector3 position) {
            return GetTerrain(position).SampleHeight(position);
        }
        static Vector3 SampleTerrainNormal(Vector3 position) {
            var terrain = GetTerrain(position);
            var data = terrain.terrainData;
            var terrainSize = data.size;
            var terrainPosition = terrain.GetPosition();
            float x = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, position.x);
            float y = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, position.z);
            return data.GetInterpolatedNormal(x, y);
        }
        static Terrain GetTerrain(Vector3 position) {
            return Terrain.activeTerrains
                .OrderBy(terrain =>
                    new Vector2(
                        terrain.transform.position.x + (terrain.terrainData.size.x * 0.5f) - position.x,
                        terrain.transform.position.z + (terrain.terrainData.size.z * 0.5f) - position.z
                    ).sqrMagnitude
                )
                .First();
        }
        void OnDrawGizmos() {
            Gizmos.color = Color.red.WithAlpha(0.125f);
            Gizmos.DrawWireCube(meshBounds.center, meshBounds.size);
            Gizmos.color = Color.magenta.WithAlpha(0.125f);
            Gizmos.DrawWireCube(vertexBounds.center, vertexBounds.size);

            Gizmos.color = Color.green.WithAlpha(0.5f);
            foreach (var position in GetBoundsPositions().Select(SampleTerrainPosition)) {
                Gizmos.DrawSphere(position, 0.25f);
                Gizmos.DrawLine(position, position + SampleTerrainNormal(position));
            }
        }
        [UnityEditor.CustomEditor(typeof(Environment))]
        class EnvironmentEditor : RuntimeEditorTools<Environment> {
            protected override void DrawEditorTools() {
                DrawButton("Update Transform", target.SetupTransform);
                DrawButton("Remove MeshCollider", () => {
                    target.RemoveCollider(target.fallInstance);
                    target.RemoveCollider(target.winterInstance);
                });
                DrawButton("Update static flags", () => {
                    target.UpdateStaticFlags(target.fallInstance);
                    target.UpdateStaticFlags(target.winterInstance);
                });
            }
        }
        void RemoveCollider(GameObject obj) {
            if (!obj) {
                return;
            }
            if (obj.TryGetComponent<MeshCollider>(out var collider)) {
                DestroyImmediate(collider);
            }
        }
        void UpdateStaticFlags(GameObject obj) {
            if (!obj) {
                return;
            }
            if (!obj.isStatic) {
                obj.isStatic = true;
                UnityEditor.EditorUtility.SetDirty(obj);
            }
#if UNITY_2021
            if (obj.TryGetComponent<Renderer>(out var renderer)) {
                if (!renderer.staticShadowCaster) {
                    renderer.staticShadowCaster = true;
                    UnityEditor.EditorUtility.SetDirty(renderer);
                }
            }
#endif
        }
#endif
    }
}