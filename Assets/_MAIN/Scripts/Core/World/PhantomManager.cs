using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using CHARACTERS;
using DIALOGUE;

/// <summary>
/// Управляет синхронизацией пяти фантомов.
/// Персонажи-фантомы берутся из CharacterConfigurationAsset через DialogueSystem.instance.config
/// — добавь их туда как обычных Character_Sprite с именами Genesis/Melancholy/Fury/Stigma/Ego.
///
/// Доступ в txt файлах:
///   $sync_genesis   $sync_melancholy   $sync_fury   $sync_stigma   $sync_ego
///
/// Пример:
///   Нарратор "Фурия: $sync_fury%"
///   raise_sync(fury 15)
///   decrease_sync(ego 5)
/// </summary>
public class PhantomManager : MonoBehaviour
{
    public static PhantomManager instance { get; private set; }

    // ─── Типы ────────────────────────────────────────────────────────

    public enum PhantomType
    {
        Genesis,     // Интеллектуал
        Melancholy,  // Эмпат
        Fury,        // Ветеран
        Stigma,      // Безумец
        Ego          // Чистый лист
    }

    // ─── Данные одного фантома ───────────────────────────────────────

    [Serializable]
    public class PhantomData
    {
        [HorizontalGroup(200), LabelText("Тип")]
        public PhantomType type;

        [HorizontalGroup, LabelText("Имя в конфиге")]
        [Tooltip("Должно совпадать с Name или Alias в Character Configuration Asset")]
        public string characterName;

        [Range(0f, 100f), LabelText("Синхронизация %")]
        public float sync = 0f;

        [HideInInspector] public int lastThreshold = -1;
    }

    // ─── Inspector ───────────────────────────────────────────────────

    [Title("Фантомы")]
    [InfoBox("Имена должны совпадать с Name/Alias в Character Configuration Asset (DialogueSystem → Config → CharacterConfigurationAsset)")]
    [SerializeField]
    private List<PhantomData> phantoms = new List<PhantomData>
    {
        new PhantomData { type = PhantomType.Genesis,    characterName = "Genesis",    sync = 0f },
        new PhantomData { type = PhantomType.Melancholy, characterName = "Melancholy", sync = 0f },
        new PhantomData { type = PhantomType.Fury,       characterName = "Fury",       sync = 0f },
        new PhantomData { type = PhantomType.Stigma,     characterName = "Stigma",     sync = 0f },
        new PhantomData { type = PhantomType.Ego,        characterName = "Ego",        sync = 0f },
    };

    [Title("Настройки")]
    [SerializeField] private bool debugLog = true;

    // ─── Внутренние переменные VariableStore ─────────────────────────

    private static readonly Dictionary<PhantomType, string> VAR_NAMES = new()
    {
        { PhantomType.Genesis,    "sync_genesis"    },
        { PhantomType.Melancholy, "sync_melancholy" },
        { PhantomType.Fury,       "sync_fury"       },
        { PhantomType.Stigma,     "sync_stigma"     },
        { PhantomType.Ego,        "sync_ego"        },
    };

    private static readonly int[] THRESHOLDS = { 0, 25, 50, 75, 100 };
    private static readonly string[] SPRITE_NAMES = { "silhouette", "outline", "partial", "almost", "full" };

