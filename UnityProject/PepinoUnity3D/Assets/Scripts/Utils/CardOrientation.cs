using UnityEngine;

namespace PepinoGame.Utils
{
    /// <summary>
    /// Pack cards: face in XY, thin axis Z (face ≈ +Z).
    /// Local hand uses LookRotation toward the camera in HandManager (viewport layout).
    /// </summary>
    public static class CardOrientation
    {
        public static Quaternion FaceUpOnTable(float yawDegrees = 0f)
        {
            return Quaternion.Euler(-90f, yawDegrees, 0f);
        }

        public static Quaternion FaceDownOnTable(float yawDegrees = 0f)
        {
            return Quaternion.Euler(90f, yawDegrees, 0f);
        }

        public static Quaternion OpponentBack(float yawTowardCenterDegrees)
        {
            return Quaternion.Euler(70f, yawTowardCenterDegrees, 0f);
        }
    }
}
