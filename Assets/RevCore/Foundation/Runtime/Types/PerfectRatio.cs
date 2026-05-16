namespace RevCore
{
    /// <summary>
    /// Axis to preserve when fitting content into a container at a fixed aspect ratio.
    /// Used by aspect-ratio-preserving UI components.
    /// </summary>
    public enum PerfectRatio
    {
        /// <summary>No constraint — content fills the container along both axes independently.</summary>
        None,
        /// <summary>Width is matched to the container; height is derived from the source aspect ratio.</summary>
        Width,
        /// <summary>Height is matched to the container; width is derived from the source aspect ratio.</summary>
        Height,
    }
}
