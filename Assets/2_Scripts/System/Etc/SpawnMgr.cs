using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class SpawnMgr : MonoBehaviour
{
    public enum SpawnPattern
    {
        Circle,
        Rectangle,
        Random,
    }

    private float SPAWN_RADIUS = 12f;

    // 웨이브 관리
    private int nextWaveIndex;
    private float maxWaveInvIndex;
    private List<WaveData> waveDatas = new();
    private List<ActiveWave> activeWaves = new();
    public const float WAVE_ADDITIVE_TIME = 5f; // 웨이브 추가 시간

    // 스폰 관리
    public static int UNIT_UNIQUE_ID = 0;
    private Transform playerTransform;
    private List<Unit> aliveEnemies = new();
    private Dictionary<int, Stack<Unit>> enemyPools = new();
    private Stack<ActiveWave> activeWavePools = new();

    public List<Unit> AliveEnemies => aliveEnemies;

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        CheckWaveCompletion(deltaTime);
    }

    public void Reset()
    {
        StopAllCoroutines();

        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            RemoveEnemy(aliveEnemies[i]);
        }

        for (int i = activeWaves.Count - 1; i >= 0; i--)
        {
            RemoveActiveWave(activeWaves[i]);
        }

        activeWaves.Clear();
        waveDatas.Clear();
        nextWaveIndex = 0;
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        this.playerTransform = playerTransform;
    }

    public void Init()
    {
        waveDatas = DataMgr.GetWaveDatas();
        maxWaveInvIndex = 1f / waveDatas.Count;
        StartNextWave();
    }

    #region 웨이브 관리

    public void StartNextWave()
    {
        if (nextWaveIndex >= waveDatas.Count)
            return;

        // 웨이브 생성
        WaveData waveData = waveDatas[nextWaveIndex++];
        ActiveWave activeWave = CreateActiveWave(waveData);

        // 웨이브 연출
        UIMgr.Instance.stageUI.UpdateStageSlider(nextWaveIndex * maxWaveInvIndex);
        if (waveData.waveType == WaveType.Boss)
        {
            UIMgr.Instance.warningUI.ShowWarning();
        }

        // 웨이브 내 스폰될 그룹 유닛들 생성
        List<int> groupIDs = waveData.spawnGroupIDs;
        foreach (int id in groupIDs)
        {
            SpawnGroupData data = DataMgr.GetSpawnGroupData(id);
            StartCoroutine(SpawnGroupCoroutine(data, activeWave));
        }
    }

    private void CheckWaveCompletion(float deltaTime)
    {
        bool canNextWave = false;
        List<ActiveWave> completedWaves = new();
        foreach (ActiveWave wave in activeWaves)
        {
            wave.Update(deltaTime);

            if (wave.isCompleted)
                completedWaves.Add(wave);
            else if (wave.waveID == nextWaveIndex - 1 && wave.isTimeExceeded)
                canNextWave = true;
        }

        foreach (ActiveWave wave in completedWaves)
        {
            activeWaves.Remove(wave);
        }

        if (canNextWave)
            StartNextWave();
    }

    private ActiveWave CreateActiveWave(WaveData waveData)
    {
        ActiveWave wave = PopActiveWave();
        wave.Init(waveData.waveID, GetWaveDuration(waveData), waveData.spawnGroupIDs.Count);
        return wave;
    }

    private void RemoveActiveWave(ActiveWave wave)
    {
        wave.Reset();
        activeWaves.Remove(wave);
        PushActiveWave(wave);
    }

    #endregion

    #region 스폰 그룹 관리

    private IEnumerator SpawnGroupCoroutine(SpawnGroupData data, ActiveWave activeWave)
    {
        if (data.startDelay > 0f)
            yield return new WaitForSeconds(data.startDelay);

        // 반복 횟수만큼 추가 스폰
        for (int i = 0; i < data.repeat; i++)
        {
            for (int j = 0; j < data.count; j++)
            {
                Unit enemy = CreateEnemy(data.unitID, data.pattern);
                activeWave.waveEnemies.Add(enemy);
            }

            if (data.repeatInterval > 0f)
                yield return new WaitForSeconds(data.repeatInterval);
        }

        activeWave.AddGroupCount();
        yield return null;
    }

    private Unit CreateEnemy(int unitID, SpawnPattern pattern)
    {
        Vector3 spawnPosition = GetSpawnPosition(pattern);
        Unit enemy = PopEnemy(unitID);
        enemy.Init(UNIT_UNIQUE_ID++, unitID, spawnPosition);

        aliveEnemies.Add(enemy);
        return enemy;
    }

    public void RemoveEnemy(Unit enemy)
    {
        foreach (ActiveWave wave in activeWaves)
        {
            if (wave.waveEnemies.Remove(enemy))
            {
                if (wave.IsWaveClear())
                {
                    wave.isCompleted = true;
                    StartNextWave();
                }

                break;
            }
        }

        aliveEnemies.Remove(enemy);
        enemy.Reset();
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

    #region 유틸

    public float GetWaveDuration(WaveData waveData)
    {
        float time = 0f;

        List<int> spawnGroupIDs = waveData.spawnGroupIDs;
        foreach (int groupID in spawnGroupIDs)
        {
            SpawnGroupData data = DataMgr.GetSpawnGroupData(groupID);
            time += data.startDelay + data.repeatInterval * (data.repeat - 1);
        }

        return time + WAVE_ADDITIVE_TIME;
    }

    #endregion

    #region 오브젝트 풀링

    private Unit PopEnemy(int unitID)
    {
        if (enemyPools.TryGetValue(unitID, out Stack<Unit> pool) && pool.Count > 0)
            return pool.Pop();

        Unit unit = GameMgr.Instance.resourceMgr.GetEnemyPrefab(unitID);
        return Instantiate(unit, Vector3.zero, Quaternion.identity);
    }

    private void PushEnemy(Unit enemy)
    {
        if (enemyPools.TryGetValue(enemy.UnitID, out Stack<Unit> pool))
            pool.Push(enemy);

        else
        {
            enemyPools[enemy.UnitID] = new Stack<Unit>();
            enemyPools[enemy.UnitID].Push(enemy);
        }
    }

    private void PushActiveWave(ActiveWave wave)
    {
        activeWavePools.Push(wave);
    }

    private ActiveWave PopActiveWave()
    {
        if (!activeWavePools.TryPop(out ActiveWave pooledWave))
            pooledWave = new ActiveWave();

        activeWaves.Add(pooledWave);
        return pooledWave;
    }

    #endregion
}
