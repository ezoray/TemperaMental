using System;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Frames
{
    public class Frame
    {
        readonly int width;
        readonly int height;

        private readonly EmitterDetail?[] grid;

        public Frame(int width, int height)
        {
            this.width = width;
            this.height = height;
            grid = new EmitterDetail?[width * height];
        }

        public Frame(Frame otherFrame)
        {
            width = otherFrame.width;
            height = otherFrame.height;
            grid = new EmitterDetail?[width * height];

            Array.Copy(otherFrame.grid, grid, grid.Length);
        }   

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            grid[GetIndex(emitterDetail.Position.x, emitterDetail.Position.y)] = emitterDetail;
        }

        public bool TryRemoveEmitter(Vector2Int position)
        {
            int index = GetIndex(position.x, position.y);

            if (!grid[index].HasValue) return false;

            grid[index] = null;
            return true;
        }

        public bool CheckSameEmitterAtPosition(Vector2Int pos, int currentEmitterId)
        {
            return grid[GetIndex(pos.x, pos.y)]?.EmitterId == currentEmitterId;
        }

        public void ClearEmitters()
        {
            Array.Clear(grid, 0, grid.Length);
        }

        // pass in action method as parameter to process active emitters rather than having to pass out a list
        public void ActionActiveEmitters(Action<EmitterDetail> action)
        {
            for (int i = 0; i < grid.Length; i++)
            {
                if (grid[i].HasValue)
                    action(grid[i].Value);
            }
        }

        private int GetIndex(int x, int y)
        {
#if UNITY_EDITOR
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                throw new System.ArgumentOutOfRangeException($"Position ({x},{y}) out of grid bounds");
            }
#endif
            return y * width + x;
        }
    }
}