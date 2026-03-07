using System;
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

        [SerializeField] UnityEvent<Vector3Int,Color> OnAddEmitter;
        [SerializeField] UnityEvent<Vector3Int> OnRemoveEmitter;
        [SerializeField] UnityEvent<VisualFrameDetail> OnChangeFrame;

        private void Awake()
        {
            frames = new List<Frame>();
            emitterDetails = new List<VisualEmitterDetail>();

            currentEmitterId = 0;

            AddFrame();
        }

        public void DeleteFrame()
        {
            if (frames.Count == 0) return;

            frames.RemoveAt(currentFrameIndex);

            if (frames.Count == 0)
            {
                // deleted only existing frame
                AddFrame();
            }
            else if (currentFrameIndex >= frames.Count)
            {
                // deleted last frame
                currentFrameIndex = frames.Count - 1;
                GoToFrameByIndex(currentFrameIndex);
            }
            else
            {
                // deleted in-between frame
                GoToFrameByIndex(currentFrameIndex);
            }
        }

        public void ClearAllFrames()
        {
            frames.Clear();
            AddFrame();
        }

        public void InsertFrame()
        {
            frames.Insert(++currentFrameIndex, new Frame());

            OnChangeFrame?.Invoke(GetFrameDetail(frames[currentFrameIndex]));
        }

        public void PasteOntoFrame()
        {
            if (copiedFrame == null) return;

            frames[currentFrameIndex] = copiedFrame;
            OnChangeFrame?.Invoke(GetFrameDetail(copiedFrame));
            LogMan.Log($"Frame pasted");
        }

        public void CopyFrame()
        {
            copiedFrame = new Frame(frames[currentFrameIndex]);
            LogMan.Log($"Frame {currentFrameIndex + 1} copied!");
        }

    
        public void GoToNextFrame()
        {
            if(currentFrameIndex +1 <= frames.Count -1)
            {
                currentFrameIndex++;
                currentFrame = frames[currentFrameIndex];

                OnChangeFrame?.Invoke(GetFrameDetail(currentFrame));
            }
        }

        public void GoToPreviousFrame()
        {
            if (currentFrameIndex > 0)
            {
                currentFrameIndex--;
                currentFrame = frames[currentFrameIndex];

                OnChangeFrame?.Invoke(GetFrameDetail(currentFrame));
            }
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

            currentFrameIndex = frames.Count - newFrames.Count;
            GoToFrameByIndex(currentFrameIndex);
        }

        public void SetFrames(List<Frame> newFrames)
        {
            if (newFrames == null || newFrames.Count == 0)
            {
                LogMan.LogWarning("No frames found");
                return;
            }
 
            this.frames = newFrames;
            emitterDetails.Clear();

            currentFrameIndex = 0;
            currentFrame = frames[currentFrameIndex];

            OnChangeFrame?.Invoke(GetFrameDetail(currentFrame));            
        }

        public void AddFrame()
        {
            Frame frame = new Frame();

            frames.Add(frame);
            currentFrame = frame;
            currentFrameIndex = frames.Count -1;

            OnChangeFrame?.Invoke(GetFrameDetail(frame));
        }

        public void SetEmitter(int emitterId)
        {
            currentEmitterId = emitterId;
        }

        public void RemoveEmitterAtPosition(Vector3Int position)
        {
            currentFrame.RemoveEmitter(position);

            OnRemoveEmitter?.Invoke(position);
        }

        public void AddEmitterAtPosition(Vector3Int cellPosition)
        {
            LogMan.Log("MatrixManager AddEmitterAtPosition: " + cellPosition);

            currentFrame.AddEmitter(new EmitterDetail(cellPosition, currentEmitterId));

            OnAddEmitter?.Invoke(cellPosition, EmitterUtils.GetColor(currentEmitterId));
        }

        public void GoToFrame(int frameNumber)
        {
            GoToFrameByIndex(frameNumber - 1);
        }

        public void GoToStartFrame()
        {
            if (frames.Count == 0) return;

            currentFrameIndex = 0;
            GoToFrameByIndex(currentFrameIndex);
        }

        public void GoToEndFrame()
        {
            if (frames.Count == 0) return;

            currentFrameIndex = frames.Count - 1;
            GoToFrameByIndex(currentFrameIndex);
        }

        public List<Frame> GetFramesFromCurrentPosition()
        {
            int count = frames.Count - currentFrameIndex;

            return frames.GetRange(currentFrameIndex, count);
        }

        public int GetCurrentFrameNumber()
        {
            return currentFrameIndex + 1;
        }

        public List<Frame> GetFrames()
        {
            return frames;
        }

        private VisualFrameDetail GetFrameDetail(Frame frame)
        {
            emitterDetails.Clear();

            foreach (var emitterDetail in frame.Matrix.Values)
            {
                emitterDetails.Add(new VisualEmitterDetail(emitterDetail.Position, EmitterUtils.GetColor(emitterDetail.EmitterId)));
            }

            return new VisualFrameDetail(currentFrameIndex + 1, frames.Count, emitterDetails);
        }

        private void GoToFrameByIndex(int newIndex)
        {
            if (newIndex >= frames.Count) return;

            currentFrameIndex = newIndex;
            currentFrame = frames[currentFrameIndex];

            OnChangeFrame?.Invoke(GetFrameDetail(currentFrame));
        }

    }
}