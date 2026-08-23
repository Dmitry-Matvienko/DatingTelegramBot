namespace DatingBot.Application.Interfaces;

public interface IAiEmbeddingService
{
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    double CalculateCosineSimilarity(float[] vectorA, float[] vectorB);
    byte[] VectorToBytes(float[] vector);
    float[] BytesToVector(byte[] bytes);
}
