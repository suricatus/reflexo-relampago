using System;
using System.Collections.Generic;
using Suricatus.FastDrive;

namespace Suricatus.ReflexoRelampago
{
    /// <summary>As três fases da partida, na ordem em que acontecem.</summary>
    public enum Phase
    {
        Aquecimento,
        Desafio,
        Sprint
    }

    /// <summary>Como os alvos se comportam em uma fase.</summary>
    public sealed class PhaseSettings
    {
        /// <summary>Duração da fase. Só vale para o aquecimento e o sprint; o desafio fica com o que sobra.</summary>
        public float Seconds;
        /// <summary>Quanto tempo cada alvo fica na tela.</summary>
        public float VisibleSeconds;
        /// <summary>Espera entre um alvo sair (tocado ou perdido) e o próximo aparecer.</summary>
        public float GapSeconds;
        /// <summary>Lado do alvo, em fração do lado da área do jogo.</summary>
        public float Size;
        /// <summary>Chance de cada alvo vir acompanhado de uma distração (0 a 1).</summary>
        public float DistractionChance;

        public PhaseSettings(float seconds, float visibleSeconds, float gapSeconds, float size, float distractionChance)
        {
            Seconds = seconds;
            VisibleSeconds = visibleSeconds;
            GapSeconds = gapSeconds;
            Size = size;
            DistractionChance = distractionChance;
        }
    }

    /// <summary>Uma faixa de resultado (ex.: Medalha de Prata a partir de 3000 pontos).</summary>
    public sealed class Tier
    {
        public readonly string Name;
        public readonly int MinScore;
        /// <summary>Se a partida que chega nesta faixa conta como vitória (título, confete e som de vitória).</summary>
        public readonly bool Win;

        public Tier(string name, int minScore, bool win)
        {
            Name = name;
            MinScore = minScore;
            Win = win;
        }
    }

    /// <summary>Conteúdo do conteudo/reflexo.json do cliente. Tudo é opcional; o que faltar fica com o padrão.</summary>
    public sealed class ReflexoContent
    {
        public const string FileName = "reflexo";

        public static readonly IReadOnlyList<string> DefaultHowToPlay = new[]
        {
            "Toque nos alvos assim que aparecerem",
            "Quanto mais rápido, mais pontos",
            "Evite as distrações: elas tiram pontos",
        };

        public PhaseSettings Warmup = new PhaseSettings(10, 1.6f, 0.5f, 0.34f, 0f);
        public PhaseSettings Challenge = new PhaseSettings(0, 1.2f, 0.35f, 0.28f, 0.3f);
        public PhaseSettings Sprint = new PhaseSettings(10, 0.85f, 0.15f, 0.28f, 0.3f);

        public int PointsPerHit = 100;
        public int SpeedBonus = 50;
        /// <summary>Até este tempo de reação, o bônus é inteiro.</summary>
        public float FullBonusSeconds = 0.3f;
        /// <summary>A partir deste tempo de reação, não há bônus.</summary>
        public float NoBonusSeconds = 1.0f;
        public int DistractionPenalty = 50;

        public readonly List<Tier> Tiers = new List<Tier>(DefaultTiers());
        public readonly List<string> HowToPlay = new List<string>(DefaultHowToPlay);

        /// <summary>Se alguma fase pode mostrar distrações (a legenda "Evite" só aparece nesse caso).</summary>
        public bool HasDistractions => Warmup.DistractionChance > 0 || Challenge.DistractionChance > 0 || Sprint.DistractionChance > 0;

        /// <summary>Calibradas para a partida de 40 s: quem joga na média chega à Prata; quem é rápido, ao Ouro.</summary>
        static IEnumerable<Tier> DefaultTiers() => new[]
        {
            new Tier("Medalha de Bronze", 0, false),
            new Tier("Medalha de Prata", 4000, true),
            new Tier("Medalha de Ouro", 5500, true),
        };

