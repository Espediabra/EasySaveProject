namespace EasySaveProject.Core.Services;

/// <summary>
/// Gère la pause et la reprise d'un job de sauvegarde en cours.
/// Thread-safe. Indépendant de toute UI.
/// 
/// La stratégie appelle WaitIfPaused() à chaque fichier.
/// La vue (console ou GUI) appelle Toggle() sur action utilisateur.
/// </summary>
public class PauseService
{
    private readonly ManualResetEventSlim _resumeEvent = new(initialState: true);
    private          bool                 _isPaused    = false;
    private volatile bool                 _stopRequested;
    private readonly object               _lock        = new();

    public bool IsPaused { get { lock (_lock) return _isPaused; } }
    public bool IsStopRequested => _stopRequested;

    /// <summary>
    /// Bascule entre pause et reprise.
    /// Appelé par la vue (touche Espace en console, bouton en GUI).
    /// </summary>
    public void Toggle()
    {
        lock (_lock)
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }
    }

    /// <summary>
    /// Met en pause : le prochain WaitIfPaused() bloquera.
    /// </summary>
    public void Pause()
    {
        lock (_lock)
        {
            _isPaused = true;
            _resumeEvent.Reset(); // bloque les appelants de WaitIfPaused
        }
    }

    /// <summary>
    /// Reprend : tous les WaitIfPaused() en attente sont libérés.
    /// </summary>
    public void Resume()
    {
        lock (_lock)
        {
            _isPaused = false;
            _resumeEvent.Set(); // libère les appelants bloqués
        }
    }

    /// <summary>
    /// Appelé par la stratégie entre chaque fichier (ou chunk).
    /// Bloque si en pause, retourne immédiatement sinon.
    /// </summary>
    public void WaitIfPaused()
    {
        _resumeEvent.Wait(); // bloquant si Reset(), immédiat si Set()
    }

    /// <summary>
    /// Stops all jobs: unblocks any WaitIfPaused and sets the stop flag.
    /// The strategy checks IsStopRequested after waking.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            _stopRequested = true;
            _isPaused = false;
            _resumeEvent.Set();
        }
    }

    /// <summary>Resets to initial state (called after all jobs in a run complete).</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _isPaused = false;
            _stopRequested = false;
            _resumeEvent.Set();
        }
    }
}