using System;
using System.Collections.Generic;

namespace Suricatus.ReflexoRelampago
{
    public enum PieceKind
    {
        Target,
        Distraction
    }

    /// <summary>Por que uma peça saiu da tela.</summary>
    public enum PieceEnd
    {
        /// <summary>Alvo tocado.</summary>
        Hit,
        /// <summary>Alvo que sumiu sem ser tocado.</summary>
        Expired,
        /// <summary>Distração tocada.</summary>
        Tapped,
        /// <summary>Distração que saiu junto com o alvo dela, ou peça recolhida no fim da partida.</summary>
        Gone
    }

    /// <summary>Um alvo ou uma distração na tela. Posição e tamanho em fração da área do jogo (0 a 1).</summary>
    public sealed class Piece
    {
        public readonly int Id;
        public readonly PieceKind Kind;
        /// <summary>Centro da peça.</summary>
        public readonly float X, Y;
        /// <summary>Lado da peça.</summary>
        public readonly float Size;
        /// <summary>Qual das artes do cliente usar (alvo-1, alvo-2…).</summary>
        public readonly int Variant;
        public readonly float ShownAt;
        public readonly float HidesAt;

        public Piece(int id, PieceKind kind, float x, float y, float size, int variant, float shownAt, float hidesAt)
        {
            Id = id;
            Kind = kind;
            X = x;
            Y = y;
            Size = size;
            Variant = variant;
            ShownAt = shownAt;
            HidesAt = hidesAt;
        }

        public float VisibleSeconds => HidesAt - ShownAt;
    }

    public enum TapOutcome
    {
        /// <summary>Peça que já tinha saído (ex.: segundo toque rápido). Não conta.</summary>
        Ignored,
        Hit,
        Distraction
    }

    public readonly struct TapResult
    {
        public readonly TapOutcome Outcome;
        /// <summary>Pontos do acerto, ou a penalidade da distração.</summary>
        public readonly int Points;
        public readonly float ReactionSeconds;
        public readonly Piece Piece;

        public TapResult(TapOutcome outcome, int points, float reactionSeconds, Piece piece)
        {
            Outcome = outcome;
            Points = points;
            ReactionSeconds = reactionSeconds;
            Piece = piece;
        }
    }

    /// <summary>
    /// Uma partida do Reflexo Relâmpago, sem Unity: um alvo por vez, às vezes com uma distração junto,
    /// no ritmo do <see cref="Pacing"/>. Quem desenha escuta <see cref="Shown"/> e <see cref="Hidden"/>.
    /// </summary>
    public sealed class ReflexMatch
    {
        /// <summary>Espera entre o "Já!" e o primeiro alvo.</summary>
        public const float FirstTargetDelay = 0.3f;
        /// <summary>Folga das peças até a borda da área.</summary>
        public const float EdgeMargin = 0.02f;
        /// <summary>Distância mínima entre um alvo e o anterior, para os alvos mudarem de lugar.</summary>
        public const float MinJump = 0.3f;
        const int PlacementTries = 24;

        readonly ReflexoContent content;
        readonly Random random;
        readonly int targetVariants;
        readonly int distractionVariants;
        int nextId = 1;
        float nextSpawnAt = FirstTargetDelay;
        float lastX = -1, lastY = -1;
        int lastVariant = -1;

        public ReflexMatch(ReflexoContent content, float totalSeconds, Random random, int targetVariants = 1, int distractionVariants = 1)
        {
            this.content = content;
            this.random = random;
            this.targetVariants = Math.Max(1, targetVariants);
            this.distractionVariants = Math.Max(1, distractionVariants);
            Pacing = new Pacing(content, totalSeconds);
        }

        public event Action<Piece> Shown;
        public event Action<Piece, PieceEnd> Hidden;

        public Pacing Pacing { get; }
        public float Elapsed { get; private set; }
        public Phase Phase => Pacing.PhaseAt(Elapsed);
        public bool Stopped { get; private set; }
        public Piece Target { get; private set; }
        public Piece Distraction { get; private set; }

        public int Hits { get; private set; }
        /// <summary>Alvos que sumiram sem ser tocados (oportunidades perdidas).</summary>
        public int Expired { get; private set; }
        public int DistractionTaps { get; private set; }
        public int EmptyTaps { get; private set; }

        /// <summary>Toques na área do jogo: em alvo, em distração ou no vazio.</summary>
        public int Taps => Hits + DistractionTaps + EmptyTaps;

        /// <summary>Alvos que tiveram a chance inteira: acertados ou perdidos. O que estava na tela no fim não conta.</summary>
        public int Goal => Hits + Expired;

        /// <summary>Acertos entre todos os toques na área do jogo (0 a 1). Tocar a esmo derruba a precisão.</summary>
        public float Precision => Taps == 0 ? 0 : (float)Hits / Taps;

        public int PrecisionPercent => (int)Math.Round(Precision * 100, MidpointRounding.AwayFromZero);

