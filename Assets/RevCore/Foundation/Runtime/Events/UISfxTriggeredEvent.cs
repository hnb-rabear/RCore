namespace RevCore
{
    /// <summary>
    /// Published when a UI element requests a sound effect by name. The audio module subscribes
    /// to this on the global bus, letting UI components stay decoupled from the audio system.
    /// </summary>
    public readonly struct UISfxTriggeredEvent : IEvent
    {
        /// <summary>The clip name to play, looked up in the active audio collection.</summary>
        public string Sfx { get; }

        /// <summary>Creates an event for the given clip name.</summary>
        public UISfxTriggeredEvent(string sfx) => Sfx = sfx;
    }
}
