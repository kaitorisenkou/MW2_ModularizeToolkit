using ModularWeapons2;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace MW2_ModularizeToolkit {
    public class MW2MTK_CameraRenderer : MonoBehaviour {
        public static void Render(RenderTexture renderTexture, params MW2MTKCameraRequest[] requests) {
            float orthographicSize = camera.orthographicSize;
            cameraRenderer.requestsInt = requests;
            camera.SetTargetBuffers(renderTexture.colorBuffer, renderTexture.depthBuffer);
            camera.Render();
            cameraRenderer.requestsInt = null;
            camera.orthographicSize = orthographicSize;
            camera.targetTexture = null;
        }
        MW2MTKCameraRequest[] requestsInt = null;
        public void OnPostRender() {
            foreach (var i in requestsInt.OrderBy(t => t.layerOrder)) {
                var matrix = new Matrix4x4();
                matrix.SetTRS(i.offset, Quaternion.identity, Vector3.one);
                GenDraw.DrawMeshNowOrLater(MeshMakerPlanes.NewPlaneMesh(1f, false), Quaternion.Euler(90, 0, 0) * i.offset, Quaternion.identity, i.material, true);
            }
        }

        private static Camera camera = InitCamera();
        private static MW2MTK_CameraRenderer cameraRenderer;
        private static Camera InitCamera() {
            GameObject gameObject = new GameObject("MW2MTKCamera", new Type[] { typeof(Camera) });
            gameObject.SetActive(false);
            gameObject.AddComponent<MW2MTK_CameraRenderer>();
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            Camera component = gameObject.GetComponent<Camera>();
            component.transform.position = new Vector3(0f, 10f, 0f);
            component.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            component.orthographic = true;
            component.cullingMask = 0;
            component.orthographicSize = 1f;
            component.clearFlags = CameraClearFlags.Color;
            component.backgroundColor = new Color(0f, 0f, 0f, 0f);
            component.useOcclusionCulling = false;
            component.renderingPath = RenderingPath.Forward;
            component.nearClipPlane = 5f;
            component.farClipPlane = 12f;

            cameraRenderer = gameObject.GetComponent<MW2MTK_CameraRenderer>();
            return component;
        }
    }
    public struct MW2MTKCameraRequest {
        public Material material;
        public Vector2 offset;
        public int layerOrder;
        public MW2MTKCameraRequest(Material material, Vector2 offset, int layerOrder) {
            this.material = material;
            this.offset = offset;
            this.layerOrder = layerOrder;
        }
    }
}
