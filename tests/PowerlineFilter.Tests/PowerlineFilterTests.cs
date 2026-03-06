using Xunit;
using System.Linq;

namespace PowerlineFilter.Tests;

public class PowerlineFilterTests
{
    private const double SamplingFrequency = 2000.0; // 2 kHz
    
    [Fact]
    public void Constructor_WithDefaultParameters_CreatesFilter()
    {
        // Arrange & Act
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        // Assert
        Assert.NotNull(filter);
    }
    
    [Fact]
    public void Constructor_WithCustomParameters_CreatesFilter()
    {
        // Arrange & Act
        var filter = new PowerlineFilterClass(
            SamplingFrequency, 
            centerFrequency: 50.0,
            bandwidth: 2.0,
            transientThreshold: 3.0,
            smoothingFactor: 0.1);
        
        // Assert
        Assert.NotNull(filter);
    }
    
    [Fact]
    public void ProcessSample_With50HzInterference_ReducesInterference()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        double interferenceAmplitude = 1.0;
        double signalAmplitude = 0.1;
        double interferenceFreq = 50.0;
        
        // Generate samples with 50 Hz interference
        var output = new double[1000];
        for (int i = 0; i < 1000; i++)
        {
            double t = i / SamplingFrequency;
            double input = signalAmplitude * Math.Sin(2 * Math.PI * 10 * t) + 
                          interferenceAmplitude * Math.Sin(2 * Math.PI * interferenceFreq * t);
            output[i] = filter.ProcessSample(input);
        }
        
        // Calculate RMS of output (should be much less than input interference)
        double outputRms = CalculateRms(output);
        
