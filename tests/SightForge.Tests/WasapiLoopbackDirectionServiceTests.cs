using NAudio.Wave;
using SightForge.Models;
using SightForge.Services;
using Xunit;

namespace SightForge.Tests;

public sealed class WasapiLoopbackDirectionServiceTests
{
    private static readonly WaveFormat StereoFloat = WaveFormat.CreateIeeeFloatWaveFormat(48_000, 2);
    private static readonly SoundForgeSettings Settings = new()
    {
        DirectionDeadZone = 0.10,
        MinimumAudibleLevel = 0.001
    };

    [Fact]
    public void AnalyzeBuffer_ClassifiesLeftDominantAudio()
    {
        var buffer = CreateStereoFloatBuffer(0.80f, 0.10f, 512);
        var result = WasapiLoopbackDirectionService.AnalyzeBuffer(buffer, buffer.Length, StereoFloat, Settings);

        Assert.Equal(HorizontalAudioDirection.Left, result.Direction);
        Assert.True(result.LeftLevel > result.RightLevel);
        Assert.Contains("o'clock", result.ClockPosition);
    }

    [Fact]
    public void AnalyzeBuffer_ClassifiesRightDominantAudio()
    {
        var buffer = CreateStereoFloatBuffer(0.10f, 0.80f, 512);
        var result = WasapiLoopbackDirectionService.AnalyzeBuffer(buffer, buffer.Length, StereoFloat, Settings);

        Assert.Equal(HorizontalAudioDirection.Right, result.Direction);
        Assert.True(result.RightLevel > result.LeftLevel);
    }

    [Fact]
    public void AnalyzeBuffer_ClassifiesBalancedAudioAsCenter()
    {
        var buffer = CreateStereoFloatBuffer(0.55f, 0.55f, 512);
        var result = WasapiLoopbackDirectionService.AnalyzeBuffer(buffer, buffer.Length, StereoFloat, Settings);

        Assert.Equal(HorizontalAudioDirection.Center, result.Direction);
        Assert.Equal("12 o'clock", result.ClockPosition);
    }

    [Fact]
    public void AnalyzeBuffer_ReturnsSilenceForUnsupportedEncoding()
    {
        var format = new WaveFormat(48_000, 24, 2);
        var buffer = new byte[256];
        var result = WasapiLoopbackDirectionService.AnalyzeBuffer(buffer, buffer.Length, format, Settings);

        Assert.Equal(0, result.CombinedLevel);
        Assert.Equal(HorizontalAudioDirection.Center, result.Direction);
    }

    private static byte[] CreateStereoFloatBuffer(float left, float right, int frameCount)
    {
        var buffer = new byte[frameCount * 2 * sizeof(float)];
        for (var frame = 0; frame < frameCount; frame++)
        {
            BitConverter.GetBytes(left).CopyTo(buffer, frame * 8);
            BitConverter.GetBytes(right).CopyTo(buffer, (frame * 8) + 4);
        }
        return buffer;
    }
}
