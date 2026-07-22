using UnityEngine;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Little Games Pack cards: face lies in XY, thin axis is Z (face ≈ +Z).
    /// </summary>
    public static class CardOrientation
    {
        /// <summary>Flat on the table, face readable from above.</summary>
        public static Quaternion FaceUpOnTable(float yawDegrees = 0f)
        {
            // +Z face → +Y up
            return Quaternion.Euler(-90f, yawDegrees, 0f);
        }

        /// <summary>
        /// Held in front of the camera (HandContainer parented to camera).
        /// Face points at the player; Z twist fans the hand.
        /// </summary>
        public static Quaternion FacePlayerHand(float fanTwistDegrees)
        {
            // Slight lean so they feel held, not a flat UI sticker
            return Quaternion.Euler(12f, 180f, -fanTwistDegrees);
        }

        /// <summary>Back up on the table (opponents).</summary>
        public static Quaternion FaceDownOnTable(float yawDegrees = 0f)
        {
            return Quaternion.Euler(90f, yawDegrees, 0f);
        }
    }
}
