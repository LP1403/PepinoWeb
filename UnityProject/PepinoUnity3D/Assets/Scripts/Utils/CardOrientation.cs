using UnityEngine;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Little Games Pack cards lie in XY with the face on +Z.
    /// These helpers put the face toward the table camera / player.
    /// </summary>
    public static class CardOrientation
    {
        /// <summary>Flat on the table, face up (readable from above).</summary>
        public static Quaternion FaceUpOnTable(float yawDegrees = 0f)
        {
            return Quaternion.Euler(-90f, yawDegrees, 0f);
        }

        /// <summary>
        /// Local hand: mostly face-up, tipped toward the south camera (player seat).
        /// <paramref name="fanTwistDegrees"/> fans the card in the hand arc.
        /// </summary>
        public static Quaternion FacePlayerHand(float fanTwistDegrees)
        {
            // -68° X: face readable; Z twist = fan
            return Quaternion.Euler(-68f, 0f, -fanTwistDegrees);
        }

        /// <summary>Opponent / deck: back facing the table camera.</summary>
        public static Quaternion FaceDownOnTable(float yawDegrees = 0f)
        {
            return Quaternion.Euler(90f, yawDegrees, 0f);
        }
    }
}
