using System.Collections;
using System.IO;
using Garapon.Core;
using Garapon.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Garapon.Tests
{
    public class GaraponMachineTests
    {
        sealed class FixedRandom : IRandomSource
        {
            public int Value;
            public int Next(int maxExclusive) => Value;
        }

        const int DrawCount = 12;

        string savePath;
        float previousTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousTimeScale = Time.timeScale;
            savePath = Path.GetTempFileName();
            File.Delete(savePath);
            SaveFile.PathOverride = savePath;
            yield return SceneManager.LoadSceneAsync("Town");
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = previousTimeScale;
            SaveFile.PathOverride = null;
            File.Delete(savePath);
        }

        [UnityTest]
        public IEnumerator EachDrawReleasesOnePrizeAndKeepsBallsInMachine()
        {
            var session = GameSession.Instance;
            var machine = Object.FindAnyObjectByType<GaraponMachine>();
            session.SetRandomSource(new FixedRandom { Value = 999999 });
            session.State.Tickets = DrawCount;
            Time.timeScale = 4f;

            for (var i = 0; i < DrawCount; i++)
            {
                machine.Interact();
                var limit = Time.realtimeSinceStartup + 30f;
                while (machine.IsBusy && Time.realtimeSinceStartup < limit)
                {
                    yield return null;
                }

                Assert.IsFalse(machine.IsBusy, $"Draw {i} did not finish.");
                Assert.AreEqual(i + 1, machine.BallsAwarded);
                Assert.AreEqual(i + 1, session.State.TotalDraws);
            }

            Assert.AreEqual(0, session.State.Tickets);
            Assert.AreEqual(DrawCount, session.State.GetItem("tissue"));
            yield return new WaitForSeconds(3f);

            Assert.AreEqual(0, machine.BallsEscaped, "Balls left the machine without passing the exit.");
            Assert.AreEqual(30, Object.FindObjectsByType<GaraponBall>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator GoldDrawWinsTicketAndBusStopLoadsEnding()
        {
            var session = GameSession.Instance;
            var machine = Object.FindAnyObjectByType<GaraponMachine>();
            var busStop = Object.FindAnyObjectByType<HotSpringBusStop>();
            Assert.IsNotNull(busStop, "Run Garapon/Build World to add the bus stop to the Town scene.");
            session.SetRandomSource(new FixedRandom { Value = 0 });
            session.State.Tickets = 1;
            Time.timeScale = 4f;

            machine.Interact();
            var limit = Time.realtimeSinceStartup + 30f;
            while (machine.IsBusy && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(session.State.HasTripTicket);
            Assert.AreEqual("Town", SceneManager.GetActiveScene().name, "The gold ball no longer ends the run by itself.");

            busStop.Interact();
            Assert.IsFalse(session.State.Cleared, "The first press only asks for confirmation.");
            busStop.Interact();

            limit = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != "Ending" && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.AreEqual("Ending", SceneManager.GetActiveScene().name);
            Assert.IsTrue(SaveFile.Load().Cleared);
        }
    }
}
