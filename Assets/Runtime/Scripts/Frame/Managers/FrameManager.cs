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
        int gridWidth;
        int gridHeight;

        readonly List<Frame> frames = new List<Frame>();
        Frame currentFrame;
        int currentFrameIndex;
        int currentEmitterId;
        Frame copiedFrame;

        bool isRecording;

        PlaybackState playbackState;

        public string onText;
        public string offText;

        [SerializeField] UnityEvent<int> onEmitterTypeChanged;
        [SerializeField] UnityEvent<EmitterDetail> onAddEmitter;
        [SerializeField] UnityEvent<Vector2Int, int> onRemoveEmitter;
        [SerializeField] UnityEvent<FrameDetail> onFrameChanged;
        [SerializeField] UnityEvent<bool> onRecordingStateChanged;


        private void Awake()
        {
            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            currentEmitterId = ConfigRegistry.Grid.DefaultEmitterId;

            onText = ConfigRegistry.UI.OnText;
            offText = ConfigRegistry.UI.OffText;

            AddFrame();
        }

        public void RecordFrame(ulong[] emitterGroups)
        {
            if (playbackState == PlaybackState.Playing) return;

            InsertFrameAt(currentFrameIndex + 1, new Frame(gridWidth, gridHeight, emitterGroups));
        }

        public void ToggleRecording()
        {
            isRecording = !isRecording;

            LogMan.Log("Recording " + (isRecording ? onText : offText));

            onRecordingStateChanged?.Invoke(isRecording);
        }

        public void UpdateCurrentFrame(ulong[] emitterGroups)
        {  
            currentFrame.SetEmitterGroups(emitterGroups);

            NotifyFrameChanged();
        }

        public ulong[] GetCurrentFrameEmitters()
        {
            return currentFrame.GetEmitterGroups();
        }

        public void SetPlaybackState(PlaybackState newPlaybackState)
        {
            playbackState = newPlaybackState;

            if(playbackState == PlaybackState.Playing)
            {
                isRecording = false;
                onRecordingStateChanged?.Invoke(isRecording);
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
            currentFrame.ClearEmitters();

            NotifyFrameChanged();
        }

        public void DuplicateFrame()
        {
            LogMan.Log($"Frame duplicated");
            InsertFrameAt(currentFrameIndex + 1, new Frame(frames[currentFrameIndex]));
        }

        public void InsertFrame()
        {
            InsertFrameAt(currentFrameIndex + 1, new Frame(gridWidth, gridHeight));

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

            LogMan.Log($"Appended {newFrames.Count} frames. Total frames: {frames.Count}");

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

        public void RemoveEmitter(Vector2Int position)
        {
            if (currentFrame.TryRemoveEmitter(position))
            {
                int emitterCount = EmitterUtils.GetEmitterCount(currentFrame.GetEmitterGroups());

                onRemoveEmitter?.Invoke(position, emitterCount);
            }
        }

        public void AddEmitter(Vector2Int cellPosition)
        {
            if (!currentFrame.CheckSameEmitterAtPosition(cellPosition, currentEmitterId))
            {
                currentFrame.AddEmitter(cellPosition, currentEmitterId);

                EmitterDetail emitterDetail = new EmitterDetail(cellPosition, currentEmitterId, currentFrame.GetEmitterGroups());

                onAddEmitter?.Invoke(emitterDetail);
            }
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
                frames.Add(new Frame(gridWidth, gridHeight));
                SetCurrentFrame(0);
            }
            else
            {
                InsertFrameAt(currentFrameIndex + 1, new Frame(gridWidth, gridHeight));
            }
        }

        private void InsertFrameAt(int index, Frame frame)
        {
            frames.Insert(index, frame);
            SetCurrentFrame(index);
        }

        private void NotifyFrameChanged()
        {
            onFrameChanged?.Invoke(GetFrameDetail(currentFrame));
        }

        public bool IsRecording { get => isRecording; set => isRecording = value; }
    }
}