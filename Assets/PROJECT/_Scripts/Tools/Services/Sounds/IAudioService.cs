using System;
using UnityEngine;

public interface IAudioService
{
    /// <summary>Проиграть звук согласно настройкам конфига.
    /// Для 2D OneShot – играет через общий AudioSource.
    /// Для 2D Loop – создаёт эмиттер в 2D.
    /// Для 3D – создаёт/берёт из пула AudioEmitter и играет в мире.</summary>
    /// <param name="config">AudioConfig с клипами и параметрами.</param>
    /// <param name="parent">Родитель эмиттера (для 3D/2D loop).</param>
    /// <param name="position">Мировая позиция (если нет parent).</param>
    /// <param name="clipIndex">Явный индекс клипа (или -1 для случайного).</param>
    /// <param name="fadeIn">Время фейда при старте (сек).</param>
    /// <param name="onFinished">Коллбек по окончанию (для не-лупа).</param>
    void Play(AudioConfig config, Transform parent = null, Vector3? position = null, int clipIndex = -1, float fadeIn = 0f, Action onFinished = null);

    /// <summary>Остановить все активные источники для данного конфига.</summary>
    /// <param name="config">AudioConfig, по которому сгруппированы источники.</param>
    /// <param name="fadeOut">Фейд-аут перед стопом (сек).</param>
    void Stop(AudioConfig config, float fadeOut = 0f);

    /// <summary>Пауза всех активных источников по конфигу.</summary>
    void Pause(AudioConfig config);

    /// <summary>Продолжить все активные источники по конфигу.</summary>
    void Resume(AudioConfig config);

    /// <summary>Проиграть 2D one-shot через общий буфер.</summary>
    void Play2DOneShot(AudioConfig config, int clipIndex = -1, Action onFinished = null);

    /// <summary>Проиграть 2D луп (через emitter в 2D-режиме).</summary>
    void Play2DLoop(AudioConfig config, int clipIndex = -1, float fadeIn = 0f, Action onFinished = null);

    /// <summary>Проиграть 3D звук (или принудительно 2D при force2D).
    /// Возвращает созданный эмиттер (можно хранить/двигать вручную).</summary>
    AudioEmitter Play3D(AudioConfig config,Transform parent = null, Vector3? position = null,bool loop = false, int clipIndex = -1, float fadeIn = 0f, bool force2D = false, Action onFinished = null);

    /// <summary>Установить громкость шины (по AudioType).</summary>
    void SetVolume(AudioType type, float value);

    /// <summary>Получить текущую громкость шины (по AudioType).</summary>
    float GetVolume(AudioType type);

}
