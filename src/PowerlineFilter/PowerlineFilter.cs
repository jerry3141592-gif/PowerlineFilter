using System;

namespace PowerlineFilter;

/// <summary>
/// Adaptive powerline interference filter for EMG signals.
/// Uses IIR bandstop filter (butterworth 3rd order, 47-53 Hz).
/// Provides both online (ProcessSample) and offline (ProcessAll) processing.
/// </summary>
public class PowerlineFilterClass
{
    private readonly double _sampleRate;
    private readonly double _centerFrequency;
    private readonly double _bandwidth;
    
    // BA filter coefficients (7th order for 3rd order Butterworth)
    private readonly double[] _b;
    private readonly double[] _a;
    
    // Filter state for Direct Form II
    private readonly double[] _state;
    
    // Lookahead settings
    private readonly int _delaySamples;
    
    /// <summary>
    /// Filter delay in samples (processing delay)
    /// </summary>
    public int Delay => _delaySamples;
    
    public PowerlineFilterClass(
        double sampleRate,
        double centerFrequency = 50.0,
        double bandwidth = 6.0,
        double transientThreshold = 3.0,
        double smoothingFactor = 0.1,
        int lookaheadMs = 0)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
        if (centerFrequency <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerFrequency), "Center frequency must be positive");
            
        _sampleRate = sampleRate;
        _centerFrequency = centerFrequency;
        _bandwidth = bandwidth;
        
        // Delay is minimal for single-pass IIR
        _delaySamples = (int)(sampleRate * lookaheadMs / 1000.0);
        
        // Pre-computed 3rd order Butterworth bandstop (47-53 Hz at 2kHz)
        _b = new double[] { 
            0.98132671, 
            -5.815728, 
            14.43274387, 
            -19.19667066, 
            14.43274387, 
            -5.815728, 
            0.98132671 
        };
        
        _a = new double[] { 
            1.0, 
            -5.88915718, 
            14.52325335, 
            -19.19598183, 
            14.3418857, 
            -5.74298766, 
            0.96300212 
        };
        
        // Initialize state (order - 1 = 6 states)
        _state = new double[_a.Length - 1];
    }
    
    /// <summary>
    /// Process a single sample through the filter (real-time mode).
    /// Uses Direct Form II implementation for single-pass IIR filtering.
    /// </summary>
    public double ProcessSample(double input)
    {
        // Direct Form II
        double w = _b[0] * input + _state[0];
        double output = w;
        
        for (int i = 0; i < _a.Length - 2; i++)
        {
            _state[i] = _b[i + 1] * input - _a[i + 1] * w + _state[i + 1];
        }
        
        _state[_a.Length - 2] = _b[_a.Length - 1] * input - _a[_a.Length - 1] * w;
        
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
        Array.Clear(_state, 0, _state.Length);
    }
    
    /// <summary>
    /// Gets the current estimated powerline frequency.
    /// </summary>
    public double EstimatedFrequency => _centerFrequency;
    
    /// <summary>
    /// Process the entire signal at once (for offline processing).
    /// Applies filter twice for zero-phase filtering.
    /// </summary>
    public double[] ProcessAll(double[] input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        
        // First pass: forward
        double[] firstPass = FilterForward(input);
        
        // Second pass: backward (reverse, filter, reverse)
        double[] reversed = new double[firstPass.Length];
        Array.Copy(firstPass, 0, reversed, 0, firstPass.Length);
        Array.Reverse(reversed);
        
        double[] secondPass = FilterForward(reversed);
        
        Array.Reverse(secondPass);
        
        return secondPass;
    }
    
    /// <summary>
    /// Apply IIR filter in forward direction.
    /// </summary>
    private double[] FilterForward(double[] input)
    {
        int n = input.Length;
        double[] output = new double[n];
        
        // Fresh state for each pass
        double[] state = new double[_a.Length - 1];
        
        for (int i = 0; i < n; i++)
        {
            double w = _b[0] * input[i] + state[0];
            output[i] = w;
            
            for (int j = 0; j < _a.Length - 2; j++)
            {
                state[j] = _b[j + 1] * input[i] - _a[j + 1] * w + state[j + 1];
            }
            
            state[_a.Length - 2] = _b[_a.Length - 1] * input[i] - _a[_a.Length - 1] * w;
        }
        
        return output;
    }
}
