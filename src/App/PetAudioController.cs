using Godot;
using System;

namespace DesktopPet.App;

internal sealed class PetAudioController
{
    internal enum Sound
    {
        Click,
        Suction,
        Gulp,
        Reject
    }

    private readonly Node _owner;
    internal bool Muted { get; set; }

    internal PetAudioController(Node owner) => _owner = owner;

    internal void Play(Sound sound)
    {
        if (Muted)
            return;

        var player = new AudioStreamPlayer();
        player.Stream = CreateSound(sound);
        player.Finished += player.QueueFree;
        _owner.AddChild(player);
        player.Play();
    }

    private static AudioStreamWav CreateSound(Sound sound)
    {
        const int rate = 22050;
        var duration = sound switch
        {
            Sound.Click => 0.11,
            Sound.Suction => 0.20,
            Sound.Gulp => 0.24,
            _ => 0.18
        };
        var sampleCount = (int)(rate * duration);
        var data = new byte[sampleCount * 2];

        for (var index = 0; index < sampleCount; index++)
        {
            var t = index / (double)rate;
            var normalized = index / (double)sampleCount;
            var envelope = Math.Sin(Math.PI * normalized) * (1.0 - normalized * 0.35);
            var frequency = sound switch
            {
                Sound.Click => 520 + 460 * normalized,
                Sound.Suction => 980 - 650 * normalized,
                Sound.Gulp => 190 - 70 * normalized,
                _ => 150 + Math.Sin(normalized * Math.PI * 5) * 90
            };
            var wave = Math.Sin(Math.Tau * frequency * t);
            if (sound == Sound.Gulp)
                wave += 0.35 * Math.Sin(Math.Tau * frequency * 0.48 * t);
            var sample = (short)Math.Clamp(wave * envelope * 10500, short.MinValue, short.MaxValue);
            data[index * 2] = (byte)(sample & 0xff);
            data[index * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = rate,
            Stereo = false,
            Data = data
        };
    }
}
