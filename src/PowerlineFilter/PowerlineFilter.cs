namespace PowerlineFilter;

/// <summary>
/// Adaptive powerline interference filter for EMG signals.
/// Uses adaptive frequency tracking and transient detection to avoid IIR filter ringing.
/// </summary>
public class PowerlineFilterClass
{
    private readonly double _sampleRate;
    private readonly double _minFrequency;
    private readonly double _maxFrequency;
    private readonly double _bandwidth;
    private readonly double _transientThreshold;
    private readonly double _smoothingFactor;
    
    // State variables
    private double _estimatedFrequency;
    private readonly double[] _iirState;
    private double _previousOutput;
    private bool _isTransient;
    private double _energyShortTerm;
    private double _energyLongTerm;
    private readonly int _windowSizeShort;
    private readonly int _windowSizeLong;
    private readonly Queue<double> _shortTermWindow;
    private readonly Queue<double> _longTermWindow;
    
    // Filter coefficients (notch filter)
    private double _b0, _b1, _b2, _a1, _a2;
    private double _x1, _x2; // Input delays
    private double _y1, _y2; // Output delays
    
    // Frequency estimation using zero-crossing with phase tracking
    private readonly Queue<double> _zeroCrossingTimes;
    private double _lastZeroCrossing;
    private bool _lastSign;
    private int _sampleCount;
    
    // Phase-locked loop components
    private double _phase;
    private double _phaseError;
    
