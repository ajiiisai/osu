// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Skinning;
using osu.Game.Storyboards;
using osu.Game.Storyboards.Drawables;
using osuTK;

namespace osu.Game.Tests.Visual.Gameplay
{
    public class TestSceneDrawableStoryboardSprite : SkinnableTestScene
    {
        protected override Ruleset CreateRulesetForSkinProvider() => new OsuRuleset();

        [Cached]
        private Storyboard storyboard { get; set; } = new Storyboard();

        private IEnumerable<DrawableStoryboardSprite> sprites => this.ChildrenOfType<DrawableStoryboardSprite>();

        [Test]
        public void TestSkinSpriteDisallowedByDefault()
        {
            const string lookup_name = "hitcircleoverlay";

            AddStep("allow skin lookup", () => storyboard.UseSkinSprites = false);

            AddStep("create sprites", () => SetContents(_ => createSprite(lookup_name, Anchor.TopLeft, Vector2.Zero)));

            assertSpritesFromSkin(false);
        }

        [Test]
        public void TestAllowLookupFromSkin()
        {
            const string lookup_name = "hitcircleoverlay";

            AddStep("allow skin lookup", () => storyboard.UseSkinSprites = true);

            AddStep("create sprites", () => SetContents(_ => createSprite(lookup_name, Anchor.TopLeft, Vector2.Zero)));

            assertSpritesFromSkin(true);

            AddAssert("skinnable sprite has correct size", () => sprites.Any(s => Precision.AlmostEquals(s.ChildrenOfType<SkinnableSprite>().Single().Size, new Vector2(128, 128))));
        }

        [Test]
        public void TestFlippedSprite()
        {
            const string lookup_name = "hitcircleoverlay";

            AddStep("allow skin lookup", () => storyboard.UseSkinSprites = true);
            AddStep("create sprites", () => SetContents(_ => createSprite(lookup_name, Anchor.TopLeft, Vector2.Zero)));
            AddStep("flip sprites", () => sprites.ForEach(s =>
            {
                s.FlipH = true;
                s.FlipV = true;
            }));
            AddAssert("origin flipped", () => sprites.All(s => s.Origin == Anchor.BottomRight));
        }

        [Test]
        public void TestNegativeScale()
        {
            const string lookup_name = "hitcircleoverlay";

            AddStep("allow skin lookup", () => storyboard.UseSkinSprites = true);
            AddStep("create sprites", () => SetContents(_ => createSprite(lookup_name, Anchor.TopLeft, Vector2.Zero)));
            AddStep("scale sprite", () => sprites.ForEach(s => s.VectorScale = new Vector2(-1)));
            AddAssert("origin flipped", () => sprites.All(s => s.Origin == Anchor.BottomRight));
        }

        [Test]
        public void TestNegativeScaleWithFlippedSprite()
        {
            const string lookup_name = "hitcircleoverlay";

            AddStep("allow skin lookup", () => storyboard.UseSkinSprites = true);
            AddStep("create sprites", () => SetContents(_ => createSprite(lookup_name, Anchor.TopLeft, Vector2.Zero)));
            AddStep("scale sprite", () => sprites.ForEach(s => s.VectorScale = new Vector2(-1)));
            AddAssert("origin flipped", () => sprites.All(s => s.Origin == Anchor.BottomRight));
            AddStep("flip sprites", () => sprites.ForEach(s =>
            {
                s.FlipH = true;
                s.FlipV = true;
            }));
            AddAssert("origin back", () => sprites.All(s => s.Origin == Anchor.TopLeft));
        }

        private DrawableStoryboardSprite createSprite(string lookupName, Anchor origin, Vector2 initialPosition)
            => new DrawableStoryboardSprite(
                new StoryboardSprite(lookupName, origin, initialPosition)
            ).With(s =>
            {
                s.LifetimeStart = double.MinValue;
                s.LifetimeEnd = double.MaxValue;
            });

        private void assertSpritesFromSkin(bool fromSkin) =>
            AddAssert($"sprites are {(fromSkin ? "from skin" : "from storyboard")}",
                () => sprites.All(sprite => sprite.ChildrenOfType<SkinnableSprite>().Any() == fromSkin));
    }
}
