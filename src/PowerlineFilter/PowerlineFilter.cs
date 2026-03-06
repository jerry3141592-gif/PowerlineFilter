using System;

namespace PowerlineFilter;

/// <summary>
/// Adaptive powerline interference filter for EMG signals.
/// Uses IIR bandstop filter with block-based processing and lookahead.
/// Provides online filtering with minimal delay.
/// </summary>
public class PowerlineFilterClass
{
    private readonly double _sampleRate;
    private readonly double _centerFrequency;
    private readonly double _bandwidth;
    
    // BA filter coefficients (7th order for 3rd order Butterworth)
    private readonly double[] _b;
    private readonly double[] _a;
    
    // Lookahead settings
    private readonly int _lookaheadSamples;
    private readonly int _chunkSize;
    private readonly int _delaySamples;
    
    // Buffers for block processing
    private double[] _inputBuffer;
    private double[] _outputBuffer;
    private int _bufferWritePos;
    private int _bufferReadPos;
    private int _samplesInBuffer;
    
    /// <summary>
    /// Filter delay in samples (lookahead + processing delay)
    /// </summary>
    public int Delay => _delaySamples;
    
    public PowerlineFilterClass(
        double sampleRate,
        double centerFrequency = 50.0,
        double bandwidth = 6.0,
        double transientThreshold = 3.0,
        double smoothingFactor = 0.1,
        int lookaheadMs = 20)  // 20ms lookahead by default
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
        if (centerFrequency <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerFrequency), "Center frequency must be positive");
            
        _sampleRate = sampleRate;
        _centerFrequency = centerFrequency;
        _bandwidth = bandwidth;
        
        // Lookahead: default 20ms (40 samples at 2kHz)
        _lookaheadSamples = (int)(sampleRate * lookaheadMs / 1000.0);
        
        // Chunk size: enough for stable filter response
        _chunkSize = _lookaheadSamples + 100;  // lookahead + some extra
        
        // Delay = lookahead (we look ahead this many samples)
        _delaySamples = _lookaheadSamples;
        
        // Initialize buffers
        _inputBuffer = new double[_chunkSize];
        _outputBuffer = new double[_chunkSize];
        
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
        
        _bufferWritePos = 0;
        _bufferReadPos = 0;
        _samplesInBuffer = 0;
    }
    
    /// <summary>
    /// Process a single sample through the filter (online mode).
    /// Uses internal buffering with lookahead for zero-phase filtering.
    /// Returns filtered output with delay.
    /// </summary>
    public double ProcessSample(double input)
    {
        // Write input sample
        _inputBuffer[_bufferWritePos] = input;
        
        // Advance write position
        _bufferWritePos++;
        
        if (_bufferWritePos >= _chunkSize)
        {
            // Process chunk when buffer is full
            ProcessChunk();
            
            // Reset write position (keep last lookahead samples)
            _bufferWritePos = _lookaheadSamples;
        }
        
        // Read output (delayed by lookahead)
        int outputPos = _bufferWritePos - _delaySamples;
        
        if (outputPos >= 0 && outputPos < _chunkSize)
        {
            double output = _outputBuffer[outputPos];
            return output;
        }
        
        // Not enough data yet - return input
        return input;
    }
    
    /// <summary>
    /// Process a block of samples through the filter (online mode).
    /// Uses lookahead for better filtering performance.
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
    /// Process chunk with forward-backward filtering (zero-phase).
    /// </summary>
    private void ProcessChunk()
    {
        // Apply forward-backward filtering to the chunk
        double[] filtered = ApplyFiltFilt(_inputBuffer);
        
        // Copy output (skip first lookahead samples for delay)
        Array.Copy(filtered, _outputBuffer, _chunkSize - _lookaheadSamples);
        
        // Keep last lookahead samples for next chunk
        for (int i = 0; i < _lookaheadSamples; i++)
        {
            _inputBuffer[i] = _inputBuffer[_chunkSize - _lookaheadSamples + i];
        }
    }
    
    /// <summary>
    /// Apply zero-phase filtering (forward-backward).
    /// </summary>
    private double[] ApplyFiltFilt(double[] input)
    {
        // Forward filter
        double[] forward = FilterForward(input);
        
        // Backward filter
        double[] reversed = new double[forward.Length];
        Array.Copy(forward, 0, reversed, 0, forward.Length);
        Array.Reverse(reversed);
        
        double[] backward = FilterForward(reversed);
        Array.Reverse(backward);
        
        return backward;
    }
    
    /// <summary>
    /// Apply IIR filter in forward direction.
    /// </summary>
    private double[] FilterForward(double[] input)
    {
        int n = input.Length;
        double[] output = new double[n];
        
        // Fresh state for clean filtering
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
    
    /// <summary>
    /// Reset the filter state.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_inputBuffer, 0, _inputBuffer.Length);
        Array.Clear(_outputBuffer, 0, _outputBuffer.Length);
        _bufferWritePos = 0;
        _bufferReadPos = 0;
        _samplesInBuffer = 0;
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
        
        return ApplyFiltFilt(input);
    }
}
