using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Frames
{
    public class FrameManager : MonoBehaviour
    {
        readonly List<Frame> frames = new List<Frame>();

        // immutable copy of every frame, replaced after each change, read by the sequencer on the clock thread
        volatile FrameSnapshot[] publishedSequence = new FrameSnapshot[0];

        // immutable copy of the current frame, read by the transforms on the clock thread.
        // a new snapshot is published whenever the current frame changes, so a reference check detects changes
        volatile FrameSnapshot publishedCurrentFrame;

        Frame currentFrame;
        int currentFrameIndex;
        int currentEmitterId;
        Frame copiedFrame;

        bool isRecording;
        bool isFollowingTempera;

        PlaybackState playbackState;

        public string onText;
        public string offText;

        [SerializeField] UnityEvent<int> onEmitterTypeChanged;
        [SerializeField] UnityEvent<EmitterDetail> onAddEmitter;
        [SerializeField] UnityEvent<EmitterDetail> onRemoveEmitter;
        [SerializeField] UnityEvent<FrameDetail> onFrameChanged;
        [SerializeField] UnityEvent<bool> onRecordingStateChanged;


        private void Awake()
        {
            currentEmitterId = ConfigRegistry.Grid.DefaultEmitterId;

            onText = ConfigRegistry.UI.OnText;
            offText = ConfigRegistry.UI.OffText;

            AddFrame();
        }

        // the result of a transform: the start groups and the events carried along with them
        public void ReceiveTransformResult(FrameSnapshot result)
        {
            if (isRecording)
            {
                RecordFrame(result);
            }
            else
            {
                UpdateCurrentFrame(result);
            }
        }

        public void ToggleRecording()
        {
            SetRecording(!isRecording);
        }

        private void UpdateCurrentFrame(FrameSnapshot content)
        {
            currentFrame.SetContent(content);

            NotifyFrameChanged();
        }

        // replaces the current frame's timed events
        public void SetCurrentFrameEvents(IEnumerable<FrameEvent> events)
        {
            currentFrame.SetEvents(events);

            NotifyFrameChanged();
        }

        private void SetRecording(bool recordingValue)
        {
            if (isRecording == recordingValue) return;

            isRecording = recordingValue;

            LogMan.Log("Recording " + (isRecording ? onText : offText));

            onRecordingStateChanged?.Invoke(isRecording);
        }

        public ulong[] GetCurrentFrameEmitters()
        {
            return currentFrame.GetEmitterGroups();
        }

        public FrameSnapshot GetCurrentFrameSnapshot()
        {
            return currentFrame.GetSnapshot();
        }

        public void SetPlaybackState(PlaybackState newPlaybackState)
        {
            bool wasPlaying = playbackState == PlaybackState.Playing;

            playbackState = newPlaybackState;

            if(playbackState == PlaybackState.Playing)
            {
                SetRecording(false);
            }
            else if (wasPlaying)
            {
                // playback may have stopped part way through a frame's events: show and send the frame as it
                // starts again, which is what editing and transforms work on
                NotifyFrameChanged();
            }
        }

        public void DeleteFrame()
        {
            if (frames.Count == 0) return;

            LogMan.Log("Frame deleted");

            if (frames.Count == 1)
            {
                ClearFrame();
                return;
            }

            frames.RemoveAt(currentFrameIndex);

            SetCurrentFrame(currentFrameIndex);
        }

        public void DeleteAllFrames()
        {
            frames.Clear();
            AddFrame();

            LogMan.Log("All frames deleted");
        }

        public void ClearFrame()
        {
            currentFrame.Clear();

            LogMan.Log("Frame cleared");

            NotifyFrameChanged();
        }

        public void DuplicateFrame()
        {
            LogMan.Log($"Frame duplicated");

            InsertFrameAt(currentFrameIndex + 1, new Frame(frames[currentFrameIndex]));
        }

        public void InsertFrame()
        {
            InsertFrameAt(currentFrameIndex + 1, new Frame());

            LogMan.Log("New frame");
        }

        public void PasteOntoFrame()
        {
            if (copiedFrame == null) return;

            frames[currentFrameIndex] = new Frame(copiedFrame);
            SetCurrentFrame(currentFrameIndex);

            LogMan.Log("Frame pasted");
        }

        public void CopyFrame()
        {
            copiedFrame = new Frame(currentFrame);
            LogMan.Log($"Frame copied");
        }

        public void GoToPlaybackFrame(int frameNumber)
        {
            SetCurrentFrame(frameNumber - 1);
        }

        public void GoToSelectedFrame(int frameNumber)
        {
            if (playbackState == PlaybackState.Playing) return;

            SetCurrentFrame(frameNumber - 1);
        }

        public void AppendFrames(List<Frame> newFrames)
        {
            if (newFrames == null || newFrames.Count == 0)
            {
                LogMan.LogWarning("No frames to append");
                return;
            }

            frames.AddRange(newFrames);

            LogMan.Log($"{newFrames.Count} frames appended");

            SetCurrentFrame(frames.Count - newFrames.Count);
        }

        public void SetFrames(List<Frame> newFrames)
        {
            if (newFrames == null || newFrames.Count == 0)
            {
                LogMan.LogWarning("No frames found");
                return;
            }

            frames.Clear();
            frames.AddRange(newFrames);

            SetCurrentFrame(0);
        }

        public void SetEmitterType(int emitterId)
        {
            if (emitterId != currentEmitterId)
            {
                currentEmitterId = emitterId;

                onEmitterTypeChanged?.Invoke(currentEmitterId);
            }
        }

        // --- changes made on the Tempera (Follow Tempera) ---
        // applied to the current frame only while following and while the sequencer isn't playing (it then owns the
        // device). Raised with IsFromTempera so nothing is sent back. A repeat of what the frame already holds is
        // ignored, so an echo of the app's own output can't loop

        public void SetFollowTempera(bool isFollowing)
        {
            isFollowingTempera = isFollowing;
        }

        private bool CanFollowTempera => isFollowingTempera && playbackState != PlaybackState.Playing;

        public void SetEmitterTypeFromTempera(int emitterId)
        {
            if (!CanFollowTempera) return;

            SetEmitterType(emitterId);
        }

        public void AddEmitterFromTempera(Vector2Int position, int emitterId)
        {
            if (!CanFollowTempera || currentFrame.CheckSameEmitterAtPosition(position, emitterId)) return;

            // the colour is already resolved for 2-Lane by the decoder
            currentFrame.AddEmitter(position, emitterId, enforceTwoLane: false);
            PublishSequence();

            int emitterCount = EmitterUtils.GetEmitterCount(currentFrame.GetEmitterGroups());
            onAddEmitter?.Invoke(new EmitterDetail(position, emitterId, emitterCount, isFromTempera: true));
        }

        public void RemoveEmitterFromTempera(Vector2Int position)
        {
            if (!CanFollowTempera) return;

            if (currentFrame.TryRemoveEmitter(position, out int emitterId))
            {
                PublishSequence();

                int emitterCount = EmitterUtils.GetEmitterCount(currentFrame.GetEmitterGroups());
                onRemoveEmitter?.Invoke(new EmitterDetail(position, emitterId, emitterCount, isFromTempera: true));
            }
        }

        public void RemoveEmitter(Vector2Int position)
        {
            if (currentFrame.TryRemoveEmitter(position, out int emitterId))
            {
                PublishSequence();

                int emitterCount = EmitterUtils.GetEmitterCount(currentFrame.GetEmitterGroups());
                onRemoveEmitter?.Invoke(new EmitterDetail(position, emitterId, emitterCount));
            }
        }

        public void AddEmitter(Vector2Int cellPosition)
        {
            int actualEmitterId = currentFrame.AddEmitter(cellPosition, currentEmitterId);
            PublishSequence();

            int emitterCount = EmitterUtils.GetEmitterCount(currentFrame.GetEmitterGroups());
            EmitterDetail emitterDetail = new EmitterDetail(cellPosition, actualEmitterId, emitterCount);
            onAddEmitter?.Invoke(emitterDetail);
        }

        public IReadOnlyList<Frame> GetFramesFromCurrentPosition()
        {
            int count = frames.Count - currentFrameIndex;

            List<Frame> range = frames.GetRange(currentFrameIndex, count);

            LogMan.Log("Frame Count: " + range.Count);

            return range;
        }

        public int GetCurrentFrameNumber()
        {
            return currentFrameIndex + 1;
        }

        public IReadOnlyList<Frame> GetFrames()
        {
            return frames;
        }

        private FrameDetail GetFrameDetail(Frame frame)
        {
            return new FrameDetail(currentFrameIndex + 1, frames.Count, frame.GetEmitterGroups());
        }

        private void RecordFrame(FrameSnapshot content)
        {
            if (playbackState == PlaybackState.Playing) return;

            InsertFrameAt(currentFrameIndex + 1, new Frame(content));
        }

        private void SetCurrentFrame(int index)
        {
            int newIndex = Mathf.Clamp(index, 0, frames.Count - 1);

            currentFrame = frames[newIndex];
            currentFrameIndex = newIndex;

            NotifyFrameChanged();
        }

        private void AddFrame()
        {
            if (frames.Count == 0)
            {
                frames.Add(new Frame());
                SetCurrentFrame(0);
            }
            else
            {
                InsertFrameAt(currentFrameIndex + 1, new Frame());
            }
        }

        private void InsertFrameAt(int index, Frame frame)
        {        
            frames.Insert(index, frame);
            SetCurrentFrame(index);
        }

        public FrameSnapshot[] PublishedSequence => publishedSequence;

        public FrameSnapshot PublishedCurrentFrame => publishedCurrentFrame;

        private void PublishSequence()
        {
            var sequence = new FrameSnapshot[frames.Count];

            for (int i = 0; i < sequence.Length; i++)
            {
                sequence[i] = frames[i].GetSnapshot();
            }

            publishedSequence = sequence;

            if (currentFrame != null)
            {
                publishedCurrentFrame = currentFrame.GetSnapshot();
            }
        }

        private void NotifyFrameChanged()
        {
            // every structural or content change ends here, apart from single emitter edits
            PublishSequence();

            onFrameChanged?.Invoke(GetFrameDetail(currentFrame));
        }
    }
}