using UnityEngine;
using UnityEngine.InputSystem;

namespace BookTower.Presentation
{
    // Isolated geometric experiment, deliberately not a P15 completion trigger.
    // The production story/light/room-surface sequence still needs implementation.
    public class AnamorphosisStudy : MonoBehaviour
    {
        public Transform[] fragments;
        public Vector3[] aligned;
        public Vector3[] unfolded;
        public Camera studyCamera;
        public Vector3 focus;
        [Range(0, 1)] public float amount;
        private float target;
        private int yaw;

        public void SetAmount(float value)
        {
            amount = Mathf.Clamp01(value);
            for (int i = 0; i < fragments.Length; i++)
                fragments[i].localPosition = Vector3.Lerp(unfolded[i], aligned[i], amount);
        }

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.fKey.wasPressedThisFrame) target = 1 - target;
                if (keys.qKey.wasPressedThisFrame) yaw = (yaw + 3) % 4;
                if (keys.eKey.wasPressedThisFrame) yaw = (yaw + 1) % 4;
            }
            SetAmount(Mathf.MoveTowards(amount, target, Time.deltaTime * .6f));
            if (studyCamera != null)
            {
                studyCamera.transform.rotation = Quaternion.Euler(30, yaw * 90, 0);
                studyCamera.transform.position = focus - studyCamera.transform.forward * 45;
            }
        }
    }
}
