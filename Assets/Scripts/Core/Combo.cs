using System;

namespace Garapon.Core
{
    // Picking trash up in quick succession builds a combo that raises its value,
    // which rewards walking good routes instead of leaving everything to the robot.
    public sealed class Combo
    {
        readonly GameData data;
        readonly GameState state;

        double lastPickup = double.NegativeInfinity;

        public Combo(GameData data, GameState state)
        {
            this.data = data;
            this.state = state;
        }

        public long Count { get; private set; }

        // Registers a pickup at the given time and returns the multiplier for it.
        public double Register(double time)
        {
            Count = IsActive(time) ? Count + 1 : 1;
            lastPickup = time;
            if (Count > state.BestCombo)
            {
                state.BestCombo = Count;
            }

            return Multiplier;
        }

        public bool IsActive(double time) => time - lastPickup <= data.ComboWindowSeconds;

        // Seconds left before the combo breaks, for the HUD.
        public double SecondsLeft(double time) => Math.Max(0, data.ComboWindowSeconds - (time - lastPickup));

        public double Multiplier => 1 + Math.Min(Math.Max(Count - 1, 0), data.ComboMaxSteps) * data.ComboStep;
    }
}
