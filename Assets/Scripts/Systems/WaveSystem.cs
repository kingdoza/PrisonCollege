using UnityEngine;

public class WaveSystem : PersistentSingleton<WaveSystem>
{
    public enum DayState
    {
        Day,
        Night,
    }

    [System.Serializable]
    public struct WaveEntry
    {
        public BehaviorWeightSet behaviorWeightSet;
        public DayState dayState;
        [TextArea] public string explanation;
        [Range(0, 1)] public float enforceProb;
        public bool isEndWithArena;
    }

    [Header("Skybox")]
    [SerializeField] private Material _daySkybox;
    [SerializeField] private Material _nightSkybox;
    [Header("Wave Info Entries")]
    [SerializeField] private WaveEntry[] waveEntries;
    [Header("Stat Factors")]
    [SerializeField] private float _nightChaosFactor;
    [SerializeField] private float _nightProjectFactor;

    [SerializeField] private int _currentWave = 0;
    private DayState _currentDayState;
    private float _chaosFactor = 0;
    private float _projectFactor = 0;

    public DayState CurrentDayState => _currentDayState;
    public int CurrentWave => _currentWave;
    public BehaviorWeightSet BehaviorWeightSet => waveEntries[_currentWave - 1].behaviorWeightSet;
    public float ChaosFactor => _chaosFactor;
    public float ProjectFactor => _projectFactor;
    public bool IsLastWave => _currentWave >= waveEntries.Length;
    public bool IsCurrentWaveEndWithArena => waveEntries[_currentWave - 1].isEndWithArena;
    public string WaveInfoExplanation
    { 
        get 
        {
            string explanation = string.Empty;
            //if (_currentDayState == DayState.Night)
            //{
            //    explanation = $"Ω√∞£: π„ <size=80%>(»•∂ı +{((_chaosFactor - 1) * 100).ToString("F0")}%, ¿œ»ø¿≤ +{((_projectFactor - 1) * 100).ToString("F0")}%)</size>\r\n";
            //}
            //else
            //{
            //    explanation = $"Ω√∞£: ≥∑\r\n";
            //}
            explanation += waveEntries[_currentWave - 1].explanation;
            return explanation;
        } 
    }

    public bool HasToEnforce
    {
        get
        {
            float randValue = UnityEngine.Random.value;
            return randValue < waveEntries[_currentWave - 1].enforceProb;
        }
    }




    public void NewWaveEntered()
    {
        _currentWave++;
        _currentDayState = waveEntries[_currentWave - 1].dayState;
        if (_currentDayState == DayState.Day)
        {
            _chaosFactor = 1;
            _projectFactor = 1;
        }
        else
        {
            _chaosFactor = _nightChaosFactor;
            _projectFactor = _nightProjectFactor;
        }
    }



    public void ApplySkybox()
    {
        if(_currentDayState == DayState.Day)
        {
            RenderSettings.skybox = _daySkybox;
        }
        else
        {
            RenderSettings.skybox = _nightSkybox;
        }
        DynamicGI.UpdateEnvironment();
    }



    public void ResetWave()
    {
        _currentWave = 0;
    }
}