using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using DatingBot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DatingBot.Infrastructure.Services;

public class LocalAiEmbeddingService(ILogger<LocalAiEmbeddingService> logger) : IAiEmbeddingService
{
    private const int EmbeddingDimension = 384;
    private static readonly char[] Delimiters = [' ', ',', '.', '!', '?', ';', ':', '-', '\n', '\r', '\t'];

    public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult<float[]?>(null);
        }

        try
        {
            var vector = new float[EmbeddingDimension];
            var cleanText = text.Trim().ToLowerInvariant();
            
            // Собираем диапазоны слов без аллокации строк
            var wordRanges = ExtractWordRanges(cleanText.AsSpan());
            if (wordRanges.Count == 0)
            {
                return Task.FromResult<float[]?>(null);
            }

            var textSpan = cleanText.AsSpan();

            // 1. Векторизация отдельных слов и 3-грамм
            for (var w = 0; w < wordRanges.Count; w++)
            {
                var wordSpan = textSpan[wordRanges[w]];
                ApplyTokenToVector(wordSpan, vector, 1.0f);

                // Добавляем 3-граммы для понимания корней русских слов и морфологии
                if (wordSpan.Length >= 4)
                {
                    for (var i = 0; i <= wordSpan.Length - 3; i++)
                    {
                        var trigramSpan = wordSpan.Slice(i, 3);
                        ApplyTokenToVector(trigramSpan, vector, 0.4f);
                    }
                }
            }

            // 2. Векторизация 2-словных биграмм для контекста
            for (var w = 0; w < wordRanges.Count - 1; w++)
            {
                var word1Span = textSpan[wordRanges[w]];
                var word2Span = textSpan[wordRanges[w + 1]];
                ApplyBigramToVector(word1Span, word2Span, vector, 1.5f);
            }

            // 3. Быстрая SIMD L2-нормализация вектора
            NormalizeL2(vector.AsSpan());

            return Task.FromResult<float[]?>(vector);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при генерации векторного эмбеддинга для текста");
            return Task.FromResult<float[]?>(null);
        }
    }

    private static List<Range> ExtractWordRanges(ReadOnlySpan<char> span)
    {
        var ranges = new List<Range>();
        var start = -1;

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            var isDelimiter = false;
            for (var d = 0; d < Delimiters.Length; d++)
            {
                if (c == Delimiters[d])
                {
                    isDelimiter = true;
                    break;
                }
            }

            if (isDelimiter)
            {
                if (start >= 0)
                {
                    ranges.Add(start..i);
                    start = -1;
                }
            }
            else
            {
                if (start < 0)
                {
                    start = i;
                }
            }
        }

        if (start >= 0)
        {
            ranges.Add(start..span.Length);
        }

        return ranges;
    }

    private static void ApplyTokenToVector(ReadOnlySpan<char> token, Span<float> vector, float weight)
    {
        Span<byte> utf8Bytes = stackalloc byte[128];
        int bytesWritten;

        if (Encoding.UTF8.GetByteCount(token) <= utf8Bytes.Length)
        {
            bytesWritten = Encoding.UTF8.GetBytes(token, utf8Bytes);
            ApplyHashToVector(utf8Bytes[..bytesWritten], vector, weight);
        }
        else
        {
            var maxBytes = Encoding.UTF8.GetByteCount(token);
            var rent = ArrayPool<byte>.Shared.Rent(maxBytes);
            try
            {
                bytesWritten = Encoding.UTF8.GetBytes(token, rent);
                ApplyHashToVector(rent.AsSpan(0, bytesWritten), vector, weight);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rent);
            }
        }
    }

    private static void ApplyBigramToVector(ReadOnlySpan<char> word1, ReadOnlySpan<char> word2, Span<float> vector, float weight)
    {
        Span<byte> utf8Bytes = stackalloc byte[256];
        var byteCount1 = Encoding.UTF8.GetByteCount(word1);
        var byteCount2 = Encoding.UTF8.GetByteCount(word2);

        if (byteCount1 + 1 + byteCount2 <= utf8Bytes.Length)
        {
            var w1 = Encoding.UTF8.GetBytes(word1, utf8Bytes);
            utf8Bytes[w1] = (byte)'_';
            var w2 = Encoding.UTF8.GetBytes(word2, utf8Bytes[(w1 + 1)..]);
            ApplyHashToVector(utf8Bytes[..(w1 + 1 + w2)], vector, weight);
        }
        else
        {
            var totalBytes = byteCount1 + 1 + byteCount2;
            var rent = ArrayPool<byte>.Shared.Rent(totalBytes);
            try
            {
                var w1 = Encoding.UTF8.GetBytes(word1, rent);
                rent[w1] = (byte)'_';
                var w2 = Encoding.UTF8.GetBytes(word2, rent.AsSpan(w1 + 1));
                ApplyHashToVector(rent.AsSpan(0, w1 + 1 + w2), vector, weight);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rent);
            }
        }
    }

    private static void ApplyHashToVector(ReadOnlySpan<byte> utf8Bytes, Span<float> vector, float weight)
    {
        Span<byte> hashBytes = stackalloc byte[32];
        SHA256.HashData(utf8Bytes, hashBytes);

        for (var i = 0; i < hashBytes.Length - 3; i += 4)
        {
            var val = BinaryPrimitives.ReadUInt32LittleEndian(hashBytes.Slice(i, 4));
            var index = (int)(val % (uint)EmbeddingDimension);
            var sign = ((val >> 16) & 1) == 0 ? 1.0f : -1.0f;
            var magnitude = (((val >> 17) & 0xFF) / 255.0f) * weight;

            vector[index] += sign * magnitude;
        }
    }

    private static void NormalizeL2(Span<float> vector)
    {
        var sumSquares = 0.0f;
        var simdSize = Vector<float>.Count;
        var i = 0;

        var vSum = Vector<float>.Zero;
        for (; i <= vector.Length - simdSize; i += simdSize)
        {
            var v = new Vector<float>(vector.Slice(i, simdSize));
            vSum += v * v;
        }

        sumSquares = Vector.Dot(vSum, Vector<float>.One);

        for (; i < vector.Length; i++)
        {
            sumSquares += vector[i] * vector[i];
        }

        var norm = MathF.Sqrt(sumSquares);
        if (norm > 0.000001f)
        {
            var invNorm = 1.0f / norm;
            var vInv = new Vector<float>(invNorm);
            i = 0;
            for (; i <= vector.Length - simdSize; i += simdSize)
            {
                var v = new Vector<float>(vector.Slice(i, simdSize));
                (v * vInv).CopyTo(vector.Slice(i, simdSize));
            }

            for (; i < vector.Length; i++)
            {
                vector[i] *= invNorm;
            }
        }
    }

    public double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA == null || vectorB == null)
        {
            return 0.0;
        }

        return CalculateCosineSimilarity(vectorA.AsSpan(), vectorB.AsSpan());
    }

    public double CalculateCosineSimilarity(float[] vectorA, byte[] vectorBBytes)
    {
        if (vectorA == null || vectorBBytes == null)
        {
            return 0.0;
        }

        return CalculateCosineSimilarity(vectorA.AsSpan(), vectorBBytes.AsSpan());
    }

    public double CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<byte> vectorBBytes)
    {
        if (vectorBBytes.IsEmpty || vectorBBytes.Length % sizeof(float) != 0)
        {
            return 0.0;
        }

        return CalculateCosineSimilarity(vectorA, MemoryMarshal.Cast<byte, float>(vectorBBytes));
    }

    public double CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        if (vectorA.IsEmpty || vectorB.IsEmpty || vectorA.Length != vectorB.Length)
        {
            return 0.0;
        }

        var dotProduct = 0.0f;
        var normA = 0.0f;
        var normB = 0.0f;

        var vectorLength = vectorA.Length;
        var simdSize = Vector<float>.Count;
        var i = 0;

        var vDot = Vector<float>.Zero;
        var vNormA = Vector<float>.Zero;
        var vNormB = Vector<float>.Zero;

        for (; i <= vectorLength - simdSize; i += simdSize)
        {
            var va = new Vector<float>(vectorA.Slice(i, simdSize));
            var vb = new Vector<float>(vectorB.Slice(i, simdSize));

            vDot += va * vb;
            vNormA += va * va;
            vNormB += vb * vb;
        }

        dotProduct = Vector.Dot(vDot, Vector<float>.One);
        normA = Vector.Dot(vNormA, Vector<float>.One);
        normB = Vector.Dot(vNormB, Vector<float>.One);

        for (; i < vectorLength; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        var denominator = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        if (denominator <= 0.000001f)
        {
            return 0.0;
        }

        var similarity = (double)(dotProduct / denominator);
        return Math.Clamp(similarity, 0.0, 1.0);
    }

    public byte[] VectorToBytes(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public float[] BytesToVector(byte[] bytes)
    {
        var vector = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
        return vector;
    }
}
