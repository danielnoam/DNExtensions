

namespace DNExtensions.ObjectPooling
{
    public interface IPooledObject
    {
        void OnPoolGet();
        void OnPoolReturn();
        void OnPoolRecycle();
    }

}