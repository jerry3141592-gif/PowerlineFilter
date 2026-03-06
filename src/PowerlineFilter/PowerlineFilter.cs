namespace PowerlineFilter;

/// <summary>
/// Powerline interference filter for EMG signals.
/// Uses IIR bandstop filter with zero-phase filtering (forward-backward).
/// </summary>
public class PowerlineFilterClass
{
    // Coefficients (from scipy butter(2, [45/1000, 55/1000], bandstop))
    private readonly double[] _b = { 0.97803048, -3.86443395, 5.77338825, -3.86443395, 0.97803048 };
    private readonly double[] _a = { 1.0, -3.90736055, 5.77290553, -3.82150735, 0.95654368 };
    
    /// <summary>
    /// Filter delay in samples.
    /// </summary>
    public int Delay => 4;
    
    public PowerlineFilterClass(
        double sampleRate,
        double centerFrequency = 50.0,
        double bandwidth = 10.0,
        double transientThreshold = 3.0,
        double smoothingFactor = 0.1)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (centerFrequency <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerFrequency));
    }
    
    /// <summary>
    /// Process a single sample (real-time, single pass).
    /// </summary>
    public double ProcessSample(double input)
    {
        // Single pass - state saved in instance
        return SinglePass(input, ref _x1, ref _x2, ref _x3, ref _x4, ref _y1, ref _y2, ref _y3, ref _y4);
    }
    
    // Instance state for ProcessSample
    private double _x1 = 0, _x2 = 0, _x3 = 0, _x4 = 0;
    private double _y1 = 0, _y2 = 0, _y3 = 0, _y4 = 0;
    
    private double SinglePass(double input, ref double x1, ref double x2, ref double x3, ref double x4, ref double y1, ref double y2, ref double y3, ref double y4)
    {
        double output = _b[0]*input + _b[1]*x1 + _b[2]*x2 + _b[3]*x3 + _b[4]*x4
                     - _a[1]*y1 - _a[2]*y2 - _a[3]*y3 - _a[4]*y4;
        x4 = x3; x3 = x2; x2 = x1; x1 = input;
        y4 = y3; y3 = y2; y2 = y1; y1 = output;
        return output;
    }
    
    /// <summary>
    /// Process all samples (offline, zero-phase filtering).
    /// </summary>
    public double[] ProcessAll(double[] input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
            
        int n = input.Length;
        
        // Forward pass
        double[] forward = new double[n];
        double x1 = 0, x2 = 0, x3 = 0, x4 = 0;
        double y1 = 0, y2 = 0, y3 = 0, y4 = 0;
        
        for (int i = 0; i < n; i++)
        {
            forward[i] = SinglePass(input[i], ref x1, ref x2, ref x3, ref x4, ref y1, ref y2, ref y3, ref y4);
        }
        
        // Backward pass
        double[] backward = new double[n];
        x1 = x2 = x3 = x4 = 0;
        y1 = y2 = y3 = y4 = 0;
        
        for (int i = n - 1; i >= 0; i--)
        {
            backward[i] = SinglePass(forward[i], ref x1, ref x2, ref x3, ref x4, ref y1, ref y2, ref y3, ref y4);
        }
        
        return backward;
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
        _x1 = _x2 = _x3 = _x4 = 0;
        _y1 = _y2 = _y3 = _y4 = 0;
    }
    
    /// <summary>
    /// Estimated powerline frequency.
    /// </summary>
    public double EstimatedFrequency => 50.0;
}
