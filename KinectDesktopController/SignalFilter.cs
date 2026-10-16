using System;

namespace KinectDesktopController
{
    public class SignalFilter
    {
        private readonly double _alpha;
        private double? _smoothedX;
        private double? _smoothedY;

        public SignalFilter(double alpha = 0.5)
        {
            _alpha = alpha;
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

        public void Reset()
        {
            _smoothedX = null;
            _smoothedY = null;
        }
        
        public double GetDistance(double shoulderZ, double handZ)
        {
            return shoulderZ - handZ;
        }
    }
}
