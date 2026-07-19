# Kinect Desktop Controller

Kinect Desktop Controller is a background application for Windows that allows you to control your mouse, interact with your desktop, and send media commands using your body and voice through an Xbox One Kinect sensor.

Built entirely on **.NET 8.0 (WPF)**, this tool leverages the official Kinect for Windows SDK 2.0 to provide seamless, hands-free navigation.

## Features

- **Hands-Free Mouse Control:** Move the mouse by pointing your hand towards the screen.
- **Click & Drag:** Close your hand into a fist to simulate a left mouse click. Keep it closed to drag windows or select text.
- **Scroll:** Move your other hand up and down to intuitively scroll through web pages or documents.
- **Smart Sleep/Wake Gestures:** Need to step away or watch a movie? Simply raise both hands above your head in a "Lasso" gesture (index and middle fingers pointing out) to instantly suspend tracking. Perform the gesture again to wake it back up.
- **Voice Commands:** Includes a built-in voice recognition engine. Just say `"play"` or `"pause"` out loud to control your background media players (Spotify, YouTube, VLC, etc.) completely hands-free.
- **Non-Intrusive UI:** Runs transparently over your desktop with a minimal feedback ring so you always know where your virtual cursor is without obstructing your workflow.

## How to Run

1. See `INSTALL.md` for full installation prerequisites (requires .NET 8.0 and the Kinect SDK v2.0).
2. Ensure your Kinect is plugged in and powered on.
3. Launch `KinectDesktopController.exe`.
4. Stand or sit in front of the Kinect until it tracks your body.
5. Raise your right hand to take control of the cursor!

## Gestures & Controls

| Action | Physical Gesture |
|---|---|
| **Move Cursor** | Point your open right hand towards the screen and move it around. The interaction box is calibrated to chest-height for comfortable usage without fully extending your arm. |
| **Left Click / Drag** | Close your right hand into a fist. A click sound will play. Move your fist to drag. Open your hand to release. |
| **Scroll** | Raise your left hand while controlling the cursor to scroll the active window up or down. |
| **Sleep / Wake** | Raise both hands above your head in a "Lasso" gesture (two fingers pointing out). The app will suspend tracking so you can watch movies uninterrupted. Do it again to resume. |

## Voice Commands
The app continuously listens for media commands. Say any of the following to globally play or pause your music/videos:
- `"play"`
- `"pause"`
- `"play media"`
- `"pause media"`
- `"play video"`
- `"pause video"`

*(Note: Voice commands require a quiet environment or a trained Windows Speech Recognition profile to work reliably over background noise).*