        public void Tick(float deltaTime)
        {
            if (Stopped || deltaTime <= 0) return;
            Elapsed += deltaTime;

            if (Target != null && Elapsed >= Target.HidesAt)
            {
                Expired++;
                ClearTarget(PieceEnd.Expired);
            }

            if (Target == null && Elapsed >= nextSpawnAt && Elapsed < Pacing.TotalSeconds) Spawn();
        }

        public TapResult TapPiece(int id)
        {
            if (Stopped) return default;

            if (Target != null && Target.Id == id)
            {
                var piece = Target;
                float reaction = Elapsed - piece.ShownAt;
                int points = content.PointsFor(reaction);
                Hits++;
                ClearTarget(PieceEnd.Hit);
                return new TapResult(TapOutcome.Hit, points, reaction, piece);
            }

            if (Distraction != null && Distraction.Id == id)
            {
                var piece = Distraction;
                DistractionTaps++;
                HideDistraction(PieceEnd.Tapped);
                return new TapResult(TapOutcome.Distraction, content.DistractionPenalty, Elapsed - piece.ShownAt, piece);
            }

            return default;
        }

        /// <summary>Toque na área do jogo fora de qualquer peça.</summary>
        public void TapEmpty()
        {
            if (!Stopped) EmptyTaps++;
        }

        /// <summary>Fim da partida: recolhe o que está na tela, sem contar como perdido.</summary>
        public void Stop()
        {
            if (Stopped) return;
            Stopped = true;
            HideDistraction(PieceEnd.Gone);
            HideTarget(PieceEnd.Gone);
        }

        /// <summary>O alvo saiu (tocado ou perdido): a distração dele sai junto e o próximo vem depois do intervalo.</summary>
        void ClearTarget(PieceEnd end)
        {
            nextSpawnAt = Elapsed + Pacing.At(Elapsed).GapSeconds;
            HideDistraction(PieceEnd.Gone);
            HideTarget(end);
        }

        void HideTarget(PieceEnd end)
        {
            if (Target == null) return;
            var piece = Target;
            Target = null;
            Hidden?.Invoke(piece, end);
        }

        void HideDistraction(PieceEnd end)
        {
            if (Distraction == null) return;
            var piece = Distraction;
            Distraction = null;
            Hidden?.Invoke(piece, end);
        }

        void Spawn()
        {
            var moment = Pacing.At(Elapsed);
            float size = moment.Size;
            float hidesAt = Elapsed + moment.VisibleSeconds;

            bool first = lastX < 0;
            float fromX = lastX, fromY = lastY;
            var (x, y) = Place(size, (px, py) => first ? float.MaxValue : Distance(px, py, fromX, fromY), MinJump);
            Target = new Piece(nextId++, PieceKind.Target, x, y, size, NextTargetVariant(), Elapsed, hidesAt);
            lastX = x;
            lastY = y;
            Shown?.Invoke(Target);

            if (moment.DistractionChance <= 0 || random.NextDouble() >= moment.DistractionChance) return;
            // A distração tem o tamanho do alvo e nunca encosta nele; sem lugar para ela, o alvo vem sozinho.
            float clearance = size * 1.15f;
            var (dx, dy) = Place(size, (px, py) => Distance(px, py, x, y), clearance);
            if (Distance(dx, dy, x, y) < clearance) return;
            Distraction = new Piece(nextId++, PieceKind.Distraction, dx, dy, size, random.Next(distractionVariants), Elapsed, hidesAt);
            Shown?.Invoke(Distraction);
        }

        /// <summary>Alvos trocam de arte a cada vez, quando o cliente tem mais de uma.</summary>
        int NextTargetVariant()
        {
            int variant = random.Next(targetVariants);
            if (targetVariants > 1 && variant == lastVariant) variant = (variant + 1 + random.Next(targetVariants - 1)) % targetVariants;
            lastVariant = variant;
            return variant;
        }

        /// <summary>
        /// Sorteia um centro para uma peça de lado <paramref name="size"/> dentro da área: o primeiro com
        /// <paramref name="distance"/> de pelo menos <paramref name="wanted"/>, ou o mais distante dos sorteados.
        /// </summary>
        (float x, float y) Place(float size, Func<float, float, float> distance, float wanted)
        {
            float min = size / 2 + EdgeMargin;
            float max = 1 - min;
            (float x, float y) best = (0.5f, 0.5f);
            float bestDistance = float.MinValue;
            for (int i = 0; i < PlacementTries; i++)
            {
                float x = Range(min, max), y = Range(min, max);
                float d = distance(x, y);
                if (d >= wanted) return (x, y);
                if (d > bestDistance)
                {
                    bestDistance = d;
                    best = (x, y);
                }
            }
            return best;
        }

        float Range(float min, float max) => max <= min ? (min + max) / 2 : min + (float)random.NextDouble() * (max - min);

        static float Distance(float ax, float ay, float bx, float by)
        {
            float dx = ax - bx, dy = ay - by;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