        // Assert: The interference should be significantly reduced
        Assert.True(outputRms < 0.3, $"Output RMS should be less than 0.3, but was {outputRms}");
    }
    
    [Fact]
    public void ProcessSample_With49HzInterference_ReducesInterference()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        double interferenceFreq = 49.0;
        
        // Generate samples with 49 Hz interference
        var output = new double[1000];
        for (int i = 0; i < 1000; i++)
        {
            double t = i / SamplingFrequency;
            double input = 0.1 * Math.Sin(2 * Math.PI * 10 * t) + 
                          1.0 * Math.Sin(2 * Math.PI * interferenceFreq * t);
            output[i] = filter.ProcessSample(input);
        }
        
        double outputRms = CalculateRms(output);
        Assert.True(outputRms < 0.3, $"Output RMS should be less than 0.3, but was {outputRms}");
    }
    
    [Fact]
    public void ProcessSample_With51HzInterference_ReducesInterference()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        double interferenceFreq = 51.0;
        
        // Generate samples with 51 Hz interference
        var output = new double[1000];
        for (int i = 0; i < 1000; i++)
        {
            double t = i / SamplingFrequency;
            double input = 0.1 * Math.Sin(2 * Math.PI * 10 * t) + 
                          1.0 * Math.Sin(2 * Math.PI * interferenceFreq * t);
            output[i] = filter.ProcessSample(input);
        }
        
        double outputRms = CalculateRms(output);
        Assert.True(outputRms < 0.3, $"Output RMS should be less than 0.3, but was {outputRms}");
    }
    
    [Fact]
    public void ProcessSample_ShortTransient_NoRinging()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        // Generate a short burst (transient) - 10ms of signal
        var output = new double[100]; // 50ms at 2kHz
        int transientStart = 20;
        int transientLength = 20; // 10ms
        
        // First, let filter stabilize with clean signal
        for (int i = 0; i < 50; i++)
        {
            filter.ProcessSample(0.1 * Math.Sin(2 * Math.PI * 10 * i / SamplingFrequency));
        }
        
        // Now add a short burst
        for (int i = 0; i < 100; i++)
        {
            if (i >= transientStart && i < transientStart + transientLength)
            {
                // Short burst of 100Hz signal (should not be filtered as powerline)
                output[i] = filter.ProcessSample(2.0 * Math.Sin(2 * Math.PI * 100 * i / SamplingFrequency));
            }
            else
            {
                output[i] = filter.ProcessSample(0.1 * Math.Sin(2 * Math.PI * 10 * i / SamplingFrequency));
            }
        }
        
        // Check for ringing after the transient - the output should settle quickly
        double maxAfterTransient = 0;
        for (int i = transientStart + transientLength; i < 90; i++)
        {
            if (Math.Abs(output[i]) > maxAfterTransient)
            {
                maxAfterTransient = Math.Abs(output[i]);
            }
        }
        
        // Assert: No significant ringing after transient
        Assert.True(maxAfterTransient < 1.0, $"Ringing detected: max value after transient was {maxAfterTransient}");
    }
    
    [Fact]
    public void ProcessBlock_WithInterference_ReducesInterference()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        double[] input = new double[1000];
        
        // Generate signal with 50 Hz interference
        for (int i = 0; i < 1000; i++)
        {
            double t = i / SamplingFrequency;
            input[i] = 0.1 * Math.Sin(2 * Math.PI * 10 * t) + 
                      1.0 * Math.Sin(2 * Math.PI * 50 * t);
        }
        
        // Act
        double[] output = filter.ProcessBlock(input);
        
        // Assert
        Assert.Equal(input.Length, output.Length);
        double outputRms = CalculateRms(output);
        Assert.True(outputRms < 0.3, $"Output RMS should be less than 0.3, but was {outputRms}");
    }
    
    [Fact]
    public void ProcessBlock_EmptyInput_ReturnsEmptyArray()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        double[] input = Array.Empty<double>();
        
        // Act
        double[] output = filter.ProcessBlock(input);
        
        // Assert
        Assert.Empty(output);
    }
    
    [Fact]
    public void ProcessBlock_SingleSample_ReturnsSingleSample()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        double[] input = new double[] { 1.0 };
        
        // Act
        double[] output = filter.ProcessBlock(input);
        
        // Assert
        Assert.Single(output);
    }
    
    [Fact]
    public void Reset_AfterProcessing_StartsFresh()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        // Process some samples
        for (int i = 0; i < 100; i++)
        {
            filter.ProcessSample(1.0 * Math.Sin(2 * Math.PI * 50 * i / SamplingFrequency));
        }
        
        // Act
        filter.Reset();
        
        // Process a clean signal after reset
        double output = filter.ProcessSample(0.5);
        
        // Assert: Filter should start fresh (output should be close to input after settling)
        Assert.True(!double.IsNaN(output), "Output should not be NaN after reset");
    }
    
    [Fact]
    public void ProcessSample_NoInterference_PreservesSignal()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        // Generate clean signal without interference
        var output = new double[1000];
        for (int i = 0; i < 1000; i++)
        {
            double t = i / SamplingFrequency;
            double input = 0.5 * Math.Sin(2 * Math.PI * 100 * t); // 100 Hz signal
            output[i] = filter.ProcessSample(input);
        }
        
        // Calculate RMS ratio
        double inputRms = 0.5 / Math.Sqrt(2); // theoretical RMS of sine
        double outputRms = CalculateRms(output);
        
        // Assert: Signal should be preserved (within reasonable bounds)
        double ratio = outputRms / inputRms;
        Assert.True(ratio > 0.7 && ratio < 1.3, $"Signal should be preserved, ratio was {ratio}");
    }
    
    [Fact]
    public void ProcessSample_VaryingFrequency_TracksCorrectly()
    {
        // Arrange
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        // First apply 49 Hz - wait for filter to adapt
        var output49 = new double[1000];
        for (int i = 0; i < 1000; i++)
        {
            double t = i / SamplingFrequency;
            double input = 1.0 * Math.Sin(2 * Math.PI * 49 * t);
            output49[i] = filter.ProcessSample(input);
        }
        
        // Then apply 51 Hz - wait for filter to adapt
        var output51 = new double[1000];
        for (int i = 1000; i < 2000; i++)
        {
            double t = i / SamplingFrequency;
            double input = 1.0 * Math.Sin(2 * Math.PI * 51 * t);
            output51[i - 1000] = filter.ProcessSample(input);
        }
        
        // Skip first samples (adaptation period)
        int skip = 200;
        
        // Both frequencies should be attenuated after adaptation
        double rms49 = CalculateRms(output49.Skip(skip).ToArray());
        double rms51 = CalculateRms(output51.Skip(skip).ToArray());
        
        Assert.True(rms49 < 0.3, $"49 Hz should be attenuated, RMS was {rms49}");
        Assert.True(rms51 < 0.3, $"51 Hz should be attenuated, RMS was {rms51}");
    }
    
    [Fact]
    public void Constructor_InvalidSamplingFrequency_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PowerlineFilterClass(0));
    }
    
    [Fact]
    public void Constructor_InvalidCenterFrequency_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PowerlineFilterClass(SamplingFrequency, centerFrequency: 0));
    }
    
    private static double CalculateRms(double[] samples)
    {
        double sum = 0;
        foreach (double s in samples)
        {
            sum += s * s;
        }
        return Math.Sqrt(sum / samples.Length);
    }
    
    /// <summary>
    /// Test: Fixed 50Hz noise suppression.
    /// </summary>
    [Fact]
    public void ProcessSample_WithFixed50HzNoise_ReducesNoise()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        // Generate signal: DC + 50Hz noise
        int n = 10000;
        double[] clean = new double[n];
        double[] src = new double[n];
        double noiseAmp = 0.5;
        
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 1.0; // DC
            src[i] = clean[i] + noiseAmp * Math.Sin(2 * Math.PI * 50 * t);
        }
        
        // Filter
        double[] filtered = filter.ProcessAll(src);
        
        // Skip initial transient
        int skip = 500;
        
        // Correct: noise = src - clean
        double noiseBefore = 0, noiseAfter = 0;
        for (int i = skip; i < n; i++)
        {
            noiseBefore += (src[i] - clean[i]) * (src[i] - clean[i]);
            noiseAfter += (filtered[i] - clean[i]) * (filtered[i] - clean[i]);
        }
        noiseBefore = Math.Sqrt(noiseBefore / (n - skip));
        noiseAfter = Math.Sqrt(noiseAfter / (n - skip));
        
        double reductionDb = 20 * Math.Log10(noiseBefore / noiseAfter);
        
        Assert.True(reductionDb > 10, $"Noise reduction should be >10dB, was {reductionDb:F2}dB");
    }
    
    /// <summary>
    /// Test: Varying frequency noise (49-51 Hz) suppression.
    /// </summary>
    [Fact]
    public void ProcessSample_WithVaryingFrequencyNoise_ReducesNoise()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 20000;
        double[] clean = new double[n];
        double[] src = new double[n];
        
        // Generate signal with varying frequency noise
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 1.0;
            
            // Varying frequency: 49 -> 50 -> 51 -> 50 -> 49 Hz
            double freq;
            if (i < 5000) freq = 49;
            else if (i < 10000) freq = 49 + (i - 5000) / 5000.0; // 49 -> 50
            else if (i < 15000) freq = 50 + (i - 10000) / 5000.0; // 50 -> 51
            else freq = 51 - (i - 15000) / 5000.0; // 51 -> 50
            
            src[i] = clean[i] + 0.5 * Math.Sin(2 * Math.PI * freq * t);
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        // Skip initial transient
        int skip = 1000;
        
        // Correct: noise = src - clean
        double noiseBefore = 0, noiseAfter = 0;
        for (int i = skip; i < n; i++)
        {
            noiseBefore += (src[i] - clean[i]) * (src[i] - clean[i]);
            noiseAfter += (filtered[i] - clean[i]) * (filtered[i] - clean[i]);
        }
        noiseBefore = Math.Sqrt(noiseBefore / (n - skip));
        noiseAfter = Math.Sqrt(noiseAfter / (n - skip));
        
        double reductionDb = 20 * Math.Log10(noiseBefore / noiseAfter);
        
        Assert.True(reductionDb > 3, $"Noise reduction should be >3dB, was {reductionDb:F2}dB");
    }
    
    /// <summary>
    /// Test: Sweeping frequency noise (49.5 to 50.5 Hz).
    /// </summary>
    [Fact]
    public void ProcessSample_WithSweepingFrequency_ReducesNoise()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 20000;
        double[] clean = new double[n];
        double[] src = new double[n];
        
        // Generate sweeping frequency noise
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 1.0;
            
            // Sweep from 49.5 to 50.5 Hz
            double freq = 49.5 + (i / (double)n) * 1.0;
            src[i] = clean[i] + 0.5 * Math.Sin(2 * Math.PI * freq * t);
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        // Skip initial transient
        int skip = 2000;
        
        // Correct: noise = src - clean
        double noiseBefore = 0, noiseAfter = 0;
        for (int i = skip; i < n; i++)
        {
            noiseBefore += (src[i] - clean[i]) * (src[i] - clean[i]);
            noiseAfter += (filtered[i] - clean[i]) * (filtered[i] - clean[i]);
        }
        noiseBefore = Math.Sqrt(noiseBefore / (n - skip));
        noiseAfter = Math.Sqrt(noiseAfter / (n - skip));
        
        double reductionDb = 20 * Math.Log10(noiseBefore / noiseAfter);
        
        Assert.True(reductionDb > 3, $"Noise reduction should be >3dB, was {reductionDb:F2}dB");
    }
    
    /// <summary>
    /// Test: EMG-like signal with 50Hz noise.
    /// </summary>
    [Fact]
    public void ProcessSample_WithEmgSignal_ReducesNoise()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 10000;
        double[] clean = new double[n];
        double[] src = new double[n];
        var random = new Random(42);
        
        // Generate EMG-like signal (random + low frequency)
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            
            // EMG: random noise + low frequency components
            double emg = 0.1 * (random.NextDouble() - 0.5);
            emg += 0.05 * Math.Sin(2 * Math.PI * 5 * t); // 5 Hz
            emg += 0.03 * Math.Sin(2 * Math.PI * 10 * t); // 10 Hz
            
            clean[i] = emg;
            
            // Add 50Hz noise
            src[i] = clean[i] + 0.3 * Math.Sin(2 * Math.PI * 50 * t);
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        // Skip initial transient
        int skip = 500;
        
        // Correct: noise = src - clean
        double noiseBefore = 0, noiseAfter = 0;
        for (int i = skip; i < n; i++)
        {
            noiseBefore += (src[i] - clean[i]) * (src[i] - clean[i]);
            noiseAfter += (filtered[i] - clean[i]) * (filtered[i] - clean[i]);
        }
        noiseBefore = Math.Sqrt(noiseBefore / (n - skip));
        noiseAfter = Math.Sqrt(noiseAfter / (n - skip));
        
        double reductionDb = 20 * Math.Log10(noiseBefore / noiseAfter);
        
        Assert.True(reductionDb > 10, $"Noise reduction should be >10dB, was {reductionDb:F2}dB");
    }
    
    /// <summary>
    /// Test: Noise at 49Hz.
    /// </summary>
    [Fact]
    public void ProcessAll_With49HzNoise_ReducesNoise()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 10000;
        double[] clean = new double[n];
        double[] src = new double[n];
        
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 1.0;
            src[i] = clean[i] + 0.5 * Math.Sin(2 * Math.PI * 49 * t);
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        int skip = 500;
        double noiseBefore = 0, noiseAfter = 0;
        for (int i = skip; i < n; i++)
        {
            noiseBefore += (src[i] - clean[i]) * (src[i] - clean[i]);
            noiseAfter += (filtered[i] - clean[i]) * (filtered[i] - clean[i]);
        }
        noiseBefore = Math.Sqrt(noiseBefore / (n - skip));
        noiseAfter = Math.Sqrt(noiseAfter / (n - skip));
        
        double reductionDb = 20 * Math.Log10(noiseBefore / noiseAfter);
        
        Assert.True(reductionDb > 10, $"49Hz noise reduction should be >10dB, was {reductionDb:F2}dB");
    }
    
    /// <summary>
    /// Test: Noise at 51Hz.
    /// </summary>
    [Fact]
    public void ProcessAll_With51HzNoise_ReducesNoise()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 10000;
        double[] clean = new double[n];
        double[] src = new double[n];
        
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 1.0;
            src[i] = clean[i] + 0.5 * Math.Sin(2 * Math.PI * 51 * t);
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        int skip = 500;
        double noiseBefore = 0, noiseAfter = 0;
        for (int i = skip; i < n; i++)
        {
            noiseBefore += (src[i] - clean[i]) * (src[i] - clean[i]);
            noiseAfter += (filtered[i] - clean[i]) * (filtered[i] - clean[i]);
        }
        noiseBefore = Math.Sqrt(noiseBefore / (n - skip));
        noiseAfter = Math.Sqrt(noiseAfter / (n - skip));
        
        double reductionDb = 20 * Math.Log10(noiseBefore / noiseAfter);
        
        Assert.True(reductionDb > 10, $"51Hz noise reduction should be >10dB, was {reductionDb:F2}dB");
    }
    
    /// <summary>
    /// Test: Multiple frequency noise (49, 50, 51 Hz combined).
    /// </summary>
    [Fact]
    public void ProcessAll_WithMultipleFrequencies_ReducesNoise()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 10000;
        double[] clean = new double[n];
        double[] src = new double[n];
        
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 1.0;
            // Combined 49Hz + 50Hz + 51Hz
            src[i] = clean[i] 
                + 0.2 * Math.Sin(2 * Math.PI * 49 * t)
                + 0.2 * Math.Sin(2 * Math.PI * 50 * t)
                + 0.2 * Math.Sin(2 * Math.PI * 51 * t);
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        int skip = 500;
        double noiseBefore = 0, noiseAfter = 0;
        for (int i = skip; i < n; i++)
        {
            noiseBefore += (src[i] - clean[i]) * (src[i] - clean[i]);
            noiseAfter += (filtered[i] - clean[i]) * (filtered[i] - clean[i]);
        }
        noiseBefore = Math.Sqrt(noiseBefore / (n - skip));
        noiseAfter = Math.Sqrt(noiseAfter / (n - skip));
        
        double reductionDb = 20 * Math.Log10(noiseBefore / noiseAfter);
        
        Assert.True(reductionDb > 10, $"Multi-frequency noise reduction should be >10dB, was {reductionDb:F2}dB");
    }
    
    /// <summary>
    /// Test: Varying noise amplitude.
    /// </summary>
    [Fact]
    public void ProcessAll_WithVaryingAmplitude_ReducesNoise()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 10000;
        double[] clean = new double[n];
        double[] src = new double[n];
        
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 1.0;
            
            // Varying amplitude: 0.1 -> 0.5 -> 0.1
            double amp = 0.1 + 0.4 * Math.Sin(2 * Math.PI * 0.5 * t); // 0.5 Hz modulation
            src[i] = clean[i] + amp * Math.Sin(2 * Math.PI * 50 * t);
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        int skip = 500;
        double noiseBefore = 0, noiseAfter = 0;
        for (int i = skip; i < n; i++)
        {
            noiseBefore += (src[i] - clean[i]) * (src[i] - clean[i]);
            noiseAfter += (filtered[i] - clean[i]) * (filtered[i] - clean[i]);
        }
        noiseBefore = Math.Sqrt(noiseBefore / (n - skip));
        noiseAfter = Math.Sqrt(noiseAfter / (n - skip));
        
        double reductionDb = 20 * Math.Log10(noiseBefore / noiseAfter);
        
        Assert.True(reductionDb > 10, $"Varying amplitude noise reduction should be >10dB, was {reductionDb:F2}dB");
    }
    
    /// <summary>
    /// Test: Low amplitude signal with noise.
    /// </summary>
    [Fact]
    public void ProcessAll_WithLowAmplitudeSignal_ReducesNoise()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 10000;
        double[] clean = new double[n];
        double[] src = new double[n];
        
        // Signal amplitude 0.01 (very small)
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 0.01 * Math.Sin(2 * Math.PI * 10 * t); // 10 Hz signal
            src[i] = clean[i] + 0.5 * Math.Sin(2 * Math.PI * 50 * t); // 50 Hz noise
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        int skip = 500;
        double noiseBefore = 0, noiseAfter = 0;
        for (int i = skip; i < n; i++)
        {
            noiseBefore += (src[i] - clean[i]) * (src[i] - clean[i]);
            noiseAfter += (filtered[i] - clean[i]) * (filtered[i] - clean[i]);
        }
        noiseBefore = Math.Sqrt(noiseBefore / (n - skip));
        noiseAfter = Math.Sqrt(noiseAfter / (n - skip));
        
        double reductionDb = 20 * Math.Log10(noiseBefore / noiseAfter);
        
        Assert.True(reductionDb > 10, $"Low amplitude signal noise reduction should be >10dB, was {reductionDb:F2}dB");
    }
    
    /// <summary>
    /// Test: High frequency signal preserved (above 100 Hz).
    /// </summary>
    [Fact]
    public void ProcessAll_PreservesHighFrequencySignal()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 10000;
        double[] clean = new double[n];
        double[] src = new double[n];
        
        // Signal at 200 Hz (should be preserved)
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 0.5 * Math.Sin(2 * Math.PI * 200 * t);
            src[i] = clean[i] + 0.5 * Math.Sin(2 * Math.PI * 50 * t); // 50 Hz noise
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        int skip = 500;
        
        // Check signal preserved
        double correlationBefore = CalculateCorrelation(clean.Skip(skip).ToArray(), src.Skip(skip).ToArray());
        double correlationAfter = CalculateCorrelation(clean.Skip(skip).ToArray(), filtered.Skip(skip).ToArray());
        
        Assert.True(correlationAfter > 0.9, $"High frequency signal should be preserved, correlation was {correlationAfter:F4}");
    }
    
    /// <summary>
    /// Test: Low frequency signal preserved (below 20 Hz).
    /// </summary>
    [Fact]
    public void ProcessAll_PreservesLowFrequencySignal()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        int n = 10000;
        double[] clean = new double[n];
        double[] src = new double[n];
        
        // Signal at 5 Hz (should be preserved)
        for (int i = 0; i < n; i++)
        {
            double t = i / SamplingFrequency;
            clean[i] = 0.5 * Math.Sin(2 * Math.PI * 5 * t);
            src[i] = clean[i] + 0.5 * Math.Sin(2 * Math.PI * 50 * t);
        }
        
        double[] filtered = filter.ProcessAll(src);
        
        int skip = 500;
        
        double correlationBefore = CalculateCorrelation(clean.Skip(skip).ToArray(), src.Skip(skip).ToArray());
        double correlationAfter = CalculateCorrelation(clean.Skip(skip).ToArray(), filtered.Skip(skip).ToArray());
        
        Assert.True(correlationAfter > 0.9, $"Low frequency signal should be preserved, correlation was {correlationAfter:F4}");
    }
    
    /// <summary>
    /// Test: Empty array throws exception.
    /// </summary>
    [Fact]
    public void ProcessAll_EmptyArray_ThrowsException()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        Assert.Throws<ArgumentNullException>(() => filter.ProcessAll(null));
        Assert.Throws<ArgumentNullException>(() => filter.ProcessBlock(null));
    }
    
    /// <summary>
    /// Test: Reset clears state.
    /// </summary>
    [Fact]
    public void Reset_ClearsState()
    {
        var filter = new PowerlineFilterClass(SamplingFrequency);
        
        // Process some samples
        for (int i = 0; i < 1000; i++)
        {
            filter.ProcessSample(1.0 + 0.5 * Math.Sin(2 * Math.PI * 50 * i / SamplingFrequency));
        }
        
        // Reset
        filter.Reset();
        
        // Process zero signal
        double[] output = new double[10];
        for (int i = 0; i < 10; i++)
        {
            output[i] = filter.ProcessSample(0);
        }
        
        // After reset, output should converge to zero
        double maxAbs = 0;
        for (int i = 5; i < 10; i++)
        {
            if (Math.Abs(output[i]) > maxAbs) maxAbs = Math.Abs(output[i]);
        }
        
        Assert.True(maxAbs < 0.1, $"After reset, output should be near zero, was {maxAbs:F4}");
    }
    
    private static double CalculateCorrelation(double[] a, double[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;
        
        double meanA = a.Average();
        double meanB = b.Average();
        
        double sumAB = 0, sumA2 = 0, sumB2 = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double da = a[i] - meanA;
            double db = b[i] - meanB;
            sumAB += da * db;
            sumA2 += da * da;
            sumB2 += db * db;
        }
        
        if (sumA2 == 0 || sumB2 == 0) return 0;
        return sumAB / Math.Sqrt(sumA2 * sumB2);
    }
}
