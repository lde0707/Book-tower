using UnityEngine;

namespace BookTower.UI
{
    // Unity 6000.3.10 can retain Windows UIA providers during native shutdown
    // (UUM-146676). Release only this process's providers after Quit is confirmed.
    // Accessibility remains enabled for the entire interactive session.
    internal static class WindowsQuitCleanup
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("BookTowerWindowsQuit")]
        static extern int BTPrepareWindowShutdown();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            Application.quitting -= Disconnect;
            Application.quitting += Disconnect;
        }
        static void Disconnect()
        {
            int result = BTPrepareWindowShutdown();
            Debug.Log("[WindowsQuit] closing-window guard and UIA release; HRESULT=" + result);
        }
#endif
    }
}
