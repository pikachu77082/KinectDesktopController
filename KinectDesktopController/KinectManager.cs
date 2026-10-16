using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Text.Json;
using System.Speech.Recognition;
using System.Runtime.InteropServices;

namespace KinectDesktopController
{

    public class KinectManager
    {
        private KinectSensor _sensor;
        private MultiSourceFrameReader _multiReader;
        private Body[] _bodies;
        private OverlayRenderer _renderer;
        private MainWindow _window;
        private SignalFilter _filter;
        
        private ulong _masterTrackingId = 0;
        private int _masterBodyIndex = -1;
        
        private FaceFrameSource[] _faceSources = new FaceFrameSource[6];
        private FaceFrameReader[] _faceReaders = new FaceFrameReader[6];
        private bool[] _faceTracked = new bool[6];

        private double _dpiX = 1.0;
        private double _dpiY = 1.0;

        private SoundPlayer _clickSound;
        private bool _isTrackingActive = true;
        private DateTime _lastInteractionTime = DateTime.Now;
        private CameraSpacePoint _lastHandPosition;

        private SpeechRecognitionEngine _speechEngine;

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        
        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public KinectManager(MainWindow window)
        {
            _window = window;
            _filter = new SignalFilter(0.2);
        }

        public void Start()
        {
            try
            {
                _clickSound = new SoundPlayer(@"C:\Windows\Media\Windows Navigation Start.wav");
                _clickSound.LoadAsync();

                _speechEngine = new SpeechRecognitionEngine();
                Choices commands = new Choices();
                commands.Add(new string[] { "play", "pause", "play video", "pause video", "play media", "pause media" });

                GrammarBuilder gb = new GrammarBuilder();
                gb.Append(commands);

                Grammar g = new Grammar(gb);
                _speechEngine.LoadGrammar(g);

                _speechEngine.SpeechRecognized += SpeechRecognized;
                _speechEngine.SetInputToDefaultAudioDevice();
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch { }

            _window.Dispatcher.Invoke(() => 
            {
                System.Windows.PresentationSource source = System.Windows.PresentationSource.FromVisual(_window);
                if (source != null && source.CompositionTarget != null)
                {
                    _dpiX = source.CompositionTarget.TransformToDevice.M11;
                    _dpiY = source.CompositionTarget.TransformToDevice.M22;
                }
            });

            _sensor = KinectSensor.GetDefault();
            if (_sensor == null)
            {
                _window.UpdateStatus("Kinect Sensor not found.");
                return;
            }
            
            _sensor.IsAvailableChanged += (s, e) => {
                _window.Dispatcher.Invoke(() => {
                    _window.UpdateStatus(e.IsAvailable ? "Waiting for Master User..." : "Kinect disconnected or initializing...", e.IsAvailable ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Red);
                });
            };

            _window.UpdateStatus(_sensor.IsAvailable ? "Waiting for Master User..." : "Kinect disconnected or initializing...", _sensor.IsAvailable ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Red);

            _multiReader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
            _multiReader.MultiSourceFrameArrived += MultiReader_MultiSourceFrameArrived;

            _bodies = new Body[_sensor.BodyFrameSource.BodyCount];
            
            // Initialize Face sources
            FaceFrameFeatures faceFeatures = FaceFrameFeatures.BoundingBoxInColorSpace | FaceFrameFeatures.PointsInColorSpace | FaceFrameFeatures.RotationOrientation | FaceFrameFeatures.FaceEngagement;
            for (int i = 0; i < _sensor.BodyFrameSource.BodyCount; i++)
            {
                _faceSources[i] = new FaceFrameSource(_sensor, 0, faceFeatures);
                _faceReaders[i] = _faceSources[i].OpenReader();
                
                int index = i;
                _faceReaders[i].FrameArrived += (s, e) => FaceReader_FrameArrived(s, e, index);
            }

            _sensor.Open();
            
            _renderer = new OverlayRenderer(_sensor.DepthFrameSource.FrameDescription.Width, _sensor.DepthFrameSource.FrameDescription.Height);
            _window.SetShadowSource(_renderer.Bitmap);
        }

        private void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e, int index)
        {
            using (FaceFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null && frame.IsTrackingIdValid && frame.FaceFrameResult != null)
                {
                    _faceTracked[index] = true;
                }
                else
                {
                    _faceTracked[index] = false;
                }
            }
        }

