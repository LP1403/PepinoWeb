using UnityEngine;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Pack cards: face in XY, thin axis Z (face ≈ +Z).
    /// </summary>
    public static class CardOrientation
    {
        public static Quaternion FaceUpOnTable(float yawDegrees = 0f)
        {
            return Quaternion.Euler(-90f, yawDegrees, 0f);
        }

        /// <summary>
        /// Camera-child hand: face the player (card +Z toward camera = local -Z → Y 180).
        /// </summary>
        public static Quaternion FacePlayerHand(float fanTwistDegrees)
        {
            // Slight lean so they feel held, not a sticker
            return Quaternion.Euler(8f, 180f, -fanTwistDegrees);
        }

        public static Quaternion FaceDownOnTable(float yawDegrees = 0f)
        {
            return Quaternion.Euler(90f, yawDegrees, 0f);
        }

        /// <summary>Opponent backs standing on the rim, facing roughly toward table center / camera.</summary>
        public static Quaternion OpponentBack(float yawTowardCenterDegrees)
        {
            return Quaternion.Euler(65f, yawTowardCenterDegrees, 0f);
        }
    }
}
