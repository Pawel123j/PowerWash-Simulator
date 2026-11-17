using UnityEngine;

namespace Zmywak.Kitchen.Input
{
    /// <summary>
    /// Turns a raw pointer press/move/release stream into the gestures the kitchen loop cares about:
    /// a quick short drag (swipe), holding still (rinse), and holding while moving (scrub/rub).
    /// Pure C# (no MonoBehaviour, no Input System types) so it can be unit tested directly.
    /// </summary>
    public class TouchGestureRecognizer
    {
        private const float SwipeMinDistance = 60f;
        private const float SwipeMaxDuration = 0.35f;

        private Vector2 _pressStartPos;
        private Vector2 _lastPos;

        public bool IsPressed { get; private set; }
        public float HoldDuration { get; private set; }
        public float PointerSpeed { get; private set; }
        public Vector2 PreviousPosition { get; private set; }
        public bool SwipeDetectedThisFrame { get; private set; }

        /// <summary>Where the last press ended up against where it began (screen pixels).</summary>
        public Vector2 SwipeDelta { get; private set; }

        /// <summary>The last press ended in a quick flick upwards - the throw onto the rack.</summary>
        public bool FlickedUp => SwipeDetectedThisFrame && SwipeDelta.y > FlickMinRise && SwipeDelta.y > Mathf.Abs(SwipeDelta.x) * 0.8f;

        private const float FlickMinRise = 80f;

        public void PressBegin(Vector2 pos)
        {
            IsPressed = true;
            HoldDuration = 0f;
            PointerSpeed = 0f;
            SwipeDetectedThisFrame = false;
            _pressStartPos = pos;
            _lastPos = pos;
            PreviousPosition = pos;
        }

        public void PressStay(Vector2 pos, float deltaTime)
        {
            SwipeDetectedThisFrame = false;
            if (!IsPressed || deltaTime <= 0f)
            {
                return;
            }

            HoldDuration += deltaTime;
            PreviousPosition = _lastPos;
            PointerSpeed = Vector2.Distance(pos, _lastPos) / deltaTime;
            _lastPos = pos;
        }

        public void PressEnd(Vector2 pos)
        {
            float distance = Vector2.Distance(pos, _pressStartPos);
            SwipeDelta = pos - _pressStartPos;
            SwipeDetectedThisFrame = IsPressed && distance >= SwipeMinDistance && HoldDuration <= SwipeMaxDuration;
            IsPressed = false;
        }
    }
}
