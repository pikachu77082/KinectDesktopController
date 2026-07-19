using System;

namespace KinectDesktopController
{
    public class SignalFilter
    {
        private readonly double _alpha;
        private double? _smoothedX;
        private double? _smoothedY;

        // Hysteresis configuration
        private readonly double _thresholdDown;
        private readonly double _thresholdUp;
        private bool _isCurrentlyDown;

        public SignalFilter(double alpha = 0.5, double thresholdDown = 0.55, double thresholdUp = 0.50)
        {
            _alpha = alpha;
            _thresholdDown = thresholdDown;
            _thresholdUp = thresholdUp;
        }

        public (double x, double y) FilterXY(double x, double y)
        {
            if (!_smoothedX.HasValue || !_smoothedY.HasValue)
            {
                _smoothedX = x;
                _smoothedY = y;
            }
            else
            {
                _smoothedX = _alpha * x + (1 - _alpha) * _smoothedX.Value;
                _smoothedY = _alpha * y + (1 - _alpha) * _smoothedY.Value;
            }

            return (_smoothedX.Value, _smoothedY.Value);
        }

        public bool CalculateZClickState(double shoulderZ, double handZ)
        {
            // Calculate distance: how far in front of the shoulder is the hand?
            // Kinect Z coordinates increase as they go away from the camera.
            // So if hand is closer to camera, hand.Z < shoulder.Z.
            // distance = shoulder.Z - hand.Z
            double distance = shoulderZ - handZ;

            if (!_isCurrentlyDown)
            {
                if (distance > _thresholdDown)
                {
                    _isCurrentlyDown = true;
                }
            }
            else
            {
                if (distance < _thresholdUp)
                {
                    _isCurrentlyDown = false;
                }
            }

            return _isCurrentlyDown;
        }

        public void Reset()
        {
            _smoothedX = null;
            _smoothedY = null;
            _isCurrentlyDown = false;
        }
        
        public double GetDistance(double shoulderZ, double handZ)
        {
            return shoulderZ - handZ;
        }
    }
}