        private void MultiReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiFrame = e.FrameReference.AcquireFrame();
            if (multiFrame == null) return;

                using (BodyFrame bodyFrame = multiFrame.BodyFrameReference.AcquireFrame())
                using (BodyIndexFrame bodyIndexFrame = multiFrame.BodyIndexFrameReference.AcquireFrame())
                {
                    if (bodyFrame == null || bodyIndexFrame == null) return;

                    bodyFrame.GetAndRefreshBodyData(_bodies);

                    var trackedBodies = _bodies.Where(b => b.IsTracked).ToList();
                    
                    if (trackedBodies.Count > 4)
                    {
                        _window.UpdateStatus("Too many people detected (>4). Input disabled.", System.Windows.Media.Brushes.Red);
                        _window.HideCursor();
                        _renderer.RenderBodyIndex(bodyIndexFrame, -1); // Clear shadow
                        return;
                    }

                    UpdateMasterUser(trackedBodies);

                    if (_masterBodyIndex != -1 && _masterBodyIndex < _bodies.Length)
                    {
                        Body masterBody = _bodies[_masterBodyIndex];
                        bool hasFace = _faceTracked[_masterBodyIndex];
                        
                        CheckWakeSleepGesture(masterBody);

                        if (hasFace)
                        {

                            if (_isTrackingActive)
                            {
                                _window.UpdateStatus("Master User Locked (Tracking)");
                                _renderer.RenderBodyIndex(bodyIndexFrame, _masterBodyIndex);
                                ProcessMasterUserInput(masterBody);
                                
                                if (_isTrackingActive)
                                {
                                    _window.ShowCursor();
                                }
                                else
                                {
                                    if ((DateTime.Now - _lastInteractionTime).TotalSeconds < 3)
                                    {
                                        _window.UpdateStatus("Tracking paused (Inactivity). Lasso both hands above head to wake.", System.Windows.Media.Brushes.Yellow);
                                    }
                                    else
                                    {
                                        _window.UpdateStatus("");
                                    }
                                    _renderer.RenderBodyIndex(bodyIndexFrame, -1);
                                    _window.HideCursor();
                                }
                            }
                            else
                            {
                                if ((DateTime.Now - _lastInteractionTime).TotalSeconds < 3)
                                {
                                    _window.UpdateStatus("Tracking Suspended. Lasso both hands above head to wake.", System.Windows.Media.Brushes.Yellow);
                                }
                                else
                                {
                                    _window.UpdateStatus("");
                                }
                                _renderer.RenderBodyIndex(bodyIndexFrame, -1);
                                _window.HideCursor();
                            }
                        }
                        else
                        {
                            _window.UpdateStatus("Master User Detected (Awaiting Face Validate)...", System.Windows.Media.Brushes.Yellow);
                            _window.HideCursor();
                            _renderer.RenderBodyIndex(bodyIndexFrame, _masterBodyIndex);
                        }
                    }
                    else
                    {
                        _window.UpdateStatus("Waiting for Master User...");
                        _window.HideCursor();
                        _renderer.RenderBodyIndex(bodyIndexFrame, -1); // Clear shadow
                    }
                }
        }

        private void UpdateMasterUser(List<Body> trackedBodies)
        {
            if (trackedBodies.Count == 0)
            {
                _masterTrackingId = 0;
                _masterBodyIndex = -1;
                return;
            }

            // Check if our master is still tracked
            bool masterStillTracked = false;
            for (int i = 0; i < _bodies.Length; i++)
            {
                if (_bodies[i].IsTracked && _bodies[i].TrackingId == _masterTrackingId)
                {
                    masterStillTracked = true;
                    _masterBodyIndex = i;
                    break;
                }
            }

            if (!masterStillTracked)
            {
                // Assign new master user (first one found)
                for (int i = 0; i < _bodies.Length; i++)
                {
                    if (_bodies[i].IsTracked)
                    {
                        _masterTrackingId = _bodies[i].TrackingId;
                        _masterBodyIndex = i;
                        break;
                    }
                }
            }
            
            // Sync face tracking IDs
            for (int i = 0; i < _bodies.Length; i++)
            {
                if (_bodies[i].IsTracked)
                {
                    _faceSources[i].TrackingId = _bodies[i].TrackingId;
                }
                else
                {
                    _faceSources[i].TrackingId = 0;
                    _faceTracked[i] = false;
                }
            }
        }
        
        private bool _gestureWasActive = false;
        private DateTime _lastGestureToggleTime = DateTime.MinValue;

        private void CheckWakeSleepGesture(Body body)
        {
            Joint head = body.Joints[JointType.Head];
            Joint handLeft = body.Joints[JointType.HandLeft];
            Joint handRight = body.Joints[JointType.HandRight];

            if (head.TrackingState == TrackingState.NotTracked || 
                handLeft.TrackingState == TrackingState.NotTracked || 
                handRight.TrackingState == TrackingState.NotTracked)
            {
                _gestureWasActive = false;
                return;
            }

            bool isGestureActive = (body.HandLeftState == HandState.Lasso && body.HandRightState == HandState.Lasso) && 
                                   (handLeft.Position.Y > head.Position.Y && handRight.Position.Y > head.Position.Y);

            if (isGestureActive && !_gestureWasActive && (DateTime.Now - _lastGestureToggleTime).TotalSeconds > 2.0)
            {
                _isTrackingActive = !_isTrackingActive;
                _lastInteractionTime = DateTime.Now; // Update time to allow fading the suspended message
                _lastGestureToggleTime = DateTime.Now;
            }
            
            _gestureWasActive = isGestureActive;
        }

        private void ProcessMasterUserInput(Body body)
        {
            Joint hand = body.Joints[JointType.HandRight];
            Joint shoulder = body.Joints[JointType.ShoulderRight];

            if (hand.TrackingState == TrackingState.NotTracked || shoulder.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            Joint head = body.Joints[JointType.Head];
            Joint chest = body.Joints[JointType.SpineMid];

            if (head.TrackingState == TrackingState.NotTracked || chest.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // Timeout Logic: Keep active if hand moves more than 1cm
            float dx = hand.Position.X - _lastHandPosition.X;
            float dy = hand.Position.Y - _lastHandPosition.Y;
            float dz = hand.Position.Z - _lastHandPosition.Z;
            float distSq = dx * dx + dy * dy + dz * dz;

            if (distSq > 0.0001f) // roughly 1cm movement
            {
                _lastInteractionTime = DateTime.Now;
                _lastHandPosition = hand.Position;
            }

            if (_isTrackingActive && (DateTime.Now - _lastInteractionTime).TotalSeconds > 30)
            {
                _isTrackingActive = false;
                _lastInteractionTime = DateTime.Now; // Reset so the inactivity message shows for 3 seconds
            }

            if (!_isTrackingActive) return;

            // Dynamic interaction box perfectly scaled to the user's body (reduced by 20% per user request)
            double boxHeight = Math.Max(0.3, head.Position.Y - chest.Position.Y) * 0.8;
            
            // Map width proportionally to standard 16:9 screen aspect ratio so movement isn't stretched
            double screenRatio = SystemParameters.PrimaryScreenWidth / SystemParameters.PrimaryScreenHeight;
            double boxWidth = boxHeight * screenRatio * 0.45; // Scaled down further to require less horizontal reach
            
            // Shift Y up so the user's chest level is the absolute bottom of the screen
            double boxCenterY = chest.Position.Y + (boxHeight / 2);
            // Shift X to the right so the user's right hand doesn't cross their torso, but keep it narrow
            double boxCenterX = shoulder.Position.X + (boxWidth * 0.25);
            
            double relativeX = hand.Position.X - boxCenterX; 
            double relativeY = hand.Position.Y - boxCenterY;
            
            // Normalize to 0-1 range
            double normalizedX = (relativeX + (boxWidth / 2)) / boxWidth;
            double normalizedY = 1.0 - ((relativeY + (boxHeight / 2)) / boxHeight); // Invert Y for screen coords

            normalizedX = Math.Max(0, Math.Min(1, normalizedX));
            normalizedY = Math.Max(0, Math.Min(1, normalizedY));

            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            double rawScreenX = normalizedX * screenWidth;
            double rawScreenY = normalizedY * screenHeight;

            var (smoothX, smoothY) = _filter.FilterXY(rawScreenX, rawScreenY);

            _window.Dispatcher.Invoke(() => 
            {
                _window.UpdateCursor(smoothX, smoothY, body.HandRightState == HandState.Closed);
            });

            // Apply DPI scaling for hardware physical mouse!
            int physicalX = (int)(smoothX * _dpiX);
            int physicalY = (int)(smoothY * _dpiY);

            ManageTouchState(physicalX, physicalY, body.HandRightState);
        }

        private bool _wasDown = false;
        private int _lockedX = -1;
        private int _lockedY = -1;
        private const int ClickJitterRadius = 15;

        private void ManageTouchState(int x, int y, HandState handState)
        {
            if (handState == HandState.Lasso)
            {
                // Hover (Move)
                if (_wasDown)
                {
                    PenInjector.InjectPen(_lockedX, _lockedY, PenInjector.POINTER_FLAGS.UP | PenInjector.POINTER_FLAGS.INRANGE);
                    _wasDown = false;
                }
                else
                {
                    PenInjector.InjectPen(x, y, PenInjector.POINTER_FLAGS.INRANGE | PenInjector.POINTER_FLAGS.UPDATE);
                }
            }
            else if (handState == HandState.Closed)
            {
                // Click & Drag (Fist)
                if (!_wasDown)
                {
                    _lockedX = x;
                    _lockedY = y;
                    PenInjector.InjectPen(_lockedX, _lockedY, PenInjector.POINTER_FLAGS.DOWN | PenInjector.POINTER_FLAGS.INRANGE | PenInjector.POINTER_FLAGS.INCONTACT);
                    _wasDown = true;
                    try { _clickSound?.Play(); } catch { }
                }
                else
                {
                    int dx = x - _lockedX;
                    int dy = y - _lockedY;
                    if (Math.Sqrt(dx * dx + dy * dy) > ClickJitterRadius)
                    {
                        // Broke lock radius, allow dragging
                        PenInjector.InjectPen(x, y, PenInjector.POINTER_FLAGS.UPDATE | PenInjector.POINTER_FLAGS.INRANGE | PenInjector.POINTER_FLAGS.INCONTACT);
                        _lockedX = x;
                        _lockedY = y;
                    }
                }
            }
            else
            {
                // Open Hand / Unknown -> Lift and hover
                if (_wasDown)
                {
                    PenInjector.InjectPen(_lockedX, _lockedY, PenInjector.POINTER_FLAGS.UP | PenInjector.POINTER_FLAGS.INRANGE);
                    _wasDown = false;
                }
                PenInjector.InjectPen(x, y, PenInjector.POINTER_FLAGS.INRANGE | PenInjector.POINTER_FLAGS.UPDATE);
            }
        }

        private void SpeechRecognized(object sender, System.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence >= 0.4f)
            {
                string command = e.Result.Text;
                if (command.Contains("play") || command.Contains("pause"))
                {
                    byte scanCode = (byte)MapVirtualKey(VK_MEDIA_PLAY_PAUSE, 0);
                    keybd_event(VK_MEDIA_PLAY_PAUSE, scanCode, KEYEVENTF_EXTENDEDKEY, 0);
                    keybd_event(VK_MEDIA_PLAY_PAUSE, scanCode, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
                }
            }
        }

        public void Stop()
        {
            if (_multiReader != null)
            {
                _multiReader.Dispose();
                _multiReader = null;
            }

            for(int i=0; i<6; i++)
            {
                if (_faceReaders[i] != null) _faceReaders[i].Dispose();
                if (_faceSources[i] != null) _faceSources[i].Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
                _sensor = null;
            }
        }
    }
}
