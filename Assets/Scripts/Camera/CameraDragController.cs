using UnityEngine;
using Verrarium.Core;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Verrarium.CameraControl
{
    /// <summary>
    /// Cho phép người chơi kéo thả bằng chuột để di chuyển camera.
    /// Attach script này lên Main Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraDragController : MonoBehaviour
    {
        [Header("Drag Settings")]
        [SerializeField] private float dragSpeed = 1f;
        [SerializeField] private KeyCode dragMouseButton = KeyCode.Mouse1;

        [Header("Zoom Settings")]
        [SerializeField] private bool enableScrollZoom = true;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minOrthographicSize = 3f;
        [SerializeField] private float maxOrthographicSize = 40f;
        [SerializeField] private float perspectiveZoomSpeed = 10f;

        [Header("Focus Settings")]
        [SerializeField] private float focusOrthographicSize = 12f;
        [SerializeField] private float focusFollowSpeed = 5f;
        [SerializeField] private bool focusSnapsImmediately = true;

        [Header("World Bounds")]
        [SerializeField] private bool clampToWorldBounds = true;
        [SerializeField] private float boundaryPadding = 2f;

        private Camera cam;
        private bool isDragging = false;
        private Vector3 lastMousePosition;
        private SimulationSupervisor supervisor;
        private Transform lockedTarget;
        private float targetFocusZoom;
        private bool snapFocusRuntime;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private ButtonControl dragButtonControl;
        private KeyCode cachedButton;
#endif

        private void Awake()
        {
            cam = GetComponent<Camera>();
            supervisor = SimulationSupervisor.Instance;
            targetFocusZoom = focusOrthographicSize;
            snapFocusRuntime = focusSnapsImmediately;

            // Ensure default drag button is right mouse if user hasn't set one.
            if (dragMouseButton == KeyCode.Mouse0)
            {
                dragMouseButton = KeyCode.Mouse1;
            }
        }

        private void Update()
        {
            HandleZoomInput();
            HandleDragInput();
            HandleLockedTargetFollow();
        }

        private void HandleZoomInput()
        {
            if (!enableScrollZoom || cam == null || lockedTarget != null)
                return;

            float scrollDelta = 0f;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Mouse.current != null)
                scrollDelta = Mouse.current.scroll.ReadValue().y;
#else
            scrollDelta = Input.mouseScrollDelta.y;
#endif

            if (Mathf.Approximately(scrollDelta, 0f))
                return;

            if (cam.orthographic)
            {
                float newSize = Mathf.Clamp(
                    cam.orthographicSize - scrollDelta * zoomSpeed,
                    minOrthographicSize,
                    maxOrthographicSize);
                cam.orthographicSize = newSize;
            }
            else
            {
                float move = scrollDelta * zoomSpeed * perspectiveZoomSpeed * Time.deltaTime;
                transform.Translate(transform.forward * move, Space.World);
            }

            ApplyWorldBounds();
        }

        private void HandleDragInput()
        {
            if (lockedTarget != null)
                return;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Mouse.current != null)
            {
                if (dragButtonControl == null || cachedButton != dragMouseButton)
                {
                    dragButtonControl = GetMouseButtonControl(dragMouseButton);
                    cachedButton = dragMouseButton;
                }

                if (dragButtonControl != null)
                {
                    if (dragButtonControl.wasPressedThisFrame)
                    {
                        isDragging = true;
                        lastMousePosition = GetMouseScreenPosition();
                    }
                    else if (dragButtonControl.wasReleasedThisFrame)
                    {
                        isDragging = false;
                    }
                }
            }
#else
            if (Input.GetKeyDown(dragMouseButton))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetKeyUp(dragMouseButton))
            {
                isDragging = false;
            }