    public PowerlineFilterClass(
        double sampleRate,
        double centerFrequency = 50.0,
        double bandwidth = 2.0,
        double transientThreshold = 3.0,
        double smoothingFactor = 0.1)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
        if (centerFrequency <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerFrequency), "Center frequency must be positive");
            
        _sampleRate = sampleRate;
        _minFrequency = 49.0;
        _maxFrequency = 51.0;
        _bandwidth = bandwidth;
        _transientThreshold = transientThreshold;
        _smoothingFactor = smoothingFactor;
        
        _estimatedFrequency = Math.Clamp(centerFrequency, _minFrequency, _maxFrequency);
        
        // Window sizes for transient detection (50ms and 200ms)
        _windowSizeShort = (int)(0.05 * sampleRate);
        _windowSizeLong = (int)(0.2 * sampleRate);
        
        _shortTermWindow = new Queue<double>();
        _longTermWindow = new Queue<double>();
        
        _zeroCrossingTimes = new Queue<double>();
        _isTransient = false;
        
        _iirState = new double[2];
        
        _phase = 0;
        _phaseError = 0;
        _sampleCount = 0;
        
        // Initialize filter coefficients
        UpdateFilterCoefficients();
    }
    
    /// <summary>
    /// Process a single sample through the filter.
    /// </summary>
    public double ProcessSample(double input)
    {
        _sampleCount++;
        
        // Update frequency estimate using PLL
        UpdateFrequencyEstimatePLL(input);
        
        // Detect transients
        DetectTransient(input);
        
        // Update filter coefficients based on estimated frequency
        UpdateFilterCoefficients();
        
        // Apply notch filter with adaptive Q
        double output = ApplyNotchFilter(input);
        
        // Apply minimal smoothing to avoid abrupt changes (less aggressive)
        output = ApplySmoothing(output);
        
        // Update energy windows
        UpdateEnergyWindows(Math.Abs(input));
        
        return output;
    }
    
    /// <summary>
    /// Process a block of samples through the filter.
    /// </summary>
    public double[] ProcessBlock(double[] input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
            
        double[] output = new double[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = ProcessSample(input[i]);
        }
        return output;
    }
    
    /// <summary>
    /// Reset the filter state.
    /// </summary>
    public void Reset()
    {
        _estimatedFrequency = 50.0;
        _x1 = _x2 = 0;
        _y1 = _y2 = 0;
        _previousOutput = 0;
        _isTransient = false;
        _energyShortTerm = 0;
        _energyLongTerm = 0;
        _shortTermWindow.Clear();
        _longTermWindow.Clear();
        _zeroCrossingTimes.Clear();
        _lastZeroCrossing = 0;
        _sampleCount = 0;
        _phase = 0;
        _phaseError = 0;
        UpdateFilterCoefficients();
    }
    
    /// <summary>
    /// Gets the current estimated powerline frequency.
    /// </summary>
    public double EstimatedFrequency => _estimatedFrequency;
    
    private void UpdateFrequencyEstimatePLL(double input)
    {
        // Phase-locked loop for frequency estimation
        // Generate reference sine and cosine at estimated frequency
        double omega = 2 * Math.PI * _estimatedFrequency / _sampleRate;
        double refSin = Math.Sin(_phase);
        double refCos = Math.Cos(_phase);
        
        // Mix input with reference to get phase error
        // This is a simplified PLL approach
        _phaseError = input * refSin;
        
        // Update phase
        _phase += omega + _smoothingFactor * 2 * _phaseError;
        
        // Wrap phase to [0, 2*PI)
        while (_phase >= 2 * Math.PI)
            _phase -= 2 * Math.PI;
        
        // Estimate frequency from phase difference
        if (_sampleCount > 10)
        {
            double instantaneousFreq = omega * _sampleRate / (2 * Math.PI);
            
            // Only update if frequency is in reasonable range
            if (instantaneousFreq >= 40 && instantaneousFreq <= 60)
            {
                // Smooth the frequency estimate
                _estimatedFrequency = _estimatedFrequency * 0.99 + instantaneousFreq * 0.01;
                _estimatedFrequency = Math.Clamp(_estimatedFrequency, _minFrequency, _maxFrequency);
            }
        }
    }
    
    private void UpdateFrequencyEstimate(double input)
    {
        // Zero-crossing detection for frequency estimation
        bool currentSign = input >= 0;
        
        if (currentSign != _lastSign && input != 0)
        {
            double currentTime = _zeroCrossingTimes.Count > 0 
                ? _zeroCrossingTimes.Count * (1.0 / _sampleRate)
                : 0;
                
            if (_lastZeroCrossing > 0)
            {
                double period = currentTime - _lastZeroCrossing;
                if (period > 0.001) // Avoid very small periods
                {
                    double measuredFreq = 1.0 / period;
                    
                    // Only update if frequency is in valid range
                    if (measuredFreq >= 40 && measuredFreq <= 60)
                    {
                        // Smooth the frequency estimate
                        _estimatedFrequency = _estimatedFrequency * (1 - _smoothingFactor) + 
                                             measuredFreq * _smoothingFactor;
                        _estimatedFrequency = Math.Clamp(_estimatedFrequency, _minFrequency, _maxFrequency);
                    }
                }
            }
            
            _lastZeroCrossing = currentTime;
            
            // Keep only recent zero crossings
            if (_zeroCrossingTimes.Count > 5)
                _zeroCrossingTimes.Dequeue();
            _zeroCrossingTimes.Enqueue(currentTime);
        }
        
        _lastSign = currentSign;
    }
    
    private void DetectTransient(double input)
    {
        _shortTermWindow.Enqueue(input * input);
        _longTermWindow.Enqueue(input * input);
        
        if (_shortTermWindow.Count > _windowSizeShort)
            _shortTermWindow.Dequeue();
        if (_longTermWindow.Count > _windowSizeLong)
            _longTermWindow.Dequeue();
        
        // Calculate energies
        _energyShortTerm = _shortTermWindow.Average();
        _energyLongTerm = _longTermWindow.Average();
        
        // Detect transient: sudden increase in energy
        if (_energyLongTerm > 0.0001) // Avoid division by very small numbers
        {
            double ratio = _energyShortTerm / _energyLongTerm;
            _isTransient = ratio > _transientThreshold * _transientThreshold;
        }
    }
    
    private double UpdateFilterCoefficients()
    {
        // Design notch filter using bilinear transform
        // Use lower Q for better attenuation of interference
        double w0 = 2 * Math.PI * _estimatedFrequency / _sampleRate;
        
        // Calculate Q based on whether we're in transient
        // Lower Q during transients to reduce ringing, higher otherwise
        double Q = _isTransient ? 1.0 : 8.0;
        
        double alpha = Math.Sin(w0) / (2 * Q);
        double beta = Math.Cos(w0);
        
        // Notch filter coefficients
        _b0 = 1;
        _b1 = -2 * beta;
        _b2 = 1;
        
        // Normalize by a0
        double a0 = 1 + alpha;
        _b0 /= a0;
        _b1 /= a0;
        _b2 /= a0;
        
        _a1 = -2 * beta / a0;
        _a2 = (1 - alpha) / a0;
        
        return Q;
    }
    
    private double ApplyNotchFilter(double input)
    {
        // Direct Form II transposed IIR filter
        double output = _b0 * input + _iirState[0];
        
        _iirState[0] = _b1 * input - _a1 * output + _iirState[1];
        _iirState[1] = _b2 * input - _a2 * output;
        
        return output;
    }
    
    private double ApplySmoothing(double output)
    {
        // Skip smoothing to preserve signal fidelity
        _previousOutput = output;
        return output;
    }
    
    private void UpdateEnergyWindows(double absInput)
    {
        // Already handled in DetectTransient
    }
}
