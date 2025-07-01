using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class SpawnManager : MonoBehaviour
{
    public enum SpawnPattern
    {
        Circle,
        Rectangle,
        Random,
    }

    private float SPAWN_RADIUS = 6f;

    // 웨이브 관리
    private List<WaveData> waveDatas = new();
    private List<SpawnGroupData> spawnGroupDatas = new();
    private List<ActiveWave> activeWaves = new();
    private int nextWaveIndex = 0;
    public const float WAVE_TIME_EXCEEDED_TIME = 15f; // 웨이브 추가 시간

    // 스폰 관리
    private List<Unit> activeEnemies = new();
    private Dictionary<int, Stack<Unit>> enemyPools = new();

    // 스폰 위치 계산
    private Transform playerTransform;

    // 이벤트
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<int> OnWaveTimeExceeded; // 시간 초과 이벤트
    public event Action<Unit> OnEnemySpawned;
    public event Action<Unit> OnEnemyDied;

    // 코루틴 관리
    private Dictionary<int, Coroutine> waveCoroutines = new Dictionary<int, Coroutine>();
    private Dictionary<int, Coroutine> waveTimerCoroutines = new Dictionary<int, Coroutine>();

    [Serializable]
    public class ActiveWave
    {
        public int waveID;
        public WaveData waveData;
        public List<Unit> waveEnemies = new List<Unit>();
        public float elapsedTime;
        public bool isTimeExceeded;

        public ActiveWave(int waveID, WaveData waveData)
        {
            this.waveID = waveID;
            this.waveData = waveData;
            this.elapsedTime = 0f;
            this.isTimeExceeded = false;
        }
    }

    private void Update()
    {
        // 활성 적들 정리 및 웨이브 상태 체크
        CleanupDeadEnemies();
        CheckWaveCompletion();
    }

    #region 초기화

    public void SetPlayerTransform(Transform playerTransform)
    {
        this.playerTransform = playerTransform;
    }

    public void Init(int wave)
    {
        nextWaveIndex = 0;
        waveDatas.Clear();
        activeWaves.Clear();

        WaveData waveData = DataManager.GetWaveData(wave);
        waveDatas.Add(waveData);

        List<int> spawnGroupIDs = waveData.spawnGroupIDs;
        foreach (int groupID in spawnGroupIDs)
        {
            spawnGroupDatas.Add(DataManager.GetSpawnGroupData(groupID));
        }

        StartNextWave();
    }

    #endregion

    #region 웨이브 관리

    public void StartNextWave()
    {
        WaveData waveData = waveDatas[nextWaveIndex];
        ActiveWave activeWave = new ActiveWave(waveData.waveID, waveData);
        activeWaves.Add(activeWave);

        OnWaveStarted?.Invoke(waveData.waveID);

        // 웨이브 코루틴 시작
        Coroutine waveCoroutine = StartCoroutine(WaveCoroutine(activeWave));
        waveCoroutines[waveData.waveID] = waveCoroutine;

        // 웨이브 타이머 코루틴 시작
        Coroutine timerCoroutine = StartCoroutine(WaveTimerCoroutine(activeWave));
        waveTimerCoroutines[waveData.waveID] = timerCoroutine;

        nextWaveIndex++;
    }

    private IEnumerator WaveCoroutine(ActiveWave activeWave)
    {
        // 웨이브 내 스폰 그룹들을 순차적으로 실행
        foreach (int groupID in activeWave.waveData.spawnGroupIDs)
        {
            SpawnGroupData spawnGroup = GetSpawnGroupData(groupID);
            yield return StartCoroutine(SpawnGroupCoroutine(spawnGroup, activeWave));
        }

        // 스폰 그룹이 모두 완료되면 웨이브가 끝날 때까지 대기
        while (activeWave.waveEnemies.Count > 0)
        {
            yield return null;
        }

        CompleteWave(activeWave);
    }

    private IEnumerator WaveTimerCoroutine(ActiveWave activeWave)
    {
        while (activeWave.elapsedTime < activeWave.waveData.duration)
        {
            activeWave.elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 시간 초과
        activeWave.isTimeExceeded = true;
        OnWaveTimeExceeded?.Invoke(activeWave.waveID);
    }

    private void CompleteWave(ActiveWave activeWave)
    {
        OnWaveCompleted?.Invoke(activeWave.waveID);

        // 코루틴 정리
        if (waveCoroutines.TryGetValue(activeWave.waveID, out Coroutine waveCoroutine))
        {
            StopCoroutine(waveCoroutine);
            waveCoroutines.Remove(activeWave.waveID);
        }

        if (waveTimerCoroutines.TryGetValue(activeWave.waveID, out Coroutine timerCoroutine))
        {
            StopCoroutine(timerCoroutine);
            waveTimerCoroutines.Remove(activeWave.waveID);
        }

        activeWaves.Remove(activeWave);
    }

    private void CheckWaveCompletion()
    {
        // 각 활성 웨이브의 적들이 모두 죽었는지 확인
        for (int i = activeWaves.Count - 1; i >= 0; i--)
        {
            ActiveWave activeWave = activeWaves[i];

            // 해당 웨이브의 적들을 정리
            for (int j = activeWave.waveEnemies.Count - 1; j >= 0; j--)
            {
                Unit enemy = activeWave.waveEnemies[j];
                if (enemy == null || enemy.IsDead)
                {
                    RemoveEnemy(enemy);
                    activeWave.waveEnemies.RemoveAt(j);
                }
            }
        }
    }

    public void StopWave(int waveID)
    {
        ActiveWave activeWave = GetActiveWave(waveID);
        if (activeWave == null)
            return;

        if (waveCoroutines.TryGetValue(waveID, out Coroutine waveCoroutine))
        {
            StopCoroutine(waveCoroutine);
            waveCoroutines.Remove(waveID);
        }

        if (waveTimerCoroutines.TryGetValue(waveID, out Coroutine timerCoroutine))
        {
            StopCoroutine(timerCoroutine);
            waveTimerCoroutines.Remove(waveID);
        }

        foreach (Unit enemy in activeWave.waveEnemies)
        {
            PushEnemy(enemy);
        }

        activeWaves.Remove(activeWave);
    }

    public void StopAllWaves()
    {
        foreach (var coroutine in waveCoroutines.Values)
        {
            StopCoroutine(coroutine);
        }
        waveCoroutines.Clear();

        foreach (var coroutine in waveTimerCoroutines.Values)
        {
            StopCoroutine(coroutine);
        }
        waveTimerCoroutines.Clear();

        foreach (ActiveWave activeWave in activeWaves)
        {
            foreach (Unit enemy in activeWave.waveEnemies)
            {
                RemoveEnemy(enemy);
            }
        }

        activeWaves.Clear();
    }

    #endregion

    #region 스폰 그룹 관리

    private IEnumerator SpawnGroupCoroutine(SpawnGroupData spawnGroup, ActiveWave activeWave)
    {
        if (spawnGroup.startDelay > 0f)
            yield return new WaitForSeconds(spawnGroup.startDelay);

        // 반복 횟수만큼 스폰
        for (int i = 0; i < spawnGroup.repeat; i++)
        {
            for (int j = 0; j < spawnGroup.count; j++)
            {
                Unit enemy = CreateEnemy(spawnGroup.unitID, spawnGroup.pattern);
                activeWave.waveEnemies.Add(enemy);
            }

            if (spawnGroup.repeatInterval > 0f)
                yield return new WaitForSeconds(spawnGroup.repeatInterval);
        }
    }

    private Unit CreateEnemy(int unitID, SpawnPattern pattern)
    {
        Vector3 spawnPosition = GetSpawnPosition(pattern);
        Unit enemy = PopEnemy(unitID);
        enemy.Init(unitID, spawnPosition);

        activeEnemies.Add(enemy);
        OnEnemySpawned?.Invoke(enemy);
        return enemy;
    }

    private void RemoveEnemy(Unit enemy)
    {
        activeEnemies.Remove(enemy);
        OnEnemyDied?.Invoke(enemy);
        PushEnemy(enemy);
    }

    private Vector3 GetSpawnPosition(SpawnPattern pattern)
    {
        if (playerTransform == null)
            return Vector3.zero;

        Vector3 playerPos = playerTransform.position;
        return pattern switch
        {
            SpawnPattern.Circle => GetCircleSpawnPosition(playerPos),
            SpawnPattern.Rectangle => GetRectangleSpawnPosition(playerPos),
            SpawnPattern.Random => GetRandomSpawnPosition(playerPos),
            _ => Vector3.zero,
        };
    }

    private Vector3 GetCircleSpawnPosition(Vector3 playerPos)
    {
        Vector2 randomPoint = UnityEngine.Random.insideUnitCircle.normalized;

        float x = playerPos.x + SPAWN_RADIUS * randomPoint.x;
        float z = playerPos.z + SPAWN_RADIUS * randomPoint.y;

        return new Vector3(x, 0f, z);
    }

    private Vector3 GetRectangleSpawnPosition(Vector3 playerPos)
    {
        float halfSize = SPAWN_RADIUS * 0.5f;
        float randomSide = UnityEngine.Random.Range(0f, 4f);

        float x, z;

        if (randomSide < 1f) // 위쪽 변
        {
            x = playerPos.x + UnityEngine.Random.Range(-halfSize, halfSize);
            z = playerPos.z + halfSize;
        }

        else if (randomSide < 2f) // 오른쪽 변
        {
            x = playerPos.x + halfSize;
            z = playerPos.z + UnityEngine.Random.Range(-halfSize, halfSize);
        }

        else if (randomSide < 3f) // 아래쪽 변
        {
            x = playerPos.x + UnityEngine.Random.Range(-halfSize, halfSize);
            z = playerPos.z - halfSize;
        }

        else // 왼쪽 변
        {
            x = playerPos.x - halfSize;
            z = playerPos.z + UnityEngine.Random.Range(-halfSize, halfSize);
        }

        return new Vector3(x, 0f, z);
    }

    private Vector3 GetRandomSpawnPosition(Vector3 playerPos)
    {
        // 반지름 6인 원의 외각에 랜덤 배치 (0.8 ~ 1.0 배율)
        float randomRadius = UnityEngine.Random.Range(0.8f, 1.0f) * SPAWN_RADIUS;
        Vector2 randomPoint = UnityEngine.Random.insideUnitCircle.normalized;

        float x = playerPos.x + randomRadius * randomPoint.x;
        float z = playerPos.z + randomRadius * randomPoint.y;

        return new Vector3(x, 0f, z);
    }

    #endregion

    #region 오브젝트 풀링

    private Unit PopEnemy(int unitID)
    {
        if (enemyPools.TryGetValue(unitID, out Stack<Unit> pool) && pool.Count > 0)
            return pool.Pop();

        Unit unit = GameManager.Instance.resourceManager.GetUnitPrefab(unitID);
        return Instantiate(unit, Vector3.zero, Quaternion.identity);
    }

    private void PushEnemy(Unit enemy)
    {
        if (enemyPools.TryGetValue(enemy.UnitID, out Stack<Unit> pool))
        {
            enemy.Reset();
            pool.Push(enemy);
        }
    }

    #endregion

    #region 유틸

    private WaveData GetWaveData(int waveID)
    {
        if (waveDatas == null)
            return null;

        foreach (WaveData wave in waveDatas)
        {
            if (wave.waveID == waveID)
                return wave;
        }

        return null;
    }

    private SpawnGroupData GetSpawnGroupData(int groupID)
    {
        if (spawnGroupDatas == null)
            return null;

        foreach (SpawnGroupData group in spawnGroupDatas)
        {
            if (group.groupID == groupID)
                return group;
        }

        return null;
    }

    private ActiveWave GetActiveWave(int waveID)
    {
        foreach (ActiveWave activeWave in activeWaves)
        {
            if (activeWave.waveID == waveID)
                return activeWave;
        }

        return null;
    }

    private void CleanupDeadEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Unit enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead)
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }

    #endregion

    #region 웨이브 시간 계산

    public float GetWaveDuration(WaveData waveData)
    {
        float time = 0f;

        foreach (int groupID in waveData.spawnGroupIDs)
        {
            SpawnGroupData data = GetSpawnGroupData(groupID);
            time += GetSpawnGroupDuration(data);
        }

        return time + WAVE_TIME_EXCEEDED_TIME;
    }

    private float GetSpawnGroupDuration(SpawnGroupData spawnGroup)
    {
        return spawnGroup.startDelay + spawnGroup.repeatInterval * (spawnGroup.repeat - 1);
    }

    #endregion
}