        /// <summary>Pontos de um acerto com o tempo de reação dado: o acerto mais o bônus por rapidez.</summary>
        public int PointsFor(float reactionSeconds)
        {
            float span = NoBonusSeconds - FullBonusSeconds;
            float share = span <= 0 ? (reactionSeconds <= FullBonusSeconds ? 1 : 0) : (NoBonusSeconds - reactionSeconds) / span;
            share = share < 0 ? 0 : share > 1 ? 1 : share;
            return PointsPerHit + (int)Math.Round(SpeedBonus * share, MidpointRounding.AwayFromZero);
        }

        /// <summary>A faixa mais alta que a pontuação alcança, ou null quando não alcança nenhuma.</summary>
        public Tier TierFor(int score)
        {
            Tier best = null;
            foreach (var tier in Tiers)
                if (score >= tier.MinScore) best = tier;
            return best;
        }

        public static ReflexoContent Parse(string json)
        {
            var content = new ReflexoContent();
            if (string.IsNullOrWhiteSpace(json)) return content;

            var root = Json.ParseObject(json);
            var phases = root.GetObject("fases");
            ReadPhase(phases.GetObject("aquecimento"), content.Warmup, true);
            ReadPhase(phases.GetObject("desafio"), content.Challenge, false);
            ReadPhase(phases.GetObject("sprint"), content.Sprint, true);

            content.PointsPerHit = Math.Max(0, root.GetInt("pontosPorAcerto", content.PointsPerHit));
            var bonus = root.GetObject("bonusRapido");
            content.SpeedBonus = Math.Max(0, bonus.GetInt("pontos", content.SpeedBonus));
            content.FullBonusSeconds = Clamp((float)bonus.GetNumber("inteiroAte", content.FullBonusSeconds), 0f, 3f);
            content.NoBonusSeconds = Clamp((float)bonus.GetNumber("zeraEm", content.NoBonusSeconds), content.FullBonusSeconds, 5f);
            content.DistractionPenalty = Math.Max(0, root.GetInt("penalidadeDistracao", content.DistractionPenalty));

            var tiers = ReadTiers(root.GetList("faixas"));
            if (tiers.Count > 0)
            {
                content.Tiers.Clear();
                content.Tiers.AddRange(tiers);
            }

            var steps = root.GetStringList("comoJogar");
            if (steps.Count > 0)
            {
                content.HowToPlay.Clear();
                content.HowToPlay.AddRange(steps);
            }
            return content;
        }

        static void ReadPhase(Dictionary<string, object> obj, PhaseSettings phase, bool hasDuration)
        {
            if (obj == null) return;
            if (hasDuration) phase.Seconds = Clamp((float)obj.GetNumber("segundos", phase.Seconds), 0f, 30f);
            phase.VisibleSeconds = Clamp((float)obj.GetNumber("tempoNaTela", phase.VisibleSeconds), 0.4f, 5f);
            phase.GapSeconds = Clamp((float)obj.GetNumber("intervalo", phase.GapSeconds), 0f, 3f);
            phase.Size = Clamp((float)obj.GetNumber("tamanho", phase.Size), 0.12f, 0.45f);
            phase.DistractionChance = Clamp((float)obj.GetNumber("distracoes", phase.DistractionChance), 0f, 1f);
        }

        /// <summary>
        /// Faixas em ordem de pontos. Quando nenhuma diz "vitoria", todas menos a mais baixa contam como vitória.
        /// </summary>
        static List<Tier> ReadTiers(List<object> list)
        {
            var read = new List<(string name, int points, bool? win)>();
            if (list == null) return new List<Tier>();
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> obj)) continue;
                var name = obj.GetString("nome");
                if (string.IsNullOrWhiteSpace(name)) continue;
                bool? win = obj.ContainsKey("vitoria") ? obj.GetBool("vitoria") : (bool?)null;
                read.Add((name.Trim(), Math.Max(0, obj.GetInt("pontos", 0)), win));
            }
            read.Sort((a, b) => a.points.CompareTo(b.points));

            bool anyFlag = read.Exists(t => t.win.HasValue);
            var tiers = new List<Tier>();
            for (int i = 0; i < read.Count; i++)
                tiers.Add(new Tier(read[i].name, read[i].points, anyFlag ? read[i].win == true : i > 0));
            return tiers;
        }

        static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
