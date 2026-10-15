#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using RageHunters.Progression;

namespace RageHunters.Tests.EditMode
{
    public class ProgressionTests
    {
        private GameObject _progressionGo;
        private ProgressionManager _manager;

        [SetUp]
        public void SetUp()
        {
            _progressionGo = new GameObject("TestProgressionManager");
            _manager = _progressionGo.AddComponent<ProgressionManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_progressionGo != null)
            {
                Object.DestroyImmediate(_progressionGo);
            }
        }

        [Test]
        public void Progression_InitialState_LevelOneWithZeroXP()
        {
            Assert.AreEqual(1, _manager.CurrentLevel);
            Assert.AreEqual(0, _manager.CurrentXP);
            Assert.AreEqual(0, _manager.TotalXP);
        }

        [Test]
        public void Progression_AddXP_IncreasesCurrentAndTotalXP()
        {
            _manager.AddXP(50);
            Assert.AreEqual(50, _manager.CurrentXP);
            Assert.AreEqual(50, _manager.TotalXP);
        }

        [Test]
        public void Progression_AddNegativeXP_Ignored()
        {
            _manager.AddXP(50);
            _manager.AddXP(-20);
            Assert.AreEqual(50, _manager.CurrentXP);
            Assert.AreEqual(50, _manager.TotalXP);
        }

        [Test]
        public void Progression_LevelUpThreshold_IncrementsLevel()
        {
            // Level 1 -> 2 requires 200 XP
            int needed = _manager.XPToNextLevel;
            _manager.AddXP(needed + 25);

            Assert.AreEqual(2, _manager.CurrentLevel, "Should reach Level 2.");
            Assert.AreEqual(25, _manager.CurrentXP, "Remaining XP should rollover.");
            Assert.AreEqual(needed + 25, _manager.TotalXP, "Total XP should reflect cumulative earnings.");
        }

        [Test]
        public void Progression_RecordMatchStats_AwardsCalculatedXP()
        {
            var stats = new MatchStats
            {
                Won = true,
                Extracted = true,
                Kills = 2,
                ObjectivesCompleted = 3,
                ItemsCollected = 5,
                SurvivalTime = 320f
            };

            _manager.RecordMatchStats(stats);

            // Calculation:
            // 3 objectives * 100 = 300
            // 2 kills * 75 = 150
            // 5 items * 10 = 50
            // extraction = 150
            // 320s survival (1 * 5min block) = 25
            // Total = 675 XP
            int expectedXP = (3 * 100) + (2 * 75) + (5 * 10) + 150 + (1 * 25);
            Assert.AreEqual(expectedXP, _manager.TotalXP, $"Expected {expectedXP} XP from match stats.");
            Assert.AreEqual(1, _manager.TotalMatchesPlayed);
            Assert.AreEqual(1, _manager.TotalWins);
            Assert.AreEqual(2, _manager.TotalKills);
        }
    }
}
#endif
