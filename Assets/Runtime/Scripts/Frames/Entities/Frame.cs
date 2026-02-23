using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tempera.Mental.Frames
{
    public class Frame
    {
        Dictionary<Vector3Int, EmitterDetail> matrix;

        public Frame()
        {
            matrix = new Dictionary<Vector3Int, EmitterDetail>();
        }

        public Frame(Frame otherFrame)
        {
            // Pass the old dictionary into the new one's constructor.
            // Because EmitterDetail is a struct, this performs a "Value Copy"
            // for every entry automatically.
            matrix = new Dictionary<Vector3Int, EmitterDetail>(otherFrame.Matrix);
        }

        public void RemoveEmitter(Vector3Int position)
        {
            if (matrix.ContainsKey(position))
            {
                matrix.Remove(position);
            }
        }

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            if (matrix.ContainsKey(emitterDetail.Position))
            {
                matrix.Remove(emitterDetail.Position);
            }

            matrix.Add(emitterDetail.Position, emitterDetail);
        }

        public Dictionary<Vector3Int, EmitterDetail> Matrix { get => matrix; }
    }
}