#endif

            if (!isDragging)
                return;

            Vector3 currentMousePosition = GetMouseScreenPosition();
            Vector3 delta = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;

            if (cam.orthographic)
            {
                // Chuyển đổi pixel movement thành world units
                float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
                Vector3 movement = new Vector3(-delta.x * unitsPerPixel, -delta.y * unitsPerPixel, 0f);
                transform.Translate(movement * dragSpeed, Space.World);
            }
            else
            {
                // Fallback cho camera perspective (ít dùng trong project này)
                Vector3 lastWorld = cam.ScreenToWorldPoint(new Vector3(currentMousePosition.x, currentMousePosition.y, cam.nearClipPlane));
                Vector3 newWorld = cam.ScreenToWorldPoint(new Vector3(currentMousePosition.x + delta.x, currentMousePosition.y + delta.y, cam.nearClipPlane));
                Vector3 movement = lastWorld - newWorld;
                movement.z = 0f;
                transform.Translate(movement * dragSpeed, Space.World);
            }

            ApplyWorldBounds();
        }

        private void HandleLockedTargetFollow()
        {
            if (lockedTarget == null || cam == null)
                return;

            Vector3 desiredPosition = lockedTarget.position;
            desiredPosition.z = transform.position.z;

            float lerp = focusFollowSpeed * Time.deltaTime;
            transform.position = snapFocusRuntime ? desiredPosition : Vector3.Lerp(transform.position, desiredPosition, lerp);

            if (cam.orthographic)
            {
                float desiredSize = Mathf.Clamp(targetFocusZoom, minOrthographicSize, maxOrthographicSize);
                cam.orthographicSize = snapFocusRuntime
                    ? desiredSize
                    : Mathf.Lerp(cam.orthographicSize, desiredSize, lerp);
            }

            ApplyWorldBounds();
        }

        private void ApplyWorldBounds()
        {
            if (!clampToWorldBounds || supervisor == null)
                return;

            Vector3 position = transform.position;
            Vector2 worldSize = supervisor.WorldSize;

            float halfWidth = worldSize.x / 2f;
            float halfHeight = worldSize.y / 2f;

            position.x = Mathf.Clamp(position.x, -halfWidth + boundaryPadding, halfWidth - boundaryPadding);
            position.y = Mathf.Clamp(position.y, -halfHeight + boundaryPadding, halfHeight - boundaryPadding);

            transform.position = position;
        }

        private Vector3 GetMouseScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Mouse.current != null)
            {
                Vector2 pos = Mouse.current.position.ReadValue();
                return new Vector3(pos.x, pos.y, 0f);
            }
            return Vector3.zero;
#else
            return Input.mousePosition;
#endif
        }

        public void FocusOnTarget(Transform target, float customZoom = -1f, bool snapImmediately = true)
        {
            if (cam == null)
                return;

            if (target == null)
            {
                ReleaseLock();
                return;
            }

            lockedTarget = target;
            targetFocusZoom = customZoom > 0f ? customZoom : focusOrthographicSize;
            snapFocusRuntime = snapImmediately;
            isDragging = false;
            if (snapFocusRuntime && lockedTarget != null)
            {
                Vector3 desiredPosition = lockedTarget.position;
                desiredPosition.z = transform.position.z;
                transform.position = desiredPosition;

                if (cam.orthographic)
                    cam.orthographicSize = Mathf.Clamp(targetFocusZoom, minOrthographicSize, maxOrthographicSize);

                ApplyWorldBounds();
            }
        }

        public void ReleaseLock()
        {
            lockedTarget = null;
            snapFocusRuntime = focusSnapsImmediately;
        }

        public bool IsLocked => lockedTarget != null;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private ButtonControl GetMouseButtonControl(KeyCode key)
        {
            if (Mouse.current == null) return null;

            switch (key)
            {
                case KeyCode.Mouse0:
                    return Mouse.current.leftButton;
                case KeyCode.Mouse1:
                    return Mouse.current.rightButton;
                case KeyCode.Mouse2:
                    return Mouse.current.middleButton;
                default:
                    return Mouse.current.leftButton; // fallback
            }
        }
#endif
    }
}

