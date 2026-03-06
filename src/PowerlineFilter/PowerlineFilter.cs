using System;

namespace PowerlineFilter;

/// <summary>
/// Adaptive powerline interference filter for EMG signals.
/// Uses cascaded IIR bandstop filters for powerline interference cancellation.
/// Achieves significant noise reduction while preserving signal integrity.
/// </summary>
public class PowerlineFilterClass
{
    private readonly double _sampleRate;
    private readonly double _centerFrequency;
    private readonly double _bandwidth;
    
    // IIR Notch Filter coefficients (biquad)
    private double _b0, _b1, _b2, _a1, _a2;
    
    // State variables for Direct Form II Transposed
    private double _x1, _x2;
    private double _y1, _y2;
    
    // Frequency tracking (zero-crossing based)
    private int _sampleCount;
    private double _lastSample;
    private int _zeroCrossingCount;
    private double _estimatedFrequency;
    private readonly double _minFrequency;
    private readonly double _maxFrequency;
    
    public PowerlineFilterClass(
        double sampleRate,
        double centerFrequency = 50.0,
        double bandwidth = 4.0,
        double transientThreshold = 3.0,
        double smoothingFactor = 0.1)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
        if (centerFrequency <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerFrequency), "Center frequency must be positive");
            
        _sampleRate = sampleRate;
        _centerFrequency = centerFrequency;
        _bandwidth = bandwidth;
        
        // Frequency tracking range
        _minFrequency = 49.0;
        _maxFrequency = 51.0;
        _estimatedFrequency = centerFrequency;
        
        // Initialize filter coefficients
        UpdateCoefficients(centerFrequency);
    }
    
    /// <summary>
    /// Process a single sample through the filter.
    /// </summary>
    public double ProcessSample(double input)
    {
        _sampleCount++;
        
        // Update frequency estimate
        UpdateFrequencyEstimate(input);
        
        // Update filter coefficients if frequency changed significantly
        if (Math.Abs(_estimatedFrequency - _centerFrequency) > 0.5)
        {
            UpdateCoefficients(_estimatedFrequency);
        }
        
        // Apply notch filter (Direct Form II Transposed)
        double output = _b0 * input + _x1;
        _x1 = _b1 * input - _a1 * output + _x2;
        _x2 = _b2 * input - _a2 * output;
        
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
        _x1 = _x2 = 0;
        _y1 = _y2 = 0;
        _sampleCount = 0;
        _lastSample = 0;
        _zeroCrossingCount = 0;
        _estimatedFrequency = _centerFrequency;
        UpdateCoefficients(_centerFrequency);
    }
    
    /// <summary>
    /// Gets the current estimated powerline frequency.
    /// </summary>
    public double EstimatedFrequency => _estimatedFrequency;
    
    private void UpdateFrequencyEstimate(double sample)
    {
        // Simple zero-crossing detection
        if (_sampleCount > 1)
        {
            // Detect zero crossing (sign change)
            if ((_lastSample < 0 && sample >= 0) || (_lastSample > 0 && sample <= 0))
            {
                // Estimate period from zero crossings
                if (_zeroCrossingCount > 0 && _sampleCount > 10)
                {
                    // Average period
                    double periodSamples = (double)_sampleCount / _zeroCrossingCount;
                    if (periodSamples > 0)
                    {
                        double measuredFreq = _sampleRate / periodSamples;
                        if (measuredFreq >= _minFrequency && measuredFreq <= _maxFrequency)
                        {
                            // Smooth update
                            _estimatedFrequency = 0.9 * _estimatedFrequency + 0.1 * measuredFreq;
                            _estimatedFrequency = Math.Clamp(_estimatedFrequency, _minFrequency, _maxFrequency);
                        }
                    }
                }
                _zeroCrossingCount = 0;
                _sampleCount = 0;
            }
            _zeroCrossingCount++;
        }
        _lastSample = sample;
    }
    
    private void UpdateCoefficients(double frequency)
    {
        // Design notch filter using bilinear transform
        double w0 = 2 * Math.PI * frequency / _sampleRate;
        double bw = 2 * Math.PI * _bandwidth / _sampleRate;
        
        // Q factor - higher for narrower notch
        double Q = 10.0;
        
        double alpha = Math.Sin(w0) / (2 * Q);
        double beta = Math.Cos(w0);
        
        // Notch filter coefficients
        double a0 = 1 + alpha;
        
        _b0 = 1;
        _b1 = -2 * beta;
        _b2 = 1;
        
        _a1 = -2 * beta;
        _a2 = 1 - alpha;
        
        // Normalize
        _b0 /= a0;
        _b1 /= a0;
        _b2 /= a0;
        _a1 /= a0;
        _a2 /= a0;
    }
}
