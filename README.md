# Powerline Interference Filter for EMG Signals

## Problem Description

EMG (Electromyography) signals are often contaminated with 50 Hz powerline interference. The challenge is:

1. **Sampling frequency**: 2 kHz
2. **Interference frequency**: Can vary from 49 to 51 Hz
3. **Signal similarity**: Sometimes the useful EMG signal resembles the interference
4. **IIR filter ringing**: Short useful signal waves can be misinterpreted as powerline interference, causing IIR filters to "ring"

## Algorithm Description

### Key Principles

1. **Adaptive Frequency Tracking**: The algorithm continuously estimates the actual powerline frequency using zero-crossing detection or phase-locked loop (PLL) approach.

2. **Notch Filter with Adaptive Q**: Instead of a fixed notch filter, we use an adaptive Q-factor that can be dynamically adjusted based on signal characteristics.

3. **Transient Detection**: Before applying aggressive filtering, the algorithm detects transients (short bursts) in the signal to avoid "ringing".

4. **Smooth Transitions**: When filtering is applied or changed, smooth transitions are used to avoid introducing artifacts.

### Algorithm Steps

1. **Frequency Estimation**:
   - Use zero-crossing detection to estimate the current powerline frequency
   - Apply a moving average filter to smooth the frequency estimate
   - Range: 49-51 Hz (clamped)

2. **Transient Detection**:
   - Calculate the signal energy in a sliding window
   - Compare short-term energy to long-term energy
   - If energy suddenly increases (transient detected), reduce filter aggressiveness

3. **Adaptive Notch Filtering**:
   - Design a notch filter with center frequency at the estimated powerline frequency
   - Q-factor is reduced during transients to minimize ringing
   - Use bilinear transform for IIR filter design

4. **Output Smoothing**:
   - Apply smooth transitions between filtered and unfiltered signal
   - This prevents abrupt changes that could be perceived as artifacts

### Why This Approach Works

- **Adaptive frequency tracking** handles the 49-51 Hz variation
- **Transient detection** prevents IIR filter ringing when short EMG bursts occur
- **Smooth transitions** ensure the filter doesn't introduce new artifacts
- **The algorithm is real-time friendly** and can process samples as they arrive

## Implementation

The implementation is in C# with the following components:

- `PowerlineFilter` - Main filter class
- Unit tests using xUnit

## Building and Running Tests

```bash
dotnet restore
dotnet build
dotnet test
```
