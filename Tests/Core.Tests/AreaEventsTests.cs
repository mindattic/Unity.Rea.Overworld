using System;
using System.IO;
using MindAttic.Overworld.Core;
using NUnit.Framework;

namespace MindAttic.Overworld.Tests
{
    /// <summary>
    /// U.OVW-US-A26 (RECONSTRUCTION 9.4; BIBLE "Events"): standing in an event box runs its script, once on entry and
    /// again every its script's rate while the leader stays inside; the 2006 database's own events are data only.
    /// </summary>
    public class AreaEventsTests
    {
        static string ContentDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Assets/StreamingAssets/Content");
        }

        static readonly ContentRegistry Registry = ContentRegistry.Load(new DirectoryContentSource(ContentDirectory()));

        Game game;
        Party party;
        ScriptInterpreter scripts;
        WorldClock clock;
        Hero chrono;

        [SetUp]
        public void SetUp()
        {
            game = new Game();
            party = new Party(Registry.Heroes, null, new Random(5));
            var messages = new MessageBoard(Registry.Hud);
            clock = new WorldClock(Registry.Time, Registry.Heroes.Locomotion.GameSpeed, messages);
            scripts = new ScriptInterpreter(Registry.Scripts, party, null) { Clock = clock, Messages = messages }.Install(game);
            party.Install(game);
            clock.Install(game);
            new AreaEvents(Registry.PlayableWorld, party, scripts).Install(game);
            Assert.IsTrue(game.Submit(new JoinGame(PlayerId.One)).Accepted);
            chrono = party.Heroes[0];
        }

        void Steps(int n)
        {
            for (var i = 0; i < n; i++) game.Step();
        }

        EventRecord Find(string name) => Registry.PlayableWorld.Events.Find(e => e.Name == name);

        void PlaceLeaderIn(EventRecord e, float fx = 0.5f, float fy = 0.5f)
        {
            var x = e.X + e.Left + (e.Right - e.Left) * fx;
            var y = e.Y + e.Bottom + (e.Top - e.Bottom) * fy;
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, x, y)).Accepted);
        }

        [Test]
        public void ContainsTestsTheBoxWithTheOriginAdded()
        {
            var e = Find("Poison Box");
            Assert.IsTrue(AreaEvents.Contains(e, e.X, e.Y));
            Assert.IsTrue(AreaEvents.Contains(e, e.X + e.Left, e.Y + e.Top));
            Assert.IsFalse(AreaEvents.Contains(e, e.X + e.Left - 0.001f, e.Y));
        }

        [Test]
        public void PoisonBoxHurtsEveryHalfSecondWhileStandingInIt()
        {
            var box = Find("Poison Box");
            var hp = chrono.Health;
            PlaceLeaderIn(box);
            Steps(1);
            Assert.AreEqual(hp - 1, chrono.Health, "runs once on entry");
            Steps(29); // short of half a second (0.5 s = 30 ticks) more
            Assert.AreEqual(hp - 1, chrono.Health, "the rate has not elapsed again yet");
            Steps(1); // the 30th tick after entry: 0.5 s elapsed
            Assert.AreEqual(hp - 2, chrono.Health);
        }

        [Test]
        public void HealingBoxHealsAndStopsOnceTheLeaderLeaves()
        {
            var box = Find("Healing Box");
            Assert.IsTrue(game.Submit(new SetHeroVitals(PlayerId.One, 0, 10, null, null, null)).Accepted);
            PlaceLeaderIn(box);
            Steps(1);
            Assert.AreEqual(11, chrono.Health);
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, 999f, 999f)).Accepted);
            Steps(60);
            Assert.AreEqual(11, chrono.Health, "away from the box, nothing more happens");
        }

        [Test]
        public void EnteringDarkWoodsTurnsNightOnceNotEveryTick()
        {
            var box = Find("Entering Dark Woods");
            Assert.AreNotEqual("Night", clock.Period.Name);
            PlaceLeaderIn(box);
            Steps(1);
            Assert.AreEqual("Night", clock.Period.Name);

            // Setting the period back to Day and staying inside does not flip it again (rate 0: once on entry only).
            Assert.IsTrue(game.Submit(new SetClock(PlayerId.One, 12, 0)).Accepted);
            Assert.AreNotEqual("Night", clock.Period.Name);
            Steps(10);
            Assert.AreNotEqual("Night", clock.Period.Name, "already inside; rate 0 only ran once, on entry");

            // Leaving and re-entering runs it again.
            Assert.IsTrue(game.Submit(new PlaceHero(PlayerId.One, 0, 999f, 999f)).Accepted);
            Steps(1);
            Assert.IsTrue(game.Submit(new SetClock(PlayerId.One, 12, 0)).Accepted);
            PlaceLeaderIn(box);
            Steps(1);
            Assert.AreEqual("Night", clock.Period.Name);
        }

        [Test]
        public void TheOlderDatabasesEventsAreDataOnlyNotEvaluated()
        {
            Assert.IsNull(Registry.PlayableWorld.Events.Find(e => e.Name == "Test"), "Test is the 2006 database's event, not the 2007 world's");
            Assert.IsNull(Registry.PlayableWorld.Events.Find(e => e.Name == "Cinematic"));
            var reference = Registry.ReferenceWorld;
            Assert.IsNotNull(reference.Events.Find(e => e.Name == "Test"));
            Assert.IsNotNull(reference.Events.Find(e => e.Name == "Dark Woods"));
            Assert.IsNotNull(reference.Events.Find(e => e.Name == "Cinematic"));
            Assert.AreEqual(3, reference.Scripts.Count);
        }
    }
}
