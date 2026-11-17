using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Core.GameState;
using Zmywak.Core.Graphics;
using Zmywak.UI.Common;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// Renders a camera into an offscreen RenderTexture and saves a PNG to Logs/Screenshots - deliberately
    /// not UnityEngine.ScreenCapture, which needs an actual displayed frame buffer and does not produce
    /// reliable output in -batchmode (there is no window to capture).
    /// Screen Space - Overlay canvases are composited straight to the screen and never go through
    /// camera.Render(), so for the duration of the capture they are switched to Screen Space - Camera on
    /// the captured camera (and their CanvasScalers re-run against the capture's size); otherwise every
    /// HUD and menu would be missing from the images. Camera-space UI also receives the camera's
    /// post-processing, so UI colours in captures can look slightly tinted compared to the device.
    /// </summary>
    public static class TestScreenshot
    {
        private const float UiPlaneOffset = 0.2f;
        private static bool _clearedThisRun; // reset by the domain reload of each test run

        /// <param name="inspectLayout">Called while the UI is laid out for the capture's size, to check what
        /// the image shows (controls on screen, not overlapping) at that screen shape.</param>
        public static void Capture(Camera camera, string fileName, int width = 1080, int height = 1920,
            System.Action inspectLayout = null)
        {
            if (camera == null)
            {
                Debug.LogWarning($"TestScreenshot: camera is null, skipping capture of {fileName}");
                return;
            }

            var renderTexture = new RenderTexture(width, height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            camera.targetTexture = renderTexture;
            // The kitchen camera widens its view for tall screens at the end of a frame; the capture is
            // rendered before then, so fit it to the capture's shape now - as the phone would show it.
            CameraWidthFitter fitter = camera.GetComponent<CameraWidthFitter>();
            if (fitter != null)
            {
                fitter.Fit();
            }

            // A loading screen still fading out over a scene that has loaded (a test's step right after a scene change)
            // would cover the very thing being captured: it is left out of the picture. One still loading stays in.
            CanvasGroup fadingCover = null;
            float fadingAlpha = 0f;
            LoadingScreen loading = Object.FindAnyObjectByType<LoadingScreen>();
            if (loading != null && !SceneLoader.IsLoading)
            {
                fadingCover = loading.GetComponent<CanvasGroup>();
                fadingAlpha = fadingCover != null ? fadingCover.alpha : 0f;
                if (fadingCover != null)
                {
                    fadingCover.alpha = 0f;
                }
            }

            List<Canvas> switchedCanvases = MoveOverlayCanvasesOnto(camera);

            camera.Render();
            inspectLayout?.Invoke();

            RenderTexture.active = renderTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            camera.targetTexture = previousTarget;
            // Under URP the camera keeps the RenderTexture's aspect after targetTexture is restored, until
            // its next on-screen render. Any WorldToScreenPoint in between (the tests compute touch targets
            // right after a capture) would then use the portrait capture's projection against the actual
            // screen and miss. Reset it to follow the screen again immediately.
            camera.ResetAspect();
            if (fitter != null)
            {
                fitter.Fit();
            }

            RestoreOverlayCanvases(switchedCanvases);
            if (fadingCover != null)
            {
                fadingCover.alpha = fadingAlpha;
            }

            RenderTexture.active = previousActive;
            Object.Destroy(renderTexture);

            byte[] png = texture.EncodeToPNG();
            Object.Destroy(texture);

            string dir = Path.Combine(Application.dataPath, "..", "Logs", "Screenshots");
            Directory.CreateDirectory(dir);
            if (!_clearedThisRun)
            {
                // Old captures from earlier runs (possibly under names no test writes any more) would
                // otherwise sit next to the fresh ones and look current.
                _clearedThisRun = true;
                foreach (string old in Directory.GetFiles(dir, "*.png"))
                {
                    WithRetry(() => File.Delete(old), $"remove the old {Path.GetFileName(old)}");
                }
            }

            string path = Path.Combine(dir, fileName);
            WithRetry(() => File.WriteAllBytes(path, png), $"write {fileName}");
            var uiNames = new List<string>();
            foreach (Canvas canvas in switchedCanvases)
            {
                uiNames.Add($"{canvas.name} ({canvas.gameObject.scene.name})");
            }

            Debug.Log($"TestScreenshot: saved {path} - camera '{camera.name}' ({camera.gameObject.scene.name}), UI: {string.Join(", ", uiNames)}");
        }

        private static List<Canvas> MoveOverlayCanvasesOnto(Camera camera)
        {
            var switched = new List<Canvas>();
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                // Just in front of the scene but clearly past the near clip plane: exactly on it (the
                // kitchen camera's near plane is 1) half the UI quads got clipped away.
                canvas.planeDistance = camera.nearClipPlane + UiPlaneOffset;
                RerunScaler(canvas);
                switched.Add(canvas);
            }

            Canvas.ForceUpdateCanvases();
            return switched;
        }

        /// <summary>
        /// Screenshots are diagnostics, not what a test checks: a file held for a moment by another process -
        /// OneDrive syncing Logs/, an image viewer - is retried, then reported as a warning rather than failing
        /// the gameplay test that happened to take the picture.
        /// </summary>
        private static void WithRetry(System.Action fileOperation, string what)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    fileOperation();
                    return;
                }
                catch (IOException e) when (attempt < 5)
                {
                    System.Threading.Thread.Sleep(200 * attempt);
                    if (attempt == 4)
                    {
                        Debug.LogWarning($"TestScreenshot: could not {what} ({e.Message}); one last try.");
                    }
                }
                catch (IOException e)
                {
                    Debug.LogWarning($"TestScreenshot: gave up trying to {what}: {e.Message}");
                    return;
                }
            }
        }

        private static void RestoreOverlayCanvases(List<Canvas> canvases)
        {
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null)
                {
                    continue;
                }

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                RerunScaler(canvas);
            }

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>CanvasScaler only recomputes its scale factor in OnEnable/Update; toggling it forces
        /// that now, against the canvas's current render target.</summary>
        private static void RerunScaler(Canvas canvas)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.enabled)
            {
                scaler.enabled = false;
                scaler.enabled = true;
            }
        }
    }
}
