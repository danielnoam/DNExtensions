using UnityEngine;

namespace DNExtensions.Utilities.Rewind
{
    public class RewindableTransform : Rewindable
    {
        private readonly FrameRecordContainer<Vector3> _positions = new FrameRecordContainer<Vector3>();
        private readonly FrameRecordContainer<Quaternion> _rotations = new FrameRecordContainer<Quaternion>();
        private readonly FrameRecordContainer<Vector3> _scales = new FrameRecordContainer<Vector3>();

        public override void Record(int frame)
        {
            
            _positions.Record(frame, transform.position, pos => 
                !_positions.HasRecords() || pos != _positions.GetLastRecorded());
            
            _rotations.Record(frame, transform.rotation, rot => 
                !_rotations.HasRecords() || rot != _rotations.GetLastRecorded());
            
            _scales.Record(frame, transform.localScale, scale => 
                !_scales.HasRecords() || scale != _scales.GetLastRecorded());
        }

        public override void Rewind(int frame)
        {
            if (_positions.TryGetValue(frame, out var position))
            {
                transform.position = position;
            }
            
            if (_rotations.TryGetValue(frame, out var rotation))
            {
                transform.rotation = rotation;
            }
            
            if (_scales.TryGetValue(frame, out var scale))
            {
                transform.localScale = scale;
            }
        }
        

        public override void ClearFutureFrames(int frame)
        {
            _positions.RemoveFramesAfter(frame);
            _rotations.RemoveFramesAfter(frame);
            _scales.RemoveFramesAfter(frame);
        }
    }
}