using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Kitchen.Input
{
    /// <summary>
    /// Single point of contact with the Input System for the kitchen scene. Raycasts under the pointer,
    /// claims whichever dish was pressed first (a soaking one comes back out of the water), and forwards gesture
    /// updates to it until release. A drag that
    /// starts beside a glass being held to the light (and no tilt sensor steering it) moves its reflection.
    /// </summary>
    public class DishInputRouter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float maxRayDistance = 100f;
        [SerializeField] private WashStation washStation;

        // A drag across half the screen's width moves the reflection across the whole glass.
        private const float SteerScreenFraction = 0.5f;

        private readonly TouchGestureRecognizer _recognizer = new TouchGestureRecognizer();
        private DishController _activeDish;
        private readonly HashSet<object> _blockers = new HashSet<object>();
        private bool _waitForRelease;
        private DishController _steeringGlass;
        private float _steerLastX;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (washStation == null)
            {
                washStation = FindAnyObjectByType<WashStation>();
            }
        }

        /// <summary>
        /// While anything blocks it (a panel covering the kitchen - the repair card, the pause menu, several at
        /// once), touches do not reach the dishes and a gesture in progress is ended. After the last blocker
        /// goes, the finger must lift first - otherwise the finger that closed the panel would grab whatever dish
        /// lies underneath it.
        /// </summary>
        public void AddBlocker(object blocker)
        {
            if (!_blockers.Add(blocker) || _activeDish == null)
            {
                return;
            }

            // The panel took over: end the gesture in progress as if the finger lifted.
            Vector2 screenPos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
            _recognizer.PressEnd(screenPos);
            _activeDish.OnGestureEnd(_recognizer, screenPos);
            _activeDish = null;
        }

        public void RemoveBlocker(object blocker)
        {
            if (_blockers.Remove(blocker) && _blockers.Count == 0)
            {
                _waitForRelease = true;
            }
        }

        public bool IsBlocked => _blockers.Count > 0;

        /// <summary>
        /// Whether a screen point is on an on-screen control - set by the HUD, which knows its buttons (the kitchen
        /// does not depend on the UI). A touch that starts on a button belongs to the button: without this, pressing
        /// one drawn over the counter also grabbed whatever dish lay under it, since dishes are found by a ray.
        /// </summary>
        public static System.Func<Vector2, bool> IsOverControl;

        /// <summary>The dish the finger is working on right now (null between gestures).</summary>
        public DishController ActiveDish => _activeDish;

        /// <summary>The glass whose reflection a drag beside it is sliding, if one is.</summary>
        public DishController SteeringGlass => _steeringGlass;

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            Vector2 screenPos = pointer.position.ReadValue();
            // Asked every frame, so the Input System tracks presses within a frame: a tap that went down and up between
            // two frames leaves isPressed false, and on a slow frame it took nothing (seen on the emulator at 30 fps).
            bool pressedThisFrame = pointer.press.wasPressedThisFrame;
            bool isPressed = pointer.press.isPressed;

            if (IsBlocked)
            {
                return;
            }

            if (_waitForRelease)
            {
                _waitForRelease = isPressed;
                return;
            }

            if (!isPressed)
            {
                _steeringGlass = null;
            }

            if (isPressed && _steeringGlass != null)
            {
                // Dragging beside a glass held to the light slides the reflection across it.
                _steeringGlass.SteerInspectionLight((screenPos.x - _steerLastX) / (Screen.width * SteerScreenFraction));
                _steerLastX = screenPos.x;
            }
            else if (isPressed && _activeDish == null)
            {
                if (IsOverControl != null && IsOverControl(screenPos))
                {
                    _waitForRelease = true; // the button's touch, until the finger lifts
                    return;
                }

                DishController hit = RaycastDish(screenPos);
                if (hit != null && hit.TryClaim()) // refused while another dish occupies the sink
                {
                    _activeDish = hit;
                    _recognizer.PressBegin(screenPos);
                }
                else if (hit == null && washStation != null && washStation.Occupant != null && washStation.Occupant.IsInspecting &&
                         !DishController.TiltSteersReflection)
                {
                    _steeringGlass = washStation.Occupant;
                    _steerLastX = screenPos.x;
                }
            }
            else if (isPressed && _activeDish != null)
            {
                _recognizer.PressStay(screenPos, Time.deltaTime);
                _activeDish.OnGestureUpdate(_recognizer, screenPos);
            }
            else if (!isPressed && _activeDish != null)
            {
                _recognizer.PressEnd(screenPos);
                _activeDish.OnGestureEnd(_recognizer, screenPos);
                _activeDish = null;
            }
            else if (pressedThisFrame && !isPressed && _activeDish == null)
            {
                TapWithinFrame(screenPos);
            }
        }

        /// <summary>A whole tap between two frames: what a press and its release would have done, at once.</summary>
        private void TapWithinFrame(Vector2 screenPos)
        {
            if (IsOverControl != null && IsOverControl(screenPos))
            {
                return; // the button's tap
            }

            DishController hit = RaycastDish(screenPos);
            if (hit == null || !hit.TryClaim())
            {
                return;
            }

            _recognizer.PressBegin(screenPos);
            _recognizer.PressEnd(screenPos);
            hit.OnGestureEnd(_recognizer, screenPos);
        }

        private DishController RaycastDish(Vector2 screenPos)
        {
            if (targetCamera == null)
            {
                return null;
            }

            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            DishController held = washStation != null ? washStation.Occupant : null;
            if (held != null && held.TryGetWashContact(screenPos, out _, out _))
            {
                return held;
            }
            DishController dish = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance)
                ? hit.collider.GetComponentInParent<DishController>()
                : null;

            // A dish soaking on the bottom of the sink lies below the counter's top, whose collider spans the sink's
            // hole: the ray stops there first. It is looked for on its own, under anything else.
            DishController soaking = washStation != null ? washStation.Soaking : null;
            if (dish == null && soaking != null && soaking.Collider != null && soaking.Collider.Raycast(ray, out _, maxRayDistance))
            {
                dish = soaking;
            }

            return dish;
        }
    }
}
