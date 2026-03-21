using System.Collections.Generic;
using UnityEngine;

namespace Tempera.Mental.Frames
{
    public class Frame
    {
        private const int WIDTH = 8;
        private const int HEIGHT = 8;

        private readonly EmitterDetail?[] matrix;

        public Frame()
        {
            matrix = new EmitterDetail?[WIDTH * HEIGHT];
        }

        public Frame(Frame otherFrame)
        {
            matrix = new EmitterDetail?[WIDTH * HEIGHT];
            System.Array.Copy(otherFrame.matrix, matrix, matrix.Length);
        }

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            matrix[GetIndex(emitterDetail.Position.x, emitterDetail.Position.y)] = emitterDetail;
        }

        public bool TryRemoveEmitter(Vector2Int position)
        {
            int index = GetIndex(position.x, position.y);

            if (!matrix[index].HasValue) return false;

            matrix[index] = null;
            return true;
        }

        public bool CheckSameEmitterAtPosition(Vector2Int pos, int currentEmitterId)
        {
            return matrix[GetIndex(pos.x, pos.y)]?.EmitterId == currentEmitterId;
        }

        public void ClearEmitters()
        {
            System.Array.Clear(matrix, 0, matrix.Length);
        }

        public void ListActiveEmitters(List<EmitterDetail> results)
        {
            results.Clear();
            for (int i = 0; i < matrix.Length; i++)
            {
                if (matrix[i].HasValue)
                    results.Add(matrix[i].Value);
            }
        }

        private int GetIndex(int x, int y)
        {
#if UNITY_EDITOR
            if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT)
            {
                throw new System.ArgumentOutOfRangeException($"Position ({x},{y}) out of grid bounds");
            }
#endif
            return y * WIDTH + x;
        }
    }
}