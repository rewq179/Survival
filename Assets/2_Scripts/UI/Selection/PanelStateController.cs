using System;

// UI 상태 관리 전용 클래스
public class PanelStateController
{
    public enum PanelState
    {
        Hidden,
        Showing,
        Visible,
        Hiding
    }

    private PanelState currentState;
    public event Action<PanelState> OnStateChanged;

    public PanelState CurrentState => currentState;

    public void SetState(PanelState state)
    {
        if (currentState == state)
            return;

        currentState = state;
        OnStateChanged?.Invoke(state);
    }

    public bool CanShow() => currentState == PanelState.Hidden;
    public bool CanHide() => currentState == PanelState.Visible;
    public bool IsAnimating() => currentState == PanelState.Showing || currentState == PanelState.Hiding;
}

