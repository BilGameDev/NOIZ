using UnityEngine;
using DG.Tweening;
using static SwipeDirectionHelper;
using UnityEngine.Events;

public enum CutResult
{
    None,
    Success,
    WrongDirection
}

[RequireComponent(typeof(Collider))]
public class NoteMover : MonoBehaviour
{
    private Vector3 moveDirection;
    private Lane lane;
    private NotePool pool;

    private bool active;
    private bool cut;
    private bool missed;

    [Header("Cut Settings")]
    public CutDirection requiredCut;

    [Header("Movement Animation")]
    public float quickMoveDuration = 0.3f;
    public float quickMoveDistance = 5f;
    public Ease quickMoveEase = Ease.OutCubic;
    public float slowMoveSpeed = 1f;

    [Header("Cut Window")]
    public float cuttableMinZ = -1.0f;
    public float cuttableMaxZ = 3.0f;

    private Vector3 startPosition;
    private Vector3 quickMoveEndPosition;
    private bool isQuickMoving = true;
    private Tween currentTween;

    public Lane Lane => lane;
    public float SpawnTime { get; private set; }
    public bool IsProcessed => cut || missed;

    public void Initialize(
        Vector3 direction,
        Lane lane,
        NotePool pool,
        CutDirection requiredCut)
    {
        moveDirection = direction.normalized;
        this.lane = lane;
        this.pool = pool;
        this.requiredCut = requiredCut;

        active = true;
        cut = false;
        missed = false;
        isQuickMoving = true;
        SpawnTime = Time.time;

        currentTween?.Kill();
        
        transform.rotation = Quaternion.LookRotation(direction);

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        startPosition = transform.position;
        quickMoveEndPosition = startPosition + moveDirection * quickMoveDistance;

        currentTween = transform.DOMove(quickMoveEndPosition, quickMoveDuration)
            .SetEase(quickMoveEase)
            .OnComplete(() =>
            {
                isQuickMoving = false;
            });

        lane.RegisterNote(this);
    }

    private void Update()
    {
        if (!active)
            return;

        if (!isQuickMoving)
        {
            transform.position += moveDirection * slowMoveSpeed * Time.deltaTime;
        }

        if (transform.position.z <= NOIZManager.Settings.noteEnd)
        {
            OnDespawn();
        }
    }

    private void OnDespawn()
    {
        if (!active) return;

        if (!cut && !missed)
        {
            missed = true;
            NOIZEventHandler.MissNote();
        }

        Cleanup();
    }

    public bool CanBeCut()
    {
        return active && !cut && !missed;
    }

    public bool IsWithinCuttableZRange()
    {
        float z = transform.position.z;
        return z <= cuttableMaxZ && z >= cuttableMinZ;
    }

    public CutResult TryCut(Vector3 swipeWorldStart, Vector3 swipeWorldEnd, Vector2 swipe2DDir)
    {
        if (!CanBeCut())
            return CutResult.None;

        if (!IsWithinCuttableZRange())
            return CutResult.None;

        if (!IsCorrectCut(requiredCut, swipe2DDir))
            return CutResult.WrongDirection;

        cut = true;
        active = false;

        currentTween?.Kill();

        if (lane != null)
            lane.UnregisterNote(this);

        Vector3 swipeWorldDir = (swipeWorldEnd - swipeWorldStart).normalized;

        bool sliced = false;
        if (NoteSlicer.Instance != null)
        {
            sliced = NoteSlicer.Instance.Slice(this, swipeWorldStart, swipeWorldEnd, swipeWorldDir);
        }

        if (!sliced)
        {
            PlayFallbackCutAnimation();
        }

        NOIZEventHandler.HitNote(this);
        Cleanup();

        return CutResult.Success;
    }

    private void PlayFallbackCutAnimation()
    {
        transform.DOScale(0f, 0.08f).SetEase(Ease.InBack);
        transform.DORotate(new Vector3(0f, 180f, 0f), 0.08f, RotateMode.FastBeyond360);
    }

    public void Cleanup()
    {
        currentTween?.Kill();
        active = false;

        if (lane != null)
            lane.UnregisterNote(this);

        if (pool != null)
            pool.Return(gameObject);
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
    }
}