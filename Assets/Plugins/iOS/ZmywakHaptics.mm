// Reconstructed on 2026-11-17: the original file was lost from disk. Written to the contract in
// Assets/_Project/Scripts/Core/Haptics/HapticsService.cs: ZmywakHapticPulse(int strength), 0 light, 1 medium, 2 heavy,
// through UIKit's UIImpactFeedbackGenerator (the Taptic Engine).
#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *ZmywakGenerators[3];

static UIImpactFeedbackGenerator *ZmywakGeneratorFor(int strength)
{
    int index = strength <= 0 ? 0 : (strength >= 2 ? 2 : 1);
    if (ZmywakGenerators[index] == nil)
    {
        UIImpactFeedbackStyle style = index == 0 ? UIImpactFeedbackStyleLight
                                    : (index == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy);
        // Kept for the life of the app (one per strength), so this is correct with or without ARC.
        ZmywakGenerators[index] = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
    }
    return ZmywakGenerators[index];
}

extern "C" void ZmywakHapticPulse(int strength)
{
    // UIKit wants the main thread; Unity's scripting thread is the main thread on iOS, but stay safe.
    if (![NSThread isMainThread])
    {
        dispatch_async(dispatch_get_main_queue(), ^{ ZmywakHapticPulse(strength); });
        return;
    }
    UIImpactFeedbackGenerator *generator = ZmywakGeneratorFor(strength);
    [generator impactOccurred];
    [generator prepare]; // warm the Taptic Engine for the next pulse
}
