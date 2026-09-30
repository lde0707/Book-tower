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
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class PlayerInputReader : MonoBehaviour
    {
        /// <summary>WASD/방향키. 아직 카메라 기준으로 변환되지 않은 원본 값</summary>
        public Vector2 Move { get; private set; }

        /// <summary>활공 키를 누르고 있는가</summary>
        public bool GlideHeld { get; private set; }

        /// <summary>이번 프레임에 스페이스를 눌렀는가. 땅에 있으면 점프가 된다</summary>
        public bool JumpPressed { get; private set; }

        /// <summary>이번 프레임에 상호작용 키(F)를 눌렀는가.
        /// 몸이 향한 쪽의 대상 하나에 작용한다 — TargetInteractor 가 읽는다</summary>
        public bool InteractPressed { get; private set; }

        /// <summary>이번 프레임에 좌클릭했는가.
        /// 커서가 가리킨 것에 작용한다 — FoldInteractor·EchoInteractor 가 읽는다.
        /// F 와 나눠 둔 이유: F 를 눌렀을 때 마우스가 놓인 자리로 광선을 쏘면
        /// 엉뚱한 게 눌린다. 두 입력은 '무엇을 고르는가'가 서로 다르다</summary>
        public bool PointerPressed { get; private set; }

        /// <summary>펴기(역방향) 보조키를 누르고 있는가</summary>
        public bool ModifierHeld { get; private set; }
        public bool RunHeld { get; private set; }

        private void Update()
        {
            // 연출·일시정지 중에는 입력을 통째로 막는다
            if (BookTower.UI.PaperUI.BlocksGameplay || (GameFlow.Instance != null && !GameFlow.Instance.AcceptsPlayerInput) || !BookTower.CameraSystem.PaperCamera.AimActive)
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

            float x = (BookTower.UI.PaperSettingsRuntime.Held(3)?1:0) - (BookTower.UI.PaperSettingsRuntime.Held(2)?1:0);
            float z = (BookTower.UI.PaperSettingsRuntime.Held(0)?1:0) - (BookTower.UI.PaperSettingsRuntime.Held(1)?1:0);

            Vector2 raw = new Vector2(x, z);
            Move = raw.sqrMagnitude > 1f ? raw.normalized : raw;

            GlideHeld = BookTower.UI.PaperSettingsRuntime.Held(4);
            JumpPressed = BookTower.UI.PaperSettingsRuntime.Pressed(4);
            ModifierHeld = kb.leftShiftKey.isPressed;
            RunHeld = BookTower.UI.PaperSettingsRuntime.RunHeld;
            InteractPressed = BookTower.UI.PaperSettingsRuntime.Pressed(5);
            PointerPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            // The dialogue owns advance inputs while locomotion/look remain available.
            if(BookTower.UI.PaperUI.Instance&&BookTower.UI.PaperUI.Instance.StoryActive)
            {InteractPressed=false;PointerPressed=false;}
        }

        private static float Axis(KeyControl a, KeyControl b)
            => (a.isPressed || b.isPressed) ? 1f : 0f;

        private void Clear()
        {
            Move = Vector2.zero;
            GlideHeld = false;
            JumpPressed = false;
            InteractPressed = false;
            PointerPressed = false;
            ModifierHeld = false;
            RunHeld = false;
        }
    }
}
