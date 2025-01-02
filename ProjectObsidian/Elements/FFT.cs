using System;
using System.Numerics;

public class RealtimeAudioFFT
{
    private readonly int bufferSize;
    private readonly int sampleRate;
    private readonly Complex[] fftBuffer;
    private readonly double[] windowFunction;
    private int bufferIndex;

    public class FFTResult
    {
        public double[] Magnitudes { get; set; }
        public double[] Frequencies { get; set; }
        public Complex[] ComplexOutput { get; set; }
    }

    public RealtimeAudioFFT(int bufferSize = 2048, int sampleRate = 44100)
    {
        // Ensure buffer size is power of 2
        this.bufferSize = bufferSize;
        this.sampleRate = sampleRate;
        this.fftBuffer = new Complex[bufferSize];
        this.bufferIndex = 0;

        // Pre-calculate window function
        this.windowFunction = new double[bufferSize];
        for (int i = 0; i < bufferSize; i++)
        {
            // Hanning window
            windowFunction[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (bufferSize - 1)));
        }
    }

    public void ProcessSample(float sample)
    {
        // Add new sample to buffer
        fftBuffer[bufferIndex] = new Complex(sample * windowFunction[bufferIndex], 0);
        bufferIndex = (bufferIndex + 1) % bufferSize;
    }

    public void ProcessBuffer(float[] samples)
    {
        int count = Math.Min(samples.Length, bufferSize - bufferIndex);

        // Add samples to buffer
        for (int i = 0; i < count; i++)
        {
            fftBuffer[bufferIndex] = new Complex(samples[i] * windowFunction[bufferIndex], 0);
            bufferIndex = (bufferIndex + 1) % bufferSize;
        }

        // If there are remaining samples, continue from start of buffer
        for (int i = count; i < samples.Length; i++)
        {
            fftBuffer[bufferIndex] = new Complex(samples[i] * windowFunction[bufferIndex], 0);
            bufferIndex = (bufferIndex + 1) % bufferSize;
        }
    }

    public FFTResult ComputeCurrentFFT()
    {
        // Create a copy of the buffer for FFT processing
        Complex[] fftData = new Complex[bufferSize];

        // Reorder the buffer so the oldest sample is at index 0
        for (int i = 0; i < bufferSize; i++)
        {
            int index = (bufferIndex + i) % bufferSize;
            fftData[i] = fftBuffer[index];
        }

        // Perform FFT
        FFT(fftData);

        // Calculate magnitudes and frequencies
        double[] magnitudes = new double[bufferSize / 2];
        double[] frequencies = new double[bufferSize / 2];

        for (int i = 0; i < bufferSize / 2; i++)
        {
            magnitudes[i] = Math.Sqrt(
                fftData[i].Real * fftData[i].Real +
                fftData[i].Imaginary * fftData[i].Imaginary
            );

            frequencies[i] = i * sampleRate / (double)bufferSize;
        }

        return new FFTResult
        {
            Magnitudes = magnitudes,
            Frequencies = frequencies,
            ComplexOutput = fftData
        };
    }

    private void FFT(Complex[] buffer)
    {
        int bits = (int)Math.Log(buffer.Length, 2);

        // Bit reversal
        for (int j = 1; j < buffer.Length - 1; j++)
        {
            int swapPos = BitReverse(j, bits);
            if (swapPos > j)
            {
                var temp = buffer[j];
                buffer[j] = buffer[swapPos];
                buffer[swapPos] = temp;
            }
        }

        // Cooley-Tukey FFT
        for (int M = 2; M <= buffer.Length; M *= 2)
        {
            double angle = -2 * Math.PI / M;
            Complex Wm = new Complex(Math.Cos(angle), Math.Sin(angle));

            for (int k = 0; k < buffer.Length; k += M)
            {
                Complex W = Complex.One;
                for (int j = 0; j < M / 2; j++)
                {
                    Complex t = W * buffer[k + j + M / 2];
                    Complex u = buffer[k + j];
                    buffer[k + j] = u + t;
                    buffer[k + j + M / 2] = u - t;
                    W *= Wm;
                }
            }
        }
    }

    private int BitReverse(int n, int bits)
    {
        int reversedN = n;
        int count = bits - 1;

        n >>= 1;
        while (n > 0)
        {
            reversedN = (reversedN << 1) | (n & 1);
            count--;
            n >>= 1;
        }

        return ((reversedN << count) & ((1 << bits) - 1));
    }

    // Helper method to get frequency bands
    public double[] GetFrequencyBands(FFTResult fftResult, int numBands)
    {
        double[] bands = new double[numBands];
        int bandWidth = fftResult.Magnitudes.Length / numBands;

        for (int i = 0; i < numBands; i++)
        {
            double sum = 0;
            int start = i * bandWidth;
            int end = start + bandWidth;

            for (int j = start; j < end; j++)
            {
                sum += fftResult.Magnitudes[j];
            }

            bands[i] = sum / bandWidth;
        }

        return bands;
    }
}