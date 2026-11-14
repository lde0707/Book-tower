#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <commctrl.h>
#include <wchar.h>

// Unity 6000.3.10's PlayerMainWndProc accesses its UIA provider after shutdown
// when a late WM_GETOBJECT/UiaRootObjectId request arrives (UUM-146676).
// https://learn.microsoft.com/windows/win32/winauto/wm-getobject
// Install only after Application.quitting: live-session accessibility is unchanged.
static int installed = 0;
static constexpr UINT_PTR GuardId = 0x424f4f4b;

static LRESULT CALLBACK ClosingWindow(HWND window, UINT message, WPARAM wp, LPARAM lp,
    UINT_PTR id, DWORD_PTR)
{
    if (message == WM_GETOBJECT) return 0;
    if (message == WM_NCDESTROY) RemoveWindowSubclass(window, ClosingWindow, id);
    return DefSubclassProc(window, message, wp, lp);
}

static BOOL CALLBACK GuardUnityWindow(HWND window, LPARAM)
{
    wchar_t name[128] = {};
    GetClassNameW(window, name, 128);
    if (wcscmp(name, L"UnityWndClass") == 0 &&
        SetWindowSubclass(window, ClosingWindow, GuardId, 0)) ++installed;
    return TRUE;
}

extern "C" __declspec(dllexport) int BTPrepareWindowShutdown()
{
    // The native callback must outlive Mono and Unity's plugin unloading phase.
    HMODULE self = nullptr;
    if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
        reinterpret_cast<LPCWSTR>(&BTPrepareWindowShutdown), &self))
        return static_cast<int>(HRESULT_FROM_WIN32(GetLastError()));
    EnumThreadWindows(GetCurrentThreadId(), GuardUnityWindow, 0);
    if (!installed) return static_cast<int>(HRESULT_FROM_WIN32(ERROR_NOT_FOUND));

    // Guard first: Microsoft disallows re-publishing a provider during disconnect.
    // https://learn.microsoft.com/windows/win32/api/uiautomationcoreapi/nf-uiautomationcoreapi-uiadisconnectallproviders
    HMODULE uia = LoadLibraryW(L"UIAutomationCore.dll");
    if (!uia) return static_cast<int>(HRESULT_FROM_WIN32(GetLastError()));
    using Disconnect = HRESULT(WINAPI*)();
    auto disconnect = reinterpret_cast<Disconnect>(GetProcAddress(uia, "UiaDisconnectAllProviders"));
    HRESULT result = disconnect ? disconnect() : HRESULT_FROM_WIN32(ERROR_PROC_NOT_FOUND);
    FreeLibrary(uia);
    return static_cast<int>(result);
}
