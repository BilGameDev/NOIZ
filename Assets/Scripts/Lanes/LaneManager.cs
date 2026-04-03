using System.Collections.Generic;
using UnityEngine;
using static SwipeDirectionHelper;

public class LaneManager : MonoBehaviour
{
    public static List<Lane> Lanes;
    [SerializeField] List<Lane> lanes;

    public enum LaneSelectionMode
    {
        RoundRobin,
        Random,
        LeastRecentlyUsed,
        WeightedRandom
    }

    private float[] laneLastUsedTime;
    private int lastUsedLaneIndex = -1;
    private int roundRobinIndex = 0;

    [Header("Lane Management")]
    public float laneCooldown = 0.3f;
    public bool alternateLanes = true;
    public bool preventSameLaneConsecutive = true;
    public LaneSelectionMode selectionMode = LaneSelectionMode.RoundRobin;

    void Start()
    {
        InitializeLaneTracking();
        Lanes = lanes;
    }

    void OnEnable()
    {
        NOIZEventHandler.OnGameReset += ResetLaneTracking;
    }

    void OnDisable()
    {
        NOIZEventHandler.OnGameReset -= ResetLaneTracking;
    }

    private void InitializeLaneTracking()
    {
        if (lanes != null && lanes.Count > 0)
        {
            laneLastUsedTime = new float[lanes.Count];

            for (int i = 0; i < lanes.Count; i++)
            {
                laneLastUsedTime[i] = -Mathf.Infinity;
            }
        }
    }

    private void ResetLaneTracking()
    {
        if (laneLastUsedTime != null)
        {
            for (int i = 0; i < laneLastUsedTime.Length; i++)
            {
                laneLastUsedTime[i] = -Mathf.Infinity;
            }
        }

        lastUsedLaneIndex = -1;
        roundRobinIndex = 0;
    }

    public void SetLaneLastUsedTime(int laneIndex, float time)
    {
        if (laneLastUsedTime != null && laneIndex >= 0 && laneIndex < laneLastUsedTime.Length)
        {
            laneLastUsedTime[laneIndex] = time;
            lastUsedLaneIndex = laneIndex;
        }
    }

    public void SpawnNoteOnLane(int laneIndex, NotePool notePool)
    {
        if (laneIndex < 0 || laneIndex >= lanes.Count)
            return;

        Lane lane = lanes[laneIndex];

        Vector3 start = new Vector3(lane.transform.position.x, lane.transform.position.y, NOIZManager.Settings.noteStart);
        Vector3 end = new Vector3(lane.transform.position.x, lane.transform.position.y, NOIZManager.Settings.noteEnd);
        Vector3 direction = (end - start).normalized;

        GameObject note = notePool.Get();
        note.transform.position = start;
        note.transform.rotation = Quaternion.LookRotation(direction);

        CutDirection requiredCut = (CutDirection)Random.Range(0, 4);

        NoteMover mover = note.GetComponent<NoteMover>();
        if (mover != null)
            mover.Initialize(direction, lane, notePool, requiredCut);

        NoteVisual visual = note.GetComponent<NoteVisual>();
        if (visual != null)
            visual.SetDirection(requiredCut);
    }

    public int GetNextLane()
    {
        if (lanes == null || lanes.Count == 0)
            return -1;

        float currentTime = Time.time;
        List<int> validLanes = new List<int>();

        for (int i = 0; i < lanes.Count; i++)
        {
            bool onCooldown = currentTime - laneLastUsedTime[i] < laneCooldown;

            if (!onCooldown)
            {
                if (preventSameLaneConsecutive && i == lastUsedLaneIndex)
                    continue;

                validLanes.Add(i);
            }
        }

        if (validLanes.Count == 0)
        {
            float soonestTime = float.MaxValue;
            int soonestLane = -1;

            for (int i = 0; i < lanes.Count; i++)
            {
                if (preventSameLaneConsecutive && i == lastUsedLaneIndex)
                    continue;

                float timeUntilReady = laneCooldown - (currentTime - laneLastUsedTime[i]);
                if (timeUntilReady < soonestTime && timeUntilReady > 0)
                {
                    soonestTime = timeUntilReady;
                    soonestLane = i;
                }
            }

            if (soonestLane != -1)
            {
                Debug.Log($"All lanes on cooldown. Using lane {soonestLane} in {soonestTime:F2}s");
                return soonestLane;
            }
        }

        if (validLanes.Count == 0)
            return -1;

        switch (selectionMode)
        {
            case LaneSelectionMode.RoundRobin:
                return GetRoundRobinLane(validLanes);
            case LaneSelectionMode.Random:
                return validLanes[Random.Range(0, validLanes.Count)];
            case LaneSelectionMode.LeastRecentlyUsed:
                return GetLeastRecentlyUsedLane(validLanes);
            case LaneSelectionMode.WeightedRandom:
                return GetWeightedRandomLane(validLanes);
            default:
                return validLanes[0];
        }
    }

    private int GetRoundRobinLane(List<int> validLanes)
    {
        int startIndex = roundRobinIndex;

        for (int i = 0; i < lanes.Count; i++)
        {
            int laneToCheck = (startIndex + i) % lanes.Count;
            if (validLanes.Contains(laneToCheck))
            {
                roundRobinIndex = (laneToCheck + 1) % lanes.Count;
                return laneToCheck;
            }
        }

        roundRobinIndex = (validLanes[0] + 1) % lanes.Count;
        return validLanes[0];
    }

    private int GetLeastRecentlyUsedLane(List<int> validLanes)
    {
        int oldestLane = validLanes[0];
        float oldestTime = laneLastUsedTime[oldestLane];

        foreach (int laneIndex in validLanes)
        {
            if (laneLastUsedTime[laneIndex] < oldestTime)
            {
                oldestTime = laneLastUsedTime[laneIndex];
                oldestLane = laneIndex;
            }
        }

        Debug.Log($"LeastRecentlyUsed selected lane {oldestLane} (last used {Time.time - oldestTime:F2}s ago)");
        return oldestLane;
    }

    private int GetWeightedRandomLane(List<int> validLanes)
    {
        float currentTime = Time.time;
        float[] weights = new float[validLanes.Count];
        float totalWeight = 0f;

        for (int i = 0; i < validLanes.Count; i++)
        {
            int laneIndex = validLanes[i];
            float timeSinceUsed = currentTime - laneLastUsedTime[laneIndex];
            weights[i] = Mathf.Pow(timeSinceUsed + 1f, 2);
            totalWeight += weights[i];
        }

        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0f;

        for (int i = 0; i < validLanes.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
            {
                Debug.Log($"WeightedRandom selected lane {validLanes[i]}");
                return validLanes[i];
            }
        }

        return validLanes[0];
    }
}
