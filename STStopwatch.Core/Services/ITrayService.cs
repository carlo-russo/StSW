using System;

namespace STStopwatch.Core.Services
{
    public interface ITrayService : IDisposable
    {
        void Initialize(Action onRestore, Action onToggleMinimal, Action onToggleTopmost, Action onExit);
        void UpdateState(bool isMinimal, bool isAlwaysOnTop);
        void ShowNotification(string title, string message);
        void SetVisible(bool isVisible);
    }
}
