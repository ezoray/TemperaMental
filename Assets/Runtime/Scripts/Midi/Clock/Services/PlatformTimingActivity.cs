using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace TemperaMental.Midi.Clock
{
    // Asks the operating system for accurate timers while the clock runs.
    // macOS: declares latency-critical activity ([NSProcessInfo beginActivityWithOptions:reason:]), which turns off
    //   App Nap and timer coalescing. Without it, timers in an app that isn't in front (e.g. while a DAW has focus)
    //   can fire 10–20 ms late, giving uneven MIDI clock pulses.
    // Windows: requests 1 ms timer resolution (timeBeginPeriod), as the default of about 15.6 ms would make the
    //   clock thread oversleep.
    public static class PlatformTimingActivity
    {
        // NSActivityUserInitiatedAllowingIdleSystemSleep | NSActivityLatencyCritical
        const ulong MacActivityOptions = (0x00FFFFFFUL & ~(1UL << 20)) | 0xFF00000000UL;

        const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";
        const uint WindowsTimerPeriodMs = 1;

        static IntPtr macActivityToken;
        static bool isWindowsPeriodSet;

        public static bool IsActive => macActivityToken != IntPtr.Zero || isWindowsPeriodSet;

        public static void Begin(string reason)
        {
            try
            {
                if (IsMacOS && macActivityToken == IntPtr.Zero)
                {
                    BeginMacActivity(reason);
                }
                else if (IsWindows && !isWindowsPeriodSet)
                {
                    isWindowsPeriodSet = TimeBeginPeriod(WindowsTimerPeriodMs) == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Couldn't request accurate timers from the OS: {ex.Message}");
            }
        }

        // every object is checked before use: messaging a bad pointer, or passing a nil reason (which raises an
        // Objective-C exception), would bring the whole process down
        static void BeginMacActivity(string reason)
        {
            IntPtr processInfoClass = GetClass("NSProcessInfo");
            IntPtr stringClass = GetClass("NSString");

            if (processInfoClass == IntPtr.Zero || stringClass == IntPtr.Zero)
            {
                Debug.LogWarning("Accurate timers not requested: NSProcessInfo or NSString not found");
                return;
            }

            IntPtr processInfo = SendMessage(processInfoClass, Selector("processInfo"));
            IntPtr reasonString = CreateNSString(stringClass, reason);

            if (processInfo == IntPtr.Zero || reasonString == IntPtr.Zero)
            {
                Debug.LogWarning("Accurate timers not requested: couldn't get the process info or create the reason text");
                return;
            }

            IntPtr token = SendMessageActivity(processInfo, Selector("beginActivityWithOptions:reason:"), MacActivityOptions, reasonString);

            if (token == IntPtr.Zero)
            {
                Debug.LogWarning("Accurate timers not requested: macOS returned no activity");
                return;
            }

            // the activity lasts as long as the token object is alive, so keep it
            macActivityToken = SendMessage(token, Selector("retain"));
        }

        // [NSString stringWithUTF8String:], with the UTF-8 bytes marshalled by hand
        static IntPtr CreateNSString(IntPtr stringClass, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text + "\0");
            IntPtr buffer = Marshal.AllocHGlobal(bytes.Length);

            try
            {
                Marshal.Copy(bytes, 0, buffer, bytes.Length);

                // the NSString copies the bytes, so the buffer can be freed straight away
                return SendMessagePointer(stringClass, Selector("stringWithUTF8String:"), buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public static void End()
        {
            try
            {
                if (macActivityToken != IntPtr.Zero)
                {
                    IntPtr processInfo = SendMessage(GetClass("NSProcessInfo"), Selector("processInfo"));
                    SendMessagePointer(processInfo, Selector("endActivity:"), macActivityToken);
                    SendMessage(macActivityToken, Selector("release"));
                }

                if (isWindowsPeriodSet)
                {
                    TimeEndPeriod(WindowsTimerPeriodMs);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Couldn't release accurate timers: {ex.Message}");
            }
            finally
            {
                macActivityToken = IntPtr.Zero;
                isWindowsPeriodSet = false;
            }
        }

        static bool IsMacOS =>
            Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor;

        static bool IsWindows =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        // macOS (native libraries are only loaded when first called, so these are safe to declare everywhere)

        [DllImport(ObjCLibrary, EntryPoint = "objc_getClass")]
        static extern IntPtr GetClass(string name);

        [DllImport(ObjCLibrary, EntryPoint = "sel_registerName")]
        static extern IntPtr Selector(string name);

        [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector);

        [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        static extern IntPtr SendMessagePointer(IntPtr receiver, IntPtr selector, IntPtr argument);

        [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        static extern IntPtr SendMessageActivity(IntPtr receiver, IntPtr selector, ulong options, IntPtr reason);

        // Windows

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        static extern uint TimeBeginPeriod(uint periodMs);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        static extern uint TimeEndPeriod(uint periodMs);
    }
}
