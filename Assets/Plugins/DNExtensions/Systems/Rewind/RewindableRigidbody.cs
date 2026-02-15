using UnityEngine;

namespace DNExtensions.Rewind
{
    [RequireComponent(typeof(Rigidbody))]
    public class RewindableRigidbody : Rewindable
    {
        private Rigidbody _rigidbody;
        
        private readonly FrameRecordContainer<Vector3> _positions = new FrameRecordContainer<Vector3>();
        private readonly FrameRecordContainer<Quaternion> _rotations = new FrameRecordContainer<Quaternion>();
        private readonly FrameRecordContainer<Vector3> _velocities = new FrameRecordContainer<Vector3>();
        private readonly FrameRecordContainer<Vector3> _angularVelocities = new FrameRecordContainer<Vector3>();

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public override void Record(int frame)
        {

            _positions.Record(frame, _rigidbody.position, pos => 
                !_positions.HasRecords() || pos != _positions.GetLastRecorded());
            

            _rotations.Record(frame, _rigidbody.rotation, rot => 
                !_rotations.HasRecords() || rot != _rotations.GetLastRecorded());
            

            _velocities.Record(frame, _rigidbody.linearVelocity, vel => 
                !_velocities.HasRecords() || vel != _velocities.GetLastRecorded());
            
            _angularVelocities.Record(frame, _rigidbody.angularVelocity, angVel => 
                !_angularVelocities.HasRecords() || angVel != _angularVelocities.GetLastRecorded());
        }

        public override void Rewind(int frame)
        {
            if (_positions.TryGetValue(frame, out var position))
            {
                _rigidbody.position = position;
            }
            
            if (_rotations.TryGetValue(frame, out var rotation))
            {
                _rigidbody.rotation = rotation;
            }
            
            if (_velocities.TryGetValue(frame, out var velocity))
            {
                _rigidbody.linearVelocity = velocity;
            }
            if (_angularVelocities.TryGetValue(frame, out var angularVelocity))
            {
                _rigidbody.angularVelocity = angularVelocity;
            }
        }
        

        public override void ClearFutureFrames(int frame)
        {
            _positions.RemoveFramesAfter(frame);
            _rotations.RemoveFramesAfter(frame);
            _velocities.RemoveFramesAfter(frame);
            _angularVelocities.RemoveFramesAfter(frame);
        }
    }
}