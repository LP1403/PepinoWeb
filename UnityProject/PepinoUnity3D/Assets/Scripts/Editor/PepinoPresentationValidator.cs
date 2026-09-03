#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using PepinoGame.Controllers;
using PepinoGame.Managers;
using PepinoGame.Utils;

namespace PepinoGame.Editor
{
    /// <summary>
    /// Automated presentation checks — run from menu while in Play Mode.
    /// </summary>
    public static class PepinoPresentationValidator
    {
        [MenuItem("Pepino/Validate Presentation (Play Mode)")]
        public static void Validate()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PepinoValidate] Enter Play Mode first.");
                return;
            }

            PepinoAlphaBootstrap.ApplyCameraFrame();

            var key = GameObject.Find("TableKeyLight");
            if (key != null) Object.Destroy(key);

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[PepinoValidate] FAIL no camera");
                return;
            }

            float nearY = cam.WorldToViewportPoint(new Vector3(0, 0.9f, -1.0f)).y;
            float midY = cam.WorldToViewportPoint(new Vector3(0, 0.9f, 0.12f)).y;
            float farY = cam.WorldToViewportPoint(new Vector3(0, 0.9f, 1.0f)).y;

            bool frameOk = nearY > 0.18f && nearY < 0.38f
                           && midY > 0.42f && midY < 0.62f
                           && farY > 0.62f && farY < 0.85f;

            var seats = GameObject.Find("OpponentSeats");
            int seatKids = seats != null ? seats.transform.childCount : 0;
            int seatRends = 0;
            if (seats != null)
            {
                for (int i = 0; i < seats.transform.childCount; i++)
                    seatRends += seats.transform.GetChild(i).GetComponentsInChildren<Renderer>()
                        .Count(r => r.enabled);
            }

            bool rivalsOk = seatKids > 0 && seatRends >= 3;

            var gm = GameManager.Instance;
            int last = gm?.CurrentGameState?.lastPlayedCards?.Count ?? 0;
            var tm = Object.FindAnyObjectByType<TableManager>();
            // Reflection-free: count non-drawpile children
            var tableCont = GameObject.Find("TableContainer");
            int played = 0;
            if (tableCont != null)
            {
                foreach (Transform t in tableCont.transform)
                    if (t.name != "DecorativeDrawPile") played++;
            }

            bool discardOk = last == 0 || played >= last;
            bool lightOk = !Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)
                .Any(l => l.enabled
                          && (l.type == LightType.Point || l.type == LightType.Spot)
                          && l.name != "TableOverheadLight");

            Debug.Log(
                $"[PepinoValidate] frame={(frameOk ? "PASS" : "FAIL")} near={nearY:F2} mid={midY:F2} far={farY:F2} | " +
                $"rivals={(rivalsOk ? "PASS" : "FAIL")} seats={seatKids} rends={seatRends} | " +
                $"discard={(discardOk ? "PASS" : "FAIL")} visual={played}/server={last} | " +
                $"lighting={(lightOk ? "PASS" : "FAIL")}");

            if (frameOk && rivalsOk && discardOk && lightOk)
                Debug.Log("[PepinoValidate] ALL CHECKS PASSED");
            else
                Debug.LogError("[PepinoValidate] SOME CHECKS FAILED — keep iterating");
        }
    }
}
#endif
