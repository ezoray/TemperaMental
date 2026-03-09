using System.Collections.Generic;
using UnityEngine;

namespace Tempera.Mental.Frames
{
    public class Frame
    {
        Dictionary<Vector2Int, EmitterDetail> matrix;

        public Frame()
        {
            matrix = new Dictionary<Vector2Int, EmitterDetail>();
        }

        public Frame(Frame otherFrame)
        {
            matrix = new Dictionary<Vector2Int, EmitterDetail>(otherFrame.Matrix);
        }

        public void ClearEmitters()
        {
            matrix.Clear();
        }

        public bool CheckSameEmitterAtPosition(Vector2Int cellPosition, int currentEmitterId)
        {
            if (!matrix.TryGetValue(cellPosition, out var existingEmitterDetail)) return false;

            return existingEmitterDetail.EmitterId == currentEmitterId;
        }

        public bool TryRemoveEmitter(Vector2Int position)
        {
            return matrix.Remove(position);
        }

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            matrix[emitterDetail.Position] = emitterDetail;
        }

        public Dictionary<Vector2Int, EmitterDetail> Matrix { get => matrix; }
    }
}