    // ─── Lifecycle ───────────────────────────────────────────────────

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Start — потому что DialogueSystem.instance гарантированно готов
        InitVariableStore();
        EnsurePhantomCharactersExist();
    }

    // ─── Инициализация ───────────────────────────────────────────────

    private void InitVariableStore()
    {
        foreach (var p in phantoms)
        {
            string varName = VAR_NAMES[p.type];
            if (!VariableStore.HasVariable(varName))
                VariableStore.CreateVariable(varName, Mathf.RoundToInt(p.sync));
            else
                VariableStore.TrySetValue(varName, Mathf.RoundToInt(p.sync));
        }
    }

    /// <summary>
    /// Создаёт персонажей-фантомов через CharacterManager если их ещё нет.
    /// Конфиг берётся из DialogueSystem.instance.config.characterConfigurationAsset
    /// — там должны быть прописаны фантомы как обычные Character_Sprite.
    /// </summary>
    private void EnsurePhantomCharactersExist()
    {
        // Проверяем что DialogueSystem доступен
        if (DialogueSystem.instance == null)
        {
            Debug.LogError("[PhantomManager] DialogueSystem.instance не найден");
            return;
        }

        var config = DialogueSystem.instance.config;
        if (config == null || config.characterConfigurationAsset == null)
        {
            Debug.LogError("[PhantomManager] characterConfigurationAsset не назначен в DialogueSystemConfigurationSO");
            return;
        }

        foreach (var p in phantoms)
        {
            // Проверяем что конфиг для этого фантома существует в ассете
            CharacterConfigData charConfig = config.characterConfigurationAsset.GetConfig(p.characterName);
            if (charConfig == null)
            {
                Debug.LogWarning($"[PhantomManager] '{p.characterName}' не найден в Character Configuration Asset — добавь его туда");
                continue;
            }

            // Создаём персонажа если ещё не существует
            if (!CharacterManager.instance.HasCharacter(p.characterName))
            {
                CharacterManager.instance.CreateCharacter(p.characterName, revealAfterCreation: false);
                if (debugLog)
                    Debug.Log($"[PhantomManager] Создан фантом-персонаж: {p.characterName}");
            }
        }
    }

    // ─── Публичный API ───────────────────────────────────────────────

    public void Raise(PhantomType type, float amount)
    {
        PhantomData p = GetData(type);
        if (p == null) return;

        float before = p.sync;
        p.sync = Mathf.Clamp(p.sync + amount, 0f, 100f);
        Apply(p);

        if (debugLog)
            Debug.Log($"[PhantomManager] {type} ↑ {before:F0}% → {p.sync:F0}% (+{amount})");
    }

    public void Decrease(PhantomType type, float amount)
    {
        PhantomData p = GetData(type);
        if (p == null) return;

        float before = p.sync;
        p.sync = Mathf.Clamp(p.sync - amount, 0f, 100f);
        Apply(p);

        if (debugLog)
            Debug.Log($"[PhantomManager] {type} ↓ {before:F0}% → {p.sync:F0}% (-{amount})");
    }

    public float GetSync(PhantomType type)
    {
        PhantomData p = GetData(type);
        return p?.sync ?? 0f;
    }

    public bool HasReached(PhantomType type, float threshold) =>
        GetSync(type) >= threshold;

    public PhantomType GetDominant()
    {
        PhantomType dominant = PhantomType.Ego;
        float max = -1f;
        foreach (var p in phantoms)
            if (p.sync > max) { max = p.sync; dominant = p.type; }
        return dominant;
    }

    public void PrintAll()
    {
        foreach (var p in phantoms)
            Debug.Log($"[PhantomManager] {p.type,-12} {p.sync:F0}%");
    }

    // ─── Применение изменений ────────────────────────────────────────

    private void Apply(PhantomData p)
    {
        // Пишем в VariableStore — $sync_fury и т.д. доступны в txt сразу
        VariableStore.TrySetValue(VAR_NAMES[p.type], Mathf.RoundToInt(p.sync));

        // Проверяем порог → меняем спрайт
        int newThreshold = GetCurrentThreshold(p.sync);
        if (newThreshold != p.lastThreshold)
        {
            p.lastThreshold = newThreshold;
            TryUpdatePhantomSprite(p, newThreshold);
        }
    }

    private int GetCurrentThreshold(float sync)
    {
        for (int i = THRESHOLDS.Length - 1; i >= 0; i--)
            if (sync >= THRESHOLDS[i]) return i;
        return 0;
    }

    private void TryUpdatePhantomSprite(PhantomData p, int thresholdIndex)
    {
        string spriteName = SPRITE_NAMES[Mathf.Clamp(thresholdIndex, 0, SPRITE_NAMES.Length - 1)];

        Character character = CharacterManager.instance.GetCharacter(p.characterName);
        if (character == null)
        {
            Debug.LogWarning($"[PhantomManager] Персонаж '{p.characterName}' не найден в CharacterManager");
            return;
        }

        if (character is Character_Sprite spriteChar)
        {
            Sprite sprite = spriteChar.GetSprite(spriteName);
            if (sprite != null)
                spriteChar.TransitionSprite(sprite, speed: 1f);
            else
                Debug.LogWarning($"[PhantomManager] Спрайт '{spriteName}' не найден для {p.characterName}");
        }
        else
        {
            Debug.LogWarning($"[PhantomManager] {p.characterName} не является Character_Sprite");
        }
    }

    private PhantomData GetData(PhantomType type)
    {
        foreach (var p in phantoms)
            if (p.type == type) return p;
        Debug.LogWarning($"[PhantomManager] Фантом {type} не найден в списке");
        return null;
    }

    // ─── Editor кнопки ───────────────────────────────────────────────

    [Button("Показать все значения"), ButtonGroup]
    private void EditorPrintAll() => PrintAll();

    [Button("Сбросить всё"), ButtonGroup]
    private void EditorResetAll()
    {
        foreach (var p in phantoms)
        {
            p.sync = 0f;
            p.lastThreshold = -1;
            if (Application.isPlaying) Apply(p);
        }
        Debug.Log("[PhantomManager] Все синхронизации сброшены");
    }

    [Button("Переинициализировать персонажей"), ButtonGroup]
    private void EditorReinit()
    {
        if (Application.isPlaying)
            EnsurePhantomCharactersExist();
        else
            Debug.Log("[PhantomManager] Только в Play Mode");
    }
}