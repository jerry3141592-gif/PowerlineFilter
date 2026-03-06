using System;
using System.IO;
using System.Text;

namespace PowerlineFilter;

/// <summary>
/// Adaptive LMS filter for powerline interference cancellation.
/// Uses adaptive notch filter to estimate and cancel 50Hz interference.
/// </summary>
public class AdaptiveLmsFilter
{
    private readonly double _sampleRate;
    private readonly double _centerFrequency;
    private readonly int _numTaps;
    private readonly double _mu; // Step size
    
    // LMS state
    private readonly double[] _weights;
    private readonly double[] _buffer;
    private int _bufferIndex;
    
    // Reference oscillators for frequency estimation
    private double _phase;
    private readonly double _phaseIncrement;
    
    /// <summary>
    /// Filter delay in samples (internal buffer + processing)
    /// </summary>
    public int Delay => _numTaps / 2;
    
    public AdaptiveLmsFilter(
        double sampleRate,
        double centerFrequency = 50.0,
        int numTaps = 64,
        double mu = 0.1)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (centerFrequency <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerFrequency));
            
        _sampleRate = sampleRate;
        _centerFrequency = centerFrequency;
        _numTaps = numTaps;
        _mu = mu;
        
        _weights = new double[numTaps];
        _buffer = new double[numTaps];
        _phase = 0;
        _phaseIncrement = 2 * Math.PI * centerFrequency / sampleRate;
    }
    
    /// <summary>
    /// Process a single sample through the adaptive filter.
    /// Uses LMS algorithm to estimate and cancel sinusoidal interference.
    /// </summary>
    public double ProcessSample(double input)
    {
        // Generate reference signal (sin and cos)
        double refSin = Math.Sin(_phase);
        double refCos = Math.Cos(_phase);
        
        // Update phase
        _phase += _phaseIncrement;
        if (_phase > 2 * Math.PI)
            _phase -= 2 * Math.PI;
        
        // Update buffer with reference signals
        _buffer[_bufferIndex] = refSin;
        
        // Compute filter output (estimated interference)
        double interference = 0;
        for (int i = 0; i < _numTaps; i++)
        {
            int idx = (_bufferIndex - i + _numTaps) % _numTaps;
            interference += _weights[i] * _buffer[idx];
        }
        
        // Compute error (this is the cleaned output)
        double error = input - interference;
        
        // Update weights using LMS
        for (int i = 0; i < _numTaps; i++)
        {
            int idx = (_bufferIndex - i + _numTaps) % _numTaps;
            _weights[i] += _mu * error * _buffer[idx];
        }
        
        // Advance buffer index
        _bufferIndex++;
        if (_bufferIndex >= _numTaps)
            _bufferIndex = 0;
        
        return error;
    }
    
    /// <summary>
    /// Process a block of samples.
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
        Array.Clear(_weights, 0, _weights.Length);
        Array.Clear(_buffer, 0, _buffer.Length);
        _phase = 0;
        _bufferIndex = 0;
    }
}
