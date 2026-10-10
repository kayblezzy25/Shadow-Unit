using UnityEngine;

namespace ShadowUnit
{
    public enum Team { Blue = 0, Red = 1 }

    public static class TeamExtensions
    {
        public static Team Other(this Team t) => t == Team.Blue ? Team.Red : Team.Blue;

        public static Color UiColor(this Team t) =>
            t == Team.Blue ? new Color(0.36f, 0.66f, 1f) : new Color(1f, 0.36f, 0.28f);

        public static string Label(this Team t) => t == Team.Blue ? "BLUE" : "RED";
    }
}
