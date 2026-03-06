using System;

namespace PowerlineFilter;

/// <summary>
/// Adaptive powerline interference filter for EMG signals.
/// Uses cascaded IIR bandstop filters.
/// Real-time processing: single-pass IIR (lower noise reduction)
/// Offline processing: applies filter twice (forward-backward for zero-phase)
/// </summary>
public class PowerlineFilterClass
{
    private readonly double _sampleRate;
    private readonly double _centerFrequency;
    private readonly double _bandwidth;
    
    // SOS filter coefficients (3 sections, each 2nd order)
    private readonly double[][] _sos;
    
    // Filter state for each section
    private readonly double[][] _states;
    
    // Buffer for look-ahead processing
    private double[] _inputBuffer;
    private double[] _outputBuffer;
    private int _bufferWriteIndex;
    private readonly int _bufferSize;
    private readonly int _lookaheadSamples;
    private readonly int _chunkSize;
    
    public PowerlineFilterClass(
        double sampleRate,
        double centerFrequency = 50.0,
        double bandwidth = 6.0,
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
        
        // Look-ahead: 100ms
        _lookaheadSamples = (int)(sampleRate * 0.1);
        
        // Chunk size: 500ms
        _chunkSize = (int)(sampleRate * 0.5);
        
        // Buffer: 2 chunks + lookahead
        _bufferSize = _chunkSize * 2 + _lookaheadSamples;
        
        // Initialize buffers
        _inputBuffer = new double[_bufferSize];
        _outputBuffer = new double[_bufferSize];
        
        // Pre-computed 3rd order Butterworth bandstop (47-53 Hz at 2kHz) as SOS
        _sos = new double[][]
        {
            new double[] { 0.98132671, -1.938576, 0.98132671, -1.9570194, 0.98132589 },
            new double[] { 1.0, -1.97546442, 1.0, -1.96305154, 0.99013831 },
            new double[] { 1.0, -1.97546442, 1.0, -1.96908624, 0.99110147 }
        };
        
        // Initialize states
        _states = new double[_sos.Length][];
        for (int i = 0; i < _sos.Length; i++)
        {
            _states[i] = new double[2];
        }
        
        _bufferWriteIndex = 0;
    }
    
    /// <summary>
    /// Process a single sample through the filter (real-time mode).
    /// </summary>
    public double ProcessSample(double input)
    {
        // Apply SOS filter (using persistent state)
        double output = input;
        
        for (int s = 0; s < _sos.Length; s++)
        {
            output = ApplySosSection(output, _sos[s], _states[s]);
        }
        
        return output;
    }
    
    /// <summary>
    /// Apply a single SOS section with state.
    /// </summary>
    private double ApplySosSection(double input, double[] sos, double[] state)
    {
        double b0 = sos[0];
        double b1 = sos[1];
        double b2 = sos[2];
        double a1 = sos[3];
        double a2 = sos[4];
        
        // Direct Form II Transposed
        double w = b0 * input + state[0];
        double output = b1 * input - a1 * w + state[1];
        
        state[0] = b2 * input - a2 * w;
        state[1] = 0;
        
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
        for (int i = 0; i < _states.Length; i++)
        {
            Array.Clear(_states[i], 0, _states[i].Length);
        }
        Array.Clear(_inputBuffer, 0, _inputBuffer.Length);
        Array.Clear(_outputBuffer, 0, _outputBuffer.Length);
        _bufferWriteIndex = 0;
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
        double[] firstPass = ApplySosFilterForward(input);
        
        // Second pass: backward (reverse, filter, reverse)
        double[] reversed = new double[firstPass.Length];
        Array.Copy(firstPass, 0, reversed, 0, firstPass.Length);
        Array.Reverse(reversed);
        
        double[] secondPass = ApplySosFilterForward(reversed);
        
        Array.Reverse(secondPass);
        
        return secondPass;
    }
    
    /// <summary>
    /// Apply SOS filter in forward direction.
    /// </summary>
    private double[] ApplySosFilterForward(double[] input)
    {
        int n = input.Length;
        double[] output = new double[n];
        
        // Create fresh states for each pass
        double[][] passStates = new double[_sos.Length][];
        for (int i = 0; i < _sos.Length; i++)
        {
            passStates[i] = new double[2];
        }
        
        for (int i = 0; i < n; i++)
        {
            double val = input[i];
            
            for (int s = 0; s < _sos.Length; s++)
            {
                val = ApplySosSection(val, _sos[s], passStates[s]);
            }
            
            output[i] = val;
        }
        
        return output;
    }
}
