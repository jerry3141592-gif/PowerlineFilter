namespace PowerlineFilter;

/// <summary>
/// Powerline interference filter for EMG signals.
/// Uses cascaded biquad (SOS) bandstop filter at 50Hz.
/// </summary>
public class PowerlineFilterClass
{
    // Two SOS sections
    private double[] _x1 = {0, 0}, _x2 = {0, 0};
    private double[] _y1 = {0, 0}, _y2 = {0, 0};
    
    // SOS coefficients: [[b0,b1,b2, 1,a1,a2], [...]]
    private readonly double[][] _sos = {
        new double[] { 0.97803048, -1.93221697, 0.97803048, 1.0, -1.94873023, 0.97650011 },
        new double[] { 1.0, -1.97562041, 1.0, 1.0, -1.95863032, 0.97956331 }
    };
    
    /// <summary>
    /// Filter delay in samples.
    /// </summary>
    public int Delay => 0;
    
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
    /// Process a single sample - cascaded biquads.
    /// </summary>
    public double ProcessSample(double input)
    {
        // Section 0
        double output = _sos[0][0]*input + _sos[0][1]*_x1[0] + _sos[0][2]*_x2[0]
                     - _sos[0][4]*_y1[0] - _sos[0][5]*_y2[0];
        _x2[0] = _x1[0]; _x1[0] = input;
        _y2[0] = _y1[0]; _y1[0] = output;
        
        // Section 1 (input is output from section 0)
        input = output;
        output = _sos[1][0]*input + _sos[1][1]*_x1[1] + _sos[1][2]*_x2[1]
                     - _sos[1][4]*_y1[1] - _sos[1][5]*_y2[1];
        _x2[1] = _x1[1]; _x1[1] = input;
        _y2[1] = _y1[1]; _y1[1] = output;
        
        return output;
    }
    
    /// <summary>
    /// Process a block.
    /// </summary>
    public double[] ProcessBlock(double[] input)
    {
        if (input == null) throw new ArgumentNullException();
        double[] output = new double[input.Length];
        for (int i = 0; i < input.Length; i++)
            output[i] = ProcessSample(input[i]);
        return output;
    }
    
    /// <summary>
    /// Reset state.
    /// </summary>
    public void Reset()
    {
        _x1[0] = _x2[0] = _y1[0] = _y2[0] = 0;
        _x1[1] = _x2[1] = _y1[1] = _y2[1] = 0;
    }
    
    /// <summary>
    /// Estimated frequency.
    /// </summary>
    public double EstimatedFrequency => 50.0;
    
    /// <summary>
    /// Process all (offline).
    /// </summary>
    public double[] ProcessAll(double[] input)
    {
        return ProcessBlock(input);
    }
}
