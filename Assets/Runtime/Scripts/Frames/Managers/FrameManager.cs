using System.Collections.Generic;
using Tempera.Mental.Core;
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

        List<VisualEmitterDetail> emittersInFrame;

        [SerializeField] UnityEvent<Vector3Int,Color> OnAddEmitter;
        [SerializeField] UnityEvent<Vector3Int> OnRemoveEmitter;
        [SerializeField] UnityEvent<List<VisualEmitterDetail>> OnChangeFrame;

        private void Awake()
        {
            frames = new List<Frame>();
            emittersInFrame = new List<VisualEmitterDetail>();

            currentEmitterId = 0;

            AddFrame();
        }

        public void DeleteFrame()
        {
            if (frames.Count == 0) return; // Guard clause

            frames.RemoveAt(currentFrameIndex);

            if (frames.Count == 0)
            {
                // Case 1: No frames left, reset to a clean state
                currentFrameIndex = 0;
                AddFrame();
            }
            else if (currentFrameIndex >= frames.Count)
            {
                // Case 2: We deleted the last frame, move index back by 1
                currentFrameIndex = frames.Count - 1;
                GoToFrame(currentFrameIndex);
            }
            else
            {
                // Case 3: We deleted a middle frame, stay at current index 
                // but load the frame that "slid" into this position
                GoToFrame(currentFrameIndex);
            }
        }


        public void ClearAllFrames()
        {
            frames.Clear();
            AddFrame();
        }

        public void DuplicateFrame()
        {
            Frame frame = new Frame(currentFrame);

            frames.Add(frame);
            currentFrame = frame;
            currentFrameIndex = frames.Count - 1;

            OnChangeFrame?.Invoke(GetEmittersInFrame(frame));
        }

        public void GoToFrame(int newIndex)
        {
            if (newIndex < frames.Count)
            {
                currentFrame = frames[newIndex];
                emittersInFrame = GetEmittersInFrame(currentFrame);
                currentFrameIndex = newIndex;
            }

            OnChangeFrame?.Invoke(emittersInFrame);
        }

        internal void GoToNextFrame()
        {
            if(currentFrameIndex +1 <= frames.Count -1)
            {
                currentFrameIndex++;
                currentFrame = frames[currentFrameIndex];

                emittersInFrame = GetEmittersInFrame(currentFrame);
                OnChangeFrame?.Invoke(emittersInFrame);
            }
        }

        public void GoToPreviousFrame()
        {
            if (currentFrameIndex > 0)
            {
                currentFrameIndex--;
                currentFrame = frames[currentFrameIndex];

                // Extract visuals and fire the event
                emittersInFrame = GetEmittersInFrame(currentFrame);
                OnChangeFrame?.Invoke(emittersInFrame);
            }
        }

        public void AppendFrames(List<Frame> newFrames)
        {
            if (newFrames == null || newFrames.Count == 0) return;

            // This adds the entire collection to the end of your 'frames' list
            frames.AddRange(newFrames);

            Debug.Log($"Appended {newFrames.Count} frames. Total frames: {frames.Count}");

            // Optional: If you want the UI to update immediately to show the first 
            // of the newly appended frames:
            currentFrameIndex = frames.Count - newFrames.Count;
            GoToFrame(currentFrameIndex);
        }

        // todo needs a better name eg CreateNewFrameSet
        // todo create clear frame action
        public void AddFrames(List<Frame> frames)
        {
            this.frames = frames;
            emittersInFrame.Clear();

            if(frames.Count > 0)
            {
                currentFrame = frames[0];
                emittersInFrame = GetEmittersInFrame(currentFrame);
            }

            OnChangeFrame?.Invoke(emittersInFrame);
        }

        public void AddFrame()
        {
            Frame frame = new Frame();

            frames.Add(frame);
            currentFrame = frame;
            currentFrameIndex = frames.Count -1;

            OnChangeFrame?.Invoke(GetEmittersInFrame(frame));
        }

        private List<VisualEmitterDetail> GetEmittersInFrame(Frame frame)
        {
            emittersInFrame.Clear();

            foreach (var emitterDetail in frame.Matrix.Values)
            {
                emittersInFrame.Add(new VisualEmitterDetail(emitterDetail.Position, EmitterUtils.GetColor(emitterDetail.EmitterId)));
            }

            return emittersInFrame;
        }

        public void SetEmitter(int emitterId)
        {
            currentEmitterId = emitterId;
        }

        public void RemoveEmitterFromCell(Vector3Int position)
        {
            currentFrame.RemoveEmitter(position);

            OnRemoveEmitter?.Invoke(position);
        }

        public void AddEmitterAtPosition(Vector3Int cellPosition)
        {
            Debug.Log("MatrixManager AddEmitterAtPosition: " + cellPosition);

            currentFrame.AddEmitter(new EmitterDetail(cellPosition, currentEmitterId));

            OnAddEmitter?.Invoke(cellPosition, EmitterUtils.GetColor(currentEmitterId));
        }

        public List<Frame> Frames { get => frames; }
    }
}