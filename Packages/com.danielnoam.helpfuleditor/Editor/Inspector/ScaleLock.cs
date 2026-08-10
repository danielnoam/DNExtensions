namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// The proportional lock on a scale row: whether dragging one axis carries the other two with it.
    ///
    /// State lives here rather than on an inspector because an inspector is rebuilt for every object
    /// clicked, and a lock that reset with each one would never stay on long enough to be worth
    /// having. One instance therefore stands for one row across every inspector that draws it — the
    /// Transform and RectTransform local rows are the same row as far as anyone using them is
    /// concerned, and had a lock each only because the state was written out twice.
    /// </summary>
    internal sealed class ScaleLock
    {
        /// <summary>The local scale row, shared by the Transform and RectTransform inspectors.</summary>
        public static readonly ScaleLock Local = new ScaleLock();

        /// <summary>
        /// The world scale row. Its own lock rather than the local one: an object under a
        /// non-uniformly scaled parent has different proportions in each of the two spaces, so
        /// wanting one held says nothing about wanting the other.
        /// </summary>
        public static readonly ScaleLock World = new ScaleLock();

        /// <summary>A field rather than a property because the row takes it by ref to draw its toggle.</summary>
        public bool locked;

        private bool _seeded;
        private bool _seededDefault;

        /// <summary>
        /// Seeds the lock from the setting the first time, and again whenever that setting changes.
        /// Comparing against the value last seeded is what lets the preference take effect on the
        /// next selection rather than waiting for a recompile, without overwriting a lock that has
        /// since been toggled by hand.
        /// </summary>
        public void SyncWithSetting()
        {
            bool setting = HelpfulEditorSettings.Inspector.scaleLockDefaultOn;
            if (_seeded && setting == _seededDefault) return;

            locked = setting;
            _seededDefault = setting;
            _seeded = true;
        }
    }
}
