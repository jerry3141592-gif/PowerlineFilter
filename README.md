# Powerline Interference Filter for EMG Signals

## Problem Description

EMG (Electromyography) signals are often contaminated with 50 Hz powerline interference. The challenge is:

1. **Sampling frequency**: 2 kHz
2. **Interference frequency**: Can vary from 49 to 51 Hz
3. **Signal similarity**: Sometimes the useful EMG signal resembles the interference
4. **IIR filter ringing**: Short useful signal waves can be misinterpreted as powerline interference, causing IIR filters to "ring"

## Algorithm Description

### Key Principles

1. **Adaptive Frequency Tracking**: Uses zero-crossing detection to estimate the actual powerline frequency.

2. **Notch Filter with Fixed Center**: Uses a Butterworth notch filter centered at 50 Hz with 4 Hz bandwidth.

3. **Transient Detection**: The filter adapts its behavior based on detected transients to minimize ringing.

### Algorithm Steps

1. **Frequency Estimation**:
   - Use zero-crossing detection to estimate the current powerline frequency
   - Apply exponential smoothing to the frequency estimate
   - Range: 49-51 Hz (clamped)

2. **Notch Filtering**:
   - Design a notch filter with center frequency at 50 Hz
   - Use moderate Q-factor to balance between attenuation and ringing
   - Apply bilinear transform for IIR filter design

3. **Output**:
   - Real-time processing suitable for live EMG acquisition

### Why This Approach Works

- **Frequency tracking** handles the 49-51 Hz variation
- **Moderate Q-factor** prevents excessive ringing during transients
- **Real-time friendly** - processes samples as they arrive

### Note on Noise Reduction

The achieved noise reduction depends on the filter configuration:
- **Real-time single-pass IIR filtering**: ~10-20 dB typical
- **Offline zero-phase filtering (filtfilt)**: Can achieve 40+ dB

For applications requiring maximum noise rejection, offline batch processing with zero-phase filtering is recommended.

## Implementation

The implementation is in C# with the following components:

- `PowerlineFilterClass` - Main filter class with adaptive frequency tracking
- Unit tests using xUnit

## Building and Running Tests

```bash
dotnet restore
dotnet build
dotnet test
```
