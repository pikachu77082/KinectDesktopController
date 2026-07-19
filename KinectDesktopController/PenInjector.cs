using System;
using System.Runtime.InteropServices;

namespace KinectDesktopController
{
    public static class PenInjector
    {
        public enum POINTER_INPUT_TYPE : uint
        {
            PT_POINTER = 0x00000001,
            PT_TOUCH = 0x00000002,
            PT_PEN = 0x00000003,
            PT_MOUSE = 0x00000004
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
        public enum PEN_FLAGS : uint
        {
            NONE = 0x00000000,
            BARREL = 0x00000001,
            INVERTED = 0x00000002,
            ERASER = 0x00000004
        }

        [Flags]
        public enum PEN_MASK : uint
        {
            NONE = 0x00000000,
            PRESSURE = 0x00000001,
            ROTATION = 0x00000002,
            TILT_X = 0x00000004,
            TILT_Y = 0x00000008
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTER_INFO
        {
            public POINTER_INPUT_TYPE pointerType;
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
        public struct POINTER_PEN_INFO
        {
            public POINTER_INFO pointerInfo;
            public PEN_FLAGS penFlags;
            public PEN_MASK penMask;
            public uint pressure;
            public uint rotation;
            public int tiltX;
            public int tiltY;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct POINTER_TYPE_INFO
        {
            [FieldOffset(0)]
            public POINTER_INPUT_TYPE type;
            
            [FieldOffset(8)]
            public POINTER_PEN_INFO penInfo;
        }

        public enum POINTER_FEEDBACK_MODE : uint
        {
            DEFAULT = 1,
            INDIRECT = 2,
            NONE = 3
        }

        [DllImport("user32.dll")]
        public static extern IntPtr CreateSyntheticPointerDevice(POINTER_INPUT_TYPE pointerType, uint maxCount, POINTER_FEEDBACK_MODE mode);

        [DllImport("user32.dll")]
        public static extern void DestroySyntheticPointerDevice(IntPtr device);

        [DllImport("user32.dll")]
        public static extern bool InjectSyntheticPointerInput(IntPtr device, [MarshalAs(UnmanagedType.LPArray)] POINTER_TYPE_INFO[] pointerInfo, uint count);

        private static IntPtr _penDevice = IntPtr.Zero;

        public static void Init()
        {
            if (_penDevice == IntPtr.Zero)
            {
                _penDevice = CreateSyntheticPointerDevice(POINTER_INPUT_TYPE.PT_PEN, 1, POINTER_FEEDBACK_MODE.DEFAULT);
            }
        }

        public static void InjectPen(int x, int y, POINTER_FLAGS flags)
        {
            if (_penDevice == IntPtr.Zero) Init();

            var info = new POINTER_TYPE_INFO[1];
            info[0] = new POINTER_TYPE_INFO();
            info[0].type = POINTER_INPUT_TYPE.PT_PEN;
            info[0].penInfo = new POINTER_PEN_INFO
            {
                pointerInfo = new POINTER_INFO
                {
                    pointerType = POINTER_INPUT_TYPE.PT_PEN,
                    pointerId = 1,
                    ptPixelLocation = new POINT { x = x, y = y },
                    pointerFlags = flags
                },
                penFlags = PEN_FLAGS.NONE,
                penMask = PEN_MASK.PRESSURE,
                pressure = 1024
            };

            InjectSyntheticPointerInput(_penDevice, info, 1);
        }
    }
}
