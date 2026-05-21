using Trainings.Infrastructure.Configuration;

namespace Trainings.Infrastructure.Services;

/// <summary>
/// Singleton that holds the effective runtime mode state.
/// Initialized from configuration defaults and can be overridden at runtime by a SuperAdmin.
/// Overrides are lost on application restart.
/// </summary>
public sealed class AppRuntimeModeState
{
    private readonly object _lock = new();
    private readonly bool _defaultReadOnly;
    private readonly bool _defaultNoEmail;
    private bool _readOnly;
    private bool _noEmail;

    public AppRuntimeModeState(AppModeOptions defaults)
    {
        _defaultReadOnly = defaults.ReadOnly;
        _defaultNoEmail = defaults.NoEmail;
        _readOnly = defaults.ReadOnly;
        _noEmail = defaults.NoEmail;
    }

    public (bool ReadOnly, bool NoEmail) GetEffective()
    {
        lock (_lock)
        {
            return (_readOnly, _noEmail);
        }
    }

    public (bool ReadOnly, bool NoEmail) GetDefaults() => (_defaultReadOnly, _defaultNoEmail);

    public void Set(bool readOnly, bool noEmail)
    {
        lock (_lock)
        {
            _readOnly = readOnly;
            _noEmail = noEmail;
        }
    }

    public void ResetToDefaults()
    {
        lock (_lock)
        {
            _readOnly = _defaultReadOnly;
            _noEmail = _defaultNoEmail;
        }
    }
}
