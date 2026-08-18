using UnityEngine;
using TMPro;

/// <summary>
/// HUD principal: reloj del turno, contador de dia ("Dia 3 de 10"), y
/// dinero ganado en el turno actual (leido de ShiftStatsTracker, no lo
/// cuenta por su cuenta). La barra de Sanity es un componente aparte
/// (SanityMeterUI) - este script no la duplica.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private ShiftClock shiftClock;
    [SerializeField] private DayCycleManager dayCycleManager;

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text moneyText;

    void Start()
    {
        if (dayCycleManager != null)
            dayCycleManager.onDayStarted.AddListener(HandleDayStarted);

        if (ShiftStatsTracker.Instance != null)
            ShiftStatsTracker.Instance.onMoneyChanged.AddListener(UpdateMoneyText);

        int currentDay = ProgressionData.Instance != null ? ProgressionData.Instance.CurrentDay : 1;
        UpdateDayText(currentDay);
        UpdateMoneyText(ShiftStatsTracker.Instance != null ? ShiftStatsTracker.Instance.MoneyEarnedThisShift : 0f);
    }

    void OnDestroy()
    {
        if (dayCycleManager != null)
            dayCycleManager.onDayStarted.RemoveListener(HandleDayStarted);

        if (ShiftStatsTracker.Instance != null)
            ShiftStatsTracker.Instance.onMoneyChanged.RemoveListener(UpdateMoneyText);
    }

    void Update()
    {
        if (shiftClock != null && timeText != null)
            timeText.text = shiftClock.CurrentInGameTimeString;
    }

    void HandleDayStarted(int day)
    {
        UpdateDayText(day);
    }

    void UpdateDayText(int day)
    {
        if (dayText == null) return;

        int totalDays = ProgressionData.Instance != null ? ProgressionData.Instance.TotalDays : 10;
        dayText.text = $"Day {day} of {totalDays}";
    }

    void UpdateMoneyText(float money)
    {
        if (moneyText == null) return;
        moneyText.text = $"${money:F2}";
    }
}