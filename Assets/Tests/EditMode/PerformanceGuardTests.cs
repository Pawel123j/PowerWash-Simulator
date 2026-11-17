// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Graphics;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Tests
{
    public class PerformanceGuardTests
    {
        private DishDefinition _plate;

        private DishDefinition _pan;

        [SetUp]
        public void SetUp()
        {
            _plate = AssetDatabase.LoadAssetAtPath<DishDefinition>("Assets/_Project/ScriptableObjects/Dish_plate.asset");
            _pan = AssetDatabase.LoadAssetAtPath<DishDefinition>("Assets/_Project/ScriptableObjects/Dish_pan.asset");
            Assert.IsNotNull(_plate, "Run Zmywak > Setup > Run All first");
            DirtPrewarm.Clear();
        }

        [Test]
        public void TheStartingTier_FollowsWhatThePhoneCanDo()
        {
            Assert.AreEqual(QualityTier.High, DeviceProfile.StartingTier(handheld: false, 2000, 512, 35, 2, 480000), "A desktop or the editor is High");
            Assert.AreEqual(QualityTier.Low, DeviceProfile.StartingTier(handheld: true, 2048, 512, 45, 8, 2592000), "Two gigabytes is a budget phone");
            Assert.AreEqual(QualityTier.Low, DeviceProfile.StartingTier(handheld: true, 8192, 2048, 35, 8, 2592000), "An old shader model is Low");
            Assert.AreEqual(QualityTier.Low, DeviceProfile.StartingTier(handheld: true, 6144, 2048, 50, 4, 2592000), "Four cores is Low");
            Assert.AreEqual(QualityTier.Medium, DeviceProfile.StartingTier(handheld: true, 4096, 2048, 50, 8, 2592000), "Four gigabytes is mid-range");
            Assert.AreEqual(QualityTier.Medium, DeviceProfile.StartingTier(handheld: true, 6144, 2048, 50, 8, 4608000), "A huge screen on 6 GB is mid-range");
            Assert.AreEqual(QualityTier.High, DeviceProfile.StartingTier(handheld: true, 12288, 4096, 50, 8, 4608000), "A flagship is High");
        }

        [Test]
        public void LowerTiers_DrawTheDirtWithFewerTexels()
        {
            Assert.Less(DeviceProfile.DirtResolution(QualityTier.Low), DeviceProfile.DirtResolution(QualityTier.Medium));
            Assert.Less(DeviceProfile.DirtResolution(QualityTier.Medium), DeviceProfile.DirtResolution(QualityTier.High));
            DishInstance low = new DishInstance(_plate, 1, circular: true, DeviceProfile.DirtResolution(QualityTier.Low));
            Assert.AreEqual(DeviceProfile.DirtResolution(QualityTier.Low), low.Canvas.Resolution);
            Assert.Greater(low.Canvas.RemainingFraction(0), 0.5f, "A low-tier plate came without its dirt");
        }

        [Test]
        public void DirtMadeAhead_OnAWorkerThread_IsAWholeDishOfDirt()
        {
            DirtPrewarm.Prepare(_pan, 128);
            DirtCanvas canvas = null;
            for (int i = 0; i < 200; i++)
            {
                if (canvas != null)
                {
                    break;
                }
                canvas = DirtPrewarm.Take(_pan, 128);
                if (canvas == null)
                {
                    DirtPrewarm.Prepare(_pan, 128);
                    Task.Delay(20).Wait();
                }
            }
            Assert.IsNotNull(canvas, "The pan's dirt was never ready");
            DishInstance pan = new DishInstance(_pan, canvas);
            DishInstance madeOnTheSpot = new DishInstance(_pan, 7, circular: true, 128);
            Assert.AreEqual(madeOnTheSpot.Canvas.LayerCount, pan.Canvas.LayerCount);
            Assert.AreEqual(128, pan.Canvas.Resolution);
            for (int layer = 0; layer < pan.Canvas.LayerCount; layer++)
            {
                Assert.AreEqual(madeOnTheSpot.Canvas.RemainingFraction(layer), pan.Canvas.RemainingFraction(layer), 9.999999747378752E-05, $"Layer {layer} of the pan made ahead is not a whole layer of dirt");
            }
            Assert.AreEqual(WashStage.AwaitingScrape, pan.Stage);
            Assert.IsNull(DirtPrewarm.Take(_pan, 128), "One canvas made ahead was handed out twice");
        }

        [Test]
        public void AskingForADifferentSharpness_DoesNotHandOutTheWrongCanvas()
        {
            DirtPrewarm.Prepare(_plate, 128);
            Task.Delay(500).Wait();
            Assert.IsNull(DirtPrewarm.Take(_plate, 256), "A 128 canvas went to a 256 dish");
        }

        [Test]
        public void TheGlazeOfAKind_IsWorkedOutOnce_AndShared()
        {
            Color[] first = DishResourcePool.Glaze(_plate, 128);
            Color[] again = DishResourcePool.Glaze(_plate, 128);
            Assert.AreSame(first, again);
            CollectionAssert.AreEqual(DishDecor.BuildBase(_plate, 128), first, "The shared glaze differs from the plate's own");
        }

        [Test]
        public void PooledArrays_GoRound()
        {
            Color32[] array = DishResourcePool.RentPixels(64);
            DishResourcePool.Return(array);
            Assert.AreSame(array, DishResourcePool.RentPixels(64), "A returned array was not reused");
            Assert.AreNotSame(array, DishResourcePool.RentPixels(64), "One array was handed to two dishes");
        }
    }
}
