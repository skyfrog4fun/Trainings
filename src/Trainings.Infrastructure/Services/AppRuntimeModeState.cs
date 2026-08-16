using Trainings.Infrastructure.Configuration;

namespace Trainings.Infrastructure.Services;

/// <summary>
/// Singleton that holds the effective runtime mode state.
/// Initialized from configuration defaults and can be overridden at runtime by a SuperAdmin.
/// Overrides are lost on application restart.
/// </summary>
public sealed class AppRuntimeModeState(AppModeOptions defaults)
{
    private readonly Lock _lock = new();
    private readonly bool _defaultReadOnly = defaults.ReadOnly;
    private readonly bool _defaultNoEmail = defaults.NoEmail;
    private bool _readOnly = defaults.ReadOnly;
    private bool _noEmail = defaults.NoEmail;

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
