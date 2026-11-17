using UnityEngine;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// A test deadline counted in time the game actually ran. Real time is the wrong clock for it: the laptop
    /// dropped into Modern Standby in the middle of a test (160 s, the Unity process frozen with it) and a
    /// "wait up to 10 s for a dish" failed although the game had run for a moment. Between two checks it counts
    /// the real time that passed, but at most <see cref="Time.maximumDeltaTime"/> per frame rendered - the same
    /// cap Unity puts on a frame's game time - so a frozen stretch counts as one slow frame.
    /// It may be checked as often or as rarely as a loop likes: an earlier version counted one frame per check,
    /// and a loop that checked once per 120-frame sweep over a glass never reached its 30 s - the test ran
    /// into the framework's 180 s timeout instead of failing with its own message.
    /// </summary>
    public sealed class PlayDeadline
    {
        private readonly float _seconds;
        private float _ran;
        private int _lastFrame;
        private float _lastRealtime;

        public PlayDeadline(float seconds)
        {
            _seconds = seconds;
            _lastFrame = Time.frameCount;
            _lastRealtime = Time.realtimeSinceStartup;
        }

        public bool Passed
        {
            get
            {
                int frames = Time.frameCount - _lastFrame;
                if (frames > 0)
                {
                    float real = Time.realtimeSinceStartup - _lastRealtime;
                    _ran += Mathf.Min(real, frames * Time.maximumDeltaTime);
                    _lastFrame = Time.frameCount;
                    _lastRealtime = Time.realtimeSinceStartup;
                }

                return _ran >= _seconds;
            }
        }
    }
}
