#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>

static int objectRequests = 0, otherMessages = 0;
static LRESULT CALLBACK Window(HWND h, UINT m, WPARAM w, LPARAM l)
{
    if (m == WM_GETOBJECT) { ++objectRequests; return 0; }
    if (m == WM_APP) { ++otherMessages; return 0; }
    return DefWindowProcW(h, m, w, l);
}
int wmain(int argc, wchar_t** argv)
{
    if (argc != 2) return 1;
    WNDCLASSW type = {}; type.lpfnWndProc = Window; type.lpszClassName = L"UnityWndClass"; type.hInstance = GetModuleHandleW(nullptr);
    if (!RegisterClassW(&type)) return 2;
    HWND window = CreateWindowW(type.lpszClassName, L"Shutdown guard test", 0, 0, 0, 32, 32, nullptr, nullptr, type.hInstance, nullptr);
    if (!window) return 3;
    SendMessageW(window, WM_GETOBJECT, 0, -25);
    if (objectRequests != 1) return 4;
    HMODULE dll = LoadLibraryW(argv[1]); if (!dll) return 5;
    using Prepare = int(*)(); auto prepare = reinterpret_cast<Prepare>(GetProcAddress(dll, "BTPrepareWindowShutdown"));
    if (!prepare || prepare() != 0) return 6;
    FreeLibrary(dll); // Simulates Unity unloading plugins before the last window message.
    SendMessageW(window, WM_GETOBJECT, 0, -25);
    if (objectRequests != 1) return 7;
    SendMessageW(window, WM_APP, 0, 0);
    if (otherMessages != 1) return 8;
    DestroyWindow(window);
    puts("PASS live accessibility requests reach original window procedure");
    puts("PASS closing accessibility requests are suppressed after plugin unload");
    puts("PASS unrelated window messages still reach original procedure");
    puts("PASS native window destruction completes");
    return 0;
}
