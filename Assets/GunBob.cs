using Unity.Netcode;
using UnityEngine;

public class GunBob : MonoBehaviour
{
    public enum BobPresets
    {
        HEAVY,
        MEDIUM,
        LIGHT,
        CUSTOM
    }

    [Header("Multipliers")]
    [SerializeField] private float aimMultiplier = 0.5f;
    [SerializeField] private float sprintMultiplier = 2f;

    [Header("Preset")]
    [SerializeField] private BobPresets preset = BobPresets.MEDIUM;

    [Header("Bob Settings")]
    [SerializeField] private float frequency = 1f;
    [SerializeField] private float verticalAmplitude = 1f;
    [SerializeField] private float horizontalAmplitude = 0.5f;
    [SerializeField] private float bobSmoothSpeed = 10f;

    [Header("Rotation Wiggle (one per footstep)")]
    [SerializeField] private float maxZRot = 5f;
    [SerializeField] private float minZRot = 5f;

    [Tooltip("How much of a footstep the wiggle lasts. Lower = quicker, snappier kick.")]
    [SerializeField, Range(0.1f, 1f)] private float wiggleLength = 1f;

    [Tooltip("Fraction of the wiggle spent rising to the peak. Lower = sharper hit.")]
    [SerializeField, Range(0.02f, 0.5f)] private float wiggleAttack = 0.34f;

    [Tooltip("Higher = the wiggle drops off faster after the peak.")]
    [SerializeField, Range(1f, 6f)] private float wiggleFalloff = 2.5f;

    [Header("Runtime")]
    public bool pause;

    private const float TwoPi = Mathf.PI * 2f;
    private const float PhaseWrap = TwoPi * 2f; // sin(phase) has period 2π, cos(phase * 0.5) has period 4π

    private Gun gun;

    // Accumulated bob phase. Advancing it by (dt * frequency) means speed
    // changes (e.g. sprinting) never make the phase jump.
    private float phase;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    private bool leftFoot;

    private float wiggleTime = -1f;
    private float wiggleDuration = 1f;
    private float wiggleTargetZ;

    private bool isSprinting;


#if UNITY_EDITOR

    // Used to detect manual changes in the Inspector.
    private BobPresets previousPreset;

    private float previousFrequency;
    private float previousVerticalAmplitude;
    private float previousHorizontalAmplitude;
    private float previousBobSmoothSpeed;
    private float previousMaxZRot;
    private float previousMinZRot;


    private void OnValidate()
    {
        // First initialization.
        if (!Application.isPlaying &&
            previousPreset == default &&
            frequency == 1f &&
            verticalAmplitude == 1f &&
            horizontalAmplitude == 0.5f)
        {
            previousPreset = preset;
            SavePreviousValues();
        }

        // If the user changes the preset dropdown,
        // apply the new preset values.
        if (preset != previousPreset)
        {
            ApplyPreset();
            SavePreviousValues();
            return;
        }

        // If the user manually changes a value while using
        // a preset, switch to CUSTOM.
        if (preset != BobPresets.CUSTOM && ValuesChanged())
        {
            preset = BobPresets.CUSTOM;
        }

        SavePreviousValues();
    }


    private bool ValuesChanged()
    {
        return
            !Mathf.Approximately(frequency, previousFrequency) ||
            !Mathf.Approximately(verticalAmplitude, previousVerticalAmplitude) ||
            !Mathf.Approximately(horizontalAmplitude, previousHorizontalAmplitude) ||
            !Mathf.Approximately(bobSmoothSpeed, previousBobSmoothSpeed) ||
            !Mathf.Approximately(maxZRot, previousMaxZRot) ||
            !Mathf.Approximately(minZRot, previousMinZRot);
    }


    private void SavePreviousValues()
    {
        previousPreset = preset;

        previousFrequency = frequency;
        previousVerticalAmplitude = verticalAmplitude;
        previousHorizontalAmplitude = horizontalAmplitude;
        previousBobSmoothSpeed = bobSmoothSpeed;

        previousMaxZRot = maxZRot;
        previousMinZRot = minZRot;
    }


