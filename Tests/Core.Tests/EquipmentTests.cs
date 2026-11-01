using System;
using System.IO;
using MindAttic.Overworld.Core;
using NUnit.Framework;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;

namespace MindAttic.Overworld.Tests
{
    /// <summary>
    /// U.OVW-US-A30 (RECONSTRUCTION 16.3; BIBLE "Equipping Alexander"): hero.mdb's own hero, Alexander, with the Steel
    /// Sword and the other three weapons, and the empty armour and relic slots.
    /// </summary>
    public class EquipmentTests
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
        Equipment equipment;

        [SetUp]
        public void SetUp()
        {
            game = new Game();
            equipment = new Equipment(Registry.HeroDatabase).Install(game);
            Assert.IsTrue(game.Submit(new JoinGame(PlayerId.One)).Accepted);
        }

        [Test]
        public void AlexanderIsTheHeroDatabasesOneHeroStartingWithTheSteelSword()
        {
            Assert.AreEqual("Alexander", equipment.Hero.Name);
            Assert.AreEqual(50, equipment.Hero.Health);
            Assert.AreEqual(50, equipment.Hero.MaxHealth);
            Assert.AreEqual(1, equipment.Hero.Strength);
            Assert.AreEqual(2, equipment.Hero.Agility);
            Assert.AreEqual(2, equipment.Hero.Speed);
            Assert.AreEqual("Steel Sword", equipment.WeaponOf(PlayerId.One).Name);
        }

        [Test]
        public void TheFourWeaponsAreTheHeroDatabasesOwnAxeSteelSwordKatanaAndBroadSword()
        {
            var weapons = Registry.HeroDatabase.Weapons;
            Assert.AreEqual(4, weapons.Count);
            CollectionAssert.AreEquivalent(new[] { "Axe", "Steel Sword", "Katana", "Broad Sword" }, System.Linq.Enumerable.Select(weapons, w => w.Name));
        }

        [Test]
        public void CyclingWrapsAroundAllFourWeapons()
        {
            var start = equipment.WeaponOf(PlayerId.One).Name;
            Assert.IsTrue(game.Submit(new CycleEquippedWeapon(PlayerId.One, 1)).Accepted);
            Assert.AreNotEqual(start, equipment.WeaponOf(PlayerId.One).Name);

            // Three more steps the same way visits the remaining two weapons and arrives back at the start (four in all).
            for (var i = 0; i < 3; i++) game.Submit(new CycleEquippedWeapon(PlayerId.One, 1));
            Assert.AreEqual(start, equipment.WeaponOf(PlayerId.One).Name, "a full cycle of the four weapons returns to the start");

            Assert.IsTrue(game.Submit(new CycleEquippedWeapon(PlayerId.One, -1)).Accepted);
            Assert.AreNotEqual(start, equipment.WeaponOf(PlayerId.One).Name, "stepping back leaves the last weapon, not the start");
        }

        [Test]
        public void TheArmourAndRelicSlotsAreEmptyInTheOriginalAndNotInvented()
        {
            // RECONSTRUCTION 16.3: Armor, Armor_Head/Body/Arms/Feet and Relic are empty tables; HeroDatabase already
            // fails loudly if a re-export ever filled one in (see its own EmptyTables check), so there is nothing to
            // read here: the hero screen shows every slot as "none" (HudPresenter.EquipmentLine), not a Core concept.
            Assert.AreEqual(0, Registry.HeroDatabase.Database.Table("Armor")?.RowCount ?? 0);
            Assert.AreEqual(0, Registry.HeroDatabase.Database.Table("Relic")?.RowCount ?? 0);
        }

    }
}
