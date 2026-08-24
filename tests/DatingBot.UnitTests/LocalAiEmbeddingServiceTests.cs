using DatingBot.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DatingBot.UnitTests;

public class LocalAiEmbeddingServiceTests
{
    private readonly LocalAiEmbeddingService _service = new(NullLogger<LocalAiEmbeddingService>.Instance);

    [Fact]
    public async Task Should_GenerateVector_ForValidText()
    {
        var text = "Люблю спорт, музыку и путешествия";
        var vector = await _service.GenerateEmbeddingAsync(text);

        vector.Should().NotBeNull();
        vector!.Length.Should().Be(384);
    }

    [Fact]
    public async Task Should_CalculateHighCosineSimilarity_ForSemanticallySimilarTexts()
    {
        var text1 = "Люблю походы, палатки, природу и костры";
        var text2 = "Люблю походы, кемпинг, природу и костры";

        var vec1 = await _service.GenerateEmbeddingAsync(text1);
        var vec2 = await _service.GenerateEmbeddingAsync(text2);

        vec1.Should().NotBeNull();
        vec2.Should().NotBeNull();

        var similarity = _service.CalculateCosineSimilarity(vec1!, vec2!);
        similarity.Should().BeGreaterThan(0.55);
    }

    [Fact]
    public async Task Should_CalculateLowCosineSimilarity_ForCompletelyDifferentTexts()
    {
        var text1 = "Я увлекаюсь квантовой физикой и математикой";
        var text2 = "Обожаю готовить торты, печь сладости и кулинарию";

        var vec1 = await _service.GenerateEmbeddingAsync(text1);
        var vec2 = await _service.GenerateEmbeddingAsync(text2);

        vec1.Should().NotBeNull();
        vec2.Should().NotBeNull();

        var similarity = _service.CalculateCosineSimilarity(vec1!, vec2!);
        similarity.Should().BeLessThan(0.40);
    }

    [Fact]
    public async Task Should_CorrectlyConvert_VectorToBytesAndBack()
    {
        var original = await _service.GenerateEmbeddingAsync("Тестовый текст для проверки сериализации");
        original.Should().NotBeNull();

        var bytes = _service.VectorToBytes(original!);
        var restored = _service.BytesToVector(bytes);

        restored.Length.Should().Be(original!.Length);
        for (var i = 0; i < original.Length; i++)
        {
            restored[i].Should().BeApproximately(original[i], 0.0001f);
        }
    }

    [Fact]
    public async Task Should_CalculateCosineSimilarity_UsingByteSpan_IdenticalToFloatArray()
    {
        var vec1 = await _service.GenerateEmbeddingAsync("Текст номер один для проверки спанов");
        var vec2 = await _service.GenerateEmbeddingAsync("Текст номер два для проверки спанов");

        vec1.Should().NotBeNull();
        vec2.Should().NotBeNull();

        var bytes2 = _service.VectorToBytes(vec2!);

        var simArray = _service.CalculateCosineSimilarity(vec1!, vec2!);
        var simSpan = _service.CalculateCosineSimilarity(vec1!.AsSpan(), vec2!.AsSpan());
        var simByteSpan = _service.CalculateCosineSimilarity(vec1!.AsSpan(), bytes2.AsSpan());

        simSpan.Should().BeApproximately(simArray, 0.00001);
        simByteSpan.Should().BeApproximately(simArray, 0.00001);
    }

    [Fact]
    public async Task Should_ReturnNull_ForEmptyOrWhitespaceText()
    {
        var result1 = await _service.GenerateEmbeddingAsync("");
        var result2 = await _service.GenerateEmbeddingAsync("   \t\n  ");
        var result3 = await _service.GenerateEmbeddingAsync(" , . ! ? ");

        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }
}