    private void ApplyPreset()
    {
        switch (preset)
        {
            case BobPresets.HEAVY:

                frequency = 15f;
                verticalAmplitude = 7f;
                horizontalAmplitude = 7f;
                bobSmoothSpeed = 10f;

                maxZRot = 12f;
                minZRot = 7f;

                break;


            case BobPresets.MEDIUM:

                frequency = 15f;
                verticalAmplitude = 5f;
                horizontalAmplitude = 5f;
                bobSmoothSpeed = 10f;

                maxZRot = 3f;
                minZRot = 1f;

                break;


            case BobPresets.LIGHT:

                frequency = 15f;
                verticalAmplitude = 3f;
                horizontalAmplitude = 3f;
                bobSmoothSpeed = 10f;

                maxZRot = 7f;
                minZRot = 2f;

                break;


            case BobPresets.CUSTOM:

                // Don't change anything.

                break;
        }
    }

#endif


    private void Start()
    {
        gun = GetComponentInParent<Gun>();

        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }


    public void SetSprinting(bool value)
    {
        isSprinting = value;
    }


    private void Update()
    {
        if (!pause)
        {
            AdvancePhase();
            UpdateBob();
            UpdateWiggle();
        }
        else
        {
            StopWiggle();
        }

        MoveBackToBasePos();
    }


    private float GetBobMultiplier()
    {
        if (!gun || !gun.GetManager()) return 0f;

        if (gun.GetManager().isAiming())
        {
            return aimMultiplier;
        }

        if (isSprinting)
        {
            return 1f;
        }

        return 1f;
    }


    private float GetFrequencyMultiplier()
    {
        return isSprinting ? sprintMultiplier : 1f;
    }


    private float GetEffectiveFrequency()
    {
        return frequency * GetFrequencyMultiplier();
    }


    private void MoveBackToBasePos()
    {
        float smooth = 1f - Mathf.Exp(
            -bobSmoothSpeed * Time.deltaTime
        );

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            basePosition,
            smooth
        );
    }


    private void StopWiggle()
    {
        wiggleTime = -1f;

        float smooth = 1f - Mathf.Exp(
            -bobSmoothSpeed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            baseRotation,
            smooth
        );
    }


    // Advances the bob phase and fires a footstep every time it crosses
    // a multiple of 2π (once per full vertical bob cycle).
    private void AdvancePhase()
    {
        int prevStep = Mathf.FloorToInt(phase / TwoPi);

        phase += Time.deltaTime * GetEffectiveFrequency();

        // Wrap to keep the value small. Wrapping at 4π keeps both the
        // sin(phase) and cos(phase * 0.5) terms continuous.
        if (phase >= PhaseWrap)
        {
            phase -= PhaseWrap;
        }

        int newStep = Mathf.FloorToInt(phase / TwoPi);

        if (newStep != prevStep)
        {
            OnFootstep();
        }
    }


    private void OnFootstep()
    {
        float bobMultiplier = GetBobMultiplier();

        float rot = Random.Range(
            minZRot * bobMultiplier,
            maxZRot * bobMultiplier
        );

        // Alternate sides each step.
        leftFoot = !leftFoot;
        wiggleTargetZ = leftFoot ? -rot : rot;

        // The wiggle lasts a fraction of one step, so it always finishes
        // (back at 0) before the next footstep fires.
        wiggleDuration = (TwoPi / GetEffectiveFrequency()) * wiggleLength;
        wiggleTime = 0f;
    }


    private void UpdateBob()
    {
        float bobMultiplier = GetBobMultiplier();

        float vertical =
            Mathf.Sin(phase) *
            verticalAmplitude * bobMultiplier / 100f;

        float horizontal =
            Mathf.Cos(phase * 0.5f) *
            horizontalAmplitude * bobMultiplier / 100f;

        Vector3 targetPosition = basePosition + new Vector3(
            horizontal,
            vertical,
            0f
        );

        // Framerate-independent smoothing.
        float smooth = 1f - Mathf.Exp(
            -bobSmoothSpeed * Time.deltaTime
        );

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            smooth
        );
    }


    private void UpdateWiggle()
    {
        float z = 0f;

        if (wiggleTime >= 0f)
        {
            wiggleTime += Time.deltaTime;

            float u = Mathf.Clamp01(wiggleTime / wiggleDuration);

            float envelope;

            if (u < wiggleAttack)
            {
                // Quick ease-out rise to the peak.
                envelope = Mathf.Sin(u / wiggleAttack * Mathf.PI * 0.5f);
            }
            else
            {
                // Fast drop after the peak, with a short tail.
                float k = (u - wiggleAttack) / (1f - wiggleAttack);
                envelope = Mathf.Pow(1f - k, wiggleFalloff);
            }

            z = wiggleTargetZ * envelope;

            if (u >= 1f)
            {
                wiggleTime = -1f;
            }
        }

        transform.localRotation =
            baseRotation *
            Quaternion.Euler(0f, 0f, z);
    }
}