using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using DatingBot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DatingBot.Infrastructure.Services;

public class LocalAiEmbeddingService(ILogger<LocalAiEmbeddingService> logger) : IAiEmbeddingService
{
    private const int EmbeddingDimension = 384;

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
            var words = cleanText.Split([' ', ',', '.', '!', '?', ';', ':', '-', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                return Task.FromResult<float[]?>(null);
            }

            // Векторизация слов и смысловых n-грамм
            foreach (var word in words)
            {
                ApplyTokenToVector(word, vector, 1.0f);

                // Добавляем 3-граммы и 4-граммы для понимания корней русских слов и морфологии
                if (word.Length >= 4)
                {
                    for (var i = 0; i <= word.Length - 3; i++)
                    {
                        var trigram = word.Substring(i, 3);
                        ApplyTokenToVector(trigram, vector, 0.4f);
                    }
                }
            }

            // Добавляем 2-словные биграммы для захвата контекста (например: "люблю музыку", "играю футбол")
            for (var i = 0; i < words.Length - 1; i++)
            {
                var bigram = $"{words[i]}_{words[i + 1]}";
                ApplyTokenToVector(bigram, vector, 1.5f);
            }

            // L2-нормализация вектора (чтобы длина вектора была равна 1.0 для быстрого косинусного сходства)
            NormalizeL2(vector);

            return Task.FromResult<float[]?>(vector);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при генерации векторного эмбеддинга для текста");
            return Task.FromResult<float[]?>(null);
        }
    }

    public double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length || vectorA.Length == 0)
        {
            return 0.0;
        }

        // SIMD-ускоренное скалярное произведение
        var dotProduct = 0.0f;
        var normA = 0.0f;
        var normB = 0.0f;

        var vectorLength = vectorA.Length;
        var simdSize = Vector<float>.Count;
        var i = 0;

        for (; i <= vectorLength - simdSize; i += simdSize)
        {
            var va = new Vector<float>(vectorA, i);
            var vb = new Vector<float>(vectorB, i);

            dotProduct += Vector.Dot(va, vb);
            normA += Vector.Dot(va, va);
            normB += Vector.Dot(vb, vb);
        }

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

    private static void ApplyTokenToVector(string token, float[] vector, float weight)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        for (var i = 0; i < hashBytes.Length - 3; i += 4)
        {
            var val = BitConverter.ToUInt32(hashBytes, i);
            var index = (int)(val % (uint)EmbeddingDimension);
            var sign = ((val >> 16) & 1) == 0 ? 1.0f : -1.0f;
            var magnitude = (((val >> 17) & 0xFF) / 255.0f) * weight;

            vector[index] += sign * magnitude;
        }
    }

    private static void NormalizeL2(float[] vector)
    {
        var sumSquares = 0.0f;
        for (var i = 0; i < vector.Length; i++)
        {
            sumSquares += vector[i] * vector[i];
        }

        var norm = MathF.Sqrt(sumSquares);
        if (norm > 0.000001f)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }
    }
}
