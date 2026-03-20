using System.Collections.Generic;
using Tempera.Mental.Core;
using Tempera.Mental.Logs;
using Tempera.Mental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Tempera.Mental.Frames
{
    public class FrameManager : MonoBehaviour
    {
        List<Frame> frames;
        Frame currentFrame;
        int currentFrameIndex;
        int currentEmitterId;
        Frame copiedFrame;

        List<VisualEmitterDetail> emitterDetails;

        [SerializeField] UnityEvent<Vector2Int,Color> onAddEmitter;
        [SerializeField] UnityEvent<Vector2Int> onRemoveEmitter;
        [SerializeField] UnityEvent<VisualFrameDetail> onFrameChange;

        private void Awake()
        {
            frames = new List<Frame>();
            emitterDetails = new List<VisualEmitterDetail>();

            AddFrame();
        }

        public void DeleteFrame()
        {
            if (frames.Count == 0) return;

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
        }

        public void ClearFrame()
        {
            currentFrame.ClearEmitters();

            NotifyFrameChanged();
        }

        public void DuplicateFrame()
        {
            InsertFrameAt(currentFrameIndex + 1, new Frame(frames[currentFrameIndex]));
        }

        public void InsertFrame()
        {
            LogMan.Log("InsertFrame");

            InsertFrameAt(currentFrameIndex + 1, new Frame());
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
            LogMan.Log($"Frame {currentFrameIndex + 1} copied");
        }
    
        public void GoToNextFrame()
        {
            SetCurrentFrame(currentFrameIndex +1);
        }

        public void GoToPreviousFrame()
        {
            SetCurrentFrame(currentFrameIndex - 1);
        }

        public void GoToFrame(int frameNumber)
        {
            SetCurrentFrame(frameNumber - 1);
        }

        public void GoToStartFrame()
        {
            if (frames.Count == 0) return;

            SetCurrentFrame(0);
        }

        public void GoToEndFrame()
        {
            if (frames.Count == 0) return;

            SetCurrentFrame(frames.Count - 1);
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
 
            this.frames = new List<Frame>(newFrames);
            emitterDetails.Clear();

            SetCurrentFrame(0);           
        }

        public void SetEmitter(int emitterId)
        {
            currentEmitterId = emitterId;
        }

        public void RemoveEmitterAtPosition(Vector2Int position)
        {
            if (currentFrame.TryRemoveEmitter(position))
            {
                onRemoveEmitter?.Invoke(position);
            }
        }

        public void AddEmitterAtPosition(Vector2Int cellPosition)
        {
            LogMan.Log("FrameManager AddEmitterAtPosition: " + cellPosition);

            if (!currentFrame.CheckSameEmitterAtPosition(cellPosition, currentEmitterId))
            {
                currentFrame.AddEmitter(new EmitterDetail(cellPosition, currentEmitterId));

                onAddEmitter?.Invoke(cellPosition, EmitterUtils.GetColor(currentEmitterId));
            }
        }

        public IReadOnlyList<Frame> GetFramesFromCurrentPosition()
        {
            int count = frames.Count - currentFrameIndex;

            return frames.GetRange(currentFrameIndex, count);
        }

        public int GetCurrentFrameNumber()
        {
            return currentFrameIndex + 1;
        }

        public IReadOnlyList<Frame> GetFrames()
        {
            return frames;
        }

        private VisualFrameDetail GetFrameDetail(Frame frame)
        {
            emitterDetails.Clear();

            foreach (var emitterDetail in frame.Matrix.Values)
            {
                emitterDetails.Add(new VisualEmitterDetail(new Vector3Int(emitterDetail.Position.x, emitterDetail.Position.y),
                    EmitterUtils.GetColor(emitterDetail.EmitterId)));
            }

            return new VisualFrameDetail(currentFrameIndex + 1, frames.Count, emitterDetails);
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

        private void NotifyFrameChanged()
        {
            onFrameChange?.Invoke(GetFrameDetail(currentFrame));
        }
    }
}