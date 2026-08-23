# Спецификация: Локальные AI-эмбеддинги (Local AI Embeddings)

## 1. Назначение модуля

Сервис `LocalAiEmbeddingService` предназначен для генерации смысловых векторных представлений (эмбеддингов) описания пользователя (`AiDescription`) и расчета семантического косинусного сходства между анкетами.

---

## 2. Контракт сервиса `IAiEmbeddingService`

```csharp
public interface IAiEmbeddingService
{
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    double CalculateCosineSimilarity(float[] vector1, float[] vector2);
    byte[] VectorToBytes(float[] vector);
    float[] BytesToVector(byte[] bytes);
}
```

---

## 3. Архитектура и алгоритм векторизации

1. **Размерность вектора**: 384 измерения (`VectorSize = 384`).
2. **Токенизация и N-граммы**:
   - Текст приводится к нижнему регистру, очищается от спецсимволов.
   - Разбивается на слова и подсловные символьные n-граммы (длиной от 3 до 5 символов).
   - Поддерживает многоязычный юникод (кириллица, латиница, деванагари).
3. **Хеширование признаков (Feature Hashing Trick)**:
   - Каждая n-грамма хешируется детерминированным алгоритмом Murmur/FNV-1a.
   - Индекс ячейки в векторе: `index = Math.Abs(hash) % 384`.
   - Знак приращения: `sign = ((hash >> 15) & 1) == 0 ? 1.0f : -1.0f`.
   - Вектор накапливает частоты признаков с TF-IDF взвешиванием длины слова.
4. **L2-нормализация (Единичный вектор)**:
   - Вектор нормализуется так, чтобы \(\|v\|_2 = 1.0\):
     \[
     v_i = \frac{v_i}{\sqrt{\sum_{k=1}^{384} v_k^2}}
     \]

---

## 4. Аппаратное SIMD-ускорение косинусного сходства

Так как векторы нормализованы к единичной длине, косинусное сходство эквивалентно скалярному произведению (Dot Product):

\[
\text{Similarity}(A, B) = \sum_{i=1}^{384} A_i B_i
\]

Вычисление производится блоками по `Vector<float>.Count` чисел (8 float для AVX2 / 16 float для AVX-512) с помощью инструкций `System.Numerics.Vector<float>`:

```csharp
var simdBatches = vector1.Length / Vector<float>.Count;
var sumVector = Vector<float>.Zero;

for (var i = 0; i < simdBatches; i++)
{
    var v1 = new Vector<float>(vector1, i * Vector<float>.Count);
    var v2 = new Vector<float>(vector2, i * Vector<float>.Count);
    sumVector += v1 * v2;
}

var dotProduct = Vector.Dot(sumVector, Vector<float>.One);
```

- Скорость вычисления: **< 100 наносекунд** на пару векторов.
- Порог классификации Tier 1 (высокая совместимость): \(\ge 0.55\).

---

## 5. Сериализация в базу данных

- Вектор из 384 `float` преобразуется в массив из `384 * 4 = 1536` байт (`Buffer.BlockCopy`) и сохраняется в столбец `UserProfile.AiVector` (`varbinary(1536)`).
