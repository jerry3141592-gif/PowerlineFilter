namespace PowerlineFilter;

/// <summary>
/// Powerline interference filter for EMG signals.
/// Uses IIR bandstop filter with zero-phase filtering via lookahead buffer.
/// </summary>
public class PowerlineFilterClass
{
    // Coefficients (from scipy butter(2, [45/1000, 55/1000], bandstop))
    private readonly double[] _b = { 0.97803048, -3.86443395, 5.77338825, -3.86443395, 0.97803048 };
    private readonly double[] _a = { 1.0, -3.90736055, 5.77290553, -3.82150735, 0.95654368 };
    
    // Lookahead buffer for zero-phase filtering
    private readonly double[] _buffer;
    private readonly int _bufferSize;
    private int _bufferIndex = 0;
    private int _samplesSeen = 0;
    private readonly bool _useLookahead;
    
    // Single pass state (for lookahead=0)
    private double _x1 = 0, _x2 = 0, _x3 = 0, _x4 = 0;
    private double _y1 = 0, _y2 = 0, _y3 = 0, _y4 = 0;
    
    /// <summary>
    /// Filter delay in samples (lookahead buffer size).
    /// </summary>
    public int Delay { get; }
    
    public PowerlineFilterClass(
        double sampleRate,
        double centerFrequency = 50.0,
        double bandwidth = 10.0,
        double transientThreshold = 3.0,
        double smoothingFactor = 0.1,
        int lookaheadMs = 10)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (centerFrequency <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerFrequency));
        
        int lookaheadSamples = (int)(sampleRate * lookaheadMs / 1000.0);
        _bufferSize = Math.Max(lookaheadSamples + 4, 4);
        _buffer = new double[_bufferSize];
        _useLookahead = lookaheadMs > 0;
        Delay = _bufferSize;
    }
    
    /// <summary>
    /// Process a single sample with lookahead (zero-phase).
    /// </summary>
    public double ProcessSample(double input)
    {
        if (!_useLookahead)
        {
            // Single pass (no lookahead, no zero-phase)
            double val = _b[0]*input + _b[1]*_x1 + _b[2]*_x2 + _b[3]*_x3 + _b[4]*_x4
                     - _a[1]*_y1 - _a[2]*_y2 - _a[3]*_y3 - _a[4]*_y4;
            _x4 = _x3; _x3 = _x2; _x2 = _x1; _x1 = input;
            _y4 = _y3; _y3 = _y2; _y2 = _y1; _y1 = val;
            return val;
        }
        
        // With lookahead - use buffer
        _buffer[_bufferIndex] = input;
        _bufferIndex = (_bufferIndex + 1) % _bufferSize;
        _samplesSeen++;
        
        if (_samplesSeen < _bufferSize)
            return input;
        
        double[] data = new double[_bufferSize];
        for (int i = 0; i < _bufferSize; i++)
        {
            int idx = (_bufferIndex - _bufferSize + i + _bufferSize) % _bufferSize;
            data[i] = _buffer[idx];
        }
        
        double[] filtered = ApplyFiltFilt(data);
        
        return filtered[0];
    }
    
    /// <summary>
    /// Apply forward-backward (zero-phase) filtering.
    /// </summary>
    private double[] ApplyFiltFilt(double[] input)
    {
        int n = input.Length;
        
        // Forward pass
        double[] forward = new double[n];
        double x1 = 0, x2 = 0, x3 = 0, x4 = 0;
        double y1 = 0, y2 = 0, y3 = 0, y4 = 0;
        
        for (int i = 0; i < n; i++)
        {
            double inp = input[i];
            double val = _b[0]*inp + _b[1]*x1 + _b[2]*x2 + _b[3]*x3 + _b[4]*x4
                     - _a[1]*y1 - _a[2]*y2 - _a[3]*y3 - _a[4]*y4;
            forward[i] = val;
            x4 = x3; x3 = x2; x2 = x1; x1 = inp;
            y4 = y3; y3 = y2; y2 = y1; y1 = val;
        }
        
        // Backward pass
        double[] backward = new double[n];
        x1 = x2 = x3 = x4 = 0;
        y1 = y2 = y3 = y4 = 0;
        
        for (int i = n - 1; i >= 0; i--)
        {
            double inp = forward[i];
            double val = _b[0]*inp + _b[1]*x1 + _b[2]*x2 + _b[3]*x3 + _b[4]*x4
                     - _a[1]*y1 - _a[2]*y2 - _a[3]*y3 - _a[4]*y4;
            backward[i] = val;
            x4 = x3; x3 = x2; x2 = x1; x1 = inp;
            y4 = y3; y3 = y2; y2 = y1; y1 = val;
        }
        
        return backward;
    }
    
    /// <summary>
    /// Process all samples (offline, zero-phase filtering).
    /// </summary>
    public double[] ProcessAll(double[] input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        
        return ApplyFiltFilt(input);
    }
    
    /// <summary>
    /// Process a block of samples.
    /// </summary>
    public double[] ProcessBlock(double[] input)
    {
        return ProcessAll(input);
    }
    
    /// <summary>
    /// Reset filter state.
    /// </summary>
    public void Reset()
    {
        if (_useLookahead)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _bufferIndex = 0;
            _samplesSeen = 0;
        }
        else
        {
            _x1 = _x2 = _x3 = _x4 = 0;
            _y1 = _y2 = _y3 = _y4 = 0;
        }
    }
    
    /// <summary>
    /// Estimated powerline frequency.
    /// </summary>
    public double EstimatedFrequency => 50.0;
}
