using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using BookTower.Core;

namespace BookTower.PlayerSystem
{
    /// <summary>
    /// 입력을 한 곳에서만 읽는다.
    ///
    /// 이렇게 분리하는 이유:
    ///  - 상태 클래스들이 Keyboard.current를 직접 만지지 않게 되어 테스트·교체가 쉬움
    ///  - 게임패드를 나중에 붙일 때 이 파일만 고치면 됨
    ///  - 연출 중(Cutscene) 입력 차단을 한 곳에서 처리할 수 있음
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputReader : MonoBehaviour
    {
        /// <summary>WASD/방향키. 아직 카메라 기준으로 변환되지 않은 원본 값</summary>
        public Vector2 Move { get; private set; }

        /// <summary>활공 키를 누르고 있는가</summary>
        public bool GlideHeld { get; private set; }

        /// <summary>이번 프레임에 스페이스를 눌렀는가. 땅에 있으면 점프가 된다</summary>
        public bool JumpPressed { get; private set; }

        /// <summary>이번 프레임에 상호작용을 눌렀는가</summary>
        public bool InteractPressed { get; private set; }

        /// <summary>펴기(역방향) 보조키를 누르고 있는가</summary>
        public bool ModifierHeld { get; private set; }

        private void Update()
        {
            // 연출·일시정지 중에는 입력을 통째로 막는다
            if (GameFlow.Instance != null && !GameFlow.Instance.AcceptsPlayerInput)
            {
                Clear();
                return;
            }

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb == null)
            {
                Clear();
                return;
            }

            float x = Axis(kb.dKey, kb.rightArrowKey) - Axis(kb.aKey, kb.leftArrowKey);
            float z = Axis(kb.wKey, kb.upArrowKey) - Axis(kb.sKey, kb.downArrowKey);

            Vector2 raw = new Vector2(x, z);
            Move = raw.sqrMagnitude > 1f ? raw.normalized : raw;

            GlideHeld = kb.spaceKey.isPressed;
            JumpPressed = kb.spaceKey.wasPressedThisFrame;
            ModifierHeld = kb.leftShiftKey.isPressed;
            InteractPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        private static float Axis(KeyControl a, KeyControl b)
            => (a.isPressed || b.isPressed) ? 1f : 0f;

        private void Clear()
        {
            Move = Vector2.zero;
            GlideHeld = false;
            JumpPressed = false;
            InteractPressed = false;
            ModifierHeld = false;
        }
    }
}
