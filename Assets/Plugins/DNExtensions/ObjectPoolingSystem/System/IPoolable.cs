namespace DNExtensions.Utilities.ObjectPooling
{
    /// <summary>
    /// Interface for objects that need lifecycle callbacks when used with ObjectPool.
    /// Implement to receive notifications for get, return, and recycle events.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when object is retrieved from the pool and activated.
        /// </summary>
        void OnPoolGet();

        /// <summary>
        /// Called when object is returned to the pool and deactivated.
        /// </summary>
        void OnPoolReturn();

        /// <summary>
        /// Called when object is forcibly recycled due to pool size constraints.
        /// </summary>
        void OnPoolRecycle();
    }
}