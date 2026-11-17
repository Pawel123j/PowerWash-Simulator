using UnityEngine;
using UnityEngine.InputSystem;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Input;
using Zmywak.Kitchen.Tools;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>
    /// Kuba's hand in a blue nitrile glove, doing what the finger on the screen does: reaching in from the
    /// bottom of the view to where the finger is, gripping a dish to take it from the pile or carry it to the
    /// rack, pressing it flat under the tap, scrubbing with whichever tool is picked - sponge, steel wool, the
    /// soapy brush, the scraper, the spray - and pointing when a drag beside a glass slides its reflection. Pure feedback: it reads the
    /// input router and never touches the game, and it has no colliders, so it can never catch a tap.
    /// The rig is built by the scene setup (KitchenArt.BuildHand); poses are finger curls blended over time.
    /// </summary>
    public class GlovedHand : MonoBehaviour
    {
        private enum Pose
        {
            Hidden,
            Grab,
            Flat,
            Point,
            Sponge,
            SteelWool,
            Brush,
            Scraper,
            Shower
        }

        // Curl angles (degrees) of each finger's three joints and the thumb's (yaw, joint 1, joint 2) per pose.
        private static readonly Vector3[] FingerCurl =
        {
            new Vector3(10f, 10f, 5f),   // Hidden (relaxed)
            new Vector3(42f, 38f, 22f),  // Grab
            new Vector3(6f, 6f, 4f),     // Flat
            new Vector3(80f, 92f, 60f),  // Point (index overridden below)
            new Vector3(38f, 34f, 22f),  // Sponge (fingertips over its back edge)
            new Vector3(52f, 48f, 32f),  // SteelWool
            new Vector3(82f, 92f, 62f),  // Brush (a fist round the handle)
            new Vector3(78f, 90f, 60f),  // Scraper (the same, pushing)
            new Vector3(74f, 86f, 58f)   // Shower (round the grip, squeezing the lever)
        };

        private static readonly Vector3[] ThumbCurl =
        {
            new Vector3(-40f, 12f, 10f),
            new Vector3(-68f, 30f, 22f),
            new Vector3(-35f, 8f, 6f),
            new Vector3(-70f, 40f, 30f),
            // On a sponge or a wad of steel wool it presses down along the side of it, not out to the left.
            new Vector3(-12f, 34f, 20f),
            new Vector3(8f, 42f, 30f),
            // Round a handle the thumb closes over the curled fingers (a negative yaw swings it outwards, away from
            // the palm - it stuck out sideways like a sixth finger).
            new Vector3(32f, 30f, 24f),
            new Vector3(28f, 30f, 22f),
            new Vector3(26f, 28f, 22f)
        };

        // Where the hand sits relative to the contact point for a tool held by a handle: behind the head that does
        // the work - for the spray well back and up, on the grip of a head aimed at the spot from there.
        private static readonly Vector3 BrushGrip = new Vector3(0f, 0.03f, -0.085f);

        // The sponge and the steel wool are held by their back edge, the hand behind them - down the screen, under the player's
        // own finger - so the spot being scrubbed, and the dirt ahead of it, stay in sight instead of under a glove.
        private static readonly Vector3 SpongeGrip = new Vector3(0.008f, 0.012f, -0.05f);
        private static readonly Vector3 WoolGrip = new Vector3(0.006f, 0.014f, -0.042f);

        /// <summary>While scrubbing, the hand is drawn this much smaller than when carrying a dish: it shows the work, not itself.</summary>
        private const float ScrubbingScale = 0.88f;
        private static readonly Vector3 ScraperGrip = new Vector3(0f, 0.025f, -0.095f);
        private static readonly Vector3 ShowerGrip = new Vector3(0f, 0.098f, -0.18f);

        [SerializeField] private DishInputRouter router;
        [SerializeField] private WashStation washStation;
        [SerializeField] private Camera viewCamera;

        [Tooltip("The glove, sleeve and fingers; moved back along the handle for the brush.")]
        [SerializeField] private Transform hand;
        [Tooltip("Index, middle, ring, little finger - three joints each, knuckle first.")]
        [SerializeField] private Transform[] fingerJoints = new Transform[12];
        [Tooltip("Thumb base, then its two joints.")]
        [SerializeField] private Transform[] thumbJoints = new Transform[3];
        [SerializeField] private GameObject sponge;
        [SerializeField] private GameObject steelWool;
        [SerializeField] private GameObject brush;
        [SerializeField] private GameObject scraper;
        [SerializeField] private GameObject shower;
        [Tooltip("The spray head's face: where the shower's water comes out (KitchenFx aims it from here).")]
        [SerializeField] private Transform sprayNozzle;
        [Tooltip("The cone of water from the nozzle to the dish, shown only while the spray is held on one.")]
        [SerializeField] private GameObject sprayJet;

        [Tooltip("Hand size (real metres -> scene units) at a pile dish's scale; it grows with the dish held in the sink.")]
        [SerializeField] private float baseScale = 3.3f;

        // A plate's width in the scene: the hand is sized for plates and pans, smaller for narrower dishes.
        private const float ReferenceDishWidth = 0.9f;
        [SerializeField] private float followSharpness = 16f;
        [SerializeField] private float poseSharpness = 12f;

        private Quaternion[] _fingerBind;
        private Quaternion[] _thumbBind;
        private Vector3 _handBind;
        private Vector3 _curl;
        private Vector3 _thumb;
        private float _indexCurlBlend; // 1 = index straight (pointing)
        private float _presence;
        private float _scale;
        private Vector3 _contact;
        private Vector2 _lastPointer;
        private float _scrubPhase;
        private float _scrubEnergy;
        private Pose _pose = Pose.Hidden;

        private bool _wanted;

        /// <summary>The spray head's nozzle while the shower is held on a dish (not while the hand pulls away), otherwise null.</summary>
        public Transform SprayNozzle => _wanted && _pose == Pose.Shower && sprayNozzle != null ? sprayNozzle : null;

        /// <summary>Where the finger touches the dish (what the spray is aimed at).</summary>
        public Vector3 Contact => _contact;

        /// <summary>0-1: how hard the hand is working at a spot right now (HandWear warms the skin with it).</summary>
        public float ScrubEnergy => _scrubEnergy;

        private HandWear _wear;

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("GlovedHand.Awake");
            _wear = GetComponent<HandWear>();
            router = router != null ? router : FindAnyObjectByType<DishInputRouter>();
            washStation = washStation != null ? washStation : FindAnyObjectByType<WashStation>();
            viewCamera = viewCamera != null ? viewCamera : Camera.main;

            _fingerBind = new Quaternion[fingerJoints.Length];
            for (int i = 0; i < fingerJoints.Length; i++)
            {
                _fingerBind[i] = fingerJoints[i] != null ? fingerJoints[i].localRotation : Quaternion.identity;
            }

            _thumbBind = new Quaternion[thumbJoints.Length];
            for (int i = 0; i < thumbJoints.Length; i++)
            {
                _thumbBind[i] = thumbJoints[i] != null ? thumbJoints[i].localRotation : Quaternion.identity;
            }

            _handBind = hand != null ? hand.localPosition : Vector3.zero;
            _curl = FingerCurl[0];
            _thumb = ThumbCurl[0];
            _scale = baseScale;
            ShowTool(null);
            SetVisible(false);
        }

        private void LateUpdate()
        {
            Pose pose = Decide(out Vector3 contact, out float dishScale, out Vector2 pointer);
            float dt = Time.unscaledDeltaTime;

            bool wanted = pose != Pose.Hidden;
            _wanted = wanted;
            _presence = Mathf.MoveTowards(_presence, wanted ? 1f : 0f, dt * (wanted ? 6f : 4f));
            if (_presence <= 0f)
            {
                SetVisible(false);
                _pose = Pose.Hidden;
                return;
            }

            SetVisible(true);
            if (wanted)
            {
                if (_pose == Pose.Hidden)
                {
                    // Appear where the finger is and at the size for this dish - not sliding over from where it
                    // last was, nor shrinking from the size it had over a dish held in the sink.
                    _contact = contact;
                    _scale = baseScale * dishScale;
                    _lastPointer = pointer;
                }

                _pose = pose;
                // The working tool is the aiming feedback: lagging behind the brush makes a fast rub
                // look as if it cleans the wrong spot. Only carrying/rinsing uses the softer follow.
                _contact = pose >= Pose.Sponge ? contact :
                    Vector3.Lerp(_contact, contact, 1f - Mathf.Exp(-followSharpness * DishController.ScrubStrength * dt));
                float wantedScale = baseScale * dishScale * (pose >= Pose.Sponge ? ScrubbingScale : 1f);
                _scale = Mathf.Lerp(_scale, wantedScale, 1f - Mathf.Exp(-followSharpness * dt));
            }

            // Scrubbing: small circles while the finger moves, like a hand working at a spot.
            float moved = (pointer - _lastPointer).magnitude / Mathf.Max(1f, Screen.height);
            _lastPointer = pointer;
            bool scrubbing = _pose >= Pose.Sponge;
            _scrubEnergy = Mathf.MoveTowards(_scrubEnergy, scrubbing && moved > 0.0005f ? 1f : 0f, dt * 4f);
            _scrubPhase += dt * 13f;
            var wobble = new Vector3(Mathf.Cos(_scrubPhase), 0f, Mathf.Sin(_scrubPhase)) * (0.006f * _scrubEnergy);

            // A worn-out hand is never quite still: a fine shake at 8-11 Hz, like the real thing, growing as he tires.
            float tremor = _wear != null ? _wear.Current.Tremor : 0f;
            Vector3 shake = Vector3.zero;
            if (tremor > 0f)
            {
                float time = Time.unscaledTime;
                shake = new Vector3(Mathf.Sin(time * 57.1f) + 0.5f * Mathf.Sin(time * 71.3f + 1.3f), 0f,
                    Mathf.Sin(time * 63.7f + 0.7f) + 0.4f * Mathf.Sin(time * 49.9f)) * (0.0014f * tremor);
            }

            // Pulled back out of view as it goes: down the screen and towards the camera.
            Vector3 retreat = new Vector3(0.12f, 0.35f, -0.45f) * ((1f - _presence) * _scale);
            DishController workingDish = router != null ? router.ActiveDish : null;
            Quaternion surfaceRotation = scrubbing && workingDish != null ? workingDish.transform.rotation : Quaternion.identity;
            transform.position = _contact + surfaceRotation * ((wobble + shake) * _scale) + retreat;
            transform.localScale = Vector3.one * _scale;
            transform.rotation = surfaceRotation * Quaternion.Euler(0f, -14f + Mathf.Sin(_scrubPhase * 0.5f) * 4f * _scrubEnergy, 0f);

            BlendPose(dt);
            ShowTool(_pose);

            if (sprayJet != null)
            {
                bool spraying = SprayNozzle != null;
                sprayJet.SetActive(spraying);
                if (spraying)
                {
                    // A jet of water is never quite still.
                    float flutter = 1f + Mathf.Sin(Time.unscaledTime * 53f) * 0.06f + Mathf.Sin(Time.unscaledTime * 31f) * 0.05f;
                    sprayJet.transform.localScale = new Vector3(flutter, flutter, 1f);
                }
            }
        }

        /// <summary>What the hand should be doing, where, and at what scale, from the input router's state.</summary>
        private Pose Decide(out Vector3 contact, out float dishScale, out Vector2 pointer)
        {
            contact = _contact;
            dishScale = _scale / Mathf.Max(0.01f, baseScale);
            pointer = _lastPointer;

            Pointer device = Pointer.current;
            if (router == null || viewCamera == null || device == null || !device.press.isPressed || router.IsBlocked || Time.timeScale == 0f)
            {
                return Pose.Hidden;
            }

            pointer = device.position.ReadValue();
            Ray ray = viewCamera.ScreenPointToRay(pointer);

            DishController steering = router.SteeringGlass;
            if (steering != null)
            {
                // Beside the glass: the fingertip on the counter-height plane the glass is held over.
                var plane = new Plane(Vector3.up, steering.transform.position - Vector3.up * 0.05f);
                if (plane.Raycast(ray, out float distance))
                {
                    contact = ray.GetPoint(distance);
                }

                dishScale = steering.ScaleFactor * 0.6f; // beside a glass: sized as for the glass
                return Pose.Point;
            }

            DishController dish = router.ActiveDish;
            if (dish == null)
            {
                return Pose.Hidden;
            }

            // Scaled with how big the dish is drawn, and a little with the dish itself: a sponge the size of a
            // plate's scrubbing spot would bury a glass - and the streaks the player has to find on it.
            float sizeFit = dish.Definition != null ? Mathf.Clamp(dish.Definition.PlaceholderScale.x / ReferenceDishWidth, 0.6f, 1f) : 1f;
            dishScale = dish.ScaleFactor * sizeFit;
            if (dish.CurrentStage == WashStage.AwaitingScrub)
            {
                if (!dish.TryGetWashContact(pointer, out _, out contact))
                {
                    return Pose.Hidden;
                }
            }
            else
            {
                contact = dish.Collider != null && dish.Collider.Raycast(ray, out RaycastHit hit, 100f)
                    ? hit.point
                    : dish.transform.position;
            }

            switch (dish.CurrentStage)
            {
                case WashStage.AwaitingRinse:
                    return Pose.Flat;
                case WashStage.AwaitingScrub:
                    return ToolPose();
                default:
                    return Pose.Grab; // taking it from the pile, carrying it to the rack
            }
        }

        private static Pose ToolPose()
        {
            ActiveToolSelector selector = ActiveToolSelector.Instance;
            string id = selector != null && selector.CurrentTool != null ? selector.CurrentTool.Id : string.Empty;
            switch (id)
            {
                case "steel_wool":
                    return Pose.SteelWool;
                case "soak":
                    return Pose.Brush;
                case "scraper":
                    return Pose.Scraper;
                case "shower":
                    return Pose.Shower;
                default:
                    return Pose.Sponge;
            }
        }

        private void BlendPose(float dt)
        {
            float t = 1f - Mathf.Exp(-poseSharpness * dt);
            _curl = Vector3.Lerp(_curl, FingerCurl[(int)_pose], t);
            _thumb = Vector3.Lerp(_thumb, ThumbCurl[(int)_pose], t);
            _indexCurlBlend = Mathf.Lerp(_indexCurlBlend, _pose == Pose.Point ? 1f : 0f, t);

            // Scrubbing presses down: the fingers flex a little with each circle.
            Vector3 press = new Vector3(4f, 3f, 2f) * (Mathf.Sin(_scrubPhase) * _scrubEnergy);

            for (int finger = 0; finger < 4; finger++)
            {
                Vector3 curl = _curl + press;
                if (finger == 0)
                {
                    curl = Vector3.Lerp(curl, new Vector3(4f, 4f, 2f), _indexCurlBlend);
                }

                for (int joint = 0; joint < 3; joint++)
                {
                    int i = finger * 3 + joint;
                    if (fingerJoints[i] != null)
                    {
                        fingerJoints[i].localRotation = _fingerBind[i] * Quaternion.Euler(curl[joint], 0f, 0f);
                    }
                }
            }

            if (thumbJoints[0] != null)
            {
                thumbJoints[0].localRotation = _thumbBind[0] * Quaternion.Euler(0f, _thumb.x, 0f);
            }

            for (int joint = 1; joint < thumbJoints.Length; joint++)
            {
                if (thumbJoints[joint] != null)
                {
                    thumbJoints[joint].localRotation = _thumbBind[joint] * Quaternion.Euler(_thumb[joint], 0f, 0f);
                }
            }

            if (hand != null)
            {
                Vector3 offset = _pose == Pose.Brush ? BrushGrip
                    : _pose == Pose.Scraper ? ScraperGrip
                    : _pose == Pose.Shower ? ShowerGrip
                    : _pose == Pose.Sponge ? SpongeGrip
                    : _pose == Pose.SteelWool ? WoolGrip
                    : Vector3.zero;
                hand.localPosition = Vector3.Lerp(hand.localPosition, _handBind + offset, t);
            }
        }

        private void ShowTool(Pose? pose)
        {
            if (scraper != null)
            {
                scraper.SetActive(pose == Pose.Scraper);
            }

            if (shower != null)
            {
                shower.SetActive(pose == Pose.Shower);
            }

            if (sponge != null)
            {
                sponge.SetActive(pose == Pose.Sponge);
            }

            if (steelWool != null)
            {
                steelWool.SetActive(pose == Pose.SteelWool);
            }

            if (brush != null)
            {
                brush.SetActive(pose == Pose.Brush);
            }
        }

        private void SetVisible(bool visible)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf != visible)
                {
                    child.gameObject.SetActive(visible);
                }
            }
        }
    }
}
