namespace RevCore
{
    public readonly struct UISfxTriggeredEvent : IEvent
    {
        public string Sfx { get; }
        public UISfxTriggeredEvent(string sfx) => Sfx = sfx;
    }
}
