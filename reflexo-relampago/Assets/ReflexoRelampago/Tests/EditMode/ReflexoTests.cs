using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Suricatus.ReflexoRelampago.Tests
{
    public class ReflexoContentTests
    {
        [Test]
        public void EmptyContentUsesDefaults()
        {
            var content = ReflexoContent.Parse("");
            Assert.AreEqual(100, content.PointsPerHit);
            Assert.AreEqual(50, content.SpeedBonus);
            Assert.AreEqual(50, content.DistractionPenalty);
            Assert.AreEqual(10, content.Warmup.Seconds);
            Assert.AreEqual(0, content.Warmup.DistractionChance, "o aquecimento não tem distração");
            Assert.AreEqual(3, content.Tiers.Count);
            Assert.AreEqual(3, content.HowToPlay.Count);
            Assert.IsTrue(content.HasDistractions);
        }

        [Test]
        public void ReadsPhasesScoringAndSteps()
        {
            var content = ReflexoContent.Parse(@"{
                ""fases"": {
                    ""aquecimento"": { ""segundos"": 8, ""tempoNaTela"": 2, ""intervalo"": 0.6, ""tamanho"": 0.35 },
                    ""desafio"": { ""distracoes"": 0.5 },
                    ""sprint"": { ""segundos"": 12, ""tempoNaTela"": 0.7 }
                },
                ""pontosPorAcerto"": 10,
                ""bonusRapido"": { ""pontos"": 5, ""inteiroAte"": 0.2, ""zeraEm"": 0.8 },
                ""penalidadeDistracao"": 7,
                ""comoJogar"": [""Toque no logo""]
            }");
            Assert.AreEqual(8, content.Warmup.Seconds);
            Assert.AreEqual(2, content.Warmup.VisibleSeconds);
            Assert.AreEqual(0.6f, content.Warmup.GapSeconds, 1e-5);
            Assert.AreEqual(0.35f, content.Warmup.Size, 1e-5);
            Assert.AreEqual(0.5f, content.Challenge.DistractionChance, 1e-5);
            Assert.AreEqual(1.2f, content.Challenge.VisibleSeconds, 1e-5, "campo ausente fica com o padrão");
            Assert.AreEqual(12, content.Sprint.Seconds);
            Assert.AreEqual(0.7f, content.Sprint.VisibleSeconds, 1e-5);
            Assert.AreEqual(10, content.PointsPerHit);
            Assert.AreEqual(5, content.SpeedBonus);
            Assert.AreEqual(7, content.DistractionPenalty);
            CollectionAssert.AreEqual(new[] { "Toque no logo" }, content.HowToPlay);
        }

        [Test]
        public void KeepsValuesInsideSafeLimits()
        {
            var content = ReflexoContent.Parse(@"{
                ""fases"": { ""sprint"": { ""tempoNaTela"": 0.05, ""tamanho"": 0.9, ""distracoes"": 3, ""intervalo"": -1 } },
                ""pontosPorAcerto"": -5,
                ""bonusRapido"": { ""inteiroAte"": 2, ""zeraEm"": 1 }
            }");
            Assert.AreEqual(0.4f, content.Sprint.VisibleSeconds, 1e-5);
            Assert.AreEqual(0.45f, content.Sprint.Size, 1e-5);
            Assert.AreEqual(1f, content.Sprint.DistractionChance, 1e-5);
            Assert.AreEqual(0f, content.Sprint.GapSeconds, 1e-5);
            Assert.AreEqual(0, content.PointsPerHit);
            Assert.GreaterOrEqual(content.NoBonusSeconds, content.FullBonusSeconds, "o bônus nunca zera antes de começar a cair");
        }

        [Test]
        public void WithoutDistractionsTheLegendHidesThem()
        {
            var content = ReflexoContent.Parse(@"{ ""fases"": { ""desafio"": { ""distracoes"": 0 }, ""sprint"": { ""distracoes"": 0 } } }");
            Assert.IsFalse(content.HasDistractions);
        }
    }

    public class ScoringTests
    {
        [TestCase(0.1f, 150)]
        [TestCase(0.3f, 150)]
        [TestCase(0.65f, 125)]
        [TestCase(1.0f, 100)]
        [TestCase(1.5f, 100)]
        public void FasterHitsEarnUpTo50Extra(float reaction, int points)
        {
            Assert.AreEqual(points, new ReflexoContent().PointsFor(reaction));
        }

        [Test]
        public void TiersFollowTheScore()
        {
            var content = new ReflexoContent();
            Assert.AreEqual("Medalha de Bronze", content.TierFor(0).Name);
            Assert.IsFalse(content.TierFor(3999).Win);
            Assert.AreEqual("Medalha de Prata", content.TierFor(4000).Name);
            Assert.IsTrue(content.TierFor(4000).Win, "vitória a partir da Prata");
            Assert.AreEqual("Medalha de Ouro", content.TierFor(9000).Name);
        }

        [Test]
        public void TiersAreSortedAndWinFromTheSecondByDefault()
        {
            var content = ReflexoContent.Parse(@"{ ""faixas"": [
                { ""nome"": ""Craque"", ""pontos"": 2000 },
                { ""nome"": ""Participou"", ""pontos"": 0 },
                { ""nome"": ""Bom"", ""pontos"": 1000 },
                { ""pontos"": 500 }
            ] }");
            CollectionAssert.AreEqual(new[] { "Participou", "Bom", "Craque" }, content.Tiers.ConvertAll(t => t.Name), "faixa sem nome fica de fora");
            CollectionAssert.AreEqual(new[] { false, true, true }, content.Tiers.ConvertAll(t => t.Win));
        }

        [Test]
        public void ExplicitWinFlagsAreRespected()
        {
            var content = ReflexoContent.Parse(@"{ ""faixas"": [
                { ""nome"": ""Bronze"", ""pontos"": 0 },
                { ""nome"": ""Prata"", ""pontos"": 1000 },
                { ""nome"": ""Ouro"", ""pontos"": 2000, ""vitoria"": true }
            ] }");
            CollectionAssert.AreEqual(new[] { false, false, true }, content.Tiers.ConvertAll(t => t.Win));
        }

        [Test]
        public void ScoreBelowEveryTierHasNone()
        {
            var content = ReflexoContent.Parse(@"{ ""faixas"": [ { ""nome"": ""Ouro"", ""pontos"": 500 } ] }");
            Assert.IsNull(content.TierFor(499));
        }
    }

    public class PacingTests
    {
        [Test]
        public void WarmupChallengeAndSprintInOrder()
        {
            var pacing = new Pacing(new ReflexoContent(), 40);
            Assert.AreEqual(Phase.Aquecimento, pacing.PhaseAt(0));
            Assert.AreEqual(Phase.Aquecimento, pacing.PhaseAt(9.9f));
            Assert.AreEqual(Phase.Desafio, pacing.PhaseAt(10));
            Assert.AreEqual(Phase.Desafio, pacing.PhaseAt(29.9f));
            Assert.AreEqual(Phase.Sprint, pacing.PhaseAt(30));
        }

        [Test]
        public void ChallengeTightensTowardTheSprint()
        {
            var content = new ReflexoContent();
            var pacing = new Pacing(content, 40);
            Assert.AreEqual(content.Challenge.VisibleSeconds, pacing.At(10).VisibleSeconds, 1e-4);
            Assert.AreEqual((content.Challenge.VisibleSeconds + content.Sprint.VisibleSeconds) / 2, pacing.At(20).VisibleSeconds, 1e-4);
            Assert.Less(pacing.At(29).GapSeconds, pacing.At(11).GapSeconds);
            Assert.AreEqual(content.Sprint.VisibleSeconds, pacing.At(35).VisibleSeconds, 1e-4);
            Assert.Greater(pacing.At(5).VisibleSeconds, pacing.At(35).VisibleSeconds, "o aquecimento deixa o alvo mais tempo na tela");
            Assert.Greater(pacing.At(5).Size, pacing.At(35).Size, "o aquecimento tem alvos maiores");
        }

        [Test]
        public void ShortMatchShrinksWarmupAndSprint()
        {
            var pacing = new Pacing(new ReflexoContent(), 12);
            Assert.AreEqual(6, pacing.WarmupSeconds, 1e-4);
            Assert.AreEqual(6, pacing.SprintSeconds, 1e-4);
            Assert.AreEqual(Phase.Aquecimento, pacing.PhaseAt(5.9f));
            Assert.AreEqual(Phase.Sprint, pacing.PhaseAt(6));
        }
    }

    public class ReflexMatchTests
    {
        const float Step = 0.02f;

        sealed class Recorder
        {
            public readonly List<Piece> Shown = new List<Piece>();
            public readonly List<(Piece piece, PieceEnd end)> Hidden = new List<(Piece, PieceEnd)>();

            public Recorder(ReflexMatch match)
            {
                match.Shown += p => Shown.Add(p);
                match.Hidden += (p, e) => Hidden.Add((p, e));
            }
        }

        static void Run(ReflexMatch match, float seconds)
        {
            for (float t = 0; t < seconds - 1e-4f; t += Step) match.Tick(Step);
        }

        static void RunUntil(ReflexMatch match, Func<bool> condition, float limit = 60)
        {
            for (float t = 0; t < limit && !condition(); t += Step) match.Tick(Step);
            Assert.IsTrue(condition(), "a condição não aconteceu a tempo");
        }

        static ReflexoContent NoDistractions() =>
            ReflexoContent.Parse(@"{ ""fases"": { ""desafio"": { ""distracoes"": 0 }, ""sprint"": { ""distracoes"": 0 } } }");

        static ReflexoContent AlwaysDistractions() =>
            ReflexoContent.Parse(@"{ ""fases"": { ""aquecimento"": { ""distracoes"": 1 }, ""desafio"": { ""distracoes"": 1 }, ""sprint"": { ""distracoes"": 1 } } }");

        [Test]
        public void FirstTargetComesRightAfterTheStart()
        {
            var match = new ReflexMatch(new ReflexoContent(), 40, new Random(1));
            var log = new Recorder(match);
            Run(match, ReflexMatch.FirstTargetDelay - 0.05f);
            Assert.IsEmpty(log.Shown);
            Run(match, 0.1f);
            Assert.AreEqual(1, log.Shown.Count);
            Assert.AreEqual(PieceKind.Target, log.Shown[0].Kind);
        }

        [Test]
        public void UntouchedTargetIsAMissedChanceAndTheNextOneFollows()
        {
            var content = NoDistractions();
            var match = new ReflexMatch(content, 40, new Random(2));
            var log = new Recorder(match);
            RunUntil(match, () => match.Target != null);
            var first = match.Target;
            Run(match, content.Warmup.VisibleSeconds + 0.05f);
            Assert.AreEqual(1, match.Expired);
            Assert.AreEqual((first, PieceEnd.Expired), log.Hidden[0]);
            Assert.AreEqual(0, match.Taps, "alvo perdido não é toque");
            RunUntil(match, () => match.Target != null, content.Warmup.GapSeconds + 0.1f);
            Assert.AreNotEqual(first.Id, match.Target.Id);
        }

        [Test]
        public void HitPaysForSpeed()
        {
            var match = new ReflexMatch(NoDistractions(), 40, new Random(3));
            var log = new Recorder(match);
            RunUntil(match, () => match.Target != null);
            var target = match.Target;
            Run(match, 0.2f);
            var tap = match.TapPiece(target.Id);
            Assert.AreEqual(TapOutcome.Hit, tap.Outcome);
            Assert.AreEqual(150, tap.Points, "abaixo de 0,3 s o bônus é inteiro");
            Assert.AreEqual(1, match.Hits);
            Assert.IsNull(match.Target);
            Assert.AreEqual((target, PieceEnd.Hit), log.Hidden[0]);

            Assert.AreEqual(TapOutcome.Ignored, match.TapPiece(target.Id).Outcome, "segundo toque no mesmo alvo não conta");
            Assert.AreEqual(1, match.Taps);
        }

        [Test]
        public void DistractionCostsPointsAndLeavesTheTarget()
        {
            var match = new ReflexMatch(AlwaysDistractions(), 40, new Random(4));
            var log = new Recorder(match);
            RunUntil(match, () => match.Distraction != null);
            var target = match.Target;
            var distraction = match.Distraction;
            var tap = match.TapPiece(distraction.Id);
            Assert.AreEqual(TapOutcome.Distraction, tap.Outcome);
            Assert.AreEqual(50, tap.Points);
            Assert.AreEqual(1, match.DistractionTaps);
            Assert.AreSame(target, match.Target, "o alvo continua na tela");
            Assert.AreEqual((distraction, PieceEnd.Tapped), log.Hidden[0]);
        }

        [Test]
        public void DistractionLeavesWithItsTarget()
        {
            var match = new ReflexMatch(AlwaysDistractions(), 40, new Random(5));
            var log = new Recorder(match);
            RunUntil(match, () => match.Distraction != null);
            var distraction = match.Distraction;
            match.TapPiece(match.Target.Id);
            Assert.IsNull(match.Distraction);
            Assert.Contains((distraction, PieceEnd.Gone), log.Hidden);
            Assert.AreEqual(0, match.DistractionTaps);
        }

        [Test]
        public void WarmupHasNoDistractions()
        {
            var match = new ReflexMatch(new ReflexoContent(), 40, new Random(6));
            var log = new Recorder(match);
            Run(match, 9.9f);
            Assert.IsTrue(log.Shown.Count > 0);
            Assert.IsTrue(log.Shown.TrueForAll(p => p.Kind == PieceKind.Target));
        }

        [Test]
        public void PiecesStayInsideAndNeverOverlap()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var match = new ReflexMatch(AlwaysDistractions(), 40, new Random(seed));
                var log = new Recorder(match);
                Piece target = null;
                match.Shown += p =>
                {
                    if (p.Kind == PieceKind.Target) target = p;
                    else Assert.GreaterOrEqual(Distance(p, target), p.Size, $"semente {seed}: distração encostando no alvo");
                };
                Run(match, 40);
                foreach (var p in log.Shown)
                {
                    Assert.GreaterOrEqual(p.X - p.Size / 2, 0, $"semente {seed}");
                    Assert.GreaterOrEqual(p.Y - p.Size / 2, 0, $"semente {seed}");
                    Assert.LessOrEqual(p.X + p.Size / 2, 1, $"semente {seed}");
                    Assert.LessOrEqual(p.Y + p.Size / 2, 1, $"semente {seed}");
                }
            }
        }

        [Test]
        public void TargetsChangePlaceAndArt()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var match = new ReflexMatch(NoDistractions(), 40, new Random(seed), targetVariants: 5);
                var log = new Recorder(match);
                Run(match, 40);
                var targets = log.Shown.FindAll(p => p.Kind == PieceKind.Target);
                Assert.Greater(targets.Count, 10);
                for (int i = 1; i < targets.Count; i++)
                {
                    Assert.GreaterOrEqual(Distance(targets[i], targets[i - 1]), ReflexMatch.MinJump, $"semente {seed}: alvo no mesmo lugar");
                    Assert.AreNotEqual(targets[i].Variant, targets[i - 1].Variant, $"semente {seed}: mesma arte duas vezes seguidas");
                }
            }
        }

        [Test]
        public void PrecisionCountsEveryTapInTheArea()
        {
            var match = new ReflexMatch(AlwaysDistractions(), 40, new Random(7));
            RunUntil(match, () => match.Distraction != null);
            match.TapEmpty();
            match.TapPiece(match.Distraction.Id);
            match.TapPiece(match.Target.Id);
            Assert.AreEqual(3, match.Taps);
            Assert.AreEqual(1, match.Hits);
            Assert.AreEqual(33, match.PrecisionPercent);
        }

        [Test]
        public void NoTapsMeansZeroPrecision()
        {
            var match = new ReflexMatch(new ReflexoContent(), 40, new Random(8));
            Run(match, 5);
            Assert.AreEqual(0, match.PrecisionPercent);
        }

        [Test]
        public void StopClearsTheScreenWithoutCountingTheLastTarget()
        {
            var match = new ReflexMatch(AlwaysDistractions(), 40, new Random(9));
            var log = new Recorder(match);
            RunUntil(match, () => match.Distraction != null);
            match.TapPiece(match.Target.Id);
            RunUntil(match, () => match.Target != null);
            match.Stop();
            Assert.IsNull(match.Target);
            Assert.IsNull(match.Distraction);
            Assert.AreEqual(PieceEnd.Gone, log.Hidden[log.Hidden.Count - 1].end);
            Assert.AreEqual(1, match.Goal, "o alvo que estava na tela no fim não conta");
            int shown = log.Shown.Count;
            Run(match, 5);
            match.TapEmpty();
            Assert.AreEqual(shown, log.Shown.Count, "nada aparece depois do fim");
            Assert.AreEqual(1, match.Taps, "toque depois do fim não conta");
        }

        [Test]
        public void SprintBringsTargetsMoreOften()
        {
            var match = new ReflexMatch(NoDistractions(), 40, new Random(10));
            var shownAt = new List<float>();
            match.Shown += p => shownAt.Add(p.ShownAt);
            // Jogador perfeito: toca cada alvo 0,4 s depois de aparecer.
            for (float t = 0; t < 40; t += Step)
            {
                match.Tick(Step);
                if (match.Target != null && match.Elapsed - match.Target.ShownAt >= 0.4f) match.TapPiece(match.Target.Id);
            }
            int warmup = shownAt.FindAll(t => t < 10).Count;
            int sprint = shownAt.FindAll(t => t >= 30).Count;
            Assert.Greater(sprint, warmup);
        }

        static float Distance(Piece a, Piece b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
