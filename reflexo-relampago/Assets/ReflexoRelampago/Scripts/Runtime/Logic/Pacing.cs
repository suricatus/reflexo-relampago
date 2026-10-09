namespace Suricatus.ReflexoRelampago
{
    /// <summary>Como os alvos se comportam em um instante da partida.</summary>
    public readonly struct Moment
    {
        public readonly Phase Phase;
        public readonly float VisibleSeconds;
        public readonly float GapSeconds;
        public readonly float Size;
        public readonly float DistractionChance;

        public Moment(Phase phase, float visibleSeconds, float gapSeconds, float size, float distractionChance)
        {
            Phase = phase;
            VisibleSeconds = visibleSeconds;
            GapSeconds = gapSeconds;
            Size = size;
            DistractionChance = distractionChance;
        }
    }

    /// <summary>
    /// O ritmo da partida: aquecimento no começo, sprint no fim e desafio no meio. No desafio, o tempo na tela
    /// e o intervalo vão dos valores do desafio aos do sprint, para a partida apertar aos poucos.
    /// </summary>
    public sealed class Pacing
    {
        readonly ReflexoContent content;

        public Pacing(ReflexoContent content, float totalSeconds)
        {
            this.content = content;
            TotalSeconds = totalSeconds > 0 ? totalSeconds : 1;
            WarmupSeconds = content.Warmup.Seconds;
            SprintSeconds = content.Sprint.Seconds;
            // Partida curta demais para as duas fases inteiras: elas encolhem na mesma proporção e o desafio some.
            float fixedPhases = WarmupSeconds + SprintSeconds;
            if (fixedPhases > TotalSeconds)
            {
                float scale = TotalSeconds / fixedPhases;
                WarmupSeconds *= scale;
                SprintSeconds *= scale;
            }
        }

        public float TotalSeconds { get; }
        public float WarmupSeconds { get; }
        public float SprintSeconds { get; }
        public float SprintStart => TotalSeconds - SprintSeconds;

        public Phase PhaseAt(float elapsed)
        {
            if (elapsed < WarmupSeconds) return Phase.Aquecimento;
            if (elapsed >= SprintStart) return Phase.Sprint;
            return Phase.Desafio;
        }

        public Moment At(float elapsed)
        {
            var phase = PhaseAt(elapsed);
            switch (phase)
            {
                case Phase.Aquecimento:
                    return From(phase, content.Warmup);
                case Phase.Sprint:
                    return From(phase, content.Sprint);
                default:
                    float span = SprintStart - WarmupSeconds;
                    float t = span <= 0 ? 1 : (elapsed - WarmupSeconds) / span;
                    var a = content.Challenge;
                    var b = content.Sprint;
                    return new Moment(phase,
                        Lerp(a.VisibleSeconds, b.VisibleSeconds, t),
                        Lerp(a.GapSeconds, b.GapSeconds, t),
                        a.Size,
                        a.DistractionChance);
            }
        }

        static Moment From(Phase phase, PhaseSettings s) => new Moment(phase, s.VisibleSeconds, s.GapSeconds, s.Size, s.DistractionChance);

        static float Lerp(float a, float b, float t) => a + (b - a) * (t < 0 ? 0 : t > 1 ? 1 : t);
    }
}
