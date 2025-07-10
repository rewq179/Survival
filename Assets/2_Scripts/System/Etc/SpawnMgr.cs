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
    private List<WaveData> waveDatas = new();
    private List<ActiveWave> activeWaves = new();
    public const float WAVE_ADDITIVE_TIME = 5f; // 웨이브 추가 시간

    // 스폰 관리
    public static int UNIT_UNIQUE_ID = 0;
    private Transform playerTransform;
    private List<Unit> aliveEnemies = new();
    private Dictionary<int, Stack<Unit>> enemyPools = new();

    [Serializable]
    public class ActiveWave
    {
        public int waveID;
        private float waveTime;
        public float waveDuration;
        public bool isTimeExceeded;
        public bool isCompleted;
        public List<Unit> waveEnemies = new();

        public ActiveWave(int waveID, float waveDuration)
        {
            this.waveID = waveID;
            this.waveTime = 0f;
            this.waveDuration = waveDuration;
            this.isTimeExceeded = false;
            this.isCompleted = false;
        }

        public void Update(float time)
        {
            if (isCompleted)
                return;

            waveTime += time;
            if (waveTime >= waveDuration)
                isTimeExceeded = true;
        }
    }

    private void Update()
    {
        CheckWaveCompletion();
    }

    #region 초기화

    public void SetPlayerTransform(Transform playerTransform)
    {
        this.playerTransform = playerTransform;
    }

    public void Init()
    {
        nextWaveIndex = 0;
        waveDatas.Clear();
        activeWaves.Clear();
        
        waveDatas = DataMgr.GetWaveDatas();
        StartNextWave();
    }

    #endregion

    #region 웨이브 관리

    public void StartNextWave()
    {
        WaveData waveData = waveDatas[nextWaveIndex++];
        ActiveWave activeWave = new ActiveWave(waveData.waveID, GetWaveDuration(waveData));
        activeWaves.Add(activeWave);
        StartCoroutine(WaveCoroutine(activeWave, waveData.spawnGroupIDs));
    }

    private IEnumerator WaveCoroutine(ActiveWave activeWave, List<int> spawnGroupIDs)
    {
        // 웨이브 내 스폰 그룹들을 순차적으로 실행
        foreach (int groupID in spawnGroupIDs)
        {
            SpawnGroupData spawnGroup = DataMgr.GetSpawnGroupData(groupID);
            yield return StartCoroutine(SpawnGroupCoroutine(spawnGroup, activeWave));
        }

        yield return null;
    }

    private void CheckWaveCompletion()
    {
        float time = Time.deltaTime;
        
        bool canNextWave = false;
        List<ActiveWave> completedWaves = new();
        foreach (ActiveWave wave in activeWaves)
        {
            wave.Update(time);
            
            if (wave.isCompleted)
                completedWaves.Add(wave);
            else if(wave.isTimeExceeded)
                canNextWave = true;
        }
        
        foreach (ActiveWave wave in completedWaves)
        {
            activeWaves.Remove(wave);
        }
        
        if (canNextWave)
        {
            StartNextWave();
        }
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
                if (wave.waveEnemies.Count == 0)
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

    private ActiveWave GetActiveWaveData(int waveID)
    {
        foreach (ActiveWave activeWave in activeWaves)
        {
            if (activeWave.waveID == waveID)
                return activeWave;
        }

        return null;
    }

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

    #endregion
}
