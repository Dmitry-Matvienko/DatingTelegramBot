---
name: matchmaking-scoring
description: >-
  Methodology and logic for dating profile recommendation, 4-tier matchmaking scoring, priority queuing, and mutual match events.
  Use when designing or tuning the candidate search algorithm, geo-distance calculations, or rating/match processing.
---

# Matchmaking & Recommendation Scoring (DatingBot)

## 1. Базовые фильтры отбора кандидатов (Hard Filters)

Пул кандидатов отбирается строго по следующим критериям:
- **Завершенность анкеты:** `UserProfile.IsCompleted == true`.
- **Исключение себя:** `Candidate.UserId != CurrentUser.Id`.
- **Статус пользователя:** `User.State != UserState.Banned`.
- **Соответствие целевого пола:** Взаимное совпадение полов (`Gender` и `TargetGender`).
- **Возрастной диапазон:** Соответствие битовым флагам `AgeFilters` (`AgeCategoryFilter`) или ручному диапазону `SearchMinAge`..`SearchMaxAge`.
- **Исключение оцененных и пожалованных:** Отсутствие записей в `ProfileRatings` от текущего пользователя и отсутствие активных жалоб в `ProfileReports`.
- **Языковая совместимость:** Совпадение языковой группы (`UserProfileRepository.GetCompatibleLanguages`).

## 2. 4-уровневый каскадный алгоритм (Cascade Tier Logic)

Выдача кандидатов ранжируется по 4 приоритетным уровням (`MatchTier`):

1. **Tier 1: AI Compatibility (`MatchTier.AiCompatibility`)**
   - Семантическое косинусное сходство векторов эмбеддингов описаний (`AiVector`, 384 float) $\ge 0.55$.
   - Вычисляется с аппаратным SIMD-ускорением (`System.Numerics.Vector<float>`).
   - Бейдж: *"✨ Высокая совместимость по интересам и вайбу на основе ИИ"*.

2. **Tier 2: Common Interests (`MatchTier.CommonInterests`)**
   - Наличие $\ge 1$ общих интересов между пользователями.
   - Ранжирование: по убыванию количества общих интересов.
   - Бейдж: *"🎯 N общих интереса: 🎵 Музыка, 🎮 Игры"*.

3. **Tier 3: Same City (`MatchTier.SameCity`)**
   - Пользователи проживают в одном городе (`CityId` или название города).
   - Бейдж: *"📍 Собеседник из вашего города"*.

4. **Tier 4: Nearby City (`MatchTier.NearbyCity`)**
   - Кандидаты из соседних городов в радиусе до **500 км**.
   - Расстояние вычисляется по формуле гаверсинусов (Haversine Distance) по географическим координатам (`Latitude`, `Longitude`).
   - Ранжирование: по возрастанию расстояния в километрах.
   - Бейдж: *"🚗 Город Химки (~19 км от вас)"*.

## 3. 10-балльная система рейтинга и взаимная симпатия (Mutual Match)

- Пользователь ставит оценку от **1 до 10** ($\star$).
- Запись сохраняется в сущности `ProfileRating` (`FromUserId`, `ToUserId`, `Score`, `CreatedAt`).
- **Очередь входящих оценок:** Непросмотренные оценки $\ge 6$ показываются в приоритетном порядке (`SendRaterCardAsync`).
- **Взаимная симпатия:** Если оба пользователя оценили друг друга на балл $\ge 6$:
  1. Детектируется Mutual Match (без отдельной таблицы, рассчитывается из `ProfileRatings`).
  2. Обоим пользователям отправляются уведомления с карточками совпадения и прямой ссылкой на контакт (`@username` или `tg://user?id={TelegramId}`).

## 4. Сброс истории для города (`ResetHistoryForCityAsync`)

При исчерпании пула анкет в городе и соседних городах пользователю предоставляется действие **«🔄 Искать заново»**, очищающее историю выставленных им оценок для анкет текущего города.
