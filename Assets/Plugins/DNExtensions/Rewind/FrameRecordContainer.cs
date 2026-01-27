using System.Collections.Generic;

namespace DNExtensions.Utilities.Rewind
{
    public class FrameRecordContainer<T>
    {
        private readonly List<int> _frames = new List<int>();
        private readonly List<T> _values = new List<T>();

        public void Record(int frame, T value, System.Func<T, bool> hasChanged)
        {
            if (hasChanged(value))
            {
                _frames.Add(frame);
                _values.Add(value);
            }
        }

        public bool TryGetValue(int frame, out T value)
        {
            if (_frames.Count == 0 || frame < 0)
            {
                value = default;
                return false;
            }

            // Binary search to find the largest frame <= target frame
            int left = 0;
            int right = _frames.Count - 1;
            int result = -1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                
                if (_frames[mid] <= frame)
                {
                    result = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            if (result >= 0)
            {
                value = _values[result];
                return true;
            }

            value = default;
            return false;
        }

        public T GetLastRecorded()
        {
            if (_values.Count == 0)
            {
                return default;
            }
            return _values[^1];
        }

        
        
        public bool HasRecords()
        {
            return _values.Count > 0;
        }

        /// <summary>
        /// Removes all recorded frames after the specified frame.
        /// This is used to clear stale data when rewinding.
        /// </summary>
        public void RemoveFramesAfter(int frame)
        {
            // Find the first index where frame is greater than the target
            int removeIndex = -1;
            
            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i] > frame)
                {
                    removeIndex = i;
                    break;
                }
            }
            
            // If we found frames to remove, remove them
            if (removeIndex >= 0)
            {
                int countToRemove = _frames.Count - removeIndex;
                _frames.RemoveRange(removeIndex, countToRemove);
                _values.RemoveRange(removeIndex, countToRemove);
            }
        }

        /// <summary>
        /// Clears all recorded data.
        /// </summary>
        public void Clear()
        {
            _frames.Clear();
            _values.Clear();
        }
    }
}