using System;
using System.Runtime.InteropServices;

namespace KinectDesktopController
{
    public static class TouchInjector
    {
        [DllImport("User32.dll")]
        public static extern bool InitializeTouchInjection(uint maxCount, TouchFeedback feedbackMode);

        [DllImport("User32.dll")]
        public static extern bool InjectTouchInput(uint count, [MarshalAs(UnmanagedType.LPArray)] POINTER_TOUCH_INFO[] contacts);

        public enum TouchFeedback : uint
        {
            DEFAULT = 0x1,
            INDIRECT = 0x2,
            NONE = 0x3
        }

        [Flags]
        public enum POINTER_FLAGS : uint
        {
            NONE = 0x00000000,
            NEW = 0x00000001,
            INRANGE = 0x00000002,
            INCONTACT = 0x00000004,
            FIRSTBUTTON = 0x00000010,
            SECONDBUTTON = 0x00000020,
            THIRDBUTTON = 0x00000040,
            FOURTHBUTTON = 0x00000080,
            FIFTHBUTTON = 0x00000100,
            PRIMARY = 0x00002000,
            CONFIDENCE = 0x00004000,
            CANCELED = 0x00008000,
            DOWN = 0x00010000,
            UPDATE = 0x00020000,
            UP = 0x00040000,
            WHEEL = 0x00080000,
            HWHEEL = 0x00100000,
            CAPTURECHANGED = 0x00200000,
            HASTRANSFORM = 0x00400000
        }

        [Flags]
        public enum TOUCH_FLAGS : uint
        {
            NONE = 0x00000000
        }

        [Flags]
        public enum TOUCH_MASK : uint
        {
            NONE = 0x00000000,
            CONTACTAREA = 0x00000001,
            ORIENTATION = 0x00000002,
            PRESSURE = 0x00000004
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTER_INFO
        {
            public uint pointerType;
            public uint pointerId;
            public uint frameId;
            public POINTER_FLAGS pointerFlags;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
            public POINT ptPixelLocation;
            public POINT ptHimetricLocation;
            public POINT ptPixelLocationRaw;
            public POINT ptHimetricLocationRaw;
            public uint dwTime;
            public uint historyCount;
            public int InputData;
            public uint dwKeyStates;
            public ulong PerformanceCount;
            public int ButtonChangeType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTER_TOUCH_INFO
        {
            public POINTER_INFO pointerInfo;
            public TOUCH_FLAGS touchFlags;
            public TOUCH_MASK touchMask;
            public RECT rcContact;
            public RECT rcOrientation;
            public uint pressure;
            public uint orientation;
        }

        public const uint PT_TOUCH = 0x00000002;

        public static void Init()
        {
            InitializeTouchInjection(1, TouchFeedback.DEFAULT);
        }

        public static void InjectTouch(int x, int y, POINTER_FLAGS flags)
        {
            var contacts = new POINTER_TOUCH_INFO[1];
            contacts[0] = new POINTER_TOUCH_INFO
            {
                pointerInfo = new POINTER_INFO
                {
                    pointerType = PT_TOUCH,
                    pointerId = 1,
                    ptPixelLocation = new POINT { x = x, y = y },
                    pointerFlags = flags
                },
                touchFlags = TOUCH_FLAGS.NONE,
                touchMask = TOUCH_MASK.CONTACTAREA | TOUCH_MASK.ORIENTATION | TOUCH_MASK.PRESSURE,
                pressure = 1024,
                orientation = 90,
                rcContact = new RECT { left = x - 5, top = y - 5, right = x + 5, bottom = y + 5 },
                rcOrientation = new RECT { left = x - 5, top = y - 5, right = x + 5, bottom = y + 5 }
            };

            InjectTouchInput(1, contacts);
        }
    }
}
